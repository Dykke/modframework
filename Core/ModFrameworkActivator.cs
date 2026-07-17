// ModFrameworkActivator.cs
// ModFramework v6.0.1
//
// Single entry point for mods to register with the framework. Mods call
// OnActivate in their own OnActivate, and OnDeactivate in their own
// OnDeactivate. The activator mints a ModIdentity (internal constructor)
// that the mod can then use for all subsequent privileged framework calls.
//
// v6.0.1 additions:
//   - OnActivate(ModBehaviour) overload — looks up the corresponding
//     ModController.DLLMod via ModController.Instance.Mods and dispatches
//     to the existing OnActivate(ModController.DLLMod, ...). Lets mods
//     call OnActivate(this) directly from inside a ModBehaviour.OnActivate()
//     without manually bridging to ModController.DLLMod (the v6.0 design
//     assumed ModBehaviour IS a ModController.DLLMod, but they are
//     separate types in the game's type system).
//
// Per-session: each game launch, the activator clears all entries.
// Per-mod: only one ModIdentity per ModId. Re-registration is a no-op.

using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ModFramework.Core
{
    /// <summary>
    /// Mod registration entry point. Mods call OnActivate from their
    /// OnActivate to get a ModIdentity; they call OnDeactivate from their
    /// OnDeactivate to clean up.
    /// </summary>
    [ModFrameworkPublicAPI("v6.0", Reason = "The single entry point for v6.0 mod registration")]
    public static class ModFrameworkActivator
    {
        private const string Tag = "[ModFramework.Activator]";

        /// <summary>
        /// Register a mod with the framework. Reads the mod's meta.tyd (if present)
        /// to extract the Permissions, mints a ModIdentity, and registers it.
        /// Returns the ModIdentity for use in subsequent privileged calls.
        /// </summary>
        /// <param name="dllMod">The game's ModController.DLLMod instance (this in most mods).</param>
        /// <param name="callingAssembly">The mod's main assembly (usually Assembly.GetCallingAssembly()).</param>
        /// <param name="explicitPermissions">Optional. If supplied, skips reading meta.tyd and uses these permissions directly. Useful for test mods.</param>
        public static ModIdentity OnActivate(ModController.DLLMod dllMod, Assembly callingAssembly = null, Permission explicitPermissions = Permission.None)
        {
            FrameworkSignatureCheck.RequireValid();

            if (dllMod == null)
            {
                throw new ArgumentNullException("dllMod",
                    "ModFrameworkActivator.OnActivate: dllMod is null. Pass `this` from your mod's OnActivate.");
            }

            string modId = SafeReadModId(dllMod);
            string displayName = SafeReadDisplayName(dllMod);
            string folder = SafeReadFolder(dllMod);
            if (callingAssembly == null) callingAssembly = Assembly.GetCallingAssembly();
            string assemblyHash = ComputeAssemblyHash(callingAssembly);

            Permission permissions;
            if (explicitPermissions != Permission.None)
            {
                permissions = explicitPermissions;
            }
            else
            {
                permissions = ReadPermissionsFromMetaTyd(folder, modId);
            }

            var identity = new ModIdentity(modId, displayName, assemblyHash, permissions);
            ModRegistry.Register(identity, dllMod, callingAssembly);

            AuditLog.Log(modId, displayName, "ACTIVATE", folder, "OK",
                "permissions=" + permissions + " assemblyHash=" + assemblyHash);

            Debug.Log(Tag + " Activated mod '" + displayName + "' (" + modId +
                ") with permissions " + permissions);
            return identity;
        }

        /// <summary>
        /// v6.0.1 convenience overload: register a mod from inside a ModBehaviour subclass.
        /// Looks up the corresponding <see cref="ModController.DLLMod"/> via
        /// <c>ModController.Instance.Mods</c> by matching the calling assembly's file name
        /// against each DLLMod's <c>FileName</c>, then dispatches to the canonical
        /// <see cref="OnActivate(ModController.DLLMod, Assembly, Permission)"/> overload.
        /// </summary>
        /// <param name="modBehaviour">The ModBehaviour instance (typically <c>this</c> from inside OnActivate).</param>
        /// <param name="callingAssembly">Optional. The mod's main assembly. If null, uses <c>modBehaviour.GetType().Assembly</c>.</param>
        /// <param name="explicitPermissions">Optional. If supplied, skips reading meta.tyd and uses these permissions directly. Useful for test mods.</param>
        /// <returns>The minted <see cref="ModIdentity"/>.</returns>
        /// <exception cref="ArgumentNullException">thrown if <paramref name="modBehaviour"/> is null.</exception>
        /// <exception cref="InvalidOperationException">thrown if no matching <see cref="ModController.DLLMod"/> can be found in <c>ModController.Instance.Mods</c>.</exception>
        [ModFrameworkPublicAPI("v6.0.1", Reason = "Convenience overload for ModBehaviour subclasses (added v6.0.1 to unblock ModFrameworkExample compile)")]
        public static ModIdentity OnActivate(ModBehaviour modBehaviour, Assembly callingAssembly = null, Permission explicitPermissions = Permission.None)
        {
            if (modBehaviour == null)
            {
                throw new ArgumentNullException("modBehaviour",
                    "ModFrameworkActivator.OnActivate: modBehaviour is null. Pass `this` from your mod's OnActivate.");
            }

            // Default the assembly to the mod's own assembly (the assembly that defines
            // the ModBehaviour subclass). Assembly.GetCallingAssembly() inside the framework
            // would return the framework's assembly, not the mod's, because of how the
            // call chain crosses the framework boundary.
            if (callingAssembly == null)
            {
                callingAssembly = modBehaviour.GetType().Assembly;
            }

            // Preferred path: the game sets ModBehaviour.ParentMod to the owning
            // ModController.DLLMod in ModController.FinalizeAssembly (ModController.cs:101)
            // BEFORE it calls the behaviour's OnActivate, so it is already populated by
            // the time we get here. This is the robust way to resolve the DLLMod because
            // it does NOT depend on Assembly.Location, which is EMPTY for mods the game
            // loads via ScriptDomain.LoadAssembly (Assembly.Load(byte[])) — the empty
            // location is why the old assembly-file-name lookup always failed with
            // "could not find a ModController.DLLMod for assembly ''".
            ModController.DLLMod dllMod = modBehaviour.ParentMod;

            // Fallback: only used if ParentMod is somehow null (e.g. the mod created
            // the GameObject itself instead of letting the game's loader do it).
            if (dllMod == null)
            {
                dllMod = FindDllModForAssembly(callingAssembly);
            }

            if (dllMod == null)
            {
                string asmLocation = callingAssembly != null ? callingAssembly.Location : "<null>";
                if (string.IsNullOrEmpty(asmLocation))
                {
                    asmLocation = callingAssembly != null ? callingAssembly.FullName : "<null>";
                }
                throw new InvalidOperationException(
                    "ModFrameworkActivator.OnActivate(ModBehaviour): could not find a ModController.DLLMod for assembly '" +
                    asmLocation + "'. " +
                    "This usually means the ModBehaviour GameObject was not created by the game's mod loader. " +
                    "If you created the GameObject yourself, pass a ModController.DLLMod explicitly via " +
                    "OnActivate(ModController.DLLMod, Assembly, Permission) instead.");
            }

            return OnActivate(dllMod, callingAssembly, explicitPermissions);
        }

        /// <summary>
        /// Unregister a mod. Call from your mod's OnDeactivate.
        /// </summary>
        public static void OnDeactivate(ModIdentity identity)
        {
            if (identity == null) return;
            ModRegistry.Unregister(identity.ModId);
            AuditLog.Log(identity.ModId, identity.DisplayName, "DEACTIVATE", "", "OK", "sessionEnded=" + identity.SessionNonce);
        }

        // ---- internals ----

        /// <summary>
        /// v6.0.1 helper: find the <see cref="ModController.DLLMod"/> whose FileName matches
        /// the calling assembly's file name. Used by the
        /// <see cref="OnActivate(ModBehaviour, Assembly, Permission)"/> overload to bridge
        /// from a ModBehaviour instance to its owning DLLMod.
        /// </summary>
        private static ModController.DLLMod FindDllModForAssembly(Assembly asm)
        {
            if (asm == null) return null;
            try
            {
                if (ModController.Instance == null) return null;
                var mods = ModController.Instance.Mods;
                if (mods == null) return null;

                // ModController.DLLMod.FileName is stored WITHOUT the ".dll" extension
                // (ModController.LoadMod passes Path.GetFileNameWithoutExtension(path) to
                // FinalizeAssembly). Match against the assembly's simple name, which is
                // also extension-less and is available even when Assembly.Location is empty.
                string simpleName = (asm.GetName() != null) ? asm.GetName().Name : null;

                // Assembly.Location is EMPTY for byte[]-loaded mod assemblies, so only
                // use it as a secondary signal when it happens to be present.
                string location = asm.Location;
                string fileNoExt = !string.IsNullOrEmpty(location)
                    ? Path.GetFileNameWithoutExtension(location)
                    : null;

                foreach (var m in mods)
                {
                    if (m == null || string.IsNullOrEmpty(m.FileName)) continue;
                    if ((simpleName != null && string.Equals(m.FileName, simpleName, StringComparison.OrdinalIgnoreCase)) ||
                        (fileNoExt != null && string.Equals(m.FileName, fileNoExt, StringComparison.OrdinalIgnoreCase)))
                    {
                        return m;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning(Tag + " FindDllModForAssembly failed: " + ex.Message);
            }
            return null;
        }

        private static string SafeReadModId(ModController.DLLMod dllMod)
        {
            try
            {
                // The ModController.DLLMod base class doesn't expose a public ID
                // property at compile time — but it does have FileName and Meta.Name.
                // Use whichever is available.
                if (dllMod == null) return string.Empty;
                if (!string.IsNullOrEmpty(dllMod.FileName)) return dllMod.FileName;
                var meta = dllMod.Meta;
                if (meta != null && !string.IsNullOrEmpty(meta.Name)) return meta.Name;
                return string.Empty;
            }
            catch { return string.Empty; }
        }

        private static string SafeReadDisplayName(ModController.DLLMod dllMod)
        {
            try
            {
                if (dllMod == null) return "Unknown";
                var meta = dllMod.Meta;
                if (meta != null && !string.IsNullOrEmpty(meta.Name)) return meta.Name;
                if (!string.IsNullOrEmpty(dllMod.FileName)) return dllMod.FileName;
                return "Unknown";
            }
            catch { return "Unknown"; }
        }

        private static string SafeReadFolder(ModController.DLLMod dllMod)
        {
            try { return dllMod.FolderPath() ?? string.Empty; } catch { return string.Empty; }
        }

        private static string ComputeAssemblyHash(Assembly asm)
        {
            if (asm == null) return string.Empty;
            try
            {
                var location = asm.Location;
                if (string.IsNullOrEmpty(location) || !File.Exists(location)) return string.Empty;
                var bytes = File.ReadAllBytes(location);
                using (var sha = SHA256.Create())
                {
                    var hash = sha.ComputeHash(bytes);
                    var sb = new StringBuilder(hash.Length * 2);
                    for (int i = 0; i < hash.Length; i++) sb.Append(hash[i].ToString("x2"));
                    return sb.ToString();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning(Tag + " ComputeAssemblyHash failed: " + ex.Message);
                return string.Empty;
            }
        }

        private static Permission ReadPermissionsFromMetaTyd(string folder, string modId)
        {
            if (string.IsNullOrEmpty(folder)) return Permission.None;
            var metaPath = Path.Combine(folder, "meta.tyd");
            if (!File.Exists(metaPath))
            {
                Debug.LogWarning(Tag + " No meta.tyd in " + folder + " for mod '" + modId + "'. Defaulting to Permission.None (every privileged call will throw).");
                return Permission.None;
            }
            try
            {
                var lines = File.ReadAllLines(metaPath);
                foreach (var raw in lines)
                {
                    var line = raw == null ? string.Empty : raw.Trim();
                    if (line.Length == 0 || line.StartsWith(";", StringComparison.Ordinal)) continue;
                    int sp = line.IndexOfAny(new[] { ' ', '\t' });
                    if (sp <= 0) continue;
                    var key = line.Substring(0, sp).Trim();
                    var val = line.Substring(sp + 1).Trim();
                    // Tolerate a trailing colon on the key. The meta.tyd convention writes
                    // "Permissions: Patcher, ..." (with colon) while other tyd keys omit it,
                    // so strip a single trailing ':' before comparing.
                    if (key.EndsWith(":", StringComparison.Ordinal))
                    {
                        key = key.Substring(0, key.Length - 1).Trim();
                    }
                    if (string.Equals(key, "Permissions", StringComparison.OrdinalIgnoreCase))
                    {
                        // The game re-serializes meta.tyd into canonical TYD form: Key "value".
                        // So the value we read here is normally wrapped in double quotes, e.g.
                        //   Permissions "Patcher, ServiceRegister, EventPublish"
                        // Older colon-style sources (Permissions: Patcher, ...) get rewritten by
                        // the game to Permissions ": Patcher, ..." (the colon absorbed into the
                        // quoted value), so strip surrounding quotes AND a stray leading colon
                        // before handing the flag list to PermissionParser.
                        if (val.Length >= 2 && val[0] == '"' && val[val.Length - 1] == '"')
                        {
                            val = val.Substring(1, val.Length - 2).Trim();
                        }
                        if (val.StartsWith(":", StringComparison.Ordinal))
                        {
                            val = val.Substring(1).Trim();
                        }
                        return PermissionParser.Parse(val);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(Tag + " Failed to parse meta.tyd for '" + modId + "': " + ex.Message);
            }
            return Permission.None;
        }
    }
}

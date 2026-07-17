// ModDependencies.cs
// ModFramework v6.0
//
// Dependency declaration and verification system.
//
// USAGE:
//   In your mod's OnActivate (after ModFrameworkActivator.OnActivate), call:
//
//       ModIdentity id = ModFrameworkActivator.OnActivate(this);
//       ModDependencies.VerifyOrWarn(id, this,
//           new ModDependency { Name = "0Harmony.dll",     Kind = ModDependencyKind.File, Severity = ModDependencySeverity.Required },
//           new ModDependency { Name = "ModFramework.dll", Kind = ModDependencyKind.File, Severity = ModDependencySeverity.Required },
//           new ModDependency { Name = "SomeOptionalMod",  Kind = ModDependencyKind.Mod,  Severity = ModDependencySeverity.Optional, DownloadUrl = "https://twitch.tv/..." }
//       );
//
// If a REQUIRED dependency is missing, the player gets an in-game dialog
// explaining what's wrong and where to get it. The mod author is expected
// to abort their own initialization in that case (the mod will still
// load — they need to be defensive about it).
//
// v6.0 changes:
//   - Class is marked with [ModFrameworkPublicAPI("v6.0")] — it's part of
//     the curated v6.0 public API surface.
//   - ShowMissingMessage / VerifyOrWarn now write an audit log entry so
//     the player can see which mod declared which missing dep in the
//     in-game "Mod Audit Log" window.
//   - No permission check is needed (this is a read-only utility: it only
//     checks File.Exists / Directory.Exists / GameObject.Find; no privileged
//     operations).
//   - C# 5 compatibility: removed C# 6+ string interpolation in favor of
//     string concatenation (the v5.x code used $"..." which is not C# 5).

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ModFramework.Core
{
    /// <summary>What kind of dependency this is. Determines how IsPresent() looks for it.</summary>
    public enum ModDependencyKind
    {
        /// <summary>A file. Look in the mod's Dependencies/ folder first, then mod root, then Managed/.</summary>
        File,
        /// <summary>A folder. Look in the mod's Dependencies/ folder first, then mod root.</summary>
        Folder,
        /// <summary>Another installed mod (by name). Matches against ModController.Instance.Mods.</summary>
        Mod,
        /// <summary>A DLL in the game's Managed folder. Only checks there.</summary>
        ManagedAssembly,
    }

    /// <summary>How critical this dependency is. Determines whether the missing-dep dialog is shown.</summary>
    public enum ModDependencySeverity
    {
        /// <summary>Mod cannot run without this. A missing required dep shows a dialog and the mod should bail.</summary>
        Required,
        /// <summary>Mod runs but with reduced functionality. Logged but no dialog shown.</summary>
        Optional,
    }

    /// <summary>Declares a single dependency for a mod.</summary>
    public class ModDependency
    {
        /// <summary>File name (e.g. "0Harmony.dll") OR folder name OR mod name. Required.</summary>
        public string Name;

        /// <summary>How to resolve the dependency. Defaults to File.</summary>
        public ModDependencyKind Kind = ModDependencyKind.File;

        /// <summary>How critical this dependency is. Defaults to Required.</summary>
        public ModDependencySeverity Severity = ModDependencySeverity.Required;

        /// <summary>If non-null, shown in the missing-dep dialog so the player can grab the missing piece.</summary>
        public string DownloadUrl;

        /// <summary>Override the search path. If null, the standard search order is used.</summary>
        public string CustomPath;

        /// <summary>
        /// Optional well-known GameObject name registered by an addendum DLL mod.
        /// When set, <see cref="ModDependencies.IsPresent"/> returns true as soon as
        /// <c>GameObject.Find(ServiceObjectName)</c> succeeds (live check — avoids
        /// activation-order races). Prefer this for optional cross-mod features.
        /// </summary>
        public string ServiceObjectName;

        /// <summary>
        /// DLLMods folder name when it differs from <see cref="Name"/> (display/meta name).
        /// Example: Name = "LogoScape - Native File Browser", FolderName = "LogoScapeDependency".
        /// </summary>
        public string FolderName;

        public ModDependency() { }
        public ModDependency(string name, ModDependencyKind kind, ModDependencySeverity severity = ModDependencySeverity.Required)
        {
            Name = name;
            Kind = kind;
            Severity = severity;
        }
    }

    /// <summary>
    /// Dependency verification utilities. All public methods are safe to call from anywhere
    /// in the mod lifecycle (Initialize, OnActivate, etc.) — they never throw.
    ///
    /// v6.0: also writes to the framework's audit log so the player can see which
    /// mods declared which missing dependencies in the in-game "Mod Audit Log" window.
    /// </summary>
    [ModFrameworkPublicAPI("v6.0", Reason = "Dependency verification")]
    public static class ModDependencies
    {
        private const string Tag = "[ModDependencies]";

        // ------------------------------------------------------------------
        // Public API
        // ------------------------------------------------------------------

        /// <summary>
        /// Returns the list of dependencies that are NOT satisfied. Required + missing are returned;
        /// Optional + missing are not (but logged as warnings).
        /// </summary>
        public static List<ModDependency> Check(ModController.DLLMod parentMod, params ModDependency[] deps)
        {
            var missing = new List<ModDependency>();
            if (deps == null || deps.Length == 0) return missing;

            foreach (var dep in deps)
            {
                if (dep == null || string.IsNullOrEmpty(dep.Name)) continue;
                if (IsPresent(parentMod, dep)) continue;

                if (dep.Severity == ModDependencySeverity.Required)
                {
                    missing.Add(dep);
                    Debug.LogWarning(Tag + " MISSING REQUIRED dep '" + dep.Name + "' (" + dep.Kind + ") for mod " + SafeModName(parentMod));
                }
                else
                {
                    Debug.LogWarning(Tag + " Missing optional dep '" + dep.Name + "' (" + dep.Kind + ") for mod " + SafeModName(parentMod) + " — running with reduced functionality");
                }
            }
            return missing;
        }

        /// <summary>True if every required dep is present (optional deps are not enforced).</summary>
        public static bool ArePresent(ModController.DLLMod parentMod, params ModDependency[] deps)
        {
            return Check(parentMod, deps).Count == 0;
        }

        /// <summary>
        /// Show an in-game error dialog listing the missing required dependencies.
        /// Silently no-ops if the list is empty or the dialog system isn't ready yet.
        ///
        /// v6.0: also writes a DIALOG_SHOWN line to the audit log so the player can see
        /// which mod showed a missing-dep dialog.
        /// </summary>
        public static void ShowMissingMessage(IList<ModDependency> missing, string modTitle = null)
        {
            if (missing == null || missing.Count == 0) return;
            try
            {
                string title = string.IsNullOrEmpty(modTitle) ? "Mod dependency missing" : (modTitle + " — dependency missing");
                string body = BuildMessage(missing, modTitle);
                WindowManager.SpawnDialog(body, true, DialogWindow.DialogType.Error);
                // v6.0: audit-log the dialog
                try { AuditLog.Log("<dialog>", modTitle ?? "<unknown>", "DEP_DIALOG_SHOWN", title, "OK", missing.Count + " missing dep(s)"); }
                catch { /* AuditLog may not be initialized yet */ }
            }
            catch (Exception ex)
            {
                Debug.LogError(Tag + " Could not show missing-dep dialog: " + ex);
            }
        }

        /// <summary>
        /// One-shot: check, log, and show dialog if required deps are missing. Returns true if all
        /// required deps are present (or only optional ones were missing). The mod author can use
        /// the return value to decide whether to abort Initialize().
        /// </summary>
        public static bool VerifyOrWarn(ModController.DLLMod parentMod, params ModDependency[] deps)
        {
            var missing = Check(parentMod, deps);
            if (missing.Count > 0)
            {
                ShowMissingMessage(missing, SafeModName(parentMod));
            }
            return missing.Count == 0;
        }

        /// <summary>
        /// v6.0.1 convenience overload: <see cref="VerifyOrWarn(ModController.DLLMod, ModDependency[])"/>
        /// that takes a <see cref="ModIdentity"/> instead of a <see cref="ModController.DLLMod"/>.
        /// Looks up the registered DLLMod via <see cref="ModRegistry.GetDLLMod(ModIdentity)"/>
        /// and dispatches to the canonical overload.
        /// </summary>
        /// <returns>True if all required deps are present; false if any required dep is missing or the identity is not registered.</returns>
        [ModFrameworkPublicAPI("v6.0.1", Reason = "Convenience overload for ModIdentity (the v6.0+ handle). Added v6.0.1 to unblock ModFrameworkExample compile.")]
        public static bool VerifyOrWarn(ModIdentity identity, params ModDependency[] deps)
        {
            if (identity == null) return false;
            var dllMod = ModRegistry.GetDLLMod(identity);
            if (dllMod == null)
            {
                Debug.LogWarning(Tag + " VerifyOrWarn: identity '" + identity.ModId + "' is not registered. " +
                    "Call ModFrameworkActivator.OnActivate first.");
                return false;
            }
            return VerifyOrWarn(dllMod, deps);
        }

        /// <summary>True if this single dependency is present. Never throws.</summary>
        public static bool IsPresent(ModController.DLLMod parentMod, ModDependency dep)
        {
            if (dep == null || string.IsNullOrEmpty(dep.Name)) return true; // vacuously true
            try
            {
                // Live service-object check first — dependency may activate after this mod.
                if (!string.IsNullOrEmpty(dep.ServiceObjectName))
                {
                    try
                    {
                        if (GameObject.Find(dep.ServiceObjectName) != null)
                            return true;
                    }
                    catch { /* swallow */ }
                }

                switch (dep.Kind)
                {
                    case ModDependencyKind.File:
                        return CheckFilePaths(parentMod, dep);
                    case ModDependencyKind.Folder:
                        return CheckFolderPaths(parentMod, dep);
                    case ModDependencyKind.Mod:
                        return CheckModInstalled(parentMod, dep);
                    case ModDependencyKind.ManagedAssembly:
                        return File.Exists(Path.Combine(GetManagedFolderSafe(), dep.Name));
                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning(Tag + " IsPresent check failed for '" + dep.Name + "': " + ex.Message);
                return false;
            }
        }

        // ------------------------------------------------------------------
        // Internals
        // ------------------------------------------------------------------

        private static bool CheckFilePaths(ModController.DLLMod parentMod, ModDependency dep)
        {
            if (!string.IsNullOrEmpty(dep.CustomPath))
            {
                return File.Exists(dep.CustomPath) || Directory.Exists(dep.CustomPath);
            }
            string modRoot = SafeFolderPath(parentMod);
            if (!string.IsNullOrEmpty(modRoot))
            {
                if (File.Exists(Path.Combine(modRoot, dep.Name))) return true;
                string depsFolder = Path.Combine(modRoot, "Dependencies");
                if (File.Exists(Path.Combine(depsFolder, dep.Name))) return true;
            }
            string managed = GetManagedFolderSafe();
            if (!string.IsNullOrEmpty(managed) && File.Exists(Path.Combine(managed, dep.Name))) return true;
            return false;
        }

        private static bool CheckFolderPaths(ModController.DLLMod parentMod, ModDependency dep)
        {
            if (!string.IsNullOrEmpty(dep.CustomPath) && Directory.Exists(dep.CustomPath)) return true;
            string modRoot = SafeFolderPath(parentMod);
            if (!string.IsNullOrEmpty(modRoot))
            {
                if (Directory.Exists(Path.Combine(modRoot, dep.Name))) return true;
                if (Directory.Exists(Path.Combine(modRoot, "Dependencies", dep.Name))) return true;
            }
            return false;
        }

        private static bool CheckModInstalled(ModController.DLLMod parentMod, ModDependency dep)
        {
            // First, is the mod currently loaded?
            try
            {
                if (ModController.Instance != null && ModController.Instance.Mods != null)
                {
                    foreach (var m in ModController.Instance.Mods)
                    {
                        if (m == null || m.Meta == null) continue;
                        if (NameMatchesMod(dep.Name, m)) return true;
                        if (!string.IsNullOrEmpty(dep.FolderName) && NameMatchesMod(dep.FolderName, m)) return true;
                    }
                }
            }
            catch { /* swallow — game state not ready */ }

            // Second, is the folder present even if not loaded?
            string modRoot = SafeFolderPath(parentMod);
            if (!string.IsNullOrEmpty(modRoot))
            {
                string parent = Path.GetDirectoryName(modRoot);
                if (!string.IsNullOrEmpty(parent))
                {
                    if (Directory.Exists(Path.Combine(parent, dep.Name))) return true;
                    if (!string.IsNullOrEmpty(dep.FolderName) &&
                        Directory.Exists(Path.Combine(parent, dep.FolderName))) return true;
                }
            }
            return false;
        }

        private static bool NameMatchesMod(string name, ModController.DLLMod mod)
        {
            if (mod == null || string.IsNullOrEmpty(name)) return false;
            try
            {
                if (mod.Meta != null && string.Equals(mod.Meta.Name, name, StringComparison.OrdinalIgnoreCase))
                    return true;
                if (string.Equals(mod.FileName, name, StringComparison.OrdinalIgnoreCase))
                    return true;
                string folder = SafeFolderPath(mod);
                if (!string.IsNullOrEmpty(folder))
                {
                    string leaf = Path.GetFileName(folder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                    if (string.Equals(leaf, name, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            catch { }
            return false;
        }

        private static string GetManagedFolderSafe()
        {
            try
            {
                // GameRoot/Software Inc_Data/Managed
                string root = Application.dataPath;
                if (string.IsNullOrEmpty(root)) return null;
                return Path.Combine(root, "Managed");
            }
            catch
            {
                return null;
            }
        }

        private static string SafeFolderPath(ModController.DLLMod parentMod)
        {
            try
            {
                if (parentMod == null) return null;
                return parentMod.FolderPath();
            }
            catch
            {
                return null;
            }
        }

        private static string SafeModName(ModController.DLLMod parentMod)
        {
            try
            {
                if (parentMod == null) return "<unknown mod>";
                if (parentMod.Meta != null && !string.IsNullOrEmpty(parentMod.Meta.Name)) return parentMod.Meta.Name;
                if (!string.IsNullOrEmpty(parentMod.FileName)) return parentMod.FileName;
                return "<unnamed mod>";
            }
            catch
            {
                return "<unknown mod>";
            }
        }

        private static string BuildMessage(IList<ModDependency> missing, string modTitle)
        {
            var sb = new System.Text.StringBuilder();
            if (!string.IsNullOrEmpty(modTitle))
            {
                sb.AppendLine("'" + modTitle + "' could not start because the following required dependencies are missing:");
            }
            else
            {
                sb.AppendLine("The following required dependencies are missing:");
            }
            sb.AppendLine();
            int i = 1;
            foreach (var d in missing)
            {
                sb.AppendLine("  " + i + ". " + d.Name + " (" + d.Kind + ")");
                i++;
                if (!string.IsNullOrEmpty(d.DownloadUrl))
                {
                    sb.AppendLine("     Get it from: " + d.DownloadUrl);
                }
            }
            sb.AppendLine();
            sb.AppendLine("Quit the game, install the missing items, then try again.");
            return sb.ToString();
        }
    }
}

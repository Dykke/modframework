// ModHarmony.cs
// ModFramework v6.1
//
// Centralized Harmony wrapper. Cleans up the boilerplate `new Harmony(id).PatchAll()` pattern
// in mod Initialize() and provides clean unpatching for mod unload.
//
// v6.0: New methods take ModIdentity + enforce Permission.HarmonyPatch.
//
// v6.1: Removed the 4 v5.x [Obsolete] string-modId variants
// (CreateInstance(string) / CreateAndPatchAll(string, Assembly) /
// UnpatchAll(Harmony) / PatchCount(Harmony)). These intentionally skipped
// the permission check for back-compat, but a malicious Nexus DLL could
// exploit that to patch any method without declaring Permission.HarmonyPatch.
// NormalizeId was made private (still used internally by IdFromIdentity).
//
// USAGE (v6.0+):
//   var identity = ModFrameworkActivator.OnActivate(this);  // in your mod's OnActivate
//   _harmony = ModHarmony.CreateAndPatchAll(identity);
//   // ... in your mod's OnDeactivate:
//   ModHarmony.UnpatchAll(_harmony);
//   ModFrameworkActivator.OnDeactivate(identity);

using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ModFramework.Core
{
    public static class ModHarmony
    {
        private const string Tag = "[ModHarmony]";

        // ==================================================================
        // v6.0 API — takes ModIdentity + permission check + audit log
        // ==================================================================

        /// <summary>Create a new Harmony instance. Requires Permission.HarmonyRead.</summary>
        [ModFrameworkPublicAPI("v6.0")]
        public static Harmony CreateInstance(ModIdentity id)
        {
            SecurityGuards.RequirePermission(id, Permission.HarmonyRead);
            string harmId = IdFromIdentity(id);
            try
            {
                var harmony = new Harmony(harmId);
                AuditLog.Log(id == null ? null : id.ModId, id == null ? null : id.DisplayName,
                    "HARMONY_CREATE", harmId, "OK", "");
                return harmony;
            }
            catch (Exception ex)
            {
                AuditLog.Log(id == null ? null : id.ModId, id == null ? null : id.DisplayName,
                    "HARMONY_CREATE", harmId, "ERROR", ex.GetType().Name);
                Debug.LogError(string.Format("{0} Failed to create Harmony instance '{1}': {2}", Tag, harmId, ex.Message));
                return null;
            }
        }

        /// <summary>Create a Harmony instance and call PatchAll on it. Requires Permission.HarmonyPatch.</summary>
        [ModFrameworkPublicAPI("v6.0")]
        public static Harmony CreateAndPatchAll(ModIdentity id, Assembly assembly = null)
        {
            SecurityGuards.RequirePermission(id, Permission.HarmonyPatch);
            string harmId = IdFromIdentity(id);
            try
            {
                var harmony = new Harmony(harmId);
                assembly = assembly ?? (id == null ? Assembly.GetCallingAssembly() : ModRegistry.GetAssembly(id));
                if (assembly == null) assembly = Assembly.GetCallingAssembly();
                harmony.PatchAll(assembly);
                int count = PatchCount(harmony);
                AuditLog.Log(id == null ? null : id.ModId, id == null ? null : id.DisplayName,
                    "HARMONY_PATCHALL", harmId, "OK", count + " methods patched");
                Debug.Log(string.Format("{0} Patched {1} methods with id '{2}'", Tag, count, harmId));
                return harmony;
            }
            catch (Exception ex)
            {
                AuditLog.Log(id == null ? null : id.ModId, id == null ? null : id.DisplayName,
                    "HARMONY_PATCHALL", harmId, "ERROR", ex.GetType().Name);
                Debug.LogError(string.Format("{0} PatchAll failed for '{1}': {2}", Tag, harmId, ex.Message));
                return null;
            }
        }

        /// <summary>Unpatch every method that this Harmony instance has patched. Requires Permission.HarmonyUnpatch.</summary>
        [ModFrameworkPublicAPI("v6.0")]
        public static void UnpatchAll(ModIdentity id, Harmony harmony)
        {
            SecurityGuards.RequirePermission(id, Permission.HarmonyUnpatch);
            if (harmony == null) return;
            try
            {
                int before = PatchCount(harmony);
                harmony.UnpatchAll(harmony.Id);
                AuditLog.Log(id == null ? null : id.ModId, id == null ? null : id.DisplayName,
                    "HARMONY_UNPATCHALL", harmony.Id, "OK", before + " methods removed");
                Debug.Log(string.Format("{0} Unpatched all methods from '{1}'", Tag, harmony.Id));
            }
            catch (Exception ex)
            {
                Debug.LogWarning(string.Format("{0} UnpatchAll failed for '{1}': {2}", Tag, harmony.Id, ex.Message));
            }
        }

        /// <summary>Return the number of methods currently patched by this harmony instance. Requires Permission.HarmonyRead.</summary>
        [ModFrameworkPublicAPI("v6.0")]
        public static int PatchCount(ModIdentity id, Harmony harmony)
        {
            SecurityGuards.RequirePermission(id, Permission.HarmonyRead);
            return PatchCount(harmony);
        }

        // ---- internals (private helpers, not part of public API) ----

        // Internal helper: count the number of methods patched by a harmony
        // instance. Used by the v6.0 public API (PatchCount(ModIdentity, Harmony))
        // and by the CreateAndPatchAll logging. Not a public method — mods that
        // need a count go through PatchCount(ModIdentity, Harmony).
        private static int PatchCount(Harmony harmony)
        {
            if (harmony == null) return 0;
            try
            {
                var methods = harmony.GetPatchedMethods();
                int n = 0;
                foreach (var _ in methods) n++;
                return n;
            }
            catch { return 0; }
        }

        // ---- internals ----

        // v5.x NormalizeId was made private in v6.1 (was public+Obsolete in v6.0/v6.0.1).
        // Still used internally by IdFromIdentity to derive the harmony ID from a ModIdentity.
        private static string NormalizeId(string modId)
        {
            if (string.IsNullOrEmpty(modId)) return modId;
            if (modId.StartsWith("com.", StringComparison.OrdinalIgnoreCase)) return modId;
            return "com." + modId;
        }

        private static string IdFromIdentity(ModIdentity id)
        {
            if (id == null) return "com.modframework.unknown";
            var raw = id.ModId;
            if (string.IsNullOrEmpty(raw)) raw = "com.modframework." + id.SessionNonce.ToString("N");
            return NormalizeId(raw);
        }
    }
}

// ModHarmony.cs
// ModFramework v5.1
//
// Centralized Harmony wrapper. Cleans up the boilerplate `new Harmony(id).PatchAll()` pattern
// in mod Initialize() and provides clean unpatching for mod unload.
//
// USAGE:
//   private Harmony _harmony;
//
//   public override void Initialize(ModController.DLLMod parentMod)
//   {
//       _harmony = ModHarmony.CreateAndPatchAll("com.zicarius.techfrontier");
//   }
//
//   public override void OnDeactivate()
//   {
//       ModHarmony.UnpatchAll(_harmony);
//   }
//
// Also enforces the patcher-ID convention "com.<author>.<modname>" so two mods using
// ModHarmony don't accidentally collide.

using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ModFramework.Core
{
    public static class ModHarmony
    {
        private const string Tag = "[ModHarmony]";

        /// <summary>Default patcher-ID convention. Returns "com.&lt;author&gt;.&lt;modname&gt;" if &lt;author&gt;.&lt;modname&gt; was supplied, else returns the input as-is.</summary>
        public static string NormalizeId(string modId)
        {
            if (string.IsNullOrEmpty(modId)) return modId;
            if (modId.StartsWith("com.", StringComparison.OrdinalIgnoreCase)) return modId;
            return $"com.{modId}";
        }

        /// <summary>Create a new Harmony instance. The mod author is responsible for calling PatchAll (or specific Patch methods) on it.</summary>
        public static Harmony CreateInstance(string modId)
        {
            string id = NormalizeId(modId);
            try
            {
                return new Harmony(id);
            }
            catch (Exception ex)
            {
                Debug.LogError($"{Tag} Failed to create Harmony instance '{id}': {ex.Message}");
                return null;
            }
        }

        /// <summary>Create a Harmony instance and call PatchAll on it. Defaults to the calling assembly if no assembly is supplied.</summary>
        public static Harmony CreateAndPatchAll(string modId, Assembly assembly = null)
        {
            string id = NormalizeId(modId);
            try
            {
                var harmony = new Harmony(id);
                assembly = assembly ?? Assembly.GetCallingAssembly();
                harmony.PatchAll(assembly);
                Debug.Log($"{Tag} Patched {PatchCount(harmony)} methods with id '{id}'");
                return harmony;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{Tag} PatchAll failed for '{id}': {ex.Message}");
                return null;
            }
        }

        /// <summary>Unpatch every method that this Harmony instance has patched. Safe to call with a null harmony.</summary>
        public static void UnpatchAll(Harmony harmony)
        {
            if (harmony == null) return;
            try
            {
                harmony.UnpatchAll(harmony.Id);
                Debug.Log($"{Tag} Unpatched all methods from '{harmony.Id}'");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{Tag} UnpatchAll failed for '{harmony.Id}': {ex.Message}");
            }
        }

        /// <summary>Return the number of methods currently patched by this harmony instance. Returns 0 if null.</summary>
        public static int PatchCount(Harmony harmony)
        {
            if (harmony == null) return 0;
            try
            {
                var methods = harmony.GetPatchedMethods();
                int n = 0;
                foreach (var _ in methods) n++;
                return n;
            }
            catch
            {
                return 0;
            }
        }
    }
}

// ModLoader.cs
// ModFramework v5.1
//
// Mod discovery and inspection utilities.
//
// USAGE:
//   if (ModLoader.IsModLoaded("SomeOtherMod"))
//   {
//       var otherMod = ModLoader.FindMod("SomeOtherMod");
//       Debug.Log("Other mod's folder: " + ModLoader.GetModFolder("SomeOtherMod"));
//   }
//
//   foreach (var name in ModLoader.GetAllLoadedModNames())
//   {
//       Debug.Log("Loaded: " + name);
//   }
//
// All methods are safe to call from any point in the mod lifecycle. They return
// false / null / empty when the game state is not available (e.g., Initialize()
// may run before ModController.Instance is populated).

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ModFramework.Core
{
    public static class ModLoader
    {
        private const string Tag = "[ModLoader]";

        // ------------------------------------------------------------------
        // Loaded-mod queries
        // ------------------------------------------------------------------

        /// <summary>True if a mod with the given name is currently loaded into the game.</summary>
        public static bool IsModLoaded(string modName)
        {
            return FindMod(modName) != null;
        }

        /// <summary>Find a loaded mod by its meta name OR its file name (case-insensitive).</summary>
        public static ModController.DLLMod FindMod(string modName)
        {
            if (string.IsNullOrEmpty(modName)) return null;
            try
            {
                if (ModController.Instance == null || ModController.Instance.Mods == null) return null;
                foreach (var m in ModController.Instance.Mods)
                {
                    if (m == null) continue;
                    if (ModNameMatches(m, modName)) return m;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{Tag} FindMod failed for '{modName}': {ex.Message}");
            }
            return null;
        }

        /// <summary>List of all loaded mods' display names (Meta.Name, or FileName fallback).</summary>
        public static List<string> GetAllLoadedModNames()
        {
            var names = new List<string>();
            try
            {
                if (ModController.Instance == null || ModController.Instance.Mods == null) return names;
                foreach (var m in ModController.Instance.Mods)
                {
                    if (m == null) continue;
                    string n = null;
                    try { n = m.Meta?.Name; } catch { }
                    if (string.IsNullOrEmpty(n)) n = m.FileName;
                    if (!string.IsNullOrEmpty(n) && !names.Contains(n)) names.Add(n);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{Tag} GetAllLoadedModNames failed: {ex.Message}");
            }
            return names;
        }

        /// <summary>Snapshot list of all loaded DLLMod instances. Safe to enumerate — entries may be null.</summary>
        public static List<ModController.DLLMod> GetAllLoadedMods()
        {
            var list = new List<ModController.DLLMod>();
            try
            {
                if (ModController.Instance == null || ModController.Instance.Mods == null) return list;
                list.AddRange(ModController.Instance.Mods);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{Tag} GetAllLoadedMods failed: {ex.Message}");
            }
            return list;
        }

        // ------------------------------------------------------------------
        // Installed (not necessarily loaded) mod queries
        // ------------------------------------------------------------------

        /// <summary>True if a mod folder exists at &lt;parentModFolder&gt;/../&lt;modName&gt;.</summary>
        public static bool IsModInstalled(string modName, ModController.DLLMod relativeTo = null)
        {
            if (string.IsNullOrEmpty(modName)) return false;
            string folder = GetModFolder(modName, relativeTo);
            return !string.IsNullOrEmpty(folder) && Directory.Exists(folder);
        }

        /// <summary>Returns the absolute path to the mod's folder if it's installed, or null. If the mod is loaded, uses the mod's own FolderPath().</summary>
        public static string GetModFolder(string modName, ModController.DLLMod relativeTo = null)
        {
            if (string.IsNullOrEmpty(modName)) return null;
            // Loaded: use the mod's own folder
            var loaded = FindMod(modName);
            if (loaded != null)
            {
                try { return loaded.FolderPath(); } catch { }
            }
            // Not loaded: try sibling folder to the calling mod
            try
            {
                string root = GetRoot(relativeTo);
                if (string.IsNullOrEmpty(root)) return null;
                return Path.Combine(root, modName);
            }
            catch { return null; }
        }

        // ------------------------------------------------------------------
        // Internals
        // ------------------------------------------------------------------

        private static bool ModNameMatches(ModController.DLLMod mod, string modName)
        {
            if (mod == null || string.IsNullOrEmpty(modName)) return false;
            try
            {
                if (mod.Meta != null && string.Equals(mod.Meta.Name, modName, StringComparison.OrdinalIgnoreCase))
                    return true;
                if (string.Equals(mod.FileName, modName, StringComparison.OrdinalIgnoreCase))
                    return true;
                string folder = null;
                try { folder = mod.FolderPath(); } catch { }
                if (!string.IsNullOrEmpty(folder))
                {
                    string leaf = Path.GetFileName(folder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                    if (string.Equals(leaf, modName, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            catch { }
            return false;
        }

        private static string GetRoot(ModController.DLLMod relativeTo)
        {
            try
            {
                if (relativeTo == null) return null;
                string folder = relativeTo.FolderPath();
                if (string.IsNullOrEmpty(folder)) return null;
                // Walk up until we find a parent that contains other mod folders.
                // Convention: <game>/<SomeFolder>/<ModName>, so the parent of ModName is the root.
                string parent = Path.GetDirectoryName(folder);
                return parent;
            }
            catch
            {
                return null;
            }
        }
    }
}

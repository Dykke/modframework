// ModFileAccess.cs
// ModFramework v5.1
//
// Safe file I/O for mods.
//
// USAGE:
//   string path = ModFileAccess.GetModDataPath(parentMod, "session.json");
//   ModFileAccess.WriteJson(path, myData);
//   var data = ModFileAccess.TryReadJson<MyData>(path, out var ok);
//
// Provides safe wrappers around System.IO that never throw — they return false / null / default
// on failure and log a warning. All paths are resolved relative to the mod's folder, and parent
// directories are created automatically when writing.
//
// JSON serialization uses Newtonsoft.Json (ILMerged into ModFramework.dll per Q3), which supports
// Dictionaries, HashSets, properties, polymorphic types, and other features that Unity's
// JsonUtility cannot handle. For TechFrontier's TFSessionHeader (which has 5+ Dictionary<,>
// properties) Newtonsoft is the right call.

using System;
using System.IO;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;

namespace ModFramework.Core
{
    public static class ModFileAccess
    {
        private const string Tag = "[ModFileAccess]";

        // ------------------------------------------------------------------
        // Path helpers
        // ------------------------------------------------------------------

        /// <summary>Absolute path to the mod's root folder. Never null but may be empty if mod state is unavailable.</summary>
        public static string GetModFolder(ModController.DLLMod parentMod)
        {
            try { return parentMod?.FolderPath() ?? string.Empty; }
            catch { return string.Empty; }
        }

        /// <summary>Absolute path to &lt;modFolder&gt;/Data/&lt;sub...&gt;. Auto-creates parent directories on write.</summary>
        public static string GetModDataPath(ModController.DLLMod parentMod, params string[] subPaths)
        {
            string root = GetModFolder(parentMod);
            if (string.IsNullOrEmpty(root)) return string.Empty;
            string[] parts;
            if (subPaths == null || subPaths.Length == 0)
            {
                parts = new[] { "Data" };
            }
            else
            {
                parts = new string[subPaths.Length + 1];
                parts[0] = "Data";
                for (int i = 0; i < subPaths.Length; i++)
                {
                    parts[i + 1] = subPaths[i] ?? string.Empty;
                }
            }
            return Path.Combine(parts);
        }

        /// <summary>Absolute path to the game's Managed folder (e.g. ".../Software Inc_Data/Managed").</summary>
        public static string GetManagedFolder()
        {
            try { return Path.Combine(Application.dataPath, "Managed"); }
            catch { return string.Empty; }
        }

        /// <summary>Absolute path to the game's root folder (where the executable lives).</summary>
        public static string GetGameRoot()
        {
            try { return Path.GetDirectoryName(Application.dataPath) ?? string.Empty; }
            catch { return string.Empty; }
        }

        // ------------------------------------------------------------------
        // Existence / directory helpers
        // ------------------------------------------------------------------

        public static bool Exists(string path) => SafeExists(path, asDir: false);
        public static bool DirectoryExists(string path) => SafeExists(path, asDir: true);

        public static void EnsureDirectory(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{Tag} Could not create directory '{path}': {ex.Message}");
            }
        }

        // ------------------------------------------------------------------
        // Text I/O
        // ------------------------------------------------------------------

        public static string ReadText(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            try
            {
                if (!File.Exists(path))
                {
                    Debug.LogWarning($"{Tag} ReadText: file not found '{path}'");
                    return null;
                }
                return File.ReadAllText(path);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{Tag} ReadText failed for '{path}': {ex.Message}");
                return null;
            }
        }

        public static bool WriteText(string path, string content, bool createDirIfMissing = true)
        {
            if (string.IsNullOrEmpty(path)) return false;
            try
            {
                if (createDirIfMissing) EnsureDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, content ?? string.Empty, Encoding.UTF8);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{Tag} WriteText failed for '{path}': {ex.Message}");
                return false;
            }
        }

        public static bool AppendText(string path, string content, bool createDirIfMissing = true)
        {
            if (string.IsNullOrEmpty(path)) return false;
            try
            {
                if (createDirIfMissing) EnsureDirectory(Path.GetDirectoryName(path));
                File.AppendAllText(path, content ?? string.Empty, Encoding.UTF8);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{Tag} AppendText failed for '{path}': {ex.Message}");
                return false;
            }
        }

        // ------------------------------------------------------------------
        // JSON I/O (Newtonsoft.Json)
        // ------------------------------------------------------------------

        /// <summary>Serialize <paramref name="data"/> to JSON and write to <paramref name="path"/>. Returns true on success.</summary>
        public static bool WriteJson<T>(string path, T data, bool prettyPrint = true)
        {
            if (string.IsNullOrEmpty(path)) return false;
            try
            {
                EnsureDirectory(Path.GetDirectoryName(path));
                string json = JsonConvert.SerializeObject(data, prettyPrint ? Formatting.Indented : Formatting.None);
                File.WriteAllText(path, json, new UTF8Encoding(false));
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{Tag} WriteJson<{typeof(T).Name}> failed for '{path}': {ex.Message}");
                return false;
            }
        }

        /// <summary>Read JSON from <paramref name="path"/> and deserialize to <typeparamref name="T"/>. Returns default(T) on failure (logs warning).</summary>
        public static T ReadJson<T>(string path) where T : class
        {
            if (string.IsNullOrEmpty(path)) return null;
            try
            {
                if (!File.Exists(path)) return null;
                string json = File.ReadAllText(path);
                if (string.IsNullOrEmpty(json)) return null;
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{Tag} ReadJson<{typeof(T).Name}> failed for '{path}': {ex.Message}");
                return null;
            }
        }

        /// <summary>Like ReadJson but signals success via the out parameter. Returns default(T) and ok=false on failure.</summary>
        public static bool TryReadJson<T>(string path, out T result) where T : class
        {
            result = ReadJson<T>(path);
            return result != null;
        }

        /// <summary>Serialize <paramref name="data"/> to a JSON string without touching the file system. Returns null on failure.</summary>
        public static string ToJsonString<T>(T data, bool prettyPrint = true)
        {
            try
            {
                return JsonConvert.SerializeObject(data, prettyPrint ? Formatting.Indented : Formatting.None);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{Tag} ToJsonString<{typeof(T).Name}> failed: {ex.Message}");
                return null;
            }
        }

        // ------------------------------------------------------------------
        // Binary I/O
        // ------------------------------------------------------------------

        public static byte[] ReadBytes(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
            try { return File.ReadAllBytes(path); }
            catch (Exception ex)
            {
                Debug.LogWarning($"{Tag} ReadBytes failed for '{path}': {ex.Message}");
                return null;
            }
        }

        public static bool WriteBytes(string path, byte[] data, bool createDirIfMissing = true)
        {
            if (string.IsNullOrEmpty(path)) return false;
            try
            {
                if (createDirIfMissing) EnsureDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, data ?? Array.Empty<byte>());
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{Tag} WriteBytes failed for '{path}': {ex.Message}");
                return false;
            }
        }

        // ------------------------------------------------------------------
        // Delete
        // ------------------------------------------------------------------

        public static bool Delete(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            try
            {
                if (File.Exists(path)) File.Delete(path);
                else if (Directory.Exists(path)) Directory.Delete(path, true);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{Tag} Delete failed for '{path}': {ex.Message}");
                return false;
            }
        }

        public static bool DeleteIfExists(string path) => Delete(path);

        // ------------------------------------------------------------------
        // Internals
        // ------------------------------------------------------------------

        private static bool SafeExists(string path, bool asDir)
        {
            if (string.IsNullOrEmpty(path)) return false;
            try
            {
                return asDir ? Directory.Exists(path) : File.Exists(path);
            }
            catch { return false; }
        }
    }
}

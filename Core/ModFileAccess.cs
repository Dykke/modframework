// ModFileAccess.cs
// ModFramework v6.1
//
// Safe file I/O for mods.
//
// v6.0: New v6.0 API takes ModIdentity + SafePath + per-op permission check.
//
// v6.1: Removed the 18 v5.x [Obsolete] wrappers
// (GetModDataPath / GetModFolder / GetManagedFolder / ReadText / WriteText /
// AppendText / WriteJson / ReadJson / TryReadJson / ToJsonString / ReadBytes /
// WriteBytes / Delete / DeleteIfExists / Exists / DirectoryExists /
// EnsureDirectory / GetGameRoot). These intentionally skipped the permission
// check for back-compat with v5.x Workshop CS mods, but a malicious Nexus DLL
// could exploit them to write to arbitrary paths (e.g. `C:\Windows\...`) with
// no declared FileWrite permission. The v6.1 removal closes this AV-1
// (arbitrary file overwrite) bypass — the only way to do file I/O in v6.1+ is
// via the v6.0 API (ModIdentity + SafePath + Permission.FileWrite).
//
// USAGE (v6.0+):
//   var identity = ModFrameworkActivator.OnActivate(this);   // in your mod's OnActivate
//   SafePath path = SafePath.GetModDataPathSafe(identity, "session.json");
//   ModFileAccess.WriteJson(identity, path, myData);
//   var data = ModFileAccess.TryReadJson<MyData>(identity, path, out var ok);

using System;
using System.IO;
using System.Text;
using UnityEngine;

// v6.1.1: JSON serialization uses Unity's engine-native JsonUtility instead of
// Newtonsoft.Json. The ILMerged Newtonsoft 13.0.3 fails its JsonWriter static
// initializer under Unity 2018.4's Mono runtime ("type initializer for
// 'Newtonsoft.Json.JsonWriter' threw an exception"), so WriteJson always threw
// and settings never persisted. JsonUtility is guaranteed to work in-engine and
// removes the fragile ~700KB ILMerge dependency. NOTE for consumers: JsonUtility
// only serializes public fields (or [SerializeField] private fields) on
// [Serializable] types; it does NOT support Dictionary, properties, or
// top-level arrays. Mod settings should be plain [Serializable] POCOs with
// public fields.

namespace ModFramework.Core
{
    public static class ModFileAccess
    {
        private const string Tag = "[ModFileAccess]";

        // ==================================================================
        // v6.0 API — takes ModIdentity + SafePath + permission check + audit
        // ==================================================================

        // ---- Read ----

        /// <summary>Read text from a SafePath. Requires Permission.FileRead.</summary>
        [ModFrameworkPublicAPI("v6.0")]
        public static string ReadText(ModIdentity id, SafePath path)
        {
            if (path == null) return null;
            SecurityGuards.RequirePermission(id, Permission.FileRead);
            try
            {
                if (!File.Exists(path.ResolvedAbsolute))
                {
                    AuditLog.Log(id == null ? null : id.ModId, id == null ? null : id.DisplayName,
                        "FILE_READ", path.ResolvedAbsolute, "MISSING", "");
                    return null;
                }
                var content = File.ReadAllText(path.ResolvedAbsolute);
                AuditLog.Log(id == null ? null : id.ModId, id == null ? null : id.DisplayName,
                    "FILE_READ", path.ResolvedAbsolute, "OK", content.Length + " bytes");
                return content;
            }
            catch (Exception ex)
            {
                AuditLog.Log(id == null ? null : id.ModId, id == null ? null : id.DisplayName,
                    "FILE_READ", path.ResolvedAbsolute, "ERROR", ex.GetType().Name);
                Debug.LogWarning(string.Format("{0} ReadText failed for '{1}': {2}", Tag, path.ResolvedAbsolute, ex.Message));
                return null;
            }
        }

        /// <summary>Read bytes from a SafePath. Requires Permission.FileRead.</summary>
        [ModFrameworkPublicAPI("v6.0")]
        public static byte[] ReadBytes(ModIdentity id, SafePath path)
        {
            if (path == null) return null;
            SecurityGuards.RequirePermission(id, Permission.FileRead);
            try
            {
                if (!File.Exists(path.ResolvedAbsolute)) return null;
                var bytes = File.ReadAllBytes(path.ResolvedAbsolute);
                AuditLog.Log(id == null ? null : id.ModId, id == null ? null : id.DisplayName,
                    "FILE_READ", path.ResolvedAbsolute, "OK", bytes.Length + " bytes");
                return bytes;
            }
            catch (Exception ex)
            {
                Debug.LogWarning(string.Format("{0} ReadBytes failed for '{1}': {2}", Tag, path.ResolvedAbsolute, ex.Message));
                return null;
            }
        }

        /// <summary>Read JSON from a SafePath. Requires Permission.FileRead.</summary>
        [ModFrameworkPublicAPI("v6.0")]
        public static T ReadJson<T>(ModIdentity id, SafePath path) where T : class
        {
            string content = ReadText(id, path);
            if (string.IsNullOrEmpty(content)) return null;
            try { return JsonUtility.FromJson<T>(content); }
            catch (Exception ex)
            {
                Debug.LogWarning(string.Format("{0} ReadJson<{1}> deserialize failed for '{2}': {3}",
                    Tag, typeof(T).Name, path == null ? "<null>" : path.ResolvedAbsolute, ex.Message));
                return null;
            }
        }

        [ModFrameworkPublicAPI("v6.0")]
        public static bool TryReadJson<T>(ModIdentity id, SafePath path, out T result) where T : class
        {
            result = ReadJson<T>(id, path);
            return result != null;
        }

        // ---- Write ----

        /// <summary>Write text to a SafePath. Requires Permission.FileWrite (or FileAppend for append).</summary>
        [ModFrameworkPublicAPI("v6.0")]
        public static bool WriteText(ModIdentity id, SafePath path, string content, bool createDirIfMissing = true)
        {
            if (path == null) return false;
            SecurityGuards.RequirePermission(id, Permission.FileWrite);
            try
            {
                if (createDirIfMissing) EnsureDirectory(Path.GetDirectoryName(path.ResolvedAbsolute));
                File.WriteAllText(path.ResolvedAbsolute, content ?? string.Empty, Encoding.UTF8);
                AuditLog.Log(id == null ? null : id.ModId, id == null ? null : id.DisplayName,
                    "FILE_WRITE", path.ResolvedAbsolute, "OK", (content == null ? 0 : content.Length) + " bytes");
                return true;
            }
            catch (Exception ex)
            {
                AuditLog.Log(id == null ? null : id.ModId, id == null ? null : id.DisplayName,
                    "FILE_WRITE", path.ResolvedAbsolute, "ERROR", ex.GetType().Name);
                Debug.LogWarning(string.Format("{0} WriteText failed for '{1}': {2}", Tag, path.ResolvedAbsolute, ex.Message));
                return false;
            }
        }

        /// <summary>Append text to a SafePath. Requires Permission.FileAppend.</summary>
        [ModFrameworkPublicAPI("v6.0")]
        public static bool AppendText(ModIdentity id, SafePath path, string content, bool createDirIfMissing = true)
        {
            if (path == null) return false;
            SecurityGuards.RequirePermission(id, Permission.FileAppend);
            try
            {
                if (createDirIfMissing) EnsureDirectory(Path.GetDirectoryName(path.ResolvedAbsolute));
                File.AppendAllText(path.ResolvedAbsolute, content ?? string.Empty, Encoding.UTF8);
                AuditLog.Log(id == null ? null : id.ModId, id == null ? null : id.DisplayName,
                    "FILE_APPEND", path.ResolvedAbsolute, "OK", (content == null ? 0 : content.Length) + " bytes");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning(string.Format("{0} AppendText failed for '{1}': {2}", Tag, path.ResolvedAbsolute, ex.Message));
                return false;
            }
        }

        /// <summary>Write bytes to a SafePath. Requires Permission.FileWrite.</summary>
        [ModFrameworkPublicAPI("v6.0")]
        public static bool WriteBytes(ModIdentity id, SafePath path, byte[] data, bool createDirIfMissing = true)
        {
            if (path == null) return false;
            SecurityGuards.RequirePermission(id, Permission.FileWrite);
            try
            {
                if (createDirIfMissing) EnsureDirectory(Path.GetDirectoryName(path.ResolvedAbsolute));
                File.WriteAllBytes(path.ResolvedAbsolute, data ?? Array.Empty<byte>());
                AuditLog.Log(id == null ? null : id.ModId, id == null ? null : id.DisplayName,
                    "FILE_WRITE", path.ResolvedAbsolute, "OK", (data == null ? 0 : data.Length) + " bytes");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning(string.Format("{0} WriteBytes failed for '{1}': {2}", Tag, path.ResolvedAbsolute, ex.Message));
                return false;
            }
        }

        /// <summary>Write JSON to a SafePath. Requires Permission.FileWrite.</summary>
        [ModFrameworkPublicAPI("v6.0")]
        public static bool WriteJson<T>(ModIdentity id, SafePath path, T data, bool prettyPrint = true)
        {
            if (path == null) return false;
            SecurityGuards.RequirePermission(id, Permission.FileWrite);
            try
            {
                EnsureDirectory(Path.GetDirectoryName(path.ResolvedAbsolute));
                string json = JsonUtility.ToJson(data, prettyPrint);
                File.WriteAllText(path.ResolvedAbsolute, json, new UTF8Encoding(false));
                AuditLog.Log(id == null ? null : id.ModId, id == null ? null : id.DisplayName,
                    "FILE_WRITE", path.ResolvedAbsolute, "OK", json.Length + " bytes JSON");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning(string.Format("{0} WriteJson<{1}> failed for '{2}': {3}",
                    Tag, typeof(T).Name, path.ResolvedAbsolute, ex.Message));
                return false;
            }
        }

        /// <summary>Serialize to JSON string without touching the file system.</summary>
        [ModFrameworkPublicAPI("v6.0")]
        public static string ToJsonString<T>(ModIdentity id, T data, bool prettyPrint = true)
        {
            SecurityGuards.RequirePermission(id, Permission.FileRead); // need some permission to call; Read is the cheapest
            try { return JsonUtility.ToJson(data, prettyPrint); }
            catch (Exception ex)
            {
                Debug.LogWarning(string.Format("{0} ToJsonString<{1}> failed: {2}", Tag, typeof(T).Name, ex.Message));
                return null;
            }
        }

        // ---- Delete ----

        [ModFrameworkPublicAPI("v6.0")]
        public static bool Delete(ModIdentity id, SafePath path)
        {
            if (path == null) return false;
            SecurityGuards.RequirePermission(id, Permission.FileDelete);
            try
            {
                if (File.Exists(path.ResolvedAbsolute)) File.Delete(path.ResolvedAbsolute);
                else if (Directory.Exists(path.ResolvedAbsolute)) Directory.Delete(path.ResolvedAbsolute, true);
                AuditLog.Log(id == null ? null : id.ModId, id == null ? null : id.DisplayName,
                    "FILE_DELETE", path.ResolvedAbsolute, "OK", "");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning(string.Format("{0} Delete failed for '{1}': {2}", Tag, path.ResolvedAbsolute, ex.Message));
                return false;
            }
        }

        [ModFrameworkPublicAPI("v6.0")]
        public static bool DeleteIfExists(ModIdentity id, SafePath path) { return Delete(id, path); }

        // ---- Existence / directory ----

        [ModFrameworkPublicAPI("v6.0")]
        public static bool Exists(ModIdentity id, SafePath path)
        {
            SecurityGuards.RequirePermission(id, Permission.FileDirectoryList);
            return path != null && File.Exists(path.ResolvedAbsolute);
        }

        [ModFrameworkPublicAPI("v6.0")]
        public static bool DirectoryExists(ModIdentity id, SafePath path)
        {
            SecurityGuards.RequirePermission(id, Permission.FileDirectoryList);
            return path != null && Directory.Exists(path.ResolvedAbsolute);
        }

        [ModFrameworkPublicAPI("v6.0")]
        public static void EnsureDirectory(ModIdentity id, SafePath path)
        {
            if (path == null) return;
            SecurityGuards.RequirePermission(id, Permission.FileDirectoryList);
            EnsureDirectory(path.ResolvedAbsolute);
        }

        // ---- internals (private helpers, not part of public API) ----

        // Internal helper: create a directory if it doesn't exist. Used by
        // the v6.0 public API (WriteText/WriteBytes/AppendText/WriteJson with
        // createDirIfMissing=true). Not a public method — mods that need
        // directory creation go through EnsureDirectory(ModIdentity, SafePath).
        private static void EnsureDirectory(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[ModFramework.ModFileAccess] Could not create directory '" + path + "': " + ex.Message);
            }
        }
    }
}

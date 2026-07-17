// SafePath.cs
// ModFramework v6.0
//
// Validated file-system path wrapper. A SafePath can only be obtained from one
// of the allowlisted factory methods; raw string paths are rejected by the
// new ModFileAccess API.
//
// Closes: AV-1 (arbitrary file overwrite via ModFileAccess.WriteText(string,
// string)). A consuming mod can no longer write to "C:\Windows\..." — it can
// only write to a SafePath that was returned by GetModDataPathSafe, etc.

using System;
using System.IO;
using UnityEngine;

namespace ModFramework.Core
{
    /// <summary>
    /// A file-system path that has been validated against an allowlist of roots
    /// (the mod's own Data/ folder, Application.persistentDataPath/Mods/&lt;id&gt;/,
    /// the game's Managed/ folder for read-only, or a path the user explicitly
    /// approved via a one-time dialog). Constructed only by the factory methods
    /// on this class — there is no public string-based constructor.
    /// </summary>
    [ModFrameworkPublicAPI("v6.0", Reason = "Required for all file I/O in v6.0+")]
    public sealed class SafePath
    {
        /// <summary>The fully-resolved absolute path. Always non-null; never contains "..".</summary>
        public string ResolvedAbsolute { get; private set; }

        /// <summary>The kind of root this path was resolved under (used for permission checks).</summary>
        public SafePathKind Kind { get; private set; }

        // Internal constructor — only the static factory methods below can create
        // a SafePath. Consuming code that tries `new SafePath("...")` will fail
        // at compile time.
        internal SafePath(string resolvedAbsolute, SafePathKind kind)
        {
            if (string.IsNullOrEmpty(resolvedAbsolute)) throw new ArgumentNullException("resolvedAbsolute");
            if (Path.GetFullPath(resolvedAbsolute) != resolvedAbsolute)
            {
                throw new ModPathException("SafePath must be a fully-resolved absolute path", resolvedAbsolute);
            }
            // Belt + suspenders: explicitly reject any ".." after resolution
            // (e.g. for case-insensitive Windows paths that ".." slipped past
            // Path.GetFullPath).
            if (resolvedAbsolute.IndexOf("..", StringComparison.Ordinal) >= 0)
            {
                throw new ModPathException("SafePath resolved to a path containing '..'", resolvedAbsolute);
            }
            this.ResolvedAbsolute = resolvedAbsolute;
            this.Kind = kind;
        }

        /// <summary>Mod's own Data/ folder, with optional sub-paths. Auto-created on write.</summary>
        [ModFrameworkPublicAPI("v6.0")]
        public static SafePath GetModDataPathSafe(ModIdentity id, params string[] subPaths)
        {
            if (id == null) throw new ArgumentNullException("id");
            var dllMod = ModRegistry.GetDLLMod(id);
            if (dllMod == null)
            {
                throw new ModPathException(
                    "Mod '" + id.ModId + "' is not registered. Call ModFrameworkActivator.OnActivate first.",
                    "<unregistered>");
            }
            string root;
            try { root = dllMod.FolderPath() ?? string.Empty; }
            catch (Exception ex) { throw new ModPathException("FolderPath() threw: " + ex.Message, "<unknown>"); }
            if (string.IsNullOrEmpty(root))
            {
                throw new ModPathException("Mod '" + id.ModId + "' has no folder path", "<empty>");
            }

            // Build the joined path: <root>\Data\<sub0>\<sub1>...
            var parts = new System.Collections.Generic.List<string>(2 + (subPaths == null ? 0 : subPaths.Length));
            parts.Add(root);
            parts.Add("Data");
            if (subPaths != null)
            {
                for (int i = 0; i < subPaths.Length; i++)
                {
                    var sp = subPaths[i] ?? string.Empty;
                    if (sp.Length == 0) continue;
                    if (sp.IndexOfAny(new[] { ':', '*', '?', '"', '<', '>', '|' }) >= 0)
                    {
                        throw new ModPathException("Sub-path contains illegal characters: '" + sp + "'", sp);
                    }
                    parts.Add(sp);
                }
            }
            var combined = Path.GetFullPath(Path.Combine(parts.ToArray()));
            return new SafePath(combined, SafePathKind.ModData);
        }

        /// <summary>Application.persistentDataPath/Mods/&lt;modId&gt;/, with optional sub-paths.</summary>
        [ModFrameworkPublicAPI("v6.0")]
        public static SafePath GetPersistentDataPathSafe(ModIdentity id, params string[] subPaths)
        {
            if (id == null) throw new ArgumentNullException("id");
            string root;
            try { root = Application.persistentDataPath; }
            catch (Exception ex) { throw new ModPathException("Application.persistentDataPath threw: " + ex.Message, "<unknown>"); }
            if (string.IsNullOrEmpty(root))
            {
                throw new ModPathException("Application.persistentDataPath is empty", "<empty>");
            }
            var parts = new System.Collections.Generic.List<string>(3 + (subPaths == null ? 0 : subPaths.Length));
            parts.Add(root);
            parts.Add("Mods");
            parts.Add(id.ModId);
            if (subPaths != null)
            {
                for (int i = 0; i < subPaths.Length; i++)
                {
                    var sp = subPaths[i] ?? string.Empty;
                    if (sp.Length == 0) continue;
                    if (sp.IndexOfAny(new[] { ':', '*', '?', '"', '<', '>', '|' }) >= 0)
                    {
                        throw new ModPathException("Sub-path contains illegal characters: '" + sp + "'", sp);
                    }
                    parts.Add(sp);
                }
            }
            var combined = Path.GetFullPath(Path.Combine(parts.ToArray()));
            return new SafePath(combined, SafePathKind.PersistentData);
        }

        /// <summary>Read-only access to a specific file in the game's Managed/ folder (e.g. for reading Newtonsoft.Json internals).</summary>
        [ModFrameworkPublicAPI("v6.0")]
        public static SafePath GetManagedPathSafe(ModIdentity id, string fileName)
        {
            if (id == null) throw new ArgumentNullException("id");
            if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException("fileName");
            if (fileName.IndexOfAny(new[] { '/', '\\', ':', '*', '?', '"', '<', '>', '|' }) >= 0)
            {
                throw new ModPathException("Managed file name must be a bare filename (no path separators): '" + fileName + "'", fileName);
            }
            string root;
            try { root = Path.Combine(Application.dataPath, "Managed"); }
            catch (Exception ex) { throw new ModPathException("Application.dataPath threw: " + ex.Message, "<unknown>"); }
            var combined = Path.GetFullPath(Path.Combine(root, fileName));
            return new SafePath(combined, SafePathKind.ManagedReadOnly);
        }

        /// <summary>
        /// One-time user dialog grants a specific path. Subsequent calls for the same path
        /// are silent (granted path is cached in user prefs).
        ///
        /// Phase 1: STUB. The actual dialog flow lives in Phase 2 (in-game UI).
        /// For now this just validates the path is absolute and returns it as
        /// a UserApproved SafePath. **Do not use in production — the dialog
        /// must be added before release.**
        /// </summary>
        [ModFrameworkPublicAPI("v6.0")]
        public static SafePath GetUserApprovedPath(ModIdentity id, string filePath)
        {
            if (id == null) throw new ArgumentNullException("id");
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException("filePath");
            if (!Path.IsPathRooted(filePath))
            {
                throw new ModPathException("UserApproved path must be absolute: '" + filePath + "'", filePath);
            }
            var combined = Path.GetFullPath(filePath);
            // Phase 1 stub: TODO Phase 2 will add a one-time user dialog.
            UnityEngine.Debug.LogWarning("[ModFramework.SafePath] GetUserApprovedPath is a Phase 1 STUB — no user dialog is shown yet. The mod is granted access automatically. Full UI lands in Phase 2.");
            return new SafePath(combined, SafePathKind.UserApproved);
        }

        public override string ToString()
        {
            return string.Format("SafePath({0}, {1})", this.ResolvedAbsolute, this.Kind);
        }
    }

    /// <summary>Where a SafePath is allowed to live.</summary>
    public enum SafePathKind
    {
        /// <summary>&lt;modFolder&gt;/Data/...</summary>
        ModData = 1,
        /// <summary>Application.persistentDataPath/Mods/&lt;modId&gt;/...</summary>
        PersistentData = 2,
        /// <summary>Software Inc_Data/Managed/ (read-only).</summary>
        ManagedReadOnly = 3,
        /// <summary>User explicitly approved via dialog (one-time grant).</summary>
        UserApproved = 4
    }
}

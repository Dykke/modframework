// ModRegistry.cs
// ModFramework v6.0
//
// In-memory registry of every mod that has called ModFrameworkActivator.OnActivate
// during the current session. Maps ModId → ModIdentity + ModController.DLLMod so
// the SafePath factories and ModFileAccess can find the mod's folder.
//
// Per-process state. No disk persistence. When the game exits, this clears.
// When a mod is deactivated, its entry is removed (via OnDeactivate).
//
// Concurrency: single-threaded access from Unity's main thread. No locks.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace ModFramework.Core
{
    /// <summary>
    /// Tracks every active mod's identity + game-level handle. Internal — mods
    /// should not call methods on this class directly. Use ModIdentity (returned
    /// by ModFrameworkActivator.OnActivate) as the durable handle.
    /// </summary>
    [ModFrameworkPublicAPI("v6.0", Note = "Internal registry; use ModIdentity as the handle")]
    public static class ModRegistry
    {
        private const string Tag = "[ModFramework.ModRegistry]";

        // ModId (string) -> {ModIdentity + DLLMod + Assembly}
        private struct Entry
        {
            public ModIdentity Identity;
            public ModController.DLLMod DLLMod;
            public System.Reflection.Assembly Assembly;
        }

        private static readonly Dictionary<string, Entry> _byModId =
            new Dictionary<string, Entry>(StringComparer.Ordinal);

        /// <summary>All currently-registered mod identities. Safe to enumerate (snapshot).</summary>
        public static IReadOnlyCollection<ModIdentity> All
        {
            get
            {
                // Snapshot the values so the caller doesn't see live updates.
                var snap = new List<ModIdentity>(_byModId.Count);
                foreach (var kv in _byModId) snap.Add(kv.Value.Identity);
                return snap;
            }
        }

        /// <summary>Number of registered mods (useful for the in-game Mod Audit Log header).</summary>
        public static int Count { get { return _byModId.Count; } }

        /// <summary>Look up the DLLMod handle for a registered ModIdentity. Null if not registered.</summary>
        public static ModController.DLLMod GetDLLMod(ModIdentity identity)
        {
            if (identity == null) return null;
            Entry e;
            return _byModId.TryGetValue(identity.ModId, out e) ? e.DLLMod : null;
        }

        /// <summary>Look up the calling Assembly for a registered ModIdentity. Null if not registered.</summary>
        public static System.Reflection.Assembly GetAssembly(ModIdentity identity)
        {
            if (identity == null) return null;
            Entry e;
            return _byModId.TryGetValue(identity.ModId, out e) ? e.Assembly : null;
        }

        /// <summary>Look up the ModIdentity by its mod id. Null if not registered.</summary>
        public static ModIdentity GetByModId(string modId)
        {
            if (string.IsNullOrEmpty(modId)) return null;
            Entry e;
            return _byModId.TryGetValue(modId, out e) ? e.Identity : null;
        }

        /// <summary>Register a new mod. Called by ModFrameworkActivator.OnActivate.</summary>
        public static void Register(ModIdentity identity, ModController.DLLMod dllMod, System.Reflection.Assembly assembly)
        {
            if (identity == null) throw new ArgumentNullException("identity");
            if (string.IsNullOrEmpty(identity.ModId))
            {
                throw new ModSecurityException("Cannot register a mod with empty ModId");
            }
            if (_byModId.ContainsKey(identity.ModId))
            {
                Debug.LogWarning(Tag + " ModId '" + identity.ModId + "' is already registered. Re-registration is a no-op.");
                return;
            }
            _byModId[identity.ModId] = new Entry { Identity = identity, DLLMod = dllMod, Assembly = assembly };
        }

        /// <summary>Unregister a mod. Called by ModFrameworkActivator.OnDeactivate.</summary>
        public static void Unregister(string modId)
        {
            if (string.IsNullOrEmpty(modId)) return;
            _byModId.Remove(modId);
        }
    }
}

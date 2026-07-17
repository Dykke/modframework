// ModIdentity.cs
// ModFramework v6.0
//
// Unforgeable per-mod identity issued at OnActivate. Every privileged framework
// call (file I/O, Harmony patch, event subscription, service registration) takes
// a ModIdentity as its first parameter. The identity is minted by the framework
// (internal constructor) and cannot be constructed by consuming code.
//
// Closes: AV-1 (attribution), AV-2 (patched methods attributable),
//         AV-3 (event triggers attributable), AV-7 (settings prefix changes
//         attributable).
//
// Full implementation lands in Phase 1 (D2 = M-2). This file is the Phase 0
// skeleton — class shape, no validation logic yet.

using System;

namespace ModFramework.Core
{
    /// <summary>
    /// Unforgeable per-mod identity. Constructor is internal — only ModFramework
    /// can mint one. Every privileged framework call takes a ModIdentity as
    /// its first parameter; the identity proves which mod is making the call
    /// and is recorded in the audit log.
    /// </summary>
    [ModFrameworkPublicAPI("v6.0", Reason = "Required for every privileged call")]
    public sealed class ModIdentity
    {
        /// <summary>Nexus mod ID (from meta.tyd's ID field, or empty for non-Nexus mods).</summary>
        public string ModId { get; private set; }

        /// <summary>Human-readable name (from meta.tyd's Name field).</summary>
        public string DisplayName { get; private set; }

        /// <summary>SHA-256 of the calling mod's .dll. Recomputed on every activation.</summary>
        public string AssemblyHash { get; private set; }

        /// <summary>Per-session GUID; rotates on every game launch.</summary>
        public Guid SessionNonce { get; private set; }

        /// <summary>When this identity was minted.</summary>
        public DateTime IssuedAt { get; private set; }

        /// <summary>The bitfield of permissions this mod declared in meta.tyd.</summary>
        public Permission Permissions { get; internal set; }

        // Internal-only constructor — only ModFramework can mint an identity.
        // Consuming mods that try to call this from their own code will get a
        // compile error ("inaccessible due to its protection level").
        internal ModIdentity(string modId, string displayName, string assemblyHash, Permission permissions)
        {
            this.ModId = modId ?? string.Empty;
            this.DisplayName = displayName ?? string.Empty;
            this.AssemblyHash = assemblyHash ?? string.Empty;
            this.SessionNonce = Guid.NewGuid();
            this.IssuedAt = DateTime.UtcNow;
            this.Permissions = permissions;
        }

        public override string ToString()
        {
            return string.Format("ModIdentity({0}, {1}, {2})", this.ModId, this.DisplayName, this.SessionNonce);
        }
    }
}

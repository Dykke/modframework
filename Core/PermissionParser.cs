// PermissionParser.cs
// ModFramework v6.0
//
// Parses the "Permissions:" line of a mod's meta.tyd. Accepts either a preset
// name (e.g. "Patcher") or a comma-separated list of individual flags
// (e.g. "FileRead, FileWrite, HarmonyPatch"). Unknown names throw
// ModSecurityException at OnActivate (the mod does not load).
//
// Full implementation lands in Phase 2 (D6 = M-5). This file is the Phase 0
// skeleton — the parsing loop + the unknown-flag detector.

using System;
using System.Collections.Generic;

namespace ModFramework.Core
{
    /// <summary>
    /// Parses the "Permissions:" field of a mod's meta.tyd into a Permission
    /// bitfield. Called by ModFramework.OnActivate.
    /// </summary>
    [ModFrameworkPublicAPI("v6.0", Reason = "Required for meta.tyd parsing")]
    public static class PermissionParser
    {
        /// <summary>
        /// Parse a comma-separated permissions string. Returns Permission.None
        /// for an empty or missing string (the mod loads but every privileged
        /// call will throw at runtime).
        /// </summary>
        /// <exception cref="ModSecurityException">Thrown if any individual flag
        /// name is not a defined member of the Permission enum.</exception>
        public static Permission Parse(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return Permission.None;

            // Split on commas, trim, drop empties.
            var parts = raw.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var flags = new List<Permission>(parts.Length);
            for (int i = 0; i < parts.Length; i++)
            {
                var token = parts[i].Trim();
                if (token.Length == 0) continue;

                // If it's a preset, expand it.
                var expanded = PermissionPresets.Expand(token);
                if (expanded != null)
                {
                    // Recurse with the expanded list.
                    var presetFlags = Parse(expanded);
                    flags.Add(presetFlags);
                    continue;
                }

                // Otherwise, look it up as an individual flag.
                Permission parsed;
                if (Enum.TryParse<Permission>(token, out parsed) && parsed != Permission.None)
                {
                    flags.Add(parsed);
                }
                else
                {
                    throw new ModSecurityException(
                        "Unknown permission flag in meta.tyd: '" + token +
                        "'. Add it to the Permission enum in modframework/Core/Permission.cs first.");
                }
            }

            // OR all the flags together.
            Permission result = Permission.None;
            for (int i = 0; i < flags.Count; i++) result |= flags[i];
            return result;
        }
    }
}

// ModFrameworkPublicAPI.cs
// ModFramework v6.0
//
// Marker attribute for every public method/type that is part of ModFramework's
// curated external API. The post-build grep (in Build-ModFramework.ps1) checks
// that no public method exists in the framework's source without this attribute
// — the goal is to keep the attack surface auditable and to prevent accidental
// API exposure during future refactors.
//
// USAGE:
//   [ModFrameworkPublicAPI("v6.0")]
//   public static class ModIdentity { ... }
//
//   [ModFrameworkPublicAPI("v6.0", Reason = "Trusted-issuer key material")]
//   internal static class KeyMaterial { ... }
//
//   [ModFrameworkPublicAPI("v6.0", Note = "Removes in v6.1 — use EventKey instead")]
//   public static void Trigger(string eventName, object data) { ... }

using System;

namespace ModFramework.Core
{
    /// <summary>
    /// Marks a public type or method as part of ModFramework's curated external
    /// API. Required on every `public` member that should be visible to consuming
    /// mods. Missing this attribute on a `public` member is a build error
    /// (enforced by Build-ModFramework.ps1's post-build grep).
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum |
        AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field |
        AttributeTargets.Interface | AttributeTargets.Delegate,
        AllowMultiple = false, Inherited = false)]
    public sealed class ModFrameworkPublicAPIAttribute : Attribute
    {
        /// <summary>API version that introduced this member (e.g. "v6.0", "v6.1").</summary>
        public string Since { get; private set; }

        /// <summary>Why this member is public. Defaults to empty; recommended for non-obvious exposure.</summary>
        public string Reason { get; set; }

        /// <summary>Free-form note (e.g. "deprecation in v6.1", "replaces X").</summary>
        public string Note { get; set; }

        public ModFrameworkPublicAPIAttribute(string since)
        {
            if (string.IsNullOrEmpty(since)) throw new ArgumentNullException("since");
            this.Since = since;
        }
    }
}

// ModPermissionException.cs
// ModFramework v6.0
//
// Thrown when a privileged call is made by a mod that did not declare the
// required Permission in its meta.tyd. Catches ModSecurityException.

using System;

namespace ModFramework.Core
{
    /// <summary>
    /// Thrown when a mod calls a privileged framework method without the
    /// required permission flag in its meta.tyd. Always audit-logged with
    /// the modId, required flag, and the granted mask.
    /// </summary>
    [ModFrameworkPublicAPI("v6.0", Reason = "Security violation type")]
    public sealed class ModPermissionException : ModSecurityException
    {
        /// <summary>The mod that attempted the call (null if no identity was supplied).</summary>
        public string ModId { get; private set; }

        /// <summary>The permission the call required.</summary>
        public Permission Required { get; private set; }

        /// <summary>The permission bitfield the mod had granted.</summary>
        public Permission Granted { get; private set; }

        public ModPermissionException(string modId, Permission required, Permission granted)
            : base(BuildMessage(modId, required, granted))
        {
            this.ModId = modId ?? string.Empty;
            this.Required = required;
            this.Granted = granted;
        }

        private static string BuildMessage(string modId, Permission required, Permission granted)
        {
            return string.Format(
                "Mod '{0}' attempted a privileged call requiring {1} but was only granted {2}. " +
                "Add {1} to the mod's meta.tyd Permissions field (or remove the call).",
                modId ?? "<unknown>", required, granted);
        }
    }
}

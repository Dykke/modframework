// SecurityGuards.cs
// ModFramework v6.0
//
// Internal helper methods for permission checks. Every privileged framework
// method calls RequirePermission(...) at the top of its body, before doing
// the actual work. If the caller's ModIdentity doesn't have the required
// permission, throws ModPermissionException and writes an audit log line.

using System;

namespace ModFramework.Core
{
    /// <summary>
    /// Internal permission-check helpers. Not for direct use by mods.
    /// </summary>
    [ModFrameworkPublicAPI("v6.0", Note = "Internal guard; mods should not call directly")]
    public static class SecurityGuards
    {
        /// <summary>
        /// Throws ModPermissionException if the mod's identity does not have
        /// the required permission. Also calls FrameworkSignatureCheck.RequireValid.
        /// </summary>
        public static void RequirePermission(ModIdentity identity, Permission required)
        {
            FrameworkSignatureCheck.RequireValid();
            if (identity == null)
            {
                throw new ModPermissionException("<no identity>", required, Permission.None);
            }
            if ((identity.Permissions & required) != required)
            {
                AuditLog.Log(identity.ModId, identity.DisplayName, "PERMISSION_DENIED",
                    required.ToString(), "DENIED",
                    "granted=" + identity.Permissions);
                throw new ModPermissionException(identity.ModId, required, identity.Permissions);
            }
        }
    }
}

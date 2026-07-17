// ModSecurityException.cs
// ModFramework v6.0
//
// Base class for all v6.0 security exceptions (ModPathException,
// ModPermissionException, future: ModSignatureException, etc.). Consuming
// code can catch this to handle any security violation uniformly.
//
// Note: in v4.x the framework did not throw on security violations — it
// just did whatever the caller asked. v6.0 introduces "fail closed" —
// any untrusted call throws.

using System;

namespace ModFramework.Core
{
    /// <summary>
    /// Base class for every v6.0 security violation. Always audit-logged
    /// with the modId (if known) and the operation. Mods that want to
    /// gracefully handle "I asked for permission and was denied" can
    /// catch this type; mods that want to fail-fast can let it bubble.
    /// </summary>
    [ModFrameworkPublicAPI("v6.0", Reason = "Security violation base type")]
    public class ModSecurityException : Exception
    {
        public ModSecurityException(string message) : base(message) { }
        public ModSecurityException(string message, Exception inner) : base(message, inner) { }
    }
}

// ModPathException.cs
// ModFramework v6.0
//
// Thrown by SafePath factory methods when a path fails validation:
//   - contains ".." traversal
//   - resolves outside the allowlisted root
//   - is not a fully-resolved absolute path
//   - is a write attempt on a read-only path kind (e.g. ManagedReadOnly)
//
// Catches ModSecurityException (so callers can do `catch (ModSecurityException)`
// for any v6.0 security violation).

using System;

namespace ModFramework.Core
{
    /// <summary>
    /// Thrown when a SafePath factory method rejects a path. Indicates either
    /// a malicious attempt (path traversal) or a programming error (joined
    /// subPaths produced a path outside the allowlisted root).
    /// </summary>
    [ModFrameworkPublicAPI("v6.0", Reason = "Security violation type")]
    public sealed class ModPathException : ModSecurityException
    {
        /// <summary>The rejected path (for diagnostic logging). May be the original input, not the resolved absolute.</summary>
        public string RejectedPath { get; private set; }

        public ModPathException(string message, string rejectedPath)
            : base(message)
        {
            this.RejectedPath = rejectedPath ?? string.Empty;
        }

        public ModPathException(string message, string rejectedPath, Exception inner)
            : base(message, inner)
        {
            this.RejectedPath = rejectedPath ?? string.Empty;
        }
    }
}

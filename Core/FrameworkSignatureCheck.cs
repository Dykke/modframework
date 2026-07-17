// FrameworkSignatureCheck.cs
// ModFramework v6.0
//
// D3 (M-7): Static-ctor signature check. Runs once per session, on first
// access to any ModFramework.Core type. Compares the actually-loaded
// ModFramework.dll's public key token against the expected token
// (e0967644e3ffec06) and throws ModSecurityException on mismatch.
//
// Purpose: prevent a malicious actor from replacing the genuine
// ModFramework.dll in the game's Managed\ folder with a tampered build
// that pretends to be v6.0 but is unsigned or signed with a different
// keypair. The token check is the last line of defense — even if a mod
// manages to drop a file into Managed\, the check refuses to load.
//
// Trade-off: a missing ModFramework.dll or an accidentally-regenerated
// keypair will refuse to load. This is intentional — fail closed.

using System;
using System.Reflection;
using System.Security;
using UnityEngine;

namespace ModFramework.Core
{
    /// <summary>
    /// Strong-name signature verification. Runs once per session. The expected
    /// public key token is the SHA-1 hash of the committed ModFramework.pub
    /// file (see modframework/keys/README.md). If the actually-loaded assembly
    /// does not match, all subsequent ModFramework.Core calls will throw
    /// ModSecurityException.
    ///
    /// Note: this is a "soft check" — it does not block the assembly from
    /// being loaded by the runtime. It blocks ModFramework.Core.* from being
    /// usable, which is what every Nexus DLL mod depends on. A mod that
    /// avoids the v6.0 API entirely is unaffected.
    /// </summary>
    [ModFrameworkPublicAPI("v6.0", Note = "Internal verification class; not meant to be called by mods")]
    public static class FrameworkSignatureCheck
    {
        // The expected public key token. MUST match modframework/keys/ModFramework.pub.
        // Build-ModFramework.ps1 verifies this at build time. To regenerate, follow
        // the procedure in modframework/keys/README.md and bump the major version.
        public const string ExpectedPublicKeyToken = "e0967644e3ffec06";

        // The state — set once on first access. Once set to false, every
        // privileged call throws.
        private static bool _passed = false;
        private static bool _hasRun = false;
        private static string _actualToken = null;
        private static string _failureReason = null;

        /// <summary>True if the signature check passed (or was skipped because the dev build was unsigned).</summary>
        public static bool Passed { get { EnsureChecked(); return _passed; } }

        /// <summary>The actual public key token of the running ModFramework.dll. Null if not strong-name signed.</summary>
        public static string ActualToken { get { EnsureChecked(); return _actualToken; } }

        /// <summary>Human-readable failure reason. Null if the check passed.</summary>
        public static string FailureReason { get { EnsureChecked(); return _failureReason; } }

        /// <summary>
        /// Throws ModSecurityException if the signature check failed. Called
        /// by every privileged framework method. Cheap when the check has
        /// already passed (one boolean comparison).
        /// </summary>
        public static void RequireValid()
        {
            EnsureChecked();
            if (!_passed)
            {
                throw new ModSecurityException(
                    "ModFramework signature check failed: " + (_failureReason ?? "unknown") +
                    ". The installed ModFramework.dll is not a genuine v6.0 build. " +
                    "Re-install the framework from the official Nexus page.");
            }
        }

        // Static initializer. Runs once per session on first reference to
        // any member of this class. We do not use a true C# static ctor
        // because we want to DEFER the check until first use (so loading
        // the assembly itself never fails — only using it does).
        private static void EnsureChecked()
        {
            if (_hasRun) return;
            _hasRun = true;

            try
            {
                var asm = typeof(FrameworkSignatureCheck).Assembly;
                var name = asm.GetName();

                // The public key token is part of the assembly's name. Empty
                // for unsigned assemblies, an 8-byte hex string for signed ones.
                byte[] tokenBytes = name.GetPublicKeyToken();
                if (tokenBytes == null || tokenBytes.Length == 0)
                {
                    _passed = false;
                    _actualToken = null;
                    _failureReason = "ModFramework.dll is not strong-name signed (token is null/empty)";
                    Debug.LogError("[ModFramework.SigCheck] " + _failureReason);
                    return;
                }

                // Convert to lowercase hex (8 bytes -> 16 hex chars).
                _actualToken = BitConverter.ToString(tokenBytes).Replace("-", "").ToLowerInvariant();

                if (string.Equals(_actualToken, ExpectedPublicKeyToken, StringComparison.OrdinalIgnoreCase))
                {
                    _passed = true;
                    Debug.Log("[ModFramework.SigCheck] OK — token " + _actualToken);
                }
                else
                {
                    _passed = false;
                    _failureReason = "token mismatch (expected " + ExpectedPublicKeyToken + ", got " + _actualToken + ")";
                    Debug.LogError("[ModFramework.SigCheck] FAIL — " + _failureReason +
                        ". The installed ModFramework.dll was signed with a different keypair. " +
                        "Re-install the genuine v6.0 build from Nexus.");
                }
            }
            catch (Exception ex)
            {
                _passed = false;
                _failureReason = "exception during check: " + ex.GetType().Name + ": " + ex.Message;
                Debug.LogError("[ModFramework.SigCheck] " + _failureReason);
            }
        }
    }
}

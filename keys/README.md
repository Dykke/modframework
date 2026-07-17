# ModFramework Signing Keys

**This folder contains the public key used to verify the `ModFramework.dll` assembly.**

The private key is in `_secure/ModFramework.snk` (gitignored) at the repository root.

---

## Current public key token

**`e0967644e3ffec06`**

This is the 8-byte SHA-1 hash of the public key blob, reverse-byte-ordered, and rendered as 16 hex characters. It is the unique fingerprint of the `ModFramework.dll` strong-name signature.

Any consumer (mod, .cmd wrapper, build verification script) that wants to confirm a deployed `ModFramework.dll` is genuine must:
1. Extract the public key token from the deployed DLL using `sn.exe -T <path-to-ModFramework.dll>`
2. Compare it to `e0967644e3ffec06`
3. Treat any mismatch as a tampered or substituted build

---

## How the keypair was generated

```powershell
# Private keypair (kept out of git in _secure/)
sn.exe -k _secure\ModFramework.snk

# Public key only (committed)
sn.exe -p _secure\ModFramework.snk modframework\keys\ModFramework.pub

# Display the token (from the .pub file)
sn.exe -t modframework\keys\ModFramework.pub
```

Both commands require `sn.exe` (part of the .NET Framework SDK or Windows SDK). On a typical VS 2022 install it lives at:
`C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\sn.exe`

> **OneDrive note:** `sn.exe -k` fails with `Failed to generate a strong name key pair -- Keyset does not exist` if the target folder is on OneDrive and the write is being synchronised. Workaround: generate in `%TEMP%` then `Copy-Item` to the target. (Confirmed 2026-07-16.)

---

## When to regenerate

- **Never on a whim.** Regenerating produces a new token, which means every consumer that verifies the token will treat the new DLL as untrusted.
- **Only if the private key is compromised** (e.g. accidentally committed, exfiltrated, lost).
- **Never delete `_secure/ModFramework.snk`.** A deleted keypair is unrecoverable, and any future build that tries to re-sign with it will fail. Treat it as you would an SSL private key.

## If you must regenerate

1. Delete `modframework/keys/ModFramework.pub`
2. Delete `_secure/ModFramework.snk`
3. Run the two `sn.exe` commands above
4. Update the token in this README
5. Update the `ExpectedPublicKeyToken` constant in `modframework/Core/ModFramework.cs` (added in v6.0)
6. **Bump the major version** of `ModFramework.dll` (v6.0 → v6.1 or v7.0) — the new token is a breaking change for every consumer
7. Re-publish on Nexus with a clear "security incident / new signature" note

## Why this split?

- `_secure/ModFramework.snk` — private, NEVER committed. Used by `Build-ModFramework.ps1` to sign the DLL.
- `keys/ModFramework.pub` — public, committed. Used by `ModFramework.dll` itself (static-ctor signature check) and by external verifiers.

This is the standard pattern for any project that wants reproducible builds without leaking the private key. The committed public key is the source of truth for "what should the signature be"; the private key in `_secure/` is the developer-local proof of "I am authorised to publish a new build."

---

**Last regenerated:** 2026-07-16 (initial generation for v6.0 release)

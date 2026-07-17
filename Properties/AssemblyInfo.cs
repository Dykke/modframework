// AssemblyInfo.cs
// ModFramework v6.1
//
// Strong-name signing + version info. The .snk file is at ../../_secure/ModFramework.snk
// (repo root, outside the published folder, gitignored). The build will fail
// with a clear error if the .snk is missing or if the keys/ModFramework.pub
// was extracted from a different keypair (cross-check done in Build-ModFramework.ps1).
//
// v6.0.1 (2026-07-16):
//   - Added OnActivate(ModBehaviour) convenience overload (looks up the matching
//     ModController.DLLMod via ModController.Instance.Mods by assembly filename).
//   - Added ModDependencies.VerifyOrWarn(ModIdentity, ...) convenience overload
//     (looks up the registered DLLMod via ModRegistry.GetDLLMod(identity)).
//   - Both additions unblock the ModFrameworkExample canonical reference mod from
//     compiling. See cursor-stuff/sessions/2026-07-16.md (Phase 3 Polish: in-game
//     test step) for the full change set.
//
// v6.1 (2026-07-16):
//   - Turned the 6 v4.x single-file classes (UIHelper / ModLogger / ModEvents /
//     Notifications / ModSettings / ModUtils in ModFramework.cs) from `public`
//     to `internal`. Mods that target the v4.x class names now get a CS0122
//     (inaccessible) compile error instead of a CS0618 (obsolete) warning.
//   - Removed all 28 v5.x [Obsolete] wrappers from the v6.0 Core/ files
//     (ModFileAccess 18 + ModHarmony 4 + ModEvents 1 + ModServiceBridge 5).
//     These intentionally skipped the permission check (SecurityGuards.
//     RequirePermission) for back-compat with v5.x Workshop CS mods. The
//     v6.1 removal closes the permission bypass — the only way to do file
//     I/O / events / services through the framework in v6.1+ is via the
//     v6.0 API (which requires ModIdentity + RequirePermission).
//   - Workshop CS mods (BasementFloor / BuildAnarchy / BlueprintPlus /
//     CloudServices / FurniturePlacementTester / AutoCourier partial) pin
//     to ModFramework v6.0.0 (the last version with the [Obsolete]
//     wrappers). The MIGRATION_v5_to_v6.md v6.1 section explains the pin.
//   - Closes the last back-compat surface for the v6.0 Nexus DLL mod
//     audience. See cursor-stuff/plans/modframework-v6.1-backlog.md for
//     the full plan + the Workshop pin decision rationale.
//
// v6.1.1 (2026-07-17):
//   - Settings persistence fix: ModFileAccess now serializes via Unity's
//     JsonUtility instead of Newtonsoft.Json. Newtonsoft 13.0.3's JsonWriter
//     static ctor throws a type-initializer exception under the game's old
//     Unity 2018.4 Mono runtime, so WriteJson always failed and settings
//     never persisted. Newtonsoft dropped + no longer ILMerged (~700KB
//     smaller). Serialized types now need [Serializable] + public fields.
//   - Settings window: real tab switching (Mods / Audit Log toggle separate
//     containers instead of both drawing into one panel), brighter/larger
//     mod-list rows. FileDelete permission added to the example's meta.tyd
//     (Patcher preset lacks it) so "Clear Settings" no longer errors.

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General assembly info
[assembly: AssemblyTitle("ModFramework")]
[assembly: AssemblyDescription("Reusable utilities for Software Inc modding — security-hardened v6.1")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Zicarius")]
[assembly: AssemblyProduct("ModFramework")]
[assembly: AssemblyCopyright("Copyright © 2026 Zicarius")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// COM visibility — not a COM library
[assembly: ComVisible(false)]

// Version: 6.1.1.0
// FileVersion is the human-readable build identifier; AssemblyVersion is the
// strong-name bound version. AssemblyVersion stays at 6.1.0.0 so consuming
// mods pinned to that strong-name identity keep binding; only FileVersion
// bumps for this bugfix release.
[assembly: AssemblyVersion("6.1.0.0")]
[assembly: AssemblyFileVersion("6.1.1.0")]

// InternalsVisibleTo for test access will be added in a future phase when the
// ModFramework.Tests project is scaffolded. For now, only the
// [ModFrameworkPublicAPI] surface is visible to consumers (LimitlessTeams,
// MoreCompanies, AutoCourier, etc.).

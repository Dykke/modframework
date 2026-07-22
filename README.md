# ModFramework for Software Inc

![Software Inc](https://img.shields.io/badge/Game-Software_Inc-blue?style=for-the-badge&logo=steam) ![Unity 2018.4](https://img.shields.io/badge/Unity-2018.4-black?style=for-the-badge&logo=unity) ![License](https://img.shields.io/badge/License-MIT-success?style=for-the-badge) ![v6.1](https://img.shields.io/badge/Version-6.1-orange?style=for-the-badge)

A security-focused UI, utility, and gameplay framework for building Software Inc Nexus DLL mods.

---

## What's new in v6.1 (2026-07-16)

**v6.1 closes the v5.x [Obsolete] back-compat permission bypass.** In v6.0/v6.0.1 the v4.x single-file classes (`UIHelper`, `ModLogger`, `ModEvents`, `Notifications`, `ModSettings`, `ModUtils`) were kept as `[Obsolete]` `public` so Workshop CS mods would keep building. The `[Obsolete]` wrappers in `Core/` (28 of them) intentionally skipped the `SecurityGuards.RequirePermission` check. A malicious Nexus DLL could exploit the bypass to do file I/O / events / services with no declared permission.

**v6.1 closes the bypass:**

- The 6 v4.x single-file classes are now `internal` (not `public`).
- All 28 `[Obsolete]` v5.x wrappers in `Core/` (`ModFileAccess` 18 + `ModHarmony` 4 + `ModEvents` 1 + `ModServiceBridge` 5) are **deleted**.
- The only way to do file I/O / events / services through the framework in v6.1+ is via the v6.0 API (`ModIdentity` + `RequirePermission`).
- Verified by `modframework/tests/ModFrameworkV61BypassTest.cs` — produces 20 expected compile errors (6 CS0122 for the internal classes + 14 CS1503/CS7036 for the removed wrappers).

**Workshop CS mod impact:** the 6 Workshop CS mods (`BasementFloor` / `BuildAnarchy` / `BlueprintPlus` / `CloudServices` / `FurniturePlacementTester` / `AutoCourier` partial) **pin to ModFramework v6.0.0** (the last version with the `[Obsolete]` wrappers). They are explicitly out of v6.x's audience per the v6.0 plan. See [MIGRATION_v5_to_v6.md](MIGRATION_v5_to_v6.md) for the pin instructions.

**v6.1 also adds the in-game "Mod Audit Log" + "Mod permissions" windows.** A tiny shim mod (`ModFrameworkSettings/`) hosts the `ModMeta` entry; the framework does all UI work. See "In-game Audit Log Window" below.

### v6.1 changelog

- v4.x single-file classes (`UIHelper`, `ModLogger`, `ModEvents`, `Notifications`, `ModSettings`, `ModUtils`) changed from `public` to `internal`.
- 28 `[Obsolete]` v5.x wrappers deleted from `Core/`. The bypass is closed.
- `AssemblyVersion` bumped 6.0.1.0 → 6.1.0.0 (strong-name token unchanged: `e0967644e3ffec06`).
- New file: `modframework/UI/ModFrameworkSettingsWindow.cs` — in-game windows.
- New file: `modframework/tests/ModFrameworkV61BypassTest.cs` + `.csproj` — bypass verification.
- New shim mod: `ModFrameworkSettings/` — hosts the Mods tab entry for the in-game windows.

### In-game Audit Log Window

After installing v6.1 + the `ModFrameworkSettings` shim mod, the Mods tab shows a "ModFramework Settings" entry. Click it to open a 2-tab window:

- **Mods tab** — list of every registered Nexus DLL mod (from `ModRegistry.All`). For each: modId, declared permissions, audit log line count.
- **Audit Log tab** — scrollable view of the last 1000 audit log lines (from today's file). Filterable by modId. Date picker for historical logs. "Open folder" button opens Explorer at `%persistentDataPath%/ModFramework/`.

---

## What's new in v6.1.1 (2026-07-17)

**Settings persistence + Settings-window UI fixes.**

- **Settings now persist across restarts.** The ILMerged Newtonsoft.Json 13.0.3 `JsonWriter` static constructor throws under the game's Unity 2018.4 Mono runtime (Newtonsoft 13.x dropped old-runtime support), so every `WriteJson` failed silently. `ModFileAccess` now uses Unity's engine-native `JsonUtility.ToJson`/`FromJson<T>` instead. Newtonsoft was **removed** from the framework and both ILMerge paths — the DLL is now **~700 KB smaller**. Settings classes must be `[Serializable]` with public fields (no `Dictionary`, properties, or top-level arrays).
- Real `Mods` / `Audit Log` tab buttons; dark high-contrast text on the light `WindowManager` panel; the audit-log filter box renders typed text (dark `textComponent.color`).

## What's new in v6.1.2 (2026-07-17)

**In-game Settings window no longer clips content.** The game (`OptionsWindow.AddModOption`) sizes each mod's Options>Mods scroll region **once**, from the parent's *direct children's* bounds. Fixed-size containers capped the height, `SetActive`-hidden tabs were excluded from the measurement, and overflowing `Text` glyphs weren't measured — so long permission lists and long audit logs were cut off. Fixed by: zero-size tab containers, keeping both tabs active and toggling visibility via `CanvasGroup.alpha` (so both are measured), and self-sizing every label to its `preferredHeight`. The game's native scroll now covers the full content. **Verified in-game 2026-07-17.**

---

## What's new in v6.0 (2026-07-16)

**v6.0 is a security-focused overhaul.** Every privileged call now requires a `ModIdentity` (issued by the framework at `OnActivate`), the matching `Permission` flag (declared in `meta.tyd`), and — for file I/O — a `SafePath` (obtained from allowlisted factories, not raw `string`).

### The 5 big changes

| | v5.x | v6.0 |
|---|---|---|
| File I/O | `ModFileAccess.WriteText(string, string)` | `ModFileAccess.WriteText(ModIdentity, SafePath, string)` |
| Events | `ModEvents.Trigger(string, object)` (any mod can trigger any event) | `ModEvents.Trigger(ModIdentity, EventKey, object)` (only the publisher can trigger) |
| Services | `ModServiceBridge.Find(string)` (hijackable via `GameObject.Find`) | `ModServiceBridge.Find(ModIdentity, ServiceToken)` (unforgeable token + collision check) |
| Harmony | `new Harmony("...").PatchAll(...)` | `ModHarmony.CreateInstance(identity, "...").PatchAll(...)` |
| Permissions | (none — any mod can do anything) | 22 fine-grained flags + 4 presets, declared in `meta.tyd` |

### Other v6.0 changes

- **Strong-name signing** — `ModFramework.dll` is signed with token `e0967644e3ffec06`. A static-ctor check refuses to load if the signature mismatches (prevents malicious replacement).
- **Audit log** — every privileged call is logged to `%persistentDataPath%/ModFramework/audit-YYYY-MM-DD.log` with the mod's `ModId`, `DisplayName`, operation, target, and result.
- **C# 5 compatibility** — the framework is C# 5 compatible so older Workshop mod compilers can consume it.
- **In-game "Mod Audit Log"** window (added in v6.1) — see "In-game Audit Log Window" below.
- **v4.x [Obsolete] back-compat (v6.0 → v6.1)** — all 6 v4.x legacy classes (UIHelper, ModLogger, ModEvents, Notifications, ModSettings, ModUtils) were `[Obsolete]` in v6.0/v6.0.1. In v6.1 they are now `internal` and the 28 [Obsolete] v5.x wrappers in Core/ are deleted. Workshop CS mods MUST pin to ModFramework v6.0.0 — see MIGRATION_v5_to_v6.md.

See [MIGRATION_v5_to_v6.md](MIGRATION_v5_to_v6.md) for the full migration guide.

---

## Quick Start — Create a New v6.0 Mod (Nexus DLL)

### 1. Install the framework

Drop `ModFramework.dll` (signed, ~2.56 MB as of v6.1.1) into `<game>/Software Inc_Data/Managed/`. The game's existing `0Harmony` + `0HarmonyLoader` mod is no longer needed — `0Harmony` is bundled inside `ModFramework.dll` via ILMerge. (Newtonsoft.Json is **no longer** bundled as of v6.1.1 — the framework uses Unity's `JsonUtility`.)

### 2. Scaffold a new mod

```powershell
.\ModFramework\Scaffolding\CreateMod.ps1 -ModName "MyAwesomeMod" -GameDir "C:\SteamLibrary\steamapps\common\Software Inc"
```

This generates a complete, ready-to-build mod project that references `ModFramework.dll` from your local `Managed/` folder.

### 3. Activate the framework in your mod

```csharp
using ModFramework.Core;

public class MyModBehaviour : ModBehaviour
{
    private ModIdentity myId;

    public override void OnActivate()
    {
        // 1) Get a ModIdentity. Reads your meta.tyd Permissions: line.
        myId = ModFrameworkActivator.OnActivate(this);
        if (myId == null) return; // framework refused to activate

        // 2) Save settings using a SafePath (mod's own Data/ folder).
        SafePath path = SafePath.GetModDataPathSafe(myId, "settings.json");
        var settings = ModFileAccess.ReadJson<MySettings>(myId, path);
        if (settings == null) { settings = new MySettings(); }

        // 3) Set up a Harmony instance.
        HarmonyLib.Harmony harmony = ModHarmony.CreateInstance(myId, "com.zicarius.mymod");
        ModHarmony.CreateAndPatchAll(myId, harmony, Assembly.GetExecutingAssembly());

        // 4) Publish an event for other mods to subscribe to.
        EventKey key = ModEvents.Publish(myId, "MyMod.OnPlayerJoined");
    }

    public override void OnDeactivate()
    {
        ModFrameworkActivator.OnDeactivate(myId);
    }
}
```

### 4. Declare your permissions in `meta.tyd`

```
ID=MyAwesomeMod
Name=My Awesome Mod
Author=YourName
Version=1.0.0
Description=Does awesome things.
Permissions: Patcher
```

If you need more than the `Patcher` preset (e.g. you register a service), list the extra flags explicitly:
```
Permissions: Patcher, ServiceRegister, EventPublish
```

Available presets: `ReadOnly`, `Patcher`, `ServiceProvider`, `ServiceConsumer`. See [DOCUMENTATION.md](DOCUMENTATION.md) for the full list of 22 fine-grained flags.

---

## Working with the v6.1 Security Barriers (overview)

v6.1 is stricter than v5.x on purpose. Coming from the old API, there are four
barriers you now have to satisfy:

1. **Ship a pre-compiled DLL mod.** The in-game C# compiler can't see `ModFramework.dll`, so a source mod fails with `CS0246`. Build a `.csproj` that references `ModFramework.dll` and drop the built DLL in `DLLMods/`.
2. **Declare permissions in `meta.tyd`** — or the privileged call throws `ModPermissionException` at runtime.
3. **Get a `ModIdentity`** from `ModFrameworkActivator.OnActivate(this)` and pass it as the first argument to every privileged call (`null` = refused, bail out).
4. **File I/O needs a `SafePath`** from an allowlisted factory — raw strings and `..` paths throw `ModPathException`.

Clear all four and every privileged call your mod makes is audit-logged. **See [DOCUMENTATION.md](DOCUMENTATION.md#working-with-the-v61-security-barriers) for the full step-by-step guide, per-barrier fixes, the SafePath factory table, and the JsonUtility settings rules.**

---

## v5.x Quick Start (LEGACY)

The v5.x API is kept as `[Obsolete]` for emergency back-compat. New mods should use the v6.0 API above.

The v5.x XML UI system (`ModFramework.UI`) and the v5.x `ModBehaviour` patterns work the same as before. See the git history for the v5.2 README.

---

## Architecture

```
ModFramework v6.1
├── modframework/ModFramework.cs          # v4.x single-file back-compat (UIHelper, ModLogger, etc. — now `internal` in v6.1)
├── modframework/ModFramework.csproj      # C# project, strong-name signed
├── modframework/Core/                    # v6.0 curated public API surface
│   ├── ModFrameworkPublicAPI.cs          # [ModFrameworkPublicAPI("v6.0")] marker attribute
│   ├── ModIdentity.cs                    # D2: per-mod identity (ModId, DisplayName, Permissions, AssemblyHash)
│   ├── ModFrameworkActivator.cs          # D2: OnActivate / OnDeactivate lifecycle
│   ├── ModRegistry.cs                    # D2: in-memory ModId -> {ModIdentity, DLLMod, Assembly}
│   ├── SecurityGuards.cs                 # D2: RequirePermission helper
│   ├── SafePath.cs                       # D1: validated path wrapper (4 factory kinds)
│   ├── ModFileAccess.cs                  # D1+D2: file I/O (v6.0 API + v5.x [Obsolete] back-compat)
│   ├── EventKey.cs                       # D4: struct — namespaced event identifier
│   ├── ModEvents.cs                      # D4: v6.0 event bus with global whitelist
│   ├── ServiceToken.cs                   # D5: struct — service ownership token
│   ├── ModServiceBridge.cs               # D5: ModServiceHost (provider) + ModServiceBridge (consumer)
│   ├── ModHarmony.cs                     # D2: Harmony wrapper
│   ├── ModSafety.cs                      # utility: try/catch wrappers with audit log
│   ├── ModUtils.cs                       # utility: formatting
│   ├── ModDependencies.cs                # utility: dependency check with audit log on dialog
│   ├── ModLoader.cs                      # utility: mod discovery
│   ├── Permission.cs                     # 22 fine-grained flags
│   ├── PermissionPresets.cs              # 4 named presets
│   ├── PermissionParser.cs               # meta.tyd parser
│   ├── FrameworkSignatureCheck.cs        # D3: pubkey token check
│   ├── AuditLog.cs                       # 30-day retention log
│   ├── ModSecurityException.cs           # base exception
│   ├── ModPathException.cs               # D1: path validation
│   └── ModPermissionException.cs         # D2: permission denied
├── modframework/_secure/                  # PRIVATE keypair (gitignored)
│   └── ModFramework.snk                  # 596 bytes — strong-name signing key
├── modframework/keys/                     # PUBLIC key (committed)
│   ├── ModFramework.pub                  # 160 bytes — public key for verification
│   └── README.md                         # keypair usage notes
├── modframework/Scaffolding/              # Mod project generator
│   ├── CreateMod.ps1                     # CLI
│   ├── CreateModGUI.ps1                  # GUI
│   └── Templates/                        # meta.tyd, csproj, behaviour, meta, UI.xml
├── modframework/Tools/ilmerge/            # ILMerge.exe + license
├── modframework/UI/                       # v5.x custom XML tags (accordion, charts, etc.)
├── modframework/GameData/                 # v5.x game data wrappers
├── modframework/Harmony/0Harmony.dll      # Bundled for ILMerge
└── modframework/Build-ModFramework.ps1    # Build script
```

---

## Security Model

v6.0 closes the following attack vectors from the v5.x public API:

| AV | Description | Mitigation |
|---|---|---|
| AV-1 | Arbitrary file overwrite via `ModFileAccess.WriteText(@"C:\Windows\...")` | D1: SafePath factory methods (path must be under allowlisted root) |
| AV-2 | Patched game methods invisible to user | D2: every Harmony patch audit-logged with mod's ModId |
| AV-3 | Any mod can `Trigger` any event by name | D4: EventKey (unforgeable, only publisher can trigger) |
| AV-4 | `GameObject.Find("MyService")` collision hijack | D5: ServiceToken + collision check (refuses to register duplicate names) |
| AV-5 | Untraceable privileged calls | D2: ModIdentity is the first arg on every privileged call |
| AV-6 | Malicious `ModFramework.dll` replacement | D3: strong-name signature check (refuses to load if token mismatches) |
| AV-7 | "Why did my settings change?" — no audit trail | D2: every setting change audit-logged with timestamp + ModId |

The 22 fine-grained permission flags + 4 presets let mod authors declare the smallest set of capabilities their mod needs, and the in-game "Mod permissions" view lets users see exactly what each mod is allowed to do.

---

## Example Mod

`ModFrameworkExample/` (sibling directory) is the canonical reference mod. It demonstrates every v6.0 API in working code with comments explaining what each call does and which permission it requires. **Ships as a pre-compiled DLL mod** (`ModFrameworkExample.dll` in `DLLMods/ModFrameworkExample/`) — it references `ModFramework.dll`, so it cannot be a source mod (the game's in-game compiler would fail with CS0246; see "Barrier 1" above).

---

## Documentation

- **[MIGRATION_v5_to_v6.md](MIGRATION_v5_to_v6.md)** — v5.x → v6.0 migration guide for existing mod authors
- **[DOCUMENTATION.md](DOCUMENTATION.md)** — Full v6.0/v6.1 API reference, including the v6.1 four-barrier author guide

---

## Third-Party Licenses

### Harmony

Bundles [Harmony](https://github.com/pardeike/Harmony) v2.3.3 by Andreas Pardeike for runtime method patching. ILMerged into `ModFramework.dll` in Release builds.

**License:** MIT
**Copyright:** (c) 2017 Andreas Pardeike
**Full license:** [Harmony/LICENSE](Harmony/LICENSE)

### Newtonsoft.Json (removed in v6.1.1)

v6.0–v6.0.1 bundled [Newtonsoft.Json](https://www.newtonsoft.com/json) v13.0.3 by James Newton-King (ILMerged into `ModFramework.dll`). **As of v6.1.1 it is no longer bundled** — its 13.x static initializer fails under the game's Unity 2018.4 Mono runtime, so the framework switched to Unity's built-in `JsonUtility`.

**License:** MIT
**Copyright:** (c) 2008 James Newton-King
**Full license:** [Newtonsoft.Json LICENSE](https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md)

### ILMerge

Bundles [ILMerge](https://github.com/dotnet/ILMerge) for merging the Release build into a single DLL.

**License:** MIT
**Full license:** [Tools/ilmerge/ILMerge.LICENSE.txt](Tools/ilmerge/ILMerge.LICENSE.txt)

---

*ModFramework is authored by Zicarius.*

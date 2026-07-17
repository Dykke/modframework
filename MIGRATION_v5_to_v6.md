# ModFramework v5.x → v6.0 Migration Guide

**For:** Mod authors currently using ModFramework v5.1 or earlier
**Estimated effort:** 30–60 minutes per mod
**Breaking change level:** MAJOR (every consuming mod must migrate)

This guide lists every breaking change from v5.x to v6.0, with a before/after code example for each. The migration is mandatory — v5.x code will still **compile** against v6.0 (the v5.x API is kept as `[Obsolete]`), but the v6.0 framework will emit `ModPermissionException` at runtime if a v5.x call path bypasses the new permission check.

---

## v6.1 update (2026-07-16) — Workshop CS mod users

**If your mod is a Workshop CS mod** (`BasementFloor`, `BuildAnarchy`, `BlueprintPlus`, `CloudServices`, `FurniturePlacementTester`, `AutoCourier` partial, or any other mod that vendors a copy of `ModFramework.cs` in its own `mod-folder/` and does not have a `meta.tyd` `Permissions:` line):

### Pin to ModFramework v6.0.0

In your mod's `meta.tyd`, add a hard dependency on `ModFramework.dll` version `6.0.0.0`:

```
Depends: ModFramework.dll@6.0.0.0
```

This is the last version with the `[Obsolete]` v5.x wrappers that your mod depends on. ModFramework v6.1+ is **not** compatible with Workshop CS mods — the v4.x single-file classes (`UIHelper`, `ModLogger`, `ModEvents`, `Notifications`, `ModSettings`, `ModUtils`) are now `internal`, and the 28 `[Obsolete]` v5.x wrappers in `Core/` are deleted. Your mod will get a CS0122 (inaccessible due to protection level) compile error if it references any of these.

**Why this matters:** v6.0.0 → v6.1 is a **permission-bypass closure**, not a feature release. The 28 `[Obsolete]` wrappers in v6.0.0/v6.0.1 intentionally skipped `SecurityGuards.RequirePermission` for back-compat. A malicious Nexus DLL could exploit the bypass to do file I/O / events / services with no declared permission. v6.1 closes the bypass; Workshop CS mods that depended on the wrappers can no longer build.

**Workshop CS mods are out of v6.x's audience.** The v6.0 plan's audience decision is "Nexus DLL mods only". Workshop CS mods are explicitly out of scope. If a v6.0.0 security fix is ever needed in the future, the user can backport manually — but there is no ongoing v6.x support for Workshop CS mods.

### If you are a Nexus DLL mod author

Continue using the latest ModFramework. The v6.0 API (`ModIdentity` + `SafePath` + `EventKey` + `ServiceToken` + 22 permission flags) is unchanged in v6.1. The only difference is that the v4.x legacy back-compat surface is gone — but you should be using the v6.0 API anyway, so this does not affect you. The `[CS0618] obsolete` warnings in your build output will go away.

### What changed in v6.1

- 6 v4.x single-file classes (`UIHelper`, `ModLogger`, `ModEvents`, `Notifications`, `ModSettings`, `ModUtils`) changed from `public` to `internal` in `modframework/ModFramework.cs`.
- 28 `[Obsolete]` v5.x wrappers deleted from `Core/`:
  - `ModFileAccess.cs` — 18 wrappers
  - `ModHarmony.cs` — 4 wrappers
  - `ModEvents.cs` — 1 wrapper
  - `ModServiceBridge.cs` — 5 wrappers
- `AssemblyVersion` bumped 6.0.1.0 → 6.1.0.0. Strong-name token unchanged (`e0967644e3ffec06`).
- In-game "Mod Audit Log" + "Mod permissions" windows added. See the "In-game Audit Log Window" section below.

### How to verify your mod is not affected

Compile your mod against `ModFramework.dll` v6.1.0.0. If you get 0 errors and 0 `[CS0618]` warnings, you are using only the v6.0 API and are not affected by the v6.1 changes. If you get any `[CS0618]` warnings, you are using a v5.x back-compat path — migrate to the v6.0 API (see the v6.0 migration sections below).

### In-game Audit Log Window (new in v6.1)

A new "ModFramework Settings" entry appears in the Mods tab. Click it to open:
- **Mods tab** — list of every registered mod + its declared permissions.
- **Audit Log tab** — scrollable view of every privileged call (last 1000 lines from today's log, filterable, with date picker + "Open folder" button).

This is a separate tiny shim mod (`ModFrameworkSettings/`) that ships with v6.1.

---

## TL;DR — the 5 things that MUST change

1. **Call `ModFrameworkActivator.OnActivate(this)` in your mod's `OnActivate` method** to get a `ModIdentity`. Pass it as the first argument to every privileged framework call.
2. **Wrap every file path in a `SafePath`** (use `SafePath.GetModDataPathSafe(id, ...)` etc.) — you can no longer pass raw `string` paths to `ModFileAccess`.
3. **Declare your permissions in `meta.tyd`** — add a `Permissions: ...` line with the smallest set of flags your mod needs. Without it, the framework uses the `Patcher` preset (which is wrong for most mods).
4. **Replace `string` event/service names with `EventKey` / `ServiceToken`** — the v6.0 event bus and service registry both use unforgeable tokens.
5. **Drop direct calls to v4.x [Obsolete] classes** — `UIHelper`, `ModLogger`, `ModEvents` v4.x, `Notifications`, `ModSettings` v4.x, `ModUtils` v4.x. Use the v6.0 equivalents (see the table below).

The v5.x back-compat layer means **your v5.x code will still compile** — but the `CS0618` warnings will drown your build output, and runtime calls through the v5.x API **bypass the permission check** (they are deliberately unsafe for v6.0; the back-compat is for emergency hotfixes only).

---

## 1. `OnActivate` / `OnDeactivate` lifecycle

Every v6.0 mod must call `ModFrameworkActivator.OnActivate` in their `OnActivate` to get a `ModIdentity`. The `ModIdentity` is the unforgeable handle the framework uses to attribute your calls to your mod.

### Before (v5.x)
```csharp
public class MyModBehaviour : ModBehaviour
{
    public override void OnActivate()
    {
        // No lifecycle hook — you just started doing your thing.
        ModFileAccess.WriteText("data.json", myJson);
    }
}
```

### After (v6.0)
```csharp
public class MyModBehaviour : ModBehaviour
{
    private ModIdentity myId;

    public override void OnActivate()
    {
        myId = ModFrameworkActivator.OnActivate(this);
        if (myId == null) return; // framework refused to activate (e.g. missing permission)

        SafePath dataPath = SafePath.GetModDataPathSafe(myId, "data.json");
        ModFileAccess.WriteJson(myId, dataPath, myJson);
    }

    public override void OnDeactivate()
    {
        ModFrameworkActivator.OnDeactivate(myId);
    }
}
```

**Why this matters:** Without a `ModIdentity`, the framework can't attribute your calls to your mod. The audit log shows `<unattributed>` for unattributed calls, and the permission system can't know what your mod is allowed to do.

---

## 2. `SafePath` replaces raw `string` paths

`string` paths are no longer accepted by `ModFileAccess`. You must obtain a `SafePath` from one of 4 factories:

| Factory | Resolves to | Use case |
|---|---|---|
| `SafePath.GetModDataPathSafe(id, "sub/file.txt")` | `<modFolder>/Data/sub/file.txt` | Per-mod data (settings, caches, save files) |
| `SafePath.GetPersistentDataPathSafe(id, "sub/file.txt")` | `%persistentDataPath%/Mods/<modId>/sub/file.txt` | Cross-save data (achievements, analytics) |
| `SafePath.GetManagedPathSafe(id, "0Harmony.dll")` | `<game>/Software Inc_Data/Managed/0Harmony.dll` | Read-only — checking game DLLs |
| `SafePath.GetUserApprovedPath(id, "C:\...")` | Whatever the user approved | One-time user dialog grant |

### Before (v5.x)
```csharp
string path = Path.Combine(parentMod.FolderPath(), "Data", "settings.json");
ModFileAccess.WriteText(path, json);
```

### After (v6.0)
```csharp
SafePath path = SafePath.GetModDataPathSafe(myId, "settings.json");
ModFileAccess.WriteText(myId, path, json);
```

**What happens if I pass a raw `string`?** You get a `CS0618` warning (the v5.x method is `[Obsolete]`), and the call still works — **but it bypasses the permission check**. The audit log will show `<unattributed>` and a missing permission, and your mod's `FileWrite` operation will be invisible to the user.

---

## 3. Declare your permissions in `meta.tyd`

v6.0 mods MUST declare their permissions in `meta.tyd`. The framework reads this on `OnActivate` and rejects any mod that tries to call a privileged API it didn't declare.

### The 22 permission flags

`FileRead`, `FileWrite`, `FileAppend`, `FileDelete`, `FileDirectoryList`, `FileUserApproved`, `HarmonyRead`, `HarmonyPatch`, `HarmonyUnpatch`, `EventPublish`, `EventSubscribe`, `ServiceRegister`, `ServiceConsume`, `SettingsRead`, `SettingsWrite`, `SettingsDelete`, `GameEventWhitelist`, `AuditLogRead`, `NotificationPublish`, `NotificationManage`, `UiAddElement`, `UiManageWindow`.

### 4 presets (named, equivalent to the flag lists)

- `ReadOnly` → `FileRead, FileDirectoryList, HarmonyRead, EventSubscribe, SettingsRead, AuditLogRead`
- `Patcher` → `ReadOnly + HarmonyPatch, HarmonyUnpatch, SettingsWrite, SettingsDelete, ServiceConsume`
- `ServiceProvider` → `ReadOnly + ServiceRegister, EventPublish, FileWrite, FileDelete, NotificationPublish`
- `ServiceConsumer` → `ReadOnly + ServiceConsume`

### Before (v5.x, `meta.tyd`)
```
ID=MyMod
Name=My Mod
Author=Zicarius
Version=1.0.0
Description=Does stuff
```

### After (v6.0, `meta.tyd`)
```
ID=MyMod
Name=My Mod
Author=Zicarius
Version=1.0.0
Description=Does stuff
Permissions: Patcher
```

If your mod needs more than the preset, list the flags explicitly:
```
Permissions: Patcher, ServiceRegister, EventPublish
```

**What happens if I don't declare permissions?** The framework uses the `Patcher` preset by default. If your mod only reads files and never patches, you should declare `Permissions: ReadOnly` instead. If you need to register a service, you must explicitly add `ServiceRegister`.

---

## 4. `EventKey` replaces `string` event names

The v6.0 event bus (`ModFramework.Core.ModEvents`) uses unforgeable `EventKey` tokens. Only the mod that published an event can trigger it later.

### Before (v5.x)
```csharp
// Subscriber
ModEvents.Subscribe("MyMod.OnPlayerJoined", (sender, args) => { ... });

// Publisher
ModEvents.Trigger("MyMod.OnPlayerJoined", player);
```

### After (v6.0)
```csharp
// Publisher
EventKey key = ModEvents.Publish(myId, "OnPlayerJoined");
// (optionally) Trigger later
ModEvents.Trigger(myId, key, player);

// Subscriber — gets the key from the publisher
ModEvents.Subscribe(myId, key, args => { ... });
```

**Why this matters:** In v5.x, any mod could `Trigger("MyMod.OnPlayerJoined", fakePlayer)` to spoof events on another mod's bus. In v6.0, `Trigger` requires the matching `ModIdentity` and `EventKey`, so only the publisher can trigger. Cross-mod event spoofing is now impossible.

---

## 5. `ServiceToken` replaces `string` service names

The v6.0 service registry (`ModFramework.Core.ModServiceHost` + `ModServiceBridge`) uses unforgeable `ServiceToken` tokens. Only the mod that registered a service can unregister it; only consumers with the token can find/send to it.

### Before (v5.x)
```csharp
// Provider
GameObject go = ModServiceHost.Register("MyMod.MyService", g => {
    g.AddComponent<MyService>();
});
// Cleanup
ModServiceHost.Unregister("MyMod.MyService");

// Consumer
if (ModServiceBridge.IsAvailable("MyMod.MyService"))
    ModServiceBridge.Send("MyMod.MyService", "DoWork", args);
```

### After (v6.0)
```csharp
// Provider
ServiceToken token = ModServiceHost.Register(myId, "MyMod.MyService", g => {
    g.AddComponent<MyService>();
});
// Expose token to consumers — via a static method on your mod, or a public constant.
// e.g. publish a public static readonly ServiceToken Token = ...; on your mod class.
// Cleanup
ModServiceHost.Unregister(myId, token);

// Consumer — gets the token from the provider (e.g. via its mod class)
if (ModServiceBridge.IsAvailable(myId, providerMod.Token))
    ModServiceBridge.Send(myId, providerMod.Token, "DoWork", args);
```

**Why this matters:** In v5.x, any mod could `GameObject.Find("MyMod.MyService")` and SendMessage a hijacked service. In v6.0, the `ServiceToken` is a struct containing the provider's `ModId` + a GUID; the consumer must hold it to use the service. The framework also refuses to register a service name already registered by a different mod (collision check).

---

## 6. v4.x [Obsolete] class replacements

| v4.x (in `ModFramework` namespace, [Obsolete]) | v6.0 replacement | Notes |
|---|---|---|
| `UIHelper.AddButton(...)` | `WindowManager.SpawnButton()` + `WindowManager.AddElementToElement(...)` | Direct calls. v6.0 doesn't provide a wrapper — the v4.x wrapper is too generic. |
| `UIHelper.AddLabel(...)` | `WindowManager.SpawnLabel()` + manual placement | Same as above |
| `ModLogger.Log(...)` | `UnityEngine.Debug.Log(...)` | ModFramework never had a "framework-aware" logger in v6.0. Direct `Debug.Log` is fine. |
| `ModEvents.Trigger(string, object)` | `ModFramework.Core.ModEvents.Trigger(ModIdentity, EventKey, object)` | See section 4 above. |
| `Notifications.Show(...)` | `WindowManager.SpawnDialog(...)` directly | Notifications class is being removed in v6.1. |
| `ModSettings.SetBool(string, bool)` | `ModFramework.Core.ModSettingsV6` (not yet implemented — see follow-up) | For now, use direct `PlayerPrefs` or write your own per-mod settings JSON via `ModFileAccess`. |
| `ModUtils.FormatCurrency(float)` | `ModFramework.Core.ModUtils.FormatCurrency(float)` | Pure formatting, no migration friction. |
| `ModUtils.IsInGame()` | `ModFramework.Core.ModUtils.IsInGame()` | Same. |

---

## 7. Harmony patching needs a `ModIdentity`

### Before (v5.x)
```csharp
Harmony instance = new Harmony("com.zicarius.mymod");
instance.PatchAll(Assembly.GetExecutingAssembly());
```

### After (v6.0)
```csharp
Harmony instance = ModHarmony.CreateInstance(myId, "com.zicarius.mymod");
ModHarmony.PatchAll(myId, instance, Assembly.GetExecutingAssembly());
```

`ModHarmony.CreateInstance` requires `Permission.HarmonyRead`. `ModHarmony.PatchAll` requires `Permission.HarmonyPatch`. Both audit-log.

---

## 8. `ModDependencies` now audit-logs

If you use `ModDependencies.ShowMissingMessage` to show a "missing dependency" dialog, the v6.0 framework now writes a `DEP_DIALOG_SHOWN` line to the audit log. Nothing to change on your end — the API signature is the same.

---

## 9. The framework's own strong-name signature check

v6.0 `ModFramework.dll` is strong-name signed (public key token `e0967644e3ffec06`). The framework's static constructor checks its own assembly's public key token and throws if it doesn't match.

**What this means for mod authors:** You don't need to do anything special. As long as the official `ModFramework.dll` from Nexus is in `Software Inc_Data/Managed/`, the framework will load. If a malicious actor replaces it with an unsigned or differently-signed DLL, the framework refuses to load. This is invisible to legitimate mod authors.

If you write your own internal tool that references `ModFramework.dll`, you can verify the signature with:
```powershell
[System.Reflection.Assembly]::ReflectionOnlyLoadFrom("ModFramework.dll").GetName().GetPublicKeyToken() | ForEach-Object { $_.ToString("x") }
# Expected: e0967644e3ffec06
```

---

## 10. C# 5 compatibility

v6.0 framework code is C# 5 compatible (no `?.`, no `$"..."`, no `nameof`, no auto-property initializers, no expression-bodied members). This is so Workshop mod authors using older C# compilers can still consume the v6.0 API.

**What this means for mod authors:** You can write your mod in any C# version you want — the C# 5 restriction only applies to the framework's own code, not to consumer code. But if you want your mod to be portable across the older Workshop compilers, follow the same rule in your own code.

---

## Quick migration checklist

Use this as a step-by-step checklist when porting a v5.x mod:

- [ ] 1. Add `using ModFramework.Core;` to your mod's main file
- [ ] 2. In your mod's `OnActivate`, add `myId = ModFrameworkActivator.OnActivate(this);`
- [ ] 3. In your mod's `OnDeactivate`, add `ModFrameworkActivator.OnDeactivate(myId);`
- [ ] 4. Add `Permissions: ...` to your `meta.tyd` (start with the closest preset)
- [ ] 5. Replace every `ModFileAccess.X(string, ...)` call with the v6.0 `(ModIdentity, SafePath, ...)` form
- [ ] 6. Replace every `ModHarmony.X(...)` call with the v6.0 `(ModIdentity, ...)` form
- [ ] 7. If you use `ModEvents`, switch to the v6.0 `EventKey` API
- [ ] 8. If you use `ModServiceHost`/`ModServiceBridge`, switch to the v6.0 `ServiceToken` API
- [ ] 9. Drop direct calls to the v4.x [Obsolete] classes (UIHelper, ModLogger, etc.)
- [ ] 10. Build clean (no CS0618 warnings, no CS8002 warnings)
- [ ] 11. In-game test: does the mod still work? Check the in-game "Mod Audit Log" window — you should see your mod's operations attributed to its `ModId` + `DisplayName`.

---

## What stays the same

To reduce migration friction, the following v5.x patterns still work in v6.0:

- **`ModBehaviour.OnActivate` / `OnDeactivate` lifecycle** — same as before
- **`WindowManager`, `GUIWindow`, `DialogWindow`, `TimeOfDay`, `GameSettings`** — game APIs, unchanged
- **`HarmonyLib.Harmony` / `Harmony.Patch` / `Harmony.Unpatch`** — game API, unchanged (you just need to obtain the instance through `ModHarmony.CreateInstance` now)
- **`ModSafety.Try(...)`** — same signature, no change. (Internally now writes to audit log on caught errors using `<unattributed>` sentinel — visible in the in-game audit log window.)
- **`ModUtils.FormatXxx(...)`** — same signatures, no change.

---

## Need help?

If your mod is using a pattern not covered here, check:
- The full API reference: `modframework/DOCUMENTATION.md` (coming in Phase 3)
- The v6.0 security plan: `cursor-stuff/plans/modframework-security-hardening-v6.md`
- The example mod: `modframework/Scaffolding/Examples/ModFrameworkExample/` (coming in Phase 3) — demonstrates every new API in working code
- The Discord #modframework channel (link in Nexus page)

For security-related questions, see `modframework/notes/security-review.md`.

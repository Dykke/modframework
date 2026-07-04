// ============================================================================
// ModFramework v5.1 - Slim utility layer for Software Inc modding
// Created by: Zicarius
// Version: 5.1
// ============================================================================
//
// v5.1 adds four new Core helpers for runtime mod management (see plan in
// cursor-stuff/modframework-v5.1-plan.md):
//
//   Core/ModSafety.cs          - Try/catch wrappers for safe mod execution
//   Core/ModUtils.cs           - Formatting helpers and IsInGame() check
//   Core/ModDependencies.cs    - Dependency declaration + verification (NEW in v5.1)
//   Core/ModFileAccess.cs      - Safe file I/O + JSON (uses Newtonsoft.Json, NEW in v5.1)
//   Core/ModLoader.cs          - Mod discovery / "is mod X loaded?" (NEW in v5.1)
//   Core/ModHarmony.cs         - Centralized Harmony patcher wrapper (NEW in v5.1)
//   GameData/                  - Null-safe wrappers for Company, Employee, Market, Product data
//   Harmony/0Harmony.dll       - Bundled Harmony 2.x for runtime patching (ILMerged in Release builds)
//   Vendor/Newtonsoft.Json.dll - Bundled Newtonsoft.Json (ILMerged in Release builds)
//
// v5.0 stripped all code redundant with the game's native API (Beta 1.8.36+).
// UI is built with WindowManager.GenerateUI() and XML files.
//
// What was removed (use native API instead):
//   ModLogger       -> Debug.Log("[YourMod] message")
//   ModSettings     -> this.SaveSetting("key", value) / this.LoadSetting<T>("key", default)
//   ModLifecycle    -> Subscribe to TimeOfDay.OnDayPassed, GameSettings.GameReady, etc. directly
//   ModEvents       -> Subscribe to game events directly
//   ModPatching     -> Use ModFramework.Core.ModHarmony.CreateAndPatchAll(...) (or new Harmony(...).PatchAll() directly)
//   Notifications   -> HUD.Instance.AddPopupMessage(...) directly
//   All UI/Custom/* -> WindowManager.GenerateUI() with XML files
//   UIHelper        -> WindowManager.GenerateUI() with XML files
//
// v5.1 NEW: a single ModFramework.dll ships 0Harmony and Newtonsoft.Json
// ILMerged inside it (Release builds only). Mods only need to reference
// ModFramework.dll — no separate Dependencies/ folder. See
// Tools/ilmerge/README.md for build setup.
// ============================================================================

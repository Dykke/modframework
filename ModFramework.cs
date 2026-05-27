// ============================================================================
// ModFramework v5 - Slim utility layer for Software Inc modding
// Created by: Zicarius
// Version: 5.0
// ============================================================================
//
// v5 strips all code redundant with the game's native API (Beta 1.8.36+).
// UI is now built with WindowManager.GenerateUI() and XML files.
//
// What's in v5:
//   Core/ModSafety.cs      - Try/catch wrappers for safe mod execution
//   Core/ModUtils.cs       - Formatting helpers and IsInGame() check
//   GameData/              - Null-safe wrappers for Company, Employee, Market, Product data
//   Harmony/0Harmony.dll   - Bundled Harmony 2.x for runtime patching
//
// What was removed (use native API instead):
//   ModLogger       -> Debug.Log("[YourMod] message")
//   ModSettings     -> this.SaveSetting("key", value) / this.LoadSetting<T>("key", default)
//   ModLifecycle    -> Subscribe to TimeOfDay.OnDayPassed, GameSettings.GameReady, etc. directly
//   ModEvents       -> Subscribe to game events directly
//   ModPatching     -> new Harmony("your.mod.id").PatchAll() directly
//   Notifications   -> HUD.Instance.AddPopupMessage(...) directly
//   All UI/Custom/* -> WindowManager.GenerateUI() with XML files
//   UIHelper        -> WindowManager.GenerateUI() with XML files
// ============================================================================

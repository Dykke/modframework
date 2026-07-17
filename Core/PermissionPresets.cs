// PermissionPresets.cs
// ModFramework v6.0
//
// Curated permission sets for common mod categories. A mod's meta.tyd can
// reference a preset by name (e.g. "Permissions: Patcher") instead of listing
// every individual flag. The PermissionParser expands the preset into the
// matching Permission bitfield at OnActivate.
//
// Four presets cover ~95% of real mods. Anything that needs a custom set
// can list individual flags in meta.tyd instead of using a preset.

namespace ModFramework.Core
{
    /// <summary>
    /// Predefined permission sets. A mod's meta.tyd can reference a preset by
    /// name (e.g. <c>Permissions: Patcher</c>) or list individual flags
    /// (e.g. <c>Permissions: FileRead, FileWrite, HarmonyPatch</c>).
    /// </summary>
    [ModFrameworkPublicAPI("v6.0", Reason = "Common case shortcut for meta.tyd")]
    public static class PermissionPresets
    {
        /// <summary>Read-only mod: read its own data files + read game state.</summary>
        public const string ReadOnly = "ReadOnly";

        /// <summary>Most common: patches game methods and reads/writes its own data files.</summary>
        public const string Patcher = "Patcher";

        /// <summary>Mod that provides a service for other mods to consume.</summary>
        public const string ServiceProvider = "ServiceProvider";

        /// <summary>Mod that consumes services other mods provide.</summary>
        public const string ServiceConsumer = "ServiceConsumer";

        /// <summary>Expand a preset name to its flag list. Returns null for unknown presets.</summary>
        public static string Expand(string preset)
        {
            if (string.IsNullOrEmpty(preset)) return null;
            if (preset == ReadOnly) return "FileRead, FileDirectoryList, SettingsRead";
            if (preset == Patcher) return "FileRead, FileWrite, FileDirectoryList, HarmonyRead, HarmonyPatch, HarmonyUnpatch, SettingsRead, SettingsWrite";
            if (preset == ServiceProvider) return "ServiceRegister, SettingsRead, SettingsWrite";
            if (preset == ServiceConsumer) return "ServiceConsume, EventSubscribe, SettingsRead";
            return null;
        }
    }
}

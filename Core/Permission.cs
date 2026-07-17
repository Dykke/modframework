// Permission.cs
// ModFramework v6.0
//
// 22 fine-grained permission flags that control access to privileged framework
// operations. Mods declare their required permissions in meta.tyd; the
// framework enforces them at every privileged call site.
//
// The full 22-flag list (per modframework-security-hardening-v6.md D6.1):
//
//   File* (6):    FileRead, FileWrite, FileAppend, FileDelete,
//                 FileDirectoryList, FileUserApproved
//   Harmony* (3): HarmonyRead, HarmonyPatch, HarmonyUnpatch
//   Event* (2):   EventSubscribe, EventPublish
//   Service* (2): ServiceRegister, ServiceConsume
//   Settings* (3):SettingsRead, SettingsWrite, SettingsDelete
//   Misc* (6):    GameReflection, GameEventWhitelist (OnGameSaved etc.),
//                 AuditLogRead, AuditLogExport,
//                 UserDialogPrompt, NetworkAccess
//
// Closes: every AV that requires a privileged operation. The mod author must
// declare each permission explicitly; missing declaration = blocked at runtime.
//
// Full implementation lands in Phase 2 (D6 = M-5). This file is the Phase 0
// skeleton — the [Flags] enum itself.

using System;

namespace ModFramework.Core
{
    /// <summary>
    /// Fine-grained permission flags declared in a mod's meta.tyd. The
    /// framework rejects any privileged call whose required flag is not set
    /// in the calling mod's ModIdentity.Permissions.
    /// </summary>
    [Flags]
    [ModFrameworkPublicAPI("v6.0", Reason = "Core of the v6.0 permission model")]
    public enum Permission
    {
        /// <summary>No permissions. The mod can do nothing privileged.</summary>
        None = 0,

        // ---- File I/O (6) ----
        /// <summary>Read files via SafePath (ModFileAccess.ReadText/Bytes/Json).</summary>
        FileRead = 1 << 0,
        /// <summary>Write files via SafePath (ModFileAccess.WriteText/Bytes/Json).</summary>
        FileWrite = 1 << 1,
        /// <summary>Append to files via SafePath (ModFileAccess.AppendText).</summary>
        FileAppend = 1 << 2,
        /// <summary>Delete files via SafePath (ModFileAccess.Delete/DeleteIfExists).</summary>
        FileDelete = 1 << 3,
        /// <summary>Enumerate directories via SafePath (ModFileAccess.DirectoryExists/EnsureDirectory).</summary>
        FileDirectoryList = 1 << 4,
        /// <summary>Prompt the user to approve a path outside the mod's own folders (GetUserApprovedPath).</summary>
        FileUserApproved = 1 << 5,

        // ---- Harmony (3) ----
        /// <summary>Create a Harmony instance and inspect patched methods (ModHarmony.CreateInstance / PatchCount).</summary>
        HarmonyRead = 1 << 6,
        /// <summary>Apply Harmony patches (ModHarmony.CreateAndPatchAll / Harmony.Patch).</summary>
        HarmonyPatch = 1 << 7,
        /// <summary>Remove Harmony patches (ModHarmony.UnpatchAll / Harmony.Unpatch).</summary>
        HarmonyUnpatch = 1 << 8,

        // ---- Events (2) ----
        /// <summary>Subscribe to events on the framework's event bus (ModEvents.Subscribe).</summary>
        EventSubscribe = 1 << 9,
        /// <summary>Publish events that other mods can subscribe to (ModEvents.Publish + Trigger).</summary>
        EventPublish = 1 << 10,

        // ---- Services (2) ----
        /// <summary>Register a service that other mods can find (ModServiceHost.Register).</summary>
        ServiceRegister = 1 << 11,
        /// <summary>Find and call a service registered by another mod (ModServiceBridge.Find).</summary>
        ServiceConsume = 1 << 12,

        // ---- Settings (3) ----
        /// <summary>Read settings via ModSettings (Get* methods).</summary>
        SettingsRead = 1 << 13,
        /// <summary>Write settings via ModSettings (Set* methods).</summary>
        SettingsWrite = 1 << 14,
        /// <summary>Delete the mod's entire settings file (ModSettings.DeleteAll).</summary>
        SettingsDelete = 1 << 15,

        // ---- Misc (6) ----
        /// <summary>Use reflection to probe game internals (ModUtils.GetSingleton, custom GameData lookups).</summary>
        GameReflection = 1 << 16,
        /// <summary>Publish to the whitelisted global game events (OnGameSaved, OnCompanyFounded, OnSoftwareReleased, OnMonthPassed, OnDayPassed, OnGameLoaded).</summary>
        GameEventWhitelist = 1 << 17,
        /// <summary>Read the audit log (in-game Mod Audit Log window).</summary>
        AuditLogRead = 1 << 18,
        /// <summary>Export the audit log to a file the user chooses.</summary>
        AuditLogExport = 1 << 19,
        /// <summary>Show a one-time user dialog (for GetUserApprovedPath prompts, etc.).</summary>
        UserDialogPrompt = 1 << 20,
        /// <summary>Open a network connection (HTTP, sockets). Currently unused — reserved for future.</summary>
        NetworkAccess = 1 << 21,
    }
}

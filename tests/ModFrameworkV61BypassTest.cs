// ModFrameworkV61BypassTest.cs
// A test mod that tries to use the v5.x [Obsolete] wrappers and v4.x single-file
// classes that were removed in ModFramework v6.1. Every line should fail to compile.
//
// v6.1 audit: this file should produce CS0122 (inaccessible due to its protection
// level) for every line below that targets an internal v4.x class, and CS0117
// (does not contain a definition) for every line below that targets a removed
// v5.x wrapper. If either is missing, the v6.1 bypass closure is INCOMPLETE.
//
// Note: we use fully qualified names to avoid the C# using-namespace shadowing
// issue (this file is in namespace ModFrameworkV61BypassTest which has a
// 'ModFramework' prefix that confuses the resolver).

using UnityEngine;

namespace ModFrameworkV61BypassTest
{
    public class BypassTest
    {
        public void TryV4xClasses()
        {
            // UIHelper is internal in v6.1 — should fail with CS0122
            var btn = ModFramework.UIHelper.AddButton("text", new Rect(0,0,100,30), null, null);
            // ModLogger is internal in v6.1 — should fail with CS0122
            ModFramework.ModLogger.Log("test");
            // ModSettings is internal in v6.1 — should fail with CS0122
            ModFramework.ModSettings.SetBool("key", true);
            // ModUtils is internal in v6.1 — should fail with CS0122
            string s = ModFramework.ModUtils.FormatCurrency(100f);
            // Notifications is internal in v6.1 — should fail with CS0122
            ModFramework.Notifications.Show("test");
            // ModFramework.ModEvents is internal in v6.1 — should fail with CS0122
            ModFramework.ModEvents.Subscribe("OnGameSaved", handler);
        }

        public void TryV5xFileAccessWrappers()
        {
            // WriteText(string, string) was removed in v6.1 — should fail with CS0117
            ModFramework.Core.ModFileAccess.WriteText("C:\\Windows\\System32\\evil.dll", "pwned");
            // ReadJson<T>(string) was removed in v6.1 — should fail with CS0117
            var data = ModFramework.Core.ModFileAccess.ReadJson<MyData>("C:\\path\\to\\data.json");
            // Delete(string) was removed in v6.1 — should fail with CS0117
            ModFramework.Core.ModFileAccess.Delete("C:\\important_file");
            // EnsureDirectory(string) was removed in v6.1 — should fail with CS0117
            ModFramework.Core.ModFileAccess.EnsureDirectory("C:\\some\\path");
        }

        public void TryV5xHarmonyWrappers()
        {
            // CreateInstance(string) was removed in v6.1 — should fail with CS0117
            var harmony = ModFramework.Core.ModHarmony.CreateInstance("com.evil.patch");
            // CreateAndPatchAll(string, Assembly) was removed in v6.1 — should fail with CS0117
            var harmony2 = ModFramework.Core.ModHarmony.CreateAndPatchAll("com.evil.patch");
            // UnpatchAll(Harmony) was removed in v6.1 — should fail with CS0117
            ModFramework.Core.ModHarmony.UnpatchAll(harmony);
            // PatchCount(Harmony) was removed in v6.1 — should fail with CS0117
            int n = ModFramework.Core.ModHarmony.PatchCount(harmony);
        }

        public void TryV5xServiceWrappers()
        {
            // Register(string, Action<GameObject>) was removed in v6.1 — should fail with CS0117
            var go = ModFramework.Core.ModServiceHost.Register("evil_service", null);
            // Unregister(string) was removed in v6.1 — should fail with CS0117
            ModFramework.Core.ModServiceHost.Unregister("evil_service");
            // IsAvailable(string) was removed in v6.1 — should fail with CS0117
            bool ok = ModFramework.Core.ModServiceBridge.IsAvailable("some_service");
            // Find(string) was removed in v6.1 — should fail with CS0117
            var svc = ModFramework.Core.ModServiceBridge.Find("some_service");
            // Send(string, string, ...) was removed in v6.1 — should fail with CS0117
            ModFramework.Core.ModServiceBridge.Send("some_service", "DoEvil", null);
        }

        public void TryV5xEventWrapper()
        {
            // Trigger(string, object) was removed in v6.1 — should fail with CS0117
            ModFramework.Core.ModEvents.Trigger("OnGameSaved", null);
        }

        // Stub
        public class MyData { public int X; }
        private void handler(object data) { }
    }
}

// ModFrameworkSettingsWindow.cs
// ModFramework v6.1
//
// In-game "Mod Audit Log" + "Mod permissions" windows. Built when the user
// opens the "ModFramework Settings" entry in the Mods tab. The framework
// provides all UI logic; the shim mod (DLLMods/ModFrameworkSettings/) hosts
// the ModMeta that calls into this class.
//
// Architecture note (cursor-stuff/code-decompilations/ModController.cs:55-120):
// the game's mod loader only scans DLLMods/ for mods, NOT Managed/. So
// ModFramework.dll (in Managed/) can't host a Mods tab entry directly. The
// shim mod is a tiny DLLMod whose ModMeta.ConstructOptionsScreen just calls
// OpenMainWindow(parent, inGame). NO game UI patching — preserves the v6.0
// plan's preference.
//
// USAGE (from the shim mod's ModMeta):
//   public override void ConstructOptionsScreen(RectTransform parent, bool inGame)
//   {
//       ModFramework.Core.ModFrameworkSettingsWindow.OpenMainWindow(parent, inGame);
//   }
//
// The window has two tabs:
//   1. "Mods" — list of every registered mod (from ModRegistry.All). For each:
//      modId, display name, declared permissions, # of today's audit log lines.
//      Click → opens the permissions sub-window (read-only display of all 22
//      permission flags + which ones are granted).
//   2. "Audit Log" — scrollable text view of the last 1000 audit log lines
//      (read from today's file). Filterable by modId. "Open log folder" button
//      opens %persistentDataPath%/ModFramework/ in Explorer. Date picker for
//      viewing historical logs.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using ModFramework.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ModFramework.Core
{
    /// <summary>
    /// Static facade for the in-game "ModFramework Settings" window. The
    /// shim mod's ModMeta.ConstructOptionsScreen calls OpenMainWindow
    /// to build the UI into the provided parent RectTransform.
    /// </summary>
    [ModFrameworkPublicAPI("v6.1", Reason = "In-game audit log + permissions window")]
    public static class ModFrameworkSettingsWindow
    {
        private const string Tag = "[ModFramework.SettingsWindow]";

        // Track the currently-open window so we can close it before opening
        // a new one. (The shim mod's Meta can be called multiple times by
        // the game's mod loader if the user re-opens the settings tab.)
        private static GUIWindow _activeMainWindow;
        private static GUIWindow _activePermissionsWindow;

        // The list of registered mods at the time the "Mods" tab was built.
        // Cached so the audit log refresh doesn't churn the UI.
        private static ModIdentity[] _cachedMods = new ModIdentity[0];

        // The "Audit Log" text area reference (for refresh).
        private static Text _activeLogText;
        private static GameObject _activeLogGameObject;
        private static InputField _activeLogFilter;
        private static int _activeLogLineCount;

        // v6.1.2: the two tab content containers. BOTH stay active at all times
        // so the game's one-shot content-size measurement (OptionsWindow
        // .AddModOption, which excludes INACTIVE children) sees both tabs and
        // sizes the scroll region to fit the taller one. Visibility is toggled
        // via a CanvasGroup alpha (0/1) instead of SetActive, which keeps the
        // objects active-for-measurement but invisible. Fixes the "content cut
        // off / scroll too short" bug (the old fixed-400px containers capped the
        // region and clipped the audit log's overflow).
        private static RectTransform _modsTab;
        private static RectTransform _auditTab;
        private static CanvasGroup _modsGroup;
        private static CanvasGroup _auditGroup;
        private static RectTransform _contentRoot;

        /// <summary>
        /// Build the main "ModFramework Settings" window inside the parent
        /// RectTransform. Called by the shim mod's ModMeta.ConstructOptionsScreen.
        /// </summary>
        public static void OpenMainWindow(RectTransform parent, bool inGame)
        {
            if (parent == null)
            {
                UnityEngine.Debug.LogWarning(Tag + " OpenMainWindow called with null parent.");
                return;
            }

            // Close any existing window to avoid duplicates.
            CloseMainWindow();

            // v6.1.2: NO wrapper SpawnPanel, and the tab containers are now
            // ZERO-SIZED RectTransforms (was a fixed 500x400). The game sizes
            // this mod's scroll region in OptionsWindow.AddModOption by taking
            // the max bounds of `parent`'s children AFTER we return — so a
            // 400px-tall container FORCED a 400px region (empty space in the
            // Mods tab) and CLIPPED the audit log where its text overflowed past
            // 400. A zero-sized container contributes nothing itself; only its
            // (measured) descendant elements define the region height, so the
            // region now fits the actual content exactly. `parent` is this mod's
            // own scrollable Options>Mods area (same as AutoCourier).
            //
            // Both containers are kept ACTIVE (CalculateRelativeRectTransformBounds
            // skips inactive objects); a CanvasGroup toggles each tab's alpha.
            _contentRoot = parent;

            _modsTab = new GameObject("ModFrameworkSettingsModsTab", typeof(RectTransform)).GetComponent<RectTransform>();
            _modsGroup = _modsTab.gameObject.AddComponent<CanvasGroup>();
            WindowManager.AddElementToElement(_modsTab.gameObject, parent.gameObject,
                new Rect(0, 0, 0, 0), new Rect(0, 0, 0, 0));

            _auditTab = new GameObject("ModFrameworkSettingsAuditTab", typeof(RectTransform)).GetComponent<RectTransform>();
            _auditGroup = _auditTab.gameObject.AddComponent<CanvasGroup>();
            WindowManager.AddElementToElement(_auditTab.gameObject, parent.gameObject,
                new Rect(0, 0, 0, 0), new Rect(0, 0, 0, 0));

            // Build each tab's content into its own container.
            BuildModsTab(_modsTab);
            BuildAuditLogTab(_auditTab);

            // Title (added after the containers so it draws on top of them).
            Text title = WindowManager.SpawnLabel();
            title.text = "ModFramework v6.1 — Settings";
            title.fontStyle = FontStyle.Bold;
            title.fontSize = 18;
            WindowManager.AddElementToElement(title.gameObject, parent.gameObject,
                new Rect(10, 10, 480, 25), new Rect(0, 0, 0, 0));

            // Real tab buttons (v6.1.1: replaced the old static "[ Mods ]  Audit
            // Log" label). Clicking one shows its container and hides the other.
            Button modsTabBtn = WindowManager.SpawnButton();
            modsTabBtn.GetComponentInChildren<Text>().text = "Mods";
            modsTabBtn.onClick.AddListener(new UnityEngine.Events.UnityAction(ShowModsTab));
            WindowManager.AddElementToElement(modsTabBtn.gameObject, parent.gameObject,
                new Rect(10, 40, 100, 24), new Rect(0, 0, 0, 0));

            Button auditTabBtn = WindowManager.SpawnButton();
            auditTabBtn.GetComponentInChildren<Text>().text = "Audit Log";
            auditTabBtn.onClick.AddListener(new UnityEngine.Events.UnityAction(ShowAuditTab));
            WindowManager.AddElementToElement(auditTabBtn.gameObject, parent.gameObject,
                new Rect(115, 40, 100, 24), new Rect(0, 0, 0, 0));

            // Default to the Mods tab.
            ShowModsTab();

            UnityEngine.Debug.Log(Tag + " Main window built into parent.");
        }

        private static void BuildModsTab(RectTransform mainPanel)
        {
            // Refresh button
            Button refreshButton = WindowManager.SpawnButton();
            refreshButton.GetComponentInChildren<Text>().text = "Refresh";
            refreshButton.onClick.AddListener(new UnityEngine.Events.UnityAction(RefreshAll));
            WindowManager.AddElementToElement(refreshButton.gameObject, mainPanel.gameObject,
                new Rect(390, 42, 100, 22), new Rect(0, 0, 0, 0));

            // List of registered mods.
            var mods = ModRegistry.All;
            _cachedMods = new ModIdentity[mods.Count];
            int idx = 0;
            foreach (var m in mods) { _cachedMods[idx++] = m; }

            Text header = WindowManager.SpawnLabel();
            header.text = "Registered mods (" + _cachedMods.Length + "):";
            header.fontSize = 13;
            header.fontStyle = FontStyle.Bold;
            WindowManager.AddElementToElement(header.gameObject, mainPanel.gameObject,
                new Rect(10, 70, 480, 18), new Rect(0, 0, 0, 0));

            if (_cachedMods.Length == 0)
            {
                Text empty = WindowManager.SpawnLabel();
                empty.text = "  (no mods currently registered. activate a Nexus DLL mod to see it here.)";
                empty.fontSize = 12;
                empty.color = new Color(0.35f, 0.35f, 0.35f);
                WindowManager.AddElementToElement(empty.gameObject, mainPanel.gameObject,
                    new Rect(10, 90, 480, 16), new Rect(0, 0, 0, 0));
                return;
            }

            // One row per mod. v6.1.2: each row WRAPS (permission lists are
            // long) and its height is measured from the wrapped text, so the
            // game's content-size calc (OptionsWindow.AddModOption iterates child
            // bounds) sizes the scroll region to fit every row instead of
            // truncating the permissions to a single 16px line.
            float rowY = 92f;
            int maxRows = 20;
            for (int i = 0; i < _cachedMods.Length && i < maxRows; i++)
            {
                var m = _cachedMods[i];
                if (m == null) continue;
                int auditCount = CountAuditLinesFor(m.ModId);
                string rowText = m.ModId + "  [" + m.Permissions + "]  (" + auditCount + " log lines)";
                rowY = AddWrappedRow(mainPanel, rowText, 13, new Color(0.1f, 0.1f, 0.1f), 10f, rowY, 470f);
            }
            if (_cachedMods.Length > maxRows)
            {
                AddWrappedRow(mainPanel, "... and " + (_cachedMods.Length - maxRows) + " more",
                    11, new Color(0.35f, 0.35f, 0.35f), 10f, rowY, 470f);
            }
        }

        private static void BuildAuditLogTab(RectTransform mainPanel)
        {
            // Filter input
            _activeLogFilter = WindowManager.SpawnInputbox();
            _activeLogFilter.text = "";
            _activeLogFilter.placeholder.GetComponent<Text>().text = "filter by modId...";
            // v6.1.1: match AutoCourier's working input exactly — it adds the box
            // straight onto the options `parent` (no wrapper panel) with a 28px
            // rect and its text reads fine. Force the text dark (Color.black, as
            // AutoCourier does for its labels) so it shows on the green box; the
            // exotic caret/selection overrides had no effect and were dropped.
            if (_activeLogFilter.textComponent != null)
                _activeLogFilter.textComponent.color = Color.black;
            Text filterPlaceholder = _activeLogFilter.placeholder != null ? _activeLogFilter.placeholder.GetComponent<Text>() : null;
            if (filterPlaceholder != null)
                filterPlaceholder.color = new Color(0.4f, 0.4f, 0.4f);
            // v6.1.1: live filter — re-run RefreshLog on every keystroke so the
            // box actually filters the visible log (previously it only applied
            // when "Reload log" was clicked). An anonymous delegate with no
            // parameter list is C# 5-legal and binds to UnityAction<string>.
            _activeLogFilter.onValueChanged.AddListener(delegate { RefreshLog(); });
            // v6.1.1: controls row moved to the TOP (just under the tabs) so the
            // log text below can never overflow over them (was the overlap bug).
            WindowManager.AddElementToElement(_activeLogFilter.gameObject, mainPanel.gameObject,
                new Rect(10, 66, 200, 28), new Rect(0, 0, 0, 0));

            // Refresh button
            Button refreshBtn = WindowManager.SpawnButton();
            refreshBtn.GetComponentInChildren<Text>().text = "Reload log";
            refreshBtn.onClick.AddListener(new UnityEngine.Events.UnityAction(RefreshLog));
            WindowManager.AddElementToElement(refreshBtn.gameObject, mainPanel.gameObject,
                new Rect(218, 66, 80, 26), new Rect(0, 0, 0, 0));

            // "Open log folder" button — opens Explorer
            Button openFolderBtn = WindowManager.SpawnButton();
            openFolderBtn.GetComponentInChildren<Text>().text = "Open folder";
            openFolderBtn.onClick.AddListener(new UnityEngine.Events.UnityAction(OpenLogFolder));
            WindowManager.AddElementToElement(openFolderBtn.gameObject, mainPanel.gameObject,
                new Rect(303, 66, 95, 26), new Rect(0, 0, 0, 0));

            // Available log dates dropdown
            var dates = AuditLog.GetAvailableLogDates();
            string[] dateOptions = new string[Math.Max(dates.Length, 1)];
            if (dates.Length == 0)
            {
                dateOptions[0] = "(no log files yet)";
            }
            else
            {
                for (int i = 0; i < dates.Length; i++)
                {
                    dateOptions[i] = dates[i].ToString("yyyy-MM-dd");
                }
            }
            GUICombobox dateCombo = WindowManager.SpawnComboBox();
            dateCombo.UpdateContent(dateOptions);
            WindowManager.AddElementToElement(dateCombo.gameObject, mainPanel.gameObject,
                new Rect(403, 66, 87, 26), new Rect(0, 0, 0, 0));

            // Log text area (a single wrapped label; the WindowManager API has
            // no true multi-line text view). v6.1.2: anchored top-left with a
            // top-left PIVOT and its height set to the wrapped text's
            // preferredHeight in RefreshLog(). That makes the label's
            // RectTransform actually as tall as the text, so the game's
            // content-size measurement includes the whole log and the native
            // Options>Mods scroll covers it (previously verticalOverflow drew the
            // text past a fixed 292px rect that the game never measured, so
            // everything below ~400px was clipped).
            _activeLogText = WindowManager.SpawnLabel();
            _activeLogText.fontSize = 11;
            _activeLogText.alignment = TextAnchor.UpperLeft;
            _activeLogText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _activeLogText.verticalOverflow = VerticalWrapMode.Overflow;
            _activeLogText.color = new Color(0.1f, 0.1f, 0.1f);
            _activeLogGameObject = _activeLogText.gameObject;
            RectTransform logRt = _activeLogText.rectTransform;
            logRt.SetParent(mainPanel, false);
            logRt.anchorMin = new Vector2(0f, 1f);
            logRt.anchorMax = new Vector2(0f, 1f);
            logRt.pivot = new Vector2(0f, 1f);
            logRt.sizeDelta = new Vector2(470f, 100f);
            logRt.anchoredPosition = new Vector2(10f, -100f);

            RefreshLog();
        }

        // v6.1.2: tab switching via CanvasGroup alpha (NOT SetActive) so both
        // containers stay active for the game's content-size measurement while
        // only one is visible. alpha 0 also blocks its raycasts.
        private static void ShowModsTab()
        {
            SetTabVisible(_modsGroup, true);
            SetTabVisible(_auditGroup, false);
        }

        private static void ShowAuditTab()
        {
            SetTabVisible(_modsGroup, false);
            SetTabVisible(_auditGroup, true);
            RefreshLog();
        }

        private static void SetTabVisible(CanvasGroup group, bool visible)
        {
            if (group == null) return;
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }

        private static void RefreshAll()
        {
            // Re-build from scratch. The shim mod's window is rebuilt by the
            // game's mod loader on tab change, so this is mostly a placeholder
            // for future "live refresh" use.
            UnityEngine.Debug.Log(Tag + " RefreshAll called. Mods=" + ModRegistry.Count + " todayLogLines=" + AuditLog.CountTodayLogLines());
        }

        private static void RefreshLog()
        {
            if (_activeLogText == null) return;
            string filter = _activeLogFilter != null ? _activeLogFilter.text : null;
            string[] lines = AuditLog.ReadTodayLog(1000);
            if (lines == null || lines.Length == 0)
            {
                _activeLogText.text = "(no audit log entries today. activate some mods and try again.)";
                _activeLogLineCount = 0;
                SizeLogToContent();
                return;
            }
            // Apply filter
            if (!string.IsNullOrEmpty(filter))
            {
                var filtered = new List<string>(lines.Length);
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i] != null && lines[i].IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        filtered.Add(lines[i]);
                    }
                }
                lines = filtered.ToArray();
            }
            // Join with newlines for the text label
            _activeLogText.text = string.Join("\n", lines);
            _activeLogLineCount = lines.Length;
            SizeLogToContent();
        }

        // v6.1.2: resize the log label's RectTransform to match the wrapped
        // text height so the whole log is measured (and scrollable) rather than
        // overflowing past a fixed rect that the game clips.
        private static void SizeLogToContent()
        {
            if (_activeLogText == null) return;
            RectTransform rt = _activeLogText.rectTransform;
            float ph = _activeLogText.preferredHeight;
            if (ph < 20f) ph = 20f;
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, ph);
        }

        // v6.1.2: add a WRAPPED, self-sizing text row to a tab container. The
        // label is anchored top-left with a top-left pivot and its height set to
        // the wrapped preferredHeight, so long text (e.g. permission lists) wraps
        // and the game's content-size measurement counts the real height instead
        // of clipping. Returns the y just below this row (for stacking).
        private static float AddWrappedRow(RectTransform container, string text, int fontSize, Color color, float left, float top, float width)
        {
            Text lbl = WindowManager.SpawnLabel();
            lbl.text = text;
            lbl.fontSize = fontSize;
            lbl.color = color;
            lbl.alignment = TextAnchor.UpperLeft;
            lbl.horizontalOverflow = HorizontalWrapMode.Wrap;
            lbl.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform rt = lbl.rectTransform;
            rt.SetParent(container, false);
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(width, 20f);
            rt.anchoredPosition = new Vector2(left, -top);
            float ph = lbl.preferredHeight;
            if (ph < fontSize + 4) ph = fontSize + 4;
            rt.sizeDelta = new Vector2(width, ph);
            return top + ph + 4f;
        }

        private static void OpenLogFolder()
        {
            try
            {
                var folder = AuditLog.GetLogFolder();
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                // Process.Start with UseShellExecute=true opens Explorer
                var psi = new ProcessStartInfo
                {
                    FileName = folder,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning(Tag + " OpenLogFolder failed: " + ex.Message);
            }
        }

        private static int CountAuditLinesFor(string modId)
        {
            // Cheap estimate: scan today's log for lines containing the modId.
            // Could be optimized with a per-mod index later, but for v6.1 the
            // 1000-line cap is enough.
            try
            {
                var lines = AuditLog.ReadTodayLog(1000);
                if (lines == null) return 0;
                int n = 0;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i] != null && lines[i].IndexOf(modId, StringComparison.OrdinalIgnoreCase) >= 0) n++;
                }
                return n;
            }
            catch { return 0; }
        }

        /// <summary>Close the main settings window. Called when the user closes the game's mod settings panel.</summary>
        public static void CloseMainWindow()
        {
            if (_activeMainWindow != null)
            {
                try { UnityEngine.Object.Destroy(_activeMainWindow.gameObject); } catch { }
                _activeMainWindow = null;
            }
            _activeLogText = null;
            _activeLogGameObject = null;
            _activeLogFilter = null;
            _modsTab = null;
            _auditTab = null;
            _modsGroup = null;
            _auditGroup = null;
            _contentRoot = null;
            _cachedMods = new ModIdentity[0];
        }

        /// <summary>Open the permissions sub-window for a single mod. Currently a no-op stub; the permissions
        /// are already shown inline in the "Mods" tab. This is here for future use.</summary>
        public static void OpenPermissionsWindow(RectTransform parent, string modId)
        {
            // TODO: full permissions sub-window with all 22 flags.
            // For v6.1 the inline display in the Mods tab is sufficient.
            UnityEngine.Debug.Log(Tag + " OpenPermissionsWindow called for modId=" + modId + " (stub — see inline display in Mods tab).");
        }
    }
}

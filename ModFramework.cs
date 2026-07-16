using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// ModFramework - Reusable utilities for Software Inc modding
/// Created by: Zicarius
/// Version: 1.0
/// 
/// Copy this file to any mod project for quick access to:
/// - UI creation helpers
/// - Logging system
/// - Window management
/// - Common utilities
/// </summary>
namespace ModFramework
{
    #region UI Utilities
    
    /// <summary>
    /// Helper methods for creating UI elements easily
    /// </summary>
    public static class UIHelper
    {
        // ========== PANELS ==========
        
        public static RectTransform AddPanel(Rect rect, GUIWindow window)
        {
            RectTransform panel = WindowManager.SpawnPanel();
            WindowManager.AddElementToWindow(panel.gameObject, window, rect, new Rect(0, 0, 0, 0));
            return panel;
        }

        public static RectTransform AddPanel(Rect rect, GameObject parent)
        {
            RectTransform panel = WindowManager.SpawnPanel();
            WindowManager.AddElementToElement(panel.gameObject, parent, rect, new Rect(0, 0, 0, 0));
            return panel;
        }

        // ========== BUTTONS ==========
        
        public static Button AddButton(string text, Rect rect, UnityAction action, GUIWindow window)
        {
            return AddButton(text, rect, action, window.MainPanel);
        }

        public static Button AddButton(string text, Rect rect, UnityAction action, GameObject panel)
        {
            Button btn = WindowManager.SpawnButton();
            btn.GetComponentInChildren<Text>().text = text;
            btn.onClick.AddListener(action);
            WindowManager.AddElementToElement(btn.gameObject, panel, rect, new Rect(0, 0, 0, 0));
            return btn;
        }

        // ========== LABELS ==========
        
        public static Text AddLabel(string text, Rect rect, GUIWindow window)
        {
            return AddLabel(text, rect, window.MainPanel);
        }

        public static Text AddLabel(string text, Rect rect, GameObject panel)
        {
            return AddLabel(text, rect, panel, 14);
        }

        public static Text AddLabel(string text, Rect rect, GameObject panel, int fontSize)
        {
            return AddLabel(text, rect, panel, fontSize, Color.black);
        }

        public static Text AddLabel(string text, Rect rect, GameObject panel, int fontSize, Color color)
        {
            Text label = WindowManager.SpawnLabel();
            label.text = text;
            label.color = color;
            label.fontSize = fontSize;
            WindowManager.AddElementToElement(label.gameObject, panel, rect, new Rect(0, 0, 0, 0));
            return label;
        }

        public static Text AddLabelBold(string text, Rect rect, GameObject panel)
        {
            return AddLabelBold(text, rect, panel, 14);
        }

        public static Text AddLabelBold(string text, Rect rect, GameObject panel, int fontSize)
        {
            Text label = WindowManager.SpawnLabel();
            label.text = text;
            label.color = new Color(0.1f, 0.1f, 0.1f);
            label.fontSize = fontSize;
            label.fontStyle = FontStyle.Bold;
            WindowManager.AddElementToElement(label.gameObject, panel, rect, new Rect(0, 0, 0, 0));
            return label;
        }

        public static Text AddSectionHeader(string text, Rect rect, GameObject panel)
        {
            Text header = WindowManager.SpawnLabel();
            header.text = text;
            header.color = new Color(0.2f, 0.4f, 0.2f);
            header.fontSize = 16;
            header.fontStyle = FontStyle.Bold;
            WindowManager.AddElementToElement(header.gameObject, panel, rect, new Rect(0, 0, 0, 0));
            return header;
        }

        // ========== INPUT FIELDS ==========
        
        public static InputField AddInputField(string text, Rect rect, UnityAction<string> onValueChanged, GUIWindow window)
        {
            return AddInputField(text, rect, onValueChanged, window.MainPanel);
        }

        public static InputField AddInputField(string text, Rect rect, UnityAction<string> onValueChanged, GameObject panel)
        {
            InputField input = WindowManager.SpawnInputbox();
            input.text = text;
            input.onValueChanged.AddListener(onValueChanged);
            WindowManager.AddElementToElement(input.gameObject, panel, rect, new Rect(0, 0, 0, 0));
            return input;
        }

        public static InputField AddIntField(int value, Rect rect, UnityAction<int> onValueChanged, GameObject panel)
        {
            InputField input = WindowManager.SpawnInputbox();
            input.text = value.ToString();
            input.contentType = InputField.ContentType.IntegerNumber;
            input.onValueChanged.AddListener(val => onValueChanged.Invoke(int.Parse(val)));
            WindowManager.AddElementToElement(input.gameObject, panel, rect, new Rect(0, 0, 0, 0));
            return input;
        }

        // ========== TOGGLES ==========
        
        public static Toggle AddToggle(string text, Rect rect, bool isOn, UnityAction<bool> onValueChanged, GUIWindow window)
        {
            return AddToggle(text, rect, isOn, onValueChanged, window.MainPanel);
        }

        public static Toggle AddToggle(string text, Rect rect, bool isOn, UnityAction<bool> onValueChanged, GameObject panel)
        {
            Toggle toggle = WindowManager.SpawnCheckbox();
            Text label = toggle.GetComponentInChildren<Text>();
            label.text = text;
            toggle.isOn = isOn;
            toggle.onValueChanged.AddListener(onValueChanged);
            WindowManager.AddElementToElement(toggle.gameObject, panel, rect, new Rect(0, 0, 0, 0));
            return toggle;
        }

        // ========== SCROLL VIEWS ==========
        
        /// <summary>
        /// Creates a scrollable area for content that exceeds the visible space
        /// </summary>
        /// <param name="rect">Position and size of the scroll view</param>
        /// <param name="parent">Parent panel or window</param>
        /// <param name="contentHeight">Total height of the content (if known, otherwise 1000)</param>
        /// <returns>The content panel where you should add scrollable elements</returns>
        public static GameObject AddScrollView(Rect rect, GameObject parent, float contentHeight = 1000f)
        {
            // Create scroll view container
            GameObject scrollViewObj = new GameObject("ScrollView");
            RectTransform scrollRect = scrollViewObj.AddComponent<RectTransform>();
            scrollViewObj.AddComponent<Image>().color = Color.clear; // FULLY TRANSPARENT - don't cover content!
            WindowManager.AddElementToElement(scrollViewObj, parent, rect, new Rect(0, 0, 0, 0));
            
            // Create viewport (masks content)
            GameObject viewportObj = new GameObject("Viewport");
            RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
            viewportObj.AddComponent<Image>().color = Color.clear;
            viewportObj.AddComponent<Mask>().showMaskGraphic = false;
            viewportRect.SetParent(scrollRect);
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.anchoredPosition = Vector2.zero;
            
            // Create content panel (this is where you add your actual content)
            GameObject contentObj = new GameObject("Content");
            RectTransform contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.SetParent(viewportRect);
            contentRect.anchorMin = new Vector2(0, 1);  // Top-left anchor
            contentRect.anchorMax = new Vector2(1, 1);  // Top-right anchor
            contentRect.pivot = new Vector2(0, 1);      // Pivot at top-left (NOT center!)
            contentRect.sizeDelta = new Vector2(0, contentHeight);
            contentRect.anchoredPosition = new Vector2(0, 0);  // Position at top-left
            
            // Add ScrollRect component
            ScrollRect scrollComponent = scrollViewObj.AddComponent<ScrollRect>();
            scrollComponent.content = contentRect;
            scrollComponent.viewport = viewportRect;
            scrollComponent.horizontal = false;
            scrollComponent.vertical = true;
            scrollComponent.movementType = ScrollRect.MovementType.Clamped;
            scrollComponent.inertia = true;
            scrollComponent.scrollSensitivity = 20f;
            
            // Create vertical scrollbar
            GameObject scrollbarObj = new GameObject("Scrollbar");
            RectTransform scrollbarRect = scrollbarObj.AddComponent<RectTransform>();
            WindowManager.AddElementToElement(scrollbarObj, scrollViewObj, 
                new Rect(rect.width - 15, 0, 15, rect.height), new Rect(0, 0, 0, 0));
            
            Scrollbar scrollbar = scrollbarObj.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            
            // Scrollbar handle
            GameObject handleObj = new GameObject("Handle");
            RectTransform handleRect = handleObj.AddComponent<RectTransform>();
            handleObj.AddComponent<Image>().color = new Color(0.5f, 0.5f, 0.5f, 0.8f);
            handleRect.SetParent(scrollbarRect);
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.sizeDelta = Vector2.zero;
            
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleObj.GetComponent<Image>();
            
            // Link scrollbar to scroll view
            scrollComponent.verticalScrollbar = scrollbar;
            scrollComponent.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            
            return contentObj;
        }
        
        /// <summary>
        /// Updates the content height of an existing scroll view
        /// Useful when dynamically adding/removing content
        /// </summary>
        public static void UpdateScrollViewContentHeight(GameObject contentPanel, float newHeight)
        {
            if (contentPanel != null)
            {
                RectTransform contentRect = contentPanel.GetComponent<RectTransform>();
                if (contentRect != null)
                {
                    contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, newHeight);
                }
            }
        }

        // ========== SLIDERS ==========

        /// <summary>
        /// Adds a slider to a GUIWindow.
        /// ⚠️ CRITICAL: Parameter order is (minValue, maxValue, value) - NOT (value, minValue, maxValue)!
        /// Wrong order will cause binary slider behavior (only min/max values accessible).
        /// </summary>
        /// <param name="minValue">Minimum slider value (e.g., 1 for cores)</param>
        /// <param name="maxValue">Maximum slider value (e.g., 64 for cores)</param>
        /// <param name="value">Initial/current slider value (e.g., 32 for current cores)</param>
        /// <param name="rect">Position and size of the slider</param>
        /// <param name="onValueChanged">Callback when slider value changes</param>
        /// <param name="window">Target GUIWindow</param>
        /// <returns>The created Slider component</returns>
        public static Slider AddSlider(float minValue, float maxValue, float value, Rect rect, UnityAction<float> onValueChanged, GUIWindow window)
        {
            return AddSlider(minValue, maxValue, value, rect, onValueChanged, window.MainPanel);
        }

        /// <summary>
        /// Adds a slider to a GameObject panel.
        /// ⚠️ CRITICAL: Parameter order is (minValue, maxValue, value) - NOT (value, minValue, maxValue)!
        /// </summary>
        public static Slider AddSlider(float minValue, float maxValue, float value, Rect rect, UnityAction<float> onValueChanged, GameObject panel)
        {
            GameObject sliderObj = new GameObject("Slider");
            sliderObj.AddComponent<RectTransform>();
            
            Slider slider = sliderObj.AddComponent<Slider>();
            slider.minValue = minValue;
            slider.maxValue = maxValue;
            slider.value = value;
            slider.onValueChanged.AddListener(onValueChanged);
            
            // Create background
            GameObject background = new GameObject("Background");
            background.transform.SetParent(sliderObj.transform);
            RectTransform bgRect = background.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0.25f);
            bgRect.anchorMax = new Vector2(1, 0.75f);
            bgRect.sizeDelta = Vector2.zero;
            Image bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            
            // Create fill area
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1, 0.75f);
            fillAreaRect.sizeDelta = new Vector2(-10, 0);
            
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform);
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            fillRect.sizeDelta = Vector2.zero;
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.2f, 0.6f, 1f, 1f);
            
            slider.fillRect = fillRect;
            
            // Create handle
            GameObject handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(sliderObj.transform);
            RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
            handleAreaRect.sizeDelta = new Vector2(-10, 0);
            handleAreaRect.anchorMin = new Vector2(0, 0);
            handleAreaRect.anchorMax = new Vector2(1, 1);
            
            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform);
            RectTransform handleRect = handle.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(10, 10);
            Image handleImage = handle.AddComponent<Image>();
            handleImage.color = new Color(1f, 1f, 1f, 1f);
            
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            
            WindowManager.AddElementToElement(sliderObj, panel, rect, new Rect(0, 0, 0, 0));
            return slider;
        }

        // ========== COMBOBOXES (DROPDOWNS) ==========

        /// <summary>
        /// Add a combobox/dropdown menu with string options (uses game's native GUICombobox)
        /// </summary>
        /// <param name="options">Array of option strings to display</param>
        /// <param name="selectedIndex">Initially selected index (default: 0)</param>
        /// <param name="rect">Position and size</param>
        /// <param name="onSelectedChanged">Callback when selection changes</param>
        /// <param name="window">Parent window</param>
        public static GUICombobox AddCombobox(string[] options, int selectedIndex, Rect rect, UnityAction onSelectedChanged, GUIWindow window)
        {
            return AddCombobox(options, selectedIndex, rect, onSelectedChanged, window.MainPanel);
        }

        /// <summary>
        /// Add a combobox/dropdown menu with string options (uses game's native GUICombobox)
        /// </summary>
        /// <param name="options">Array of option strings to display</param>
        /// <param name="selectedIndex">Initially selected index (default: 0)</param>
        /// <param name="rect">Position and size</param>
        /// <param name="onSelectedChanged">Callback when selection changes</param>
        /// <param name="panel">Parent panel</param>
        public static GUICombobox AddCombobox(string[] options, int selectedIndex, Rect rect, UnityAction onSelectedChanged, GameObject panel)
        {
            // Use game's native combobox spawner
            GUICombobox comboBox = WindowManager.SpawnComboBox();
            
            // Add options as objects (GUICombobox uses List<object>)
            comboBox.Items.Clear();
            foreach (string option in options)
            {
                comboBox.Items.Add(option);
            }
            
            // Set initial selection
            if (selectedIndex >= 0 && selectedIndex < options.Length)
            {
                comboBox.Selected = selectedIndex;
            }
            
            // Add listener
            if (onSelectedChanged != null)
            {
                comboBox.OnSelectedChanged.AddListener(onSelectedChanged);
            }
            
            // Add to parent
            WindowManager.AddElementToElement(comboBox.gameObject, panel, rect, new Rect(0, 0, 0, 0));
            
            return comboBox;
        }

        // ========== WINDOWS ==========
        
        public static GUIWindow CreateWindow(string title)
        {
            return CreateWindow(title, 800, 500);
        }

        public static GUIWindow CreateWindow(string title, float width, float height)
        {
            GUIWindow window = WindowManager.SpawnWindow();
            window.InitialTitle = window.TitleText.text = window.NonLocTitle = title;
            window.MinSize.x = width;
            window.MinSize.y = height;
            return window;
        }
    }

    #endregion

    #region Logger System

    /// <summary>
    /// Enhanced logging system with filtering and formatting
    /// </summary>
    public static class ModLogger
    {
        private static string modPrefix = "[Mod]";
        
        public static void SetPrefix(string prefix)
        {
            modPrefix = "[" + prefix + "]";
        }

        public static void Log(string message)
        {
            Debug.Log(string.Format("{0} {1}", modPrefix, message));
        }

        public static void LogWarning(string message)
        {
            Debug.LogWarning(string.Format("{0} {1}", modPrefix, message));
        }

        public static void LogError(string message)
        {
            Debug.LogError(string.Format("{0} {1}", modPrefix, message));
        }

        public static void LogSuccess(string message)
        {
            Debug.Log(string.Format("{0} ✓ {1}", modPrefix, message));
        }

        public static void LogSeparator()
        {
            Debug.Log(modPrefix + " ========================");
        }

        public static void LogSection(string sectionName)
        {
            Debug.Log(string.Format("{0} ===== {1} =====", modPrefix, sectionName));
        }
    }

    #endregion

    #region Event System

    /// <summary>
    /// Simple event system for mod communication
    /// </summary>
    public static class ModEvents
    {
        private static Dictionary<string, List<Action<object>>> events = new Dictionary<string, List<Action<object>>>();

        public static void Subscribe(string eventName, Action<object> callback)
        {
            if (!events.ContainsKey(eventName))
            {
                events[eventName] = new List<Action<object>>();
            }
            events[eventName].Add(callback);
        }

        public static void Unsubscribe(string eventName, Action<object> callback)
        {
            if (events.ContainsKey(eventName))
            {
                events[eventName].Remove(callback);
            }
        }

        public static void Trigger(string eventName, object data = null)
        {
            if (events.ContainsKey(eventName))
            {
                foreach (var callback in events[eventName])
                {
                    try
                    {
                        callback.Invoke(data);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("ModEvents error: " + e.Message);
                    }
                }
            }
        }

        public static void Clear()
        {
            events.Clear();
        }
    }

    #endregion

    #region Notification Helper

    /// <summary>
    /// Easy in-game notification system
    /// </summary>
    public static class Notifications
    {
        public static void Show(string message)
        {
            Show(message, "Info", 2f);
        }

        public static void Show(string message, string icon, float duration)
        {
            if (HUD.Instance != null)
            {
                HUD.Instance.AddPopupMessage(
                    message,
                    icon,
                    PopupManager.PopUpAction.None,
                    0,
                    PopupManager.NotificationSound.Neutral,
                    duration
                );
            }
        }

        public static void ShowSuccess(string message)
        {
            Show(message, "Cogs", 2f);
        }

        public static void ShowWarning(string message)
        {
            if (HUD.Instance != null)
            {
                HUD.Instance.AddPopupMessage(
                    message,
                    "Warning",
                    PopupManager.PopUpAction.None,
                    0,
                    PopupManager.NotificationSound.Warning,
                    3f
                );
            }
        }

        public static void ShowError(string message)
        {
            if (HUD.Instance != null)
            {
                HUD.Instance.AddPopupMessage(
                    message,
                    "Exclamation",
                    PopupManager.PopUpAction.None,
                    0,
                    PopupManager.NotificationSound.Issue,
                    4f
                );
            }
        }
    }

    #endregion

    #region Settings Persistence (disk-backed)

    /// <summary>
    /// Simple persistent mod settings store.
    ///
    /// 2026-03: Software Inc security update blocks mods that reference UnityEngine's built-in prefs API.
    /// This implementation persists settings to disk under Application.persistentDataPath instead.
    /// </summary>
    public static class ModSettings
    {
        private static readonly object _gate = new object();
        private static string _prefix = "Mod";
        private static Dictionary<string, string> _values;
        private static bool _loaded;

        public static void SetPrefix(string modName)
        {
            lock (_gate)
            {
                _prefix = string.IsNullOrWhiteSpace(modName) ? "Mod" : modName.Trim();
                _loaded = false;
                _values = null;
            }
        }

        public static void SetBool(string key, bool value) => SetString(key, value ? "1" : "0");
        public static bool GetBool(string key) => GetBool(key, false);
        public static bool GetBool(string key, bool defaultValue)
        {
            var s = GetString(key, null);
            if (string.IsNullOrEmpty(s)) return defaultValue;
            if (s == "1") return true;
            if (s == "0") return false;
            bool b;
            return bool.TryParse(s, out b) ? b : defaultValue;
        }

        public static void SetInt(string key, int value) => SetString(key, value.ToString(CultureInfo.InvariantCulture));
        public static int GetInt(string key) => GetInt(key, 0);
        public static int GetInt(string key, int defaultValue)
        {
            var s = GetString(key, null);
            int v;
            return (s != null && int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out v)) ? v : defaultValue;
        }

        public static void SetFloat(string key, float value) => SetString(key, value.ToString(CultureInfo.InvariantCulture));
        public static float GetFloat(string key) => GetFloat(key, 0f);
        public static float GetFloat(string key, float defaultValue)
        {
            var s = GetString(key, null);
            float v;
            return (s != null && float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v)) ? v : defaultValue;
        }

        public static void SetString(string key, string value)
        {
            if (string.IsNullOrEmpty(key)) return;
            lock (_gate)
            {
                EnsureLoaded();
                _values[KeyWithPrefix(key)] = value ?? "";
                Persist();
            }
        }

        public static string GetString(string key) => GetString(key, "");
        public static string GetString(string key, string defaultValue)
        {
            if (string.IsNullOrEmpty(key)) return defaultValue;
            lock (_gate)
            {
                EnsureLoaded();
                string v;
                return _values.TryGetValue(KeyWithPrefix(key), out v) ? v : defaultValue;
            }
        }

        public static void DeleteAll()
        {
            lock (_gate)
            {
                _values = new Dictionary<string, string>(StringComparer.Ordinal);
                _loaded = true;
                TryDeleteFile();
            }
        }

        private static string KeyWithPrefix(string key) => _prefix + "_" + key;

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;
            _values = new Dictionary<string, string>(StringComparer.Ordinal);

            try
            {
                var path = GetFilePath();
                if (!File.Exists(path)) return;

                var lines = File.ReadAllLines(path, Encoding.UTF8);
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    int tab = line.IndexOf('\t');
                    if (tab <= 0) continue;

                    var k = line.Substring(0, tab);
                    var v64 = line.Substring(tab + 1);
                    if (string.IsNullOrEmpty(k)) continue;

                    string v;
                    try
                    {
                        var bytes = Convert.FromBase64String(v64);
                        v = Encoding.UTF8.GetString(bytes);
                    }
                    catch
                    {
                        v = "";
                    }

                    _values[k] = v;
                }
            }
            catch
            {
                // If settings can't be read, fall back to defaults.
                _values = new Dictionary<string, string>(StringComparer.Ordinal);
            }
        }

        private static void Persist()
        {
            try
            {
                var path = GetFilePath();
                var dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                var tmp = path + ".tmp";
                using (var sw = new StreamWriter(tmp, false, Encoding.UTF8))
                {
                    foreach (var kv in _values)
                    {
                        var v64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(kv.Value ?? ""));
                        sw.Write(kv.Key);
                        sw.Write('\t');
                        sw.WriteLine(v64);
                    }
                }

                if (File.Exists(path)) File.Delete(path);
                File.Move(tmp, path);
            }
            catch
            {
                // Ignore persistence failures; settings will behave like defaults.
            }
        }

        private static void TryDeleteFile()
        {
            try
            {
                var path = GetFilePath();
                if (File.Exists(path)) File.Delete(path);
            }
            catch
            {
                // ignore
            }
        }

        private static string GetFilePath()
        {
            var safe = SanitizeFileName(_prefix);
            return Path.Combine(Application.persistentDataPath, "ModSettings", safe + ".txt");
        }

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Mod";
            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(name.Length);
            for (int i = 0; i < name.Length; i++)
            {
                var c = name[i];
                bool bad = false;
                for (int j = 0; j < invalid.Length; j++)
                {
                    if (c == invalid[j]) { bad = true; break; }
                }
                sb.Append(bad ? '_' : c);
            }
            return sb.ToString();
        }
    }

    #endregion

    #region Utility Functions

    /// <summary>
    /// Common utility functions
    /// </summary>
    public static class ModUtils
    {
        public static string FormatCurrency(float amount)
        {
            return "$" + amount.ToString("N0");
        }

        public static string FormatNumber(float number)
        {
            return FormatNumber(number, 0);
        }

        public static string FormatNumber(float number, int decimals)
        {
            return number.ToString("N" + decimals);
        }

        public static string FormatPercent(float value)
        {
            return (value * 100f).ToString("F1") + "%";
        }

        public static string FormatTime(float seconds)
        {
            int hours = (int)(seconds / 3600f);
            int minutes = (int)((seconds % 3600f) / 60f);
            int secs = (int)(seconds % 60f);
            
            if (hours > 0)
                return string.Format("{0}h {1}m {2}s", hours, minutes, secs);
            if (minutes > 0)
                return string.Format("{0}m {1}s", minutes, secs);
            return string.Format("{0}s", secs);
        }

        public static T GetSingleton<T>() where T : MonoBehaviour
        {
            return UnityEngine.Object.FindObjectOfType<T>();
        }

        public static bool IsInGame()
        {
            return GameSettings.Instance != null && HUD.Instance != null;
        }

        public static string TruncateString(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
                return text;
            return text.Substring(0, maxLength - 3) + "...";
        }
    }

    #endregion
}


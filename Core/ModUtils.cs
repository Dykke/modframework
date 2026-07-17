// ModUtils.cs
// ModFramework v6.0
//
// Common utility functions for Software Inc modders.
//
// v6.0 changes:
//   - Class is marked with [ModFrameworkPublicAPI("v6.0")] — it's part of the
//     curated v6.0 public API surface.
//   - No permission check is needed (this is a pure formatting/utility class;
//     no privileged operations).
//   - The v4.x [Obsolete] ModUtils class (in ModFramework.cs, in the
//     ModFramework namespace) is kept for back-compat. New code should use
//     this ModFramework.Core.ModUtils instead.

using System;
using UnityEngine;

/// <summary>
/// Common utility functions for Software Inc modders.
/// </summary>
namespace ModFramework.Core
{
    /// <summary>
    /// v6.0 utility class. Replaces the v4.x [Obsolete] ModUtils in the
    /// ModFramework namespace. Use this for all new code.
    /// </summary>
    [ModFrameworkPublicAPI("v6.0", Reason = "Common utility functions")]
    public static class ModUtils
    {
        /// <summary>Format a number as a currency string (e.g. $1,234,567).</summary>
        public static string FormatCurrency(float amount)
        {
            return "$" + amount.ToString("N0");
        }

        /// <summary>Format a number with the default decimal count (0).</summary>
        public static string FormatNumber(float number)
        {
            return FormatNumber(number, 0);
        }

        /// <summary>Format a number with a custom decimal count.</summary>
        public static string FormatNumber(float number, int decimals)
        {
            return number.ToString("N" + decimals);
        }

        /// <summary>Format a fractional value as a percentage string (e.g. 0.5 -> "50.0%").</summary>
        public static string FormatPercent(float value)
        {
            return (value * 100f).ToString("F1") + "%";
        }

        /// <summary>Format a number of seconds as an h/m/s string.</summary>
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

        /// <summary>
        /// Format a money value into a readable string with abbreviations.
        /// Examples: $1,500 -> "$1.5K", $2,300,000 -> "$2.3M", $5,000,000,000 -> "$5.0B"
        /// </summary>
        public static string FormatMoney(double amount)
        {
            try
            {
                if (amount >= 1000000000) return "$" + (amount / 1000000000).ToString("F1") + "B";
                if (amount >= 1000000) return "$" + (amount / 1000000).ToString("F1") + "M";
                if (amount >= 1000) return "$" + (amount / 1000).ToString("F1") + "K";
                return "$" + amount.ToString("F0");
            }
            catch
            {
                return "$0";
            }
        }

        /// <summary>
        /// Format a money value into a readable string with abbreviations.
        /// Overload that accepts float for convenience.
        /// </summary>
        public static string FormatMoney(float amount)
        {
            return FormatMoney((double)amount);
        }

        /// <summary>
        /// Find the first active singleton instance of type T in the scene.
        /// </summary>
        public static T GetSingleton<T>() where T : MonoBehaviour
        {
            return UnityEngine.Object.FindObjectOfType<T>();
        }

        /// <summary>True if the game has a loaded save (GameSettings + HUD are both alive).</summary>
        public static bool IsInGame()
        {
            return GameSettings.Instance != null && HUD.Instance != null;
        }

        /// <summary>Truncate a string to at most maxLength characters, appending "..." if truncated.</summary>
        public static string TruncateString(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
                return text;
            return text.Substring(0, maxLength - 3) + "...";
        }
    }
}

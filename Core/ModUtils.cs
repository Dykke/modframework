using System;
using UnityEngine;

/// <summary>
/// Common utility functions
/// </summary>
namespace ModFramework.Core
{
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
}

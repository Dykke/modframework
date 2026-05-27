using System;
using UnityEngine;

namespace ModFramework.Core
{
    /// <summary>
    /// A robust utility for running mod code safely.
    /// Wraps actions in try/catch blocks that beautifully log errors directly 
    /// to the game console without causing game-breaking chain reactions.
    ///
    /// The number one rule of modding Software Inc: Don't break the original game's loops.
    /// Wrap your UI callbacks, Update logic, and risky API calls in ModSafety.Try().
    /// </summary>
    public static class ModSafety
    {
        /// <summary>
        /// Try to execute an action. If it throws an exception, it is caught and
        /// logged safely.
        /// </summary>
        /// <param name="action">The code to execute</param>
        /// <param name="context">A short string describing what was happening (e.g. "MyButton_Click")</param>
        /// <returns>True if the code ran successfully, false if an error occurred.</returns>
        public static bool Try(Action action, string context = "Unknown Operation")
        {
            if (action == null) return false;

            try
            {
                action();
                return true;
            }
            catch (Exception ex)
            {
                LogError(context, ex);
                return false;
            }
        }

        /// <summary>
        /// Try to execute a function that returns a value. If it fails, returns a default value.
        /// </summary>
        /// <typeparam name="T">The return type</typeparam>
        /// <param name="func">The function to execute</param>
        /// <param name="fallback">The value to return if the function crashes</param>
        /// <param name="context">A short string describing what was happening</param>
        /// <returns>The function result, or the fallback value on error</returns>
        public static T TryGet<T>(Func<T> func, T fallback = default(T), string context = "Unknown Operation")
        {
            if (func == null) return fallback;

            try
            {
                return func();
            }
            catch (Exception ex)
            {
                LogError(context, ex);
                return fallback;
            }
        }

        /// <summary>
        /// Wraps an action so it only logs errors once instead of spamming every frame.
        /// Perfect for code that runs in Update() loops.
        ///
        /// Example:
        ///   private Action _safeUpdate;
        ///   void Awake() {
        ///       _safeUpdate = ModSafety.ThrottledErrorHandler("MyMod Update", () => {
        ///           // risky per-frame logic here
        ///       });
        ///   }
        ///   void Update() { _safeUpdate(); }
        /// </summary>
        /// <param name="context">Describes what the action does (for logging)</param>
        /// <param name="action">The code to execute each frame</param>
        /// <returns>A wrapped Action that silences repeated errors after the first log</returns>
        public static Action ThrottledErrorHandler(string context, Action action)
        {
            bool hasErrored = false;
            return () =>
            {
                if (hasErrored) return;
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    hasErrored = true;
                    LogError(context + " (further errors suppressed)", ex);
                }
            };
        }

        /// <summary>
        /// Check a condition and log a warning if it fails.
        /// Does NOT throw an exception. Use this for quick sanity checks during development.
        ///
        /// Example:
        ///   ModSafety.Assert(company != null, "Expected the player's company to exist");
        /// </summary>
        /// <param name="condition">The condition to check. If false, a warning is logged.</param>
        /// <param name="message">The message to log if the assertion fails</param>
        public static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                string warning = $"[ModFramework] ASSERT FAILED: {message}";
                Debug.LogWarning(warning);
            }
        }

        private static void LogError(string context, Exception ex)
        {
            string errorMessage = $"[ModFramework] ERROR in {context}:\n{ex.Message}\n{ex.StackTrace}";
            Debug.LogError(errorMessage);
        }
    }
}

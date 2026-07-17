// AuditLog.cs
// ModFramework v6.0
//
// Append-only audit log of every privileged framework call. Written to
// %persistentDataPath%/ModFramework/audit-<yyyy-mm-dd>.log. Old logs are
// auto-deleted after 30 days (user decision 2026-07-16). Surfaces in-game
// in the "Mod Audit Log" window — searchable + filterable by modId.
//
// Closes: every AV's attribution gap. Even if a malicious mod slips through,
// the user can see exactly which mod wrote to which file / patched which
// method / triggered which event / called which service.
//
// Full implementation lands in Phase 1 (D2 = M-2). This file is the Phase 0
// skeleton — the Log() entry point + the file-path resolution + the day
// rotation. In-game view comes in Phase 2 (D6).

using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace ModFramework.Core
{
    /// <summary>
    /// Persistent audit log of every privileged framework call. Append-only
    /// per day. Auto-rolled at midnight. 30-day retention.
    /// </summary>
    [ModFrameworkPublicAPI("v6.0", Reason = "Visibility into mod behaviour")]
    public static class AuditLog
    {
        private const string Tag = "[ModFramework.AuditLog]";
        private const int RetentionDays = 30;
        private static readonly object _gate = new object();

        /// <summary>
        /// Append a line to today's audit log.
        /// Format: [timestamp] [modId] [displayName] [operation] [target] [result] [notes]
        /// Example: [2026-07-16 14:23:01] [2915401] [LimitlessTeams] FILE_WRITE [/path/to/data] OK [1234 bytes]
        /// </summary>
        public static void Log(string modId, string displayName, string operation, string target, string result, string notes)
        {
            if (string.IsNullOrEmpty(operation)) return;
            try
            {
                lock (_gate)
                {
                    var line = string.Format(CultureInfo.InvariantCulture,
                        "[{0:yyyy-MM-dd HH:mm:ss}] [{1}] [{2}] {3} {4} {5} {6}",
                        DateTime.Now,
                        modId ?? string.Empty,
                        displayName ?? string.Empty,
                        operation,
                        target ?? string.Empty,
                        result ?? "OK",
                        notes ?? string.Empty);

                    var path = GetLogPath(DateTime.Now);
                    var dir = Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

                    File.AppendAllText(path, line + Environment.NewLine, Encoding.UTF8);

                    // Also echo to Debug.Log so it shows up in output_log.txt.
                    Debug.Log(Tag + " " + line);
                }
            }
            catch (Exception ex)
            {
                // Never let an audit-log failure crash the game.
                Debug.LogWarning(Tag + " Failed to write log line: " + ex.Message);
            }
        }

        /// <summary>Get the file path for a given day's log.</summary>
        public static string GetLogPath(DateTime day)
        {
            var dir = Path.Combine(Application.persistentDataPath, "ModFramework");
            var fileName = "audit-" + day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ".log";
            return Path.Combine(dir, fileName);
        }

        /// <summary>Get the audit log folder (under %persistentDataPath%). Created on first Log() call.</summary>
        public static string GetLogFolder()
        {
            return Path.Combine(Application.persistentDataPath, "ModFramework");
        }

        /// <summary>
        /// Read the last N lines of today's audit log. Returns empty array
        /// if no log exists yet. Used by the in-game "Mod Audit Log" window.
        /// </summary>
        public static string[] ReadTodayLog(int maxLines = 1000)
        {
            return ReadLogFile(DateTime.Now, maxLines);
        }

        /// <summary>
        /// Read the last N lines of a specific day's audit log. Returns empty
        /// array if the log file doesn't exist. Used by the in-game window's
        /// date picker.
        /// </summary>
        public static string[] ReadLogFile(DateTime day, int maxLines = 1000)
        {
            try
            {
                var path = GetLogPath(day);
                if (!File.Exists(path)) return new string[0];
                var all = File.ReadAllLines(path, Encoding.UTF8);
                if (all.Length <= maxLines) return all;
                // Return the LAST maxLines entries.
                var start = all.Length - maxLines;
                var result = new string[maxLines];
                Array.Copy(all, start, result, 0, maxLines);
                return result;
            }
            catch (Exception ex)
            {
                Debug.LogWarning(Tag + " ReadLogFile failed: " + ex.Message);
                return new string[0];
            }
        }

        /// <summary>
        /// List all available audit log dates (as DateTime values at midnight),
        /// sorted newest first. Used by the in-game window's date picker.
        /// </summary>
        public static DateTime[] GetAvailableLogDates()
        {
            try
            {
                var dir = GetLogFolder();
                if (!Directory.Exists(dir)) return new DateTime[0];
                var files = Directory.GetFiles(dir, "audit-*.log");
                var dates = new System.Collections.Generic.List<DateTime>(files.Length);
                foreach (var file in files)
                {
                    try
                    {
                        // Filename format: audit-yyyy-MM-dd.log
                        var name = Path.GetFileNameWithoutExtension(file);
                        if (name == null) continue;
                        var datePart = name.Substring("audit-".Length);
                        DateTime parsed;
                        if (DateTime.TryParseExact(datePart, "yyyy-MM-dd",
                            CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
                        {
                            dates.Add(parsed);
                        }
                    }
                    catch { /* skip unparseable filenames */ }
                }
                dates.Sort((a, b) => b.CompareTo(a));
                return dates.ToArray();
            }
            catch (Exception ex)
            {
                Debug.LogWarning(Tag + " GetAvailableLogDates failed: " + ex.Message);
                return new DateTime[0];
            }
        }

        /// <summary>Get the total number of lines in today's log (or 0 if no log).</summary>
        public static int CountTodayLogLines()
        {
            try
            {
                var path = GetLogPath(DateTime.Now);
                if (!File.Exists(path)) return 0;
                return File.ReadAllLines(path, Encoding.UTF8).Length;
            }
            catch { return 0; }
        }

        /// <summary>
        /// Delete audit log files older than RetentionDays days. Called once
        /// at OnActivate. Phase 1 will surface this in the in-game view.
        /// </summary>
        public static void PurgeOldLogs()
        {
            try
            {
                var dir = Path.Combine(Application.persistentDataPath, "ModFramework");
                if (!Directory.Exists(dir)) return;
                var cutoff = DateTime.Now.AddDays(-RetentionDays);
                foreach (var file in Directory.GetFiles(dir, "audit-*.log"))
                {
                    try
                    {
                        var info = new FileInfo(file);
                        if (info.LastWriteTime < cutoff) info.Delete();
                    }
                    catch
                    {
                        // ignore individual file failures
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning(Tag + " PurgeOldLogs failed: " + ex.Message);
            }
        }
    }
}

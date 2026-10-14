using BepInEx.Logging;
using System;
using System.IO;

namespace GLMod.TestHarness
{
    /// <summary>
    /// Logs to both the BepInEx console/log and a per-instance file in the run output folder.
    /// </summary>
    public static class HarnessLog
    {
        private static ManualLogSource _logger;
        private static string _filePath;
        private static string _prefix;
        private static readonly object _lock = new object();

        public static void Init(ManualLogSource logger, HarnessContext context)
        {
            _logger = logger;
            _filePath = context?.LogPath;
            _prefix = context != null ? $"[GLModTest][{context.Instance}] " : "[GLModTest] ";
        }

        public static void Info(string message) => Write("INFO", message);

        public static void Warn(string message) => Write("WARN", message);

        public static void Error(string message) => Write("FAIL", message);

        private static void Write(string level, string message)
        {
            string line = $"{_prefix}{level} {message}";
            if (level == "INFO")
            {
                _logger?.LogInfo(line);
            }
            else
            {
                _logger?.LogWarning(line);
            }

            if (_filePath == null) return;
            try
            {
                lock (_lock)
                {
                    File.AppendAllText(_filePath, $"{DateTime.Now:HH:mm:ss.fff} {line}{Environment.NewLine}");
                }
            }
            catch (Exception e)
            {
                _logger?.LogWarning(_prefix + "Could not write log file: " + e.Message);
            }
        }
    }
}

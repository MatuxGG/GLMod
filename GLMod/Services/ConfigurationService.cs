using BepInEx.Logging;
using GLMod.Constants;
using System;
using System.IO;

namespace GLMod.Services
{
    /// <summary>
    /// Service responsible for managing mod configuration
    /// </summary>
    public class ConfigurationService : IConfigurationService
    {
        private readonly ManualLogSource _logger;
        private string _modName;

        public string ModName => _modName;
        public string ConfigPath { get; }

        public ConfigurationService(ManualLogSource logger, string configPath)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            ConfigPath = configPath ?? throw new ArgumentNullException(nameof(configPath));
            _modName = "Vanilla";
        }

        private void Log(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                string playerName = PlayerControl.LocalPlayer?.Data?.PlayerName;
                string prefix = playerName != null ? "[GLMod] " + playerName + ": " : "[GLMod] ";
                _logger.LogInfo(prefix + message);
            }
        }

        public void FindModName()
        {
            try
            {
                DirectoryInfo dir = new DirectoryInfo(ConfigPath);
                FileInfo[] files = dir.GetFiles();
                foreach (FileInfo file in files)
                {
                    if (file.Name.EndsWith(".glmod"))
                    {
                        _modName = Path.GetFileNameWithoutExtension(file.Name);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"[FindModName] Error finding mod name: {ex.Message}");
            }
        }

        public void SetModName(string modName)
        {
            if (!string.IsNullOrEmpty(modName))
            {
                _modName = modName;
            }
        }
    }
}

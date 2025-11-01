using AmongUs.GameOptions;
using BepInEx.Logging;
using GLMod.Enums;
using System;

namespace GLMod.Services
{
    /// <summary>
    /// Service responsible for map-related operations
    /// </summary>
    public class MapService : IMapService
    {
        private readonly ManualLogSource _logger;

        public MapService(ManualLogSource logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

        public string GetMapName()
        {
            try
            {
                byte mapId = GameOptionsManager.Instance.currentGameOptions.MapId;

                // Handle special case for dlekSehT (reversed Skeld)
                if (mapId == (byte)MapNames.Dleks)
                    return "dlekSehT";

                GameMapType mapType = GameMapTypeExtensions.FromMapId(mapId);
                return mapType.ToDisplayName();
            }
            catch (Exception e)
            {
                Log("[getMapName] Catch exception " + e.Message);
                return "Unknown";
            }
        }
    }
}

using AmongUs.GameOptions;
using GLMod.Class;
using GLMod.Services.Interfaces;
using BepInEx.Logging;
using GLMod.Enums;
using System;

namespace GLMod.Services.Implementations
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

        private void Log(string message) => ServiceLogger.Log(_logger, nameof(MapService), message);

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
                Log("[GetMapName] Catch exception " + e.Message);
                return "Unknown";
            }
        }
    }
}

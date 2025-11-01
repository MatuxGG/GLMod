using System.Collections.Generic;

namespace GLMod.Enums
{
    /// <summary>
    /// Represents the different maps available in Among Us
    /// </summary>
    public enum GameMapType
    {
        Unknown,
        TheSkeld,
        MiraHQ,
        Polus,
        Airship,
        TheFungle
    }

    /// <summary>
    /// Helper class for map name conversions
    /// </summary>
    public static class GameMapTypeExtensions
    {
        private static readonly Dictionary<byte, GameMapType> MapIdToType = new Dictionary<byte, GameMapType>
        {
            { 0, GameMapType.TheSkeld },
            { 1, GameMapType.MiraHQ },
            { 2, GameMapType.Polus },
            { 4, GameMapType.Airship },
            { 5, GameMapType.TheFungle }
        };

        private static readonly Dictionary<GameMapType, string> MapTypeToDisplayName = new Dictionary<GameMapType, string>
        {
            { GameMapType.TheSkeld, "The Skeld" },
            { GameMapType.MiraHQ, "MiraHQ" },
            { GameMapType.Polus, "Polus" },
            { GameMapType.Airship, "Airship" },
            { GameMapType.TheFungle, "The Fungle" },
            { GameMapType.Unknown, "Unknown" }
        };

        /// <summary>
        /// Converts a map ID to GameMapType
        /// </summary>
        public static GameMapType FromMapId(byte mapId)
        {
            return MapIdToType.TryGetValue(mapId, out var mapType) ? mapType : GameMapType.Unknown;
        }

        /// <summary>
        /// Gets the display name for a map type
        /// </summary>
        public static string ToDisplayName(this GameMapType mapType)
        {
            return MapTypeToDisplayName.TryGetValue(mapType, out var name) ? name : "Unknown";
        }

        /// <summary>
        /// Converts a MapNames enum to GameMapType
        /// </summary>
        public static GameMapType FromMapNames(byte mapNamesValue)
        {
            return FromMapId(mapNamesValue);
        }
    }
}

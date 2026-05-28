namespace GLMod.Constants
{
    /// <summary>
    /// Contains constant values used throughout the application
    /// </summary>
    public static class GameConstants
    {
        /// <summary>
        /// API endpoint URL for GoodLoss services
        /// </summary>
        public const string API_ENDPOINT = "https://goodloss.fr/api";

        /// <summary>
        /// Characters used for generating support IDs
        /// </summary>
        public const string SUPPORT_ID_CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz123456789";

        /// <summary>
        /// Length of generated support IDs
        /// </summary>
        public const int SUPPORT_ID_LENGTH = 10;

        /// <summary>
        /// Timeout for RPC synchronization in seconds
        /// </summary>
        public const float RPC_SYNC_TIMEOUT = 5.0f;

        /// <summary>
        /// Polling interval for background processes in seconds
        /// </summary>
        public const float BACKGROUND_POLLING_INTERVAL = 0.5f;

        /// <summary>
        /// Polling interval for RPC checks in seconds
        /// </summary>
        public const float RPC_POLLING_INTERVAL = 0.1f;

        /// <summary>
        /// Default game code when unknown
        /// </summary>
        public const string DEFAULT_GAME_CODE = "XXXXXX";

        /// <summary>
        /// Default map name when unknown
        /// </summary>
        public const string DEFAULT_MAP_NAME = "Unknown";

        /// <summary>
        /// Action code prefix for disconnections detected by the background process.
        /// </summary>
        public const string ACTION_PREFIX_DC_PROCESS = "DC_PROCESS_";

        /// <summary>
        /// Action code prefix for RPC "DisconnectInternal" disconnections.
        /// </summary>
        public const string ACTION_PREFIX_DC_INTERNAL = "DC_INTERNAL_";

        /// <summary>
        /// Action code prefix for RPC "HandleDisconnect" disconnections.
        /// </summary>
        public const string ACTION_PREFIX_DC_HANDLE = "DC_HANDLE_";

        /// <summary>
        /// Action code prefix for RPC "OnDisconnect" disconnections.
        /// </summary>
        public const string ACTION_PREFIX_DC_ON = "DC_ON_";

        /// <summary>
        /// Action code prefix recorded when a sabotage starts (suffixed by the sabotage type).
        /// </summary>
        public const string ACTION_PREFIX_SAB_START = "SAB_START_";

        /// <summary>
        /// Action code prefix recorded when a sabotage ends (suffixed by the sabotage type).
        /// </summary>
        public const string ACTION_PREFIX_SAB_END = "SAB_END_";
    }
}

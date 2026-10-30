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
        /// Environment variable overriding API_ENDPOINT (e.g. https://preprod.goodloss.fr/api for tests)
        /// </summary>
        public const string API_ENDPOINT_ENV_VAR = "GLMOD_API_ENDPOINT";

        /// <summary>
        /// Clock difference with the API server ignored by ServerClock (HTTP Date has a 1s resolution)
        /// </summary>
        public const long CLOCK_SKEW_TOLERANCE_MS = 2000;

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
        /// Interval between two samples of the player positions, in seconds
        /// </summary>
        public const float POSITION_SAMPLING_INTERVAL = 0.1f;

        /// <summary>
        /// A sampled position is only sent when the straight line between the sent positions
        /// would move the player further than this from where it really was (map units)
        /// </summary>
        public const float POSITION_SIMPLIFY_TOLERANCE = 0.1f;

        /// <summary>
        /// Longest time between two sent positions of a player, even when it does not move (ms)
        /// </summary>
        public const long POSITION_MAX_GAP_MS = 5000;

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

        /// <summary>
        /// Path segment that ends the API base URL; removing it gives the Good Loss site URL.
        /// </summary>
        public const string API_PATH_SUFFIX = "/api";

        /// <summary>
        /// Site path of a game recap, followed by the game id.
        /// </summary>
        public const string SITE_GAME_PATH = "/game/";

        /// <summary>
        /// Local game history file, stored in the BepInEx config folder.
        /// </summary>
        public const string GAME_HISTORY_FILE_NAME = "glmod-history.json";

        /// <summary>
        /// Number of games kept in the local history.
        /// </summary>
        public const int GAME_HISTORY_MAX_ENTRIES = 20;

        /// <summary>
        /// Embedded resource holding the Good Loss logo shown in the menu header (GLMod/Resources/goodloss-logo.png).
        /// </summary>
        public const string LOGO_RESOURCE_NAME = "GLMod.Resources.goodloss-logo.png";

        /// <summary>
        /// Embedded Among Us Ranked logo, shown by the Among Us Ranked menu theme (GLMod/Resources/ranked-logo.png).
        /// </summary>
        public const string RANKED_LOGO_RESOURCE_NAME = "GLMod.Resources.ranked-logo.png";

        /// <summary>
        /// Mod name (prefix) that switches the menu to the Among Us Ranked theme.
        /// </summary>
        public const string RANKED_MOD_NAME = "Among Us Ranked";

        /// <summary>
        /// Value of the "theme" query parameter of the /player/* GET endpoints for the Among Us Ranked menu theme
        /// (data grouped over the Among Us Ranked mods). The Good Loss theme sends no theme.
        /// </summary>
        public const string API_THEME_RANKED = "ranked";

        /// <summary>
        /// Prefix of the embedded translation files (GLMod/Resources/Translations/&lt;language&gt;.json).
        /// </summary>
        public const string TRANSLATION_RESOURCE_PREFIX = "GLMod.Resources.Translations.";

        /// <summary>
        /// Id of the built-in Account tab of the menu (IMenuService.FindTab / UnregisterTab / Select).
        /// </summary>
        public const string MENU_TAB_ACCOUNT = "glmod.account";

        /// <summary>
        /// Id of the built-in Last game tab of the menu.
        /// </summary>
        public const string MENU_TAB_LAST_GAME = "glmod.lastgame";

        /// <summary>
        /// Id of the built-in Sessions tab of the menu.
        /// </summary>
        public const string MENU_TAB_SESSIONS = "glmod.sessions";

        /// <summary>
        /// Id of the built-in Stats tab of the menu.
        /// </summary>
        public const string MENU_TAB_STATS = "glmod.stats";

        /// <summary>
        /// Seconds after which a menu tab reloads its API data when shown.
        /// </summary>
        public const double MENU_REFRESH_SECONDS = 60;

        /// <summary>
        /// Seconds after which a menu tab retries a failed API load when shown.
        /// </summary>
        public const double MENU_RETRY_SECONDS = 10;

        /// <summary>
        /// After a game ends, how long the Last game tab keeps polling Good Loss for that game.
        /// </summary>
        public const double LAST_GAME_WAIT_SECONDS = 120;

        /// <summary>
        /// Polling interval of the Last game tab while it waits for the game that just ended.
        /// </summary>
        public const float LAST_GAME_POLL_SECONDS = 5f;

        /// <summary>
        /// Name of the UnityEngine.KeyCode that opens / closes the Good Loss menu.
        /// </summary>
        public const string MENU_TOGGLE_KEY = "F2";

        /// <summary>
        /// Size of the buffer receiving a Steam session ticket (real tickets are about 250 bytes).
        /// </summary>
        public const int STEAM_TICKET_BUFFER_SIZE = 1024;

        /// <summary>
        /// Delay before sending a Steam ticket again when the API answered "Invalid ticket"
        /// (Steam may not have registered a ticket created a moment ago).
        /// </summary>
        public const float STEAM_TICKET_RETRY_DELAY = 2f;

        /// <summary>
        /// Prefix of the secret game token issued by /user/login for a Steam ticket.
        /// </summary>
        public const string GAME_TOKEN_PREFIX = "glm_";

        /// <summary>
        /// A game token is reused by the next login while it stays valid at least this long.
        /// </summary>
        public const long GAME_TOKEN_REUSE_MARGIN_SECONDS = 86400;

        /// <summary>
        /// Endpoint giving the signed ticket of the player for the Good Loss game servers.
        /// </summary>
        public const string API_PATH_ROOM_TICKET = "/user/room-ticket";

        /// <summary>
        /// Game servers run by Good Loss (ping server of their region): the only ones that receive the room ticket.
        /// </summary>
        public static readonly string[] GOOD_LOSS_GAME_SERVERS = { "impranked.goodloss.fr", "betaranked.goodloss.fr" };

        /// <summary>
        /// End of the room ticket in the connection data (GLPlugin looks for it).
        /// </summary>
        public const string ROOM_TICKET_MARKER = "GLMT";

        /// <summary>
        /// Seconds between two reloads of the room ticket (it lasts 15 minutes; a shorter delay also
        /// brings a code just changed on the site).
        /// </summary>
        public const float ROOM_TICKET_REFRESH_SECONDS = 120f;

        /// <summary>
        /// Seconds between two checks of whether the room ticket must be reloaded.
        /// </summary>
        public const float ROOM_TICKET_POLL_SECONDS = 2f;

        /// <summary>
        /// A room ticket closer than this to its expiry is not sent anymore.
        /// </summary>
        public const long ROOM_TICKET_EXPIRY_MARGIN_SECONDS = 30;
    }
}

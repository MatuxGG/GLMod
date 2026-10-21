namespace GLMod.TestHarness
{
    /// <summary>
    /// Constants used by the end-to-end test harness.
    /// </summary>
    public static class HarnessConstants
    {
        // Environment variables set by run-e2e.ps1 for each game instance
        public const string ENV_INSTANCE = "GLMOD_TEST_INSTANCE";
        public const string ENV_RUN_ID = "GLMOD_TEST_RUN_ID";
        public const string ENV_OUTPUT = "GLMOD_TEST_OUTPUT";
        public const string ENV_MOD_NAME = "GLMOD_TEST_MOD_NAME";
        public const string ENV_MAP_ID = "GLMOD_TEST_MAP_ID";
        public const string ENV_SCENARIO = "GLMOD_TEST_SCENARIO";
        public const string ENV_REGION = "GLMOD_TEST_REGION";

        // Server region selected by every instance (a custom one: official servers are refused)
        public const string DEFAULT_REGION = "Ranked EU";

        // Scenarios (ENV_SCENARIO): the 2-game session, or the movement game
        public const string SCENARIO_SESSION = "session";
        public const string SCENARIO_MOVEMENT = "movement";

        // Scenario shape
        public const int PLAYER_COUNT = 4;
        public const int HOST_INSTANCE = 0;

        // Online games: never run the harness against official Innersloth servers
        public const string MAIN_MENU_SCENE = "MainMenu";
        public const string ONLINE_SCENE = "OnlineGame";
        public static readonly string[] OFFICIAL_SERVER_MARKERS = { "among.us", "innersloth" };

        // Hazel timeouts: 4 Unity instances on one machine stall for several seconds on heavy
        // frames (map load, meeting open); the vanilla timeouts then drop the client.
        public const int NETWORK_DISCONNECT_TIMEOUT_MS = 60000;
        public const int NETWORK_MISSING_PINGS_UNTIL_DISCONNECT = 60;
        public const float NETWORK_RELAX_INTERVAL = 1f;
        public const int TARGET_FRAME_RATE = 20;

        // Output files (written to ENV_OUTPUT)
        public const string LOBBY_FILE_FORMAT = "lobby-{0}.json";
        public const string PLAN_FILE_FORMAT = "plan-{0}.json";
        public const string RESULT_FILE_FORMAT = "result-{0}.json";
        public const string LOG_FILE_FORMAT = "instance-{0}.log";

        // Timings (seconds)
        public const float POLL_INTERVAL = 0.25f;
        public const float GLOBAL_TIMEOUT = 900f;
        public const float MENU_SETTLE_DELAY = 3f;
        public const float CONNECT_TIMEOUT = 60f;
        public const float LOBBY_FILE_TIMEOUT = 120f;
        public const float CLIENT_JOIN_STAGGER = 3f;
        public const int JOIN_MAX_ATTEMPTS = 3;
        public const float LOBBY_FULL_TIMEOUT = 120f;
        public const float LOBBY_SETTLE_DELAY = 4f;
        public const float START_COUNTDOWN_TIMEOUT = 15f;
        public const float GAME_START_TIMEOUT = 90f;
        public const float GLMOD_READY_TIMEOUT = 60f;
        public const float PLAN_TIMEOUT = 30f;
        public const float CAN_MOVE_TIMEOUT = 30f;
        public const float POST_INTRO_DELAY = 2f;
        public const float STEP_TIMEOUT = 90f;
        public const float SABOTAGE_COOLDOWN_TIMEOUT = 60f;
        public const float SABOTAGE_SETTLE_DELAY = 2f;
        public const float SWITCH_TOGGLE_DELAY = 0.3f;
        public const float EMERGENCY_COOLDOWN_TIMEOUT = 90f;
        public const float MEETING_START_TIMEOUT = 15f;
        public const float MEETING_END_TIMEOUT = 60f;
        public const float POST_MEETING_DELAY = 3f;
        public const float KILL_DELAY = 2f;
        public const float KILL_TIMEOUT = 30f;
        public const float GHOST_DELAY = 4f;
        public const float BODY_TIMEOUT = 10f;
        public const float REPORT_DELAY = 2f;
        public const float VOTING_PHASE_TIMEOUT = 180f;
        public const float PROCEED_TIMEOUT = 30f;
        public const float PROCEED_DELAY = 1f;
        public const float GAME_END_TIMEOUT = 240f;
        public const float UPLOAD_TIMEOUT = 30f;
        public const float END_SCREEN_TIMEOUT = 60f;
        public const float END_SCREEN_DELAY = 6f;
        public const float REJOIN_TIMEOUT = 90f;
        public const float QUIT_DELAY = 3f;

        // Tasks
        public const float MOVE_SETTLE_DELAY = 1f;
        public const float MINIGAME_OPEN_TIMEOUT = 5f;
        public const float MINIGAME_VISIBLE_DELAY = 2f;
        public const float MINIGAME_VISIBLE_DELAY_FAST = 0.5f;
        public const int TASK_EXTRA_STEP_ATTEMPTS = 2;
        public const int MAX_TASKS_PER_PLAYER = 20;

        // Movement game: everyone walks around the map, the impostor kills twice with a wait in between
        public const float MOVEMENT_FIRST_KILL_DELAY = 25f;
        public const float MOVEMENT_KILL_GAP = 35f;
        public const float MOVEMENT_HUNT_TIMEOUT = 150f;
        public const float MOVEMENT_HUNT_REPLAN_INTERVAL = 1f;
        public const float MOVEMENT_KILL_CONFIRM_TIMEOUT = 5f;
        public const float MOVEMENT_PAUSE_MIN = 1.5f;
        public const float MOVEMENT_PAUSE_MAX = 4f;
        public const float MOVEMENT_WALK_TIMEOUT = 45f;
        // Positions within this time of a kill are not compared (the kill moves the killer onto the victim)
        public const long MOVEMENT_KILL_EXCLUSION_MS = 1000;
        // Largest distance allowed between where a player was and where the recap draws it (map units)
        public const double MOVEMENT_MAX_DEVIATION = 0.5;
        // Interval of the former position sampling, measured for comparison only
        public const long LEGACY_SAMPLING_MS = 500;
        public const long CURRENT_SAMPLING_MS = 100;
        // A player must have walked at least this far (map units) in the positions sent
        public const double MOVEMENT_MIN_PATH_LENGTH = 10;

        // Navigation grid (built from the ship colliders, see HarnessWalker)
        public const float NAV_CELL_SIZE = 0.2f;
        public const float NAV_CLEARANCE = 0.3f;
        public const float NAV_BOUNDS_MARGIN = 1f;
        public const int NAV_MAX_CELLS = 400000;
        public const float NAV_NEAREST_SEARCH = 2f;
        public const float WALK_WAYPOINT_REACHED = 0.2f;
        public const float WALK_STUCK_TIMEOUT = 1.5f;
        public const float WALK_STUCK_DISTANCE = 0.1f;
        public const int WALK_MAX_STUCK_REPLANS = 3;

        // Where to stand relative to a console when trying to use it (first one that works)
        public static readonly UnityEngine.Vector2[] CONSOLE_APPROACH_OFFSETS =
        {
            new UnityEngine.Vector2(0f, 0f),
            new UnityEngine.Vector2(0f, -0.5f),
            new UnityEngine.Vector2(0.5f, 0f),
            new UnityEngine.Vector2(-0.5f, 0f),
            new UnityEngine.Vector2(0f, 0.5f),
            new UnityEngine.Vector2(0f, -1f),
            new UnityEngine.Vector2(1f, 0f),
            new UnityEngine.Vector2(-1f, 0f),
            new UnityEngine.Vector2(0f, 1f)
        };
    }
}

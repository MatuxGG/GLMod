using System;
using System.IO;

namespace GLMod.TestHarness
{
    /// <summary>
    /// Per-process harness configuration, read from environment variables.
    /// </summary>
    public sealed class HarnessContext
    {
        public int Instance { get; }
        public string RunId { get; }
        public string OutputDir { get; }

        /// <summary>Mod name forced through GLMod.ConfigService.SetModName, or null to keep GLMod's own.</summary>
        public string ModName { get; }

        /// <summary>Map the host selects in the lobby (Among Us MapId), or null to keep the lobby's.</summary>
        public byte? MapId { get; }

        /// <summary>HarnessConstants.SCENARIO_SESSION, SCENARIO_MOVEMENT or SCENARIO_PLUGIN.</summary>
        public string Scenario { get; }

        /// <summary>Name of the server region every instance selects (HarnessConstants.DEFAULT_REGION by default).</summary>
        public string Region { get; }

        public bool IsHost => Instance == HarnessConstants.HOST_INSTANCE;

        /// <summary>The games played by the scenario, in order (ScenarioPlan game numbers).</summary>
        public int[] GameNumbers => Scenario == HarnessConstants.SCENARIO_PLUGIN
            ? new int[0]
            : Scenario == HarnessConstants.SCENARIO_MOVEMENT
                ? new[] { ScenarioPlan.GAME_MOVEMENT }
                : new[] { ScenarioPlan.GAME_CREWMATES_WIN_BY_TASKS, ScenarioPlan.GAME_IMPOSTOR_WINS_BY_VOTE };

        public HarnessContext(int instance, string runId, string outputDir, string modName = null, byte? mapId = null,
            string scenario = HarnessConstants.SCENARIO_SESSION, string region = HarnessConstants.DEFAULT_REGION)
        {
            Instance = instance;
            RunId = runId;
            OutputDir = outputDir;
            ModName = modName;
            MapId = mapId;
            Scenario = scenario;
            Region = region;
        }

        /// <summary>
        /// Returns null when the harness is not requested (plain game launch).
        /// Throws when the environment is partially or incorrectly set.
        /// </summary>
        public static HarnessContext FromEnvironment()
        {
            string instanceValue = Environment.GetEnvironmentVariable(HarnessConstants.ENV_INSTANCE);
            if (string.IsNullOrEmpty(instanceValue))
            {
                return null;
            }

            if (!int.TryParse(instanceValue, out int instance) || instance < 0 || instance >= HarnessConstants.PLAYER_COUNT)
            {
                throw new InvalidOperationException($"{HarnessConstants.ENV_INSTANCE} must be between 0 and {HarnessConstants.PLAYER_COUNT - 1}, got '{instanceValue}'");
            }

            string runId = Environment.GetEnvironmentVariable(HarnessConstants.ENV_RUN_ID);
            if (string.IsNullOrEmpty(runId))
            {
                throw new InvalidOperationException($"{HarnessConstants.ENV_RUN_ID} is required");
            }

            string outputDir = Environment.GetEnvironmentVariable(HarnessConstants.ENV_OUTPUT);
            if (string.IsNullOrEmpty(outputDir))
            {
                throw new InvalidOperationException($"{HarnessConstants.ENV_OUTPUT} is required");
            }

            string modName = Environment.GetEnvironmentVariable(HarnessConstants.ENV_MOD_NAME);

            byte? mapId = null;
            string mapValue = Environment.GetEnvironmentVariable(HarnessConstants.ENV_MAP_ID);
            if (!string.IsNullOrWhiteSpace(mapValue))
            {
                if (!byte.TryParse(mapValue, out byte parsed))
                {
                    throw new InvalidOperationException($"{HarnessConstants.ENV_MAP_ID} must be an Among Us map id, got '{mapValue}'");
                }
                mapId = parsed;
            }

            string scenario = ParseScenario(Environment.GetEnvironmentVariable(HarnessConstants.ENV_SCENARIO));

            string region = Environment.GetEnvironmentVariable(HarnessConstants.ENV_REGION);
            region = string.IsNullOrWhiteSpace(region) ? HarnessConstants.DEFAULT_REGION : region.Trim();

            Directory.CreateDirectory(outputDir);
            return new HarnessContext(instance, runId, outputDir, string.IsNullOrWhiteSpace(modName) ? null : modName.Trim(), mapId, scenario, region);
        }

        /// <summary>
        /// Empty means the session; otherwise the scenario name, case-insensitive.
        /// </summary>
        public static string ParseScenario(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return HarnessConstants.SCENARIO_SESSION;

            string scenario = value.Trim().ToLowerInvariant();
            if (scenario != HarnessConstants.SCENARIO_SESSION && scenario != HarnessConstants.SCENARIO_MOVEMENT && scenario != HarnessConstants.SCENARIO_PLUGIN)
            {
                throw new InvalidOperationException($"{HarnessConstants.ENV_SCENARIO} must be '{HarnessConstants.SCENARIO_SESSION}', '{HarnessConstants.SCENARIO_MOVEMENT}' or '{HarnessConstants.SCENARIO_PLUGIN}', got '{value}'");
            }
            return scenario;
        }

        public string PlanPath(int gameNumber) => Path.Combine(OutputDir, string.Format(HarnessConstants.PLAN_FILE_FORMAT, gameNumber));
        public string LobbyPath(int gameNumber) => Path.Combine(OutputDir, string.Format(HarnessConstants.LOBBY_FILE_FORMAT, gameNumber));
        public string ResultPath => Path.Combine(OutputDir, string.Format(HarnessConstants.RESULT_FILE_FORMAT, Instance));
        public string LogPath => Path.Combine(OutputDir, string.Format(HarnessConstants.LOG_FILE_FORMAT, Instance));
    }
}

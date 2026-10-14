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

        public bool IsHost => Instance == HarnessConstants.HOST_INSTANCE;

        public HarnessContext(int instance, string runId, string outputDir, string modName = null, byte? mapId = null)
        {
            Instance = instance;
            RunId = runId;
            OutputDir = outputDir;
            ModName = modName;
            MapId = mapId;
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

            Directory.CreateDirectory(outputDir);
            return new HarnessContext(instance, runId, outputDir, string.IsNullOrWhiteSpace(modName) ? null : modName.Trim(), mapId);
        }

        public string PlanPath(int gameNumber) => Path.Combine(OutputDir, string.Format(HarnessConstants.PLAN_FILE_FORMAT, gameNumber));
        public string LobbyPath(int gameNumber) => Path.Combine(OutputDir, string.Format(HarnessConstants.LOBBY_FILE_FORMAT, gameNumber));
        public string ResultPath => Path.Combine(OutputDir, string.Format(HarnessConstants.RESULT_FILE_FORMAT, Instance));
        public string LogPath => Path.Combine(OutputDir, string.Format(HarnessConstants.LOG_FILE_FORMAT, Instance));
    }
}

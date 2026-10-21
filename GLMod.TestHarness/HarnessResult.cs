using System.Collections.Generic;

namespace GLMod.TestHarness
{
    /// <summary>
    /// Result written by each instance to result-N.json, read by run-e2e.ps1.
    /// </summary>
    public sealed class HarnessResult
    {
        public string RunId { get; set; }
        public int Instance { get; set; }
        public bool IsHost { get; set; }
        public bool Passed { get; set; }
        public string Phase { get; set; }
        public string LocalPlayer { get; set; }
        public string Region { get; set; }
        public string ApiEndpoint { get; set; }
        public List<string> Failures { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public List<GameResult> Games { get; set; } = new List<GameResult>();
    }

    /// <summary>
    /// What this instance saw of one match.
    /// </summary>
    public sealed class GameResult
    {
        public int Number { get; set; }
        public ScenarioPlan Plan { get; set; }
        public string EndReason { get; set; }
        public string GameId { get; set; }
        public string GameStep { get; set; }
        public List<string> Failures { get; set; } = new List<string>();
        public string GameJson { get; set; }

        /// <summary>Movement game, host only: positions sent per player and their accuracy.</summary>
        public List<PlayerMovementStats> Movement { get; set; }
    }

    /// <summary>
    /// Positions GLMod sent for one player, compared with what the host really saw (every frame)
    /// and with what a fixed 10 Hz sampler and the former 0.5 s sampler would have sent.
    /// </summary>
    public sealed class PlayerMovementStats
    {
        public string Player { get; set; }
        public int PointsSent { get; set; }
        public int Points10Hz { get; set; }
        public int PointsLegacy { get; set; }
        public double PathLength { get; set; }
        public AccuracyStats Sent { get; set; }
        public AccuracyStats Fixed10Hz { get; set; }
        public AccuracyStats Legacy { get; set; }

        public override string ToString()
        {
            return $"{Player}: {PointsSent} points sent (10 Hz: {Points10Hz}, 0.5 s: {PointsLegacy}), path {PathLength:0.0}, "
                + $"max/mean deviation sent {Sent?.MaxDeviation:0.00}/{Sent?.MeanDeviation:0.000}, "
                + $"10 Hz {Fixed10Hz?.MaxDeviation:0.00}/{Fixed10Hz?.MeanDeviation:0.000}, "
                + $"0.5 s {Legacy?.MaxDeviation:0.00}/{Legacy?.MeanDeviation:0.000} ({Sent?.Samples} real positions)";
        }
    }
}

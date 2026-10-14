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
    }
}

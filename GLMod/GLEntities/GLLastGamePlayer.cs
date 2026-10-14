namespace GLMod.GLEntities
{
    /// <summary>
    /// The player's stats in the game. Null values are not applicable (e.g. kills of a crewmate).
    /// Durations and delays are in seconds.
    /// </summary>
    public class GLLastGamePlayer
    {
        public string name { get; set; }
        public string role { get; set; }
        public string team { get; set; }
        public string color { get; set; }
        public bool win { get; set; }
        public double? rp { get; set; }

        public int? kills { get; set; }
        public int? doubleKills { get; set; }
        public bool? firstBlood { get; set; }
        public double? timeToFirstKill { get; set; }

        public bool? killed { get; set; }
        public bool? killedFirst { get; set; }
        public bool? firstVictim { get; set; }
        public bool? exiled { get; set; }
        public int? deathTurn { get; set; }
        public int? exileTurn { get; set; }
        public double? aliveDuration { get; set; }
        public int? aliveTurns { get; set; }
        public int? aliveMeetings { get; set; }

        public int? tasks { get; set; }
        public int? tasksMax { get; set; }
        public double? taskSpeed { get; set; }

        public int? goodVotes { get; set; }
        public int? totalVotes { get; set; }
        public int? goodVotesImportantMeetings { get; set; }
        public int? totalVotesImportantMeetings { get; set; }
        public int? skips { get; set; }
        public int? votesReceived { get; set; }
        public int? teammateVotes { get; set; }
        public double? voteOrder { get; set; }
        public double? voteDelay { get; set; }

        public int? bodyReported { get; set; }
        public int? selfReports { get; set; }
        public double? reportDelay { get; set; }
        public int? emergencyCalled { get; set; }
    }
}

namespace GLMod.GLEntities
{
    /// <summary>
    /// Current rank of a player on one leaderboard (e.g. Among Us Ranked, Among Us Ranked Roles).
    /// Null numbers are not available (e.g. no position while unranked).
    /// </summary>
    public class GLPlayerRank
    {
        /// <summary>Mod of the leaderboard, e.g. "Among Us Ranked Roles".</summary>
        public string mod { get; set; }

        public int? season { get; set; }

        /// <summary>Rank name, e.g. "Gold 1".</summary>
        public string name { get; set; }

        /// <summary>Absolute URL of the rank badge (PNG).</summary>
        public string image { get; set; }

        public int? elo { get; set; }

        /// <summary>Rank points within the current rank.</summary>
        public int? rp { get; set; }

        /// <summary>Position in the leaderboard (1 = first).</summary>
        public int? position { get; set; }

        /// <summary>Absolute URL of the leaderboard page.</summary>
        public string leaderboardUrl { get; set; }
    }
}

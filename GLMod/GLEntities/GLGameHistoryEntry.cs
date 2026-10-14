namespace GLMod.GLEntities
{
    /// <summary>
    /// A game played by the local player, kept in the local history file (never sent to the API).
    /// </summary>
    public class GLGameHistoryEntry
    {
        /// <summary>Good Loss game id, empty when the game was not recorded.</summary>
        public string gameId { get; set; }

        /// <summary>Recap page on the site the game was sent to, empty when not recorded.</summary>
        public string recapUrl { get; set; }

        /// <summary>End of the game, Unix milliseconds (local clock, display only).</summary>
        public long endedAtMs { get; set; }

        public string map { get; set; }
        public string modName { get; set; }
        public string role { get; set; }
        public string team { get; set; }
        public bool won { get; set; }

        public GLGameHistoryEntry()
        {
            gameId = "";
            recapUrl = "";
            endedAtMs = 0;
            map = "";
            modName = "";
            role = "";
            team = "";
            won = false;
        }
    }
}

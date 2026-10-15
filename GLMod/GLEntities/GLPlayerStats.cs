namespace GLMod.GLEntities
{
    /// <summary>
    /// Response of GET /player/stats: win statistics of a player (all mods, or one mod / season when requested).
    /// </summary>
    public class GLPlayerStats
    {
        public string username { get; set; }

        /// <summary>Mod filter applied by the API, null for all mods.</summary>
        public string mod { get; set; }

        /// <summary>Season filter applied by the API, null for all seasons.</summary>
        public int? season { get; set; }

        public GLWinStats stats { get; set; }
    }
}

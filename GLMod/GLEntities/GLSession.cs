namespace GLMod.GLEntities
{
    /// <summary>
    /// A play session (games played in a row) on Good Loss.
    /// </summary>
    public class GLSession
    {
        public long id { get; set; }

        /// <summary>Session page on the site.</summary>
        public string url { get; set; }

        /// <summary>Start, Unix seconds.</summary>
        public long start { get; set; }

        /// <summary>End (last game end), Unix seconds; null while the session is live.</summary>
        public long? end { get; set; }

        /// <summary>Number of games in the session.</summary>
        public int games { get; set; }

        /// <summary>A game of the session is in progress.</summary>
        public bool live { get; set; }

        /// <summary>The session is still open (the player can still add games to it).</summary>
        public bool current { get; set; }
    }
}

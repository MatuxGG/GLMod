namespace GLMod.GLEntities
{
    /// <summary>
    /// Response of GET /lobbies/next: the planned lobby to suggest on the main menu, null when there is none,
    /// and its texts, in the language asked (the site has more languages than the menu).
    /// </summary>
    public class GLNextLobby
    {
        public GLPlannedLobby lobby { get; set; }

        public GLNextLobbyTexts texts { get; set; }
    }

    public class GLPlannedLobby
    {
        public int id { get; set; }

        /// <summary>Page of the lobby, where the player takes a place.</summary>
        public string url { get; set; }

        /// <summary>ISO 8601 date with its offset.</summary>
        public string startsAt { get; set; }

        public string mod { get; set; }

        public string language { get; set; }

        public int players { get; set; }

        public int places { get; set; }

        public int minimum { get; set; }
    }

    public class GLNextLobbyTexts
    {
        public string title { get; set; }

        public string players { get; set; }

        public string action { get; set; }
    }
}

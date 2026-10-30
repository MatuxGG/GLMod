namespace GLMod.GLEntities
{
    /// <summary>
    /// A friend and where they are.
    /// </summary>
    public class GLFriend
    {
        public int userId { get; set; }
        public string username { get; set; }

        /// <summary>Last in-game name, null when never seen in a room.</summary>
        public string playerName { get; set; }

        /// <summary>One of the GameConstants.FRIEND_STATUS_* values.</summary>
        public string status { get; set; }

        /// <summary>Name of the region they play on, null when offline.</summary>
        public string region { get; set; }

        public bool canJoin { get; set; }

        /// <summary>Only given when canJoin is true. Never displayed.</summary>
        public string gameCode { get; set; }

        /// <summary>Unix seconds, null when unknown or hidden by their settings.</summary>
        public long? lastSeen { get; set; }
    }
}

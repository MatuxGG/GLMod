namespace GLMod.GLEntities
{
    /// <summary>
    /// An account met in a recent game.
    /// </summary>
    public class GLRecentPlayer
    {
        public int userId { get; set; }
        public string username { get; set; }
        public string playerName { get; set; }
        public bool isFriend { get; set; }
    }
}

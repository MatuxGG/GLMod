namespace GLMod.GLEntities
{
    /// <summary>
    /// Response of GET /player/last-game: the last finished game of a player and their stats in it.
    /// </summary>
    public class GLLastGame
    {
        public string username { get; set; }
        public GLLastGameInfo game { get; set; }
        public GLLastGamePlayer player { get; set; }
    }
}

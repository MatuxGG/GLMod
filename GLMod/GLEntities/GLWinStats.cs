namespace GLMod.GLEntities
{
    /// <summary>
    /// Wins and games played, in total and by team.
    /// </summary>
    public class GLWinStats
    {
        public GLWinCount games { get; set; }
        public GLWinCount crewmate { get; set; }
        public GLWinCount impostor { get; set; }
        public GLWinCount neutral { get; set; }
    }
}

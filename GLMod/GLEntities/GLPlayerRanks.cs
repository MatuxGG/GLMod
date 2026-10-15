using System.Collections.Generic;

namespace GLMod.GLEntities
{
    /// <summary>
    /// Response of GET /player/ranks: current ranks of a player, one per ranked leaderboard.
    /// </summary>
    public class GLPlayerRanks
    {
        public string username { get; set; }
        public List<GLPlayerRank> ranks { get; set; }
    }
}

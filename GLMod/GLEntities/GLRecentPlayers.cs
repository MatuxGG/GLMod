using System.Collections.Generic;

namespace GLMod.GLEntities
{
    /// <summary>
    /// POST /friends/recent answer: the accounts met in the last games, most recent first.
    /// </summary>
    public class GLRecentPlayers
    {
        public List<GLRecentPlayer> players { get; set; }
    }
}

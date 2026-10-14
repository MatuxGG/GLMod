using System.Collections.Generic;

namespace GLMod.GLEntities
{
    /// <summary>
    /// Response of GET /player/sessions: the last sessions of a player, most recent first.
    /// </summary>
    public class GLSessionList
    {
        public string username { get; set; }
        public List<GLSession> sessions { get; set; }
    }
}

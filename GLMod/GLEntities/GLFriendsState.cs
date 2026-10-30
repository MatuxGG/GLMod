using System.Collections.Generic;

namespace GLMod.GLEntities
{
    /// <summary>
    /// POST /friends/sync answer: everything the Friends tab shows. Property names match the JSON keys.
    /// </summary>
    public class GLFriendsState
    {
        public List<GLFriend> friends { get; set; }
        public List<GLFriendRequest> incoming { get; set; }
        public List<GLFriendRequest> outgoing { get; set; }

        /// <summary>The accounts in the room of the player (Good Loss servers only), without the player.</summary>
        public List<GLRoomAccount> room { get; set; }

        public List<GLBlockedPlayer> blocked { get; set; }
        public GLFriendSettings settings { get; set; }
    }
}

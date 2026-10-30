namespace GLMod.GLEntities
{
    /// <summary>
    /// A friend request, received or sent.
    /// </summary>
    public class GLFriendRequest
    {
        public int requestId { get; set; }
        public int userId { get; set; }
        public string username { get; set; }
        public string playerName { get; set; }
    }
}

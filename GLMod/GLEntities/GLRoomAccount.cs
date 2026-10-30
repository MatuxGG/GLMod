namespace GLMod.GLEntities
{
    /// <summary>
    /// A Good Loss account in the room of the player, matched to the players of the game by client id.
    /// </summary>
    public class GLRoomAccount
    {
        public int? clientId { get; set; }
        public int userId { get; set; }
        public string username { get; set; }
        public string playerName { get; set; }
        public bool isFriend { get; set; }
        public bool requested { get; set; }
    }
}

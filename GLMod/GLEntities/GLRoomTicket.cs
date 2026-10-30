namespace GLMod.GLEntities
{
    /// <summary>
    /// POST /user/room-ticket answer: the ticket GLMod hands to the Good Loss game servers.
    /// Property names match the JSON keys.
    /// </summary>
    public class GLRoomTicket
    {
        /// <summary>Signed by the site, opaque here. Never logged.</summary>
        public string ticket { get; set; }

        /// <summary>Room code configured on the account, null when there is none.</summary>
        public string roomCode { get; set; }

        /// <summary>Expiry of the ticket, Unix seconds.</summary>
        public long expiresAt { get; set; }
    }
}

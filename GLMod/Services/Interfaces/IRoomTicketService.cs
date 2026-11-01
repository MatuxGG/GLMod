using System.Collections;

namespace GLMod.Services.Interfaces
{
    /// <summary>
    /// Keeps the signed ticket that identifies the player on the Good Loss game servers, and the room
    /// code of the account. Main thread only.
    /// </summary>
    public interface IRoomTicketService
    {
        /// <summary>
        /// Room code configured on the Good Loss account, null when there is none or it is not known yet.
        /// </summary>
        string RoomCode { get; }

        /// <summary>
        /// True once the API has answered a valid ticket: a null RoomCode then means the account has no code.
        /// </summary>
        bool IsRoomCodeKnown { get; }

        /// <summary>
        /// The ticket to send to a Good Loss server, null when there is no valid one. Never log it.
        /// </summary>
        string Ticket { get; }

        /// <summary>
        /// Asks the API for a new ticket (POST /user/room-ticket). Keeps the current one on failure.
        /// </summary>
        IEnumerator Reload();

        /// <summary>
        /// Endless coroutine reloading the ticket while the player is logged in and not connected to a
        /// game: the connection data is built synchronously, so a valid ticket must be ready beforehand.
        /// </summary>
        IEnumerator KeepFresh();
    }
}

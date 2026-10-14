using GLMod.GLEntities;
using System.Collections;

namespace GLMod.Services.Interfaces
{
    /// <summary>
    /// Interface for reading a player's public data on Good Loss (sessions, last game)
    /// </summary>
    public interface IPlayerService
    {
        /// <summary>
        /// Gets the last sessions of a player (GET /player/sessions)
        /// </summary>
        /// <param name="username">Good Loss account name</param>
        /// <param name="onComplete">Called with the sessions, most recent first</param>
        /// <param name="onError">Called with a readable error (e.g. "Player not found")</param>
        /// <returns>Coroutine</returns>
        IEnumerator GetSessions(string username, System.Action<GLSessionList> onComplete, System.Action<string> onError = null);

        /// <summary>
        /// Gets the last finished game of a player and their stats in it (GET /player/last-game)
        /// </summary>
        /// <param name="username">Good Loss account name</param>
        /// <param name="onComplete">Called with the game (its game and player parts may be null when the player has no game)</param>
        /// <param name="onError">Called with a readable error (e.g. "Player not found")</param>
        /// <returns>Coroutine</returns>
        IEnumerator GetLastGame(string username, System.Action<GLLastGame> onComplete, System.Action<string> onError = null);
    }
}

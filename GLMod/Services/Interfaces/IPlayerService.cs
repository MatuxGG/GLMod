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
        /// <param name="theme">API theme grouping the data: null for every mod (Good Loss menu), "ranked" for the Among Us Ranked mods</param>
        /// <returns>Coroutine</returns>
        IEnumerator GetSessions(string username, System.Action<GLSessionList> onComplete, System.Action<string> onError = null, string theme = null);

        /// <summary>
        /// Gets the last finished game of a player and their stats in it (GET /player/last-game)
        /// </summary>
        /// <param name="username">Good Loss account name</param>
        /// <param name="onComplete">Called with the game (its game and player parts may be null when the player has no game)</param>
        /// <param name="onError">Called with a readable error (e.g. "Player not found")</param>
        /// <param name="theme">API theme grouping the data: null for every mod (Good Loss menu), "ranked" for the Among Us Ranked mods</param>
        /// <returns>Coroutine</returns>
        IEnumerator GetLastGame(string username, System.Action<GLLastGame> onComplete, System.Action<string> onError = null, string theme = null);

        /// <summary>
        /// Gets the current ranks of a player, one per ranked leaderboard (GET /player/ranks)
        /// </summary>
        /// <param name="username">Good Loss account name</param>
        /// <param name="onComplete">Called with the ranks</param>
        /// <param name="onError">Called with a readable error (e.g. "Player not found")</param>
        /// <param name="theme">API theme grouping the data: null for every mod (Good Loss menu), "ranked" for the Among Us Ranked mods</param>
        /// <returns>Coroutine</returns>
        IEnumerator GetRanks(string username, System.Action<GLPlayerRanks> onComplete, System.Action<string> onError = null, string theme = null);

        /// <summary>
        /// Gets the win statistics of a player over all mods (GET /player/stats)
        /// </summary>
        /// <param name="username">Good Loss account name</param>
        /// <param name="onComplete">Called with the statistics</param>
        /// <param name="onError">Called with a readable error (e.g. "Player not found")</param>
        /// <param name="theme">API theme grouping the data: null for every mod (Good Loss menu), "ranked" for the Among Us Ranked mods</param>
        /// <returns>Coroutine</returns>
        IEnumerator GetStats(string username, System.Action<GLPlayerStats> onComplete, System.Action<string> onError = null, string theme = null);
    }
}

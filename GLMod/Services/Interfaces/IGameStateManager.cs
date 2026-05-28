using GLMod.Enums;
using GLMod.GLEntities;
using System.Collections;
using System.Collections.Generic;

namespace GLMod.Services.Interfaces
{
    /// <summary>
    /// Interface for managing game state and flow
    /// </summary>
    public interface IGameStateManager
    {
        /// <summary>
        /// Gets the current game instance
        /// </summary>
        GLGame CurrentGame { get; }

        /// <summary>
        /// Gets the current game step/stage
        /// </summary>
        GameStep Step { get; set; }

        /// <summary>
        /// Gets the current game code
        /// </summary>
        string GameCode { get; set; }

        /// <summary>
        /// Gets the current map name
        /// </summary>
        string GameMap { get; set; }

        /// <summary>
        /// Starts a new game
        /// </summary>
        /// <param name="code">Game code</param>
        /// <param name="map">Map name</param>
        /// <param name="ranked">Is ranked game</param>
        void StartGame(string code, string map, bool ranked);

        /// <summary>
        /// Updates the ranked flag of the current game.
        /// Internally stored as "0"/"1" to match the Good Loss API contract.
        /// </summary>
        /// <param name="ranked">true for a ranked game, false otherwise</param>
        void SetRanked(bool ranked);

        /// <summary>
        /// Adds a player to the current game
        /// </summary>
        /// <param name="playerName">Player name</param>
        /// <param name="role">Player role</param>
        /// <param name="team">Player team</param>
        /// <param name="color">Player color</param>
        void AddPlayer(string playerName, string role, string team, string color);

        /// <summary>
        /// Sends the game data to the API
        /// </summary>
        /// <param name="onComplete">Callback with success status</param>
        /// <returns>Coroutine</returns>
        IEnumerator SendGame(System.Action<bool> onComplete = null);

        /// <summary>
        /// Syncs the game ID via RPC
        /// </summary>
        /// <param name="onComplete">Callback with success status</param>
        /// <returns>Coroutine</returns>
        IEnumerator SyncGameId(System.Action<bool> onComplete = null);

        /// <summary>
        /// Adds the local player to the game on the server
        /// </summary>
        /// <param name="onComplete">Callback with success status</param>
        /// <returns>Coroutine</returns>
        IEnumerator AddMyPlayer(System.Action<bool> onComplete = null);

        /// <summary>
        /// Asks the server which player should benefit from a T1 shield for the current game.
        /// Retries while the API returns 400 (not all players have sent their addMyPlayer yet).
        /// </summary>
        /// <param name="onComplete">Callback with the in-game player name (PseudoInGame)</param>
        /// <param name="onError">Callback with the error message if the call ultimately fails</param>
        /// <returns>Coroutine</returns>
        IEnumerator GetShieldPlayer(System.Action<string> onComplete = null, System.Action<string> onError = null);

        /// <summary>
        /// Sets the winning teams
        /// </summary>
        /// <param name="winners">List of winning team names</param>
        void SetWinnerTeams(List<string> winners);

        /// <summary>
        /// Adds a winner player by name
        /// </summary>
        /// <param name="playerName">Player name</param>
        void AddWinnerPlayer(string playerName);

        /// <summary>
        /// Ends the current game
        /// </summary>
        /// <returns>Coroutine</returns>
        IEnumerator EndGame();

        /// <summary>
        /// Adds an action to the current game
        /// </summary>
        /// <param name="source">Source player</param>
        /// <param name="target">Target player</param>
        /// <param name="action">Action type</param>
        void AddAction(string source, string target, string action);

        /// <summary>
        /// Resets the game state
        /// </summary>
        void ResetGame();
    }
}

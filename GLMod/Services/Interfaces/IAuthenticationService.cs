using System.Collections;

namespace GLMod.Services.Interfaces
{
    /// <summary>
    /// Interface for authentication and user session management
    /// </summary>
    public interface IAuthenticationService
    {
        /// <summary>
        /// Gets the authentication token: the secret game token ("glm_…") of the Steam ticket login.
        /// Never log or display it.
        /// </summary>
        string Token { get; }

        /// <summary>
        /// Gets the secret game token sent as "Authorization: Bearer" to the API,
        /// or an empty string when not logged in
        /// </summary>
        string BearerToken { get; }

        /// <summary>
        /// Gets whether the user is currently logged in
        /// </summary>
        bool IsLoggedIn { get; }

        /// <summary>
        /// Gets whether the user is banned
        /// </summary>
        bool IsBanned { get; }

        /// <summary>
        /// Gets the ban reason if user is banned
        /// </summary>
        string BanReason { get; }

        /// <summary>
        /// Logs in to Good Loss with a Steam session ticket (the API has Steam prove the SteamID).
        /// No other login exists: without a usable ticket the player stays logged out. A still
        /// valid game token is reused without a new ticket. Main thread only (Steam API).
        /// </summary>
        /// <param name="onComplete">Callback with success status</param>
        /// <returns>Coroutine</returns>
        IEnumerator Login(System.Action<bool> onComplete = null);

        /// <summary>
        /// Logs out the current user
        /// </summary>
        void Logout();

        /// <summary>
        /// Gets the Good Loss account name of the logged-in player
        /// </summary>
        /// <returns>Account name or empty string if not logged in</returns>
        string GetAccountName();

        /// <summary>
        /// Updates the login state without a game token (no bearer: the API calls acting for the
        /// player will not identify it). Used to log out or mark a ban.
        /// </summary>
        /// <param name="isLoggedIn">Whether user is logged in</param>
        /// <param name="token">Legacy authentication token ("username#keycloakId")</param>
        /// <param name="isBanned">Whether user is banned</param>
        /// <param name="banReason">Ban reason</param>
        void SetLoginState(bool isLoggedIn, string token, bool isBanned, string banReason);
    }
}

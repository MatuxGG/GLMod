using System.Collections;

namespace GLMod.Services
{
    /// <summary>
    /// Interface for authentication and user session management
    /// </summary>
    public interface IAuthenticationService
    {
        /// <summary>
        /// Gets the authentication token
        /// </summary>
        string Token { get; }

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
        /// Attempts to log in the user with Steam credentials
        /// </summary>
        /// <param name="onComplete">Callback with success status</param>
        /// <returns>Coroutine</returns>
        IEnumerator Login(System.Action<bool> onComplete = null);

        /// <summary>
        /// Logs out the current user
        /// </summary>
        void Logout();

        /// <summary>
        /// Gets the account name from the authentication token
        /// </summary>
        /// <returns>Account name or empty string if not logged in</returns>
        string GetAccountName();

        /// <summary>
        /// Updates the login state
        /// </summary>
        /// <param name="isLoggedIn">Whether user is logged in</param>
        /// <param name="token">Authentication token</param>
        /// <param name="isBanned">Whether user is banned</param>
        /// <param name="banReason">Ban reason</param>
        void SetLoginState(bool isLoggedIn, string token, bool isBanned, string banReason);
    }
}

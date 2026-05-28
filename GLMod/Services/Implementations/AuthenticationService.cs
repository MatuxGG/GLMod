using BepInEx.Configuration;
using BepInEx.Logging;
using GLMod.Class;
using GLMod.Constants;
using GLMod.Services.Interfaces;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;

namespace GLMod.Services.Implementations
{
    /// <summary>
    /// Handles user authentication and session management
    /// </summary>
    public class AuthenticationService : IAuthenticationService
    {
        private string _token;
        private bool _isLoggedIn;
        private bool _isBanned;
        private string _banReason;
        private readonly ConfigEntry<string> _connectionState;
        private readonly ManualLogSource _logger;

        public string Token => _token;
        public bool IsLoggedIn => _isLoggedIn;
        public bool IsBanned => _isBanned;
        public string BanReason => _banReason;

        public AuthenticationService(ManualLogSource logger, ConfigEntry<string> connectionState)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _connectionState = connectionState ?? throw new ArgumentNullException(nameof(connectionState));
            _token = null;
            _isLoggedIn = false;
            _isBanned = false;
            _banReason = "";
        }

        private void Log(string message) => ServiceLogger.Log(_logger, nameof(AuthenticationService), message);

        public IEnumerator Login(System.Action<bool> onComplete = null)
        {
            var steamId = SteamUser.GetSteamID().m_SteamID.ToString();
            var form = new Dictionary<string, string> { { "steamId", steamId } };

            ApiResponse response = null;

            // Call API service
            yield return ApiService.PostFormWithErrorHandlingAsync(
                GameConstants.API_ENDPOINT + "/user/login",
                form,
                apiResponse => { response = apiResponse; }
            );

            // Check if response is null
            if (response == null)
            {
                Log("[Login] failed, no response");
                SetLoginState(false, "", false, "");
                onComplete?.Invoke(false);
                yield break;
            }

            // Interpret response
            if (response.IsSuccess)
            {
                Log("[Login] success");
                SetLoginState(true, response.Content, false, "");
                onComplete?.Invoke(true);
            }
            else if (response.StatusCode == 403)
            {
                var trimmed = response.Content?.Trim('"').Trim();
                if (!string.IsNullOrEmpty(trimmed) && trimmed.StartsWith("Banned: ", StringComparison.OrdinalIgnoreCase))
                {
                    string reason = trimmed.Substring("Banned: ".Length);
                    Log("[Login] User banned, reason: " + reason);
                    SetLoginState(false, "", true, reason);
                }
                else
                {
                    Log($"[Login] failed 403: {trimmed}");
                    SetLoginState(false, "", false, "");
                }
                onComplete?.Invoke(false);
            }
            else
            {
                Log($"[Login] failed - Status code: {response.StatusCode}");
                SetLoginState(false, "", false, "");
                onComplete?.Invoke(false);
            }
        }

        public void Logout()
        {
            try
            {
                if (!string.IsNullOrEmpty(_token))
                {
                    SetLoginState(false, "", false, "");
                }
            }
            catch (Exception e)
            {
                Log("[Logout] Catch exception " + e.Message);
            }
        }

        public string GetAccountName()
        {
            try
            {
                if (!_isLoggedIn || string.IsNullOrEmpty(_token))
                {
                    return "";
                }

                int hashIndex = _token.IndexOf("#");
                if (hashIndex > 0)
                {
                    return _token.Substring(0, hashIndex);
                }

                return "";
            }
            catch (Exception e)
            {
                Log("[GetAccountName] Catch exception " + e.Message);
                return "";
            }
        }

        public void SetLoginState(bool isLoggedIn, string token, bool isBanned, string banReason)
        {
            _isLoggedIn = isLoggedIn;
            _token = token ?? "";
            _isBanned = isBanned;
            _banReason = banReason ?? "";
            _connectionState.Value = isLoggedIn ? "Yes" : "No";
        }
    }
}

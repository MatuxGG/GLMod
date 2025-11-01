using BepInEx.Configuration;
using GLMod.Class;
using GLMod.Constants;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;

namespace GLMod.Services
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

        public string Token => _token;
        public bool IsLoggedIn => _isLoggedIn;
        public bool IsBanned => _isBanned;
        public string BanReason => _banReason;

        public AuthenticationService(ConfigEntry<string> connectionState)
        {
            _connectionState = connectionState;
            _token = null;
            _isLoggedIn = false;
            _isBanned = false;
            _banReason = "";
        }

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
                GLMod.log("Login failed, no response");
                SetLoginState(false, "", false, "");
                onComplete?.Invoke(false);
                yield break;
            }

            // Interpret response
            if (response.IsSuccess)
            {
                GLMod.log("Login success");
                SetLoginState(true, response.Content, false, "");
                onComplete?.Invoke(true);
            }
            else if (response.StatusCode == 403)
            {
                var trimmed = response.Content?.Trim('"').Trim();
                if (!string.IsNullOrEmpty(trimmed) && trimmed.StartsWith("Banned: ", StringComparison.OrdinalIgnoreCase))
                {
                    string reason = trimmed.Substring("Banned: ".Length);
                    GLMod.log("User banned, reason: " + reason);
                    SetLoginState(false, "", true, reason);
                }
                else
                {
                    GLMod.log($"Login failed 403: {trimmed}");
                    SetLoginState(false, "", false, "");
                }
                onComplete?.Invoke(false);
            }
            else
            {
                GLMod.log($"Login failed - Status code: {response.StatusCode}");
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
                GLMod.log("[Logout] Catch exception " + e.Message);
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
                GLMod.log("[GetAccountName] Catch exception " + e.Message);
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

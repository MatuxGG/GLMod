using BepInEx.Configuration;
using BepInEx.Logging;
using GLMod.Class;
using GLMod.Constants;
using GLMod.GLEntities;
using GLMod.Services.Interfaces;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GLMod.Services.Implementations
{
    /// <summary>
    /// Handles user authentication and session management
    /// </summary>
    public class AuthenticationService : IAuthenticationService
    {
        private string _token;
        private string _bearerToken;
        private string _accountName;
        private long? _tokenExpiresAt;
        private bool _isLoggedIn;
        private bool _isBanned;
        private string _banReason;
        private readonly ConfigEntry<string> _connectionState;
        private readonly ManualLogSource _logger;
        private readonly string _apiEndpoint;

        public string Token => _token;
        public string BearerToken => _bearerToken;
        public bool IsLoggedIn => _isLoggedIn;
        public bool IsBanned => _isBanned;
        public string BanReason => _banReason;

        public AuthenticationService(ManualLogSource logger, ConfigEntry<string> connectionState, string apiEndpoint)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _connectionState = connectionState ?? throw new ArgumentNullException(nameof(connectionState));
            _apiEndpoint = apiEndpoint ?? throw new ArgumentNullException(nameof(apiEndpoint));
            _token = null;
            _bearerToken = "";
            _accountName = "";
            _tokenExpiresAt = null;
            _isLoggedIn = false;
            _isBanned = false;
            _banReason = "";
        }

        private void Log(string message) => ServiceLogger.Log(_logger, nameof(AuthenticationService), message);

        public IEnumerator Login(System.Action<bool> onComplete = null)
        {
            if (_isLoggedIn && !string.IsNullOrEmpty(_bearerToken)
                && LoginResponse.CanReuseToken(_tokenExpiresAt, ServerClock.NowMs() / 1000))
            {
                Log("[Login] game token still valid, reused");
                onComplete?.Invoke(true);
                yield break;
            }

            string ticket = null;
            HAuthTicket handle = default;
            bool created;
            try
            {
                created = SteamSessionTicket.TryCreate(out ticket, out handle);
            }
            catch (Exception e)
            {
                Log("[Login] Steam ticket unavailable: " + e.Message);
                created = false;
            }
            if (!created)
            {
                Log("[Login] failed: no Steam session ticket");
                SetLoginState(false, "", false, "");
                onComplete?.Invoke(false);
                yield break;
            }

            ApiResponse response = null;
            try
            {
                var form = new Dictionary<string, string> { { "steamTicket", ticket } };
                for (int attempt = 1; attempt <= 2; attempt++)
                {
                    response = null;
                    yield return ApiService.PostFormWithErrorHandlingAsync(
                        _apiEndpoint + "/user/login",
                        form,
                        apiResponse => { response = apiResponse; },
                        unescape: false
                    );

                    // Steam may not know a ticket created a moment ago: send it once more
                    bool unknownTicket = response != null && response.StatusCode == 401
                        && PlayerApiResponse.ReadError(response.Content) == "Invalid ticket";
                    if (!unknownTicket || attempt == 2)
                    {
                        break;
                    }
                    Log("[Login] Steam ticket not accepted yet, retrying");
                    yield return new WaitForSeconds(GameConstants.STEAM_TICKET_RETRY_DELAY);
                }
            }
            finally
            {
                // A used ticket must not stay valid: anyone holding it could open a session
                try
                {
                    SteamSessionTicket.Cancel(handle);
                }
                catch (Exception e)
                {
                    Log("[Login] cancel Steam ticket failed: " + e.Message);
                }
            }

            if (response != null && response.IsSuccess && LoginResponse.TryParseTicketLogin(response.Content, out GLLoginResponse login))
            {
                Log("[Login] success");
                if (ServerClock.OffsetMs != 0)
                {
                    Log($"[Login] local clock differs from the server by {ServerClock.OffsetMs} ms; timestamps are corrected");
                }
                SetGameTokenLogin(login);
                onComplete?.Invoke(true);
                yield break;
            }

            string banReason = response?.StatusCode == 403 ? LoginResponse.ReadBanReason(response.Content) : null;
            if (banReason != null)
            {
                Log("[Login] User banned, reason: " + banReason);
                SetLoginState(false, "", true, banReason);
                onComplete?.Invoke(false);
                yield break;
            }

            string detail = response == null
                ? "no response"
                : $"HTTP {response.StatusCode} {PlayerApiResponse.ReadError(response.Content) ?? PlayerApiResponse.Snippet(response.Content, 100)}";
            Log($"[Login] failed: {detail}");
            SetLoginState(false, "", false, "");
            onComplete?.Invoke(false);
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
            return _isLoggedIn ? _accountName ?? "" : "";
        }

        public void SetLoginState(bool isLoggedIn, string token, bool isBanned, string banReason)
        {
            _isLoggedIn = isLoggedIn;
            _token = token ?? "";
            _bearerToken = "";
            _accountName = LoginResponse.AccountNameOfLegacyToken(_token);
            _tokenExpiresAt = null;
            _isBanned = isBanned;
            _banReason = banReason ?? "";
            _connectionState.Value = isLoggedIn ? "Yes" : "No";
        }

        private void SetGameTokenLogin(GLLoginResponse login)
        {
            _isLoggedIn = true;
            _token = login.token;
            _bearerToken = login.token;
            _accountName = login.username;
            _tokenExpiresAt = login.expiresAt;
            _isBanned = false;
            _banReason = "";
            _connectionState.Value = "Yes";
        }
    }
}

using BepInEx.Logging;
using GLMod.Class;
using GLMod.Constants;
using GLMod.GLEntities;
using GLMod.Services.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GLMod.Services.Implementations
{
    /// <summary>
    /// Keeps the room ticket of the player up to date.
    /// </summary>
    public class RoomTicketService : IRoomTicketService
    {
        private readonly ManualLogSource _logger;
        private readonly IAuthenticationService _authService;
        private readonly string _apiEndpoint;
        private GLRoomTicket _current;

        public string RoomCode => _current?.roomCode;

        public string Ticket
        {
            get
            {
                if (_current == null) return null;
                return _current.expiresAt - GameConstants.ROOM_TICKET_EXPIRY_MARGIN_SECONDS > ServerClock.NowMs() / 1000 ? _current.ticket : null;
            }
        }

        public RoomTicketService(ManualLogSource logger, IAuthenticationService authService, string apiEndpoint)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _apiEndpoint = apiEndpoint ?? throw new ArgumentNullException(nameof(apiEndpoint));
        }

        private void Log(string message) => ServiceLogger.Log(_logger, nameof(RoomTicketService), message);

        public IEnumerator Reload()
        {
            if (!_authService.IsLoggedIn || string.IsNullOrEmpty(_authService.BearerToken))
            {
                _current = null;
                yield break;
            }

            ApiResponse response = null;
            yield return ApiService.PostFormWithErrorHandlingAsync(
                _apiEndpoint + GameConstants.API_PATH_ROOM_TICKET,
                new Dictionary<string, string>(),
                result => response = result,
                _authService.BearerToken,
                unescape: false);

            if (response == null || !response.IsSuccess)
            {
                // An API without this endpoint answers 404: not an error worth more than a log line
                Log($"[Reload] no room ticket: {(response == null ? "no response" : "HTTP " + response.StatusCode)}");
                yield break;
            }

            if (!PlayerApiResponse.TryParse(response.Content, out GLRoomTicket ticket, out string error) || string.IsNullOrEmpty(ticket.ticket))
            {
                Log("[Reload] unreadable room ticket: " + (error ?? "no ticket"));
                yield break;
            }

            if (_current == null)
            {
                Log($"[Reload] room ticket received, account {(ticket.roomCode == null ? "without" : "with")} a room code");
            }
            _current = ticket;
        }

        public IEnumerator KeepFresh()
        {
            float nextReload = 0f;
            while (true)
            {
                yield return new WaitForSeconds(GameConstants.ROOM_TICKET_POLL_SECONDS);

                if (!_authService.IsLoggedIn)
                {
                    _current = null;
                    nextReload = 0f;
                    continue;
                }

                if (Time.realtimeSinceStartup < nextReload || IsConnected()) continue;

                nextReload = Time.realtimeSinceStartup + GameConstants.ROOM_TICKET_REFRESH_SECONDS;
                yield return Reload();
            }
        }

        private static bool IsConnected()
        {
            try
            {
                return AmongUsClient.Instance != null && AmongUsClient.Instance.GameState != InnerNet.InnerNetClient.GameStates.NotJoined;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}

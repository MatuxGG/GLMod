using BepInEx.Logging;
using GLMod.Class;
using GLMod.GLEntities;
using GLMod.Services.Interfaces;
using System;
using System.Collections;

namespace GLMod.Services.Implementations
{
    /// <summary>
    /// Reads a player's public data on Good Loss (sessions, last game)
    /// </summary>
    public class PlayerService : IPlayerService
    {
        private readonly ManualLogSource _logger;
        private readonly string _apiEndpoint;

        public PlayerService(ManualLogSource logger, string apiEndpoint)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _apiEndpoint = apiEndpoint ?? throw new ArgumentNullException(nameof(apiEndpoint));
        }

        private void Log(string message) => ServiceLogger.Log(_logger, nameof(PlayerService), message);

        public IEnumerator GetSessions(string username, Action<GLSessionList> onComplete, Action<string> onError = null)
        {
            const string endpoint = "/player/sessions";
            return Get(endpoint, username, onComplete, onError);
        }

        public IEnumerator GetLastGame(string username, Action<GLLastGame> onComplete, Action<string> onError = null)
        {
            const string endpoint = "/player/last-game";
            return Get(endpoint, username, onComplete, onError);
        }

        private IEnumerator Get<T>(string endpoint, string username, Action<T> onComplete, Action<string> onError) where T : class
        {
            if (string.IsNullOrEmpty(username))
            {
                onError?.Invoke("Not connected");
                yield break;
            }

            ApiResponse response = null;
            yield return ApiService.GetWithErrorHandlingAsync(
                _apiEndpoint + endpoint + "?username=" + Uri.EscapeDataString(username),
                result => response = result);

            if (response == null || !response.IsSuccess)
            {
                string error = PlayerApiResponse.ReadError(response?.Content)
                    ?? (response == null || response.StatusCode == 0 ? "Network error" : "HTTP " + response.StatusCode);
                Log($"[{endpoint}] failed (status {response?.StatusCode}): {error}");
                onError?.Invoke(error);
                yield break;
            }

            if (!PlayerApiResponse.TryParse(response.Content, out T value, out string parseError))
            {
                Log($"[{endpoint}] unreadable response ({parseError}): {PlayerApiResponse.Snippet(response.Content)}");
                onError?.Invoke("Unreadable response");
                yield break;
            }

            onComplete?.Invoke(value);
        }
    }
}

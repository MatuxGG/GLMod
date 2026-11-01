using BepInEx.Logging;
using GLMod.Class;
using GLMod.Constants;
using GLMod.GLEntities;
using GLMod.Services.Interfaces;
using InnerNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GLMod.Services.Implementations
{
    /// <summary>
    /// Friends list of the Good Loss account.
    /// </summary>
    public class FriendsService : IFriendsService
    {
        private const string NotificationRequest = "friends.notify.request";
        private const string NotificationOnline = "friends.notify.online";
        private const string ErrorJoinFailed = "friends.error.join";

        private readonly ManualLogSource _logger;
        private readonly IAuthenticationService _authService;
        private readonly IMenuService _menuService;
        private readonly string _apiEndpoint;
        private bool _syncing;

        public GLFriendsState State { get; private set; }
        public int PendingRequestCount => State?.incoming?.Count ?? 0;
        public bool IsAvailable { get; private set; } = true;
        public string Error { get; private set; }
        public IReadOnlyList<GLRecentPlayer> RecentPlayers { get; private set; }
        public FriendNotificationQueue Notifications { get; } = new FriendNotificationQueue();

        public FriendsService(ManualLogSource logger, IAuthenticationService authService, IMenuService menuService, string apiEndpoint)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _menuService = menuService ?? throw new ArgumentNullException(nameof(menuService));
            _apiEndpoint = apiEndpoint ?? throw new ArgumentNullException(nameof(apiEndpoint));
        }

        private void Log(string message) => ServiceLogger.Log(_logger, nameof(FriendsService), message);

        private bool CanCall => _authService.IsLoggedIn && !string.IsNullOrEmpty(_authService.BearerToken);

        public IEnumerator Sync()
        {
            if (!CanCall)
            {
                State = null;
                Error = null;
                yield break;
            }
            if (_syncing)
            {
                yield break;
            }

            _syncing = true;
            GamePresence presence = GamePresence.Read();
            var form = new Dictionary<string, string> { { "state", presence.State } };
            AddIfSet(form, "region", presence.Region);
            AddIfSet(form, "server", presence.Server);
            AddIfSet(form, "mod", GLMod.ConfigService?.ModName);
            AddIfSet(form, "gameCode", presence.GameCode);
            AddIfSet(form, "clientId", presence.ClientId?.ToString());
            AddIfSet(form, "playerName", presence.PlayerName);

            ApiResponse response = null;
            yield return Post(GameConstants.API_PATH_FRIENDS_SYNC, form, result => response = result);
            _syncing = false;

            if (response == null || !response.IsSuccess)
            {
                // An API without these endpoints answers 404: asked again later
                IsAvailable = response?.StatusCode != 404;
                Error = PlayerApiResponse.ReadError(response?.Content)
                    ?? (response == null || response.StatusCode == 0 ? "Network error" : "HTTP " + response.StatusCode);
                Log($"[Sync] failed: {(response == null ? "no response" : "HTTP " + response.StatusCode)}");
                yield break;
            }

            if (!PlayerApiResponse.TryParse(response.Content, out GLFriendsState state, out string error))
            {
                Log("[Sync] unreadable answer: " + error);
                Error = "Unreadable response";
                yield break;
            }

            IsAvailable = true;
            Error = null;
            Notify(State, state);
            State = state;
        }

        private static void AddIfSet(Dictionary<string, string> form, string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                form[key] = value;
            }
        }

        private void Notify(GLFriendsState previous, GLFriendsState current)
        {
            // A request answered or expired while its notification was waiting is no longer news
            Notifications.RetainRequests((current.incoming ?? new List<GLFriendRequest>()).Select(r => r.requestId));
            foreach (GLFriendRequest request in FriendsLogic.NewRequests(previous, current))
            {
                Notifications.EnqueueRequest(NotificationRequest, FriendsLogic.DisplayName(request.username, request.playerName), request.requestId);
            }

            // Not during a game, and not kept for later: it would no longer be news
            if (current.settings?.onlineAlert == true && !GamePresence.IsInGame())
            {
                foreach (GLFriend friend in FriendsLogic.NewlyOnline(previous, current))
                {
                    Notifications.EnqueueAlert(NotificationOnline,FriendsLogic.DisplayName(friend.username, friend.playerName));
                }
            }
        }

        public IEnumerator KeepInSync()
        {
            float next = 0f;
            while (true)
            {
                yield return new WaitForSeconds(1f);

                if (!CanCall)
                {
                    State = null;
                    Error = null;
                    next = 0f;
                    continue;
                }
                // A mod that hides the menu or removes the tab has no friends list: nothing is declared
                if (Time.realtimeSinceStartup < next || !_menuService.IsVisible || _menuService.FindTab(GameConstants.MENU_TAB_FRIENDS) == null)
                {
                    continue;
                }

                yield return Sync();
                next = Time.realtimeSinceStartup + (IsAvailable ? GameConstants.FRIENDS_SYNC_SECONDS : GameConstants.FRIENDS_UNAVAILABLE_RETRY_SECONDS);
            }
        }

        public IEnumerator ReloadRecentPlayers()
        {
            if (!CanCall)
            {
                yield break;
            }

            ApiResponse response = null;
            yield return Post(GameConstants.API_PATH_FRIENDS_RECENT, new Dictionary<string, string>(), result => response = result);
            if (response != null && response.IsSuccess
                && PlayerApiResponse.TryParse(response.Content, out GLRecentPlayers recent) && recent.players != null)
            {
                RecentPlayers = recent.players;
            }
        }

        public IEnumerator SendRequest(int userId, Action<string> onDone = null)
        {
            return Action(GameConstants.API_PATH_FRIENDS_REQUEST, new Dictionary<string, string> { { "userId", userId.ToString() } }, onDone);
        }

        public IEnumerator Answer(int requestId, bool accept, Action<string> onDone = null)
        {
            return Action(GameConstants.API_PATH_FRIENDS_ANSWER, new Dictionary<string, string>
            {
                { "requestId", requestId.ToString() },
                { "accept", accept ? "1" : "0" },
            }, onDone);
        }

        public IEnumerator Remove(int userId, Action<string> onDone = null)
        {
            return Action(GameConstants.API_PATH_FRIENDS_REMOVE, new Dictionary<string, string> { { "userId", userId.ToString() } }, onDone);
        }

        public IEnumerator SetBlocked(int userId, bool blocked, Action<string> onDone = null)
        {
            return Action(GameConstants.API_PATH_FRIENDS_BLOCK, new Dictionary<string, string>
            {
                { "userId", userId.ToString() },
                { "blocked", blocked ? "1" : "0" },
            }, onDone);
        }

        public IEnumerator UpdateSettings(bool? refuseRequests, bool? appearOffline, bool? onlineAlert, Action<string> onDone = null)
        {
            var form = new Dictionary<string, string>();
            AddFlag(form, "refuseRequests", refuseRequests);
            AddFlag(form, "appearOffline", appearOffline);
            AddFlag(form, "onlineAlert", onlineAlert);
            return Action(GameConstants.API_PATH_FRIENDS_SETTINGS, form, onDone);
        }

        private static void AddFlag(Dictionary<string, string> form, string key, bool? value)
        {
            if (value.HasValue)
            {
                form[key] = value.Value ? "1" : "0";
            }
        }

        /// <summary>
        /// Calls an endpoint that changes the list, then reads the list again.
        /// </summary>
        private IEnumerator Action(string path, Dictionary<string, string> form, Action<string> onDone)
        {
            if (!CanCall)
            {
                onDone?.Invoke("not_connected");
                yield break;
            }

            ApiResponse response = null;
            yield return Post(path, form, result => response = result);

            string error = null;
            if (response == null || !response.IsSuccess)
            {
                error = response == null ? "network" : PlayerApiResponse.ReadError(response.Content) ?? "HTTP " + response.StatusCode;
                Log($"[{path}] failed: {error}");
            }

            yield return Sync();
            onDone?.Invoke(error);
        }

        private IEnumerator Post(string path, Dictionary<string, string> form, Action<ApiResponse> onComplete)
        {
            return ApiService.PostFormWithErrorHandlingAsync(_apiEndpoint + path, form, onComplete, _authService.BearerToken, unescape: false);
        }

        public IEnumerator Join(GLFriend friend, Action<string> onDone = null)
        {
            if (friend == null || string.IsNullOrEmpty(friend.gameCode) || PlayModeHelper.IsFreePlay())
            {
                onDone?.Invoke(ErrorJoinFailed);
                yield break;
            }

            AmongUsClient client = AmongUsClient.Instance;
            bool inGame = client.GameState != InnerNetClient.GameStates.NotJoined;
            if (FriendsLogic.MustGoToMainMenu(inGame, SceneManager.GetActiveScene().name))
            {
                string failure = null;
                try
                {
                    // Like the Leave button of the game: the scene change is not part of ExitGame
                    if (inGame)
                    {
                        client.ExitGame(DisconnectReasons.ExitGame);
                    }
                    SceneChanger.ChangeScene(GameConstants.SCENE_MAIN_MENU);
                }
                catch (Exception e)
                {
                    failure = e.Message;
                }
                if (failure != null)
                {
                    Log("[Join] could not leave the game: " + failure);
                    onDone?.Invoke(ErrorJoinFailed);
                    yield break;
                }
            }

            float deadline = Time.realtimeSinceStartup + GameConstants.FRIEND_JOIN_LEAVE_TIMEOUT;
            while (!IsAtMainMenu())
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    Log("[Join] timeout while going back to the main menu");
                    onDone?.Invoke(ErrorJoinFailed);
                    yield break;
                }
                yield return new WaitForSeconds(0.2f);
            }
            // A mod may set its region while the main menu starts: the code only exists on the friend's server
            yield return new WaitForSeconds(1f);

            string joinError = null;
            try
            {
                if (DestroyableSingleton<ServerManager>.Instance.CurrentRegion?.Name != friend.region)
                {
                    joinError = ErrorJoinFailed;
                }
                else
                {
                    client = AmongUsClient.Instance;
                    client.NetworkMode = NetworkModes.OnlineGame;
                    client.MainMenuScene = GameConstants.SCENE_MAIN_MENU;
                    client.OnlineScene = GameConstants.SCENE_ONLINE_GAME;
                    client.StartCoroutine(client.CoJoinOnlineGameFromCode(GameCode.GameNameToInt(friend.gameCode), true));
                }
            }
            catch (Exception e)
            {
                Log("[Join] failed: " + e.Message);
                joinError = ErrorJoinFailed;
            }
            onDone?.Invoke(joinError);
        }

        private static bool IsAtMainMenu()
        {
            return AmongUsClient.Instance != null
                && AmongUsClient.Instance.GameState == InnerNetClient.GameStates.NotJoined
                && SceneManager.GetActiveScene().name == GameConstants.SCENE_MAIN_MENU;
        }
    }
}

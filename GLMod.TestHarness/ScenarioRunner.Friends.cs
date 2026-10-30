using GLMod.Class;
using GLMod.Constants;
using GLMod.GLEntities;
using InnerNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using UnityEngine;
using GLModPlugin = global::GLMod.GLMod;

namespace GLMod.TestHarness
{
    /// <summary>
    /// Friends scenario: 4 Good Loss accounts of preprod (app:e2e:friend-accounts) in one room.
    /// A (host) asks B, who accepts; asks C, who refuses; asks D, who refuses everything then blocks.
    /// B leaves, opens its own lobby, appears offline, then joins A with the Join action. A starts a game:
    /// C sends a request during it (badge, no notification), and B, gone to the menu, sees A in a game.
    /// </summary>
    public static partial class ScenarioRunner
    {
        private const int InstanceB = 1;
        private const int InstanceC = 2;
        private const int InstanceD = 3;

        private const string SignalAllSawRoom = "room-seen-{0}";
        private const string SignalAskedB = "asked-b";
        private const string SignalBAccepted = "b-accepted";
        private const string SignalAskedC = "asked-c";
        private const string SignalCRefused = "c-refused";
        private const string SignalDRefusing = "d-refusing";
        private const string SignalDRefusalChecked = "d-refusal-checked";
        private const string SignalDBlocked = "d-blocked";
        private const string SignalDBlockChecked = "d-block-checked";
        private const string SignalBAtMenu = "b-at-menu";
        private const string SignalMenuSeen = "menu-seen";
        private const string SignalBOwnLobby = "b-own-lobby";
        private const string SignalLobbySeen = "lobby-seen";
        private const string SignalBHidden = "b-hidden";
        private const string SignalHiddenSeen = "hidden-seen";
        private const string SignalBBack = "b-back";
        private const string SignalBackSeen = "back-seen";
        private const string SignalGameStarted = "game-started";
        private const string SignalCAskedInGame = "c-asked-in-game";
        private const string SignalInGameChecked = "in-game-checked";
        private const string SignalBLeftGame = "b-left-game";
        private const string SignalBSawGame = "b-saw-game";
        private const string SignalRemoved = "removed";
        private const string SignalRemoveChecked = "remove-checked";
        private const string SignalFriendsDone = "friends-done";

        private static List<HarnessAccount> _accounts;

        private static Services.Interfaces.IFriendsService Friends => GLModPlugin.FriendsService;

        private static int UserId(int instance) => _accounts[instance].userId;

        private static string Username(int instance) => _accounts[instance].username;

        private static GLFriend FriendOf(int instance) => Friends.State?.friends?.FirstOrDefault(f => f.userId == UserId(instance));

        /// <summary>
        /// Syncs with the site until the condition holds.
        /// </summary>
        private static IEnumerator WaitFriends(string what, Func<bool> condition)
        {
            float deadline = Time.realtimeSinceStartup + HarnessConstants.FRIENDS_STEP_TIMEOUT;
            while (!_finished)
            {
                yield return Friends.Sync();
                if (SafeEvaluate(condition)) yield break;
                if (Time.realtimeSinceStartup > deadline)
                {
                    Fail($"Timeout ({HarnessConstants.FRIENDS_STEP_TIMEOUT}s) waiting for: {what}");
                    yield break;
                }
                yield return new WaitForSeconds(HarnessConstants.POLL_INTERVAL);
            }
        }

        /// <summary>
        /// Runs an action of the friends service and gives its error (null on success).
        /// </summary>
        private static IEnumerator Do(Func<Action<string>, IEnumerator> action, Action<string> onDone)
        {
            string result = null;
            yield return action(error => result = error);
            onDone(result);
        }

        private static IEnumerator PlayFriendsScenario()
        {
            if (!Try("read the test accounts", LoadAccounts)) yield break;

            SetPhase("friends:login");
            yield return WaitFor("the test account logged in", () => GLModPlugin.AuthService.GetAccountName() == Username(_context.Instance), HarnessConstants.FRIENDS_STEP_TIMEOUT);
            if (_finished) yield break;

            switch (_context.Instance)
            {
                case HarnessConstants.HOST_INSTANCE: yield return FriendsHost(); break;
                case InstanceB: yield return FriendsB(); break;
                case InstanceC: yield return FriendsC(); break;
                case InstanceD: yield return FriendsD(); break;
            }
        }

        private static void LoadAccounts()
        {
            string path = Environment.GetEnvironmentVariable(HarnessConstants.ENV_ACCOUNTS);
            _accounts = JsonSerializer.Deserialize<List<HarnessAccount>>(File.ReadAllText(path));
        }

        private static IEnumerator FriendsJoinFirstTime(int previousInstance)
        {
            LobbyInfo lobby = null;
            yield return WaitFor("lobby code published by host", () => (lobby = TryLoadLobby(1)) != null, HarnessConstants.LOBBY_FILE_TIMEOUT);
            if (_finished) yield break;
            if (previousInstance != HarnessConstants.HOST_INSTANCE)
            {
                yield return WaitSignal(string.Format(SignalJoinedFormat, previousInstance));
                if (_finished) yield break;
            }
            yield return PluginJoin(lobby, "friends:join");
            if (_finished) yield break;
            Signal(string.Format(SignalJoinedFormat, _context.Instance), LocalName());
        }

        /// <summary>
        /// Every instance: the site must give the accounts of the 3 other players, matched by client id.
        /// </summary>
        private static IEnumerator FriendsSeeRoom()
        {
            SetPhase("friends:room");
            yield return WaitFriends("the 3 other accounts in the room", () =>
                Friends.State?.room?.Count == HarnessConstants.PLAYER_COUNT - 1
                && Friends.State.room.All(a => a.clientId.HasValue));
            if (_finished) yield break;

            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                if (player == PlayerControl.LocalPlayer) continue;
                GLRoomAccount account = Friends.State.room.FirstOrDefault(a => a.clientId == player.OwnerId);
                Check(account != null, $"no account for the player {player.Data?.PlayerName} (client {player.OwnerId})");
                if (account != null)
                {
                    Check(account.playerName == player.Data?.PlayerName, $"account {account.username} has the name {account.playerName}, not {player.Data?.PlayerName}");
                }
            }

            Signal(string.Format(SignalAllSawRoom, _context.Instance));
            for (int i = 0; i < HarnessConstants.PLAYER_COUNT; i++)
            {
                yield return WaitSignal(string.Format(SignalAllSawRoom, i));
                if (_finished) yield break;
            }
        }

        private static IEnumerator FriendsHost()
        {
            SetPhase("friends:create");
            if (!Try("create the online game", HostOnlineGame)) yield break;
            yield return WaitFor("host in lobby", IsInLobby, HarnessConstants.CONNECT_TIMEOUT);
            if (_finished) yield break;
            if (!Try("publish the lobby code", () => WriteLobbyFile(1))) yield break;

            yield return WaitFor($"{HarnessConstants.PLAYER_COUNT} players in lobby", IsLobbyFull, HarnessConstants.LOBBY_FULL_TIMEOUT);
            if (_finished) yield break;
            yield return FriendsSeeRoom();
            if (_finished) yield break;
            yield return CaptureFriendsTab("friends-room.png");

            // ---- request accepted
            SetPhase("friends:ask-b");
            string error = "not run";
            yield return Do(done => Friends.SendRequest(UserId(InstanceB), done), e => error = e);
            Check(error == null, $"the request to B failed: {error}");
            yield return WaitFriends("the request shown as sent", () => Friends.State.room.First(a => a.userId == UserId(InstanceB)).requested);
            if (_finished) yield break;
            Signal(SignalAskedB);

            yield return WaitSignal(SignalBAccepted);
            if (_finished) yield break;
            yield return WaitFriends("B in the friends list, in my room", () => FriendOf(InstanceB)?.status == GameConstants.FRIEND_STATUS_ROOM);
            if (_finished) yield break;
            Check(FriendOf(InstanceB).gameCode == null, "a friend in my room came with a game code");
            Check(FriendsLogic.DisplayName(FriendOf(InstanceB).username, FriendOf(InstanceB).playerName).Contains("("), "the name of a friend does not show the in-game name");

            // ---- request refused: silent, and no new one for 24 h
            SetPhase("friends:ask-c");
            yield return Do(done => Friends.SendRequest(UserId(InstanceC), done), e => error = e);
            Check(error == null, $"the request to C failed: {error}");
            Signal(SignalAskedC);
            yield return WaitSignal(SignalCRefused);
            if (_finished) yield break;
            yield return Friends.Sync();
            Check(Friends.State.outgoing.Any(r => r.userId == UserId(InstanceC)), "the refusal of C is not silent: the request disappeared");
            yield return Do(done => Friends.SendRequest(UserId(InstanceC), done), e => error = e);
            Check(error == "recently_refused", $"a new request right after a refusal answered: {error ?? "sent"}");

            // ---- refusing everything and blocking look the same
            SetPhase("friends:ask-d");
            yield return WaitSignal(SignalDRefusing);
            if (_finished) yield break;
            yield return Do(done => Friends.SendRequest(UserId(InstanceD), done), e => error = e);
            Check(error == "unavailable", $"a request to a player refusing everything answered: {error ?? "sent"}");
            Signal(SignalDRefusalChecked);
            yield return WaitSignal(SignalDBlocked);
            if (_finished) yield break;
            yield return Do(done => Friends.SendRequest(UserId(InstanceD), done), e => error = e);
            Check(error == "unavailable", $"a request to a player who blocked me answered: {error ?? "sent"}");
            Signal(SignalDBlockChecked);

            // ---- status of B
            SetPhase("friends:status");
            yield return WaitSignal(SignalBAtMenu);
            if (_finished) yield break;
            yield return WaitFriends("B online in the menu", () => FriendOf(InstanceB)?.status == GameConstants.FRIEND_STATUS_ONLINE);
            if (_finished) yield break;
            Signal(SignalMenuSeen);

            yield return WaitSignal(SignalBOwnLobby);
            if (_finished) yield break;
            yield return WaitFriends("B in its own lobby, can be joined", () => FriendOf(InstanceB)?.status == GameConstants.FRIEND_STATUS_LOBBY && FriendOf(InstanceB).canJoin);
            if (_finished) yield break;
            Check(FriendOf(InstanceB).region == _context.Region, $"region of B: {FriendOf(InstanceB).region}");
            Signal(SignalLobbySeen);

            yield return WaitSignal(SignalBHidden);
            if (_finished) yield break;
            yield return WaitFriends("B appearing offline", () => FriendOf(InstanceB)?.status == GameConstants.FRIEND_STATUS_OFFLINE);
            if (_finished) yield break;
            Check(FriendOf(InstanceB).lastSeen == null && FriendOf(InstanceB).region == null && !FriendOf(InstanceB).canJoin, "appearing offline still gives information");
            Signal(SignalHiddenSeen);

            // ---- Join
            yield return WaitSignal(SignalBBack);
            if (_finished) yield break;
            yield return WaitFor("B back in my lobby", IsLobbyFull, HarnessConstants.LOBBY_FULL_TIMEOUT);
            if (_finished) yield break;
            yield return WaitFriends("B in my room after Join", () => FriendOf(InstanceB)?.status == GameConstants.FRIEND_STATUS_ROOM);
            if (_finished) yield break;
            Signal(SignalBackSeen);

            // ---- during a game
            SetPhase("friends:game");
            yield return new WaitForSeconds(HarnessConstants.LOBBY_SETTLE_DELAY);
            yield return HostStartGame();
            if (_finished) yield break;
            yield return WaitFor("game started", IsGameStarted, HarnessConstants.GAME_START_TIMEOUT);
            if (_finished) yield break;
            yield return Friends.Sync();
            Signal(SignalGameStarted);

            yield return WaitSignal(SignalCAskedInGame);
            if (_finished) yield break;
            yield return WaitFriends("the request of C during the game", () => Friends.PendingRequestCount == 1);
            if (_finished) yield break;
            Check(GamePresence.IsInGame(), "the game is not running anymore");
            Check(Friends.Notifications.Current(Time.realtimeSinceStartup, GameConstants.FRIEND_NOTIFICATION_SECONDS, GamePresence.IsInGame()) == null,
                "a notification would appear during the game");
            Check(!GLModPlugin.MenuService.IsOpen, "the menu opened by itself");
            yield return new WaitForSeconds(HarnessConstants.PLUGIN_PANEL_DELAY);
            Try("take the screenshot", () => ScreenCapture.CaptureScreenshot(Path.Combine(_context.OutputDir, "chip-badge-in-game.png")));
            yield return new WaitForSeconds(HarnessConstants.PLUGIN_PANEL_DELAY);
            Signal(SignalInGameChecked);

            yield return WaitSignal(SignalBSawGame);
            if (_finished) yield break;

            // ---- removing a friend
            SetPhase("friends:remove");
            yield return Do(done => Friends.Remove(UserId(InstanceB), done), e => error = e);
            Check(error == null, $"removing B failed: {error}");
            Check(FriendOf(InstanceB) == null, "B is still in the friends list");
            Signal(SignalRemoved);
            yield return WaitSignal(SignalRemoveChecked);
            Signal(SignalFriendsDone);
        }

        private static IEnumerator FriendsB()
        {
            yield return FriendsJoinFirstTime(HarnessConstants.HOST_INSTANCE);
            if (_finished) yield break;
            yield return FriendsSeeRoom();
            if (_finished) yield break;

            SetPhase("friends:answer");
            yield return WaitSignal(SignalAskedB);
            if (_finished) yield break;
            yield return WaitFriends("the request of A", () => Friends.State.incoming.Any(r => r.userId == UserId(HarnessConstants.HOST_INSTANCE)));
            if (_finished) yield break;
            Check(Friends.PendingRequestCount == 1, $"badge: {Friends.PendingRequestCount} requests instead of 1");
            Check(Friends.Notifications.Count >= 1, "no notification for the request");

            // Notification on screen, in the lobby
            yield return new WaitForSeconds(HarnessConstants.PLUGIN_PANEL_DELAY);
            Try("take the screenshot", () => ScreenCapture.CaptureScreenshot(Path.Combine(_context.OutputDir, "notification-request.png")));
            yield return new WaitForSeconds(HarnessConstants.PLUGIN_PANEL_DELAY);
            yield return CaptureFriendsTab("friends-request.png");

            string error = "not run";
            int requestId = Friends.State.incoming.First(r => r.userId == UserId(HarnessConstants.HOST_INSTANCE)).requestId;
            yield return Do(done => Friends.Answer(requestId, true, done), e => error = e);
            Check(error == null, $"accepting failed: {error}");
            Check(Friends.PendingRequestCount == 0, "the request is still waiting after the answer");
            Check(FriendOf(HarnessConstants.HOST_INSTANCE)?.status == GameConstants.FRIEND_STATUS_ROOM, "A is not in my friends, in my room");
            Signal(SignalBAccepted);

            // ---- status
            yield return WaitSignal(SignalDBlockChecked);
            if (_finished) yield break;
            SetPhase("friends:leave");
            if (!Try("leave the game", () =>
            {
                AmongUsClient.Instance.ExitGame(DisconnectReasons.ExitGame);
                SceneChanger.ChangeScene(HarnessConstants.MAIN_MENU_SCENE);
            })) yield break;
            yield return WaitFor("back to the main menu", IsAtMainMenu, HarnessConstants.CONNECT_TIMEOUT);
            if (_finished) yield break;
            yield return new WaitForSeconds(HarnessConstants.LOBBY_SETTLE_DELAY);
            if (!Try("select the region", SelectRegion)) yield break;
            yield return Friends.Sync();
            Signal(SignalBAtMenu);
            yield return WaitSignal(SignalMenuSeen);
            if (_finished) yield break;

            SetPhase("friends:own-lobby");
            // Among Us Ranked removes the frame rate cap at each main menu: 4 games on one PC need it
            if (!Try("cap the frame rate", LimitFrameRate)) yield break;
            if (!Try("create an own game", HostOnlineGame)) yield break;
            yield return WaitFor("own lobby", IsInLobby, HarnessConstants.CONNECT_TIMEOUT);
            if (_finished) yield break;
            yield return Friends.Sync();
            Signal(SignalBOwnLobby);
            yield return WaitSignal(SignalLobbySeen);
            if (_finished) yield break;

            SetPhase("friends:hidden");
            yield return Do(done => Friends.UpdateSettings(null, true, null, done), e => error = e);
            Check(error == null, $"appearing offline failed: {error}");
            Signal(SignalBHidden);
            yield return WaitSignal(SignalHiddenSeen);
            if (_finished) yield break;
            yield return Do(done => Friends.UpdateSettings(null, false, null, done), e => error = e);

            // ---- Join, from a lobby: leaves it, comes back to the menu, joins
            SetPhase("friends:join-action");
            GLFriend host = null;
            yield return WaitFriends("A can be joined", () => (host = FriendOf(HarnessConstants.HOST_INSTANCE))?.canJoin == true);
            if (_finished) yield break;
            Check(!string.IsNullOrEmpty(host.gameCode), "a friend who can be joined came without a code");
            string expectedCode = TryLoadLobby(1)?.Code;
            Check(host.gameCode == expectedCode, $"the code to join is {host.gameCode}, not {expectedCode}");
            yield return CaptureFriendsTab("friends-join.png");

            yield return Do(done => Friends.Join(host, done), e => error = e);
            Check(error == null, $"Join failed: {error}");
            yield return WaitFor("in the lobby of A after Join", () => IsInLobby() && PlayModeHelper.CurrentOnlineGameCode() == expectedCode, HarnessConstants.CONNECT_TIMEOUT);
            if (_finished) yield break;
            yield return new WaitForSeconds(HarnessConstants.LOBBY_SETTLE_DELAY);
            yield return Friends.Sync();
            Signal(SignalBBack);
            yield return WaitSignal(SignalBackSeen);
            if (_finished) yield break;

            // ---- A in a game, seen from the menu
            SetPhase("friends:game");
            yield return WaitSignal(SignalGameStarted);
            if (_finished) yield break;
            yield return WaitFor("game started", IsGameStarted, HarnessConstants.GAME_START_TIMEOUT);
            if (_finished) yield break;
            yield return WaitSignal(SignalInGameChecked);
            if (_finished) yield break;

            if (!Try("leave the game", () =>
            {
                AmongUsClient.Instance.ExitGame(DisconnectReasons.ExitGame);
                SceneChanger.ChangeScene(HarnessConstants.MAIN_MENU_SCENE);
            })) yield break;
            yield return WaitFor("back to the main menu", IsAtMainMenu, HarnessConstants.CONNECT_TIMEOUT);
            if (_finished) yield break;
            yield return new WaitForSeconds(HarnessConstants.PLUGIN_CHAT_DELAY);
            if (!Try("select the region", SelectRegion)) yield break;
            yield return WaitFriends("A in a game", () => FriendOf(HarnessConstants.HOST_INSTANCE)?.status == GameConstants.FRIEND_STATUS_GAME);
            if (_finished) yield break;
            Check(!FriendOf(HarnessConstants.HOST_INSTANCE).canJoin && FriendOf(HarnessConstants.HOST_INSTANCE).gameCode == null, "a friend in a game can be joined");
            Signal(SignalBSawGame);

            SetPhase("friends:removed");
            yield return WaitSignal(SignalRemoved);
            if (_finished) yield break;
            yield return WaitFriends("removed from the friends of A", () => FriendOf(HarnessConstants.HOST_INSTANCE) == null);
            Signal(SignalRemoveChecked);
            yield return WaitSignal(SignalFriendsDone);
        }

        private static IEnumerator FriendsC()
        {
            yield return FriendsJoinFirstTime(InstanceB);
            if (_finished) yield break;
            yield return FriendsSeeRoom();
            if (_finished) yield break;

            SetPhase("friends:refuse");
            yield return WaitSignal(SignalAskedC);
            if (_finished) yield break;
            yield return WaitFriends("the request of A", () => Friends.State.incoming.Any(r => r.userId == UserId(HarnessConstants.HOST_INSTANCE)));
            if (_finished) yield break;

            string error = "not run";
            int requestId = Friends.State.incoming.First(r => r.userId == UserId(HarnessConstants.HOST_INSTANCE)).requestId;
            yield return Do(done => Friends.Answer(requestId, false, done), e => error = e);
            Check(error == null, $"refusing failed: {error}");
            Check(Friends.PendingRequestCount == 0, "the request is still waiting after the refusal");
            Check(FriendOf(HarnessConstants.HOST_INSTANCE) == null, "a refused player is a friend");
            Signal(SignalCRefused);

            SetPhase("friends:game");
            yield return WaitSignal(SignalGameStarted);
            if (_finished) yield break;
            yield return WaitFor("game started", IsGameStarted, HarnessConstants.GAME_START_TIMEOUT);
            if (_finished) yield break;
            yield return Do(done => Friends.SendRequest(UserId(HarnessConstants.HOST_INSTANCE), done), e => error = e);
            Check(error == null, $"a request during a game failed: {error}");
            Signal(SignalCAskedInGame);
            yield return WaitSignal(SignalFriendsDone);
        }

        private static IEnumerator FriendsD()
        {
            yield return FriendsJoinFirstTime(InstanceC);
            if (_finished) yield break;
            yield return FriendsSeeRoom();
            if (_finished) yield break;

            SetPhase("friends:refuse-all");
            yield return WaitSignal(SignalCRefused);
            if (_finished) yield break;
            string error = "not run";
            yield return Do(done => Friends.UpdateSettings(true, null, null, done), e => error = e);
            Check(error == null, $"refusing everything failed: {error}");
            Signal(SignalDRefusing);
            yield return WaitSignal(SignalDRefusalChecked);
            if (_finished) yield break;
            yield return Do(done => Friends.UpdateSettings(false, null, null, done), e => error = e);

            SetPhase("friends:block");
            yield return Do(done => Friends.SetBlocked(UserId(HarnessConstants.HOST_INSTANCE), true, done), e => error = e);
            Check(error == null, $"blocking failed: {error}");
            Check(Friends.State.blocked.Any(b => b.userId == UserId(HarnessConstants.HOST_INSTANCE)), "A is not in the blocked players");
            Signal(SignalDBlocked);
            yield return WaitSignal(SignalDBlockChecked);
            if (_finished) yield break;
            yield return Do(done => Friends.SetBlocked(UserId(HarnessConstants.HOST_INSTANCE), false, done), e => error = e);
            Check(error == null, $"unblocking failed: {error}");

            SetPhase("friends:wait");
            yield return WaitSignal(SignalFriendsDone);
        }

        private static IEnumerator CaptureFriendsTab(string fileName)
        {
            if (!Try("select the Friends tab", () => GLModPlugin.MenuService.Select(GameConstants.MENU_TAB_FRIENDS))) yield break;
            yield return CapturePanel(fileName);
        }
    }

    /// <summary>
    /// A line of the file given by the test script: an account of app:e2e:friend-accounts.
    /// </summary>
    public sealed class HarnessAccount
    {
        public string username { get; set; }
        public int userId { get; set; }
        public string token { get; set; }
        public long expiresAt { get; set; }
    }
}

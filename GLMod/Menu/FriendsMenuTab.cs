using GLMod.Class;
using GLMod.Constants;
using GLMod.GLEntities;
using GLMod.Services.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GLMod.Menu
{
    /// <summary>
    /// Friends of the Good Loss account: requests, players of the room and of the last games, friends
    /// with their status, blocked players, settings. Names are "Account (in-game name)".
    /// </summary>
    public class FriendsMenuTab : IMenuTab
    {
        private const float RowHeight = 46f;
        private const float RowButtonWidth = 74f;
        private const float RowButtonGap = 6f;
        private const float RowButtonLine = 28f;

        private readonly IFriendsService _friends;
        private readonly IAuthenticationService _authService;
        private readonly ITranslationService _t;
        private bool _recentRequested;
        private bool _busy;
        private string _message;

        public string Id => GameConstants.MENU_TAB_FRIENDS;

        public string Title
        {
            get
            {
                int pending = _friends.PendingRequestCount;
                return pending > 0 ? $"{_t.Get("tab.friends")} ({pending})" : _t.Get("tab.friends");
            }
        }

        public FriendsMenuTab(IFriendsService friends, IAuthenticationService authService, ITranslationService translations)
        {
            _friends = friends ?? throw new ArgumentNullException(nameof(friends));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _t = translations ?? throw new ArgumentNullException(nameof(translations));
        }

        public void OnShown()
        {
            _message = null;
            if (_authService.IsLoggedIn)
            {
                Run(_friends.Sync());
                if (!_recentRequested)
                {
                    _recentRequested = true;
                    Run(_friends.ReloadRecentPlayers());
                }
            }
        }

        private static void Run(IEnumerator routine)
        {
            CoroutineRunner.Run(routine);
        }

        private void RunAction(IEnumerator routine)
        {
            _busy = true;
            Run(Finish(routine));
        }

        private IEnumerator Finish(IEnumerator routine)
        {
            yield return routine;
            _busy = false;
        }

        private void Report(string error)
        {
            _message = error == null ? null : _t.Get(FriendsLogic.ErrorKey(error, _t.Has));
        }

        public void Draw()
        {
            if (!_authService.IsLoggedIn)
            {
                MenuUI.Heading(_t.Get("friends.list"));
                MenuUI.EmptyState(_t.Get("state.offline"), _t.Get("friends.offline"));
                return;
            }

            GLFriendsState state = _friends.State;
            if (state == null)
            {
                MenuUI.Heading(_t.Get("friends.list"));
                MenuUI.EmptyState(_friends.IsAvailable ? _t.Get("state.loading") : _t.Get("state.unavailable"), _t.Get("friends.loading"));
                return;
            }

            if (_message != null)
            {
                MenuUI.Label(_message, MenuUI.Danger);
                MenuUI.Space(4f);
            }

            DrawIncoming(state);
            DrawFriends(state);
            DrawRoom(state);
            DrawRecent(state);
            DrawBlocked(state);
            DrawSettings(state);
        }

        // ------------------------------------------------------------------ sections

        private void DrawIncoming(GLFriendsState state)
        {
            if (state.incoming == null || state.incoming.Count == 0)
            {
                return;
            }

            MenuUI.Heading(_t.Get("friends.incoming"));
            foreach (GLFriendRequest request in state.incoming)
            {
                DrawRow(FriendsLogic.DisplayName(request.username, request.playerName), null, MenuUI.Accent,
                    (_t.Get("friends.accept"), () => RunAction(_friends.Answer(request.requestId, true, Report))),
                    (_t.Get("friends.refuse"), () => RunAction(_friends.Answer(request.requestId, false, Report))));
            }
            MenuUI.Space(6f);
        }

        private void DrawFriends(GLFriendsState state)
        {
            MenuUI.Heading(_t.Get("friends.list"));
            List<GLFriend> friends = FriendsLogic.Ordered(state.friends);
            if (friends.Count == 0)
            {
                MenuUI.Muted(_t.Get("friends.empty"));
                MenuUI.Space(6f);
                return;
            }

            foreach (GLFriend friend in friends)
            {
                var buttons = new List<(string, Action)>();
                if (friend.canJoin)
                {
                    GLFriend target = friend;
                    buttons.Add((_t.Get("friends.join"), () => RunAction(_friends.Join(target, Report))));
                }
                buttons.Add((_t.Get("friends.remove"), () => RunAction(_friends.Remove(friend.userId, Report))));
                buttons.Add((_t.Get("friends.block"), () => RunAction(_friends.SetBlocked(friend.userId, true, Report))));
                DrawRow(FriendsLogic.DisplayName(friend.username, friend.playerName), StatusText(friend), StatusColor(friend.status), buttons.ToArray());
            }
            MenuUI.Space(6f);
        }

        private string StatusText(GLFriend friend)
        {
            switch (friend.status)
            {
                case GameConstants.FRIEND_STATUS_ROOM:
                    return _t.Get("friends.status.room");
                case GameConstants.FRIEND_STATUS_LOBBY:
                    return _t.Get("friends.status.lobby", friend.region ?? "");
                case GameConstants.FRIEND_STATUS_GAME:
                    return _t.Get("friends.status.game", friend.region ?? "");
                case GameConstants.FRIEND_STATUS_ONLINE:
                    return _t.Get("friends.status.online");
                default:
                    if (friend.lastSeen.HasValue)
                    {
                        (string key, long value) = FriendsLogic.SeenAgo(friend.lastSeen.Value, ServerClock.NowMs() / 1000);
                        return _t.Get(key, value);
                    }
                    return _t.Get("friends.status.offline");
            }
        }

        private static Color StatusColor(string status)
        {
            switch (status)
            {
                case GameConstants.FRIEND_STATUS_ROOM: return MenuUI.Accent;
                case GameConstants.FRIEND_STATUS_LOBBY: return MenuUI.Success;
                case GameConstants.FRIEND_STATUS_ONLINE: return MenuUI.Success;
                case GameConstants.FRIEND_STATUS_GAME: return MenuUI.Warning;
                default: return MenuUI.Border;
            }
        }

        /// <summary>
        /// The players of the game, with their account when they have GLMod and are logged in.
        /// </summary>
        private void DrawRoom(GLFriendsState state)
        {
            List<PlayerControl> players = OtherPlayers();
            if (players.Count == 0)
            {
                return;
            }

            MenuUI.Heading(_t.Get("friends.room"));
            var askedMe = new HashSet<int>((state.incoming ?? new List<GLFriendRequest>()).Select(r => r.userId));
            foreach (PlayerControl player in players)
            {
                string name = player.Data?.PlayerName ?? "";
                GLRoomAccount account = state.room?.FirstOrDefault(a => a.clientId == player.OwnerId);
                if (account == null)
                {
                    DrawRow(name, _t.Get("friends.noAccount"), MenuUI.Border);
                }
                else if (account.isFriend)
                {
                    DrawRow(FriendsLogic.DisplayName(account.username, name), _t.Get("friends.alreadyFriend"), MenuUI.Accent);
                }
                else if (askedMe.Contains(account.userId))
                {
                    DrawRow(FriendsLogic.DisplayName(account.username, name), _t.Get("friends.requestReceived"), MenuUI.Accent);
                }
                else if (account.requested)
                {
                    DrawRow(FriendsLogic.DisplayName(account.username, name), _t.Get("friends.requested"), MenuUI.Border);
                }
                else
                {
                    DrawRow(FriendsLogic.DisplayName(account.username, name), null, MenuUI.Border,
                        (_t.Get("friends.add"), () => RunAction(_friends.SendRequest(account.userId, Report))));
                }
            }
            MenuUI.Space(6f);
        }

        private static List<PlayerControl> OtherPlayers()
        {
            var players = new List<PlayerControl>();
            // Elsewhere the site has no room list: every player would wrongly show as "no account"
            if (!GoodLossServers.IsInRoomWithList() || PlayerControl.LocalPlayer == null)
            {
                return players;
            }
            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                if (player != null && player != PlayerControl.LocalPlayer && player.Data != null && !player.Data.Disconnected)
                {
                    players.Add(player);
                }
            }
            return players;
        }

        private void DrawRecent(GLFriendsState state)
        {
            if (_friends.RecentPlayers == null)
            {
                return;
            }

            var inRoom = new HashSet<int>((state.room ?? new List<GLRoomAccount>()).Select(a => a.userId));
            var requested = new HashSet<int>((state.outgoing ?? new List<GLFriendRequest>()).Select(r => r.userId));
            var friends = new HashSet<int>((state.friends ?? new List<GLFriend>()).Select(f => f.userId));
            var blocked = new HashSet<int>((state.blocked ?? new List<GLBlockedPlayer>()).Select(b => b.userId));
            var askedMe = new HashSet<int>((state.incoming ?? new List<GLFriendRequest>()).Select(r => r.userId));
            List<GLRecentPlayer> players = _friends.RecentPlayers
                .Where(p => !inRoom.Contains(p.userId) && !friends.Contains(p.userId) && !blocked.Contains(p.userId) && !askedMe.Contains(p.userId))
                .ToList();
            if (players.Count == 0)
            {
                return;
            }

            MenuUI.Heading(_t.Get("friends.recent"));
            foreach (GLRecentPlayer player in players)
            {
                string name = FriendsLogic.DisplayName(player.username, player.playerName);
                if (requested.Contains(player.userId))
                {
                    DrawRow(name, _t.Get("friends.requested"), MenuUI.Border);
                }
                else
                {
                    DrawRow(name, null, MenuUI.Border,
                        (_t.Get("friends.add"), () => RunAction(_friends.SendRequest(player.userId, Report))));
                }
            }
            MenuUI.Space(6f);
        }

        private void DrawBlocked(GLFriendsState state)
        {
            if (state.blocked == null || state.blocked.Count == 0)
            {
                return;
            }

            MenuUI.Heading(_t.Get("friends.blocked"));
            foreach (GLBlockedPlayer player in state.blocked)
            {
                DrawRow(player.username, null, MenuUI.Border,
                    (_t.Get("friends.unblock"), () => RunAction(_friends.SetBlocked(player.userId, false, Report))));
            }
            MenuUI.Space(6f);
        }

        private void DrawSettings(GLFriendsState state)
        {
            GLFriendSettings settings = state.settings;
            if (settings == null)
            {
                return;
            }

            MenuUI.Heading(_t.Get("friends.settings"));
            DrawSetting(_t.Get("friends.setting.refuse"), settings.refuseRequests, value => _friends.UpdateSettings(value, null, null, Report));
            DrawSetting(_t.Get("friends.setting.offline"), settings.appearOffline, value => _friends.UpdateSettings(null, value, null, Report));
            DrawSetting(_t.Get("friends.setting.alert"), settings.onlineAlert, value => _friends.UpdateSettings(null, null, value, Report));
        }

        private void DrawSetting(string label, bool value, Func<bool, IEnumerator> change)
        {
            DrawRow(label, null, value ? MenuUI.Accent : MenuUI.Border,
                (_t.Get(value ? "friends.yes" : "friends.no"), () => RunAction(change(!value))));
        }

        // ------------------------------------------------------------------ row

        private void DrawRow(string name, string detail, Color edge, params (string text, Action action)[] buttons)
        {
            // Three buttons leave no room for "Account (in-game name)": they go on a line of their own
            bool buttonLine = buttons.Length > 2;
            float height = buttonLine ? RowHeight + RowButtonLine : detail == null ? RowHeight - 12f : RowHeight;
            Rect row = MenuUI.Reserve(height, 4f);
            MenuUI.DrawRect(row, MenuUI.IsHover(row) ? MenuUI.SurfaceHover : MenuUI.Surface, MenuUI.Corner.Medium);
            MenuUI.DrawRect(new Rect(row.x, row.y + MenuUI.U(8f), MenuUI.Px(3f), row.height - MenuUI.U(16f)), edge, MenuUI.Corner.None);

            float buttonWidth = MenuUI.U(RowButtonWidth);
            float gap = MenuUI.U(RowButtonGap);
            float buttonsWidth = buttons.Length * buttonWidth + Math.Max(0, buttons.Length - 1) * gap;
            float x = row.x + MenuUI.U(12f);
            float textWidth = row.width - MenuUI.U(20f) - (buttonLine ? 0f : buttonsWidth);

            if (detail == null)
            {
                MenuUI.DrawText(new Rect(x, row.y, textWidth, row.height), name, MenuUI.RowStyle, MenuUI.Text);
            }
            else
            {
                MenuUI.DrawText(new Rect(x, row.y + MenuUI.U(5f), textWidth, MenuUI.U(20f)), name, MenuUI.RowStyle, MenuUI.Text);
                MenuUI.DrawText(new Rect(x, row.y + MenuUI.U(24f), textWidth, MenuUI.U(18f)), detail, MenuUI.CaptionStyle, MenuUI.MutedText);
            }

            float buttonX = row.xMax - MenuUI.U(6f) - buttonsWidth;
            float buttonHeight = MenuUI.U(RowHeight - 12f) - MenuUI.U(10f);
            float y = buttonLine ? row.yMax - buttonHeight - MenuUI.U(6f) : row.y + (row.height - buttonHeight) / 2f;
            foreach ((string text, Action action) in buttons)
            {
                if (MenuUI.DrawSecondaryButton(new Rect(buttonX, y, buttonWidth, buttonHeight), text) && !_busy)
                {
                    action();
                }
                buttonX += buttonWidth + gap;
            }
        }
    }
}

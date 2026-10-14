using GLMod.Class;
using GLMod.Constants;
using GLMod.GLEntities;
using GLMod.Services.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GLMod.Menu
{
    /// <summary>
    /// Last finished game of the connected account and all the stats Good Loss computed for it
    /// (GET /player/last-game). When a game ends, the tab is selected for the next opening and
    /// polls until Good Loss returns that game.
    /// </summary>
    public class LastGameMenuTab : IMenuTab
    {
        private readonly IAuthenticationService _authService;
        private readonly RemoteData<GLLastGame> _lastGame;
        private readonly ITranslationService _t;

        // Game id recorded locally at the end of the last game, until Good Loss returns it
        private string _awaitedGameId;
        private DateTime _awaitedUntil = DateTime.MinValue;

        public string Id => GameConstants.MENU_TAB_LAST_GAME;

        public string Title => _t.Get("tab.lastgame");

        public LastGameMenuTab(IPlayerService playerService, IAuthenticationService authService, IGameHistoryService historyService, IMenuService menuService, ITranslationService translations)
        {
            if (playerService == null)
            {
                throw new ArgumentNullException(nameof(playerService));
            }
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _t = translations ?? throw new ArgumentNullException(nameof(translations));
            _lastGame = new RemoteData<GLLastGame>(playerService.GetLastGame);

            if (historyService != null)
            {
                historyService.EntryRecorded += entry =>
                {
                    if (menuService != null && !menuService.IsOpen)
                    {
                        menuService.Select(this);
                    }
                    _lastGame.Invalidate();
                    // No polling when a mod hid the menu or removed this tab
                    bool registered = menuService == null || (menuService.Enabled && menuService.FindTab(Id) == this);
                    if (registered && !string.IsNullOrEmpty(entry?.gameId) && _authService.IsLoggedIn)
                    {
                        bool alreadyPolling = IsAwaiting;
                        _awaitedGameId = entry.gameId;
                        _awaitedUntil = DateTime.UtcNow.AddSeconds(GameConstants.LAST_GAME_WAIT_SECONDS);
                        if (!alreadyPolling)
                        {
                            CoroutineRunner.Run(WaitForGame());
                        }
                    }
                };
            }
        }

        private bool IsAwaiting => _awaitedGameId != null && DateTime.UtcNow < _awaitedUntil;

        public void OnShown()
        {
            if (_authService.IsLoggedIn)
            {
                _lastGame.Refresh(_authService.GetAccountName());
            }
        }

        public void Draw()
        {
            if (!_authService.IsLoggedIn)
            {
                MenuUI.EmptyState(_t.Get("state.offline"), _t.Get("lastgame.offline"));
                return;
            }

            GLLastGame data = _lastGame.ValueFor(_authService.GetAccountName());
            if (data == null)
            {
                if (_lastGame.Error != null && !_lastGame.Loading)
                {
                    MenuUI.EmptyState(_t.Get("state.unavailable"), _t.Get("lastgame.error", _lastGame.Error));
                    if (MenuUI.Button(_t.Get("state.retry")))
                    {
                        _lastGame.Refresh(_authService.GetAccountName(), true);
                    }
                }
                else
                {
                    MenuUI.EmptyState(_t.Get("state.loading"), _t.Get("lastgame.loading"));
                }
                return;
            }

            if (data.game == null || data.player == null)
            {
                MenuUI.EmptyState(_t.Get("lastgame.empty.title"), _t.Get("lastgame.empty.text"));
                return;
            }

            if (IsAwaiting)
            {
                MenuUI.Muted(_t.Get("lastgame.processing"));
            }

            GLLastGameInfo game = data.game;
            GLLastGamePlayer player = data.player;
            DrawGameCard(game, player);

            // Yes / no facts: only the ones that happened, as tags
            var tags = new List<(string text, Color color)>();
            AddTag(tags, game.ranked, _t.Get("lastgame.tag.ranked"), MenuUI.Accent);
            AddTag(tags, player.firstBlood, _t.Get("lastgame.tag.firstBlood"), MenuUI.Success);
            AddTag(tags, player.killedFirst, _t.Get("lastgame.tag.killedFirst"), MenuUI.Accent);
            AddTag(tags, player.firstVictim, _t.Get("lastgame.tag.firstVictim"), MenuUI.Danger);
            AddTag(tags, player.killed, _t.Get("lastgame.tag.killed"), MenuUI.Danger);
            AddTag(tags, player.exiled, _t.Get("lastgame.tag.exiled"), MenuUI.Danger);
            MenuUI.Tags(tags.ToArray());

            if (!string.IsNullOrEmpty(game.url) && MenuUI.Button(_t.Get("lastgame.viewRecap")))
            {
                Application.OpenURL(game.url);
            }

            // Every numeric value, in a compact grid (null values are skipped)
            MenuUI.StatGrid(3,
                (_t.Get("lastgame.stat.duration"), StatFormat.Duration(game.duration)),
                (_t.Get("lastgame.stat.turns"), StatFormat.Number(game.turns)),
                (_t.Get("lastgame.stat.meetings"), StatFormat.Number(game.meetings)),
                (_t.Get("lastgame.stat.players"), StatFormat.Number(game.players)),
                (_t.Get("lastgame.stat.rp"), StatFormat.Number(player.rp)),
                (_t.Get("lastgame.stat.kills"), StatFormat.Number(player.kills)),
                (_t.Get("lastgame.stat.doubleKills"), StatFormat.Number(player.doubleKills)),
                (_t.Get("lastgame.stat.timeToFirstKill"), StatFormat.Duration(player.timeToFirstKill)),
                (_t.Get("lastgame.stat.tasks"), StatFormat.Ratio(player.tasks, player.tasksMax)),
                (_t.Get("lastgame.stat.taskSpeed"), StatFormat.Number(player.taskSpeed)),
                (_t.Get("lastgame.stat.deathTurn"), StatFormat.Number(player.deathTurn)),
                (_t.Get("lastgame.stat.exileTurn"), StatFormat.Number(player.exileTurn)),
                (_t.Get("lastgame.stat.aliveDuration"), StatFormat.Duration(player.aliveDuration)),
                (_t.Get("lastgame.stat.aliveTurns"), StatFormat.Number(player.aliveTurns)),
                (_t.Get("lastgame.stat.aliveMeetings"), StatFormat.Number(player.aliveMeetings)),
                (_t.Get("lastgame.stat.goodVotes"), StatFormat.Ratio(player.goodVotes, player.totalVotes)),
                (_t.Get("lastgame.stat.keyVotes"), StatFormat.Ratio(player.goodVotesImportantMeetings, player.totalVotesImportantMeetings)),
                (_t.Get("lastgame.stat.skips"), StatFormat.Number(player.skips)),
                (_t.Get("lastgame.stat.votesReceived"), StatFormat.Number(player.votesReceived)),
                (_t.Get("lastgame.stat.teammateVotes"), StatFormat.Number(player.teammateVotes)),
                (_t.Get("lastgame.stat.voteOrder"), StatFormat.Number(player.voteOrder)),
                (_t.Get("lastgame.stat.voteDelay"), StatFormat.Duration(player.voteDelay)),
                (_t.Get("lastgame.stat.bodyReported"), StatFormat.Number(player.bodyReported)),
                (_t.Get("lastgame.stat.selfReports"), StatFormat.Number(player.selfReports)),
                (_t.Get("lastgame.stat.reportDelay"), StatFormat.Duration(player.reportDelay)),
                (_t.Get("lastgame.stat.emergencies"), StatFormat.Number(player.emergencyCalled)));
        }

        /// <summary>
        /// Vanilla role in the menu language ("role.&lt;name&gt;" key), other roles (mods) as sent by the API
        /// </summary>
        private string RoleName(string role, string team)
        {
            string name = StatFormat.Role(role, team);
            string key = "role." + name.Replace(" ", "");
            return _t.Has(key) ? _t.Get(key) : name;
        }

        private static void AddTag(List<(string text, Color color)> tags, bool? value, string text, Color color)
        {
            if (value == true)
            {
                tags.Add((text, color));
            }
        }

        private void DrawGameCard(GLLastGameInfo game, GLLastGamePlayer player)
        {
            Color result = player.win ? MenuUI.Success : MenuUI.Danger;
            Rect card = MenuUI.Reserve(108f);
            MenuUI.DrawRect(card, MenuUI.Surface, MenuUI.Corner.Medium);
            MenuUI.DrawRect(card, MenuUI.WithAlpha(result, 0.07f), MenuUI.Corner.Medium);

            var edge = new Rect(card.x, card.y + MenuUI.U(12f), MenuUI.Px(4f), card.height - MenuUI.U(24f));
            MenuUI.DrawGlow(edge, MenuUI.WithAlpha(result, 0.35f));
            MenuUI.DrawRect(edge, result, MenuUI.Corner.None);

            float x = card.x + MenuUI.U(20f);
            float width = card.width - MenuUI.U(36f);
            MenuUI.DrawText(new Rect(x, card.y + MenuUI.U(8f), width, MenuUI.U(36f)), _t.Get(player.win ? "lastgame.victory" : "lastgame.defeat"), MenuUI.ResultStyle, result);
            MenuUI.DrawText(new Rect(x, card.y + MenuUI.U(12f), width, MenuUI.U(18f)), StatFormat.LocalDateTime(game.end ?? game.start ?? 0), MenuUI.CaptionStyle, MenuUI.MutedText, TextAnchor.MiddleRight);

            // "as <name>  ·  <mod>  ·  S<season>"
            string subtitle = _t.Get("lastgame.as", player.name);
            if (!string.IsNullOrEmpty(game.mod))
            {
                subtitle += "  ·  " + game.mod;
            }
            if (game.season.HasValue)
            {
                subtitle += "  ·  " + _t.Get("lastgame.season", game.season.Value);
            }
            MenuUI.DrawText(new Rect(x, card.y + MenuUI.U(42f), width, MenuUI.U(20f)), subtitle, MenuUI.RowStyle, MenuUI.MutedText);

            float pillY = card.yMax - MenuUI.U(34f);
            foreach (string value in new[] { game.map, RoleName(player.role, player.team) })
            {
                if (!string.IsNullOrEmpty(value))
                {
                    x += MenuUI.DrawPill(x, pillY, value.ToUpperInvariant(), MenuUI.Text, MenuUI.SurfaceHover) + MenuUI.U(6f);
                }
            }
        }

        /// <summary>
        /// After a game ends, reloads every LAST_GAME_POLL_SECONDS until Good Loss returns it
        /// (the host uploads the end of the game a few seconds later) or the wait expires
        /// </summary>
        private IEnumerator WaitForGame()
        {
            while (IsAwaiting)
            {
                if (!_lastGame.Loading && _authService.IsLoggedIn)
                {
                    _lastGame.Refresh(_authService.GetAccountName(), true);
                }
                yield return new WaitForSeconds(GameConstants.LAST_GAME_POLL_SECONDS);

                GLLastGame data = _lastGame.ValueFor(_authService.GetAccountName());
                if (data?.game != null && data.game.id.ToString() == _awaitedGameId)
                {
                    _awaitedGameId = null;
                }
            }
            _awaitedGameId = null;
        }
    }
}

using GLMod.Class;
using GLMod.Constants;
using GLMod.GLEntities;
using GLMod.Services.Interfaces;
using System;
using UnityEngine;

namespace GLMod.Menu
{
    /// <summary>
    /// Personal win statistics of the connected account (GET /player/stats): games, then crewmate,
    /// impostor and neutral games, like the profile page of the site. Refreshed after each game.
    /// </summary>
    public class StatsMenuTab : IMenuTab
    {
        // Bar colors of the profile page of the site
        private static readonly Color CrewmateColor = new Color { r = 0.29f, g = 0.87f, b = 0.50f, a = 1f };
        private static readonly Color NeutralColor = new Color { r = 0.66f, g = 0.33f, b = 0.97f, a = 1f };

        private readonly IAuthenticationService _authService;
        private readonly RemoteData<GLPlayerStats> _stats;
        private readonly ITranslationService _t;

        public string Id => GameConstants.MENU_TAB_STATS;

        public string Title => _t.Get("tab.stats");

        public StatsMenuTab(IPlayerService playerService, IAuthenticationService authService, IGameHistoryService historyService, ITranslationService translations)
        {
            if (playerService == null)
            {
                throw new ArgumentNullException(nameof(playerService));
            }
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _t = translations ?? throw new ArgumentNullException(nameof(translations));
            _stats = new RemoteData<GLPlayerStats>((user, theme, ok, error) => playerService.GetStats(user, ok, error, theme));

            // A finished game changes the statistics
            if (historyService != null)
            {
                historyService.EntryRecorded += _ => _stats.Invalidate();
            }
        }

        public void OnShown()
        {
            if (_authService.IsLoggedIn)
            {
                _stats.Refresh(_authService.GetAccountName());
            }
        }

        public void Draw()
        {
            MenuUI.Heading(_t.Get("stats.heading"));
            if (!_authService.IsLoggedIn)
            {
                MenuUI.EmptyState(_t.Get("state.offline"), _t.Get("stats.offline"));
                return;
            }

            GLPlayerStats data = _stats.ValueFor(_authService.GetAccountName());
            if (data == null)
            {
                if (_stats.Error != null && !_stats.Loading)
                {
                    MenuUI.EmptyState(_t.Get("state.unavailable"), _t.Get("stats.error", _stats.Error));
                    if (MenuUI.Button(_t.Get("state.retry")))
                    {
                        _stats.Refresh(_authService.GetAccountName(), true);
                    }
                }
                else
                {
                    MenuUI.EmptyState(_t.Get("state.loading"), _t.Get("stats.loading"));
                }
                return;
            }

            GLWinStats stats = data.stats;
            if (stats?.games == null || stats.games.games == 0)
            {
                MenuUI.EmptyState(_t.Get("stats.empty.title"), _t.Get("stats.empty.text"));
                return;
            }

            DrawWinRate("stats.games", stats.games, MenuUI.Accent);
            DrawWinRate("stats.crewmate", stats.crewmate, CrewmateColor);
            DrawWinRate("stats.impostor", stats.impostor, MenuUI.Danger);
            DrawWinRate("stats.neutral", stats.neutral, NeutralColor);
        }

        private void DrawWinRate(string labelKey, GLWinCount count, Color color)
        {
            int wins = count?.wins ?? 0;
            int games = count?.games ?? 0;
            MenuUI.WinRate(_t.Get(labelKey), _t.Get("stats.counts", wins, games), StatFormat.WinPercent(wins, games), color);
        }
    }
}

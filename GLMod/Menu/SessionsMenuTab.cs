using GLMod.Class;
using GLMod.Constants;
using GLMod.GLEntities;
using GLMod.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GLMod.Menu
{
    /// <summary>
    /// Last sessions of the connected account (GET /player/sessions), with a link to each session page.
    /// </summary>
    public class SessionsMenuTab : IMenuTab
    {
        private readonly IAuthenticationService _authService;
        private readonly RemoteData<GLSessionList> _sessions;
        private readonly ITranslationService _t;

        public string Id => GameConstants.MENU_TAB_SESSIONS;

        public string Title => _t.Get("tab.sessions");

        public SessionsMenuTab(IPlayerService playerService, IAuthenticationService authService, IGameHistoryService historyService, ITranslationService translations)
        {
            if (playerService == null)
            {
                throw new ArgumentNullException(nameof(playerService));
            }
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _t = translations ?? throw new ArgumentNullException(nameof(translations));
            _sessions = new RemoteData<GLSessionList>((user, theme, ok, error) => playerService.GetSessions(user, ok, error, theme));

            // A finished game changes the current session
            if (historyService != null)
            {
                historyService.EntryRecorded += _ => _sessions.Invalidate();
            }
        }

        public void OnShown()
        {
            if (_authService.IsLoggedIn)
            {
                _sessions.Refresh(_authService.GetAccountName());
            }
        }

        public void Draw()
        {
            MenuUI.Heading(_t.Get("sessions.heading"));
            if (!_authService.IsLoggedIn)
            {
                MenuUI.EmptyState(_t.Get("state.offline"), _t.Get("sessions.offline"));
                return;
            }

            GLSessionList data = _sessions.ValueFor(_authService.GetAccountName());
            if (data == null)
            {
                if (_sessions.Error != null && !_sessions.Loading)
                {
                    MenuUI.EmptyState(_t.Get("state.unavailable"), _t.Get("sessions.error", _sessions.Error));
                    if (MenuUI.Button(_t.Get("state.retry")))
                    {
                        _sessions.Refresh(_authService.GetAccountName(), true);
                    }
                }
                else
                {
                    MenuUI.EmptyState(_t.Get("state.loading"), _t.Get("sessions.loading"));
                }
                return;
            }

            List<GLSession> sessions = data.sessions ?? new List<GLSession>();
            if (sessions.Count == 0)
            {
                MenuUI.EmptyState(_t.Get("sessions.empty.title"), _t.Get("sessions.empty.text"));
                return;
            }

            MenuUI.Tiles((_t.Get("sessions.tile.sessions"), sessions.Count.ToString()), (_t.Get("sessions.tile.games"), sessions.Sum(s => s.games).ToString()));
            MenuUI.Space(4f);
            foreach (GLSession session in sessions)
            {
                DrawSessionRow(session);
            }
        }

        private void DrawSessionRow(GLSession session)
        {
            Rect row = MenuUI.Reserve(52f, 6f);
            bool hover = MenuUI.IsHover(row);
            MenuUI.DrawRect(row, hover ? MenuUI.SurfaceHover : MenuUI.Surface, MenuUI.Corner.Medium);

            Color edge = session.live ? MenuUI.Danger : session.current ? MenuUI.Accent : MenuUI.Border;
            MenuUI.DrawRect(new Rect(row.x, row.y + MenuUI.U(10f), MenuUI.Px(3f), row.height - MenuUI.U(20f)), edge, MenuUI.Corner.None);

            float x = row.x + MenuUI.U(16f);
            float buttonWidth = string.IsNullOrEmpty(session.url) ? 0f : MenuUI.U(70f);
            float width = row.width - MenuUI.U(28f) - buttonWidth;
            MenuUI.DrawText(new Rect(x, row.y + MenuUI.U(7f), width, MenuUI.U(20f)), StatFormat.LocalDateTime(session.start), MenuUI.RowStyle, MenuUI.Text);
            string details = _t.Get(session.games == 1 ? "sessions.games.one" : "sessions.games.other", session.games) + "  ·  " + StatFormat.Duration((session.end ?? ServerClock.NowMs() / 1000) - session.start).ToUpperInvariant();
            MenuUI.DrawText(new Rect(x, row.y + MenuUI.U(27f), width, MenuUI.U(18f)), details, MenuUI.CaptionStyle, MenuUI.MutedText);

            if (session.live || session.current)
            {
                // Badge after the date
                float dateWidth = MenuUI.RowStyle.CalcSize(new GUIContent(StatFormat.LocalDateTime(session.start))).x;
                Color badge = session.live ? MenuUI.Danger : MenuUI.Accent;
                string badgeText = _t.Get(session.live ? "sessions.live" : "sessions.open");
                float badgeWidth = MenuUI.PillStyle.CalcSize(new GUIContent(badgeText)).x + MenuUI.U(18f);
                float pillX = x + dateWidth + MenuUI.U(10f);
                if (session.live)
                {
                    float pulse = 0.5f + 0.5f * Mathf.Sin(Time.realtimeSinceStartup * 4f);
                    MenuUI.DrawGlow(new Rect(pillX, row.y + MenuUI.U(7f), badgeWidth, MenuUI.U(20f)), MenuUI.WithAlpha(badge, 0.2f + 0.3f * pulse));
                }
                MenuUI.DrawPill(pillX, row.y + MenuUI.U(6f), badgeText, session.live ? MenuUI.White : MenuUI.OnAccent, MenuUI.WithAlpha(badge, 0.85f));
            }

            if (buttonWidth > 0f)
            {
                var button = new Rect(row.xMax - buttonWidth - MenuUI.U(10f), row.y + MenuUI.U(12f), buttonWidth, row.height - MenuUI.U(24f));
                if (MenuUI.DrawSecondaryButton(button, _t.Get("sessions.view")))
                {
                    Application.OpenURL(session.url);
                }
            }
        }
    }
}

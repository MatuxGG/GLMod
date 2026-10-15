using GLMod.Class;
using GLMod.Constants;
using GLMod.GLEntities;
using GLMod.Services.Interfaces;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GLMod.Menu
{
    /// <summary>
    /// Good Loss account status and current ranks (GET /player/ranks): one badge per ranked leaderboard
    /// with its points, position and a link to the leaderboard.
    /// </summary>
    public class AccountMenuTab : IMenuTab
    {
        private const float RankCellHeight = 186f;

        private readonly IAuthenticationService _authService;
        private readonly string _apiEndpoint;
        private readonly ITranslationService _t;
        private readonly RemoteData<GLPlayerRanks> _ranks;

        public string Id => GameConstants.MENU_TAB_ACCOUNT;

        public string Title => _t.Get("tab.account");

        public AccountMenuTab(IAuthenticationService authService, IPlayerService playerService, IGameHistoryService historyService, string apiEndpoint, ITranslationService translations)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            if (playerService == null)
            {
                throw new ArgumentNullException(nameof(playerService));
            }
            _apiEndpoint = apiEndpoint ?? throw new ArgumentNullException(nameof(apiEndpoint));
            _t = translations ?? throw new ArgumentNullException(nameof(translations));
            _ranks = new RemoteData<GLPlayerRanks>((user, theme, ok, error) => playerService.GetRanks(user, ok, error, theme));

            // A finished game changes the points and the position
            if (historyService != null)
            {
                historyService.EntryRecorded += _ => _ranks.Invalidate();
            }
        }

        public void OnShown()
        {
            if (_authService.IsLoggedIn)
            {
                _ranks.Refresh(_authService.GetAccountName());
            }
        }

        public void Draw()
        {
            DrawProfileCard();
            if (_authService.IsBanned)
            {
                MenuUI.Field(_t.Get("account.banReason"), _authService.BanReason, MenuUI.Danger);
            }
            else if (!_authService.IsLoggedIn)
            {
                MenuUI.Muted(_t.Get("account.offlineHint"));
            }

            if (_authService.IsLoggedIn)
            {
                MenuUI.Space(6f);
                MenuUI.Heading(_t.Get("account.ranks"));
                DrawRanks();
            }

            if (_apiEndpoint != GameConstants.API_ENDPOINT)
            {
                MenuUI.Field(_t.Get("account.server"), ApiEndpointResolver.ToSiteUrl(_apiEndpoint), MenuUI.Accent);
            }
        }

        private void DrawRanks()
        {
            GLPlayerRanks data = _ranks.ValueFor(_authService.GetAccountName());
            if (data == null)
            {
                if (_ranks.Error != null && !_ranks.Loading)
                {
                    MenuUI.Muted(_t.Get("account.ranks.error", _ranks.Error));
                    if (MenuUI.Button(_t.Get("state.retry")))
                    {
                        _ranks.Refresh(_authService.GetAccountName(), true);
                    }
                }
                else
                {
                    MenuUI.Muted(_t.Get("account.ranks.loading"));
                }
                return;
            }

            List<GLPlayerRank> ranks = data.ranks ?? new List<GLPlayerRank>();
            if (ranks.Count == 0)
            {
                MenuUI.Muted(_t.Get("account.ranks.empty"));
                return;
            }

            // Two badges per row; a single badge is centered
            for (int start = 0; start < ranks.Count; start += 2)
            {
                Rect row = MenuUI.Reserve(RankCellHeight, 8f);
                float gap = MenuUI.U(8f);
                float width = (row.width - gap) / 2f;
                int count = Math.Min(2, ranks.Count - start);
                float x = count == 1 ? row.x + (row.width - width) / 2f : row.x;
                for (int i = 0; i < count; i++)
                {
                    DrawRankCell(new Rect(x + i * (width + gap), row.y, width, row.height), ranks[start + i]);
                }
            }
        }

        private void DrawRankCell(Rect cell, GLPlayerRank rank)
        {
            MenuUI.DrawRect(cell, MenuUI.Surface, MenuUI.Corner.Medium);

            float imageSize = MenuUI.U(86f);
            var image = new Rect(cell.x + (cell.width - imageSize) / 2f, cell.y + MenuUI.U(8f), imageSize, imageSize);
            if (!MenuUI.DrawRemoteImage(image, rank.image))
            {
                float placeholder = MenuUI.U(56f);
                MenuUI.DrawCircle(new Rect(image.x + (imageSize - placeholder) / 2f, image.y + (imageSize - placeholder) / 2f, placeholder, placeholder), MenuUI.SurfaceHover);
            }

            float y = image.yMax + MenuUI.U(4f);
            var line = new Rect(cell.x + MenuUI.U(6f), y, cell.width - MenuUI.U(12f), MenuUI.U(22f));
            MenuUI.DrawText(line, RankTitle(rank), MenuUI.TileValueStyle, MenuUI.Text, TextAnchor.MiddleCenter);

            y += MenuUI.U(22f);
            if (rank.position.HasValue)
            {
                MenuUI.DrawText(new Rect(line.x, y, line.width, MenuUI.U(18f)), _t.Get("account.rank.position", rank.position.Value),
                    MenuUI.RowStyle, MenuUI.AccentLight, TextAnchor.MiddleCenter);
            }
            y += MenuUI.U(18f);
            string mod = (rank.mod ?? "") + (rank.season.HasValue ? "  ·  S" + rank.season.Value : "");
            MenuUI.DrawText(new Rect(line.x, y, line.width, MenuUI.U(16f)), mod.ToUpperInvariant(), MenuUI.CaptionStyle, MenuUI.DimText, TextAnchor.MiddleCenter);

            if (!string.IsNullOrEmpty(rank.leaderboardUrl))
            {
                string label = _t.Get("account.leaderboard").ToUpperInvariant();
                float buttonWidth = Mathf.Min(cell.width - MenuUI.U(20f), MenuUI.SmallButtonStyle.CalcSize(new GUIContent(label)).x + MenuUI.U(28f));
                var button = new Rect(cell.x + (cell.width - buttonWidth) / 2f, cell.yMax - MenuUI.U(32f), buttonWidth, MenuUI.U(24f));
                if (MenuUI.DrawSecondaryButton(button, label))
                {
                    Application.OpenURL(rank.leaderboardUrl);
                }
            }
        }

        /// <summary>
        /// "GOLD 1 · 28 RP", or with the ELO when the API gives no rank points
        /// </summary>
        private string RankTitle(GLPlayerRank rank)
        {
            string name = (rank.name ?? "").ToUpperInvariant();
            if (rank.rp.HasValue)
            {
                return name + "  ·  " + _t.Get("account.rank.rp", rank.rp.Value);
            }
            if (rank.elo.HasValue)
            {
                return name + "  ·  " + _t.Get("account.rank.elo", rank.elo.Value);
            }
            return name;
        }

        private void DrawProfileCard()
        {
            bool connected = _authService.IsLoggedIn;
            bool banned = _authService.IsBanned;
            string account = connected ? _authService.GetAccountName() : "";
            string name = connected ? account : _t.Get("account.notConnected");
            string status = _t.Get(banned ? "account.status.banned" : connected ? "account.status.connected" : "account.status.offline");
            Color statusColor = banned ? MenuUI.Danger : connected ? MenuUI.Success : MenuUI.MutedText;

            Rect card = MenuUI.Reserve(86f);
            MenuUI.DrawRect(card, MenuUI.Surface, MenuUI.Corner.Medium);
            if (connected)
            {
                MenuUI.DrawAccentGradient(new Rect(card.x, card.y, card.width, card.height), true, 0.08f);
            }

            float avatarSize = MenuUI.U(54f);
            var avatar = new Rect(card.x + MenuUI.U(16f), card.y + (card.height - avatarSize) / 2f, avatarSize, avatarSize);
            if (connected)
            {
                MenuUI.DrawGlow(avatar, MenuUI.WithAlpha(MenuUI.Accent, 0.3f));
                MenuUI.DrawCircle(avatar, MenuUI.AccentStrong);
            }
            else
            {
                MenuUI.DrawCircle(avatar, MenuUI.Border);
            }
            string initial = string.IsNullOrEmpty(account) ? "?" : account.Substring(0, 1).ToUpperInvariant();
            MenuUI.DrawText(avatar, initial, MenuUI.AvatarStyle, connected ? MenuUI.White : MenuUI.MutedText);

            float textX = avatar.xMax + MenuUI.U(16f);
            float textWidth = card.xMax - textX - MenuUI.U(12f);
            MenuUI.DrawText(new Rect(textX, card.y + MenuUI.U(15f), textWidth, MenuUI.U(28f)), name, MenuUI.NameStyle, MenuUI.Text);

            // Status with a pulsing dot when connected
            float dotSize = MenuUI.U(8f);
            var dot = new Rect(textX + MenuUI.U(1f), card.y + MenuUI.U(53f), dotSize, dotSize);
            if (connected)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.realtimeSinceStartup * 3f);
                MenuUI.DrawGlow(dot, MenuUI.WithAlpha(statusColor, 0.25f + 0.35f * pulse));
            }
            MenuUI.DrawCircle(dot, statusColor);
            MenuUI.DrawText(new Rect(dot.xMax + MenuUI.U(8f), dot.y - MenuUI.U(5f), textWidth, MenuUI.U(18f)), status, MenuUI.CaptionStyle, statusColor);
        }
    }
}

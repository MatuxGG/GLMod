using GLMod.Class;
using GLMod.Constants;
using GLMod.Services.Interfaces;
using System;
using UnityEngine;

namespace GLMod.Menu
{
    /// <summary>
    /// Good Loss account status and detected mod.
    /// </summary>
    public class AccountMenuTab : IMenuTab
    {
        private readonly IAuthenticationService _authService;
        private readonly IConfigurationService _configService;
        private readonly string _apiEndpoint;
        private readonly ITranslationService _t;

        public string Id => GameConstants.MENU_TAB_ACCOUNT;

        public string Title => _t.Get("tab.account");

        public AccountMenuTab(IAuthenticationService authService, IConfigurationService configService, string apiEndpoint, ITranslationService translations)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _apiEndpoint = apiEndpoint ?? throw new ArgumentNullException(nameof(apiEndpoint));
            _t = translations ?? throw new ArgumentNullException(nameof(translations));
        }

        public void OnShown()
        {
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

            MenuUI.Space(8f);
            MenuUI.Heading(_t.Get("account.mod"));
            MenuUI.Tiles((_t.Get("account.detectedMod"), _configService.ModName), ("GLMod", "v" + GLMod.Version));
            if (_apiEndpoint != GameConstants.API_ENDPOINT)
            {
                MenuUI.Field(_t.Get("account.server"), ApiEndpointResolver.ToSiteUrl(_apiEndpoint), MenuUI.Accent);
            }
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

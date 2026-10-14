using GLMod.Class;
using GLMod.Constants;
using GLMod.Services.Interfaces;
using System;

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

        public string Title => "Account";

        public AccountMenuTab(IAuthenticationService authService, IConfigurationService configService, string apiEndpoint)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _apiEndpoint = apiEndpoint ?? throw new ArgumentNullException(nameof(apiEndpoint));
        }

        public void OnShown()
        {
        }

        public void Draw()
        {
            MenuUI.Heading("Good Loss account");
            if (_authService.IsBanned)
            {
                MenuUI.Field("Status", "Banned", MenuUI.Danger);
                MenuUI.Field("Reason", _authService.BanReason);
            }
            else if (_authService.IsLoggedIn)
            {
                MenuUI.Field("Status", "Connected", MenuUI.Success);
                MenuUI.Field("Account", _authService.GetAccountName());
            }
            else
            {
                MenuUI.Field("Status", "Not connected", MenuUI.MutedText);
                MenuUI.Muted("GLMod connects with your Steam account on the main menu. Your games are only linked to your Good Loss profile while you are connected.");
            }

            MenuUI.Space();
            MenuUI.Heading("Mod");
            MenuUI.Field("Detected mod", _configService.ModName);
            MenuUI.Field("GLMod", "v" + GLMod.Version);
            if (_apiEndpoint != GameConstants.API_ENDPOINT)
            {
                MenuUI.Field("Server", ApiEndpointResolver.ToSiteUrl(_apiEndpoint), MenuUI.Accent);
            }
        }
    }
}

using GLMod.Class;
using GLMod.Services.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;

namespace GLMod.Menu
{
    /// <summary>
    /// Steam DLC owned by the player, as checked by Good Loss. Loaded once per session (when shown while connected).
    /// </summary>
    public class DlcMenuTab : IMenuTab
    {
        private readonly IItemService _itemService;
        private readonly IAuthenticationService _authService;

        private List<string> _dlcNames;
        private bool _loading;

        public string Title => "DLC";

        public DlcMenuTab(IItemService itemService, IAuthenticationService authService)
        {
            _itemService = itemService ?? throw new ArgumentNullException(nameof(itemService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        }

        public void OnShown()
        {
            if (_dlcNames == null && !_loading && _authService.IsLoggedIn)
            {
                _loading = true;
                CoroutineRunner.Run(Load());
            }
        }

        public void Draw()
        {
            MenuUI.Heading("Owned DLC");
            if (!_authService.IsLoggedIn)
            {
                MenuUI.Muted("Connect to Good Loss to see your DLC (see the Account tab).");
                return;
            }

            if (_dlcNames == null)
            {
                MenuUI.Muted(_loading ? "Loading..." : "No DLC loaded.");
                return;
            }

            if (_dlcNames.Count == 0)
            {
                MenuUI.Muted("No DLC owned on this Steam account.");
            }
            foreach (string name in _dlcNames)
            {
                MenuUI.Label("•  " + name);
            }

            MenuUI.Space();
            MenuUI.Muted("Ownership is checked by Good Loss on your Steam account.");
        }

        private IEnumerator Load()
        {
            try
            {
                yield return _itemService.ReloadDlcOwnerships();

                var names = new List<string>();
                foreach (int appId in _itemService.SteamOwnerships)
                {
                    names.Add(SteamDlcCatalog.GetName(appId));
                }
                names.Sort(StringComparer.CurrentCultureIgnoreCase);
                _dlcNames = names;
            }
            finally
            {
                _loading = false;
            }
        }
    }
}

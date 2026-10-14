using GLMod.Class;
using GLMod.Constants;
using GLMod.GLEntities;
using GLMod.Services.Interfaces;
using System;
using System.Collections;

namespace GLMod.Menu
{
    /// <summary>
    /// Rank of the local player for the detected mod. Reloaded when shown, at most every MENU_REFRESH_SECONDS.
    /// </summary>
    public class RankMenuTab : IMenuTab
    {
        private readonly IRankService _rankService;
        private readonly IAuthenticationService _authService;
        private readonly IConfigurationService _configService;

        private GLRank _rank;
        private string _rankMod;
        private DateTime _loadedAt = DateTime.MinValue;
        private bool _loading;

        public string Title => "Rank";

        public RankMenuTab(IRankService rankService, IAuthenticationService authService, IConfigurationService configService)
        {
            _rankService = rankService ?? throw new ArgumentNullException(nameof(rankService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        }

        public void OnShown()
        {
            bool upToDate = _rank != null
                && string.IsNullOrEmpty(_rank.error)
                && _rankMod == _configService.ModName
                && (DateTime.UtcNow - _loadedAt).TotalSeconds < GameConstants.MENU_REFRESH_SECONDS;
            if (!upToDate)
            {
                Reload();
            }
        }

        public void Draw()
        {
            MenuUI.Heading("Rank  ·  " + _configService.ModName);
            if (!_authService.IsLoggedIn)
            {
                MenuUI.Muted("Connect to Good Loss to see your rank (see the Account tab).");
                return;
            }

            if (_rank == null || _rankMod != _configService.ModName)
            {
                MenuUI.Muted(_loading ? "Loading..." : "No rank loaded.");
                return;
            }

            if (!string.IsNullOrEmpty(_rank.error))
            {
                MenuUI.Muted("Your rank could not be loaded (" + _rank.error + ").");
                if (!_loading && MenuUI.Button("Retry"))
                {
                    Reload();
                }
                return;
            }

            MenuUI.Highlight(string.IsNullOrEmpty(_rank.name) ? "Unranked" : _rank.name);
            if (_rank.id <= 0)
            {
                MenuUI.Muted("Play games of this mod to get a rank.");
                return;
            }

            MenuUI.Field("Progress", _rank.percent + " %");
        }

        private void Reload()
        {
            if (_loading)
            {
                return;
            }

            _loading = true;
            CoroutineRunner.Run(Load(_configService.ModName));
        }

        private IEnumerator Load(string modName)
        {
            try
            {
                GLRank result = null;
                yield return _rankService.GetRank(modName, rank => result = rank);
                _rank = result;
                _rankMod = modName;
                _loadedAt = DateTime.UtcNow;
            }
            finally
            {
                _loading = false;
            }
        }
    }
}

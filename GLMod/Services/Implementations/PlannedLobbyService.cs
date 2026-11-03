using BepInEx.Configuration;
using BepInEx.Logging;
using GLMod.Class;
using GLMod.Constants;
using GLMod.GLEntities;
using GLMod.Services.Interfaces;
using InnerNet;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GLMod.Services.Implementations
{
    /// <summary>
    /// Reads the next planned lobby of the site for the banner of the main menu
    /// </summary>
    public class PlannedLobbyService : IPlannedLobbyService
    {
        private readonly ManualLogSource _logger;
        private readonly IMenuService _menuService;
        private readonly IConfigurationService _configService;
        private readonly ConfigEntry<string> _dismissed;
        private readonly string _apiEndpoint;

        public GLNextLobby Next { get; private set; }

        public PlannedLobbyService(ManualLogSource logger, IMenuService menuService, IConfigurationService configService, ConfigEntry<string> dismissed, string apiEndpoint)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _menuService = menuService ?? throw new ArgumentNullException(nameof(menuService));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _dismissed = dismissed ?? throw new ArgumentNullException(nameof(dismissed));
            _apiEndpoint = apiEndpoint ?? throw new ArgumentNullException(nameof(apiEndpoint));
        }

        private void Log(string message) => ServiceLogger.Log(_logger, nameof(PlannedLobbyService), message);

        public bool HasBanner => PlannedLobbyLogic.ShouldShow(Next, _dismissed.Value, DateTimeOffset.UtcNow);

        public void Dismiss()
        {
            if (Next?.lobby != null)
            {
                _dismissed.Value = Next.lobby.id.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        public IEnumerator Reload()
        {
            ApiResponse response = null;
            yield return ApiService.GetWithErrorHandlingAsync(
                PlannedLobbyLogic.BuildUrl(_apiEndpoint, GameConstants.API_PATH_LOBBIES_NEXT, GameLanguage.CurrentCode(), _configService.ModName),
                result => response = result);

            if (response == null || !response.IsSuccess)
            {
                Log($"Next lobby not read (status {response?.StatusCode})");
                yield break;
            }
            if (!PlayerApiResponse.TryParse(response.Content, out GLNextLobby next, out string error))
            {
                Log($"Unreadable next lobby ({error})");
                yield break;
            }
            Next = next;
        }

        public IEnumerator KeepFresh()
        {
            float next = 0f;
            while (true)
            {
                yield return new WaitForSeconds(1f);

                if (Time.realtimeSinceStartup < next || !_menuService.IsVisible || !IsAtMainMenu())
                {
                    continue;
                }

                yield return Reload();
                next = Time.realtimeSinceStartup + GameConstants.LOBBY_REFRESH_SECONDS;
            }
        }

        /// <summary>
        /// The banner only shows here: a player in a game or a lobby already has somewhere to play
        /// </summary>
        public static bool IsAtMainMenu()
        {
            return AmongUsClient.Instance != null
                && AmongUsClient.Instance.GameState == InnerNetClient.GameStates.NotJoined
                && SceneManager.GetActiveScene().name == GameConstants.SCENE_MAIN_MENU;
        }
    }
}

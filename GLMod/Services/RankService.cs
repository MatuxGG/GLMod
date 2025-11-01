using BepInEx.Logging;
using GLMod.Class;
using GLMod.GLEntities;
using System;
using System.Collections;
using System.Collections.Generic;

namespace GLMod.Services
{
    /// <summary>
    /// Service responsible for managing player ranks
    /// </summary>
    public class RankService : IRankService
    {
        private readonly ManualLogSource _logger;
        private readonly IAuthenticationService _authService;
        private readonly IConfigurationService _configService;
        private readonly string _apiEndpoint;

        public RankService(
            ManualLogSource logger,
            IAuthenticationService authService,
            IConfigurationService configService,
            string apiEndpoint)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _apiEndpoint = apiEndpoint ?? throw new ArgumentNullException(nameof(apiEndpoint));
        }

        private void Log(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                string playerName = PlayerControl.LocalPlayer?.Data?.PlayerName;
                string prefix = playerName != null ? "[GLMod] " + playerName + ": " : "[GLMod] ";
                _logger.LogInfo(prefix + message);
            }
        }

        public IEnumerator GetRank(string modName, System.Action<GLRank> onComplete)
        {
            if (string.IsNullOrEmpty(modName))
            {
                modName = _configService.ModName;
            }

            GLRank errorRank = new GLRank();

            if (!_authService.IsLoggedIn)
            {
                errorRank.error = "Offline";
                onComplete?.Invoke(errorRank);
                yield break;
            }

            var form = new Dictionary<string, string>
            {
                { "player", _authService.GetAccountName() },
                { "mod", modName }
            };

            string responseString = null;
            string error = null;

            // Appel de la coroutine ApiService
            yield return ApiService.PostFormAsync(_apiEndpoint + "/player/rank", form,
                result => {
                    responseString = result;
                },
                err => {
                    error = err;
                }
            );

            // Gestion du résultat
            if (error != null)
            {
                errorRank.error = "Login fail";
                onComplete?.Invoke(errorRank);
                yield break;
            }

            // Désérialiser et retourner le rang
            try
            {
                GLRank rank = GLJson.Deserialize<GLRank>(responseString);
                onComplete?.Invoke(rank);
            }
            catch (Exception ex)
            {
                Log("Erreur lors de la désérialisation du rang: " + ex.Message);
                errorRank.error = "Parse error";
                onComplete?.Invoke(errorRank);
            }
        }
    }
}

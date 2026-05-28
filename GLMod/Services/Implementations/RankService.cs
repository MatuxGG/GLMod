using BepInEx.Logging;
using GLMod.Services.Interfaces;
using GLMod.Class;
using GLMod.GLEntities;
using System;
using System.Collections;
using System.Collections.Generic;

namespace GLMod.Services.Implementations
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

        private void Log(string message) => ServiceLogger.Log(_logger, nameof(RankService), message);

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

            const string endpoint = "/player/rank";
            string responseString = null;
            string error = null;

            // Call the ApiService coroutine
            yield return ApiService.PostFormAsync(_apiEndpoint + endpoint, form,
                result => {
                    responseString = result;
                },
                err => {
                    error = err;
                }
            );

            // Result handling
            if (error != null)
            {
                Log($"[GetRank] HTTP error from {endpoint} (mod={modName}): {error}");
                errorRank.error = "Login fail";
                onComplete?.Invoke(errorRank);
                yield break;
            }

            // Deserialize and return the rank
            try
            {
                GLRank rank = GLJson.Deserialize<GLRank>(responseString);
                onComplete?.Invoke(rank);
            }
            catch (Exception ex)
            {
                Log($"[GetRank] Error while deserializing rank for mod={modName}: {ex.Message}");
                errorRank.error = "Parse error";
                onComplete?.Invoke(errorRank);
            }
        }
    }
}

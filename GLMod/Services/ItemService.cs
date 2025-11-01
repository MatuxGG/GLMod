using BepInEx.Logging;
using GLMod.Class;
using GLMod.GLEntities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GLMod.Services
{
    /// <summary>
    /// Service responsible for managing items and DLC ownership
    /// </summary>
    public class ItemService : IItemService
    {
        private readonly ManualLogSource _logger;
        private readonly IAuthenticationService _authService;
        private readonly string _apiEndpoint;
        private readonly List<GLItem> _items;
        private readonly List<int> _steamOwnerships;

        public List<GLItem> Items => _items;
        public List<int> SteamOwnerships => _steamOwnerships;

        public ItemService(
            ManualLogSource logger,
            IAuthenticationService authService,
            string apiEndpoint)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _apiEndpoint = apiEndpoint ?? throw new ArgumentNullException(nameof(apiEndpoint));

            _items = new List<GLItem>();
            _steamOwnerships = new List<int>();
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

        public IEnumerator ReloadItems()
        {
            if (!_authService.IsLoggedIn)
                yield break;

            var form = new Dictionary<string, string> { { "player", _authService.GetAccountName() } };

            string responseString = null;
            string error = null;

            // Appel de la coroutine ApiService
            yield return ApiService.PostFormAsync(_apiEndpoint + "/player/challengerItems", form,
                result => {
                    responseString = result;
                },
                err => {
                    error = err;
                }
            );

            // Vérifier l'erreur
            if (error != null)
            {
                Log("Erreur HTTP : " + error);
                yield break;
            }

            // Désérialiser la réponse
            try
            {
                _items.Clear();
                var newItems = GLJson.Deserialize<List<GLItem>>(responseString);
                _items.AddRange(newItems);
            }
            catch (Exception ex)
            {
                Log("Erreur lors du chargement des items: " + ex.Message);
            }
        }

        public bool IsUnlocked(string id)
        {
            return _items.FindAll(s => s.id == id) != null && _items.FindAll(s => s.id == id).Count > 0;
        }

        public IEnumerator ReloadDlcOwnerships()
        {
            if (!_authService.IsLoggedIn)
                yield break;

            var form = new Dictionary<string, string> { { "token", _authService.Token } };

            string responseString = null;
            string error = null;

            // Appel de la coroutine ApiService
            yield return ApiService.PostFormAsync(_apiEndpoint + "/user/steamownerships", form,
                result => {
                    responseString = result;
                },
                err => {
                    error = err;
                }
            );

            // Vérifier l'erreur
            if (error != null)
            {
                Log("Erreur HTTP : " + error);
                yield break;
            }

            // Désérialiser la réponse
            try
            {
                _steamOwnerships.Clear();
                var newOwnerships = GLJson.Deserialize<List<int>>(responseString);
                _steamOwnerships.AddRange(newOwnerships);
            }
            catch (Exception ex)
            {
                Log("Erreur lors du chargement des DLC ownerships: " + ex.Message);
            }
        }

        public bool HasDlc(int appId)
        {
            return _steamOwnerships.Count() > 0 && _steamOwnerships.Contains(appId);
        }
    }
}

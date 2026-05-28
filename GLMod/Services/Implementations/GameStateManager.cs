using AmongUs.GameOptions;
using GLMod.Services.Interfaces;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using GLMod.Class;
using GLMod.Constants;
using GLMod.Enums;
using GLMod.GLEntities;
using Hazel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GLMod.Services.Implementations
{
    /// <summary>
    /// Service responsible for managing game state and flow
    /// </summary>
    public class GameStateManager : IGameStateManager
    {
        private readonly ManualLogSource _logger;
        private readonly IAuthenticationService _authService;
        private readonly IConfigurationService _configService;
        private readonly string _apiEndpoint;
        private readonly ConfigEntry<string> _stepRpcConfig;

        public GLGame CurrentGame { get; private set; }
        public GameStep Step { get; set; }
        public string GameCode { get; set; }
        public string GameMap { get; set; }

        public GameStateManager(
            ManualLogSource logger,
            IAuthenticationService authService,
            IConfigurationService configService,
            string apiEndpoint,
            ConfigEntry<string> stepRpcConfig)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _apiEndpoint = apiEndpoint ?? throw new ArgumentNullException(nameof(apiEndpoint));
            _stepRpcConfig = stepRpcConfig ?? throw new ArgumentNullException(nameof(stepRpcConfig));

            Step = GameStep.Initial;
            GameCode = GameConstants.DEFAULT_GAME_CODE;
            GameMap = GameConstants.DEFAULT_MAP_NAME;
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

        public void StartGame(string code, string map, bool ranked)
        {
            Log("Creating game...");

            if (Step != GameStep.Initial)
            {
                Log("[CreateGame] Duplicate call");
                Step = GameStep.Initial;
            }

            if (string.IsNullOrEmpty(code))
            {
                Log("[CreateGame] Code null or empty");
            }

            if (string.IsNullOrEmpty(map))
            {
                Log("[CreateGame] Map null or empty");
            }

            try
            {
                CurrentGame = new GLGame(code, map, ranked, _configService.ModName);
                Step = GameStep.PlayersAdded;
                Log("Game created.");
            }
            catch (Exception e)
            {
                Log("[CreateGame] Catch exception " + e.Message);
                return;
            }
        }

        public void SetRanked(bool ranked)
        {
            if (CurrentGame == null)
            {
                Log("[SetRanked] Current Game null");
                return;
            }

            try
            {
                CurrentGame.SetRanked(ranked);
            }
            catch (Exception e)
            {
                Log("[SetRanked] Catch exception " + e.Message);
            }
        }

        public void AddPlayer(string playerName, string role, string team, string color)
        {
            if (Step != GameStep.PlayersAdded)
            {
                Log("[AddPlayer] Call when in step " + Step);
                return;
            }

            if (CurrentGame == null)
            {
                Log("[AddPlayer] Current Game null");
                return;
            }

            if (string.IsNullOrEmpty(playerName))
            {
                Log("[AddPlayer] PlayerName null or empty");
                return;
            }

            if (string.IsNullOrEmpty(role))
            {
                Log("[AddPlayer] Role null or empty");
                return;
            }

            if (string.IsNullOrEmpty(team))
            {
                Log("[AddPlayer] Team null or empty");
                return;
            }

            try
            {
                CurrentGame.AddPlayer(null, playerName, role, team, color);
            }
            catch (Exception e)
            {
                Log("[AddPlayer] Catch exception " + e.Message);
                return;
            }

            try
            {
                if (CurrentGame.players.Count() == PlayerControl.AllPlayerControls.Count)
                {
                    Step = GameStep.GameSent;
                }
            }
            catch (Exception e)
            {
                Log("[AddPlayer] Catch exception check " + e.Message);
                return;
            }
        }

        public IEnumerator SendGame(System.Action<bool> onComplete = null)
        {
            Log("Sending game...");

            if (!CanSendGame(onComplete))
            {
                yield break;
            }

            var form = BuildSendGameForm();
            Log("startGame: " + form["players"]);

            string responseString = null;
            string error = null;

            yield return ApiService.PostFormAsync(_apiEndpoint + "/game/start", form,
                result => responseString = result,
                err => error = err
            );

            if (error != null)
            {
                Log("[SendGame] fail, error: " + error);
                onComplete?.Invoke(false);
                yield break;
            }

            HandleSendGameResult(responseString, onComplete);
        }

        private bool CanSendGame(System.Action<bool> onComplete)
        {
            if (!AmongUsClient.Instance.AmHost)
            {
                Step = GameStep.GameIdSynced;
                onComplete?.Invoke(false);
                return false;
            }

            if (Step != GameStep.GameSent)
            {
                Log("[SendGame] Duplicate call");
                onComplete?.Invoke(false);
                return false;
            }

            if (CurrentGame.modName == null)
            {
                Log("[SendGame] Modname null");
            }

            return true;
        }

        private Dictionary<string, string> BuildSendGameForm()
        {
            return new Dictionary<string, string>
            {
                { "code", CurrentGame.code },
                { "map", CurrentGame.map },
                { "ranked", CurrentGame.ranked },
                { "modName", CurrentGame.modName },
                { "players", GLJson.Serialize<List<GLPlayer>>(CurrentGame.players) }
            };
        }

        private void HandleSendGameResult(string gameId, System.Action<bool> onComplete)
        {
            CurrentGame.id = gameId;
            Step = GameStep.GameIdSynced;
            CoroutineRunner.Run(SyncGameId(result =>
            {
                Log("Game sent.");
                onComplete?.Invoke(result);
            }));
        }

        public IEnumerator SyncGameId(System.Action<bool> onComplete = null)
        {
            if (!AmongUsClient.Instance.AmHost)
            {
                Step = GameStep.PlayersRecorded;
                onComplete?.Invoke(false);
                yield break;
            }

            if (Step != GameStep.GameIdSynced)
            {
                Log("[SyncGameId] Duplicate call");
                onComplete?.Invoke(false);
                yield break;
            }

            if (CurrentGame == null || string.IsNullOrEmpty(CurrentGame.id))
            {
                Log("[SyncGameId] Game null/id");
                onComplete?.Invoke(false);
                yield break;
            }

            _stepRpcConfig.Value = "NO";
            yield return new WaitForSeconds(GameConstants.RPC_SYNC_TIMEOUT);

            yield return CoroutineHelpers.RunAsync(
                () => Task.Run(() =>
                {
                    List<string> values = new() { CurrentGame.GetId().ToString() };
                    GLRPCProcedure.makeRpcCall(1, values);
                }),
                onError: ex => Log("[SyncGameId] RPC fail : " + ex.Message)
            );

            Step = GameStep.PlayersRecorded;
            onComplete?.Invoke(true);
        }

        public IEnumerator AddMyPlayer(System.Action<bool> onComplete = null)
        {
            if (!_authService.IsLoggedIn)
            {
                onComplete?.Invoke(false);
                yield break;
            }

            if (CurrentGame == null)
            {
                Log("[AddMyPlayer] Current Game null");
                onComplete?.Invoke(false);
                yield break;
            }

            PlayerControl me;
            GLPlayer myPlayer;

            // Initial player validation
            try
            {
                me = PlayerControl.LocalPlayer;
                myPlayer = CurrentGame.players.FindAll(p => p.playerName == me.Data.PlayerName)[0];
            }
            catch (Exception ex)
            {
                Log("[AddMyPlayer] Catch exception " + ex.Message);
                onComplete?.Invoke(false);
                yield break;
            }

            if (myPlayer == null)
            {
                Log("[AddMyPlayer] My player null");
                onComplete?.Invoke(false);
                yield break;
            }

            if (myPlayer.role == null)
            {
                Log("[AddMyPlayer] My role null");
            }

            if (myPlayer.team == null)
            {
                Log("[AddMyPlayer] My team null");
            }

            if (string.IsNullOrEmpty(myPlayer.playerName))
            {
                Log("[AddMyPlayer] My name null or empty");
            }

            // Wait for the game ID to become available
            while (string.IsNullOrEmpty(CurrentGame.id))
            {
                yield return new WaitForSeconds(0.1f);
            }

            // Prepare the form
            var form = new Dictionary<string, string>
            {
                { "gameId", CurrentGame.id },
                { "login", _authService.GetAccountName() },
                { "playerName", me.Data.PlayerName }
            };

            string responseString = null;
            string error = null;

            // Call the ApiService coroutine
            yield return ApiService.PostFormAsync(_apiEndpoint + "/game/addMyPlayer", form,
                result => {
                    responseString = result;
                },
                err => {
                    error = err;
                }
            );

            // Error handling
            if (error != null)
            {
                Log("[AddMyPlayer] Add my player fail, error: " + error);
            }
            onComplete?.Invoke(true);
        }

        public IEnumerator GetShieldPlayer(System.Action<string> onComplete = null, System.Action<string> onError = null)
        {
            if (CurrentGame == null || string.IsNullOrEmpty(CurrentGame.id))
            {
                Log("[GetShieldPlayer] Current game null or missing id");
                onError?.Invoke("game not ready");
                yield break;
            }

            string gameId = CurrentGame.id;
            const int maxAttempts = 10;
            const float retryDelay = 1.0f;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                var form = new Dictionary<string, string>
                {
                    { "gameId", gameId }
                };

                ApiResponse response = null;
                yield return ApiService.PostFormWithErrorHandlingAsync(_apiEndpoint + "/game/getShieldPlayer", form,
                    result => { response = result; });

                if (response != null && response.IsSuccess)
                {
                    onComplete?.Invoke(response.Content);
                    yield break;
                }

                if (response != null && response.StatusCode == 400)
                {
                    yield return new WaitForSeconds(retryDelay);
                    continue;
                }

                string errMsg = response?.Content ?? "no response";
                Log($"[GetShieldPlayer] fail (status {response?.StatusCode}): {errMsg}");
                onError?.Invoke(errMsg);
                yield break;
            }

            Log("[GetShieldPlayer] max attempts reached, players still not all registered");
            onError?.Invoke("max attempts reached");
        }

        public void SetWinnerTeams(List<string> winners)
        {
            if (!AmongUsClient.Instance.AmHost)
            {
                Step = GameStep.Initial;
                return;
            }

            if (Step != GameStep.PlayersRecorded && Step != GameStep.WinnerSet)
            {
                Log("[SetWinnerTeams] Call when in step " + Step);
                return;
            }

            if (CurrentGame == null)
            {
                Log("[SetWinnerTeams] Current Game null");
                return;
            }

            if (winners.Count <= 0)
            {
                Log("[SetWinnerTeams] Winners empty");
                return;
            }

            try
            {
                CurrentGame.SetWinners(winners);
                Step = GameStep.WinnerSet;
            }
            catch (Exception e)
            {
                Log("[SetWinnerTeams] Set Winner Teams fail, error: " + e.Message);
            }
        }

        public void AddWinnerPlayer(string playerName)
        {
            if (!AmongUsClient.Instance.AmHost)
            {
                Step = GameStep.Initial;
                return;
            }

            if (Step != GameStep.PlayersRecorded && Step != GameStep.WinnerSet)
            {
                Log("[AddWinnerPlayer] Call when in step " + Step);
                return;
            }

            if (CurrentGame == null)
            {
                Log("[AddWinnerPlayer] Current Game null");
                return;
            }

            if (string.IsNullOrEmpty(playerName))
            {
                Log("[AddWinnerPlayer] Player name null or empty empty");
                return;
            }

            try
            {
                CurrentGame.players.FindAll(p => p.playerName == playerName).ForEach(p => p.SetWin());
                Step = GameStep.WinnerSet;
            }
            catch (Exception e)
            {
                Log("[AddWinnerPlayer] Add Winner Player fail, error: " + e.Message);
            }
        }

        public IEnumerator EndGame()
        {
            Log("Ending game...");

            if (!AmongUsClient.Instance.AmHost)
            {
                Step = GameStep.Initial;
                yield break;
            }

            if (Step != GameStep.WinnerSet)
            {
                Log("[EndGame] Call when in step " + Step);
                yield break;
            }

            if (CurrentGame == null)
            {
                Log("[EndGame] Current Game null");
                yield break;
            }

            BackgroundEvents.endBackgroundProcess();

            yield return CoroutineHelpers.RunAsync(
                async () =>
                {
                    string json = GLJson.Serialize<GLGame>(CurrentGame);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await HttpHelper.Client.PostAsync(_apiEndpoint + "/game/end", content).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();
                    await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                },
                onCompleted: () =>
                {
                    Step = GameStep.Initial;
                    Log("Game ended.");
                },
                onError: ex => Log("[EndGame] End Game fail, error: " + ex.Message)
            );
        }

        public void AddAction(string source, string target, string action)
        {
            try
            {
                CurrentGame?.AddAction(source, target, action);
            }
            catch (Exception e)
            {
                Log("[AddAction] Catch exception " + e.Message);
            }
        }

        public void ResetGame()
        {
            CurrentGame = null;
            Step = GameStep.Initial;
        }
    }
}

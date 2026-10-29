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

        private void Log(string message) => ServiceLogger.Log(_logger, nameof(GameStateManager), message);

        public void StartGame(string code, string map, bool ranked)
        {
            Log("Creating game...");

            // Free play games are never recorded nor sent to Good Loss (also when a third-party mod starts the game)
            if (PlayModeHelper.IsFreePlay())
            {
                Log("[CreateGame] Free play: game not recorded");
                CurrentGame = null;
                Step = GameStep.Initial;
                return;
            }

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
            if (PlayModeHelper.IsFreePlay())
            {
                Log("[SendGame] Free play: not sent");
                onComplete?.Invoke(false);
                return false;
            }

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

            GLGame game = CurrentGame;
            _stepRpcConfig.Value = "NO";
            yield return new WaitForSeconds(GameConstants.RPC_SYNC_TIMEOUT);

            // A disconnect during the wait resets Step: do not bring the old game back
            if (!IsGameIdSyncCurrent(game, CurrentGame, Step))
            {
                Log("[SyncGameId] Game changed during wait");
                onComplete?.Invoke(false);
                yield break;
            }

            // The RPC goes through IL2CPP (AmongUsClient, PlayerControl): it must run on the Unity
            // main thread, which this coroutine already is. Sending it from Task.Run crashed the game.
            try
            {
                List<string> values = new() { CurrentGame.GetId().ToString() };
                GLRPCProcedure.makeRpcCall(1, values);
            }
            catch (Exception ex)
            {
                Log("[SyncGameId] RPC fail : " + ex.Message);
            }

            Step = GameStep.PlayersRecorded;
            onComplete?.Invoke(true);
        }

        /// <summary>
        /// Whether SyncGameId may still send the game id after its wait: the game is still the
        /// current one and Step is still GameIdSynced (a disconnect resets it to Initial).
        /// </summary>
        public static bool IsGameIdSyncCurrent(GLGame game, GLGame currentGame, GameStep step)
        {
            return game != null
                && ReferenceEquals(game, currentGame)
                && step == GameStep.GameIdSynced;
        }

        public IEnumerator AddMyPlayer(System.Action<bool> onComplete = null)
        {
            if (!_authService.IsLoggedIn || PlayModeHelper.IsFreePlay())
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

            // Wait for the game ID to become available (never sent if the host has no GLMod or its SendGame failed)
            GLGame game = CurrentGame;
            float waited = 0f;
            while (ShouldWaitForGameId(game, CurrentGame, waited))
            {
                yield return new WaitForSeconds(0.1f);
                waited += 0.1f;
            }

            if (!ReferenceEquals(game, CurrentGame) || string.IsNullOrEmpty(game.id))
            {
                Log("[AddMyPlayer] Game id unavailable");
                onComplete?.Invoke(false);
                yield break;
            }

            // Prepare the form. The API takes the account from the game token (Bearer).
            var form = new Dictionary<string, string>
            {
                { "gameId", game.id },
                { "playerName", me.Data.PlayerName }
            };

            yield return PostMyPlayer(_apiEndpoint + "/game/addMyPlayer", form, _authService.BearerToken, Log, onComplete);
        }

        /// <summary>
        /// Sends the addMyPlayer form and calls onComplete(false) on an HTTP or network error,
        /// onComplete(true) otherwise.
        /// </summary>
        public static IEnumerator PostMyPlayer(string url, Dictionary<string, string> form, string bearerToken,
            System.Action<string> log, System.Action<bool> onComplete = null)
        {
            string error = null;

            // Call the ApiService coroutine
            yield return ApiService.PostFormAsync(url, form,
                result => { },
                err => {
                    error = err;
                },
                bearerToken
            );

            // Error handling
            if (error != null)
            {
                log?.Invoke("[AddMyPlayer] Add my player fail, error: " + error);
                onComplete?.Invoke(false);
                yield break;
            }
            onComplete?.Invoke(true);
        }

        /// <summary>
        /// Whether AddMyPlayer keeps waiting for the game id: the game is still the current one,
        /// has no id yet, and the wait is shorter than GAME_ID_WAIT_TIMEOUT.
        /// </summary>
        public static bool ShouldWaitForGameId(GLGame game, GLGame currentGame, float waited)
        {
            return game != null
                && ReferenceEquals(game, currentGame)
                && string.IsNullOrEmpty(game.id)
                && waited < GameConstants.GAME_ID_WAIT_TIMEOUT;
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

            if (PlayModeHelper.IsFreePlay())
            {
                Log("[EndGame] Free play: not sent");
                Step = GameStep.Initial;
                yield break;
            }

            if (!AmongUsClient.Instance.AmHost)
            {
                Step = GameStep.Initial;
                yield break;
            }

            if (Step != GameStep.WinnerSet)
            {
                Log("[EndGame] Call when in step " + Step);
                // The game cannot be sent anymore: start the next one from scratch
                Step = GameStep.Initial;
                yield break;
            }

            if (CurrentGame == null)
            {
                Log("[EndGame] Current Game null");
                Step = GameStep.Initial;
                yield break;
            }

            BackgroundEvents.endBackgroundProcess();

            yield return PostEndGame(_apiEndpoint + "/game/end", CurrentGame, Log, step => Step = step);
        }

        /// <summary>
        /// Sends the finished game to /game/end, then sets the step back to Initial whatever the result:
        /// after an HTTP or network error the next game must still be started and sent.
        /// </summary>
        public static IEnumerator PostEndGame(string url, GLGame game, System.Action<string> log,
            System.Action<GameStep> setStep)
        {
            yield return CoroutineHelpers.RunAsync(
                async () =>
                {
                    string json = GLJson.Serialize<GLGame>(game);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await HttpHelper.Client.PostAsync(url, content).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();
                    await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                },
                onCompleted: () => log?.Invoke("Game ended."),
                onError: ex => log?.Invoke("[EndGame] End Game fail, error: " + ex.Message)
            );

            setStep?.Invoke(GameStep.Initial);
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

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using Hazel;
using Il2CppInterop.Runtime.Injection;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using GLMod.Enums;
using GLMod.Constants;
using GLMod.Services;
using Random = System.Random;

namespace GLMod
{
    [BepInPlugin(Id, "GLMod", "5.2.1")]
    [BepInProcess("Among Us.exe")]
    public class GLMod : BasePlugin
    {
        public const string Id = "glmod";

        public Harmony Harmony { get; } = new Harmony(Id);

        // Services
        public static IAuthenticationService AuthService { get; private set; }
        public static ITranslationService TranslationService { get; private set; }

        public static ConfigEntry<string> connectionState { get; private set; }
        public static ConfigEntry<string> translations { get; private set; }
        public static ConfigEntry<string> stepConf { get; private set; }
        public static ConfigEntry<string> stepRpc { get; private set; }
        public static ConfigEntry<string> enabled { get; private set; }
        public static ConfigEntry<string> supportId { get; private set; }

        public const string api = GameConstants.API_ENDPOINT;

        public static GLGame currentGame;
        public static List<string> enabledServices;
        public static string gameCode = GameConstants.DEFAULT_GAME_CODE;
        public static string gameMap = GameConstants.DEFAULT_MAP_NAME;
        public static string configPath;
        public static string modName = "Vanilla";
        public static GameStep step = GameStep.Initial;

        // Deprecated - Use TranslationService instead
        [Obsolete("Use TranslationService.Languages instead")]
        public static List<GLLanguage> languages => TranslationService?.Languages;

        [Obsolete("Use TranslationService.CurrentLanguage instead")]
        public static string lg
        {
            get => TranslationService?.CurrentLanguage ?? GameConstants.DEFAULT_LANGUAGE;
            set
            {
                if (TranslationService != null)
                    TranslationService.CurrentLanguage = value;
            }
        }
        public static List<int> steamOwnerships = new List<int>() { };
        public static bool debug = false;
        public static bool withUnityExplorer = false;
        internal static BepInEx.Logging.ManualLogSource Logger;
        public static List<GLItem> items = new List<GLItem>() { };

        // Deprecated - Use AuthService instead
        [Obsolete("Use AuthService.Token instead")]
        public static string token => AuthService?.Token;

        [Obsolete("Use AuthService.IsLoggedIn instead")]
        public static bool logged => AuthService?.IsLoggedIn ?? false;

        [Obsolete("Use AuthService.IsBanned instead")]
        public static bool isBanned => AuthService?.IsBanned ?? false;

        [Obsolete("Use AuthService.BanReason instead")]
        public static string banReason => AuthService?.BanReason ?? "";

        public override void Load()
        {
            Logger = Log;
            log("Loading mod...");
            connectionState = Config.Bind("GoodLoss", "Connected", "");
            enabled = Config.Bind("GoodLoss", "Enabled", "Yes");
            translations = Config.Bind("GoodLoss", "translations", "No");
            stepConf = Config.Bind("Validation", "steps", "");
            stepRpc = Config.Bind("Validation", "RPC", "");
            configPath = Path.GetDirectoryName(Config.ConfigFilePath);

            // Initialize services
            AuthService = new AuthenticationService(connectionState);
            TranslationService = new TranslationService();
          
            Random random = new Random();
            string newSupportId = new string(Enumerable.Repeat(GameConstants.SUPPORT_ID_CHARS, GameConstants.SUPPORT_ID_LENGTH)
                .Select(s => s[random.Next(s.Length)]).ToArray());
            supportId = Config.Bind("GoodLoss", "Support Id", newSupportId);

            GLMod.findModName();
            log("Mod " + modName + " configured");

            GLMod.enabledServices = new List<string>() { };
            GLMod.enableService(ServiceType.StartGame);
            GLMod.enableService(ServiceType.EndGame);
            GLMod.enableService(ServiceType.Tasks);
            GLMod.enableService(ServiceType.TasksMax);
            GLMod.enableService(ServiceType.Exiled);
            GLMod.enableService(ServiceType.Kills);
            GLMod.enableService(ServiceType.BodyReported);
            GLMod.enableService(ServiceType.Emergencies);
            GLMod.enableService(ServiceType.Turns);
            GLMod.enableService(ServiceType.Votes);
            GLMod.enableService(ServiceType.Roles);

            stepConf.Value = "YES";
            stepRpc.Value = "YES";

            if (translations.Value.ToLower() == "yes")
            {
                CoroutineRunner.Run(TranslationService.LoadTranslations());
            }
            log("Mod loaded");

            CoroutineRunner.Init();

            CoroutineRunner.Run(GLMod.verifyGLMod(null, result =>
            {
                log("GLMod verified: " +  result);
            }));

            Harmony.PatchAll();
        }


        /*
         * Items
         */

        public static IEnumerator reloadItems()
        {
            if (!AuthService.IsLoggedIn)
                yield break;

            var form = new Dictionary<string, string> { { "player", getAccountName() } };

            string responseString = null;
            string error = null;

            // Appel de la coroutine ApiService
            yield return ApiService.PostFormAsync(api + "/player/challengerItems", form,
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
                log("Erreur HTTP : " + error);
                yield break;
            }

            // Désérialiser la réponse
            try
            {
                items = GLJson.Deserialize<List<GLItem>>(responseString);
                log("Reload successes OK — " + items.Count + " successes.");
            }
            catch (System.Exception ex)
            {
                log("Erreur : " + ex.Message);
            }
        }

        public static Boolean isUnlocked(string id)
        {
            return GLMod.items.FindAll(s => s.id == id) != null && GLMod.items.FindAll(s => s.id == id).Count > 0;
        }

        /*
         * Dlc
         */

        public static IEnumerator reloadDlcOwnerships()
        {
            if (!AuthService.IsLoggedIn)
                yield break;

            var form = new Dictionary<string, string> { { "token", AuthService.Token } };

            string responseString = null;
            string error = null;

            // Appel de la coroutine ApiService
            yield return ApiService.PostFormAsync(api + "/user/steamownerships", form,
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
                log("Erreur HTTP : " + error);
                yield break;
            }

            // Désérialiser la réponse
            try
            {
                steamOwnerships = GLJson.Deserialize<List<int>>(responseString);
                log("Reload DLC ownerships OK — " + steamOwnerships.Count + " items.");
            }
            catch (System.Exception ex)
            {
                log("Erreur : " + ex.Message);
            }
        }

        public static Boolean hasDlc(int appId)
        {
            return steamOwnerships.Count() > 0 && steamOwnerships.Contains(appId);
        }

        public static void enableTranslations()
        {
            translations.Value = "YES";
        }

        /*
         * Mod Name
         */

        public static void findModName()
        {
            DirectoryInfo dir = new DirectoryInfo(configPath);
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                if (file.Name.EndsWith(".glmod"))
                {
                    modName = Path.GetFileNameWithoutExtension(file.Name);
                    return;
                }
            }
        }

        public static void setModName(string modName)
        {
            GLMod.modName = modName;
        }

        /*
         * Game
         */

        public static void log(string text)
        {
            if (!String.IsNullOrEmpty(text))
            {
                string playerName = PlayerControl.LocalPlayer?.Data?.PlayerName;
                string prefix = playerName != null ? "[GLMod] " + playerName + ": " : "[GLMod] ";
                Logger.LogInfo(prefix + text);
            }
        }

        public static void UpdateRpcStep()
        {
            if (stepRpc.Value == "NO")
            {
                stepRpc.Value = "YES: ";
            } else
            {
                stepRpc.Value = stepRpc.Value + " | ";
            }
            stepRpc.Value = stepRpc.Value + "YES";
        }

        // Step 1 : Create / Start Game

        public static void StartGame(string code, string map, Boolean ranked)
        {
            GLMod.log("Creating game...");
            if (step != GameStep.Initial)
            {
                log("[CreateGame] Duplicate call");
                step = GameStep.Initial;
            }
            if (string.IsNullOrEmpty(code))
            {
                log("[CreateGame] Code null or empty");
            }
            if (string.IsNullOrEmpty(map))
            {
                log("[CreateGame] Map null or empty");
            }

            try
            {
                currentGame = new GLGame(code, map, ranked, modName);
                step = GameStep.PlayersAdded;
                GLMod.log("Game created.");
            }
            catch (Exception e)
            {
                log("[CreateGame] Catch exception " + e.Message);
                return;
            }

        }

        // Step 2 : Add Player until all players recorded
        public static void AddPlayer(string playerName, string role, string team, string color)
        {
            if (step != GameStep.PlayersAdded)
            {
                log("[AddPlayer] Call when in step " + step);
                return;
            }

            if (currentGame == null)
            {
                log("[AddPlayer] Current Game null");
                return;
            }

            if (string.IsNullOrEmpty(playerName))
            {
                log("[AddPlayer] PlayerName null or empty");
                return;
            }

            if (string.IsNullOrEmpty(role))
            {
                log("[AddPlayer] Role null or empty");
                return;
            }

            if (string.IsNullOrEmpty(team))
            {
                log("[AddPlayer] Team null or empty");
                return;
            }

            try
            {
                currentGame.addPlayer(null, playerName, role, team, color);
            } catch (Exception e)
            {
                log("[AddPlayer] Catch exception " + e.Message);
                return;
            }

            try
            {
                if (currentGame.players.Count() == PlayerControl.AllPlayerControls.Count)
                {
                    step = GameStep.GameSent;
                }
            }
            catch (Exception e)
            {
                log("[AddPlayer] Catch exception check " + e.Message);
                return;
            }

        }

        // Step 3 : Send Game (nothing for non host)
        public static IEnumerator SendGame(System.Action<bool> onComplete = null)
        {
            GLMod.log("Sending game...");

            if (!AmongUsClient.Instance.AmHost)
            {
                step = GameStep.GameIdSynced;
                onComplete?.Invoke(false);
                yield break;
            }

            if (step != GameStep.GameSent)
            {
                log("[SendGame] Duplicate call");
                onComplete?.Invoke(false);
                yield break;
            }

            if (currentGame.modName == null)
            {
                log("[SendGame] Modname null");
            }

            var form = new Dictionary<string, string>
            {
                { "code", currentGame.code },
                { "map", currentGame.map },
                { "ranked", currentGame.ranked },
                { "modName", currentGame.modName },
                { "players", GLJson.Serialize<List<GLPlayer>>(currentGame.players) }
            };

            // DEBUG
            GLMod.log("startGame: " + GLJson.Serialize<List<GLPlayer>>(currentGame.players));

            string responseString = null;
            string error = null;

            // Appel de la coroutine ApiService
            yield return ApiService.PostFormAsync(api + "/game/start", form,
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
                log("[SendGame] fail, error: " + error);
                onComplete?.Invoke(false);
                yield break;
            }

            currentGame.id = responseString;
            step = GameStep.GameIdSynced;
            CoroutineRunner.Run(SyncGameId(result =>
            {
                GLMod.log("Game sent.");
                onComplete?.Invoke(result);
            }));
        }

        // Step 4: Sync Game Id for host
        public static IEnumerator SyncGameId(System.Action<bool> onComplete = null)
        {
            if (!AmongUsClient.Instance.AmHost) { step = GameStep.PlayersRecorded; onComplete?.Invoke(false); yield break; }
            if (step != GameStep.GameIdSynced) { log("[SyncGameId] Duplicate call"); onComplete?.Invoke(false); yield break; }
            if (currentGame == null || string.IsNullOrEmpty(currentGame.id)) { log("[SyncGameId] Game null/id"); onComplete?.Invoke(false); yield break; }

            GLMod.stepRpc.Value = "NO";
            yield return new WaitForSeconds(GameConstants.RPC_SYNC_TIMEOUT);

            bool done = false;

            Task.Run(() =>
            {
                try
                {
                    List<string> values = new() { GLMod.currentGame.getId().ToString() };
                    GLRPCProcedure.makeRpcCall(1, values);
                }
                catch (Exception ex)
                {
                    GLMod.log("[SyncGameId] RPC fail : " + ex.Message);
                }
                finally { done = true; }
            });

            // 🧵 Attend la fin sans bloquer le thread principal
            while (!done) yield return null;

            GLMod.step = GameStep.PlayersRecorded;
            onComplete?.Invoke(true);
        }

        // External process : Add My Player
        public static IEnumerator AddMyPlayer(System.Action<bool> onComplete = null)
        {
            if (!AuthService.IsLoggedIn)
            {
                onComplete?.Invoke(false);
                yield break;
            }

            if (currentGame == null)
            {
                log("[AddMyPlayer] Current Game null");
                onComplete?.Invoke(false);
                yield break;
            }

            PlayerControl me;
            GLPlayer myPlayer;

            // Validation initiale du joueur
            try
            {
                me = PlayerControl.LocalPlayer;
                myPlayer = GLMod.currentGame.players.FindAll(p => p.playerName == me.Data.PlayerName)[0];
            }
            catch (Exception ex)
            {
                GLMod.log("[AddMyPlayer] Catch exception " + ex.Message);
                onComplete?.Invoke(false);
                yield break;
            }

            if (myPlayer == null)
            {
                GLMod.log("[AddMyPlayer] My player null");
                onComplete?.Invoke(false);
                yield break;
            }

            if (myPlayer.role == null)
            {
                GLMod.log("[AddMyPlayer] My role null");
            }

            if (myPlayer.team == null)
            {
                GLMod.log("[AddMyPlayer] My team null");
            }

            if (string.IsNullOrEmpty(myPlayer.playerName))
            {
                GLMod.log("[AddMyPlayer] My name null or empty");
            }

            // Attendre que l'ID du jeu soit disponible
            while (string.IsNullOrEmpty(GLMod.currentGame.id))
            {
                yield return new WaitForSeconds(0.1f);
            }

            // Préparer le formulaire
            var form = new Dictionary<string, string>
            {
                { "gameId", GLMod.currentGame.id },
                { "login", GLMod.getAccountName() },
                { "playerName", me.Data.PlayerName }
            };

            string responseString = null;
            string error = null;

            // Appel de la coroutine ApiService
            yield return ApiService.PostFormAsync(GLMod.api + "/game/addMyPlayer", form,
                result => {
                    responseString = result;
                },
                err => {
                    error = err;
                }
            );

            // Gestion des erreurs
            if (error != null)
            {
                GLMod.log("[AddMyPlayer] Add my player fail, error: " + error);
            }
            onComplete?.Invoke(true);
        }

        // Step 5 : Set Winner Teams
        public static void SetWinnerTeams(List<string> winners)
        {
            if (!AmongUsClient.Instance.AmHost)
            {
                step = GameStep.Initial;
                return;
            }
            if (step != GameStep.PlayersRecorded && step != GameStep.WinnerSet)
            {
                log("[SetWinnerTeams] Call when in step " + step);
                return;
            }
            if (currentGame == null)
            {
                log("[SetWinnerTeams] Current Game null");
                return;
            }

            if (winners.Count <= 0)
            {
                log("[SetWinnerTeams] Winners empty");
                return;
            }

            try
            {
                currentGame.setWinners(winners);
                step = GameStep.WinnerSet;
            }
            catch (Exception e)
            {
                log("[SetWinnerTeams] Set Winner Teams fail, error: " + e.Message);
            }
            
        }

        // Step 5 : Set Winner Player
        public static void AddWinnerPlayer(string playerName)
        {
            if (!AmongUsClient.Instance.AmHost)
            {
                step = GameStep.Initial;
                return;
            }
            if (step != GameStep.PlayersRecorded && step != GameStep.WinnerSet)
            {
                log("[AddWinnerPlayer] Call when in step " + step);
                return;
            }
            if (currentGame == null)
            {
                log("[AddWinnerPlayer] Current Game null");
                return;
            }

            if (string.IsNullOrEmpty(playerName))
            {
                log("[AddWinnerPlayer] Player name null or empty empty");
                return;
            }

            try
            {
                currentGame.players.FindAll(p => p.playerName == playerName).ForEach(p => p.setWin());
                step = GameStep.WinnerSet;
            } catch (Exception e)
            {
                log("[AddWinnerPlayer] Add Winner Player fail, error: " + e.Message);
            }
            
        }

        // Step 6 : End Game

        public static IEnumerator EndGame()
        {
            GLMod.log("Ending game...");

            if (!AmongUsClient.Instance.AmHost)
            {
                step = GameStep.Initial;
                yield break;
            }

            if (step != GameStep.WinnerSet)
            {
                log("[EndGame] Call when in step " + step);
                yield break;
            }

            if (currentGame == null)
            {
                log("[EndGame] Current Game null");
                yield break;
            }

            BackgroundEvents.endBackgroundProcess();

            string error = null;
            bool done = false;

            System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    string json = GLJson.Serialize<GLGame>(currentGame);

                    // DEBUG
                    // GLMod.log("endGame: "+ json);

                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await HttpHelper.Client.PostAsync(api + "/game/end", content);
                    response.EnsureSuccessStatusCode();

                    // Lecture de la réponse si nécessaire
                    var result = await response.Content.ReadAsStringAsync();
                }
                catch (Exception e)
                {
                    error = e.Message;
                }
                finally
                {
                    done = true;
                }
            });

            // Attendre la fin de la tâche
            while (!done)
                yield return null;

            // Gestion du résultat
            if (error != null)
            {
                log("[EndGame] End Game fail, error: " + error);
            }
            else
            {
                step = GameStep.Initial;
                GLMod.log("Game ended.");
            }
        }

        /*
         * Services
         */


        public static void disableService(ServiceType service)
        {
            string serviceName = service.ToString();
            if (enabledServices.Contains(serviceName))
            {
                enabledServices.Remove(serviceName);
            }
        }

        public static void disableService(string service)
        {
            if (enabledServices.Contains(service))
            {
                enabledServices.Remove(service);
            }
        }

        public static void disableAllServices()
        {
            enabledServices = new List<string>() { };
        }

        public static void enableService(ServiceType service)
        {
            string serviceName = service.ToString();
            if (!enabledServices.Contains(serviceName))
            {
                enabledServices.Add(serviceName);
            }
        }

        public static void enableService(string service)
        {
            if (!enabledServices.Contains(service))
            {
                enabledServices.Add(service);
            }
        }

        public static bool existService(ServiceType service)
        {
            return enabledServices.Contains(service.ToString());
        }

        public static bool existService(string service)
        {
            return enabledServices.Contains(service);
        }

        public static void addAction(string source, string target, string action)
        {
            try
            {
                currentGame.addAction(source, target, action);
            } catch (Exception e)
            {
                log("[AddAction] Catch exception " + e.Message);
            }
            
        }

        /*
         * Account name
         */

        public static string getAccountName()
        {
            return AuthService?.GetAccountName() ?? "";
        }

        /*
         * Connnection
         */

        public static IEnumerator login(System.Action<bool> onComplete = null)
        {
            if (AuthService == null)
            {
                log("AuthService not initialized");
                onComplete?.Invoke(false);
                yield break;
            }

            yield return AuthService.Login(onComplete);
        }

        public static void logout()
        {
            AuthService?.Logout();
        }

        public static bool isLoggedIn()
        {
            return AuthService?.IsLoggedIn ?? false;
        }

        public static IEnumerator getRank(string customModName, System.Action<GLRank> onComplete)
        {
            if (customModName == null)
            {
                customModName = GLMod.modName;
            }

            GLRank errorRank = new GLRank();

            if (!AuthService.IsLoggedIn)
            {
                errorRank.error = "Offline";
                onComplete?.Invoke(errorRank);
                yield break;
            }

            var form = new Dictionary<string, string>
            {
                { "player", getAccountName() },
                { "mod", customModName }
            };

            string responseString = null;
            string error = null;

            // Appel de la coroutine ApiService
            yield return ApiService.PostFormAsync(api + "/player/rank", form,
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
            catch (System.Exception ex)
            {
                log("Erreur lors de la désérialisation du rang: " + ex.Message);
                errorRank.error = "Parse error";
                onComplete?.Invoke(errorRank);
            }
        }

        /*
         * Translations
         */

        public static IEnumerator loadTranslations()
        {
            if (TranslationService == null)
            {
                log("TranslationService not initialized");
                yield break;
            }

            yield return TranslationService.LoadTranslations();
        }

        public static string translate(string toTranslate)
        {
            return TranslationService?.Translate(toTranslate) ?? toTranslate;
        }

        public static bool setLg(string languageCode)
        {
            return TranslationService?.SetLanguage(languageCode) ?? false;
        }

        public static string getLg()
        {
            return TranslationService?.CurrentLanguage ?? GameConstants.DEFAULT_LANGUAGE;
        }

        public static string getNameFromCode(string code)
        {
            return TranslationService?.GetLanguageName(code);
        }

        public static string getCodeFromName(string name)
        {
            return TranslationService?.GetLanguageCode(name);
        }

        public static string getMapName()
        {
            try
            {
                byte mapId = GameOptionsManager.Instance.currentGameOptions.MapId;

                // Handle special case for dlekSehT (reversed Skeld)
                if (mapId == (byte)MapNames.Dleks)
                    return "dlekSehT";

                GameMapType mapType = GameMapTypeExtensions.FromMapId(mapId);
                return mapType.ToDisplayName();
            }
            catch (Exception e)
            {
                log("[getMapName] Catch exception " + e.Message);
                return "Unknown";
            }
        }

        public static IEnumerator getApiData(string id, System.Action<string> onComplete, System.Action<string> onError = null)
        {
            var form = new Dictionary<string, string>
            {
                { "id", id }
            };

            string responseString = null;
            string error = null;

            // Appel de la coroutine ApiService
            yield return ApiService.PostFormAsync(api + "/data", form,
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
                log("Erreur HTTP : " + error);
                onError?.Invoke(error);
                onComplete?.Invoke(""); // Retourner une chaîne vide en cas d'erreur (comportement original)
                yield break;
            }

            onComplete?.Invoke(responseString);
        }

        public static IEnumerator getChecksum(string checksumId, System.Action<string> onComplete, System.Action<string> onError = null)
        {
            string result = null;
            string error = null;
            log("getChecksum:" + checksumId);

            // Appeler getApiData en coroutine
            yield return getApiData("checksum_" + checksumId,
                data => {
                    result = data;
                },
                err => {
                    error = err;
                }
            );

            // Gestion du résultat
            if (error != null)
            {
                log($"getChecksum failed for {checksumId}, error: {error}");
                onError?.Invoke(error);
            }
            else
            {
                onComplete?.Invoke(result);
            }
        }

        private static string CalculateFileSHA512(string filePath)
        {
            using (var sha256 = SHA512.Create())
            using (var fileStream = File.OpenRead(filePath))
            {
                byte[] hashBytes = sha256.ComputeHash(fileStream);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }


        public static IEnumerator verifyDll(string checksumId, string dllPath, System.Action<bool> onComplete, System.Action<string> onError = null)
        {
            string localChecksum = GLMod.CalculateFileSHA512(dllPath);
            log("Local checksum: " + localChecksum);

            string remoteChecksum = null;
            bool hasError = false;
            string errorMessage = null;

            // Appeler la coroutine getChecksum et attendre son résultat
            yield return getChecksum(checksumId,
                checksum => {
                    remoteChecksum = checksum;
                },
                error => {
                    hasError = true;
                    errorMessage = error;
                }
            );

            // Vérifier si une erreur s'est produite
            if (hasError)
            {
                log($"verifyDll failed for {checksumId}, error: {errorMessage}");
                onError?.Invoke(errorMessage);
                yield break;
            }

            // Comparer les checksums
            log("Remote checksum: " + remoteChecksum);

            bool result;
            if (localChecksum == remoteChecksum)
            {
                log("Valid checksum");
                result = true;
            }
            else
            {
                log("Invalid checksum");
                result = false;
            }

            onComplete?.Invoke(result);
        }

        private static IEnumerator verifyGLMod(System.Action<bool> onComplete, System.Action<string> onError = null)
        {
            var pluginAttribute = typeof(GLMod).GetCustomAttribute<BepInPlugin>();
            string version = pluginAttribute?.Version.ToString();
            log(version);

            bool result = false;
            bool hasError = false;
            string errorMessage = null;

            // Appeler la coroutine verifyDll et attendre son résultat
            yield return verifyDll("glmod" + version, "BepInEx/plugins/glmod.dll",
                isValid => {
                    log("test1");
                    result = isValid;
                },
                error => {
                    log("test2");
                    hasError = true;
                    errorMessage = error;
                }
            );

            // Vérifier si une erreur s'est produite
            if (hasError)
            {
                log($"verifyGLMod failed, error: {errorMessage}");
                onError?.Invoke(errorMessage);
                yield break;
            }

            onComplete?.Invoke(result);
        }

    }
}

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
using Random = System.Random;

namespace GLMod
{
    [BepInPlugin(Id, "GLMod", "5.2.0")]
    [BepInProcess("Among Us.exe")]
    public class GLMod : BasePlugin
    {
        public const string Id = "glmod";

        public Harmony Harmony { get; } = new Harmony(Id);

        public static ConfigEntry<string> connectionState { get; private set; }
        public static ConfigEntry<string> translations { get; private set; }
        public static ConfigEntry<string> stepConf { get; private set; }
        public static ConfigEntry<string> stepRpc { get; private set; }
        public static ConfigEntry<string> enabled { get; private set; }
        public static ConfigEntry<string> supportId { get; private set; }

        public static string token = null;

        public const string api = "https://goodloss.fr/api";

        public static Boolean logged;
        public static GLGame currentGame;
        public static List<string> enabledServices;
        public static string gameCode = "XXXXXX";
        public static string gameMap = "Unknown";
        public static string configPath;
        public static string modName = "Vanilla";
        public static List<GLLanguage> languages;
        public static string lg = "en";
        public static int step = 0;
        public static string supportIdChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz123456789";
        public static List<int> steamOwnerships = new List<int>() { };
        public static bool debug = false;
        public static bool withUnityExplorer = false;
        internal static BepInEx.Logging.ManualLogSource Logger;
        public static List<GLItem> items = new List<GLItem>() { };
        public static bool isBanned = false;
        public static string banReason = "";

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
          
            Random random = new Random();
            string newSupportId = new string(Enumerable.Repeat(supportIdChars, 10)
                .Select(s => s[random.Next(s.Length)]).ToArray());
            supportId = Config.Bind("GoodLoss", "Support Id", newSupportId);

            GLMod.findModName();
            log("Mod " + modName + " configured");

            GLMod.enabledServices = new List<string>() { };
            GLMod.enableService("StartGame");
            GLMod.enableService("EndGame");
            GLMod.enableService("Tasks");
            GLMod.enableService("TasksMax");
            GLMod.enableService("Exiled");
            GLMod.enableService("Kills");
            GLMod.enableService("BodyReported");
            GLMod.enableService("Emergencies");
            GLMod.enableService("Turns");
            GLMod.enableService("Votes");
            GLMod.enableService("Roles");
            GLMod.enableService("Tasks");

            stepConf.Value = "YES";
            stepRpc.Value = "YES";

            if (translations.Value.ToLower() == "yes")
            {
                _ = GLMod.loadTranslations();
            }
            log("Mod loaded");

            CoroutineRunner.Init();

            Harmony.PatchAll();
        }


        /*
         * Items
         */

        public static IEnumerator reloadItems()
        {
            if (!logged)
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
            if (!logged)
                yield break;

            var form = new Dictionary<string, string> { { "token", token } };

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
            if (step != 0)
            {
                log("[CreateGame] Duplicate call");
                step = 0;
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
                step = 1;
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
            if (step != 1)
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
                    step = 2;
                }
            }
            catch (Exception e)
            {
                log("[AddPlayer] Catch exception check " + e.Message);
                return;
            }

        }

        // Step 3 : Send Game (nothing for non host)
        public static IEnumerator SendGame()
        {
            GLMod.log("Sending game...");

            if (!AmongUsClient.Instance.AmHost)
            {
                step = 3;
                yield break;
            }

            if (step != 2)
            {
                log("[SendGame] Duplicate call");
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
                yield break;
            }

            currentGame.id = responseString;
            step = 3;
            CoroutineRunner.Run(SyncGameId());
            GLMod.log("Game sent.");
        }

        // Step 4: Sync Game Id for host
        public static IEnumerator SyncGameId()
        {
            if (!AmongUsClient.Instance.AmHost) { step = 4; yield break; }
            if (step != 3) { log("[SyncGameId] Duplicate call"); yield break; }
            if (currentGame == null || string.IsNullOrEmpty(currentGame.id)) { log("[SyncGameId] Game null/id"); yield break; }

            GLMod.stepRpc.Value = "NO";
            yield return new WaitForSeconds(5f);

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

            GLMod.step = 4;
        }

        // External process : Add My Player
        public static IEnumerator AddMyPlayer()
        {
            if (logged == false)
            {
                yield break;
            }

            if (currentGame == null)
            {
                log("[AddMyPlayer] Current Game null");
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
                yield break;
            }

            if (myPlayer == null)
            {
                GLMod.log("[AddMyPlayer] My player null");
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
        }

        // Step 5 : Set Winner Teams
        public static void SetWinnerTeams(List<string> winners)
        {
            if (!AmongUsClient.Instance.AmHost)
            {
                step = 0;
                return;
            }
            if (step != 4 && step != 5)
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
                step = 5;
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
                step = 0;
                return;
            }
            if (step != 4 && step != 5)
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
                step = 5;
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
                step = 0;
                yield break;
            }

            if (step != 5)
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
                step = 0;
                GLMod.log("Game ended.");
            }
        }

        /*
         * Services
         */


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

        public static void enableService(string service)
        {
            if (!enabledServices.Contains(service))
            {
                enabledServices.Add(service);
            }
        }

        public static Boolean existService(string service)
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
            try
            {
                if (!logged || token == "")
                {
                    return "";
                }
                else
                {
                    return token.Substring(0, token.IndexOf("#"));
                }
            } catch (Exception e)
            {
                log("[getAccountName] Catch exception " + e.Message);
                return "";
            }
            
        }

        /*
         * Connnection
         */

        public static IEnumerator login()
        {
            var steamId = SteamUser.GetSteamID().m_SteamID.ToString();
            var form = new Dictionary<string, string> { { "steamId", steamId } };

            ApiResponse response = null;

            // Appel de la coroutine ApiService
            yield return ApiService.PostFormWithErrorHandlingAsync(api + "/user/login", form,
                apiResponse => {
                    response = apiResponse;
                }
            );

            // Vérifier si la réponse est null (ne devrait jamais arriver avec PostFormWithErrorHandlingAsync)
            if (response == null)
            {
                log("Login failed, no response");
                SetLoginState(false, "", false, "");
                yield break;
            }

            // Interprète la réponse
            if (response.IsSuccess)
            {
                log("Login success");
                token = response.Content;
                connectionState.Value = "Yes";
                logged = true;
                isBanned = false;
                banReason = "";
            }
            else if (response.StatusCode == 403)
            {
                var trimmed = response.Content?.Trim('"').Trim();
                if (!string.IsNullOrEmpty(trimmed) && trimmed.StartsWith("Banned: ", System.StringComparison.OrdinalIgnoreCase))
                {
                    isBanned = true;
                    banReason = trimmed.Substring("Banned: ".Length);
                    log("User banned, reason: " + banReason);
                }
                else
                {
                    log($"Login failed 403: {trimmed}");
                    isBanned = false;
                    banReason = "";
                }
                SetLoginState(false, "", isBanned, banReason);
            }
            else
            {
                log($"Login failed - Status code: {response.StatusCode}");
                SetLoginState(false, "", false, "");
            }
        }


        private static void SetLoginState(bool ok, string tok, bool banned, string reason)
        {
            logged = ok;
            token = tok;
            isBanned = banned;
            banReason = reason;
            connectionState.Value = ok ? "Yes" : "No";
        }

        public static void logout()
        {
            try
            {
                if (token != "")
                {
                    logged = false;
                    token = "";
                }
            } catch (Exception e)
            {
                log("[logout] Catch exception " + e.Message);
            }
           
        }

        public static Boolean isLoggedIn()
        {
            return logged;
        }

        public static IEnumerator getRank(string customModName, System.Action<GLRank> onComplete)
        {
            if (customModName == null)
            {
                customModName = GLMod.modName;
            }

            GLRank errorRank = new GLRank();

            if (!logged)
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
            string languagesURL = api + "/trans";
            string lg = null;
            string error = null;
            bool done = false;

            // Charger la liste des langues
            System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    lg = await HttpHelper.Client.GetStringAsync(languagesURL);
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

            // Attendre la fin du chargement
            while (!done)
                yield return null;

            // Vérifier l'erreur
            if (error != null)
            {
                GLMod.log("Load translations error: " + error);
                yield break;
            }

            // Désérialiser les langues
            languages = GLJson.Deserialize<List<GLLanguage>>(lg);

            // Lancer toutes les coroutines de chargement en parallèle
            List<Coroutine> loadingCoroutines = new List<Coroutine>();
            foreach (GLLanguage l in languages)
            {
                loadingCoroutines.Add(CoroutineRunner.Run(l.load()));
            }

            // Attendre que toutes les coroutines se terminent
            // (Note: Il n'y a pas de mécanisme natif pour attendre plusieurs coroutines,
            // donc on attend juste un peu pour laisser le temps aux coroutines de se terminer)
            // Pour une vraie synchronisation, il faudrait un système de compteur
            yield return new WaitForSeconds(2f); // Ajustez selon vos besoins
        }

        // Version améliorée avec compteur pour vraiment attendre la fin :
        public static IEnumerator loadTranslationsWithCounter()
        {
            string languagesURL = api + "/trans";
            string lg = null;
            string error = null;
            bool done = false;

            // Charger la liste des langues
            System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    lg = await HttpHelper.Client.GetStringAsync(languagesURL);
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

            // Attendre la fin du chargement
            while (!done)
                yield return null;

            // Vérifier l'erreur
            if (error != null)
            {
                GLMod.log("Load translations error: " + error);
                yield break;
            }

            // Désérialiser les langues
            languages = GLJson.Deserialize<List<GLLanguage>>(lg);

            // Compteur pour suivre les chargements terminés
            int totalLanguages = languages.Count;
            int loadedLanguages = 0;

            // Lancer toutes les coroutines de chargement en parallèle
            foreach (GLLanguage l in languages)
            {
                CoroutineRunner.Run(loadLanguageWithCallback(l, () => { loadedLanguages++; }));
            }

            // Attendre que toutes les langues soient chargées
            while (loadedLanguages < totalLanguages)
                yield return null;

            GLMod.log($"All {totalLanguages} languages loaded successfully.");
        }

        // Wrapper pour ajouter un callback à la fin du chargement
        private static IEnumerator loadLanguageWithCallback(GLLanguage language, System.Action onComplete)
        {
            yield return language.load();
            onComplete?.Invoke();
        }

        public static string translate(string toTranslate)
        {
            List<GLTranslation> current = languages.Find(l => l.code == lg).translations;
            GLTranslation tr = current.Find(t => t.original == toTranslate);
            if (tr != null)
            {
                return tr.translation;
            }
            else
            {
                return toTranslate;
            }

        }

        public static bool setLg(string lg)
        {
            GLMod.lg = lg.ToLower();
            return true;
        }

        public static string getLg()
        {
            return lg;
        }

        public static string getNameFromCode(string code)
        {
            return languages.Find(l => l.code == code).name;
        }

        public static string getCodeFromName(string name)
        {
            return languages.Find(l => l.name == name).code;
        }

        public static string getMapName()
        {
            try
            {
                if (GameOptionsManager.Instance.currentGameOptions.MapId == (byte)MapNames.Skeld)
                    return "The Skeld";

                if (GameOptionsManager.Instance.currentGameOptions.MapId == (byte)MapNames.MiraHQ)
                    return "MiraHQ";

                if (GameOptionsManager.Instance.currentGameOptions.MapId == (byte)MapNames.Polus)
                    return "Polus";

                if (GameOptionsManager.Instance.currentGameOptions.MapId == (byte)MapNames.Dleks)
                    return "dlekSehT";

                if (GameOptionsManager.Instance.currentGameOptions.MapId == (byte)MapNames.Airship)
                    return "Airship";

                if (GameOptionsManager.Instance.currentGameOptions.MapId == (byte)MapNames.Fungle)
                    return "The Fungle";

                return "Unknown";
            } catch (Exception e)
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
                    result = isValid;
                },
                error => {
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

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Hazel;
using Steamworks;
using System;
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
    [BepInPlugin(Id, "GLMod", "5.1.5")]
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
        public static GLRank rank;
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

        public async override void Load()
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

            Harmony.PatchAll();
        }

        /*
         * Items
         */

        public static async Task reloadItems()
        {
            if (!logged) return;
            // Load from API using toke

            try
            {
                var form = new Dictionary<string, string>
                {
                    { "player", getAccountName() }
                };

                var responseString = await ApiService.PostFormAsync(api + "/player/challengerItems", form);

                items = GLJson.Deserialize<List<GLItem>>(responseString);
            }
            catch (HttpRequestException ex)
            {
                // Log l'erreur si nécessaire
                log("Erreur HTTP : " + ex.Message);
            }
            catch (Exception ex)
            {
                // Pour tout autre type d'erreur
                log("Erreur : " + ex.Message);
            }

            //using (var client = Utils.getClient())
            //{
            //    var values = new NameValueCollection();

            //    try
            //    {
            //        values["player"] = getAccountName();
            //        var response = client.UploadValues(api + "/player/challengerItems", values);
            //        var responseString = Encoding.Default.GetString(response);
            //        responseString = System.Text.RegularExpressions.Regex.Unescape(responseString);
            //        items = GLJson.Deserialize<List<GLItem>>(responseString);
            //    }
            //    catch (WebException e)
            //    {

            //    }
            //}
        }

        public static Boolean isUnlocked(string id)
        {
            return GLMod.items.FindAll(s => s.id == id) != null && GLMod.items.FindAll(s => s.id == id).Count > 0;
        }

        /*
         * Dlc
         */

        public static async Task reloadDlcOwnerships()
        {
            if (!logged) return;
            // Load from API using token

            try
            {
                var form = new Dictionary<string, string>
                {
                    { "token", token }
                };

                var responseString = await ApiService.PostFormAsync(api + "/user/steamownerships", form);

                steamOwnerships = GLJson.Deserialize<List<int>>(responseString);
            }
            catch (HttpRequestException ex)
            {
                // Log si besoin
                log("Erreur HTTP : " + ex.Message);
            }
            catch (Exception ex)
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
        public static async Task SendGame()
        {
            GLMod.log("Sending game...");
            if (!AmongUsClient.Instance.AmHost)
            {
                step = 3;
                return;
            }

            if (step != 2)
            {
                log("[SendGame] Duplicate call");
                return;
            }

            if (currentGame.modName == null)
            {
                log("[SendGame] Modname null");
            }

            try
            {
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

                var responseString = await ApiService.PostFormAsync(api + "/game/start", form);

                currentGame.id = responseString;
                step = 3;

                await SyncGameId();
                GLMod.log("Game sent.");
            }
            catch (Exception e)
            {
                log("[SendGame] fail, error: " + e.Message);
            }
        }

        // Step 4: Sync Game Id for host
        public static async Task SyncGameId()
        {
            if (!AmongUsClient.Instance.AmHost)
            {
                step = 4;
                return;
            }

            if (step != 3)
            {
                log("[SyncGameId] Duplicate call");
                return;
            }

            if (currentGame == null)
            {
                log("[SyncGameId] Current Game null");
                return;
            }

            if (string.IsNullOrEmpty(currentGame.id))
            {
                log("[SyncGameId] Game Id null or empty");
                return;
            }

            try
            {
                GLMod.stepRpc.Value = "NO";

                await Task.Delay(5000); // attendre 5 secondes

                await Task.Run(() =>
                {
                    try
                    {
                        List<string> values = new List<string>() { GLMod.currentGame.getId().ToString() };
                        GLRPCProcedure.makeRpcCall(1, values);
                        GLMod.step = 4;
                    }
                    catch (Exception ex)
                    {
                        GLMod.log("[SyncGameId] RPC fail : " + ex.Message);
                    }
                });
            } catch (Exception e)
            {
                log("[SyncGameId] Catch exception " + e.Message);
            }
        }

        // External process : Add My Player
        public static async Task AddMyPlayer()
        {
            if (logged == false)
            {
                return;
            }

            if (currentGame == null) {
                log("[AddMyPlayer] Current Game null");
                return;
            }

            await Task.Run(async () =>
            {
                PlayerControl me;
                GLPlayer myPlayer;

                try
                {
                    me = PlayerControl.LocalPlayer;
                    myPlayer = GLMod.currentGame.players.FindAll(p => p.playerName == me.Data.PlayerName)[0];
                }
                catch (Exception ex)
                {
                    GLMod.log("[AddMyPlayer] Catch exception " + ex.Message);
                    return;
                }

                if (myPlayer == null)
                {
                    GLMod.log("[AddMyPlayer] My player null");
                    return;
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
                
                while (string.IsNullOrEmpty(GLMod.currentGame.id))
                {
                    Thread.Sleep(100);
                }

                try
                {
                    var form = new Dictionary<string, string>
                {
                    { "gameId", GLMod.currentGame.id },
                    { "login", GLMod.getAccountName() },
                    { "playerName", me.Data.PlayerName }
                };

                    // On ignore la réponse, comme dans ton code d'origine
                    await ApiService.PostFormAsync(GLMod.api + "/game/addMyPlayer", form);
                }
                catch (Exception e)
                {
                    GLMod.log("[AddMyPlayer] Add my player fail, error: " + e.Message);
                }
            });
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
        public static async Task EndGame()
        {
            GLMod.log("Ending game...");
            if (!AmongUsClient.Instance.AmHost)
            {
                step = 0;
                return;
            }

            if (step != 5)
            {
                log("[EndGame] Call when in step " + step);
                return;
            }
            
            if (currentGame == null)
            {
                log("[EndGame] Current Game null");
                return;
            }

            BackgroundEvents.endBackgroundProcess();

            try
            {
                string json = GLJson.Serialize<GLGame>(currentGame);
                
                // DEBUG
                // GLMod.log("endGame: "+ json);

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await HttpHelper.Client.PostAsync(api + "/game/end", content);
                response.EnsureSuccessStatusCode();

                // Lecture de la réponse si tu en as besoin
                var result = await response.Content.ReadAsStringAsync();

                step = 0;
                GLMod.log("Game ended.");
            }
            catch (Exception e)
            {
                log("[EndGame] End Game fail, error: " + e.Message);
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

        public static async Task login()
        {
            try
            {
                var steamId = SteamUser.GetSteamID().ToString();
                var form = new Dictionary<string, string>
        {
            { "steamId", steamId }
        };
                var response = await ApiService.PostFormWithErrorHandlingAsync(api + "/user/login", form);

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
                    // Gestion spécifique de l'erreur 403 (bannissement)
                    var trimmedContent = response.Content?.Trim();

                    // Retirer les guillemets doubles si présents
                    if (trimmedContent != null && trimmedContent.StartsWith("\"") && trimmedContent.EndsWith("\""))
                    {
                        trimmedContent = trimmedContent.Substring(1, trimmedContent.Length - 2);
                    }

                    if (!string.IsNullOrEmpty(trimmedContent) && trimmedContent.StartsWith("Banned: ", StringComparison.OrdinalIgnoreCase))
                    {
                        isBanned = true;
                        banReason = trimmedContent.Substring("Banned: ".Length);
                        log("User banned, reason: " + banReason);
                    }
                    else
                    {
                        log($"Login failed for unknown reason - Status code: {response.StatusCode}");
                        isBanned = false;
                        banReason = "";
                    }
                    token = "";
                    connectionState.Value = "No";
                    logged = false;
                }
                else
                {
                    log($"Login failed for unknown reason - Status code: {response.StatusCode}");
                    token = "";
                    connectionState.Value = "No";
                    logged = false;
                    isBanned = false;
                    banReason = "";
                }
            }
            catch (Exception e)
            {
                log("Login failed, error: " + e.Message);
                token = "";
                connectionState.Value = "No";
                logged = false;
                isBanned = false;
                banReason = "";
            }
        }

        public static void logout()
        {
            try
            {
                if (token != "")
                {
                    rank = new GLRank();
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

        public static async Task getRank()
        {
            if (!logged) return;
            var form = new Dictionary<string, string>
            {
                { "player", getAccountName() },
                { "mod", modName }
            };

            try
            {
                var responseString = await ApiService.PostFormAsync(api + "/player/rank", form);
                rank = GLJson.Deserialize<GLRank>(responseString);
            }
            catch (HttpRequestException)
            {
                rank = new GLRank();
            }
        }

        /*
         * Translations
         */

        public static async Task loadTranslations()
        {
            string languagesURL = api + "/trans";
            string lg = "";
            try
            {
                lg = await HttpHelper.Client.GetStringAsync(languagesURL);
            }
            catch (Exception e)
            {
                GLMod.log("Load translations error: " + e.Message);
            }
            languages = GLJson.Deserialize<List<GLLanguage>>(lg);

            List<Task> tasks = new List<Task>();

            foreach (GLLanguage l in languages)
            {
                tasks.Add(l.load());
            }
            await Task.WhenAll(tasks);
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

        public static async Task<string> getApiData(string id)
        {
            try
            {
                var form = new Dictionary<string, string>
                {
                    { "id", id }
                };

                var responseString = await ApiService.PostFormAsync(api + "/data", form);

                return responseString;
            }
            catch (HttpRequestException ex)
            {
                // Log l'erreur si nécessaire
                log("Erreur HTTP : " + ex.Message);
            }
            catch (Exception ex)
            {
                // Pour tout autre type d'erreur
                log("Erreur : " + ex.Message);
            }
            return "";
        }

        public static async Task<string> getChecksum(string checksumId)
        {
            return await getApiData("checksum_"+checksumId);
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

        public static async Task<bool> verifyDll(string checksumId, string dllPath)
        {
            string localChecksum = GLMod.CalculateFileSHA512(dllPath);
            string remoteChecksum = await GLMod.getChecksum(checksumId);
            log("Local checksum: " + localChecksum);
            log("Remote checksum: " + remoteChecksum);
            if (localChecksum == remoteChecksum)
            {
                log("Valid checksum");
                return true;
            }
            log("Invalid checksum");
            return false;
        }

        private static async Task<bool> verifyGLMod()
        {
            var pluginAttribute = typeof(GLMod).GetCustomAttribute<BepInPlugin>();
            string version = pluginAttribute?.Version.ToString();
            log(version);
            return await verifyDll("glmod"+ version, "BepInEx/plugins/glmod.dll");
        }
    }
}

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Random = System.Random;

namespace GLMod
{
    [BepInPlugin(Id, "GLMod", "4.0.0")]
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

        public static List<GLItem> items = new List<GLItem>() { };

        public override async void Load()
        {
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

            stepConf.Value = "YES";
            stepRpc.Value = "YES";

            if (translations.Value.ToLower() == "yes")
            {
                GLMod.loadTranslations();
            }

            Harmony.PatchAll();
        }

        /*
         * Items
         */

        public static void reloadItems()
        {
            if (!logged) return;
            // Load from API using token

            using (var client = Utils.getClient())
            {
                var values = new NameValueCollection();

                try
                {
                    values["player"] = getAccountName();
                    var response = client.UploadValues(api + "/player/challengerItems", values);
                    var responseString = Encoding.Default.GetString(response);
                    responseString = System.Text.RegularExpressions.Regex.Unescape(responseString);
                    items = GLJson.Deserialize<List<GLItem>>(responseString);
                }
                catch (WebException e)
                {

                }
            }
        }

        public static Boolean isUnlocked(string id)
        {
            return GLMod.items.FindAll(s => s.id == id) != null && GLMod.items.FindAll(s => s.id == id).Count > 0;
        }

        /*
         * Dlc
         */

        public static void reloadDlcOwnerships()
        {
            if (!logged) return;
            // Load from API using token

            using (var client = Utils.getClient())
            {
                var values = new NameValueCollection();

                try
                {
                    values["token"] = token;
                    var response = client.UploadValues(api + "/user/steamownerships", values);
                    var responseString = Encoding.Default.GetString(response);
                    steamOwnerships = GLJson.Deserialize<List<int>>(responseString);
                }
                catch (WebException e)
                {
                }
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
                if (file.Name.EndsWith(".mm"))
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
            GLMod.logWithoutInfo("Challenger - Client id: " + supportId.Value + " - Player: " + PlayerControl.LocalPlayer.Data.PlayerName + " - Info: " + text);
        }

        public static void logError(string err)
        {
            GLMod.logWithoutInfo("Challenger - Client id: " + supportId.Value + " - Player: " + PlayerControl.LocalPlayer.Data.PlayerName + " - Error: " + err);
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


        public static void logWithoutInfo(string text)
        {
            if (!String.IsNullOrEmpty(text))
            {
                using (var client = Utils.getClient())
                {
                    var values = new NameValueCollection();
                    values["text"] = text;

                    var response = client.UploadValues(api + "/log", values);

                    var responseString = Encoding.Default.GetString(response);

                }
            }

        }


        // Step 1 : Create / Start Game

        public static void StartGame(string code, string map, Boolean ranked)
        {
            CreateGame(code, map, ranked);
            //SendGame();
            //SyncGameId();
        }

        public static void CreateGame(string code, string map, Boolean ranked = false)
        {
            if (step != 0)
            {
                logError("[CreateGame] Duplicate call");
                step = 0;
            }
            if (string.IsNullOrEmpty(code))
            {
                logError("[CreateGame] Code null or empty");
            }
            if (string.IsNullOrEmpty(map))
            {
                logError("[CreateGame] Map null or empty");
            }

            try
            {
                currentGame = new GLGame(code, map, ranked, modName);
            } catch (Exception e)
            {
                logError("[CreateGame] Catch exception " + e.Message);
                return;
            }

            step = 1;
        }

        // Step 2 : Add Player until all players recorded
        public static void AddPlayer(string playerName, string role, string team)
        {
            if (step != 1)
            {
                logError("[AddPlayer] Call when in step " + step);
                return;
            }

            if (currentGame == null)
            {
                logError("[AddPlayer] Current Game null");
                return;
            }

            if (string.IsNullOrEmpty(playerName))
            {
                logError("[AddPlayer] PlayerName null or empty");
                return;
            }

            if (string.IsNullOrEmpty(role))
            {
                logError("[AddPlayer] Role null or empty");
                return;
            }

            if (string.IsNullOrEmpty(team))
            {
                logError("[AddPlayer] Team null or empty");
                return;
            }

            try
            {
                currentGame.addPlayer(null, playerName, role, team);
            } catch (Exception e)
            {
                logError("[AddPlayer] Catch exception " + e.Message);
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
                logError("[AddPlayer] Catch exception check " + e.Message);
                return;
            }

        }

        // Step 3 : Send Game (nothing for non host)
        public static void SendGame()
        {
            if (!AmongUsClient.Instance.AmHost)
            {
                step = 3;
                return;
            }

            if (step != 2)
            {
                logError("[SendGame] Duplicate call");
                return;
            }

            if (currentGame.modName == null)
            {
                logError("[SendGame] Modname null");
            }

            try
            {
                using (var client = Utils.getClient())
                {
                    var values = new NameValueCollection();
                    values["code"] = currentGame.code;
                    values["map"] = currentGame.map;
                    values["ranked"] = currentGame.ranked;
                    values["modName"] = currentGame.modName;
                    values["players"] = GLJson.Serialize<List<GLPlayer>>(currentGame.players);

                    var response = client.UploadValues(api + "/game/start", values);

                    var responseString = Encoding.Default.GetString(response);

                    currentGame.id = responseString;

                    step = 3;

                    SyncGameId();
                }
            } catch (Exception e)
            {
                logError("[SendGame] fail");
            }
        }

        // Step 4: Sync Game Id for host
        public static void SyncGameId()
        {
            if (!AmongUsClient.Instance.AmHost)
            {
                step = 4;
                return;
            }

            if (step != 3)
            {
                logError("[SyncGameId] Duplicate call");
                return;
            }

            if (currentGame == null)
            {
                logError("[SyncGameId] Current Game null");
                return;
            }

            if (string.IsNullOrEmpty(currentGame.id))
            {
                logError("[SyncGameId] Game Id null or empty");
                return;
            }

            try
            {
                GLMod.stepRpc.Value = "NO";

                System.Timers.Timer timer = new System.Timers.Timer(5000);
                timer.Elapsed += BackgroundWorkers.OnTimedEvent;
                timer.AutoReset = false;
                timer.Enabled = true;
            } catch (Exception e)
            {
                logError("[SyncGameId] Catch exception " + e.Message);
            }
        }

        // External process : Add My Player
        public static void AddMyPlayer()
        {
            if (logged == false)
            {
                return;
            }

            if (currentGame == null) {
                logError("[AddMyPlayer] Current Game null");
                return;
            }

            BackgroundWorker backgroundWorker = new BackgroundWorker();
            backgroundWorker.WorkerReportsProgress = true;
            backgroundWorker.DoWork += new DoWorkEventHandler(BackgroundWorkers.backgroundWorkerAddPlayer_DoWork);
            backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BackgroundWorkers.backgroundWorker_RunWorkerCompleted);
            backgroundWorker.ProgressChanged += new ProgressChangedEventHandler(BackgroundWorkers.backgroundWorker_ProgressChanged);
            backgroundWorker.RunWorkerAsync();
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
                logError("[SetWinnerTeams] Call when in step " + step);
                return;
            }
            if (currentGame == null)
            {
                logError("[SetWinnerTeams] Current Game null");
                return;
            }

            if (winners.Count <= 0)
            {
                logError("[SetWinnerTeams] Winners empty");
                return;
            }

            try
            {
                currentGame.setWinners(winners);
                step = 5;
            }
            catch (Exception e)
            {
                logError("[SetWinnerTeams] Set Winner Teams fail");
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
                logError("[AddWinnerPlayer] Call when in step " + step);
                return;
            }
            if (currentGame == null)
            {
                logError("[AddWinnerPlayer] Current Game null");
                return;
            }

            if (string.IsNullOrEmpty(playerName))
            {
                logError("[AddWinnerPlayer] Player name null or empty empty");
                return;
            }

            try
            {
                currentGame.players.FindAll(p => p.playerName == playerName).ForEach(p => p.setWin());
                step = 5;
            } catch (Exception e)
            {
                logError("[AddWinnerPlayer] Add Winner Player fail");
            }
            
        }

        // Step 6 : End Game
        public static void EndGame()
        {
            if (!AmongUsClient.Instance.AmHost)
            {
                step = 0;
                return;
            }

            if (step != 5)
            {
                logError("[EndGame] Call when in step " + step);
                return;
            }
            
            if (currentGame == null)
            {
                logError("[EndGame] Current Game null");
                return;
            }

            try
            {
                string json = GLJson.Serialize<GLGame>(currentGame);
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(api + "/game/end");
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(json);
                }

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                }
                step = 0;
            } catch (Exception e)
            {
                logError("[EndGame] End Game fail");
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
                logError("[AddAction] Catch exception " + e.Message);
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
                logError("[getAccountName] Catch exception " + e.Message);
                return "";
            }
            
        }

        /*
         * Connnection
         */

        public static async Task login()
        {
            using (var client = Utils.getClient())
            {
                var values = new NameValueCollection();
                
                try
                {
                    values["steamId"] = SteamUser.GetSteamID().ToString();
                    var response = client.UploadValues(api+"/user/login", values);
                    var responseString = Encoding.Default.GetString(response);
                    responseString = System.Text.RegularExpressions.Regex.Unescape(responseString);
                    token = responseString;
                    connectionState.Value = "Yes";
                    logged = true;
                }
                catch (WebException e)
                {
                    token = "";
                    connectionState.Value = "No";
                    logged = false;
                }
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
                logError("[logout] Catch exception " + e.Message);
            }
           
        }

        public static Boolean isLoggedIn()
        {
            return logged;
        }

        public static void getRank()
        {
            if (!logged) return;
            using (var client = Utils.getClient())
            {
                var values = new NameValueCollection();
                values["player"] = getAccountName();

                try
                {
                    var response = client.UploadValues(api+"/player/rank", values);
                    var responseString = Encoding.Default.GetString(response);
                    rank = GLJson.Deserialize<GLRank>(responseString);
                    return;
                }
                catch (WebException e)
                {
                    rank = new GLRank();
                    return;
                }
            }
        }

        /*
         * Translations
         */

        public static void loadTranslations()
        {
            string languagesURL = api + "/trans";
            string lg = "";
            try
            {
                using (var client = Utils.getClient())
                {
                    lg = client.DownloadString(languagesURL);
                }
            }
            catch (Exception e)
            {
                
            }
            languages = GLJson.Deserialize<List<GLLanguage>>(lg);

            foreach (GLLanguage l in languages)
            {
                l.load();
            }
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
                //if (PlayerControl.GameOptions.MapId == 0)
                //    return "The Skeld";

                //if (PlayerControl.GameOptions.MapId == 1)
                //    return "MiraHQ";

                //if (PlayerControl.GameOptions.MapId == 2)
                //    return "Polus";

                //if (PlayerControl.GameOptions.MapId == 3)
                //    return "dlekSehT";

                //if (PlayerControl.GameOptions.MapId == 4)
                //    return "Airship";

                //if (PlayerControl.GameOptions.MapId == 5)
                //    return "Submerged";

                return "Unknown";
            } catch (Exception e)
            {
                logError("[getMapName] Catch exception " + e.Message);
                return "Unknown";
            }
        }
    }
}

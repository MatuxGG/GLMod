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
        public static IGameStateManager GameStateManager { get; private set; }
        public static IServiceManager ServiceManager { get; private set; }
        public static IItemService ItemService { get; private set; }
        public static IConfigurationService ConfigService { get; private set; }

        public static ConfigEntry<string> connectionState { get; private set; }
        public static ConfigEntry<string> translations { get; private set; }
        public static ConfigEntry<string> stepConf { get; private set; }
        public static ConfigEntry<string> stepRpc { get; private set; }
        public static ConfigEntry<string> enabled { get; private set; }
        public static ConfigEntry<string> supportId { get; private set; }

        public const string api = GameConstants.API_ENDPOINT;

        // Deprecated - Use GameStateManager instead
        [Obsolete("Use GameStateManager.CurrentGame instead")]
        public static GLGame currentGame => GameStateManager?.CurrentGame;

        [Obsolete("Use ServiceManager.EnabledServices instead")]
        public static List<string> enabledServices => ServiceManager?.EnabledServices;

        [Obsolete("Use GameStateManager.GameCode instead")]
        public static string gameCode
        {
            get => GameStateManager?.GameCode ?? GameConstants.DEFAULT_GAME_CODE;
            set { if (GameStateManager != null) GameStateManager.GameCode = value; }
        }

        [Obsolete("Use GameStateManager.GameMap instead")]
        public static string gameMap
        {
            get => GameStateManager?.GameMap ?? GameConstants.DEFAULT_MAP_NAME;
            set { if (GameStateManager != null) GameStateManager.GameMap = value; }
        }

        [Obsolete("Use ConfigService.ConfigPath instead")]
        public static string configPath => ConfigService?.ConfigPath;

        [Obsolete("Use ConfigService.ModName instead")]
        public static string modName => ConfigService?.ModName ?? "Vanilla";

        [Obsolete("Use GameStateManager.Step instead")]
        public static GameStep step
        {
            get => GameStateManager?.Step ?? GameStep.Initial;
            set { if (GameStateManager != null) GameStateManager.Step = value; }
        }

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
        // Deprecated - Use ItemService instead
        [Obsolete("Use ItemService.SteamOwnerships instead")]
        public static List<int> steamOwnerships => ItemService?.SteamOwnerships ?? new List<int>();

        public static bool debug = false;
        public static bool withUnityExplorer = false;
        internal static BepInEx.Logging.ManualLogSource Logger;

        [Obsolete("Use ItemService.Items instead")]
        public static List<GLItem> items => ItemService?.Items ?? new List<GLItem>();

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
            string configPathValue = Path.GetDirectoryName(Config.ConfigFilePath);

            // Initialize services
            AuthService = new AuthenticationService(connectionState);
            TranslationService = new TranslationService();
            ConfigService = new ConfigurationService(Logger, configPathValue);
            GameStateManager = new GameStateManager(Logger, AuthService, api, stepRpc);
            ServiceManager = new ServiceManager();
            ItemService = new ItemService(Logger, AuthService, api);

            Random random = new Random();
            string newSupportId = new string(Enumerable.Repeat(GameConstants.SUPPORT_ID_CHARS, GameConstants.SUPPORT_ID_LENGTH)
                .Select(s => s[random.Next(s.Length)]).ToArray());
            supportId = Config.Bind("GoodLoss", "Support Id", newSupportId);

            ConfigService.FindModName();
            log("Mod " + ConfigService.ModName + " configured");

            // Enable default services
            ServiceManager.EnableService(ServiceType.StartGame);
            ServiceManager.EnableService(ServiceType.EndGame);
            ServiceManager.EnableService(ServiceType.Tasks);
            ServiceManager.EnableService(ServiceType.TasksMax);
            ServiceManager.EnableService(ServiceType.Exiled);
            ServiceManager.EnableService(ServiceType.Kills);
            ServiceManager.EnableService(ServiceType.BodyReported);
            ServiceManager.EnableService(ServiceType.Emergencies);
            ServiceManager.EnableService(ServiceType.Turns);
            ServiceManager.EnableService(ServiceType.Votes);
            ServiceManager.EnableService(ServiceType.Roles);

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
            if (ItemService == null)
                yield break;

            yield return ItemService.ReloadItems();
        }

        public static Boolean isUnlocked(string id)
        {
            return ItemService?.IsUnlocked(id) ?? false;
        }

        /*
         * Dlc
         */

        public static IEnumerator reloadDlcOwnerships()
        {
            if (ItemService == null)
                yield break;

            yield return ItemService.ReloadDlcOwnerships();
        }

        public static Boolean hasDlc(int appId)
        {
            return ItemService?.HasDlc(appId) ?? false;
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
            ConfigService?.FindModName();
        }

        public static void setModName(string modName)
        {
            ConfigService?.SetModName(modName);
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
            GameStateManager?.StartGame(code, map, ranked);
        }

        // Step 2 : Add Player until all players recorded
        public static void AddPlayer(string playerName, string role, string team, string color)
        {
            GameStateManager?.AddPlayer(playerName, role, team, color);
        }

        // Step 3 : Send Game (nothing for non host)
        public static IEnumerator SendGame(System.Action<bool> onComplete = null)
        {
            if (GameStateManager == null)
                yield break;

            yield return GameStateManager.SendGame(onComplete);
        }

        // Step 4: Sync Game Id for host
        public static IEnumerator SyncGameId(System.Action<bool> onComplete = null)
        {
            if (GameStateManager == null)
                yield break;

            yield return GameStateManager.SyncGameId(onComplete);
        }

        // External process : Add My Player
        public static IEnumerator AddMyPlayer(System.Action<bool> onComplete = null)
        {
            if (GameStateManager == null)
                yield break;

            yield return GameStateManager.AddMyPlayer(onComplete);
        }

        // Step 5 : Set Winner Teams
        public static void SetWinnerTeams(List<string> winners)
        {
            GameStateManager?.SetWinnerTeams(winners);
        }

        // Step 5 : Set Winner Player
        public static void AddWinnerPlayer(string playerName)
        {
            GameStateManager?.AddWinnerPlayer(playerName);
        }

        // Step 6 : End Game

        public static IEnumerator EndGame()
        {
            if (GameStateManager == null)
                yield break;

            yield return GameStateManager.EndGame();
        }

        /*
         * Services
         */

        public static void disableService(ServiceType service)
        {
            ServiceManager?.DisableService(service);
        }

        public static void disableService(string service)
        {
            ServiceManager?.DisableService(service);
        }

        public static void disableAllServices()
        {
            ServiceManager?.DisableAllServices();
        }

        public static void enableService(ServiceType service)
        {
            ServiceManager?.EnableService(service);
        }

        public static void enableService(string service)
        {
            ServiceManager?.EnableService(service);
        }

        public static bool existService(ServiceType service)
        {
            return ServiceManager?.ExistsService(service) ?? false;
        }

        public static bool existService(string service)
        {
            return ServiceManager?.ExistsService(service) ?? false;
        }

        public static void addAction(string source, string target, string action)
        {
            GameStateManager?.AddAction(source, target, action);
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

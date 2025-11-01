﻿using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GLMod.Enums;
using GLMod.Constants;
using GLMod.Services;
using Random = System.Random;
using GLMod.GLEntities;
using GLMod.Class;

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
        public static IRankService RankService { get; private set; }
        public static IIntegrityService IntegrityService { get; private set; }
        public static IMapService MapService { get; private set; }

        public static ConfigEntry<string> connectionState { get; private set; }
        public static ConfigEntry<string> translations { get; private set; }
        public static ConfigEntry<string> stepConf { get; private set; }
        public static ConfigEntry<string> stepRpc { get; private set; }
        public static ConfigEntry<string> enabled { get; private set; }
        public static ConfigEntry<string> supportId { get; private set; }

        public const string api = GameConstants.API_ENDPOINT;

        public static bool debug = false;
        public static bool withUnityExplorer = false;
        internal static BepInEx.Logging.ManualLogSource Logger;

        public override void Load()
        {
            Logger = Log;
            log("Loading mod...");

            InitializeConfiguration();
            InitializeServices();
            ConfigureDefaultSettings();

            log("Mod " + ConfigService.ModName + " configured");
            log("Mod loaded");

            CoroutineRunner.Init();
            StartVerificationAndPatching();
        }

        private void InitializeConfiguration()
        {
            connectionState = Config.Bind("GoodLoss", "Connected", "");
            enabled = Config.Bind("GoodLoss", "Enabled", "Yes");
            translations = Config.Bind("GoodLoss", "translations", "No");
            stepConf = Config.Bind("Validation", "steps", "");
            stepRpc = Config.Bind("Validation", "RPC", "");

            Random random = new Random();
            string newSupportId = new string(Enumerable.Repeat(GameConstants.SUPPORT_ID_CHARS, GameConstants.SUPPORT_ID_LENGTH)
                .Select(s => s[random.Next(s.Length)]).ToArray());
            supportId = Config.Bind("GoodLoss", "Support Id", newSupportId);
        }

        private void InitializeServices()
        {
            string configPathValue = Path.GetDirectoryName(Config.ConfigFilePath);

            AuthService = new AuthenticationService(connectionState);
            TranslationService = new TranslationService();
            ConfigService = new ConfigurationService(Logger, configPathValue);
            ConfigService.FindModName();
            GameStateManager = new GameStateManager(Logger, AuthService, ConfigService, api, stepRpc);
            ServiceManager = new ServiceManager();
            ItemService = new ItemService(Logger, AuthService, api);
            RankService = new RankService(Logger, AuthService, ConfigService, api);
            IntegrityService = new IntegrityService(Logger, api);
            MapService = new MapService(Logger);
        }

        private void ConfigureDefaultSettings()
        {
            // Enable default game event services
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

            // Load translations if enabled
            if (translations.Value.ToLower() == "yes")
            {
                CoroutineRunner.Run(TranslationService.LoadTranslations());
            }
        }

        private void StartVerificationAndPatching()
        {
            CoroutineRunner.Run(IntegrityService.VerifyGLMod(result =>
            {
                log("GLMod verified: " + result);
            }));

            Harmony.PatchAll();
        }

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

        // Legacy wrapper methods - prefer using services directly
        public static void StartGame(string code, string map, Boolean ranked) => GameStateManager?.StartGame(code, map, ranked);
        public static void AddPlayer(string playerName, string role, string team, string color) => GameStateManager?.AddPlayer(playerName, role, team, color);
        public static IEnumerator SendGame(System.Action<bool> onComplete = null) => GameStateManager?.SendGame(onComplete);
        public static IEnumerator SyncGameId(System.Action<bool> onComplete = null) => GameStateManager?.SyncGameId(onComplete);
        public static IEnumerator AddMyPlayer(System.Action<bool> onComplete = null) => GameStateManager?.AddMyPlayer(onComplete);
        public static void SetWinnerTeams(List<string> winners) => GameStateManager?.SetWinnerTeams(winners);
        public static void AddWinnerPlayer(string playerName) => GameStateManager?.AddWinnerPlayer(playerName);
        public static IEnumerator EndGame() => GameStateManager?.EndGame();
        public static void addAction(string source, string target, string action) => GameStateManager?.AddAction(source, target, action);

        public static void enableService(ServiceType service) => ServiceManager?.EnableService(service);
        public static void enableService(string service) => ServiceManager?.EnableService(service);
        public static void disableService(ServiceType service) => ServiceManager?.DisableService(service);
        public static void disableService(string service) => ServiceManager?.DisableService(service);
        public static void disableAllServices() => ServiceManager?.DisableAllServices();
        public static bool existService(ServiceType service) => ServiceManager?.ExistsService(service) ?? false;
        public static bool existService(string service) => ServiceManager?.ExistsService(service) ?? false;

    }
}

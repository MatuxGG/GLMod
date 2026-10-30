using BepInEx;
using BepInEx.Unity.IL2CPP;
using GLMod.GLEntities;
using GLMod.Services.Implementations;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using GLModPlugin = global::GLMod.GLMod;

namespace GLMod.TestHarness
{
    /// <summary>
    /// End-to-end test harness for GLMod. Inert unless GLMOD_TEST_INSTANCE is set,
    /// which run-e2e.ps1 does for each of the 4 game instances it launches.
    /// </summary>
    [BepInPlugin(Id, "GLMod.TestHarness", Version)]
    [BepInProcess("Among Us.exe")]
    [BepInDependency(GLModPlugin.Id)]
    public class TestHarnessPlugin : BasePlugin
    {
        public const string Id = "glmod.testharness";
        public const string Version = "1.0.0";

        public Harmony Harmony { get; } = new Harmony(Id);

        public override void Load()
        {
            HarnessContext context;
            try
            {
                context = HarnessContext.FromEnvironment();
            }
            catch (Exception e)
            {
                Log.LogError("[GLModTest] Invalid harness environment: " + e.Message);
                return;
            }

            if (context == null && BetaPlay.IsRequested)
            {
                // Only this patch: the others are for the scenarios (one even skips a call of the game).
                BetaPlay.Init(Log);
                Harmony.PatchAll(typeof(BetaPlayMainMenuPatch));
                Log.LogInfo("[GLModTest] Playing on the beta server");
                return;
            }

            if (context == null)
            {
                Log.LogInfo($"[GLModTest] Inactive ({HarnessConstants.ENV_INSTANCE} not set)");
                return;
            }

            HarnessLog.Init(Log, context);
            HarnessLog.Info($"Loaded: instance {context.Instance}, run {context.RunId}, output {context.OutputDir}");

            if (context.ModName != null)
            {
                // Same call a third-party mod makes in its Load() (docs/dev.MD)
                GLModPlugin.ConfigService.SetModName(context.ModName);
                HarnessLog.Info($"Mod name set to '{GLModPlugin.ConfigService.ModName}'");
            }

            if (context.Scenario == HarnessConstants.SCENARIO_FRIENDS)
            {
                UseTestAccount(context);
            }

            // With Among Us Ranked (-KeepOtherPlugins), which imposes its prod region at each main menu
            if (context.Region == HarnessConstants.BETA_REGION)
            {
                BetaPlay.Init(Log);
            }

            ScenarioRunner.Init(context);
            Harmony.PatchAll();
        }

        /// <summary>
        /// Friends scenario: the instances share one Steam account, and one cannot be friend with oneself.
        /// Each one logs in with the token of a preprod test account: the login reuses a valid token
        /// without asking Steam for a ticket.
        /// </summary>
        private static void UseTestAccount(HarnessContext context)
        {
            string path = Environment.GetEnvironmentVariable(HarnessConstants.ENV_ACCOUNTS);
            HarnessAccount account = System.Text.Json.JsonSerializer.Deserialize<List<HarnessAccount>>(File.ReadAllText(path))[context.Instance];
            typeof(AuthenticationService)
                .GetMethod("SetGameTokenLogin", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(GLModPlugin.AuthService, new object[]
                {
                    new GLLoginResponse { token = account.token, username = account.username, expiresAt = account.expiresAt },
                });
            HarnessLog.Info("Logged in as " + account.username);
        }
    }
}

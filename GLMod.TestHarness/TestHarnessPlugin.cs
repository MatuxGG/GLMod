using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using System;
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

            ScenarioRunner.Init(context);
            Harmony.PatchAll();
        }
    }
}

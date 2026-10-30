using BepInEx.Logging;
using GLMod.Class;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using System;
using System.Collections;
using UnityEngine;

namespace GLMod.TestHarness
{
    /// <summary>
    /// Plays by hand on the beta server (play-beta.ps1, GLMOD_PLAY_BETA set). Among Us Ranked imposes its
    /// prod region each time the main menu scene loads, so the beta one is set again right after,
    /// in MainMenuManager.Start. Nothing is saved: the regionInfo.json of the player does not change.
    /// </summary>
    public static class BetaPlay
    {
        private static ManualLogSource _log;

        public static bool IsRequested => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(HarnessConstants.ENV_PLAY_BETA));

        public static bool IsActive { get; private set; }

        public static void Init(ManualLogSource log)
        {
            _log = log;
            IsActive = true;
        }

        /// <summary>
        /// The region of the beta server, which is not in the regions of the game.
        /// </summary>
        public static IRegionInfo BuildRegion()
        {
            var servers = new Il2CppReferenceArray<ServerInfo>(1);
            servers[0] = new ServerInfo(HarnessConstants.BETA_REGION, HarnessConstants.BETA_HOST, HarnessConstants.BETA_PORT, false);
            return new StaticHttpRegionInfo(HarnessConstants.BETA_REGION, StringNames.NoTranslation, HarnessConstants.BETA_HOST, servers).Cast<IRegionInfo>();
        }

        public static void SelectRegion()
        {
            ServerManager servers = DestroyableSingleton<ServerManager>.Instance;
            IRegionInfo beta = BuildRegion();
            servers.AvailableRegions = new Il2CppReferenceArray<IRegionInfo>(new[] { beta });
            servers.CurrentRegion = beta;
            servers.ReselectServer();
            _log?.LogInfo($"[GLModTest] {HarnessConstants.BETA_REGION} selected");
        }

        /// <summary>
        /// Selects the region again if something changed it just after the main menu loaded.
        /// </summary>
        public static IEnumerator CheckRegionLater()
        {
            yield return new WaitForSeconds(HarnessConstants.PLAY_BETA_CHECK_DELAY);
            string current = DestroyableSingleton<ServerManager>.Instance.CurrentRegion?.Name;
            _log?.LogInfo($"[GLModTest] Region of the main menu: {current}");
            if (current != HarnessConstants.BETA_REGION)
            {
                SelectRegion();
            }
        }
    }

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
    public static class BetaPlayMainMenuPatch
    {
        public static void Postfix()
        {
            if (!BetaPlay.IsActive) return;

            try
            {
                BetaPlay.SelectRegion();
                CoroutineRunner.Run(BetaPlay.CheckRegionLater());
            }
            catch (Exception e)
            {
                HarnessLog.Error("[BetaPlay] " + e);
            }
        }
    }
}

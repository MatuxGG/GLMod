using HarmonyLib;
using System;
using GLModPlugin = global::GLMod.GLMod;

namespace GLMod.TestHarness
{
    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
    public static class HarnessMainMenuStartPatch
    {
        public static void Postfix()
        {
            try
            {
                ScenarioRunner.OnMainMenu();
            }
            catch (Exception e)
            {
                HarnessLog.Error("[MainMenuStart] " + e);
            }
        }
    }

    /// <summary>
    /// CosmeticsCache.ClearUnusedCosmetics intermittently throws a NullReferenceException inside
    /// AmongUsClient.CoStartGame when several instances run on one machine; the start coroutine then
    /// aborts, the client never reports ready and the host kicks it. The call only frees memory,
    /// so the harness skips it.
    /// </summary>
    [HarmonyPatch(typeof(CosmeticsCache), nameof(CosmeticsCache.ClearUnusedCosmetics))]
    public static class HarnessClearUnusedCosmeticsPatch
    {
        public static bool Prefix()
        {
            return false;
        }
    }

    /// <summary>
    /// Runs after GLMod's own OnGameEnd postfix so the snapshot contains the winners GLMod just set.
    /// </summary>
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
    [HarmonyAfter(GLModPlugin.Id)]
    public static class HarnessOnGameEndPatch
    {
        [HarmonyPriority(Priority.Last)]
        public static void Postfix([HarmonyArgument(0)] EndGameResult endGameResult)
        {
            try
            {
                ScenarioRunner.OnGameEnd(endGameResult.GameOverReason);
            }
            catch (Exception e)
            {
                HarnessLog.Error("[OnGameEnd] " + e);
            }
        }
    }
}

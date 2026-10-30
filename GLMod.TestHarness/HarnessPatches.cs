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
    /// Movement game: the walker's direction replaces the joystick input of the local player
    /// (vanilla FixedUpdate sets the velocity from the joystick just before).
    /// </summary>
    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.FixedUpdate))]
    public static class HarnessPlayerPhysicsFixedUpdatePatch
    {
        private static bool _errorLogged;

        public static void Postfix(PlayerPhysics __instance)
        {
            UnityEngine.Vector2? direction = HarnessWalker.Direction;
            if (!direction.HasValue) return;

            try
            {
                PlayerControl player = __instance.myPlayer;
                if (player != null && player.AmOwner && player.CanMove)
                {
                    __instance.SetNormalizedVelocity(direction.Value);
                }
            }
            catch (Exception e)
            {
                if (_errorLogged) return;
                _errorLogged = true;
                HarnessLog.Error("[PlayerPhysics.FixedUpdate] " + e);
            }
        }
    }

    /// <summary>
    /// Plugin scenario: the answers of GLPlugin arrive as chat lines.
    /// </summary>
    [HarmonyPatch(typeof(ChatController), nameof(ChatController.AddChat))]
    public static class HarnessAddChatPatch
    {
        public static void Postfix([HarmonyArgument(0)] PlayerControl sourcePlayer, [HarmonyArgument(1)] string chatText)
        {
            try
            {
                ScenarioRunner.OnChat(sourcePlayer?.Data?.PlayerName, chatText);
            }
            catch (Exception e)
            {
                HarnessLog.Error("[AddChat] " + e);
            }
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

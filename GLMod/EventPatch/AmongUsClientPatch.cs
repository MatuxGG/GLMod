using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;


namespace GLMod
{
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
    public class OnGameEndPatch
    {
        public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ref EndGameResult endGameResult)
        {
            try
            {
                if (GLMod.existService("TasksMax") || GLMod.debug)
                {
                    foreach (PlayerControl player in PlayerControl.AllPlayerControls)
                    {
                        GLMod.GameStateManager.CurrentGame.players.FindAll(p => p.playerName == player.Data.PlayerName).ForEach(p => p.setTasksMax(player.Data.Tasks.Count));
                    }
                }

                GameOverReason gameOverReason = endGameResult.GameOverReason;

                if (GLMod.existService("EndGame") || GLMod.debug)
                {
                    switch (gameOverReason)
                    {
                        case GameOverReason.CrewmatesByTask:
                        case GameOverReason.CrewmatesByVote:
                        case GameOverReason.ImpostorDisconnect:
                            GLMod.SetWinnerTeams(new List<string>() { "Crewmate" });
                            break;
                        case GameOverReason.ImpostorsByKill:
                        case GameOverReason.ImpostorsBySabotage:
                        case GameOverReason.ImpostorsByVote:
                        case GameOverReason.CrewmateDisconnect:
                            GLMod.SetWinnerTeams(new List<string>() { "Impostor" });
                            break;
                    }
                    CoroutineRunner.Run(GLMod.EndGame());
                }
            }
            catch (Exception e) {
                GLMod.log("[VanillaGameEnd] Catch exception " + e.Message);
            }
        }
    }
}

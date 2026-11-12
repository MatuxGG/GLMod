using System;
using GLMod.GLEntities;

namespace GLMod.Class
{
    /// <summary>
    /// Records the number of tasks of each player of the current game (TasksMax service).
    /// Read during the game and again before it is sent: a player who left before the end, or a
    /// mod that sends the game before AmongUsClient.OnGameEnd (Among Us Ranked), still has it.
    /// Only raises the recorded values (GLPlayer.RecordTasksMax). Main thread only.
    /// </summary>
    public static class TasksMaxRecorder
    {
        public static void Record(GLGame game)
        {
            if (game == null) return;
            if (!(GLMod.existService("TasksMax") || GLMod.debug)) return;

            try
            {
                foreach (PlayerControl player in PlayerControl.AllPlayerControls)
                {
                    if (player?.Data?.Tasks == null) continue;
                    game.RecordTasksMax(player.Data.PlayerName, player.Data.Tasks.Count);
                }
            }
            catch (Exception e)
            {
                GLMod.log("[TasksMax] Catch exception " + e.Message);
            }
        }
    }
}

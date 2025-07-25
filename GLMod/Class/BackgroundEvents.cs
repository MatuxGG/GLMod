using Il2CppSystem;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace GLMod
{
    public static class BackgroundEvents
    {
        private static List<string> playersDc;
        private static bool processEnabled = false;

        public static void handleDc(string reason, string playerName)
        {
            if (GLMod.step == 0) return;
            try
            {
                GLMod.log("handleDc: " + reason + " / " + playerName);
                GLMod.addAction(playerName, reason, "");
            }
            catch (System.Exception e)
            {
                GLMod.log("[VanillaHandleDc] Catch exception " + e.Message);
            }
        }

        public static void startBackgroundProcess()
        {
            playersDc = new List<string>() { };
            processEnabled = true;
            _ = handleProcess();
        }

        public static void endBackgroundProcess()
        {
            processEnabled = false;
        }


        private static async Task<bool> handleProcess()
        {
            while (processEnabled)
            {
                await Task.Delay(500);

                // Check if not in meeting
                int turn = int.Parse(GLMod.currentGame.turns);
                if (turn > 1000) continue;

                long timestampSeconds = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                foreach (PlayerControl player in PlayerControl.AllPlayerControls)
                {
                    if (player.Data.IsDead) continue;
                    float x = player.MyPhysics.body.transform.position.x;
                    float y = player.MyPhysics.body.transform.position.y;
                    GLMod.currentGame.addPosition(player.Data.PlayerName, x, y, timestampSeconds.ToString());
                }

                List<string> playersIndexed = new List<string>() { };
                foreach (PlayerControl p in PlayerControl.AllPlayerControls)
                {
                    playersIndexed.Add(p.Data.PlayerName);
                }
                foreach (GLPlayer currentGamePlayer in GLMod.currentGame.players)
                {
                    if (!playersDc.Contains(currentGamePlayer.playerName) && !playersIndexed.Contains(currentGamePlayer.playerName))
                    {
                        handleDc("DC_PROCESS_", currentGamePlayer.playerName);
                        playersDc.Add(currentGamePlayer.playerName);
                    }
                }
            }
            return true;
        }
    }
}

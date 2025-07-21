using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace GLMod
{
    public static class DisconnectEvents
    {
        private static List<string> playersDc;
        private static Timer _timer;

        public static void handleDc(string reason, string playerName)
        {
            if (GLMod.step == 0) return;
            try
            {
                GLMod.log("handleDc: " + reason + " / " + playerName);
                GLMod.addAction(playerName, reason, "");
            }
            catch (Exception e)
            {
                GLMod.log("[VanillaHandleDc] Catch exception " + e.Message);
            }

        }

        public static async Task<bool> startDcProcess()
        {
            playersDc = new List<string>() { };
            _timer = new Timer(5 * 1000);
            _timer.Elapsed += OnTimedEvent;
            _timer.AutoReset = true;
            _timer.Enabled = true;
            _timer.Start();

            // Task every 1 min

            return true;
        }

        public static async Task<bool> endDcProcess()
        {
            _timer.Stop();
            handleDcProcess();
            return true;
        }

        private static void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            handleDcProcess();
        }

        private static void handleDcProcess()
        {
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
    }
}

using Hazel;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Net;
using System.Text;
using System.Timers;

namespace GLMod
{
    public static class BackgroundWorkers
    {
        public static int gameId;

        // Sync Game Id
        public static void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            try
            {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId,
                    (byte)CustomRPC.ShareId, Hazel.SendOption.Reliable, -1);
                writer.Write(GLMod.currentGame.getId());
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                GLMod.step = 4;
            }
            catch (Exception ex)
            {
                GLMod.logError("[SyncGameId] RPC fail");
            }
        }

        // Receive Id
        public static void backgroundWorkerReceiveId_DoWork(object sender, DoWorkEventArgs e)
        {
            var backgroundWorker = sender as BackgroundWorker;

            try
            {
                while (GLMod.step != 3 || GLMod.currentGame == null)
                {

                }

                GLMod.currentGame.setId(gameId);
                GLMod.step = 4;
            } catch (Exception ex)
            {
                GLMod.logError("[Background Worker] Catch exception " + ex.Message);
            }

            backgroundWorker.ReportProgress(100);
        }

        // Add Player
        public static void backgroundWorkerAddPlayer_DoWork(object sender, DoWorkEventArgs e)
        {
            var backgroundWorker = sender as BackgroundWorker;

            PlayerControl me;
            GLPlayer myPlayer;
            try
            {
                me = PlayerControl.LocalPlayer;
                myPlayer = GLMod.currentGame.players.FindAll(p => p.playerName == me.Data.PlayerName)[0];
            }
            catch (Exception ex)
            {
                GLMod.logError("[AddMyPlayer] Catch exception " + ex.Message);
                return;
            }

            if (myPlayer == null)
            {
                GLMod.logError("[AddMyPlayer] My player null");
                return;
            }

            if (myPlayer.role == null)
            {
                GLMod.logError("[AddMyPlayer] My role null");
            }

            if (myPlayer.team == null)
            {
                GLMod.logError("[AddMyPlayer] My team null");
            }

            if (string.IsNullOrEmpty(myPlayer.playerName))
            {
                GLMod.logError("[AddMyPlayer] My name null or empty");
            }

            while (string.IsNullOrEmpty(GLMod.currentGame.id))
            {

            }

            try
            {
                using (var client = Utils.getClient())
                {
                    var values = new NameValueCollection();
                    values["gameId"] = GLMod.currentGame.id;
                    values["login"] = GLMod.getAccountName();
                    values["playerName"] = me.Data.PlayerName;

                    var response = client.UploadValues(GLMod.api + "/game/addMyPlayer", values);

                    var responseString = Encoding.Default.GetString(response);
                }
            }
            catch (Exception ex)
            {
                GLMod.logError("[AddMyPlayer] Add my player fail");
            }

            backgroundWorker.ReportProgress(100);
        }

        public static void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {

        }

        public static void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

        }
    }
}

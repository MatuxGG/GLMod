using HarmonyLib;
using Hazel;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using System.ComponentModel;

namespace GLMod
{

    enum CustomRPC
    {
        ShareId = 240
    }
    public static class GLRPCProcedure
    {
        // Step 4 : Receive Game Id for non host
        public static void shareId(int gameId)
        {
            try {
                BackgroundWorkers.gameId = gameId;

                BackgroundWorker backgroundWorker = new BackgroundWorker();
                backgroundWorker.WorkerReportsProgress = true;
                backgroundWorker.DoWork += new DoWorkEventHandler(BackgroundWorkers.backgroundWorkerReceiveId_DoWork);
                backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BackgroundWorkers.backgroundWorker_RunWorkerCompleted);
                backgroundWorker.ProgressChanged += new ProgressChangedEventHandler(BackgroundWorkers.backgroundWorker_ProgressChanged);
                backgroundWorker.RunWorkerAsync();
            } catch (Exception e)
            {
                GLMod.logError("[SyncGameId] Rpc worker make fail");
            }
            GLMod.UpdateRpcStep();
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
    class HandleRpcPatch
    {
        static void Postfix([HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
        {
            byte packetId = callId;
            switch (packetId)
            {
                case (byte)CustomRPC.ShareId:
                    int gameId = reader.ReadInt32();
                    GLRPCProcedure.shareId(gameId);
                    break;
            }
        }
    }
}


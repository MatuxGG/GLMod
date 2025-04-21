using HarmonyLib;
using Hazel;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Threading;

namespace GLMod
{

    enum CustomRPC
    {
        ShareId = 240
    }
    public static class GLRPCProcedure
    {
        // Step 4 : Receive Game Id for non host
        public static async Task shareId(int gameId)
        {
            try
            {
                await Task.Run(() =>
                {
                    try
                    {
                        while (GLMod.step != 3 || GLMod.currentGame == null)
                        {
                            Thread.Sleep(100);
                        }

                        GLMod.currentGame.setId(gameId);
                        GLMod.step = 4;
                    }
                    catch (Exception ex)
                    {
                        GLMod.log("[Background Worker] Catch exception " + ex.Message);
                    }
                });

            }
            catch (Exception e)
            {
                GLMod.log("[SyncGameId] Rpc worker make fail, error: " + e.Message);
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
                    _= GLRPCProcedure.shareId(gameId);
                    break;
            }
        }
    }
}


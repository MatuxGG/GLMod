using HarmonyLib;
using Hazel;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Threading;
using Iced.Intel;
using Steamworks;

namespace GLMod
{

    enum CustomRPC
    {
        HandleRpc = 240
    }
    public static class GLRPCProcedure
    {
        public static void makeRpcCall(int id, string value = "")
        {
            string sentValue = id.ToString() + "#" + value;
            GLMod.log("Envoi RPC: '" + sentValue + "'");
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId,
                            (byte)CustomRPC.HandleRpc, Hazel.SendOption.Reliable, -1);
            writer.Write(sentValue);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }

        public static async Task handleRpc(string valueKey)
        {
            GLMod.log("=== handleRpc appelé avec: '" + valueKey + "'");
            try
            {
                string[] parts = valueKey.Split('#');
                if (parts.Length < 2)
                {
                    GLMod.log("Format invalide - pas assez de parts");
                    return;
                }

                if (!int.TryParse(parts[0], out int prefix))
                {
                    GLMod.log("Impossible de parser le prefix: '" + parts[0] + "'");
                    return;
                }

                GLMod.log("Prefix: " + prefix);
                string value = parts[1];

                switch (prefix)
                {
                    case 1: // Step 4 : Receive Game Id for non host
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

                                    GLMod.currentGame.setId(int.Parse(value));
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
                        break;
                    case 2: // DisconnectInternal
                        parts = value.Split('%');
                        if (parts.Length < 2) return;
                        string reason = "dc_" + parts[0];
                        string playerName = parts[1];
                        VanillaEvents.handleDc(reason, playerName);
                        break;
                    // ...
                    // Case 99
                    default:
                        GLMod.log("Prefix non géré: " + prefix);
                        break;
                }
            }
            catch (Exception ex)
            {
                GLMod.log("Erreur dans handleRpc: " + ex.Message);
            }

            
            
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
                case (byte)CustomRPC.HandleRpc:
                    string value = reader.ReadString();
                    _= GLRPCProcedure.handleRpc(value);
                    break;
            }
        }
    }
}


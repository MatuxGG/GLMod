using Epic.OnlineServices.Logging;
using HarmonyLib;
using Hazel;
using Iced.Intel;
using Steamworks;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using static Il2CppSystem.Linq.Expressions.Interpreter.CastInstruction.CastInstructionNoT;

namespace GLMod
{

    enum CustomRPC
    {
        HandleRpc = 240
    }
    public static class GLRPCProcedure
    {
        public static void makeRpcCall(int id, List<string> values)
        {
            try
            {
                string logMessage = $"[makeRPC] {id.ToString()}: ";
                logMessage += string.Join(',', values);
                GLMod.log(logMessage);
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId,
                                (byte)CustomRPC.HandleRpc, Hazel.SendOption.Reliable, -1);
                writer.Write(id);
                writer.Write(values.Count);
                foreach (string value in values)
                {
                    writer.Write(value);
                }
                AmongUsClient.Instance.FinishRpcImmediately(writer);
            }
            catch (Exception e)
            {
                GLMod.log("Error in RPC Call: "+ e.Message);
            }
        }

        public static async Task handleRpc(int id, List<string> values)
        {
            try
            {
                string logMessage = $"[HandleRPC] {id.ToString()}: ";
                logMessage += string.Join(',', values);
                GLMod.log(logMessage);
                switch (id)
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

                                    GLMod.currentGame.setId(int.Parse(values[0]));
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
                        string reason = "dc_" + values[0];
                        string playerName = values[1];
                        VanillaEvents.handleDc(reason, playerName);
                        break;
                    // ...
                    // Case 99
                    default:
                        GLMod.log("Prefix non géré: " + id);
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
                    int id = reader.ReadInt32();
                    int valuesLength = reader.ReadInt32();
                    List<string> values = new List<string>() { };

                    for (int i = 0; i < valuesLength; i++) {
                        values.Add(reader.ReadString());
                    }

                    _ = GLRPCProcedure.handleRpc(id, values);
                    break;
            }
        }
    }
}


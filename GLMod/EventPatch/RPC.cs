using Epic.OnlineServices.Logging;
using HarmonyLib;
using Hazel;
using Iced.Intel;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using GLMod.Enums;
using GLMod.Constants;
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
                GLMod.log("Error in RPC Call: " + e.Message);
            }
        }

        public static IEnumerator handleRpc(int id, List<string> values)
        {
            string logMessage = $"[HandleRPC] {id.ToString()}: ";
            logMessage += string.Join(',', values);
            GLMod.log(logMessage);

            switch (id)
            {
                case 1: // Step 4 : Receive Game Id for non host
                        // Attendre que le jeu soit au bon step et que currentGame existe
                    while (GLMod.GameStateManager.Step != GameStep.GameIdSynced || GLMod.GameStateManager.CurrentGame == null)
                    {
                        yield return new WaitForSeconds(GameConstants.RPC_POLLING_INTERVAL);
                    }

                    try
                    {
                        GLMod.GameStateManager.CurrentGame.setId(int.Parse(values[0]));
                        GLMod.GameStateManager.Step = GameStep.PlayersRecorded;
                    }
                    catch (Exception ex)
                    {
                        GLMod.log("[HandleRpc Case 1] Catch exception " + ex.Message);
                    }

                    GLMod.UpdateRpcStep();
                    break;

                case 2: // DisconnectInternal
                    string reason = "DC_INTERNAL_" + (values[0] ?? "unknown");
                    string playerName = values[1] ?? "";
                    BackgroundEvents.handleDc(reason, playerName);
                    break;

                case 3: // HandleDisconnect
                    string reason2 = "DC_HANDLE_" + (values[0] ?? "unknown");
                    string playerName2 = values[1] ?? "";
                    BackgroundEvents.handleDc(reason2, playerName2);
                    break;

                case 4: // OnDisconnect
                    string reason3 = "DC_ON_" + (values[0] ?? "unknown");
                    string playerName3 = values[1] ?? "";
                    BackgroundEvents.handleDc(reason3, playerName3);
                    break;

                // Ajoutez les autres cases ici...

                default:
                    GLMod.log("Prefix non g�r�: " + id);
                    break;
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

                        for (int i = 0; i < valuesLength; i++)
                        {
                            values.Add(reader.ReadString() ?? "");
                        }

                        CoroutineRunner.Run(GLRPCProcedure.handleRpc(id, values));
                        break;
                }
            }
        }
    }
}
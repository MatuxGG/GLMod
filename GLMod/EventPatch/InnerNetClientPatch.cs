using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem;
using InnerNet;
using System;
using System.Collections.Generic;
using System.Text;
using GLMod.Enums;
using GLMod.Class;

namespace GLMod
{
    public class InnerNetClientPatch
    {
        /// <summary>
        /// Identifies the player on the Good Loss game servers: Impostor only knows the name a player
        /// declares, GLPlugin reads the account and its room code from this ticket. No other server gets it.
        /// </summary>
        [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.GetConnectionData))]
        public static class GetConnectionDataPatch
        {
            static void Postfix(ref Il2CppStructArray<byte> __result)
            {
                try
                {
                    if (!GoodLossServers.IsCurrentRegion()) return;

                    string ticket = GLMod.RoomTicketService?.Ticket;
                    if (ticket == null)
                    {
                        GLMod.log("[Room] no room ticket for this connection");
                        return;
                    }

                    var data = new byte[__result.Length];
                    for (int i = 0; i < data.Length; i++)
                    {
                        data[i] = __result[i];
                    }
                    __result = RoomTicketFooter.Append(data, ticket);
                    GLMod.log("[Room] room ticket added to the connection");
                }
                catch (System.Exception e)
                {
                    // Only a bonus: the player must connect anyway.
                    GLMod.log("[Room] could not add the room ticket: " + e.Message);
                }
            }
        }

        [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.DisconnectInternal))]
        public static class DisconnectInternalPatch
        {
            static void Prefix(InnerNetClient __instance, DisconnectReasons reason)
            {
                string playerName = PlayerControl.LocalPlayer?.Data?.PlayerName;
                List<string> values = new List<string> { reason.ToString(), playerName ?? "" };
                GLRPCProcedure.makeRpcCall(2, values);
                BackgroundEvents.handleDc(reason.ToString(), playerName);
                GLMod.GameStateManager.Step = GameStep.Initial;
            }
        }
        [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.HandleDisconnect))]
        public static class HandleDisconnectPatch
        {
            static void Postfix(InnerNetClient __instance, DisconnectReasons reason)
            {
                string playerName = PlayerControl.LocalPlayer?.Data?.PlayerName;
                List<string> values = new List<string> { reason.ToString(), playerName ?? "" };
                GLRPCProcedure.makeRpcCall(3, values);
                BackgroundEvents.handleDc(reason.ToString(), playerName);
                GLMod.GameStateManager.Step = GameStep.Initial;
            }

        }
        [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.OnDisconnect))]
        public static class OnDisconnectPatch
        {
            static void Postfix(InnerNetClient __instance)
            {
                string playerName = PlayerControl.LocalPlayer?.Data?.PlayerName;
                List<string> values = new List<string> { playerName ?? "" };
                GLRPCProcedure.makeRpcCall(3, values);
                BackgroundEvents.handleDc("On Disconnect", playerName);
                GLMod.GameStateManager.Step = GameStep.Initial;
            }

        }

    }
}

using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem;
using InnerNet;
using System;
using System.Collections.Generic;
using System.Text;
using GLMod.Enums;
using GLMod.Class;
using GLMod.Constants;

namespace GLMod
{
    public class InnerNetClientPatch
    {
        public const int DISCONNECT_INTERNAL_RPC_ID = 2;
        public const int HANDLE_DISCONNECT_RPC_ID = 3;
        // RPC 4 (OnDisconnect) payload: [reason, playerName], read by GLRPCProcedure.handleRpc
        public const int ON_DISCONNECT_RPC_ID = 4;
        public const string ON_DISCONNECT_REASON = "On Disconnect";

        public static List<string> BuildOnDisconnectRpcValues(string playerName)
        {
            return new List<string> { ON_DISCONNECT_REASON, playerName ?? "" };
        }

        /// <summary>
        /// Action recorded for a disconnect (DC_INTERNAL_/DC_HANDLE_/DC_ON_ + reason),
        /// shared by the local patches and the RPC receivers so both record the same value
        /// </summary>
        public static string BuildDisconnectAction(int rpcId, string reason)
        {
            string prefix;
            switch (rpcId)
            {
                case DISCONNECT_INTERNAL_RPC_ID:
                    prefix = GameConstants.ACTION_PREFIX_DC_INTERNAL;
                    break;
                case HANDLE_DISCONNECT_RPC_ID:
                    prefix = GameConstants.ACTION_PREFIX_DC_HANDLE;
                    break;
                case ON_DISCONNECT_RPC_ID:
                    prefix = GameConstants.ACTION_PREFIX_DC_ON;
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(rpcId));
            }
            return prefix + (reason ?? "unknown");
        }

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
                GLRPCProcedure.makeRpcCall(DISCONNECT_INTERNAL_RPC_ID, values);
                BackgroundEvents.handleDc(BuildDisconnectAction(DISCONNECT_INTERNAL_RPC_ID, reason.ToString()), playerName);
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
                GLRPCProcedure.makeRpcCall(HANDLE_DISCONNECT_RPC_ID, values);
                BackgroundEvents.handleDc(BuildDisconnectAction(HANDLE_DISCONNECT_RPC_ID, reason.ToString()), playerName);
                GLMod.GameStateManager.Step = GameStep.Initial;
            }

        }
        [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.OnDisconnect))]
        public static class OnDisconnectPatch
        {
            static void Postfix(InnerNetClient __instance)
            {
                string playerName = PlayerControl.LocalPlayer?.Data?.PlayerName;
                List<string> values = BuildOnDisconnectRpcValues(playerName);
                GLRPCProcedure.makeRpcCall(ON_DISCONNECT_RPC_ID, values);
                BackgroundEvents.handleDc(BuildDisconnectAction(ON_DISCONNECT_RPC_ID, ON_DISCONNECT_REASON), playerName);
                GLMod.GameStateManager.Step = GameStep.Initial;
            }

        }

    }
}

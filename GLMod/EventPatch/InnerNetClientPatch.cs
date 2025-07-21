using HarmonyLib;
using Il2CppSystem;
using InnerNet;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLMod
{
    public class InnerNetClientPatch
    {
        [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.DisconnectInternal))]
        public static class DisconnectInternalPatch
        {
            static void Prefix(InnerNetClient __instance, DisconnectReasons reason)
            {
                string playerName = PlayerControl.LocalPlayer?.Data?.PlayerName;
                List<string> values = new List<string> { reason.ToString(), playerName ?? "" };
                GLRPCProcedure.makeRpcCall(2, values);
                DisconnectEvents.handleDc(reason.ToString(), playerName);
                GLMod.step = 0;
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
                DisconnectEvents.handleDc(reason.ToString(), playerName);
                GLMod.step = 0;
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
                DisconnectEvents.handleDc("On Disconnect", playerName);
                GLMod.step = 0;
            }

        }

    }
}

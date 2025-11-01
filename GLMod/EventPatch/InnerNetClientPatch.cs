using HarmonyLib;
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

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
                GLRPCProcedure.makeRpcCall(2, reason.ToString() + "%" + playerName);
                VanillaEvents.handleDc(reason.ToString(), playerName);
                GLMod.step = 0;
            }
        }
        [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.HandleDisconnect))]
        public static class HandleDisconnectPatch
        {
            static void Postfix(InnerNetClient __instance, DisconnectReasons reason)
            {
                GLMod.step = 0;
            }

        }
        [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.OnDisconnect))]
        public static class OnDisconnectPatch
        {
            static void Postfix(InnerNetClient __instance)
            {
                GLMod.step = 0;
            }

        }

    }
}

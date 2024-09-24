using HarmonyLib;
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
            static void Postfix()
            {
                GLMod.step = 0;
            }

        }
        [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.HandleDisconnect))]
        public static class HandleDisconnectPatch
        {
            static void Postfix()
            {
                GLMod.step = 0;
            }

        }
        [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.OnDisconnect))]
        public static class OnDisconnectPatch
        {
            static void Postfix()
            {
                GLMod.step = 0;
            }

        }
    }
}

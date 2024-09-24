using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace GLMod
{
    public static class Utils
    {
        public static string debug = "";

        [HarmonyPatch(typeof(PingTracker), nameof(PingTracker.Update))]
        public static class PingPatch
        {
            public static void Postfix(PingTracker __instance)
            {
                //__instance.text.text += "\nGL Mod"
                //   + "\ndebug:" + debug;
            }
        }
        public static WebClient getClient()
        {
            WebClient client = new WebClient();
            client.Proxy = GlobalProxySelection.GetEmptyWebProxy();
            return client;
        }

    }
}

using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLMod
{
    public class PingTrackerPatch
    {
        public static string debug = "";

        [HarmonyPatch(typeof(PingTracker), nameof(PingTracker.Update))]
        public static class PingPatch
        {
            public static void Postfix(PingTracker __instance)
            {

            }
        }
    }
}

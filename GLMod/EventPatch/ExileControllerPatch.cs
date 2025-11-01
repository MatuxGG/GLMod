using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLMod
{
    public class ExileControllerPatch
    {
        [HarmonyPatch(typeof(ExileController), nameof(ExileController.ReEnableGameplay))]
        public class ReEnableGameplayPatch
        {
            public static void Postfix(ExileController __instance)
            {
                try
                {
                    if (GLMod.existService("Turns") || GLMod.debug)
                    {
                        GLMod.GameStateManager.CurrentGame.addTurn();
                    }
                } catch (Exception e)
                {
                    GLMod.log("[VanillaAddTurn] Catch exception " + e.Message);
                }
                
            }
        }
    }
}

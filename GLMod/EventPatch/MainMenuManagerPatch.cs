using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLMod
{
    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
    public class StartPatch
    {
        public static async void Postfix()
        {
            if (SteamManager.Initialized)
            {
                if (GLMod.token == null)
                {
                    try
                    {
                        await GLMod.login();
                        if (!GLMod.withUnityExplorer)
                        {
                            GLMod.getRank();
                            GLMod.reloadItems();
                            GLMod.reloadDlcOwnerships();
                        }
                    } catch (Exception e)
                    {
                        GLMod.disableAllServices();
                        GLMod.logWithoutInfo(e.Source.ToString() + " / " + e.InnerException.ToString() + " / " + e.Message.ToString());
                    }
                }
            }
        }
    }
}

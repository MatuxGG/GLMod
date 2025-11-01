using BepInEx.Unity.IL2CPP.Utils;
using GLMod.Class;
using GLMod.Services;
using HarmonyLib;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLMod
{
    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
    public class MainMenuManagerStartPatch
    {

        public static void Postfix(MainMenuManager __instance)
        {
            try
            {
                if (!SteamAPI.Init())
                {
                    return;
                }

                if (SteamUser.BLoggedOn())
                {
                    string SteamName = SteamFriends.GetPersonaName();
                    string SteamID = SteamUser.GetSteamID().m_SteamID.ToString();

                    try
                    {
                        CoroutineRunner.Run(GLMod.AuthService.Login(success =>
                        {
                            if (!GLMod.withUnityExplorer)
                            {
                                CoroutineRunner.Run(GLMod.ItemService.ReloadItems());
                                CoroutineRunner.Run(GLMod.ItemService.ReloadDlcOwnerships());
                            }
                        }));
                    }
                    catch (Exception e)
                    {
                        GLMod.disableAllServices();
                        GLMod.log(e.Source.ToString() + " / " + e.InnerException.ToString() + " / " + e.Message.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                GLMod.log($"[MainMenuManager] Error during initialization: {ex.Message}");
            }
        }
    }
}

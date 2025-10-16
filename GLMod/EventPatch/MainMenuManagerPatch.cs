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
        public static async void Postfix()
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
                        await GLMod.login();
                        if (!GLMod.withUnityExplorer)
                        {
                            _ = GLMod.reloadItems();
                            _ = GLMod.reloadDlcOwnerships();
                        }
                    }
                    catch (Exception e)
                    {
                        GLMod.disableAllServices();
                        GLMod.log(e.Source.ToString() + " / " + e.InnerException.ToString() + " / " + e.Message.ToString());
                    }

                }
                else
                {
                }
            }
            catch (Exception)
            {
                
            }

            if (SteamManager.Initialized)
            {
                if (GLMod.token == null)
                {
                    try
                    {
                        await GLMod.login();
                        if (!GLMod.withUnityExplorer)
                        {
                            _ = GLMod.getRank();
                            _ = GLMod.reloadItems();
                            _ = GLMod.reloadDlcOwnerships();
                        }
                    } catch (Exception e)
                    {
                        GLMod.disableAllServices();
                        GLMod.log(e.Source.ToString() + " / " + e.InnerException.ToString() + " / " + e.Message.ToString());
                    }
                }
            }
        }
    }
}

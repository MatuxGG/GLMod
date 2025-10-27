using BepInEx.Unity.IL2CPP.Utils;
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
                        CoroutineRunner.Run(GLMod.login(success =>
                        {
                            if (!GLMod.withUnityExplorer)
                            {
                                CoroutineRunner.Run(GLMod.reloadItems());
                                CoroutineRunner.Run(GLMod.reloadDlcOwnerships());
                            }
                        }));
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

            //if (SteamManager.Initialized)
            //{
            //    if (GLMod.token == null)
            //    {
            //        try
            //        {
            //            CoroutineRunner.Run(GLMod.login(success =>
            //            {
            //                if (!GLMod.withUnityExplorer)
            //                {
            //                    CoroutineRunner.Run(GLMod.reloadItems());
            //                    CoroutineRunner.Run(GLMod.reloadDlcOwnerships());
            //                }
            //            }));
            //        } catch (Exception e)
            //        {
            //            GLMod.disableAllServices();
            //            GLMod.log(e.Source.ToString() + " / " + e.InnerException.ToString() + " / " + e.Message.ToString());
            //        }
            //    }
            //}
        }
    }
}

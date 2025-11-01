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
                            GLMod.log("=== Login Service Test (after Steam initialization) ===");

                            if (success)
                            {
                                GLMod.log("[✓] Login Service: Authentication successful");
                                GLMod.log($"    - IsLoggedIn: {GLMod.AuthService.IsLoggedIn}");
                                GLMod.log($"    - Has Token: {!string.IsNullOrEmpty(GLMod.AuthService.Token)}");
                                GLMod.log($"    - Account Name: {GLMod.AuthService.GetAccountName()}");
                            }
                            else
                            {
                                GLMod.log("[✗] Login Service: Authentication failed");
                                GLMod.log($"    - IsLoggedIn: {GLMod.AuthService.IsLoggedIn}");
                                GLMod.log($"    - IsBanned: {GLMod.AuthService.IsBanned}");
                                if (GLMod.AuthService.IsBanned)
                                {
                                    GLMod.log($"    - Ban Reason: {GLMod.AuthService.BanReason}");
                                }
                            }

                            if (GLMod.AuthService.IsLoggedIn)
                            {
                                GLMod.log("=== Testing Services Requiring Authentication ===");

                                // Test ItemService
                                GLMod.log("[Testing] ItemService...");
                                if (GLMod.ItemService != null)
                                {
                                    GLMod.log($"    - Service available: Yes");
                                    GLMod.log($"    - Current items count: {GLMod.ItemService.Items.Count}");

                                    if (!GLMod.withUnityExplorer)
                                    {
                                        GLMod.log("    - Loading items...");
                                        CoroutineRunner.Run(GLMod.ItemService.ReloadItems());
                                        GLMod.log("    - Loading DLC ownerships...");
                                        CoroutineRunner.Run(GLMod.ItemService.ReloadDlcOwnerships());
                                    }
                                }
                                else
                                {
                                    GLMod.log($"    - Service available: No");
                                }

                                // Test RankService
                                GLMod.log("[Testing] RankService...");
                                if (GLMod.RankService != null)
                                {
                                    GLMod.log($"    - Service available: Yes");
                                    GLMod.log($"    - Account name for rank query: {GLMod.AuthService.GetAccountName()}");
                                    GLMod.log($"    - Mod name for rank query: {GLMod.ConfigService.ModName}");
                                    GLMod.log("    - Fetching rank...");

                                    CoroutineRunner.Run(GLMod.RankService.GetRank(null, rank =>
                                    {
                                        if (rank != null && string.IsNullOrEmpty(rank.error))
                                        {
                                            GLMod.log($"    [✓] Rank fetched successfully");
                                        }
                                        else
                                        {
                                            GLMod.log($"    [✗] Rank fetch failed: {rank?.error ?? "Unknown error"}");
                                        }
                                    }));
                                }
                                else
                                {
                                    GLMod.log($"    - Service available: No");
                                }

                                GLMod.log("=== Authentication-dependent services test completed ===");
                            }
                            else
                            {
                                GLMod.log("=== Skipping authentication-dependent services (not logged in) ===");
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

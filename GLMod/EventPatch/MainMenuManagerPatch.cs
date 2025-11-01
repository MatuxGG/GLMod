using BepInEx.Unity.IL2CPP.Utils;
using GLMod.Class;
using GLMod.GLEntities;
using GLMod.Services;
using HarmonyLib;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace GLMod
{
    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
    public class MainMenuManagerStartPatch
    {
        private static IEnumerator TestAuthenticationServices()
        {
            GLMod.log("=== Testing Services Requiring Authentication ===");
            GLMod.log("");

            // Test ItemService
            GLMod.log("[ItemService]");
            if (GLMod.ItemService != null)
            {
                GLMod.log("  Status: Available");
                GLMod.log("  Loading items...");

                int itemsBefore = GLMod.ItemService.Items.Count;
                yield return GLMod.ItemService.ReloadItems();
                int itemsAfter = GLMod.ItemService.Items.Count;

                GLMod.log($"  [✓] Items loaded: {itemsAfter} items");

                GLMod.log("  Loading DLC ownerships...");
                int dlcBefore = GLMod.ItemService.SteamOwnerships.Count;
                yield return GLMod.ItemService.ReloadDlcOwnerships();
                int dlcAfter = GLMod.ItemService.SteamOwnerships.Count;

                GLMod.log($"  [✓] DLC ownerships loaded: {dlcAfter} DLCs");
            }
            else
            {
                GLMod.log("  [✗] Service not available");
            }

            GLMod.log("");

            // Test RankService
            GLMod.log("[RankService]");
            if (GLMod.RankService != null)
            {
                GLMod.log("  Status: Available");
                GLMod.log($"  Account: {GLMod.AuthService.GetAccountName()}");
                GLMod.log($"  Mod: {GLMod.ConfigService.ModName}");
                GLMod.log("  Fetching rank...");

                GLRank rankResult = null;
                yield return GLMod.RankService.GetRank(null, rank => { rankResult = rank; });

                if (rankResult != null && string.IsNullOrEmpty(rankResult.error))
                {
                    GLMod.log($"  [✓] Rank: #{rankResult.id} | Percent: {rankResult.percent}% | Name: {rankResult.name}");
                }
                else
                {
                    GLMod.log($"  [✗] Failed to fetch rank: {rankResult?.error ?? "Unknown error"}");
                }
            }
            else
            {
                GLMod.log("  [✗] Service not available");
            }

            GLMod.log("");
            GLMod.log("=== Authentication Services Test Completed ===");
        }

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
                                if (!GLMod.withUnityExplorer)
                                {
                                    CoroutineRunner.Run(TestAuthenticationServices());
                                }
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

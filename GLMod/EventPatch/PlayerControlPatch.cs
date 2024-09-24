using HarmonyLib;
using InnerNet;
using System;
using System.Collections.Generic;
using static UnityEngine.GraphicsBuffer;

namespace GLMod
{
    // Exiled
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Exiled))]
    public class ExiledPatch
    {
        public static void Postfix(PlayerControl __instance)
        {
            try
            {
                if (GLMod.existService("Exiled") || GLMod.debug)
                {
                    GLMod.currentGame.addAction("Lobby", __instance.Data.PlayerName, "exiled");
                }
            } catch (Exception e)
            {
                GLMod.logError("[VanillaExile] Catch exception " + e.Message);
            }
            
        }
    }

    // Kills / Killed / Killed First
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
    public class MurderPatch
    {
        public static void Postfix(PlayerControl __instance, PlayerControl target)
        {
            try
            {
                if (GLMod.existService("Kills") || GLMod.debug)
                {
                    GLMod.currentGame.addAction(__instance.Data.PlayerName, target.Data.PlayerName != null ? target.Data.PlayerName : "", "killed");
                }
            }
            catch (Exception e)
            {
                GLMod.logError("[VanillaMurder] Catch exception " + e.Message);
            }

            
        }
    }

    // Tasks
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CompleteTask))]
    public class TaskPatch
    {
        public static void Postfix(PlayerControl __instance)
        {

           
                if (GLMod.existService("Tasks") || GLMod.debug)
                {
                    if (__instance.Data.IsDead)
                    {
                        try
                        {
                            List<GLPlayer> players = GLMod.currentGame.players.FindAll(p => p.playerName == __instance.Data.PlayerName);
                            if (players.Count > 0)
                            {
                                players.ForEach(p => p.addTasksDead());
                            }
                        }
                        catch (Exception e)
                        {
                            GLMod.logError("[VanillaTaskDead] Catch exception " + e.Message);
                        }
                    }
                    else
                    {
                        try
                        {
                        List<GLPlayer> players = GLMod.currentGame.players.FindAll(p => p.playerName == __instance.Data.PlayerName);
                            if (players.Count > 0)
                            {
                                players.ForEach(p => p.addTasks());
                            }
                        }
                        catch (Exception e)
                        {
                            GLMod.logError("[VanillaTaskAlive] Catch exception2 " + e.Message);
                        }
                    }
                }
            

            
        }
    }

    // Emergencies / Body reported
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.StartMeeting))]
    public class StartMeetingPatch
    {
        public static void Postfix(PlayerControl __instance, NetworkedPlayerInfo target)
        {
            try
            {
                if (target == null)
                {
                    if (GLMod.existService("Emergencies") || GLMod.debug)
                    {
                        GLMod.currentGame.addAction(__instance.Data.PlayerName, "", "called an emergency");
                    }
                }
                else
                {
                    if (GLMod.existService("BodyReported") || GLMod.debug)
                    {
                        GLMod.currentGame.addAction(__instance.Data.PlayerName, target.PlayerName, "reported");
                    }
                }
                if (GLMod.existService("Turns") || GLMod.debug)
                {
                    GLMod.currentGame.addTurn();
                }
            }
            catch (Exception e)
            {
                GLMod.logError("[VanillaStartMeeting] Catch exception " + e.Message);
            }
        }
    }

    // Update
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
    public class FixedUpdatePatch
    {
        public static bool gameStarted = false;

        public static void Postfix(PlayerControl __instance)
        {
            // Start

            try
            {
                if (AmongUsClient.Instance.GameState == InnerNetClient.GameStates.Started)
                {
                    if (gameStarted) return;



                    int nbImp = GameOptionsManager.Instance.CurrentGameOptions.NumImpostors;
                    int realImp = 0;
                    foreach (PlayerControl p in PlayerControl.AllPlayerControls)
                    {
                        if (p.Data.Role.IsImpostor)
                        {
                            realImp++;
                        }
                    }

                    if (nbImp != realImp) return;

                    // Start game
                    GLMod.gameMap = GLMod.getMapName();

                    VanillaEvents.startGameVanilla();
                    gameStarted = true;
                }
                else
                {
                    gameStarted = false;
                }
            }
            catch (Exception e)
            {
                GLMod.logError("[VanillaFixedUpdate] Catch exception " + e.Message);
            }


        }
    }
}

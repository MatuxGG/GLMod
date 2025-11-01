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
                    GLMod.GameStateManager.CurrentGame.addAction("Lobby", __instance.Data.PlayerName, "exiled");
                }
            } catch (Exception e)
            {
                GLMod.log("[VanillaExile] Catch exception " + e.Message);
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
                long timestampSeconds = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                // Set positions before kill
                float x = __instance.MyPhysics.body.transform.position.x;
                float y = __instance.MyPhysics.body.transform.position.y;
                GLMod.GameStateManager.CurrentGame.addPosition(__instance.Data.PlayerName, x, y, timestampSeconds.ToString());
                x = target.MyPhysics.body.transform.position.x;
                y = target.MyPhysics.body.transform.position.y;
                GLMod.GameStateManager.CurrentGame.addPosition(target.Data.PlayerName, x, y, timestampSeconds.ToString());

                if (GLMod.existService("Kills") || GLMod.debug)
                {
                    GLMod.GameStateManager.CurrentGame.addAction(__instance.Data.PlayerName, target.Data.PlayerName != null ? target.Data.PlayerName : "", "killed");
                }
            }
            catch (Exception e)
            {
                GLMod.log("[VanillaMurder] Catch exception " + e.Message);
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
                            List<GLPlayer> players = GLMod.GameStateManager.CurrentGame.players.FindAll(p => p.playerName == __instance.Data.PlayerName);
                            if (players.Count > 0)
                            {
                                players.ForEach(p => p.addTasksDead());
                            }
                        }
                        catch (Exception e)
                        {
                            GLMod.log("[VanillaTaskDead] Catch exception " + e.Message);
                        }
                    }
                    else
                    {
                        try
                        {
                        List<GLPlayer> players = GLMod.GameStateManager.CurrentGame.players.FindAll(p => p.playerName == __instance.Data.PlayerName);
                            if (players.Count > 0)
                            {
                                players.ForEach(p => p.addTasks());
                            }
                        }
                        catch (Exception e)
                        {
                            GLMod.log("[VanillaTaskAlive] Catch exception2 " + e.Message);
                        }
                    }
                }
        }
    }

    // Shapeshift
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Shapeshift))]
    public class ShapeshiftPatch
    {
        public static void Postfix(PlayerControl __instance, PlayerControl targetPlayer)
        {
            if (__instance.Data.PlayerName == targetPlayer.Data.PlayerName)
            {
                try
                {
                    if (GLMod.existService("Roles") || GLMod.debug)
                    {
                        GLMod.GameStateManager.CurrentGame.addAction(__instance.Data.PlayerName, "", "unshifted");
                    }
                }
                catch (Exception e)
                {
                    GLMod.log("[VanillaUnShapeshift] Catch exception " + e.Message);
                }
            } else
            {
                try
                {
                    if (GLMod.existService("Roles") || GLMod.debug)
                    {
                        GLMod.GameStateManager.CurrentGame.addAction(__instance.Data.PlayerName, targetPlayer.Data.PlayerName != null ? targetPlayer.Data.PlayerName : "", "shapeshifted into");
                    }
                }
                catch (Exception e)
                {
                    GLMod.log("[VanillaShapeshift] Catch exception " + e.Message);
                }
            }
            
        }
    }

    // Guardian angel protect
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ProtectPlayer))]
    public class ProtectPatch
    {
        public static void Postfix(PlayerControl __instance, PlayerControl target)
        {
            try
            {
                if (GLMod.existService("Roles") || GLMod.debug)
                {
                    GLMod.GameStateManager.CurrentGame.addAction(__instance.Data.PlayerName, target.Data.PlayerName != null ? target.Data.PlayerName : "", "protected");
                }
            }
            catch (Exception e)
            {
                GLMod.log("[VanillaProtect] Catch exception " + e.Message);
            }
        }
    }

    // Tracker track
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.StartPlayerTracking))]
    public class TrackPatch
    {
        public static void Postfix(PlayerControl __instance, PlayerControl playerToTrack)
        {
            try
            {
                if (GLMod.existService("Roles") || GLMod.debug)
                {
                    GLMod.GameStateManager.CurrentGame.addAction(__instance.Data.PlayerName, playerToTrack.Data.PlayerName, "started tracking");
                }
            }
            catch (Exception e)
            {
                GLMod.log("[VanillaTrack] Catch exception " + e.Message);
            }
        }
    }

    // Tracker untrack
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CancelPlayerTracking))]
    public class UntrackPatch
    {
        public static void Postfix(PlayerControl __instance)
        {
            try
            {
                if (GLMod.existService("Roles") || GLMod.debug)
                {
                    GLMod.GameStateManager.CurrentGame.addAction(__instance.Data.PlayerName, "", "stopped tracking");
                }
            }
            catch (Exception e)
            {
                GLMod.log("[VanillaUnTrack Catch exception " + e.Message);
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
                long timestampSeconds = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                if (target == null)
                {
                    if (GLMod.existService("Emergencies") || GLMod.debug)
                    {
                        // Set positions before kill
                        float x = __instance.MyPhysics.body.transform.position.x;
                        float y = __instance.MyPhysics.body.transform.position.y;
                        GLMod.GameStateManager.CurrentGame.addPosition(__instance.Data.PlayerName, x, y, timestampSeconds.ToString());
                        GLMod.GameStateManager.CurrentGame.addAction(__instance.Data.PlayerName, "", "called an emergency");
                    }
                }
                else
                {
                    if (GLMod.existService("BodyReported") || GLMod.debug)
                    {
                        // Set positions before kill
                        float x = __instance.MyPhysics.body.transform.position.x;
                        float y = __instance.MyPhysics.body.transform.position.y;
                        GLMod.GameStateManager.CurrentGame.addPosition(__instance.Data.PlayerName, x, y, timestampSeconds.ToString());
                        GLMod.GameStateManager.CurrentGame.addAction(__instance.Data.PlayerName, target.PlayerName, "reported");
                    }
                }
                if (GLMod.existService("Turns") || GLMod.debug)
                {
                    GLMod.GameStateManager.CurrentGame.addTurn();
                }
            }
            catch (Exception e)
            {
                GLMod.log("[VanillaStartMeeting] Catch exception " + e.Message);
            }
        }
    }

    // Tasks
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CompleteTask))]
    public class CompleteTaskPatch
    {
        public static void Postfix(PlayerControl __instance)
        {
            try
            {
                if (GLMod.existService("Tasks") || GLMod.debug)
                {
                    GLMod.GameStateManager.CurrentGame.addAction(__instance.Data.PlayerName, "", "completedTask");
                }
            }
            catch (Exception e)
            {
                GLMod.log("[VanillaCompleteTask] Catch exception " + e.Message);
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
                if (AmongUsClient.Instance.GameState != InnerNet.InnerNetClient.GameStates.Started)
                {
                    gameStarted = false;
                    return;
                }

                if (gameStarted) return;

                int nbImp = GameOptionsManager.Instance.CurrentGameOptions.NumImpostors;
                if (PlayerControl.AllPlayerControls.Count < 6) nbImp = 1;
                int realImp = 0;
                foreach (PlayerControl p in PlayerControl.AllPlayerControls)
                {
                    if (p?.Data?.Role?.IsImpostor == true)
                    {
                        realImp++;
                    }
                }

                if (nbImp != realImp) return;

                // Start game
                GLMod.GameStateManager.GameMap = GLMod.getMapName();

                CoroutineRunner.Run(VanillaEvents.startGameVanilla());
                gameStarted = true;

            }
            catch (Exception e)
            {
                GLMod.log("[VanillaFixedUpdate] Catch exception " + e.Message + " / stack = " + e.StackTrace);
            }


        }
    }
}

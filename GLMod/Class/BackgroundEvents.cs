using Il2CppSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;
using GLMod.Enums;
using GLMod.Constants;
using GLMod.GLEntities;

namespace GLMod.Class
{
    public static class BackgroundEvents
    {
        private static List<string> playersDc;
        private static bool processEnabled = false;
        private static SabotageType? cachedSabotage = null;
        // Incremented on each start: loops of a previous game stop even if they missed processEnabled = false
        private static int generation = 0;

        public static void handleDc(string reason, string playerName)
        {
            if (GLMod.GameStateManager.Step == GameStep.Initial) return;
            try
            {
                GLMod.log("handleDc: " + reason + " / " + playerName);
                GLMod.addAction(playerName, "", reason);
            }
            catch (System.Exception e)
            {
                GLMod.log("[VanillaHandleDc] Catch exception " + e.Message);
            }
        }

        public static void startBackgroundProcess()
        {
            playersDc = new List<string>() { };
            processEnabled = true;
            cachedSabotage = null;
            PositionRecorder.Reset();

            // Note: Unity's StopCoroutine does not always behave correctly under IL2CPP.
            // Loops stop through processEnabled and the generation instead.
            generation++;
            CoroutineRunner.Run(handleProcess(generation));
            CoroutineRunner.Run(handlePositions(generation));
        }

        public static void endBackgroundProcess()
        {
            processEnabled = false;

            // The last samples of each player are still pending in the simplifier
            PositionRecorder.FlushAll(GLMod.GameStateManager?.CurrentGame);
        }

        private static bool IsRunning(int runGeneration)
        {
            return processEnabled && runGeneration == generation;
        }

        private static IEnumerator handleProcess(int runGeneration)
        {
            while (IsRunning(runGeneration))
            {
                yield return new WaitForSeconds(GameConstants.BACKGROUND_POLLING_INTERVAL);
                if (!IsRunning(runGeneration)) break;

                // Check if not in meeting
                int turn = int.Parse(GLMod.GameStateManager.CurrentGame.turns);
                if (turn > 1000) continue;

                DetectPlayerDisconnections();
                TrackSabotageState();
            }
        }

        /// <summary>
        /// Samples the positions of all alive players; the simplifier only keeps the useful ones
        /// </summary>
        private static IEnumerator handlePositions(int runGeneration)
        {
            while (IsRunning(runGeneration))
            {
                yield return new WaitForSeconds(GameConstants.POSITION_SAMPLING_INTERVAL);
                if (!IsRunning(runGeneration)) break;

                try
                {
                    GLGame game = GLMod.GameStateManager.CurrentGame;
                    if (game == null) continue;

                    if (int.Parse(game.turns) > 1000)
                    {
                        // Meeting: nobody moves, send what was pending before it
                        PositionRecorder.FlushAll(game);
                    }
                    else
                    {
                        PositionRecorder.SampleAlivePlayers(game);
                    }
                }
                catch (System.Exception e)
                {
                    GLMod.log("[TrackPositions] Catch exception " + e.Message);
                }
            }
        }

        /// <summary>
        /// Detects players who have disconnected during the game
        /// </summary>
        private static void DetectPlayerDisconnections()
        {
            // Get list of currently active players
            List<string> activePlayers = new List<string>();
            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                activePlayers.Add(player.Data.PlayerName);
            }

            // Check for disconnected players
            foreach (GLPlayer gamePlayer in GLMod.GameStateManager.CurrentGame.players)
            {
                if (!playersDc.Contains(gamePlayer.playerName) && !activePlayers.Contains(gamePlayer.playerName))
                {
                    handleDc(GameConstants.ACTION_PREFIX_DC_PROCESS, gamePlayer.playerName);
                    playersDc.Add(gamePlayer.playerName);
                }
            }
        }

        /// <summary>
        /// Tracks sabotage state changes (start/end)
        /// </summary>
        private static void TrackSabotageState()
        {
            if (PlayerControl.LocalPlayer?.myTasks == null)
                return;

            SabotageType? currentSabotage = DetectCurrentSabotage();

            // Handle sabotage state transitions
            if (!cachedSabotage.HasValue && currentSabotage.HasValue)
            {
                // New sabotage started
                GLMod.addAction("", "", GameConstants.ACTION_PREFIX_SAB_START + currentSabotage.Value.ToActionString());
                cachedSabotage = currentSabotage;
            }
            else if (cachedSabotage.HasValue && !currentSabotage.HasValue)
            {
                // Sabotage ended
                GLMod.addAction("", "", GameConstants.ACTION_PREFIX_SAB_END + cachedSabotage.Value.ToActionString());
                cachedSabotage = null;
            }
            else if (cachedSabotage.HasValue && currentSabotage.HasValue && cachedSabotage != currentSabotage)
            {
                // Sabotage changed
                GLMod.addAction("", "", GameConstants.ACTION_PREFIX_SAB_END + cachedSabotage.Value.ToActionString());
                GLMod.addAction("", "", GameConstants.ACTION_PREFIX_SAB_START + currentSabotage.Value.ToActionString());
                cachedSabotage = currentSabotage;
            }
        }

        /// <summary>
        /// Detects the current active sabotage type
        /// </summary>
        /// <returns>Sabotage type or null if no sabotage</returns>
        private static SabotageType? DetectCurrentSabotage()
        {
            foreach (PlayerTask playerTask in PlayerControl.LocalPlayer.myTasks)
            {
                SabotageType? sabotage = SabotageForTask(playerTask.TaskType);
                if (sabotage.HasValue)
                    return sabotage;
            }

            return null;
        }

        /// <summary>
        /// Maps a task created by a sabotage to its sabotage type
        /// </summary>
        /// <returns>Sabotage type or null if the task is not a sabotage task</returns>
        public static SabotageType? SabotageForTask(TaskTypes taskType)
        {
            switch (taskType)
            {
                case TaskTypes.ResetSeismic:
                case TaskTypes.ResetReactor:
                    return SabotageType.Reactor;
                case TaskTypes.FixComms:
                    return SabotageType.Coms;
                case TaskTypes.FixLights:
                    return SabotageType.Lights;
                case TaskTypes.RestoreOxy:
                    return SabotageType.O2;
            }

            return null;
        }
    }
}
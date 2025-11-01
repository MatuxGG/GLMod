using Il2CppSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;
using GLMod.Enums;
using GLMod.Constants;

namespace GLMod
{
    public static class BackgroundEvents
    {
        private static List<string> playersDc;
        private static bool processEnabled = false;
        private static SabotageType? cachedSabotage = null;
        private static Coroutine backgroundCoroutine = null;

        public static void handleDc(string reason, string playerName)
        {
            if (GLMod.step == GameStep.Initial) return;
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

            // Arrêter la coroutine précédente si elle existe
            if (backgroundCoroutine != null)
            {
                CoroutineRunner.Run(StopBackgroundCoroutine());
            }

            // Démarrer la nouvelle coroutine
            backgroundCoroutine = CoroutineRunner.Run(handleProcess());
        }

        public static void endBackgroundProcess()
        {
            processEnabled = false;

            // Arrêter la coroutine
            if (backgroundCoroutine != null)
            {
                CoroutineRunner.Run(StopBackgroundCoroutine());
            }
        }

        private static IEnumerator StopBackgroundCoroutine()
        {
            if (backgroundCoroutine != null)
            {
                // Note: Unity's StopCoroutine ne fonctionne pas toujours bien en IL2CPP
                // On utilise plutôt le flag processEnabled pour arrêter proprement
                backgroundCoroutine = null;
            }
            yield break;
        }

        private static IEnumerator handleProcess()
        {
            while (processEnabled)
            {
                yield return new WaitForSeconds(GameConstants.BACKGROUND_POLLING_INTERVAL);

                // Check if not in meeting
                int turn = int.Parse(GLMod.currentGame.turns);
                if (turn > 1000) continue;

                TrackPlayerPositions();
                DetectPlayerDisconnections();
                TrackSabotageState();
            }

            backgroundCoroutine = null;
        }

        /// <summary>
        /// Tracks and records positions of all alive players
        /// </summary>
        private static void TrackPlayerPositions()
        {
            long timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                if (player.Data.IsDead) continue;

                float x = player.MyPhysics.body.transform.position.x;
                float y = player.MyPhysics.body.transform.position.y;
                GLMod.currentGame.addPosition(player.Data.PlayerName, x, y, timestamp.ToString());
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
            foreach (GLPlayer gamePlayer in GLMod.currentGame.players)
            {
                if (!playersDc.Contains(gamePlayer.playerName) && !activePlayers.Contains(gamePlayer.playerName))
                {
                    handleDc("DC_PROCESS_", gamePlayer.playerName);
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
                GLMod.addAction("", "", "SAB_START_" + currentSabotage.Value.ToActionString());
                cachedSabotage = currentSabotage;
            }
            else if (cachedSabotage.HasValue && !currentSabotage.HasValue)
            {
                // Sabotage ended
                GLMod.addAction("", "", "SAB_END_" + cachedSabotage.Value.ToActionString());
                cachedSabotage = null;
            }
            else if (cachedSabotage.HasValue && currentSabotage.HasValue && cachedSabotage != currentSabotage)
            {
                // Sabotage changed
                GLMod.addAction("", "", "SAB_END_" + cachedSabotage.Value.ToActionString());
                GLMod.addAction("", "", "SAB_START_" + currentSabotage.Value.ToActionString());
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
                switch (playerTask.TaskType)
                {
                    case TaskTypes.ResetSeismic:
                    case TaskTypes.ResetReactor:
                        return SabotageType.Reactor;
                    case TaskTypes.FixComms:
                        return SabotageType.Coms;
                    case TaskTypes.FixLights:
                        return SabotageType.Lights;
                    case TaskTypes.CleanO2Filter:
                        return SabotageType.O2;
                }
            }

            return null;
        }
    }
}
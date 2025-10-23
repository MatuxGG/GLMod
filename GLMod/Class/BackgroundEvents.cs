using Il2CppSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;

namespace GLMod
{
    public static class BackgroundEvents
    {
        private static List<string> playersDc;
        private static bool processEnabled = false;
        private static string cachedSabotage = null;
        private static Coroutine backgroundCoroutine = null;

        public static void handleDc(string reason, string playerName)
        {
            if (GLMod.step == 0) return;
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
                // Attendre 500ms (équivalent de Task.Delay(500))
                yield return new WaitForSeconds(0.5f);

                // Check if not in meeting
                int turn = int.Parse(GLMod.currentGame.turns);
                if (turn > 1000) continue;

                long timestampSeconds = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                foreach (PlayerControl player in PlayerControl.AllPlayerControls)
                {
                    if (player.Data.IsDead) continue;
                    float x = player.MyPhysics.body.transform.position.x;
                    float y = player.MyPhysics.body.transform.position.y;
                    GLMod.currentGame.addPosition(player.Data.PlayerName, x, y, timestampSeconds.ToString());
                }

                List<string> playersIndexed = new List<string>() { };
                foreach (PlayerControl p in PlayerControl.AllPlayerControls)
                {
                    playersIndexed.Add(p.Data.PlayerName);
                }

                foreach (GLPlayer currentGamePlayer in GLMod.currentGame.players)
                {
                    if (!playersDc.Contains(currentGamePlayer.playerName) && !playersIndexed.Contains(currentGamePlayer.playerName))
                    {
                        handleDc("DC_PROCESS_", currentGamePlayer.playerName);
                        playersDc.Add(currentGamePlayer.playerName);
                    }
                }

                if (PlayerControl.LocalPlayer?.myTasks != null)
                {
                    string currentSabotage = null;
                    foreach (PlayerTask playerTask in PlayerControl.LocalPlayer.myTasks)
                    {
                        switch (playerTask.TaskType)
                        {
                            case TaskTypes.ResetSeismic:
                            case TaskTypes.ResetReactor:
                                currentSabotage = "Reactor";
                                break;
                            case TaskTypes.FixComms:
                                currentSabotage = "Coms";
                                break;
                            case TaskTypes.FixLights:
                                currentSabotage = "Lights";
                                break;
                            case TaskTypes.CleanO2Filter:
                                currentSabotage = "O2";
                                break;
                        }

                        if (currentSabotage != null) break;
                    }

                    if (cachedSabotage == null)
                    {
                        if (currentSabotage != cachedSabotage) // Nouveau sabotage
                        {
                            GLMod.addAction("", "", "SAB_START_" + currentSabotage);
                            cachedSabotage = currentSabotage;
                        }
                    }
                    else
                    {
                        if (currentSabotage == null) // Fin d'un sabotage
                        {
                            GLMod.addAction("", "", "SAB_END_" + cachedSabotage);
                            cachedSabotage = null;
                        }
                        else if (cachedSabotage != currentSabotage) // Remplacement de sabotage
                        {
                            GLMod.addAction("", "", "SAB_END_" + cachedSabotage);
                            cachedSabotage = null;
                            GLMod.addAction("", "", "SAB_START_" + currentSabotage);
                            cachedSabotage = currentSabotage;
                        }
                    }
                }
            }

            backgroundCoroutine = null;
        }
    }
}
using GLMod.Class;
using GLMod.GLEntities;
using HarmonyLib;
using System;

namespace GLMod
{
    /// <summary>
    /// Records the finished game in the local history when the end screen is set up
    /// (winners are known, the GLMod game is still current).
    /// </summary>
    [HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.SetEverythingUp))]
    public class EndGameManagerSetEverythingUpPatch
    {
        private static GLGame _lastRecordedGame;

        public static void Postfix()
        {
            try
            {
                GLGame game = GLMod.GameStateManager?.CurrentGame;
                if (game == null || ReferenceEquals(game, _lastRecordedGame) || GLMod.GameHistoryService == null)
                {
                    return;
                }
                _lastRecordedGame = game;

                bool won = false;
                if (EndGameResult.CachedWinners != null)
                {
                    foreach (CachedPlayerData winner in EndGameResult.CachedWinners)
                    {
                        if (winner != null && winner.IsYou)
                        {
                            won = true;
                            break;
                        }
                    }
                }

                string myName = EndGameResult.CachedLocalPlayer?.PlayerName ?? PlayerControl.LocalPlayer?.Data?.PlayerName;
                GLPlayer me = game.players?.Find(p => p.playerName == myName);

                GLMod.GameHistoryService.Record(new GLGameHistoryEntry
                {
                    gameId = game.id ?? "",
                    recapUrl = ApiEndpointResolver.GameRecapUrl(GLMod.api, game.id),
                    endedAtMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    map = game.map ?? "",
                    modName = game.modName ?? "",
                    role = me?.role ?? "",
                    team = me?.team ?? "",
                    won = won,
                });
            }
            catch (Exception e)
            {
                GLMod.log("[EndGameManager] Could not record the game in the history: " + e.Message);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace GLMod
{
    public static class VanillaEvents
    {
        public static void startGameVanilla()
        {
            if (GLMod.existService("StartGame") || GLMod.debug)
            {
                try
                {
                    GLMod.StartGame("******", GLMod.gameMap, false);
                    foreach (PlayerControl p in PlayerControl.AllPlayerControls)
                    {
                        string role = p?.Data?.Role?.TeamType == RoleTeamTypes.Crewmate ? "Crewmate" : "Impostor";
                        GLMod.AddPlayer(p?.Data?.PlayerName, "Vanilla" + p?.Data?.Role?.Role.ToString(), role);
                    }
                    GLMod.SendGame();
                    GLMod.AddMyPlayer();
                } catch (Exception e)
                {
                    GLMod.logError("[VanillaStartGame] Catch exception " + e.Message);
                }
            }

        }
    }
}

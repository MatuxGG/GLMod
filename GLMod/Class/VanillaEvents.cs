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
                        string team = p?.Data?.Role?.TeamType == RoleTeamTypes.Crewmate ? "Crewmate" : "Impostor";
                        string role = "Vanilla" + (p?.Data?.Role?.Role.ToString() ?? string.Empty);
                        GLMod.AddPlayer(p?.Data?.PlayerName, role, team);
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

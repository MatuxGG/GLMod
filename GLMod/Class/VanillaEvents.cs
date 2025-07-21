using Hazel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GLMod
{
    public static class VanillaEvents
    {
        public static async Task startGameVanilla()
        {
            if (GLMod.existService("StartGame") || GLMod.debug)
            {
                if (GLMod.step != 0) return;
                try
                {
                    GLMod.log("Starting game...");
                    GLMod.StartGame("******", GLMod.gameMap, false);
                    foreach (PlayerControl p in PlayerControl.AllPlayerControls)
                    {
                        string team = p?.Data?.Role?.TeamType == RoleTeamTypes.Crewmate ? "Crewmate" : "Impostor";
                        string role = "Vanilla" + (p?.Data?.Role?.Role.ToString() ?? string.Empty);
                        GLMod.AddPlayer(p?.Data?.PlayerName, role, team);
                    }
                    await GLMod.SendGame();
                    await GLMod.AddMyPlayer();
                    GLMod.log("Game started.");
                } catch (Exception e)
                {
                    GLMod.log("[VanillaStartGame] Catch exception " + e.Message);
                }
            }
            await DisconnectEvents.startDcProcess();
        }
    }
}

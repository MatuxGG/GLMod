using Hazel;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using GLMod.Enums;

namespace GLMod
{
    public static class VanillaEvents
    {
        public static System.Collections.IEnumerator startGameVanilla()
        {
            if (GLMod.existService("StartGame") || GLMod.debug)
            {
                if (GLMod.step != GameStep.Initial)
                {
                    yield break;
                }

                try
                {
                    GLMod.log("Starting game...");
                    GLMod.StartGame("******", GLMod.gameMap, false);

                    foreach (PlayerControl p in PlayerControl.AllPlayerControls)
                    {
                        string team = p?.Data?.Role?.TeamType == RoleTeamTypes.Crewmate ? "Crewmate" : "Impostor";
                        string role = "Vanilla" + (p?.Data?.Role?.Role.ToString() ?? string.Empty);
                        string color = p ? p.Data.DefaultOutfit.ColorId.ToString() : "";
                        GLMod.AddPlayer(p?.Data?.PlayerName, role, team, color);
                    }

                    CoroutineRunner.Run(GLMod.SendGame(result =>
                    {
                        CoroutineRunner.Run(GLMod.AddMyPlayer(result =>
                        {
                            GLMod.log("Game started.");
                        }));
                    }));
                }
                catch (Exception e)
                {
                    GLMod.log("[VanillaStartGame] Catch exception " + e.Message);
                }
            }

            BackgroundEvents.startBackgroundProcess();

            yield break;
        }
    }
}

using GLMod.Constants;
using InnerNet;
using System;

namespace GLMod.Class
{
    /// <summary>
    /// Where the local player is, as declared to Good Loss. Must be called from the Unity main thread.
    /// </summary>
    public sealed class GamePresence
    {
        public string State { get; private set; } = GameConstants.PRESENCE_STATE_MENU;
        public string Region { get; private set; }
        public string Server { get; private set; }
        public string GameCode { get; private set; }
        public int? ClientId { get; private set; }
        public string PlayerName { get; private set; }

        public static bool IsInGame()
        {
            try
            {
                return AmongUsClient.Instance != null && AmongUsClient.Instance.GameState == InnerNetClient.GameStates.Started;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static GamePresence Read()
        {
            var presence = new GamePresence();
            try
            {
                if (DestroyableSingleton<ServerManager>.InstanceExists)
                {
                    IRegionInfo region = DestroyableSingleton<ServerManager>.Instance.CurrentRegion;
                    presence.Region = region?.Name;
                    presence.Server = region?.PingServer;
                }

                string code = PlayModeHelper.CurrentOnlineGameCode();
                if (code == null)
                {
                    return presence;
                }

                presence.State = IsInGame() ? GameConstants.PRESENCE_STATE_GAME : GameConstants.PRESENCE_STATE_LOBBY;
                presence.ClientId = AmongUsClient.Instance.ClientId;
                presence.PlayerName = PlayerControl.LocalPlayer?.Data?.PlayerName;
                // The code of a game never leaves for a server that is not ours
                presence.GameCode = GoodLossServers.IsPresenceServer(presence.Server) ? code : null;
            }
            catch (Exception)
            {
                // The game objects may not exist yet: declared as being in the menu
            }
            return presence;
        }
    }
}

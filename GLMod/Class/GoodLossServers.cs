using GLMod.Constants;
using InnerNet;
using System;
using System.Linq;

namespace GLMod.Class
{
    /// <summary>
    /// The game servers run by Good Loss: the only ones that receive the room ticket.
    /// </summary>
    public static class GoodLossServers
    {
        /// <summary>
        /// True for the ping server of a Good Loss region. Pure.
        /// </summary>
        public static bool IsGoodLossServer(string pingServer)
        {
            return pingServer != null
                && GameConstants.GOOD_LOSS_GAME_SERVERS.Contains(pingServer.Trim(), StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// True when a connection goes to a Good Loss game server: an online game on a Good Loss
        /// region. A LAN or free play game keeps the selected region but connects elsewhere. Pure.
        /// </summary>
        public static bool ReceivesRoomTicket(bool onlineGame, string pingServer)
        {
            return onlineGame && IsGoodLossServer(pingServer);
        }

        /// <summary>
        /// True for a server whose game codes may reach the site (friends: the room list and Join). Pure.
        /// </summary>
        public static bool IsPresenceServer(string pingServer)
        {
            return pingServer != null
                && GameConstants.GOOD_LOSS_PRESENCE_SERVERS.Contains(pingServer.Trim(), StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// True when the site can answer the accounts of the room: an online game on a presence server. Pure.
        /// </summary>
        public static bool HasRoomList(string gameCode, string pingServer)
        {
            return gameCode != null && IsPresenceServer(pingServer);
        }

        /// <summary>
        /// True when the local player is in a game that has a room list. Must be called from the
        /// Unity main thread.
        /// </summary>
        public static bool IsInRoomWithList()
        {
            try
            {
                return DestroyableSingleton<ServerManager>.InstanceExists
                    && HasRoomList(PlayModeHelper.CurrentOnlineGameCode(), DestroyableSingleton<ServerManager>.Instance.CurrentRegion?.PingServer);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// True when the region selected in the game is a Good Loss one. Must be called from the
        /// Unity main thread.
        /// </summary>
        public static bool IsCurrentRegion()
        {
            try
            {
                return DestroyableSingleton<ServerManager>.InstanceExists
                    && IsGoodLossServer(DestroyableSingleton<ServerManager>.Instance.CurrentRegion?.PingServer);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// True when the connection being opened may carry the room ticket. Must be called from the
        /// Unity main thread.
        /// </summary>
        public static bool IsCurrentConnectionEligible()
        {
            try
            {
                return AmongUsClient.Instance != null
                    && DestroyableSingleton<ServerManager>.InstanceExists
                    && ReceivesRoomTicket(
                        AmongUsClient.Instance.NetworkMode == NetworkModes.OnlineGame,
                        DestroyableSingleton<ServerManager>.Instance.CurrentRegion?.PingServer);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}

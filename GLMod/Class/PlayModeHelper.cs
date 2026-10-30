using InnerNet;
using System;

namespace GLMod.Class
{
    /// <summary>
    /// Kind of game being played. Free play games are never recorded nor sent to Good Loss.
    /// </summary>
    public static class PlayModeHelper
    {
        /// <summary>
        /// True in free play (practice mode, NetworkModes.FreePlay). Must be called from the Unity main thread.
        /// </summary>
        public static bool IsFreePlay()
        {
            try
            {
                return AmongUsClient.Instance != null && AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Code of the online game the player is in, null outside of one. Must be called from the Unity main thread.
        /// </summary>
        public static string CurrentOnlineGameCode()
        {
            try
            {
                AmongUsClient client = AmongUsClient.Instance;
                if (client == null || client.NetworkMode != NetworkModes.OnlineGame) return null;
                if (client.GameState != InnerNetClient.GameStates.Joined && client.GameState != InnerNetClient.GameStates.Started) return null;
                return GameCode.IntToGameName(client.GameId);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}

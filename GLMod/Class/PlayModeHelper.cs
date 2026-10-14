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
    }
}

using BepInEx.Logging;

namespace GLMod.Class
{
    /// <summary>
    /// Centralized log formatter shared by every GLMod service.
    /// Formats messages as:
    ///   [GLMod][ServiceName] PlayerName: message
    /// or, when no local player is available:
    ///   [GLMod][ServiceName] message
    /// </summary>
    public static class ServiceLogger
    {
        public static void Log(ManualLogSource logger, string serviceName, string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            string playerName = PlayerControl.LocalPlayer?.Data?.PlayerName;
            string prefix = playerName != null
                ? $"[GLMod][{serviceName}] {playerName}: "
                : $"[GLMod][{serviceName}] ";
            logger.LogInfo(prefix + message);
        }
    }
}

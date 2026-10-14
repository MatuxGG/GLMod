using Steamworks;
using System;
using System.Collections.Generic;

namespace GLMod.Class
{
    /// <summary>
    /// Names of the Among Us DLC, read from the local Steam client.
    /// </summary>
    public static class SteamDlcCatalog
    {
        private const int NameBufferSize = 256;

        private static Dictionary<int, string> _names;

        /// <summary>
        /// DLC name, or "Steam app &lt;id&gt;" when Steam does not know it (e.g. not an Among Us DLC).
        /// Must be called from the Unity main thread.
        /// </summary>
        public static string GetName(int appId)
        {
            if (_names == null || _names.Count == 0)
            {
                _names = LoadNames();
            }

            return _names.TryGetValue(appId, out string name) && !string.IsNullOrWhiteSpace(name)
                ? name
                : "Steam app " + appId;
        }

        private static Dictionary<int, string> LoadNames()
        {
            var names = new Dictionary<int, string>();
            try
            {
                int count = SteamApps.GetDLCCount();
                for (int i = 0; i < count; i++)
                {
                    if (SteamApps.BGetDLCDataByIndex(i, out AppId_t appId, out bool _, out string name, NameBufferSize))
                    {
                        names[(int)appId.m_AppId] = name;
                    }
                }
            }
            catch (Exception ex)
            {
                GLMod.log("[SteamDlcCatalog] Could not read the DLC list from Steam: " + ex.Message);
            }
            return names;
        }
    }
}

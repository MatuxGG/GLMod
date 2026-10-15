using System;

namespace GLMod.Class
{
    /// <summary>
    /// URL of a /player/* GET request. Pure.
    /// </summary>
    public static class PlayerApiQuery
    {
        /// <summary>
        /// "{api}{endpoint}?username=...", plus "&amp;theme=..." when a theme is given (values escaped)
        /// </summary>
        public static string Build(string apiEndpoint, string endpoint, string username, string theme)
        {
            string url = apiEndpoint + endpoint + "?username=" + Uri.EscapeDataString(username ?? "");
            if (!string.IsNullOrWhiteSpace(theme))
            {
                url += "&theme=" + Uri.EscapeDataString(theme.Trim());
            }
            return url;
        }
    }
}

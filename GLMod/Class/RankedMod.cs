using GLMod.Constants;
using System;

namespace GLMod.Class
{
    /// <summary>
    /// Recognizes the Among Us Ranked mod from the detected mod name. Pure.
    /// </summary>
    public static class RankedMod
    {
        /// <summary>
        /// True for "Among Us Ranked" and its variants (e.g. "Among Us Ranked Roles"), case-insensitive
        /// </summary>
        public static bool Matches(string modName)
        {
            return modName != null && modName.Trim().StartsWith(GameConstants.RANKED_MOD_NAME, StringComparison.OrdinalIgnoreCase);
        }
    }
}

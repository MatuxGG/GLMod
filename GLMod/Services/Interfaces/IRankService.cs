using GLMod.GLEntities;
using System.Collections;

namespace GLMod.Services.Interfaces
{
    /// <summary>
    /// Interface for managing player ranks
    /// </summary>
    public interface IRankService
    {
        /// <summary>
        /// Gets the player rank for a specific mod
        /// </summary>
        /// <param name="modName">Mod name (null uses current mod)</param>
        /// <param name="onComplete">Callback with rank result</param>
        /// <returns>Coroutine</returns>
        IEnumerator GetRank(string modName, System.Action<GLRank> onComplete);
    }
}

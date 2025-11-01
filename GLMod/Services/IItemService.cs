using GLMod.GLEntities;
using System.Collections;
using System.Collections.Generic;

namespace GLMod.Services
{
    /// <summary>
    /// Interface for managing items and DLC ownership
    /// </summary>
    public interface IItemService
    {
        /// <summary>
        /// Gets the list of unlocked items
        /// </summary>
        List<GLItem> Items { get; }

        /// <summary>
        /// Gets the list of owned Steam DLC app IDs
        /// </summary>
        List<int> SteamOwnerships { get; }

        /// <summary>
        /// Reloads the player's unlocked items from the server
        /// </summary>
        /// <returns>Coroutine</returns>
        IEnumerator ReloadItems();

        /// <summary>
        /// Checks if a specific item is unlocked
        /// </summary>
        /// <param name="id">Item ID to check</param>
        /// <returns>True if unlocked, false otherwise</returns>
        bool IsUnlocked(string id);

        /// <summary>
        /// Reloads the player's DLC ownerships from the server
        /// </summary>
        /// <returns>Coroutine</returns>
        IEnumerator ReloadDlcOwnerships();

        /// <summary>
        /// Checks if the player owns a specific DLC
        /// </summary>
        /// <param name="appId">Steam app ID to check</param>
        /// <returns>True if owned, false otherwise</returns>
        bool HasDlc(int appId);
    }
}

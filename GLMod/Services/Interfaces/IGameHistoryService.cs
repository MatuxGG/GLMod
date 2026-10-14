using GLMod.GLEntities;
using System;
using System.Collections.Generic;

namespace GLMod.Services.Interfaces
{
    /// <summary>
    /// Interface for the local history of the games played with GLMod
    /// </summary>
    public interface IGameHistoryService
    {
        /// <summary>
        /// Gets the recorded games, most recent first
        /// </summary>
        IReadOnlyList<GLGameHistoryEntry> Entries { get; }

        /// <summary>
        /// Gets the most recent game, or null when the history is empty
        /// </summary>
        GLGameHistoryEntry Latest { get; }

        /// <summary>
        /// Raised on the main thread after a game has been recorded
        /// </summary>
        event Action<GLGameHistoryEntry> EntryRecorded;

        /// <summary>
        /// Loads the history file (an unreadable file gives an empty history)
        /// </summary>
        void Load();

        /// <summary>
        /// Adds a game at the top of the history and saves the file
        /// </summary>
        /// <param name="entry">Game to record</param>
        void Record(GLGameHistoryEntry entry);
    }
}

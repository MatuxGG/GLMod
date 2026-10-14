using BepInEx.Logging;
using GLMod.Class;
using GLMod.GLEntities;
using GLMod.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;

namespace GLMod.Services.Implementations
{
    /// <summary>
    /// Keeps the local history of the games played with GLMod in a JSON file
    /// </summary>
    public class GameHistoryService : IGameHistoryService
    {
        private readonly ManualLogSource _logger;
        private readonly string _filePath;
        private readonly int _maxEntries;
        private List<GLGameHistoryEntry> _entries;

        public IReadOnlyList<GLGameHistoryEntry> Entries => _entries;
        public GLGameHistoryEntry Latest => _entries.Count > 0 ? _entries[0] : null;

        public event Action<GLGameHistoryEntry> EntryRecorded;

        public GameHistoryService(ManualLogSource logger, string filePath, int maxEntries)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            _maxEntries = maxEntries;
            _entries = new List<GLGameHistoryEntry>();
        }

        private void Log(string message) => ServiceLogger.Log(_logger, nameof(GameHistoryService), message);

        public void Load()
        {
            try
            {
                if (!File.Exists(_filePath))
                {
                    _entries = new List<GLGameHistoryEntry>();
                    return;
                }

                if (!GameHistoryList.TryParse(File.ReadAllText(_filePath), out List<GLGameHistoryEntry> entries))
                {
                    Log("[Load] Unreadable history file, starting with an empty history");
                }
                _entries = GameHistoryList.Add(entries, null, _maxEntries);
            }
            catch (Exception ex)
            {
                Log("[Load] Catch exception " + ex.Message);
                _entries = new List<GLGameHistoryEntry>();
            }
        }

        public void Record(GLGameHistoryEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            _entries = GameHistoryList.Add(_entries, entry, _maxEntries);
            Save();

            try
            {
                EntryRecorded?.Invoke(entry);
            }
            catch (Exception ex)
            {
                Log("[Record] EntryRecorded handler failed: " + ex.Message);
            }
        }

        private void Save()
        {
            try
            {
                // Write then move, so a crash never leaves a truncated history file. The temporary
                // name is unique: several game instances can share the same config folder.
                string tempPath = _filePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
                try
                {
                    File.WriteAllText(tempPath, GameHistoryList.Serialize(_entries));
                    File.Move(tempPath, _filePath, true);
                }
                finally
                {
                    if (File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Log("[Save] Catch exception " + ex.Message);
            }
        }
    }
}

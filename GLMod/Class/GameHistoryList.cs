using GLMod.GLEntities;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace GLMod.Class
{
    /// <summary>
    /// Pure operations on the local game history (most recent first). No IO, no logging.
    /// </summary>
    public static class GameHistoryList
    {
        /// <summary>
        /// Returns a new list with the entry first, without an older entry of the same game id,
        /// truncated to maxEntries.
        /// </summary>
        public static List<GLGameHistoryEntry> Add(IEnumerable<GLGameHistoryEntry> entries, GLGameHistoryEntry entry, int maxEntries)
        {
            var result = new List<GLGameHistoryEntry>();
            if (entry != null)
            {
                result.Add(entry);
            }

            foreach (GLGameHistoryEntry existing in entries ?? Enumerable.Empty<GLGameHistoryEntry>())
            {
                if (existing == null)
                {
                    continue;
                }

                bool sameGame = entry != null && !string.IsNullOrEmpty(entry.gameId) && existing.gameId == entry.gameId;
                if (!sameGame)
                {
                    result.Add(existing);
                }
            }

            if (maxEntries >= 0 && result.Count > maxEntries)
            {
                result.RemoveRange(maxEntries, result.Count - maxEntries);
            }

            return result;
        }

        public static string Serialize(IEnumerable<GLGameHistoryEntry> entries)
        {
            return JsonSerializer.Serialize((entries ?? Enumerable.Empty<GLGameHistoryEntry>()).ToList());
        }

        /// <summary>
        /// Parses a history file. Returns false (and an empty list) when the content is not a valid history.
        /// </summary>
        public static bool TryParse(string json, out List<GLGameHistoryEntry> entries)
        {
            entries = new List<GLGameHistoryEntry>();
            if (string.IsNullOrWhiteSpace(json))
            {
                return true;
            }

            try
            {
                List<GLGameHistoryEntry> parsed = JsonSerializer.Deserialize<List<GLGameHistoryEntry>>(json);
                if (parsed != null)
                {
                    entries = parsed.Where(e => e != null).ToList();
                }
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}

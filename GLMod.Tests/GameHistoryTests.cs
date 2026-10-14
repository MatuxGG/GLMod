using System.Collections.Generic;
using System.Linq;
using GLMod.Class;
using GLMod.GLEntities;
using Xunit;

namespace GLMod.Tests
{
    public class GameHistoryTests
    {
        private static GLGameHistoryEntry Entry(string gameId, long endedAtMs = 1) =>
            new GLGameHistoryEntry { gameId = gameId, endedAtMs = endedAtMs, map = "Polus" };

        [Fact]
        public void Add_PutsNewestFirst()
        {
            var history = GameHistoryList.Add(new List<GLGameHistoryEntry>(), Entry("1"), 10);
            history = GameHistoryList.Add(history, Entry("2"), 10);

            Assert.Equal(new[] { "2", "1" }, history.Select(e => e.gameId));
        }

        [Fact]
        public void Add_ReplacesSameGameId()
        {
            var history = GameHistoryList.Add(new[] { Entry("2", 20), Entry("1", 10) }, Entry("1", 30), 10);

            Assert.Equal(new[] { "1", "2" }, history.Select(e => e.gameId));
            Assert.Equal(30, history[0].endedAtMs);
        }

        [Fact]
        public void Add_KeepsGamesWithoutId()
        {
            var history = GameHistoryList.Add(new[] { Entry("") }, Entry(""), 10);

            Assert.Equal(2, history.Count);
        }

        [Fact]
        public void Add_TruncatesToMaxEntries()
        {
            var history = new List<GLGameHistoryEntry>();
            for (int i = 0; i < 5; i++)
            {
                history = GameHistoryList.Add(history, Entry(i.ToString()), 3);
            }

            Assert.Equal(new[] { "4", "3", "2" }, history.Select(e => e.gameId));
        }

        [Fact]
        public void Add_NullEntry_OnlyCleansAndTruncates()
        {
            var history = GameHistoryList.Add(new[] { Entry("1"), null, Entry("2"), Entry("3") }, null, 2);

            Assert.Equal(new[] { "1", "2" }, history.Select(e => e.gameId));
        }

        [Fact]
        public void SerializeThenParse_RoundTrips()
        {
            var entry = new GLGameHistoryEntry
            {
                gameId = "42",
                recapUrl = "https://goodloss.fr/game/42",
                endedAtMs = 1_700_000_000_000,
                map = "Polus",
                modName = "Among Us Ranked",
                role = "VanillaImpostor",
                team = "Impostor",
                won = true,
            };

            Assert.True(GameHistoryList.TryParse(GameHistoryList.Serialize(new[] { entry }), out var parsed));

            GLGameHistoryEntry result = Assert.Single(parsed);
            Assert.Equal(entry.gameId, result.gameId);
            Assert.Equal(entry.recapUrl, result.recapUrl);
            Assert.Equal(entry.endedAtMs, result.endedAtMs);
            Assert.Equal(entry.map, result.map);
            Assert.Equal(entry.modName, result.modName);
            Assert.Equal(entry.role, result.role);
            Assert.Equal(entry.team, result.team);
            Assert.True(result.won);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        [InlineData("null")]
        public void TryParse_EmptyContent_GivesEmptyHistory(string json)
        {
            Assert.True(GameHistoryList.TryParse(json, out var parsed));
            Assert.Empty(parsed);
        }

        [Theory]
        [InlineData("{not json")]
        [InlineData("{\"gameId\":\"1\"}")]
        public void TryParse_InvalidContent_FailsWithEmptyHistory(string json)
        {
            Assert.False(GameHistoryList.TryParse(json, out var parsed));
            Assert.Empty(parsed);
        }
    }
}

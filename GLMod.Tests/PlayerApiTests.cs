using System.Globalization;
using GLMod.Class;
using GLMod.GLEntities;
using Xunit;

namespace GLMod.Tests
{
    public class PlayerApiResponseTests
    {
        // Responses captured from https://preprod.goodloss.fr/api (sessions trimmed to 2 entries)
        private const string SessionsJson =
            "{\"username\":\"Matux\",\"sessions\":[" +
            "{\"id\":19714,\"url\":\"https:\\/\\/preprod.goodloss.fr\\/player\\/Matux\\/session\\/19714\",\"start\":1791967898,\"end\":1791968073,\"games\":8,\"live\":false,\"current\":false}," +
            "{\"id\":19710,\"url\":\"https:\\/\\/preprod.goodloss.fr\\/player\\/Matux\\/session\\/19710\",\"start\":1791919338,\"end\":1791921201,\"games\":16,\"live\":true,\"current\":true}]}";

        private const string LastGameJson =
            "{\"username\":\"Matux\",\"game\":{\"id\":19715,\"url\":\"https:\\/\\/preprod.goodloss.fr\\/game\\/19715\",\"mod\":\"Among Us Ranked\",\"season\":3,\"ranked\":true,\"map\":\"Polus\",\"start\":1791968019,\"end\":1791968073,\"duration\":54,\"turns\":1,\"meetings\":1,\"players\":4,\"winners\":[\"imp\"]}," +
            "\"player\":{\"name\":\"Shedconic\",\"role\":\"VanillaCrewmate\",\"team\":\"Crewmate\",\"color\":\"0\",\"win\":false,\"rp\":null,\"kills\":0,\"doubleKills\":0,\"firstBlood\":false,\"timeToFirstKill\":null,\"killed\":false,\"killedFirst\":false,\"firstVictim\":true,\"exiled\":false,\"deathTurn\":1,\"exileTurn\":null,\"aliveDuration\":13,\"aliveTurns\":1,\"aliveMeetings\":0,\"tasks\":0,\"tasksMax\":4,\"taskSpeed\":null,\"goodVotes\":null,\"totalVotes\":null,\"goodVotesImportantMeetings\":null,\"totalVotesImportantMeetings\":null,\"skips\":0,\"votesReceived\":0,\"teammateVotes\":0,\"voteOrder\":null,\"voteDelay\":null,\"bodyReported\":0,\"selfReports\":0,\"reportDelay\":null,\"emergencyCalled\":0}}";

        [Fact]
        public void Sessions_ParseRealResponse()
        {
            Assert.True(PlayerApiResponse.TryParse(SessionsJson, out GLSessionList list));

            Assert.Equal("Matux", list.username);
            Assert.Equal(2, list.sessions.Count);
            GLSession first = list.sessions[0];
            Assert.Equal(19714, first.id);
            Assert.Equal("https://preprod.goodloss.fr/player/Matux/session/19714", first.url);
            Assert.Equal(1791967898, first.start);
            Assert.Equal(1791968073, first.end);
            Assert.Equal(8, first.games);
            Assert.False(first.live);
            Assert.True(list.sessions[1].live);
            Assert.True(list.sessions[1].current);
        }

        [Fact]
        public void LastGame_ParseRealResponse()
        {
            Assert.True(PlayerApiResponse.TryParse(LastGameJson, out GLLastGame lastGame));

            Assert.Equal(19715, lastGame.game.id);
            Assert.Equal("https://preprod.goodloss.fr/game/19715", lastGame.game.url);
            Assert.Equal("Among Us Ranked", lastGame.game.mod);
            Assert.Equal(3, lastGame.game.season);
            Assert.True(lastGame.game.ranked);
            Assert.Equal(54, lastGame.game.duration);
            Assert.Equal(new[] { "imp" }, lastGame.game.winners);

            GLLastGamePlayer player = lastGame.player;
            Assert.Equal("Shedconic", player.name);
            Assert.False(player.win);
            Assert.Null(player.rp);
            Assert.Equal(0, player.kills);
            Assert.True(player.firstVictim);
            Assert.Null(player.exileTurn);
            Assert.Equal(13, player.aliveDuration);
            Assert.Equal(4, player.tasksMax);
            Assert.Null(player.goodVotes);
        }

        [Fact]
        public void Sessions_LiveSessionHasNoEnd()
        {
            // Production response while a game of the session is in progress
            const string json = "{\"username\":\"Matux\",\"sessions\":[{\"id\":19704,\"url\":\"https:\\/\\/goodloss.fr\\/game\\/19704\",\"start\":1791982175,\"end\":null,\"games\":1,\"live\":true,\"current\":true}]}";

            Assert.True(PlayerApiResponse.TryParse(json, out GLSessionList list, out string error), error);

            Assert.Null(list.sessions[0].end);
            Assert.True(list.sessions[0].live);
        }

        [Fact]
        public void LastGame_NullNumbersAreAccepted()
        {
            const string json = "{\"username\":\"Matux\",\"game\":{\"id\":1,\"url\":null,\"mod\":null,\"season\":null,\"ranked\":false,\"map\":null,\"start\":null,\"end\":null,\"duration\":null,\"turns\":null,\"meetings\":null,\"players\":null,\"winners\":null},\"player\":{\"name\":\"A\",\"win\":true}}";

            Assert.True(PlayerApiResponse.TryParse(json, out GLLastGame lastGame, out string error), error);

            Assert.Null(lastGame.game.duration);
            Assert.Null(lastGame.game.players);
        }

        [Fact]
        public void TryParse_ExplainsTheFailure()
        {
            Assert.False(PlayerApiResponse.TryParse("{\"sessions\":[{\"games\":\"many\"}]}", out GLSessionList _, out string error));
            Assert.Contains("games", error);
            Assert.False(PlayerApiResponse.TryParse("", out GLSessionList _, out error));
            Assert.Equal("empty body", error);
        }

        [Fact]
        public void Snippet_TruncatesLongBodies()
        {
            Assert.Equal("<null>", PlayerApiResponse.Snippet(null));
            Assert.Equal("abc", PlayerApiResponse.Snippet("abc"));
            Assert.Equal("ab...", PlayerApiResponse.Snippet("abcdef", 2));
        }

        [Fact]
        public void LastGame_WithoutGame_ParsesWithNullParts()
        {
            Assert.True(PlayerApiResponse.TryParse("{\"username\":\"Matux\",\"game\":null,\"player\":null}", out GLLastGame lastGame));
            Assert.Null(lastGame.game);
            Assert.Null(lastGame.player);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("<html>404</html>")]
        [InlineData("null")]
        [InlineData("[1,2]")]
        public void TryParse_RejectsInvalidBodies(string json)
        {
            Assert.False(PlayerApiResponse.TryParse(json, out GLSessionList _));
        }

        [Theory]
        [InlineData("{\"error\":\"Player not found\"}", "Player not found")]
        [InlineData("{\"error\":\"Missing username\"}", "Missing username")]
        [InlineData("<html>Not Found</html>", null)]
        [InlineData("{\"error\":42}", null)]
        [InlineData("", null)]
        [InlineData(null, null)]
        public void ReadError_ExtractsApiMessage(string json, string expected)
        {
            Assert.Equal(expected, PlayerApiResponse.ReadError(json));
        }
    }

    public class StatFormatTests
    {
        [Theory]
        [InlineData(0, "0 s")]
        [InlineData(54, "54 s")]
        [InlineData(65, "1 min 05 s")]
        [InlineData(599, "9 min 59 s")]
        [InlineData(1863, "31 min")]
        [InlineData(7500, "2 h 05")]
        [InlineData(-3, "0 s")]
        public void Duration_IsHumanReadable(double seconds, string expected)
        {
            Assert.Equal(expected, StatFormat.Duration(seconds));
        }

        [Fact]
        public void NullValuesStayNull()
        {
            Assert.Null(StatFormat.Duration((double?)null));
            Assert.Null(StatFormat.Ratio(null, 4));
            Assert.Null(StatFormat.Ratio(1, null));
            Assert.Null(StatFormat.YesNo(null));
            Assert.Null(StatFormat.Number((int?)null));
            Assert.Null(StatFormat.Number((double?)null));
        }

        [Fact]
        public void Values_AreFormatted()
        {
            Assert.Equal("3/4", StatFormat.Ratio(3, 4));
            Assert.Equal("Yes", StatFormat.YesNo(true));
            Assert.Equal("No", StatFormat.YesNo(false));
            Assert.Equal("2", StatFormat.Number(2.0));
            Assert.Equal("1.57", StatFormat.Number(1.5678));
            Assert.Equal("7", StatFormat.Number(7));
        }

        [Theory]
        [InlineData("VanillaImpostor", "Impostor", "Impostor")]
        [InlineData("VanillaEngineer", "Crewmate", "Engineer")]
        [InlineData("Sheriff", "Crewmate", "Sheriff")]
        [InlineData("", "Crewmate", "Crewmate")]
        [InlineData(null, "Impostor", "Impostor")]
        [InlineData("Vanilla", "Crewmate", "Vanilla")]
        public void Role_DropsVanillaPrefix(string role, string team, string expected)
        {
            Assert.Equal(expected, StatFormat.Role(role, team));
        }

        [Fact]
        public void LocalDateTime_UsesCulture()
        {
            Assert.Equal("-", StatFormat.LocalDateTime(0));
            string formatted = StatFormat.LocalDateTime(1791967898, CultureInfo.InvariantCulture);
            Assert.Matches(@"^\d{2}/\d{2}/\d{4} \d{2}:\d{2}$", formatted);
        }
    }
}

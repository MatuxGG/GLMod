using System.Globalization;
using GLMod.Class;
using GLMod.Constants;
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

        // Contract proposed for GET /player/ranks and GET /player/stats (see CLAUDE.md §3.11)
        private const string RanksJson =
            "{\"username\":\"Matux\",\"ranks\":[" +
            "{\"mod\":\"Among Us Ranked\",\"season\":3,\"name\":\"Silver 2\",\"image\":\"https://amongusranked.orianagames.com/images/ranks/argent2.png\",\"elo\":706,\"rp\":28,\"position\":22,\"leaderboardUrl\":\"https://amongusranked.orianagames.com/leaderboards\"}," +
            "{\"mod\":\"Among Us Ranked Roles\",\"season\":3,\"name\":\"Unranked\",\"image\":null,\"elo\":1000,\"rp\":null,\"position\":null,\"leaderboardUrl\":null}]}";

        private const string StatsJson =
            "{\"username\":\"Matux\",\"mod\":null,\"season\":null,\"stats\":{\"games\":{\"wins\":548,\"games\":833},\"crewmate\":{\"wins\":506,\"games\":657},\"impostor\":{\"wins\":38,\"games\":148},\"neutral\":{\"wins\":4,\"games\":28}}}";

        [Fact]
        public void Ranks_ParseProposedContract()
        {
            Assert.True(PlayerApiResponse.TryParse(RanksJson, out GLPlayerRanks ranks, out string error), error);

            Assert.Equal(2, ranks.ranks.Count);
            GLPlayerRank first = ranks.ranks[0];
            Assert.Equal("Among Us Ranked", first.mod);
            Assert.Equal(3, first.season);
            Assert.Equal("Silver 2", first.name);
            Assert.Equal(706, first.elo);
            Assert.Equal(28, first.rp);
            Assert.Equal(22, first.position);
            Assert.Equal("https://amongusranked.orianagames.com/leaderboards", first.leaderboardUrl);
            Assert.Null(ranks.ranks[1].rp);
            Assert.Null(ranks.ranks[1].position);
            Assert.Null(ranks.ranks[1].image);
        }

        [Fact]
        public void Stats_ParseProposedContract()
        {
            Assert.True(PlayerApiResponse.TryParse(StatsJson, out GLPlayerStats stats, out string error), error);

            Assert.Null(stats.mod);
            Assert.Null(stats.season);
            Assert.Equal(548, stats.stats.games.wins);
            Assert.Equal(833, stats.stats.games.games);
            Assert.Equal(657, stats.stats.crewmate.games);
            Assert.Equal(38, stats.stats.impostor.wins);
            Assert.Equal(28, stats.stats.neutral.games);
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

    public class PlayerApiQueryTests
    {
        [Fact]
        public void Build_WithoutTheme_OnlyHasTheUsername()
        {
            Assert.Equal("https://goodloss.fr/api/player/stats?username=Matux",
                PlayerApiQuery.Build("https://goodloss.fr/api", "/player/stats", "Matux", null));
            Assert.Equal("https://goodloss.fr/api/player/stats?username=Matux",
                PlayerApiQuery.Build("https://goodloss.fr/api", "/player/stats", "Matux", " "));
        }

        [Fact]
        public void Build_WithRankedTheme()
        {
            Assert.Equal("https://goodloss.fr/api/player/ranks?username=Matux&theme=ranked",
                PlayerApiQuery.Build("https://goodloss.fr/api", "/player/ranks", "Matux", GameConstants.API_THEME_RANKED));
        }

        [Fact]
        public void Build_EscapesValues()
        {
            Assert.Equal("https://goodloss.fr/api/player/sessions?username=Jean%20Pierre%26co&theme=a%2Bb",
                PlayerApiQuery.Build("https://goodloss.fr/api", "/player/sessions", "Jean Pierre&co", "a+b"));
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

        [Theory]
        // Values of the profile page of the site
        [InlineData(548, 833, 66)]
        [InlineData(506, 657, 77)]
        [InlineData(38, 148, 26)]
        [InlineData(4, 28, 14)]
        [InlineData(0, 0, 0)]
        [InlineData(5, 0, 0)]
        [InlineData(1, 2, 50)]
        [InlineData(9, 4, 100)]
        public void WinPercent_IsRounded(int wins, int games, int expected)
        {
            Assert.Equal(expected, StatFormat.WinPercent(wins, games));
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

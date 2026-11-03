using GLMod.Class;
using GLMod.GLEntities;
using System;
using System.Globalization;
using Xunit;

namespace GLMod.Tests
{
    public class PlannedLobbyLogicTests
    {
        private static readonly DateTimeOffset Now = new DateTimeOffset(2026, 11, 2, 12, 0, 0, TimeSpan.Zero);

        private static GLNextLobby Next(string url = "https://goodloss.fr/lobbies/7?lang=fr", string startsAt = "2026-11-08T20:00:00+00:00")
        {
            return new GLNextLobby
            {
                lobby = new GLPlannedLobby { id = 7, url = url, startsAt = startsAt, players = 3, places = 15, minimum = 6 },
                texts = new GLNextLobbyTexts { title = "Next lobby", players = "players", action = "Take a place" },
            };
        }

        [Fact]
        public void BuildUrl_EscapesTheValues()
        {
            Assert.Equal("https://x/api/lobbies/next?language=pt-BR&mod=Among%20Us%20Ranked",
                PlannedLobbyLogic.BuildUrl("https://x/api", "/lobbies/next", "pt-BR", "Among Us Ranked"));
        }

        [Theory]
        [InlineData(null, null, "https://x/api/lobbies/next")]
        [InlineData("fr", " ", "https://x/api/lobbies/next?language=fr")]
        [InlineData(null, "Challenger", "https://x/api/lobbies/next?mod=Challenger")]
        public void BuildUrl_LeavesOutWhatIsNotKnown(string language, string mod, string expected)
        {
            Assert.Equal(expected, PlannedLobbyLogic.BuildUrl("https://x/api", "/lobbies/next", language, mod));
        }

        [Fact]
        public void ShouldShow_ALobbyToCome()
        {
            Assert.True(PlannedLobbyLogic.ShouldShow(Next(), "", Now));
        }

        [Fact]
        public void ShouldShow_NotADismissedLobbyButTheNextOne()
        {
            Assert.False(PlannedLobbyLogic.ShouldShow(Next(), "7", Now));
            Assert.True(PlannedLobbyLogic.ShouldShow(Next(), "6", Now));
        }

        [Fact]
        public void ShouldShow_NotWithoutALobbyOrItsTexts()
        {
            Assert.False(PlannedLobbyLogic.ShouldShow(null, "", Now));
            Assert.False(PlannedLobbyLogic.ShouldShow(new GLNextLobby { texts = Next().texts }, "", Now));
            GLNextLobby noTexts = Next();
            noTexts.texts = null;
            Assert.False(PlannedLobbyLogic.ShouldShow(noTexts, "", Now));
        }

        [Fact]
        public void ShouldShow_NotALobbyAlreadyStarted()
        {
            Assert.False(PlannedLobbyLogic.ShouldShow(Next(startsAt: "2026-11-02T12:00:00+00:00"), "", Now));
            Assert.False(PlannedLobbyLogic.ShouldShow(Next(startsAt: "soon"), "", Now));
        }

        [Theory]
        [InlineData("file:///C:/Windows/System32/calc.exe")]
        [InlineData("steam://run/945360")]
        [InlineData("/lobbies/7")]
        [InlineData("")]
        [InlineData(null)]
        public void ShouldShow_NothingButAWebPageIsOpened(string url)
        {
            Assert.False(PlannedLobbyLogic.ShouldShow(Next(url), "", Now));
        }

        [Fact]
        public void Text_IsInTheTimeZoneOfThePlayer()
        {
            TimeZoneInfo plusOne = TimeZoneInfo.CreateCustomTimeZone("plus1", TimeSpan.FromHours(1), "plus1", "plus1");

            Assert.Equal("Next lobby · Sun 8 Nov 21:00 · 3/15 players", PlannedLobbyLogic.Text(Next(), plusOne, CultureInfo.InvariantCulture));
        }

        [Theory]
        [InlineData(null)]
        [InlineData(" ")]
        [InlineData("not a culture!")]
        public void CultureOf_FallsBackToInvariant(string language)
        {
            Assert.Same(CultureInfo.InvariantCulture, PlannedLobbyLogic.CultureOf(language));
        }
    }
}

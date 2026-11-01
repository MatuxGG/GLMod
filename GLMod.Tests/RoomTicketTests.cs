using GLMod.Class;
using GLMod.GLEntities;
using GLMod.TestHarness;
using System.Linq;
using System.Text;
using Xunit;

namespace GLMod.Tests
{
    public class RoomTicketFooterTests
    {
        [Fact]
        public void Append_AddsTicketLengthAndMarkerAfterTheData()
        {
            byte[] result = RoomTicketFooter.Append(new byte[] { 1, 2, 3 }, "v1.abc");

            Assert.Equal(new byte[] { 1, 2, 3 }, result.Take(3).ToArray());
            Assert.Equal("v1.abc", Encoding.ASCII.GetString(result, 3, 6));
            Assert.Equal(6, result[9]);
            Assert.Equal(0, result[10]);
            Assert.Equal("GLMT", Encoding.ASCII.GetString(result, 11, 4));
            Assert.Equal(15, result.Length);
        }

        [Fact]
        public void Append_WritesTheLengthLittleEndian()
        {
            byte[] result = RoomTicketFooter.Append(new byte[0], new string('a', 300));

            Assert.Equal(300 & 0xFF, result[300]);
            Assert.Equal(300 >> 8, result[301]);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Append_WithoutTicketKeepsTheData(string ticket)
        {
            var data = new byte[] { 1, 2, 3 };
            Assert.Same(data, RoomTicketFooter.Append(data, ticket));
        }
    }

    public class GoodLossServersTests
    {
        [Theory]
        [InlineData("impranked.goodloss.fr", true)]
        [InlineData("betaranked.goodloss.fr", true)]
        [InlineData("BETARANKED.goodloss.fr ", true)]
        [InlineData("imp.goodloss.fr", false)]
        [InlineData("matchmaker.among.us", false)]
        [InlineData("impranked.goodloss.fr.evil.example", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void OnlyTheGoodLossRankedServersGetTheTicket(string pingServer, bool expected)
        {
            Assert.Equal(expected, GoodLossServers.IsGoodLossServer(pingServer));
        }

        // k0040: a LAN or free play game keeps the selected region, its host must not get the ticket
        [Theory]
        [InlineData(true, "impranked.goodloss.fr", true)]
        [InlineData(false, "impranked.goodloss.fr", false)]
        [InlineData(true, "matchmaker.among.us", false)]
        [InlineData(false, "matchmaker.among.us", false)]
        [InlineData(true, null, false)]
        public void OnlyAnOnlineGameOnAGoodLossServerGetsTheTicket(bool onlineGame, string pingServer, bool expected)
        {
            Assert.Equal(expected, GoodLossServers.ReceivesRoomTicket(onlineGame, pingServer));
        }
    }

    public class RoomTicketResponseTests
    {
        [Fact]
        public void ParsesTheAnswerOfTheApi()
        {
            const string body = "{\"ticket\":\"v1.42.GVOVVF.1800000100.sig\",\"roomCode\":\"GVOVVF\",\"expiresAt\":1800000100}";

            Assert.True(PlayerApiResponse.TryParse(body, out GLRoomTicket ticket));
            Assert.Equal("v1.42.GVOVVF.1800000100.sig", ticket.ticket);
            Assert.Equal("GVOVVF", ticket.roomCode);
            Assert.Equal(1800000100, ticket.expiresAt);
        }

        [Fact]
        public void AccountWithoutCodeHasANullRoomCode()
        {
            Assert.True(PlayerApiResponse.TryParse("{\"ticket\":\"t\",\"roomCode\":null,\"expiresAt\":1}", out GLRoomTicket ticket));
            Assert.Null(ticket.roomCode);
        }
    }

    public class PluginChecksTests
    {
        private const string PlayerHelp = "****\nHost Commands:\n****\n\n****\nPlayer Commands:\n****\n#code: Get code\n#help: List\n";
        private const string HostHelp = "****\nHost Commands:\n****\n#ban: Ban\n#kick: Kick\n\n****\nPlayer Commands:\n****\n#code: Get code\n#help: List\n";

        [Fact]
        public void HelpOfAPlayerHidesTheHostCommands()
        {
            Assert.Null(PluginChecks.VerifyHelp(PlayerHelp, isHost: false));
            Assert.NotNull(PluginChecks.VerifyHelp(HostHelp, isHost: false));
        }

        [Fact]
        public void HelpOfTheHostListsTheHostCommands()
        {
            Assert.Null(PluginChecks.VerifyHelp(HostHelp, isHost: true));
            Assert.NotNull(PluginChecks.VerifyHelp(PlayerHelp, isHost: true));
        }

        [Fact]
        public void HelpMustNotListTheRemovedRoomCommand()
        {
            Assert.Contains("#room", PluginChecks.VerifyHelp(PlayerHelp + "#room: Link\n", isHost: false));
        }

        [Fact]
        public void NoAnswerIsAFailure()
        {
            Assert.NotNull(PluginChecks.VerifyHelp(null, isHost: false));
        }

        [Fact]
        public void TheSentCommandIsNotAnAnswer()
        {
            string[] chat = { "#room ZZZZZZ" };
            Assert.False(PluginChecks.IsAnswered(chat, "#room ZZZZZZ", "ZZZZZZ"));
            Assert.True(PluginChecks.IsAnswered(new[] { "#room ZZZZZZ", "ZZZZZZ saved" }, "#room ZZZZZZ", "ZZZZZZ"));
        }

        [Fact]
        public void FindsTheHelpAnswerAmongTheChatLines()
        {
            Assert.Equal(PlayerHelp, PluginChecks.FindHelpAnswer(new[] { "#help", "hello", PlayerHelp }));
            Assert.Null(PluginChecks.FindHelpAnswer(new[] { "#help", "hello" }));
        }
    }
}

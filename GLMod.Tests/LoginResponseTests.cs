using GLMod.Class;
using GLMod.GLEntities;
using Xunit;

namespace GLMod.Tests
{
    public class LoginResponseTests
    {
        [Fact]
        public void ToHex_EncodesOnlyTheTicketBytes()
        {
            var buffer = new byte[] { 0x14, 0x00, 0xAB, 0xFF, 0x99, 0x99 };

            Assert.Equal("1400ABFF", LoginResponse.ToHex(buffer, 4));
        }

        [Fact]
        public void ToHex_RefusesAnEmptyOrOverflowingLength()
        {
            Assert.Null(LoginResponse.ToHex(new byte[4], 0));
            Assert.Null(LoginResponse.ToHex(new byte[4], 5));
            Assert.Null(LoginResponse.ToHex(null, 1));
        }

        [Fact]
        public void TryParseTicketLogin_ReadsTheSiteAnswer()
        {
            // Answer of the site to a real ticket on 2026-10-19 (token shortened)
            const string body = "{\"token\":\"glm_d5nhAbC-_x\",\"username\":\"Matux\",\"expiresAt\":1795010739}";

            Assert.True(LoginResponse.TryParseTicketLogin(body, out GLLoginResponse login));
            Assert.Equal("glm_d5nhAbC-_x", login.token);
            Assert.Equal("Matux", login.username);
            Assert.Equal(1795010739, login.expiresAt);
        }

        [Fact]
        public void TryParseTicketLogin_KeepsEscapedCharactersOfTheUsername()
        {
            const string body = "{\"token\":\"glm_x\",\"username\":\"Dan\\u0022go\\u00e9\",\"expiresAt\":1}";

            Assert.True(LoginResponse.TryParseTicketLogin(body, out GLLoginResponse login));
            Assert.Equal("Dan\"goé", login.username);
        }

        [Theory]
        [InlineData("Matux#d1b20936-fe67")]              // legacy text answer
        [InlineData("{\"error\":\"Invalid ticket\"}")]
        [InlineData("{\"token\":\"Matux#d1b2\",\"username\":\"Matux\"}")] // not a game token
        [InlineData("{\"token\":\"glm_x\",\"username\":\"\"}")]
        [InlineData("")]
        [InlineData(null)]
        public void TryParseTicketLogin_RefusesAnythingButAGameToken(string body)
        {
            Assert.False(LoginResponse.TryParseTicketLogin(body, out GLLoginResponse login));
            Assert.Null(login);
        }

        [Theory]
        [InlineData("Banned: cheating", "cheating")]
        [InlineData("\"Banned: toxic chat\"", "toxic chat")]
        [InlineData("banned: x", "x")]
        [InlineData("", null)]
        [InlineData(null, null)]
        [InlineData("{\"error\":\"Too many logins\"}", null)]
        public void ReadBanReason(string body, string expected)
        {
            Assert.Equal(expected, LoginResponse.ReadBanReason(body));
        }

        [Theory]
        [InlineData("Matux#d1b20936-fe67-4cd3", "Matux")]
        [InlineData("glm_abc", "")]
        [InlineData("#nobody", "")]
        [InlineData("", "")]
        [InlineData(null, "")]
        public void AccountNameOfLegacyToken(string token, string expected)
        {
            Assert.Equal(expected, LoginResponse.AccountNameOfLegacyToken(token));
        }

        [Fact]
        public void CanReuseToken_OnlyWhileItStaysValidADayLonger()
        {
            const long now = 1_800_000_000;

            Assert.True(LoginResponse.CanReuseToken(now + 86400, now));
            Assert.False(LoginResponse.CanReuseToken(now + 86399, now));
            Assert.False(LoginResponse.CanReuseToken(now - 1, now));
            Assert.False(LoginResponse.CanReuseToken(null, now));
        }
    }
}

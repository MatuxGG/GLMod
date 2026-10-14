using GLMod.Class;
using GLMod.Constants;
using Xunit;

namespace GLMod.Tests
{
    public class GameConstantsTests
    {
        [Fact]
        public void ApiEndpoint_PointsToGoodLoss()
        {
            Assert.Equal("https://goodloss.fr/api", GameConstants.API_ENDPOINT);
        }

        [Fact]
        public void SupportIdAlphabet_OmitsZero()
        {
            // '0' is excluded to avoid confusion with 'O' when users read support ids aloud.
            Assert.DoesNotContain('0', GameConstants.SUPPORT_ID_CHARS);
        }

        [Fact]
        public void ActionPrefixes_EndWithUnderscore()
        {
            // Every action prefix is a string-concatenation prefix; the trailing '_'
            // is part of the contract with the Good Loss API.
            Assert.EndsWith("_", GameConstants.ACTION_PREFIX_DC_PROCESS);
            Assert.EndsWith("_", GameConstants.ACTION_PREFIX_DC_INTERNAL);
            Assert.EndsWith("_", GameConstants.ACTION_PREFIX_DC_HANDLE);
            Assert.EndsWith("_", GameConstants.ACTION_PREFIX_DC_ON);
            Assert.EndsWith("_", GameConstants.ACTION_PREFIX_SAB_START);
            Assert.EndsWith("_", GameConstants.ACTION_PREFIX_SAB_END);
        }
    }

    public class ApiEndpointResolverTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("not a url")]
        [InlineData("ftp://preprod.goodloss.fr/api")]
        public void Resolve_FallsBackToProduction(string overrideValue)
        {
            Assert.Equal(GameConstants.API_ENDPOINT, ApiEndpointResolver.Resolve(overrideValue));
        }

        [Theory]
        [InlineData("https://preprod.goodloss.fr/api", "https://preprod.goodloss.fr/api")]
        [InlineData(" https://preprod.goodloss.fr/api/ ", "https://preprod.goodloss.fr/api")]
        [InlineData("http://localhost:8080/api", "http://localhost:8080/api")]
        public void Resolve_UsesValidOverride(string overrideValue, string expected)
        {
            Assert.Equal(expected, ApiEndpointResolver.Resolve(overrideValue));
        }
    }
}

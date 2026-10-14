using GLMod.Class;
using Xunit;

namespace GLMod.Tests
{
    public class RankedModTests
    {
        [Theory]
        [InlineData("Among Us Ranked", true)]
        [InlineData("Among Us Ranked Roles", true)]
        [InlineData("among us ranked", true)]
        [InlineData("  Among Us Ranked", true)]
        [InlineData("Vanilla", false)]
        [InlineData("Challenger Maps", false)]
        [InlineData("Among Us", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void Matches_AmongUsRankedAndVariants(string modName, bool expected)
        {
            Assert.Equal(expected, RankedMod.Matches(modName));
        }
    }
}

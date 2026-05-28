using GLMod.Enums;
using Xunit;

namespace GLMod.Tests
{
    public class SabotageTypeTests
    {
        [Theory]
        [InlineData(SabotageType.Reactor, "Reactor")]
        [InlineData(SabotageType.Coms, "Coms")]
        [InlineData(SabotageType.Lights, "Lights")]
        [InlineData(SabotageType.O2, "O2")]
        public void ToActionString_ReturnsEnumName(SabotageType value, string expected)
        {
            Assert.Equal(expected, value.ToActionString());
        }

        [Theory]
        [InlineData("Reactor", SabotageType.Reactor)]
        [InlineData("Coms", SabotageType.Coms)]
        [InlineData("Lights", SabotageType.Lights)]
        [InlineData("O2", SabotageType.O2)]
        public void TryParse_KnownValues_ReturnsTrue(string input, SabotageType expected)
        {
            bool ok = SabotageTypeExtensions.TryParse(input, out var result);

            Assert.True(ok);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("Unknown")]
        [InlineData("reactor")]
        public void TryParse_UnknownValue_ReturnsFalse(string input)
        {
            bool ok = SabotageTypeExtensions.TryParse(input, out _);
            Assert.False(ok);
        }
    }

    public class GameMapTypeTests
    {
        [Theory]
        [InlineData((byte)0, GameMapType.TheSkeld)]
        [InlineData((byte)1, GameMapType.MiraHQ)]
        [InlineData((byte)2, GameMapType.Polus)]
        [InlineData((byte)4, GameMapType.Airship)]
        [InlineData((byte)5, GameMapType.TheFungle)]
        public void FromMapId_KnownIds_MapToType(byte id, GameMapType expected)
        {
            Assert.Equal(expected, GameMapTypeExtensions.FromMapId(id));
        }

        [Theory]
        [InlineData((byte)3)]
        [InlineData((byte)42)]
        [InlineData((byte)255)]
        public void FromMapId_UnknownId_ReturnsUnknown(byte id)
        {
            Assert.Equal(GameMapType.Unknown, GameMapTypeExtensions.FromMapId(id));
        }

        [Theory]
        [InlineData(GameMapType.TheSkeld, "The Skeld")]
        [InlineData(GameMapType.MiraHQ, "MiraHQ")]
        [InlineData(GameMapType.Polus, "Polus")]
        [InlineData(GameMapType.Airship, "Airship")]
        [InlineData(GameMapType.TheFungle, "The Fungle")]
        [InlineData(GameMapType.Unknown, "Unknown")]
        public void ToDisplayName_AllKnownTypes(GameMapType type, string expected)
        {
            Assert.Equal(expected, type.ToDisplayName());
        }
    }
}

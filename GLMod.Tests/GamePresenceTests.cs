using GLMod.Class;
using GLMod.Constants;
using InnerNet;
using Xunit;

namespace GLMod.Tests
{
    public class GamePresenceTests
    {
        // k0037: the end screen was declared as the menu, without the code of the room
        [Theory]
        [InlineData(InnerNetClient.GameStates.NotJoined, false)]
        [InlineData(InnerNetClient.GameStates.Joined, true)]
        [InlineData(InnerNetClient.GameStates.Started, true)]
        [InlineData(InnerNetClient.GameStates.Ended, true)]
        public void IsInOnlineGame_CoversTheEndScreen(InnerNetClient.GameStates state, bool expected)
        {
            Assert.Equal(expected, PlayModeHelper.IsInOnlineGame(state));
        }

        // k0037: a player on the end screen must not be announced as joinable
        [Theory]
        [InlineData(InnerNetClient.GameStates.Joined, GameConstants.PRESENCE_STATE_LOBBY)]
        [InlineData(InnerNetClient.GameStates.Started, GameConstants.PRESENCE_STATE_GAME)]
        [InlineData(InnerNetClient.GameStates.Ended, GameConstants.PRESENCE_STATE_GAME)]
        public void StateOf_NeverDeclaresTheEndScreenAsALobby(InnerNetClient.GameStates state, string expected)
        {
            Assert.Equal(expected, GamePresence.StateOf(state));
        }
    }
}

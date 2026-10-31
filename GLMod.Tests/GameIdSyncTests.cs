using GLMod.Enums;
using GLMod.GLEntities;
using GLMod.Services.Implementations;
using Xunit;

namespace GLMod.Tests
{
    /// <summary>
    /// k0025: a host leaving during SyncGameId's RPC_SYNC_TIMEOUT wait had Step reset to Initial,
    /// then the coroutine set it back to PlayersRecorded, so the next game was never started
    /// and its winners were written into the old game.
    /// </summary>
    public class GameIdSyncTests
    {
        private static GLGame Game(string id = "42")
            => new GLGame("******", "TheSkeld", false, "Vanilla") { id = id };

        [Fact]
        public void K0025_DisconnectDuringWait_DoesNotResumeOldGame()
        {
            GLGame game = Game();

            // InnerNetClientPatch resets Step to Initial without touching CurrentGame
            Assert.False(GameStateManager.IsGameIdSyncCurrent(game, game, GameStep.Initial));
        }

        [Fact]
        public void K0025_GameChangedDuringWait_DoesNotResumeOldGame()
        {
            GLGame game = Game();

            Assert.False(GameStateManager.IsGameIdSyncCurrent(game, Game("43"), GameStep.GameIdSynced));
            Assert.False(GameStateManager.IsGameIdSyncCurrent(game, null, GameStep.GameIdSynced));
            Assert.False(GameStateManager.IsGameIdSyncCurrent(null, null, GameStep.GameIdSynced));
        }

        [Fact]
        public void K0025_NothingChanged_SendsGameId()
        {
            GLGame game = Game();

            Assert.True(GameStateManager.IsGameIdSyncCurrent(game, game, GameStep.GameIdSynced));
        }
    }
}

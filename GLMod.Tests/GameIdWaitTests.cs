using GLMod.Constants;
using GLMod.GLEntities;
using GLMod.Services.Implementations;
using Xunit;

namespace GLMod.Tests
{
    /// <summary>
    /// k0018: AddMyPlayer waited forever for a game id that a host without GLMod
    /// (or whose SendGame failed) never sends, and threw after ResetGame.
    /// </summary>
    public class GameIdWaitTests
    {
        private static GLGame Game(string id = null)
            => new GLGame("******", "TheSkeld", false, "Vanilla") { id = id };

        // Same loop as AddMyPlayer, one step per WaitForSeconds(0.1f)
        private static int StepsUntilDone(GLGame game, System.Func<int, GLGame> currentGameAtStep)
        {
            int steps = 0;
            float waited = 0f;
            while (GameStateManager.ShouldWaitForGameId(game, currentGameAtStep(steps), waited) && steps < 100000)
            {
                steps++;
                waited += 0.1f;
            }
            return steps;
        }

        [Fact]
        public void K0018_GameIdNeverSent_StopsAfterTimeout()
        {
            GLGame game = Game();

            int steps = StepsUntilDone(game, _ => game);

            Assert.InRange(steps, 1, (int)(GameConstants.GAME_ID_WAIT_TIMEOUT / 0.1f) + 1);
        }

        [Fact]
        public void K0018_GameReset_StopsWaiting()
        {
            GLGame game = Game();

            // ResetGame sets CurrentGame to null after a few steps
            Assert.Equal(3, StepsUntilDone(game, step => step < 3 ? game : null));
            // A new game started meanwhile is not the awaited one
            Assert.False(GameStateManager.ShouldWaitForGameId(game, Game(), 0f));
            Assert.False(GameStateManager.ShouldWaitForGameId(null, null, 0f));
        }

        [Fact]
        public void K0018_GameIdReceived_StopsWaiting()
        {
            GLGame game = Game();

            Assert.True(GameStateManager.ShouldWaitForGameId(game, game, 0f));
            game.id = "42";
            Assert.False(GameStateManager.ShouldWaitForGameId(game, game, 0f));
        }
    }
}

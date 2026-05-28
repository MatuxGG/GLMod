using System.Collections.Generic;
using GLMod.GLEntities;
using Xunit;

namespace GLMod.Tests
{
    public class GLPositionTests
    {
        [Fact]
        public void Constructor_AssignsAllFields()
        {
            var pos = new GLPosition(1.5f, -2.25f, "1234567890", "5");

            Assert.Equal(1.5f, pos.x);
            Assert.Equal(-2.25f, pos.y);
            Assert.Equal("1234567890", pos.triggerTime);
            Assert.Equal("5", pos.turn);
        }
    }

    public class GLPlayerTests
    {
        [Fact]
        public void DefaultConstructor_InitializesCountersToZero()
        {
            var player = new GLPlayer();

            Assert.Equal("0", player.tasks);
            Assert.Equal("0", player.tasksDead);
            Assert.Equal("0", player.tasksMax);
            Assert.Equal("0", player.win);
            Assert.Empty(player.positions);
        }

        [Fact]
        public void AddTasks_IncrementsTaskCounter()
        {
            var player = new GLPlayer();

            player.AddTasks();
            player.AddTasks();
            player.AddTasks();

            Assert.Equal("3", player.tasks);
        }

        [Fact]
        public void AddTasksDead_IncrementsDeadTaskCounter()
        {
            var player = new GLPlayer();

            player.AddTasksDead();
            player.AddTasksDead();

            Assert.Equal("2", player.tasksDead);
        }

        [Fact]
        public void SetTasksMax_StoresValueAsString()
        {
            var player = new GLPlayer();

            player.SetTasksMax(42);

            Assert.Equal("42", player.tasksMax);
        }

        [Fact]
        public void SetWin_MarksPlayerAsWinner()
        {
            var player = new GLPlayer();

            player.SetWin();

            Assert.Equal("1", player.win);
        }

        [Fact]
        public void AddPosition_RoundsCoordinatesAndStoresTurn()
        {
            var player = new GLPlayer();

            player.AddPosition(1.234567f, 2.985654f, "ts", "7");

            Assert.Single(player.positions);
            Assert.Equal(1.23f, player.positions[0].x);
            Assert.Equal(2.99f, player.positions[0].y);
            Assert.Equal("ts", player.positions[0].triggerTime);
            Assert.Equal("7", player.positions[0].turn);
        }

        [Fact]
        public void SetColor_StoresColor()
        {
            var player = new GLPlayer();

            player.SetColor("Red");

            Assert.Equal("Red", player.color);
        }
    }

    public class GLGameTests
    {
        private static GLGame NewGame(bool ranked = false)
            => new GLGame("ABCDEF", "Polus", ranked, "Vanilla");

        [Fact]
        public void Constructor_RankedFalse_StoresZero()
        {
            var game = NewGame(ranked: false);

            Assert.Equal("0", game.ranked);
            Assert.Equal("ABCDEF", game.code);
            Assert.Equal("Polus", game.map);
            Assert.Equal("Vanilla", game.modName);
            Assert.Equal("1", game.turns);
            Assert.Equal("", game.winner);
            Assert.Empty(game.players);
            Assert.Empty(game.actions);
        }

        [Fact]
        public void Constructor_RankedTrue_StoresOne()
        {
            var game = NewGame(ranked: true);
            Assert.Equal("1", game.ranked);
        }

        [Fact]
        public void SetRanked_FlipsValue()
        {
            var game = NewGame(ranked: false);

            game.SetRanked(true);
            Assert.Equal("1", game.ranked);

            game.SetRanked(false);
            Assert.Equal("0", game.ranked);
        }

        [Fact]
        public void SetIdAndGetId_RoundTrip()
        {
            var game = NewGame();

            game.SetId(42);

            Assert.Equal("42", game.id);
            Assert.Equal(42, game.GetId());
        }

        [Fact]
        public void GetId_WhenIdIsNotInteger_ThrowsInvalidOperation()
        {
            var game = NewGame();
            game.id = "not-an-int";

            var ex = Assert.Throws<System.InvalidOperationException>(() => game.GetId());
            Assert.Contains("not-an-int", ex.Message);
        }

        [Fact]
        public void GetId_WhenIdIsNull_ThrowsInvalidOperation()
        {
            var game = NewGame();

            var ex = Assert.Throws<System.InvalidOperationException>(() => game.GetId());
            Assert.Contains("<null>", ex.Message);
        }

        [Fact]
        public void AddPlayer_AppendsPlayerWithDefaults()
        {
            var game = NewGame();

            game.AddPlayer("login1", "Alice", "Sheriff", "Crewmate", "Red");

            Assert.Single(game.players);
            var p = game.players[0];
            Assert.Equal("login1", p.login);
            Assert.Equal("Alice", p.playerName);
            Assert.Equal("Sheriff", p.role);
            Assert.Equal("Crewmate", p.team);
            Assert.Equal("Red", p.color);
            Assert.Equal("0", p.win);
        }

        [Fact]
        public void SetWinner_MarksPlayersOnWinningTeam()
        {
            var game = NewGame();
            game.AddPlayer(null, "Alice", "Crewmate", "Crewmate", "Red");
            game.AddPlayer(null, "Bob", "Impostor", "Impostor", "Blue");
            game.AddPlayer(null, "Carol", "Engineer", "Crewmate", "Green");

            game.SetWinner("Crewmate");

            Assert.Equal("Crewmate", game.winner);
            Assert.Equal("1", game.players[0].win);
            Assert.Equal("0", game.players[1].win);
            Assert.Equal("1", game.players[2].win);
        }

        [Fact]
        public void SetWinners_AcceptsMultipleTeams()
        {
            var game = NewGame();
            game.AddPlayer(null, "Alice", "Crewmate", "Crewmate", "Red");
            game.AddPlayer(null, "Bob", "Lover", "Love", "Blue");
            game.AddPlayer(null, "Carol", "Impostor", "Impostor", "Green");

            game.SetWinners(new List<string> { "Crewmate", "Love" });

            Assert.Equal("Crewmate", game.winner);
            Assert.Equal("1", game.players[0].win);
            Assert.Equal("1", game.players[1].win);
            Assert.Equal("0", game.players[2].win);
        }

        [Fact]
        public void AddTurn_BelowThousand_Shifts()
        {
            var game = NewGame();
            Assert.Equal("1", game.turns);

            game.AddTurn();

            // 1 + 1000 = 1001 (meeting turn marker)
            Assert.Equal("1001", game.turns);
        }

        [Fact]
        public void AddTurn_AtMeetingMarker_RollsBackToSequence()
        {
            var game = NewGame();
            game.turns = "1001";

            game.AddTurn();

            // 1001 - 999 = 2
            Assert.Equal("2", game.turns);
        }

        [Fact]
        public void AddAction_AppendsActionWithCurrentTurn()
        {
            var game = NewGame();

            game.AddAction("Alice", "Bob", "killed");

            Assert.Single(game.actions);
            Assert.Equal("1", game.actions[0].turn);
            Assert.Equal("Alice", game.actions[0].source);
            Assert.Equal("Bob", game.actions[0].target);
            Assert.Equal("killed", game.actions[0].action);
            Assert.False(string.IsNullOrEmpty(game.actions[0].triggerTimeMs));
        }

        [Fact]
        public void AddPosition_RoutesToCorrectPlayer_AndCarriesTurn()
        {
            var game = NewGame();
            game.AddPlayer(null, "Alice", "Sheriff", "Crewmate", "Red");
            game.turns = "3";

            game.AddPosition("Alice", 5f, 6f, "ts");

            Assert.Single(game.players[0].positions);
            Assert.Equal("3", game.players[0].positions[0].turn);
            Assert.Equal("ts", game.players[0].positions[0].triggerTime);
        }
    }
}

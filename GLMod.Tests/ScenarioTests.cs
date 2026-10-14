using System;
using System.Collections.Generic;
using GLMod.GLEntities;
using GLMod.TestHarness;
using Xunit;

namespace GLMod.Tests
{
    public class ScenarioPlanTests
    {
        private static List<ScenarioPlayer> Players(byte impostorId)
        {
            var players = new List<ScenarioPlayer>();
            for (byte id = 0; id < 4; id++)
            {
                players.Add(new ScenarioPlayer(id, "P" + id, id == impostorId));
            }
            return players;
        }

        [Fact]
        public void Build_AssignsCrewmatesByPlayerId()
        {
            ScenarioPlan plan = ScenarioPlan.Build("run", 1, Players(impostorId: 1));

            Assert.Equal(1, plan.Impostor.Id);
            Assert.Equal(0, plan.CrewA.Id);
            Assert.Equal(2, plan.CrewB.Id);
            Assert.Equal(3, plan.CrewC.Id);
            Assert.Equal("run", plan.RunId);
            Assert.Equal(1, plan.GameNumber);
        }

        [Fact]
        public void Build_RejectsWrongPlayerCount()
        {
            List<ScenarioPlayer> players = Players(impostorId: 0);
            players.RemoveAt(3);

            Assert.Throws<InvalidOperationException>(() => ScenarioPlan.Build("run", 1, players));
        }

        [Fact]
        public void Build_RejectsZeroOrSeveralImpostors()
        {
            List<ScenarioPlayer> none = Players(impostorId: 9);
            List<ScenarioPlayer> two = Players(impostorId: 0);
            two[1].IsImpostor = true;

            Assert.Throws<InvalidOperationException>(() => ScenarioPlan.Build("run", 1, none));
            Assert.Throws<InvalidOperationException>(() => ScenarioPlan.Build("run", 1, two));
        }

        [Fact]
        public void Build_RejectsDuplicateNamesAndUnknownGame()
        {
            List<ScenarioPlayer> players = Players(impostorId: 0);
            players[2].Name = players[1].Name;

            Assert.Throws<InvalidOperationException>(() => ScenarioPlan.Build("run", 1, players));
            Assert.Throws<ArgumentOutOfRangeException>(() => ScenarioPlan.Build("run", 3, Players(impostorId: 0)));
        }

        [Fact]
        public void GetVoteTarget_GameOne_TieBetweenTaskerAndSkip()
        {
            ScenarioPlan plan = ScenarioPlan.Build("run", 1, Players(impostorId: 3));

            Assert.Equal(plan.Tasker.Id, plan.GetVoteTarget(plan.Impostor.Id));
            Assert.Equal(plan.Tasker.Id, plan.GetVoteTarget(plan.Victim.Id));
            Assert.Equal(ScenarioPlan.SKIP_VOTE, plan.GetVoteTarget(plan.Tasker.Id));
            Assert.Equal(ScenarioPlan.SKIP_VOTE, plan.GetVoteTarget(plan.Caller.Id));
        }

        [Fact]
        public void GetVoteTarget_GameTwo_ExilesACrewmate()
        {
            ScenarioPlan plan = ScenarioPlan.Build("run", 2, Players(impostorId: 0));

            Assert.Equal(plan.ExiledCrewmate.Id, plan.GetVoteTarget(plan.Impostor.Id));
            Assert.Equal(plan.ExiledCrewmate.Id, plan.GetVoteTarget(plan.Reporter.Id));
            Assert.Equal(plan.Impostor.Id, plan.GetVoteTarget(plan.ExiledCrewmate.Id));
            Assert.Null(plan.GetVoteTarget(plan.Victim.Id));
        }
    }

    public class ScenarioVerifierTests
    {
        private static ScenarioPlan Plan(int gameNumber)
        {
            // Impostor "Imp", then crewmates A (victim), B (tasker / reporter), C (caller / exiled)
            return ScenarioPlan.Build("run", gameNumber, new List<ScenarioPlayer>
            {
                new ScenarioPlayer(0, "Imp", true),
                new ScenarioPlayer(1, "A", false),
                new ScenarioPlayer(2, "B", false),
                new ScenarioPlayer(3, "C", false)
            });
        }

        private static GLGame NewGame(string id)
        {
            var game = new GLGame("******", "TheSkeld", false, "Vanilla") { id = id };
            game.AddPlayer(null, "Imp", "VanillaImpostor", "Impostor", "0");
            game.AddPlayer(null, "A", "VanillaCrewmate", "Crewmate", "1");
            game.AddPlayer(null, "B", "VanillaCrewmate", "Crewmate", "2");
            game.AddPlayer(null, "C", "VanillaCrewmate", "Crewmate", "3");
            return game;
        }

        private static void CompleteTasks(GLGame game, string name, int count, bool dead)
        {
            GLPlayer player = game.players.Find(p => p.playerName == name);
            for (int i = 0; i < count; i++)
            {
                game.AddAction(name, "", "completedTask");
                if (dead) player.AddTasksDead(); else player.AddTasks();
            }
        }

        /// <summary>Game 1 as GLMod should record it (turns 1 → 1001 → 2).</summary>
        private static GLGame ExpectedGameOne()
        {
            GLGame game = NewGame("41");
            CompleteTasks(game, "B", 1, dead: false);
            game.AddAction("", "", "SAB_START_Lights");
            game.AddAction("", "", "SAB_END_Lights");
            game.AddAction("C", "", "called an emergency");
            game.AddTurn();
            game.AddAction("Imp", "B", "voted");
            game.AddAction("A", "B", "voted");
            game.AddAction("B", "", "voted");
            game.AddAction("C", "", "voted");
            game.AddTurn();
            game.AddAction("Imp", "A", "killed");
            CompleteTasks(game, "A", 3, dead: true);
            CompleteTasks(game, "B", 2, dead: false);
            CompleteTasks(game, "C", 3, dead: false);
            game.players.ForEach(p => p.SetTasksMax(3));
            game.SetWinners(new List<string> { "Crewmate" });
            return game;
        }

        /// <summary>Game 2 as GLMod should record it.</summary>
        private static GLGame ExpectedGameTwo()
        {
            GLGame game = NewGame("42");
            game.AddAction("Imp", "A", "killed");
            game.AddAction("B", "A", "reported");
            game.AddTurn();
            game.AddAction("Imp", "C", "voted");
            game.AddAction("B", "C", "voted");
            game.AddAction("C", "Imp", "voted");
            game.AddAction("Lobby", "C", "exiled");
            game.SetWinners(new List<string> { "Impostor" });
            return game;
        }

        [Fact]
        public void VerifyHostGame_PassesOnExpectedGames()
        {
            Assert.Empty(ScenarioVerifier.VerifyHostGame(ExpectedGameOne(), Plan(1)));
            Assert.Empty(ScenarioVerifier.VerifyHostGame(ExpectedGameTwo(), Plan(2)));
        }

        [Fact]
        public void VerifyGameOne_ReportsMissingSabotageAndEmergency()
        {
            GLGame game = ExpectedGameOne();
            game.actions.RemoveAll(a => a.action == "SAB_END_Lights" || a.action == "called an emergency");

            List<string> failures = ScenarioVerifier.VerifyHostGame(game, Plan(1));

            Assert.Contains(failures, f => f.Contains("SAB_END_Lights"));
            Assert.Contains(failures, f => f.Contains("called an emergency"));
        }

        [Fact]
        public void VerifyGameOne_ReportsTaskCounterMismatch()
        {
            GLGame game = ExpectedGameOne();
            game.players.Find(p => p.playerName == "A").AddTasks();

            List<string> failures = ScenarioVerifier.VerifyHostGame(game, Plan(1));

            Assert.Contains(failures, f => f.Contains("A completed every task"));
            Assert.Contains(failures, f => f.Contains("as a ghost"));
        }

        [Fact]
        public void VerifyGameOne_ReportsUnexpectedExileAndWrongTurn()
        {
            GLGame game = ExpectedGameOne();
            game.AddAction("Lobby", "B", "exiled");
            game.actions.Find(a => a.action == "killed").turn = "1";

            List<string> failures = ScenarioVerifier.VerifyHostGame(game, Plan(1));

            Assert.Contains(failures, f => f.Contains("'exiled'"));
            Assert.Contains(failures, f => f.Contains("expected in turn 2"));
        }

        [Fact]
        public void VerifyGameTwo_ReportsWrongWinnerAndMissingReport()
        {
            GLGame game = ExpectedGameTwo();
            game.actions.RemoveAll(a => a.action == "reported");
            game.winner = "Crewmate";

            List<string> failures = ScenarioVerifier.VerifyHostGame(game, Plan(2));

            Assert.Contains(failures, f => f.Contains("reported"));
            Assert.Contains(failures, f => f.Contains("winner"));
        }

        [Fact]
        public void VerifyHostGame_ReportsMissingGameId()
        {
            GLGame game = ExpectedGameTwo();
            game.id = null;

            Assert.Contains(ScenarioVerifier.VerifyHostGame(game, Plan(2)), f => f.Contains("Game id"));
        }

        [Fact]
        public void VerifyClientGame_RequiresSyncedIdAndPlayers()
        {
            Assert.Empty(ScenarioVerifier.VerifyClientGame(ExpectedGameTwo()));

            var empty = new GLGame("******", "TheSkeld", false, "Vanilla");
            Assert.Equal(2, ScenarioVerifier.VerifyClientGame(empty).Count);
        }

        [Fact]
        public void VerifyDistinctGames_RejectsReusedId()
        {
            Assert.Empty(ScenarioVerifier.VerifyDistinctGames("41", "42"));
            Assert.Empty(ScenarioVerifier.VerifyDistinctGames(null, "42"));
            Assert.Single(ScenarioVerifier.VerifyDistinctGames("41", "41"));
        }
    }
}

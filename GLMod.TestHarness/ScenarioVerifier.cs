using GLMod.Class;
using GLMod.Constants;
using GLMod.Enums;
using GLMod.GLEntities;
using System.Collections.Generic;
using System.Linq;

namespace GLMod.TestHarness
{
    /// <summary>
    /// Checks that the GLGame collected by GLMod matches the scripted scenario.
    /// Pure logic: only depends on GLMod entities.
    /// </summary>
    public static class ScenarioVerifier
    {
        public const string TEAM_IMPOSTOR = "Impostor";
        public const string TEAM_CREWMATE = "Crewmate";
        public const string EXILE_SOURCE = "Lobby";

        // GLMod turn encoding: N during gameplay, 1000 + N during the meeting of turn N
        public const string TURN_FIRST = "1";
        public const string TURN_FIRST_MEETING = "1001";
        public const string TURN_SECOND = "2";

        /// <summary>
        /// Full check of the host's game (the one uploaded to /game/end). Returns the list of failures.
        /// </summary>
        public static List<string> VerifyHostGame(GLGame game, ScenarioPlan plan)
        {
            var failures = new List<string>();
            if (game == null)
            {
                failures.Add("GLMod CurrentGame is null at game end");
                return failures;
            }

            if (string.IsNullOrEmpty(game.id))
            {
                failures.Add("Game id is empty: /game/start did not return an id");
            }

            if (!VerifyPlayers(game, plan, failures)) return failures;

            if (plan.GameNumber == ScenarioPlan.GAME_CREWMATES_WIN_BY_TASKS)
            {
                VerifyGameOne(game, plan, failures);
            }
            else
            {
                VerifyGameTwo(game, plan, failures);
            }
            return failures;
        }

        /// <summary>
        /// Check of a non-host game: it must have received the game id through RPC 1
        /// and know the 4 players. Non-hosts do not upload the end of the game.
        /// </summary>
        public static List<string> VerifyClientGame(GLGame game)
        {
            var failures = new List<string>();
            if (game == null)
            {
                failures.Add("GLMod CurrentGame is null at game end");
                return failures;
            }

            if (string.IsNullOrEmpty(game.id))
            {
                failures.Add("Game id is empty: RPC 1 (game id sync) was not received");
            }

            int playerCount = game.players?.Count ?? 0;
            if (playerCount != HarnessConstants.PLAYER_COUNT)
            {
                failures.Add($"Expected {HarnessConstants.PLAYER_COUNT} players, got {playerCount}");
            }

            return failures;
        }

        /// <summary>
        /// After a return to the lobby, the second match must be a new GLMod game.
        /// </summary>
        public static List<string> VerifyDistinctGames(string firstGameId, string secondGameId)
        {
            var failures = new List<string>();
            if (!string.IsNullOrEmpty(firstGameId) && firstGameId == secondGameId)
            {
                failures.Add($"Game 2 reused the id of game 1 ({firstGameId}): state was not reset after returning to the lobby");
            }
            return failures;
        }

        /// <summary>
        /// The end screen must have recorded the game in the local history (Games tab of the menu),
        /// with the recap link of the API's site and the right outcome for the local player.
        /// </summary>
        public static List<string> VerifyHistoryEntry(GLGameHistoryEntry entry, GLGame game, int gameNumber, bool localIsImpostor, string apiEndpoint)
        {
            var failures = new List<string>();
            if (game == null || string.IsNullOrEmpty(game.id))
            {
                return failures;
            }

            if (entry == null || entry.gameId != game.id)
            {
                failures.Add($"Game {game.id} is not the latest game of the local history (latest: {entry?.gameId ?? "none"})");
                return failures;
            }

            string expectedRecap = ApiEndpointResolver.GameRecapUrl(apiEndpoint, game.id);
            if (entry.recapUrl != expectedRecap)
            {
                failures.Add($"History recap link is '{entry.recapUrl}', expected '{expectedRecap}'");
            }

            if (entry.map != game.map)
            {
                failures.Add($"History map is '{entry.map}', expected '{game.map}'");
            }

            bool impostorsWin = gameNumber == ScenarioPlan.GAME_IMPOSTOR_WINS_BY_VOTE;
            bool expectedWon = localIsImpostor == impostorsWin;
            if (entry.won != expectedWon)
            {
                failures.Add($"History says {(entry.won ? "victory" : "defeat")}, expected {(expectedWon ? "victory" : "defeat")}");
            }

            string expectedTeam = localIsImpostor ? TEAM_IMPOSTOR : TEAM_CREWMATE;
            if (entry.team != expectedTeam)
            {
                failures.Add($"History team is '{entry.team}', expected '{expectedTeam}'");
            }
            return failures;
        }

        // Game 1: lights sabotage, emergency meeting with a tie, kill, dead tasks, crewmates win by tasks
        private static void VerifyGameOne(GLGame game, ScenarioPlan plan, List<string> failures)
        {
            List<GLAction> actions = game.actions ?? new List<GLAction>();
            string sabotageStart = GameConstants.ACTION_PREFIX_SAB_START + SabotageType.Lights.ToActionString();
            string sabotageEnd = GameConstants.ACTION_PREFIX_SAB_END + SabotageType.Lights.ToActionString();

            ExpectAction(actions, plan.Tasker.Name, "", "completedTask", TURN_FIRST, failures);
            ExpectAction(actions, "", "", sabotageStart, TURN_FIRST, failures);
            ExpectAction(actions, "", "", sabotageEnd, TURN_FIRST, failures);
            ExpectAction(actions, plan.Caller.Name, "", "called an emergency", TURN_FIRST, failures);

            ExpectAction(actions, plan.Impostor.Name, plan.Tasker.Name, "voted", TURN_FIRST_MEETING, failures);
            ExpectAction(actions, plan.Victim.Name, plan.Tasker.Name, "voted", TURN_FIRST_MEETING, failures);
            ExpectAction(actions, plan.Tasker.Name, "", "voted", TURN_FIRST_MEETING, failures);
            ExpectAction(actions, plan.Caller.Name, "", "voted", TURN_FIRST_MEETING, failures);
            ExpectCount(actions, "voted", 4, failures);
            ExpectCount(actions, "exiled", 0, failures);

            ExpectAction(actions, plan.Impostor.Name, plan.Victim.Name, "killed", TURN_SECOND, failures);
            ExpectCount(actions, "reported", 0, failures);

            if (game.turns != TURN_SECOND)
            {
                failures.Add($"Expected turns '{TURN_SECOND}' after one meeting, got '{game.turns}'");
            }

            // Victim played every task as a ghost, the two others alive
            ExpectTasks(game, actions, plan.Victim, expectAllDead: true, failures);
            ExpectTasks(game, actions, plan.Tasker, expectAllDead: false, failures);
            ExpectTasks(game, actions, plan.Caller, expectAllDead: false, failures);

            ExpectWinner(game, plan, TEAM_CREWMATE, failures);
        }

        // Game 2: kill, report by a crewmate, a crewmate exiled, impostor wins by vote
        private static void VerifyGameTwo(GLGame game, ScenarioPlan plan, List<string> failures)
        {
            List<GLAction> actions = game.actions ?? new List<GLAction>();

            ExpectAction(actions, plan.Impostor.Name, plan.Victim.Name, "killed", TURN_FIRST, failures);
            ExpectAction(actions, plan.Reporter.Name, plan.Victim.Name, "reported", TURN_FIRST, failures);
            ExpectAction(actions, plan.Impostor.Name, plan.ExiledCrewmate.Name, "voted", TURN_FIRST_MEETING, failures);
            ExpectAction(actions, plan.Reporter.Name, plan.ExiledCrewmate.Name, "voted", TURN_FIRST_MEETING, failures);
            ExpectAction(actions, plan.ExiledCrewmate.Name, plan.Impostor.Name, "voted", TURN_FIRST_MEETING, failures);
            ExpectCount(actions, "voted", 3, failures);
            ExpectAction(actions, EXILE_SOURCE, plan.ExiledCrewmate.Name, "exiled", null, failures);
            ExpectCount(actions, "called an emergency", 0, failures);

            ExpectWinner(game, plan, TEAM_IMPOSTOR, failures);
        }

        private static bool VerifyPlayers(GLGame game, ScenarioPlan plan, List<string> failures)
        {
            int playerCount = game.players?.Count ?? 0;
            if (playerCount != HarnessConstants.PLAYER_COUNT)
            {
                failures.Add($"Expected {HarnessConstants.PLAYER_COUNT} players, got {playerCount}");
                return false;
            }

            ExpectTeam(game, plan.Impostor, TEAM_IMPOSTOR, failures);
            foreach (ScenarioPlayer crewmate in plan.Crewmates)
            {
                ExpectTeam(game, crewmate, TEAM_CREWMATE, failures);
            }
            return true;
        }

        private static void ExpectTasks(GLGame game, List<GLAction> actions, ScenarioPlayer expected, bool expectAllDead, List<string> failures)
        {
            GLPlayer player = FindPlayer(game, expected.Name);
            if (player == null) return;

            int tasks = ParseInt(player.tasks);
            int tasksDead = ParseInt(player.tasksDead);
            int tasksMax = ParseInt(player.tasksMax);

            if (tasksMax <= 0)
            {
                failures.Add($"{expected.Name} should have tasksMax > 0, got '{player.tasksMax}'");
                return;
            }

            if (tasks + tasksDead != tasksMax)
            {
                failures.Add($"{expected.Name} completed every task: expected tasks + tasksDead = {tasksMax}, got {tasks} + {tasksDead}");
            }

            if (expectAllDead && tasks != 0)
            {
                failures.Add($"{expected.Name} played its tasks as a ghost: expected tasks = 0 and tasksDead = {tasksMax}, got {tasks} / {tasksDead}");
            }

            if (!expectAllDead && tasksDead != 0)
            {
                failures.Add($"{expected.Name} stayed alive: expected tasksDead = 0, got {tasksDead}");
            }

            int completedActions = actions.Count(a => a.action == "completedTask" && a.source == expected.Name);
            if (completedActions != tasks + tasksDead)
            {
                failures.Add($"{expected.Name}: {completedActions} 'completedTask' action(s) but tasks + tasksDead = {tasks + tasksDead}");
            }
        }

        private static void ExpectWinner(GLGame game, ScenarioPlan plan, string winningTeam, List<string> failures)
        {
            if (game.winner != winningTeam)
            {
                failures.Add($"Expected winner '{winningTeam}', got '{game.winner}'");
            }

            string impostorWin = winningTeam == TEAM_IMPOSTOR ? "1" : "0";
            string crewmateWin = winningTeam == TEAM_CREWMATE ? "1" : "0";

            GLPlayer impostor = FindPlayer(game, plan.Impostor.Name);
            if (impostor != null && impostor.win != impostorWin)
            {
                failures.Add($"Impostor {plan.Impostor.Name} should have win={impostorWin}, got '{impostor.win}'");
            }

            foreach (ScenarioPlayer crewmate in plan.Crewmates)
            {
                GLPlayer player = FindPlayer(game, crewmate.Name);
                if (player != null && player.win != crewmateWin)
                {
                    failures.Add($"Crewmate {crewmate.Name} should have win={crewmateWin}, got '{player.win}'");
                }
            }
        }

        private static GLPlayer FindPlayer(GLGame game, string name)
        {
            return game.players?.FirstOrDefault(p => p.playerName == name);
        }

        private static void ExpectTeam(GLGame game, ScenarioPlayer expected, string team, List<string> failures)
        {
            GLPlayer player = FindPlayer(game, expected.Name);
            if (player == null)
            {
                failures.Add($"Player {expected.Name} missing from the game");
                return;
            }

            if (player.team != team)
            {
                failures.Add($"Player {expected.Name} should be in team '{team}', got '{player.team}'");
            }
        }

        /// <summary>
        /// Expects at least one matching action; when turn is not null, it must also match.
        /// </summary>
        private static void ExpectAction(List<GLAction> actions, string source, string target, string action, string turn, List<string> failures)
        {
            List<GLAction> matches = actions
                .Where(a => a.action == action && (a.source ?? "") == source && (a.target ?? "") == target)
                .ToList();

            if (matches.Count == 0)
            {
                failures.Add($"Missing action: '{source}' {action} '{target}'");
                return;
            }

            if (turn != null && !matches.Any(a => a.turn == turn))
            {
                failures.Add($"Action '{source}' {action} '{target}' expected in turn {turn}, got turn(s) {string.Join(", ", matches.Select(a => a.turn))}");
            }
        }

        private static void ExpectCount(List<GLAction> actions, string action, int expected, List<string> failures)
        {
            int count = actions.Count(a => a.action == action);
            if (count != expected)
            {
                failures.Add($"Expected exactly {expected} '{action}' action(s), got {count}");
            }
        }

        private static int ParseInt(string value)
        {
            return int.TryParse(value, out int result) ? result : -1;
        }
    }
}

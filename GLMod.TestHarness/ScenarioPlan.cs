using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace GLMod.TestHarness
{
    /// <summary>
    /// A player as seen by the harness when the plan is built (pure data, no game types).
    /// </summary>
    public sealed class ScenarioPlayer
    {
        public byte Id { get; set; }
        public string Name { get; set; }
        public bool IsImpostor { get; set; }

        public ScenarioPlayer()
        {
        }

        public ScenarioPlayer(byte id, string name, bool isImpostor)
        {
            Id = id;
            Name = name;
            IsImpostor = isImpostor;
        }
    }

    /// <summary>
    /// Who does what during one scripted match. Built by the host once roles are assigned,
    /// then shared with the clients through the run output folder.
    /// Crewmates are sorted by player id: CrewA, CrewB, CrewC.
    ///
    /// Game 1 — crewmates win by tasks:
    ///   1. Tasker (B) plays one task.
    ///   2. Impostor sabotages the lights; Caller (C) fixes them at the panel.
    ///   3. Caller (C) presses the emergency button.
    ///   4. Votes: Impostor and Victim (A) vote Tasker, Tasker and Caller skip: tie, nobody exiled.
    ///   5. Impostor kills Victim (A).
    ///   6. Every crewmate, including the dead Victim, plays all remaining tasks.
    ///
    /// Game 2 — impostor wins by vote:
    ///   1. Impostor kills Victim (A).
    ///   2. Reporter (B) walks to the body and reports it.
    ///   3. Votes: Impostor and Reporter vote ExiledCrewmate (C), who votes Impostor.
    ///   4. ExiledCrewmate is exiled: 1 impostor vs 1 crewmate.
    ///
    /// Game 3 — movement (its own scenario): every player walks around the map;
    ///   the impostor roams, hunts and kills FirstVictim (A), roams again for a while,
    ///   then hunts and kills SecondVictim (B): 1 impostor vs 1 crewmate, impostor wins by kill.
    /// </summary>
    public sealed class ScenarioPlan
    {
        public const int GAME_CREWMATES_WIN_BY_TASKS = 1;
        public const int GAME_IMPOSTOR_WINS_BY_VOTE = 2;
        public const int GAME_MOVEMENT = 3;

        /// <summary>Marker for "skip" in vote targets (PlayerVoteArea.SkippedVote in game).</summary>
        public const byte SKIP_VOTE = 253;

        public string RunId { get; set; }
        public int GameNumber { get; set; }
        public ScenarioPlayer Impostor { get; set; }
        public ScenarioPlayer CrewA { get; set; }
        public ScenarioPlayer CrewB { get; set; }
        public ScenarioPlayer CrewC { get; set; }

        // Role names used by the scenario steps
        [JsonIgnore] public ScenarioPlayer Victim => CrewA;
        [JsonIgnore] public ScenarioPlayer Tasker => CrewB;
        [JsonIgnore] public ScenarioPlayer Caller => CrewC;
        [JsonIgnore] public ScenarioPlayer Reporter => CrewB;
        [JsonIgnore] public ScenarioPlayer ExiledCrewmate => CrewC;
        [JsonIgnore] public ScenarioPlayer FirstVictim => CrewA;
        [JsonIgnore] public ScenarioPlayer SecondVictim => CrewB;
        [JsonIgnore] public ScenarioPlayer Survivor => CrewC;
        [JsonIgnore] public IEnumerable<ScenarioPlayer> Crewmates => new[] { CrewA, CrewB, CrewC };
        [JsonIgnore] public IEnumerable<ScenarioPlayer> Players => new[] { Impostor, CrewA, CrewB, CrewC };

        /// <summary>
        /// True when the impostor is expected to win the given game.
        /// </summary>
        public static bool ImpostorsWin(int gameNumber)
        {
            return gameNumber == GAME_IMPOSTOR_WINS_BY_VOTE || gameNumber == GAME_MOVEMENT;
        }

        public static ScenarioPlan Build(string runId, int gameNumber, IEnumerable<ScenarioPlayer> players)
        {
            if (players == null) throw new ArgumentNullException(nameof(players));
            if (gameNumber < GAME_CREWMATES_WIN_BY_TASKS || gameNumber > GAME_MOVEMENT)
            {
                throw new ArgumentOutOfRangeException(nameof(gameNumber), $"Game number must be between {GAME_CREWMATES_WIN_BY_TASKS} and {GAME_MOVEMENT}");
            }

            List<ScenarioPlayer> all = players.ToList();
            if (all.Count != HarnessConstants.PLAYER_COUNT)
            {
                throw new InvalidOperationException($"Expected {HarnessConstants.PLAYER_COUNT} players, got {all.Count}");
            }

            if (all.Any(p => string.IsNullOrEmpty(p.Name)))
            {
                throw new InvalidOperationException("Every player must have a name");
            }

            if (all.Select(p => p.Name).Distinct().Count() != all.Count)
            {
                throw new InvalidOperationException("Player names must be unique (GLMod matches players by name)");
            }

            List<ScenarioPlayer> impostors = all.Where(p => p.IsImpostor).ToList();
            if (impostors.Count != 1)
            {
                throw new InvalidOperationException($"Expected exactly 1 impostor, got {impostors.Count}");
            }

            List<ScenarioPlayer> crewmates = all.Where(p => !p.IsImpostor).OrderBy(p => p.Id).ToList();

            return new ScenarioPlan
            {
                RunId = runId,
                GameNumber = gameNumber,
                Impostor = impostors[0],
                CrewA = crewmates[0],
                CrewB = crewmates[1],
                CrewC = crewmates[2]
            };
        }

        /// <summary>
        /// Returns the player id the given voter must vote for (SKIP_VOTE to skip),
        /// or null if the voter does not vote.
        /// </summary>
        public byte? GetVoteTarget(byte voterId)
        {
            if (GameNumber == GAME_CREWMATES_WIN_BY_TASKS)
            {
                if (voterId == Impostor.Id || voterId == Victim.Id) return Tasker.Id;
                if (voterId == Tasker.Id || voterId == Caller.Id) return SKIP_VOTE;
                return null;
            }

            // No meeting in the movement game
            if (GameNumber == GAME_MOVEMENT) return null;

            if (voterId == Impostor.Id || voterId == Reporter.Id) return ExiledCrewmate.Id;
            if (voterId == ExiledCrewmate.Id) return Impostor.Id;
            return null;
        }

        public override string ToString()
        {
            switch (GameNumber)
            {
                case GAME_CREWMATES_WIN_BY_TASKS:
                    return $"game 1: impostor={Impostor.Name}#{Impostor.Id}, victim={Victim.Name}#{Victim.Id}, tasker={Tasker.Name}#{Tasker.Id}, caller={Caller.Name}#{Caller.Id}";
                case GAME_IMPOSTOR_WINS_BY_VOTE:
                    return $"game 2: impostor={Impostor.Name}#{Impostor.Id}, victim={Victim.Name}#{Victim.Id}, reporter={Reporter.Name}#{Reporter.Id}, exiled={ExiledCrewmate.Name}#{ExiledCrewmate.Id}";
                default:
                    return $"game 3 (movement): impostor={Impostor.Name}#{Impostor.Id}, first victim={FirstVictim.Name}#{FirstVictim.Id}, second victim={SecondVictim.Name}#{SecondVictim.Id}, survivor={Survivor.Name}#{Survivor.Id}";
            }
        }
    }
}

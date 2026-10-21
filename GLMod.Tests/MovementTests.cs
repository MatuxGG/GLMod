using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using GLMod.GLEntities;
using GLMod.TestHarness;
using Xunit;

namespace GLMod.Tests
{
    public class NavGridTests
    {
        private const float Cell = 0.5f;

        /// <summary>10 x 10 room with a wall at x = 5 that has a door around y = 8.</summary>
        private static NavGrid RoomWithWall(bool door = true) => new NavGrid(0, 0, 10, 10, Cell,
            (x, y) => !(x > 4.5f && x < 5.5f && !(door && y > 7.5f && y < 9f)));

        [Fact]
        public void Build_CoversTheBoundsAndCountsWalkableCells()
        {
            NavGrid grid = new NavGrid(0, 0, 10, 10, Cell, (x, y) => x < 5f);

            Assert.Equal(20, grid.Width);
            Assert.Equal(20, grid.Height);
            Assert.Equal(200, grid.WalkableCount);
            Assert.True(grid.IsWalkable(new Vector2(1, 1)));
            Assert.False(grid.IsWalkable(new Vector2(7, 1)));
            Assert.False(grid.IsWalkable(new Vector2(-1, 1)));
        }

        [Fact]
        public void FindPath_StraightLineInAnOpenRoom()
        {
            NavGrid grid = new NavGrid(0, 0, 10, 10, Cell, (x, y) => true);

            List<Vector2> path = grid.FindPath(new Vector2(1, 1), new Vector2(9, 6), 1f);

            Assert.Equal(new[] { new Vector2(1, 1), new Vector2(9, 6) }, path);
        }

        [Fact]
        public void FindPath_GoesThroughTheDoor()
        {
            NavGrid grid = RoomWithWall();

            List<Vector2> path = grid.FindPath(new Vector2(1, 1), new Vector2(9, 1), 1f);

            Assert.NotNull(path);
            Assert.Equal(new Vector2(1, 1), path[0]);
            Assert.Equal(new Vector2(9, 1), path[path.Count - 1]);
            Assert.Contains(path, p => p.X > 4f && p.X < 6f && p.Y > 7.5f && p.Y < 9f);
            for (int i = 1; i < path.Count; i++)
            {
                Assert.True(grid.HasLineOfSight(path[i - 1], path[i]));
            }
        }

        [Fact]
        public void FindPath_NullWhenUnreachable()
        {
            Assert.Null(RoomWithWall(door: false).FindPath(new Vector2(1, 1), new Vector2(9, 1), 1f));
        }

        [Fact]
        public void FindPath_SnapsBlockedEndsToTheClosestWalkableCell()
        {
            NavGrid grid = RoomWithWall();

            List<Vector2> path = grid.FindPath(new Vector2(4.9f, 1), new Vector2(1, 1), 1f);

            Assert.NotNull(path);
            Assert.True(grid.IsWalkable(path[0]));
            Assert.True(Vector2.Distance(path[0], new Vector2(4.9f, 1)) <= 1f);
            Assert.Null(grid.NearestWalkable(new Vector2(5f, 1), 0.2f));
        }
    }

    public class PositionAccuracyTests
    {
        private static List<TrackPoint> Line(int count, long intervalMs, float speedPerSample) =>
            Enumerable.Range(0, count).Select(i => new TrackPoint(i * intervalMs, i * speedPerSample, 0f)).ToList();

        [Fact]
        public void Measure_ZeroOnTheSameTrack()
        {
            List<TrackPoint> truth = Line(50, 50, 0.1f);

            AccuracyStats stats = PositionAccuracy.Measure(new[] { truth[0], truth[49] }, truth);

            Assert.Equal(50, stats.Samples);
            Assert.True(stats.MaxDeviation < 1e-4);
        }

        [Fact]
        public void Measure_FindsTheCutCornerAndSkipsExcludedTimes()
        {
            // Walks right 1 s, then up 1 s; the sent track only has both ends
            var truth = new List<TrackPoint>();
            for (int i = 0; i <= 20; i++) truth.Add(new TrackPoint(i * 50, i * 0.125f, 0f));
            for (int i = 1; i <= 20; i++) truth.Add(new TrackPoint(1000 + i * 50, 2.5f, i * 0.125f));
            var sent = new[] { truth[0], truth[truth.Count - 1] };

            AccuracyStats stats = PositionAccuracy.Measure(sent, truth);
            AccuracyStats excluded = PositionAccuracy.Measure(sent, truth, new[] { (500L, 1500L) });

            Assert.InRange(stats.MaxDeviation, 1.7, 1.8);
            Assert.True(excluded.MaxDeviation < stats.MaxDeviation);
            Assert.True(excluded.Samples < stats.Samples);
        }

        [Fact]
        public void Resample_KeepsOnePointPerInterval()
        {
            List<TrackPoint> resampled = PositionAccuracy.Resample(Line(41, 50, 0.1f), 500);

            Assert.Equal(new long[] { 0, 500, 1000, 1500, 2000 }, resampled.Select(p => p.TimeMs).ToArray());
        }

        [Fact]
        public void PathLength_SumsTheSegments()
        {
            Assert.Equal(5.0, PositionAccuracy.PathLength(new[] { new TrackPoint(0, 0, 0), new TrackPoint(10, 3, 4) }), 3);
            Assert.Equal(0.0, PositionAccuracy.PathLength(new TrackPoint[0]));
        }
    }

    public class MovementVerifierTests
    {
        private static ScenarioPlan Plan() => ScenarioPlan.Build("run", ScenarioPlan.GAME_MOVEMENT, new List<ScenarioPlayer>
        {
            new ScenarioPlayer(0, "Imp", true),
            new ScenarioPlayer(1, "A", false),
            new ScenarioPlayer(2, "B", false),
            new ScenarioPlayer(3, "C", false)
        });

        /// <summary>Every player walks 20 units right in 8 s (truth every 50 ms), GLMod sent both ends.</summary>
        private static (GLGame Game, Dictionary<string, List<TrackPoint>> Truth) Walking()
        {
            var game = new GLGame("******", "Polus", false, "Vanilla") { id = "43" };
            var truth = new Dictionary<string, List<TrackPoint>>();
            foreach (string name in new[] { "Imp", "A", "B", "C" })
            {
                game.AddPlayer(null, name, "Vanilla", name == "Imp" ? "Impostor" : "Crewmate", "0");
                var track = new List<TrackPoint>();
                for (int i = 0; i <= 160; i++) track.Add(new TrackPoint(1000000 + i * 50, i * 0.125f, 0f));
                truth[name] = track;
                game.AddPosition(name, 0f, 0f, "1000000", "1");
                game.AddPosition(name, 20f, 0f, "1008000", "1");
            }
            return (game, truth);
        }

        [Fact]
        public void VerifyPositions_PassesAndReportsStats()
        {
            (GLGame game, Dictionary<string, List<TrackPoint>> truth) = Walking();
            var stats = new List<PlayerMovementStats>();

            List<string> failures = ScenarioVerifier.VerifyPositions(game, Plan(), truth, stats);

            Assert.Empty(failures);
            Assert.Equal(4, stats.Count);
            PlayerMovementStats imp = stats.First(s => s.Player == "Imp");
            Assert.Equal(2, imp.PointsSent);
            Assert.Equal(81, imp.Points10Hz);
            Assert.Equal(17, imp.PointsLegacy);
            Assert.Equal(20.0, imp.PathLength, 3);
            Assert.Equal(161, imp.Sent.Samples);
        }

        [Fact]
        public void VerifyPositions_ReportsAPlayerDrawnFarFromItsRealPosition()
        {
            (GLGame game, Dictionary<string, List<TrackPoint>> truth) = Walking();
            // A really stopped halfway for 4 s, which the 2 sent positions do not show
            truth["A"] = truth["A"].Select(p => p.TimeMs <= 1004000 ? p : new TrackPoint(p.TimeMs, Math.Max(10f, p.X - 10f), 0f)).ToList();

            List<string> failures = ScenarioVerifier.VerifyPositions(game, Plan(), truth, null);

            Assert.Single(failures);
            Assert.Contains("A: the recap would draw", failures[0]);
        }

        [Fact]
        public void VerifyPositions_IgnoresTheKillJump()
        {
            (GLGame game, Dictionary<string, List<TrackPoint>> truth) = Walking();
            // The impostor jumps 2 units at the kill, between two samples
            truth["Imp"] = truth["Imp"].Select(p => p.TimeMs == 1004000 ? new TrackPoint(p.TimeMs, p.X, 2f) : p).ToList();
            game.AddAction("Imp", "A", "killed");
            game.actions[0].triggerTimeMs = "1004000";

            Assert.Empty(ScenarioVerifier.VerifyPositions(game, Plan(), truth, null));
        }

        [Fact]
        public void VerifyPositions_ReportsMissingPositionsAndNoMovement()
        {
            (GLGame game, Dictionary<string, List<TrackPoint>> truth) = Walking();
            game.players.Find(p => p.playerName == "B").positions.Clear();
            GLPlayer c = game.players.Find(p => p.playerName == "C");
            c.positions.Clear();
            c.AddPosition(0f, 0f, "1000000", "1");
            c.AddPosition(1f, 0f, "1008000", "1");
            truth["C"] = truth["C"].Select(p => new TrackPoint(p.TimeMs, p.X / 20f, 0f)).ToList();

            List<string> failures = ScenarioVerifier.VerifyPositions(game, Plan(), truth, null);

            Assert.Contains(failures, f => f.StartsWith("B: expected positions"));
            Assert.Contains(failures, f => f.StartsWith("C barely moved"));
        }
    }
}

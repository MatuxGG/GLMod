using System;
using System.Collections.Generic;
using System.Linq;
using GLMod.Class;
using GLMod.TestHarness;
using Xunit;

namespace GLMod.Tests
{
    public class PositionSimplifierTests
    {
        private const float Tolerance = 0.1f;
        private const long MaxGap = 5000;
        private const long Interval = 100;

        private static PositionSimplifier.Sample At(long timeMs, float x, float y) => new PositionSimplifier.Sample(x, y, timeMs, "1");

        /// <summary>Feeds every sample then flushes; returns what would be sent.</summary>
        private static List<PositionSimplifier.Sample> Simplify(IEnumerable<PositionSimplifier.Sample> samples)
        {
            var simplifier = new PositionSimplifier(Tolerance, MaxGap);
            var kept = new List<PositionSimplifier.Sample>();
            foreach (PositionSimplifier.Sample sample in samples)
            {
                simplifier.Add(sample, kept);
            }
            simplifier.Flush(kept);
            return kept;
        }

        private static List<TrackPoint> Track(IEnumerable<PositionSimplifier.Sample> samples) =>
            samples.Select(s => new TrackPoint(s.TimeMs, s.X, s.Y)).ToList();

        [Fact]
        public void StraightWalk_KeepsOnlyBothEnds()
        {
            List<PositionSimplifier.Sample> walk = Enumerable.Range(0, 40).Select(i => At(i * Interval, i * 0.25f, 3f)).ToList();

            List<PositionSimplifier.Sample> kept = Simplify(walk);

            Assert.Equal(2, kept.Count);
            Assert.Equal(0, kept[0].TimeMs);
            Assert.Equal(39 * Interval, kept[1].TimeMs);
        }

        [Fact]
        public void StandingStill_KeepsOnePositionPerMaxGap()
        {
            List<PositionSimplifier.Sample> still = Enumerable.Range(0, 200).Select(i => At(i * Interval, 5f, 5f)).ToList();

            List<PositionSimplifier.Sample> kept = Simplify(still);

            Assert.Equal(new long[] { 0, 5000, 10000, 15000, 19900 }, kept.Select(s => s.TimeMs).ToArray());
        }

        [Fact]
        public void Turn_KeepsTheCorner()
        {
            var walk = new List<PositionSimplifier.Sample>();
            for (int i = 0; i <= 20; i++) walk.Add(At(i * Interval, i * 0.25f, 0f));
            for (int i = 1; i <= 20; i++) walk.Add(At((20 + i) * Interval, 5f, i * 0.25f));

            List<PositionSimplifier.Sample> kept = Simplify(walk);

            Assert.Equal(3, kept.Count);
            Assert.Equal(20 * Interval, kept[1].TimeMs);
            Assert.Equal(5f, kept[1].X);
            Assert.Equal(0f, kept[1].Y);
        }

        [Fact]
        public void StopThenWalk_KeepsTheLastStillPosition()
        {
            // Standing 2 s then walking: interpolating from the start would show a slow drift
            var samples = new List<PositionSimplifier.Sample>();
            for (int i = 0; i <= 20; i++) samples.Add(At(i * Interval, 0f, 0f));
            for (int i = 1; i <= 20; i++) samples.Add(At((20 + i) * Interval, i * 0.25f, 0f));

            List<PositionSimplifier.Sample> kept = Simplify(samples);

            Assert.Contains(kept, s => s.TimeMs == 20 * Interval && s.X == 0f);
        }

        [Fact]
        public void Teleport_KeepsBothSides()
        {
            var samples = new List<PositionSimplifier.Sample>();
            for (int i = 0; i <= 10; i++) samples.Add(At(i * Interval, 1f, 1f));
            for (int i = 11; i <= 20; i++) samples.Add(At(i * Interval, 20f, -8f));

            List<PositionSimplifier.Sample> kept = Simplify(samples);

            Assert.Contains(kept, s => s.TimeMs == 10 * Interval && s.X == 1f);
            Assert.Contains(kept, s => s.TimeMs == 11 * Interval && s.X == 20f);
        }

        [Fact]
        public void RandomWalk_EverySampleStaysWithinTolerance()
        {
            var random = new Random(42);
            var samples = new List<PositionSimplifier.Sample>();
            float x = 0, y = 0, angle = 0;
            for (int i = 0; i < 3000; i++)
            {
                // Walks (0.25 per sample) with turns and stops, like a player
                if (random.NextDouble() < 0.05) angle = (float)(random.NextDouble() * Math.PI * 2);
                bool moving = (i / 50) % 4 != 3;
                if (moving)
                {
                    x += 0.25f * (float)Math.Cos(angle);
                    y += 0.25f * (float)Math.Sin(angle);
                }
                samples.Add(At(i * Interval, x, y));
            }

            List<PositionSimplifier.Sample> kept = Simplify(samples);
            AccuracyStats accuracy = PositionAccuracy.Measure(Track(kept), Track(samples));

            Assert.Equal(samples.Count, accuracy.Samples);
            Assert.True(accuracy.MaxDeviation <= Tolerance + 1e-4, $"max deviation {accuracy.MaxDeviation}");
            Assert.True(kept.Count < samples.Count / 3, $"{kept.Count} of {samples.Count} kept");
            for (int i = 1; i < kept.Count; i++)
            {
                Assert.True(kept[i].TimeMs - kept[i - 1].TimeMs <= MaxGap);
            }
        }

        [Fact]
        public void IgnoresSamplesThatAreNotLater()
        {
            List<PositionSimplifier.Sample> kept = Simplify(new[] { At(100, 0f, 0f), At(100, 9f, 9f), At(50, 9f, 9f), At(200, 0f, 0f) });

            Assert.Equal(new long[] { 100, 200 }, kept.Select(s => s.TimeMs).ToArray());
        }

        [Fact]
        public void Flush_SendsThePendingSampleOnceAndStartsANewTrack()
        {
            var simplifier = new PositionSimplifier(Tolerance, MaxGap);
            var kept = new List<PositionSimplifier.Sample>();
            simplifier.Add(At(0, 0f, 0f), kept);
            simplifier.Add(At(100, 0f, 0f), kept);
            simplifier.Flush(kept);
            simplifier.Flush(kept);
            simplifier.Add(At(30000, 4f, 4f), kept);

            Assert.Equal(new long[] { 0, 100, 30000 }, kept.Select(s => s.TimeMs).ToArray());
        }

        [Fact]
        public void Restart_ContinuesFromAKeyPosition()
        {
            var simplifier = new PositionSimplifier(Tolerance, MaxGap);
            var kept = new List<PositionSimplifier.Sample>();
            simplifier.Add(At(0, 0f, 0f), kept);
            simplifier.Add(At(100, 0.25f, 0f), kept);

            // A kill position recorded by the patch, after the pending samples
            simplifier.Flush(kept);
            PositionSimplifier.Sample kill = At(150, 0.3f, 0f);
            kept.Add(kill);
            simplifier.Restart(kill);
            simplifier.Add(At(200, 0.3f, 0f), kept);
            simplifier.Flush(kept);

            Assert.Equal(new long[] { 0, 100, 150, 200 }, kept.Select(s => s.TimeMs).ToArray());
        }
    }
}

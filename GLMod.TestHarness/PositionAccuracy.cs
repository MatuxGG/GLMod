using System;
using System.Collections.Generic;
using System.Linq;

namespace GLMod.TestHarness
{
    /// <summary>
    /// A player position at a time (ms, GLMod ServerClock).
    /// </summary>
    public readonly struct TrackPoint
    {
        public long TimeMs { get; }
        public float X { get; }
        public float Y { get; }

        public TrackPoint(long timeMs, float x, float y)
        {
            TimeMs = timeMs;
            X = x;
            Y = y;
        }
    }

    /// <summary>
    /// Distance between where a player really was and where the recap draws it.
    /// </summary>
    public sealed class AccuracyStats
    {
        public int Samples { get; set; }
        public double MaxDeviation { get; set; }
        public double MeanDeviation { get; set; }
    }

    /// <summary>
    /// Measures how faithfully a list of sent positions describes a real trajectory, the way the
    /// site draws it: linear interpolation in time between two sent positions.
    /// Pure logic.
    /// </summary>
    public static class PositionAccuracy
    {
        /// <summary>
        /// Compares every real position (truth) with the interpolation of the sent positions at the
        /// same time. Real positions outside the sent time range or inside an excluded window are skipped.
        /// </summary>
        public static AccuracyStats Measure(IEnumerable<TrackPoint> sent, IEnumerable<TrackPoint> truth, IEnumerable<(long From, long To)> excluded = null)
        {
            List<TrackPoint> track = sent.OrderBy(p => p.TimeMs).ToList();
            List<(long From, long To)> windows = excluded?.ToList() ?? new List<(long From, long To)>();
            var stats = new AccuracyStats();
            if (track.Count == 0) return stats;

            double total = 0;
            foreach (TrackPoint real in truth)
            {
                if (real.TimeMs < track[0].TimeMs || real.TimeMs > track[track.Count - 1].TimeMs) continue;
                if (windows.Any(w => real.TimeMs >= w.From && real.TimeMs <= w.To)) continue;

                (float x, float y) = Interpolate(track, real.TimeMs);
                double deviation = Math.Sqrt((x - real.X) * (x - real.X) + (y - real.Y) * (y - real.Y));
                stats.Samples++;
                total += deviation;
                stats.MaxDeviation = Math.Max(stats.MaxDeviation, deviation);
            }

            stats.MeanDeviation = stats.Samples > 0 ? total / stats.Samples : 0;
            return stats;
        }

        /// <summary>
        /// What a fixed-interval sampler would have sent: the first position of each interval.
        /// </summary>
        public static List<TrackPoint> Resample(IEnumerable<TrackPoint> truth, long intervalMs)
        {
            var result = new List<TrackPoint>();
            long next = long.MinValue;
            foreach (TrackPoint point in truth.OrderBy(p => p.TimeMs))
            {
                if (point.TimeMs < next) continue;
                result.Add(point);
                next = point.TimeMs + intervalMs;
            }
            return result;
        }

        /// <summary>
        /// Length of the polyline through the positions, in time order (map units).
        /// </summary>
        public static double PathLength(IEnumerable<TrackPoint> points)
        {
            List<TrackPoint> ordered = points.OrderBy(p => p.TimeMs).ToList();
            double length = 0;
            for (int i = 1; i < ordered.Count; i++)
            {
                double dx = ordered[i].X - ordered[i - 1].X;
                double dy = ordered[i].Y - ordered[i - 1].Y;
                length += Math.Sqrt(dx * dx + dy * dy);
            }
            return length;
        }

        /// <summary>
        /// Position at a time inside the track (sorted by time).
        /// </summary>
        public static (float X, float Y) Interpolate(IReadOnlyList<TrackPoint> track, long timeMs)
        {
            int low = 0;
            int high = track.Count - 1;
            while (high - low > 1)
            {
                int middle = (low + high) / 2;
                if (track[middle].TimeMs <= timeMs) low = middle; else high = middle;
            }

            TrackPoint a = track[low];
            TrackPoint b = track[high];
            if (b.TimeMs <= a.TimeMs || timeMs <= a.TimeMs) return (a.X, a.Y);
            if (timeMs >= b.TimeMs) return (b.X, b.Y);

            float ratio = (float)(timeMs - a.TimeMs) / (b.TimeMs - a.TimeMs);
            return (a.X + (b.X - a.X) * ratio, a.Y + (b.Y - a.Y) * ratio);
        }
    }
}

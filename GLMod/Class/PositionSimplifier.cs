using System.Collections.Generic;

namespace GLMod.Class
{
    /// <summary>
    /// Online simplification of one player's sampled positions (opening window algorithm).
    /// A sample is dropped when the site, which draws a player by linear interpolation in time
    /// between two sent positions, would still place the player within the tolerance of where
    /// it really was at that sample's time. Standing still or walking straight costs one
    /// position per max gap; turns and teleports keep their samples.
    /// Pure logic, no Unity dependency.
    /// </summary>
    public sealed class PositionSimplifier
    {
        /// <summary>
        /// One sampled position.
        /// </summary>
        public readonly struct Sample
        {
            public float X { get; }
            public float Y { get; }
            public long TimeMs { get; }
            public string Turn { get; }

            public Sample(float x, float y, long timeMs, string turn)
            {
                X = x;
                Y = y;
                TimeMs = timeMs;
                Turn = turn;
            }
        }

        private readonly float _toleranceSquared;
        private readonly long _maxGapMs;
        private readonly List<Sample> _pending = new List<Sample>();
        private bool _hasAnchor;
        private Sample _anchor;

        public PositionSimplifier(float tolerance, long maxGapMs)
        {
            _toleranceSquared = tolerance * tolerance;
            _maxGapMs = maxGapMs;
        }

        /// <summary>
        /// Adds a sample; the samples to send (in time order) are appended to kept.
        /// Samples that are not later than the previous one are ignored.
        /// </summary>
        public void Add(Sample sample, List<Sample> kept)
        {
            if (!_hasAnchor)
            {
                Keep(sample, kept);
                return;
            }

            long lastTime = _pending.Count > 0 ? _pending[_pending.Count - 1].TimeMs : _anchor.TimeMs;
            if (sample.TimeMs <= lastTime) return;

            if (Fits(sample))
            {
                _pending.Add(sample);
                return;
            }

            if (_pending.Count == 0)
            {
                // Only the max gap can refuse a sample right after the anchor
                Keep(sample, kept);
                return;
            }

            // The previous sample becomes the new anchor; the new one starts the next window
            Keep(_pending[_pending.Count - 1], kept);
            if (Fits(sample))
            {
                _pending.Add(sample);
            }
            else
            {
                Keep(sample, kept);
            }
        }

        /// <summary>
        /// Sends the last pending sample (meeting, end of game...). The next sample starts a new track.
        /// </summary>
        public void Flush(List<Sample> kept)
        {
            if (_pending.Count > 0)
            {
                kept.Add(_pending[_pending.Count - 1]);
            }
            _pending.Clear();
            _hasAnchor = false;
        }

        /// <summary>
        /// Continues the track from a position sent by someone else (e.g. the position of a kill).
        /// Call Flush first so no pending sample is lost.
        /// </summary>
        public void Restart(Sample anchor)
        {
            _pending.Clear();
            _anchor = anchor;
            _hasAnchor = true;
        }

        private void Keep(Sample sample, List<Sample> kept)
        {
            kept.Add(sample);
            _pending.Clear();
            _anchor = sample;
            _hasAnchor = true;
        }

        /// <summary>
        /// True when the segment anchor -> end keeps every pending sample within the tolerance
        /// (distance to the interpolated position at the same time) and does not exceed the max gap.
        /// </summary>
        private bool Fits(Sample end)
        {
            long duration = end.TimeMs - _anchor.TimeMs;
            if (duration <= 0 || duration > _maxGapMs) return false;

            foreach (Sample sample in _pending)
            {
                float ratio = (float)(sample.TimeMs - _anchor.TimeMs) / duration;
                float dx = _anchor.X + (end.X - _anchor.X) * ratio - sample.X;
                float dy = _anchor.Y + (end.Y - _anchor.Y) * ratio - sample.Y;
                if (dx * dx + dy * dy > _toleranceSquared) return false;
            }
            return true;
        }
    }
}

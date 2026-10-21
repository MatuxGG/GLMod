using System.Collections.Generic;
using GLMod.Constants;
using GLMod.GLEntities;
using UnityEngine;

namespace GLMod.Class
{
    /// <summary>
    /// Records the player positions of the current game: samples go through one
    /// PositionSimplifier per player, positions of game events (kill, report, meeting)
    /// are always recorded. Main thread only.
    /// </summary>
    public static class PositionRecorder
    {
        private static readonly Dictionary<string, PositionSimplifier> Tracks = new Dictionary<string, PositionSimplifier>();
        private static readonly List<PositionSimplifier.Sample> Kept = new List<PositionSimplifier.Sample>();

        /// <summary>
        /// Forgets the tracks of the previous game.
        /// </summary>
        public static void Reset()
        {
            Tracks.Clear();
        }

        /// <summary>
        /// Samples the position of every alive player.
        /// </summary>
        public static void SampleAlivePlayers(GLGame game)
        {
            if (game == null) return;

            long timestamp = ServerClock.NowMs();
            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                if (player?.Data == null || player.Data.IsDead) continue;

                Vector3 position = player.MyPhysics.body.transform.position;
                string name = player.Data.PlayerName;
                Track(name).Add(new PositionSimplifier.Sample(position.x, position.y, timestamp, game.turns), Kept);
                Write(game, name);
            }
        }

        /// <summary>
        /// Records the position of a player at a game event, after its pending samples.
        /// </summary>
        public static void AddKeyPosition(GLGame game, PlayerControl player, long timestampMs)
        {
            if (game == null || player?.Data == null) return;

            Vector3 position = player.MyPhysics.body.transform.position;
            string name = player.Data.PlayerName;
            var sample = new PositionSimplifier.Sample(position.x, position.y, timestampMs, game.turns);

            PositionSimplifier track = Track(name);
            track.Flush(Kept);
            Kept.Add(sample);
            Write(game, name);
            track.Restart(sample);
        }

        /// <summary>
        /// Sends the pending samples of every player (meeting start, end of game).
        /// </summary>
        public static void FlushAll(GLGame game)
        {
            if (game == null) return;

            foreach (KeyValuePair<string, PositionSimplifier> track in Tracks)
            {
                track.Value.Flush(Kept);
                Write(game, track.Key);
            }
        }

        private static PositionSimplifier Track(string playerName)
        {
            if (!Tracks.TryGetValue(playerName, out PositionSimplifier track))
            {
                track = new PositionSimplifier(GameConstants.POSITION_SIMPLIFY_TOLERANCE, GameConstants.POSITION_MAX_GAP_MS);
                Tracks[playerName] = track;
            }
            return track;
        }

        private static void Write(GLGame game, string playerName)
        {
            foreach (PositionSimplifier.Sample sample in Kept)
            {
                game.AddPosition(playerName, sample.X, sample.Y, sample.TimeMs.ToString(), sample.Turn);
            }
            Kept.Clear();
        }
    }
}

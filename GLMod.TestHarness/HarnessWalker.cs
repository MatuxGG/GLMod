using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NVector2 = System.Numerics.Vector2;

namespace GLMod.TestHarness
{
    /// <summary>
    /// Walks the local player for real: a path is computed on a NavGrid built from the ship
    /// colliders, then the PlayerPhysics.FixedUpdate patch applies Direction as joystick input,
    /// so the player moves with its normal speed, collisions and network sync.
    /// </summary>
    public static class HarnessWalker
    {
        public enum WalkResult
        {
            Reached,
            Stopped,
            NoPath,
            Stuck,
            Timeout
        }

        private sealed class WalkState
        {
            public List<NVector2> Path;
            public int Index;
            public float NextPlan;
            public NVector2 LastProgress;
            public float LastProgressTime;
            public int StuckReplans;
        }

        private static readonly List<NVector2> Destinations = new List<NVector2>();

        /// <summary>Normalized direction the local player walks in, or null to leave the game input alone.</summary>
        public static Vector2? Direction { get; private set; }

        public static NavGrid Grid { get; private set; }

        public static WalkResult LastResult { get; private set; }

        /// <summary>
        /// Builds the grid of the current map: a cell is walkable when no solid ship or object
        /// collider is within the player clearance. Destinations are the consoles of the map.
        /// </summary>
        public static void BuildGrid()
        {
            // global:: because the GLMod.Constants namespace would hide the game's Constants class
            int mask = global::Constants.ShipAndObjectsMask;
            Bounds? bounds = null;
            foreach (Collider2D collider in ShipStatus.Instance.GetComponentsInChildren<Collider2D>(true))
            {
                if (collider == null || collider.isTrigger || ((1 << collider.gameObject.layer) & mask) == 0) continue;
                if (bounds == null)
                {
                    bounds = collider.bounds;
                }
                else
                {
                    Bounds union = bounds.Value;
                    union.Encapsulate(collider.bounds);
                    bounds = union;
                }
            }
            if (bounds == null) throw new InvalidOperationException("no solid collider found on the map");

            float margin = HarnessConstants.NAV_BOUNDS_MARGIN;
            Vector3 min = bounds.Value.min;
            Vector3 max = bounds.Value.max;
            long cells = (long)Math.Ceiling((max.x - min.x + 2 * margin) / HarnessConstants.NAV_CELL_SIZE)
                * (long)Math.Ceiling((max.y - min.y + 2 * margin) / HarnessConstants.NAV_CELL_SIZE);
            if (cells > HarnessConstants.NAV_MAX_CELLS)
            {
                throw new InvalidOperationException($"map bounds too large for the navigation grid ({min} - {max}, {cells} cells)");
            }

            float started = Time.realtimeSinceStartup;
            Grid = new NavGrid(min.x - margin, min.y - margin, max.x + margin, max.y + margin, HarnessConstants.NAV_CELL_SIZE,
                (x, y) => IsFree(x, y, mask));

            Destinations.Clear();
            foreach (Console console in ShipStatus.Instance.AllConsoles)
            {
                if (console == null) continue;
                Vector3 position = console.transform.position;
                NVector2? walkable = Grid.NearestWalkable(new NVector2(position.x, position.y), HarnessConstants.NAV_NEAREST_SEARCH);
                if (walkable.HasValue) Destinations.Add(walkable.Value);
            }
            if (Destinations.Count == 0) throw new InvalidOperationException("no walkable destination near the consoles");

            HarnessLog.Info($"Navigation grid: {Grid.Width}x{Grid.Height} cells, {Grid.WalkableCount} walkable, "
                + $"{Destinations.Count} destinations, built in {Time.realtimeSinceStartup - started:0.00}s");
        }

        /// <summary>
        /// Walks towards goal() (re-evaluated every replanInterval seconds, never when 0) until it is
        /// reached, stopWhen() is true or the timeout expires. The outcome is in LastResult.
        /// </summary>
        public static IEnumerator Walk(Func<NVector2?> goal, Func<bool> stopWhen, float timeout, float replanInterval)
        {
            var state = new WalkState { LastProgressTime = Time.realtimeSinceStartup, LastProgress = new NVector2(float.NaN, float.NaN) };
            float deadline = Time.realtimeSinceStartup + timeout;
            while (true)
            {
                WalkResult? result = Step(state, goal, stopWhen, deadline, replanInterval);
                if (result.HasValue)
                {
                    LastResult = result.Value;
                    break;
                }
                yield return null;
            }
            Direction = null;
        }

        /// <summary>
        /// Walks from console to console, with a short pause at each one, until stopWhen() is true
        /// or the local player is dead.
        /// </summary>
        public static IEnumerator Roam(System.Random random, Func<bool> stopWhen)
        {
            while (!SafeEvaluate(stopWhen))
            {
                NVector2 destination = Destinations[random.Next(Destinations.Count)];
                yield return Walk(() => destination, stopWhen, HarnessConstants.MOVEMENT_WALK_TIMEOUT, 0f);
                if (LastResult == WalkResult.Stopped) yield break;
                if (LastResult != WalkResult.Reached)
                {
                    HarnessLog.Info($"  roaming: {LastResult} towards ({destination.X:0.0}, {destination.Y:0.0}), next destination");
                    yield return null;
                    continue;
                }

                float pause = HarnessConstants.MOVEMENT_PAUSE_MIN
                    + (float)random.NextDouble() * (HarnessConstants.MOVEMENT_PAUSE_MAX - HarnessConstants.MOVEMENT_PAUSE_MIN);
                float pauseEnd = Time.realtimeSinceStartup + pause;
                while (Time.realtimeSinceStartup < pauseEnd && !SafeEvaluate(stopWhen))
                {
                    yield return null;
                }
            }
        }

        public static NVector2 ToNumerics(Vector2 vector)
        {
            return new NVector2(vector.x, vector.y);
        }

        private static WalkResult? Step(WalkState state, Func<NVector2?> goal, Func<bool> stopWhen, float deadline, float replanInterval)
        {
            try
            {
                if (stopWhen()) return WalkResult.Stopped;

                float now = Time.realtimeSinceStartup;
                if (now > deadline) return WalkResult.Timeout;

                PlayerControl me = PlayerControl.LocalPlayer;
                if (me == null || me.Data == null || me.Data.IsDead) return WalkResult.Stopped;

                if (!me.CanMove)
                {
                    // Kill animation, minigame...: wait without counting it as stuck
                    Direction = null;
                    state.LastProgressTime = now;
                    return null;
                }

                NVector2 position = ToNumerics(me.GetTruePosition());
                if (state.Path == null || now >= state.NextPlan)
                {
                    NVector2? target = goal();
                    if (target == null) return WalkResult.NoPath;

                    state.Path = Grid.FindPath(position, target.Value, HarnessConstants.NAV_NEAREST_SEARCH);
                    if (state.Path == null) return WalkResult.NoPath;
                    state.Index = 0;
                    state.NextPlan = replanInterval > 0 ? now + replanInterval : float.MaxValue;
                }

                while (state.Index < state.Path.Count
                    && NVector2.Distance(position, state.Path[state.Index]) < HarnessConstants.WALK_WAYPOINT_REACHED)
                {
                    state.Index++;
                }
                if (state.Index >= state.Path.Count) return WalkResult.Reached;

                if (!(NVector2.Distance(position, state.LastProgress) <= HarnessConstants.WALK_STUCK_DISTANCE))
                {
                    state.LastProgress = position;
                    state.LastProgressTime = now;
                }
                else if (now - state.LastProgressTime > HarnessConstants.WALK_STUCK_TIMEOUT)
                {
                    if (++state.StuckReplans > HarnessConstants.WALK_MAX_STUCK_REPLANS) return WalkResult.Stuck;
                    state.NextPlan = 0f;
                    state.LastProgressTime = now;
                }

                NVector2 direction = NVector2.Normalize(state.Path[state.Index] - position);
                Direction = new Vector2(direction.X, direction.Y);
                return null;
            }
            catch (Exception e)
            {
                HarnessLog.Warn("Walk step failed: " + e.Message);
                return WalkResult.Stopped;
            }
        }

        private static bool IsFree(float x, float y, int mask)
        {
            foreach (Collider2D hit in Physics2D.OverlapCircleAll(new Vector2(x, y), HarnessConstants.NAV_CLEARANCE, mask))
            {
                if (hit != null && !hit.isTrigger) return false;
            }
            return true;
        }

        private static bool SafeEvaluate(Func<bool> condition)
        {
            try
            {
                return condition();
            }
            catch (Exception)
            {
                return true;
            }
        }
    }
}

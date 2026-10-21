using System;
using System.Collections.Generic;
using System.Numerics;

namespace GLMod.TestHarness
{
    /// <summary>
    /// Walkable grid of a map and A* path finding on it, so the harness can walk players
    /// for real (physics, collisions) instead of teleporting them.
    /// Pure logic: the walkable test is given by the caller (ship colliders in game).
    /// </summary>
    public sealed class NavGrid
    {
        private static readonly int[] NeighborX = { 1, -1, 0, 0, 1, 1, -1, -1 };
        private static readonly int[] NeighborY = { 0, 0, 1, -1, 1, -1, 1, -1 };
        private const float Diagonal = 1.41421356f;

        private readonly bool[] _walkable;

        public float MinX { get; }
        public float MinY { get; }
        public float CellSize { get; }
        public int Width { get; }
        public int Height { get; }
        public int WalkableCount { get; }

        /// <summary>
        /// Builds the grid covering [minX, maxX] x [minY, maxY]; isFree(x, y) tells whether
        /// a player can stand at a cell center.
        /// </summary>
        public NavGrid(float minX, float minY, float maxX, float maxY, float cellSize, Func<float, float, bool> isFree)
        {
            if (cellSize <= 0) throw new ArgumentOutOfRangeException(nameof(cellSize));
            if (isFree == null) throw new ArgumentNullException(nameof(isFree));

            MinX = minX;
            MinY = minY;
            CellSize = cellSize;
            Width = Math.Max(1, (int)Math.Ceiling((maxX - minX) / cellSize));
            Height = Math.Max(1, (int)Math.Ceiling((maxY - minY) / cellSize));
            _walkable = new bool[Width * Height];

            int walkable = 0;
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Vector2 center = CellCenter(x, y);
                    if (isFree(center.X, center.Y))
                    {
                        _walkable[y * Width + x] = true;
                        walkable++;
                    }
                }
            }
            WalkableCount = walkable;
        }

        public bool IsWalkable(Vector2 point)
        {
            return TryGetCell(point, out int x, out int y) && _walkable[y * Width + x];
        }

        /// <summary>
        /// The center of the walkable cell closest to point, within maxDistance; null if none.
        /// </summary>
        public Vector2? NearestWalkable(Vector2 point, float maxDistance)
        {
            if (IsWalkable(point)) return point;

            int cx = (int)Math.Floor((point.X - MinX) / CellSize);
            int cy = (int)Math.Floor((point.Y - MinY) / CellSize);
            int radius = (int)Math.Ceiling(maxDistance / CellSize);

            Vector2? best = null;
            float bestDistance = maxDistance * maxDistance;
            for (int y = Math.Max(0, cy - radius); y <= Math.Min(Height - 1, cy + radius); y++)
            {
                for (int x = Math.Max(0, cx - radius); x <= Math.Min(Width - 1, cx + radius); x++)
                {
                    if (!_walkable[y * Width + x]) continue;
                    Vector2 center = CellCenter(x, y);
                    float distance = Vector2.DistanceSquared(center, point);
                    if (distance <= bestDistance)
                    {
                        best = center;
                        bestDistance = distance;
                    }
                }
            }
            return best;
        }

        /// <summary>
        /// Smoothed path from one point to another: the first point is the start, the last one
        /// the destination (or the closest walkable points to them). Null when unreachable.
        /// </summary>
        public List<Vector2> FindPath(Vector2 from, Vector2 to, float snapDistance)
        {
            Vector2? start = NearestWalkable(from, snapDistance);
            Vector2? end = NearestWalkable(to, snapDistance);
            if (start == null || end == null) return null;

            TryGetCell(start.Value, out int sx, out int sy);
            TryGetCell(end.Value, out int ex, out int ey);
            List<int> cells = AStar(sy * Width + sx, ey * Width + ex);
            if (cells == null) return null;

            var points = new List<Vector2>(cells.Count + 2) { start.Value };
            for (int i = 1; i < cells.Count - 1; i++)
            {
                points.Add(CellCenter(cells[i] % Width, cells[i] / Width));
            }
            points.Add(end.Value);
            return Smooth(points);
        }

        /// <summary>
        /// True when every point of the segment is walkable.
        /// </summary>
        public bool HasLineOfSight(Vector2 a, Vector2 b)
        {
            float step = CellSize * 0.25f;
            int steps = Math.Max(1, (int)Math.Ceiling(Vector2.Distance(a, b) / step));
            for (int i = 0; i <= steps; i++)
            {
                if (!IsWalkable(Vector2.Lerp(a, b, (float)i / steps))) return false;
            }
            return true;
        }

        private Vector2 CellCenter(int x, int y)
        {
            return new Vector2(MinX + (x + 0.5f) * CellSize, MinY + (y + 0.5f) * CellSize);
        }

        private bool TryGetCell(Vector2 point, out int x, out int y)
        {
            x = (int)Math.Floor((point.X - MinX) / CellSize);
            y = (int)Math.Floor((point.Y - MinY) / CellSize);
            return x >= 0 && y >= 0 && x < Width && y < Height;
        }

        private List<int> AStar(int start, int goal)
        {
            var cost = new float[_walkable.Length];
            var cameFrom = new int[_walkable.Length];
            var closed = new bool[_walkable.Length];
            for (int i = 0; i < cost.Length; i++) cost[i] = float.MaxValue;

            var open = new PriorityQueue<int, float>();
            cost[start] = 0;
            cameFrom[start] = -1;
            open.Enqueue(start, Heuristic(start, goal));

            while (open.TryDequeue(out int current, out float _))
            {
                if (current == goal) return Rebuild(cameFrom, goal);
                if (closed[current]) continue;
                closed[current] = true;

                int cx = current % Width;
                int cy = current / Width;
                for (int n = 0; n < NeighborX.Length; n++)
                {
                    int nx = cx + NeighborX[n];
                    int ny = cy + NeighborY[n];
                    if (nx < 0 || ny < 0 || nx >= Width || ny >= Height) continue;

                    int next = ny * Width + nx;
                    if (!_walkable[next] || closed[next]) continue;

                    bool diagonal = n >= 4;
                    // No corner cutting: both sides of a diagonal move must be free
                    if (diagonal && (!_walkable[cy * Width + nx] || !_walkable[ny * Width + cx])) continue;

                    float nextCost = cost[current] + (diagonal ? Diagonal : 1f);
                    if (nextCost >= cost[next]) continue;

                    cost[next] = nextCost;
                    cameFrom[next] = current;
                    open.Enqueue(next, nextCost + Heuristic(next, goal));
                }
            }
            return null;
        }

        private float Heuristic(int cell, int goal)
        {
            int dx = Math.Abs(cell % Width - goal % Width);
            int dy = Math.Abs(cell / Width - goal / Width);
            return Math.Max(dx, dy) + (Diagonal - 1f) * Math.Min(dx, dy);
        }

        private static List<int> Rebuild(int[] cameFrom, int goal)
        {
            var cells = new List<int>();
            for (int cell = goal; cell != -1; cell = cameFrom[cell])
            {
                cells.Add(cell);
            }
            cells.Reverse();
            return cells;
        }

        /// <summary>
        /// Keeps a point only when the next one cannot be reached in a straight line from the last kept one.
        /// </summary>
        private List<Vector2> Smooth(List<Vector2> points)
        {
            var result = new List<Vector2> { points[0] };
            int anchor = 0;
            while (anchor < points.Count - 1)
            {
                int next = anchor + 1;
                while (next + 1 < points.Count && HasLineOfSight(points[anchor], points[next + 1]))
                {
                    next++;
                }
                result.Add(points[next]);
                anchor = next;
            }
            return result;
        }
    }
}

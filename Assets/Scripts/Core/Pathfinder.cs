using System.Collections.Generic;

namespace Hollow.Core
{
    /// <summary>Grid BFS over a dungeon layout (4-neighbourhood). Instance keeps reusable buffers.</summary>
    public sealed class Pathfinder
    {
        static readonly int[] Dx = { 1, -1, 0, 0 };
        static readonly int[] Dy = { 0, 0, 1, -1 };

        readonly DungeonLayout _layout;
        readonly int[] _parent;
        readonly int[] _queue;

        public Pathfinder(DungeonLayout layout)
        {
            _layout = layout;
            int n = layout.Width * layout.Height;
            _parent = new int[n];
            _queue = new int[n];
        }

        /// <summary>Walking distance from (sx,sy) to every tile; -1 where unreachable.</summary>
        public int[,] Distances(int sx, int sy)
        {
            int w = _layout.Width, h = _layout.Height;
            var dist = new int[w, h];
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    dist[x, y] = -1;
            if (!_layout.IsFloor(sx, sy)) return dist;

            int head = 0, tail = 0;
            _queue[tail++] = sy * w + sx;
            dist[sx, sy] = 0;
            while (head < tail)
            {
                int cur = _queue[head++];
                int cx = cur % w, cy = cur / w;
                for (int d = 0; d < 4; d++)
                {
                    int nx = cx + Dx[d], ny = cy + Dy[d];
                    if (!_layout.IsFloor(nx, ny) || dist[nx, ny] >= 0) continue;
                    dist[nx, ny] = dist[cx, cy] + 1;
                    _queue[tail++] = ny * w + nx;
                }
            }
            return dist;
        }

        /// <summary>
        /// Shortest path from start (exclusive) to goal (inclusive). Returns false if unreachable.
        /// Clears and fills <paramref name="result"/>.
        /// </summary>
        public bool FindPath(int sx, int sy, int gx, int gy, List<Cell> result)
        {
            result.Clear();
            if (!_layout.IsFloor(sx, sy) || !_layout.IsFloor(gx, gy)) return false;
            if (sx == gx && sy == gy) return true;

            int w = _layout.Width;
            for (int i = 0; i < _parent.Length; i++) _parent[i] = -1;

            int start = sy * w + sx, goal = gy * w + gx;
            int head = 0, tail = 0;
            _queue[tail++] = start;
            _parent[start] = start;
            bool found = false;

            while (head < tail && !found)
            {
                int cur = _queue[head++];
                int cx = cur % w, cy = cur / w;
                for (int d = 0; d < 4; d++)
                {
                    int nx = cx + Dx[d], ny = cy + Dy[d];
                    if (!_layout.IsFloor(nx, ny)) continue;
                    int ni = ny * w + nx;
                    if (_parent[ni] >= 0) continue;
                    _parent[ni] = cur;
                    if (ni == goal) { found = true; break; }
                    _queue[tail++] = ni;
                }
            }
            if (!found) return false;

            int node = goal;
            while (node != start)
            {
                result.Add(new Cell(node % w, node / w));
                node = _parent[node];
            }
            result.Reverse();
            return true;
        }
    }
}

using System;
using System.Collections.Generic;

namespace Hollow.Core
{
    public enum Tile : byte { Wall = 0, Floor = 1 }

    public readonly struct Cell
    {
        public readonly int X;
        public readonly int Y;
        public Cell(int x, int y) { X = x; Y = y; }
        public override string ToString() { return "(" + X + "," + Y + ")"; }
    }

    public readonly struct Room
    {
        public readonly int X, Y, W, H;
        public Room(int x, int y, int w, int h) { X = x; Y = y; W = w; H = h; }
        public int CenterX { get { return X + W / 2; } }
        public int CenterY { get { return Y + H / 2; } }
        public Cell Center { get { return new Cell(CenterX, CenterY); } }

        /// <summary>True if the two rooms are closer than <paramref name="pad"/> tiles (or overlap).</summary>
        public bool Intersects(Room o, int pad)
        {
            return X - pad < o.X + o.W && X + W + pad > o.X &&
                   Y - pad < o.Y + o.H && Y + H + pad > o.Y;
        }
    }

    public sealed class DungeonLayout
    {
        public readonly int Width;
        public readonly int Height;
        public readonly Tile[,] Tiles;
        public readonly List<Room> Rooms = new List<Room>();
        public int StartRoom;
        public int BossRoom;

        public DungeonLayout(int width, int height)
        {
            Width = width;
            Height = height;
            Tiles = new Tile[width, height];
        }

        public bool InBounds(int x, int y) { return x >= 0 && y >= 0 && x < Width && y < Height; }
        public bool IsFloor(int x, int y) { return InBounds(x, y) && Tiles[x, y] == Tile.Floor; }

        public int CountFloor()
        {
            int n = 0;
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    if (Tiles[x, y] == Tile.Floor) n++;
            return n;
        }

        internal void CarveRoom(Room r)
        {
            for (int x = r.X; x < r.X + r.W; x++)
                for (int y = r.Y; y < r.Y + r.H; y++)
                    Tiles[x, y] = Tile.Floor;
        }

        /// <summary>Carves a 2x2 brush at (x,y), never touching the outer border.</summary>
        internal void CarveBrush(int x, int y)
        {
            for (int dx = 0; dx < 2; dx++)
                for (int dy = 0; dy < 2; dy++)
                {
                    int cx = x + dx, cy = y + dy;
                    if (cx >= 1 && cy >= 1 && cx <= Width - 2 && cy <= Height - 2)
                        Tiles[cx, cy] = Tile.Floor;
                }
        }
    }

    public static class DungeonGenerator
    {
        public static DungeonLayout Generate(int seed, int width = 48, int height = 48,
                                             int roomAttempts = 40, int minSize = 5, int maxSize = 10)
        {
            var rng = new Random(seed);
            var layout = new DungeonLayout(width, height);

            for (int i = 0; i < roomAttempts; i++)
            {
                int w = rng.Next(minSize, maxSize + 1);
                int h = rng.Next(minSize, maxSize + 1);
                int xMax = width - w - 1;   // exclusive upper bound => room ends at most at width-2
                int yMax = height - h - 1;
                if (xMax <= 2 || yMax <= 2) continue;
                var room = new Room(rng.Next(2, xMax), rng.Next(2, yMax), w, h);

                bool overlaps = false;
                for (int j = 0; j < layout.Rooms.Count; j++)
                    if (room.Intersects(layout.Rooms[j], 2)) { overlaps = true; break; }
                if (overlaps) continue;

                layout.Rooms.Add(room);
                layout.CarveRoom(room);
            }

            if (layout.Rooms.Count < 4) BuildFallback(layout);

            // Sort by X so the chain of corridors stays short, then connect neighbours.
            layout.Rooms.Sort((a, b) => a.CenterX.CompareTo(b.CenterX));
            for (int i = 0; i + 1 < layout.Rooms.Count; i++)
                ConnectRooms(layout, layout.Rooms[i], layout.Rooms[i + 1], rng.Next(2) == 0);

            layout.StartRoom = 0;
            layout.BossRoom = FarthestRoom(layout, layout.StartRoom);
            return layout;
        }

        static void BuildFallback(DungeonLayout layout)
        {
            // Deterministic 4-room layout used if random placement failed to fit enough rooms.
            for (int x = 0; x < layout.Width; x++)
                for (int y = 0; y < layout.Height; y++)
                    layout.Tiles[x, y] = Tile.Wall;
            layout.Rooms.Clear();
            int s = 8;
            int far = layout.Width - s - 4;
            int farY = layout.Height - s - 4;
            var rooms = new[]
            {
                new Room(4, 4, s, s), new Room(far, 4, s, s),
                new Room(4, farY, s, s), new Room(far, farY, s, s)
            };
            foreach (var r in rooms) { layout.Rooms.Add(r); layout.CarveRoom(r); }
        }

        static void ConnectRooms(DungeonLayout layout, Room a, Room b, bool horizontalFirst)
        {
            int x1 = a.CenterX, y1 = a.CenterY, x2 = b.CenterX, y2 = b.CenterY;
            if (horizontalFirst)
            {
                CarveH(layout, x1, x2, y1);
                CarveV(layout, y1, y2, x2);
            }
            else
            {
                CarveV(layout, y1, y2, x1);
                CarveH(layout, x1, x2, y2);
            }
        }

        static void CarveH(DungeonLayout l, int xa, int xb, int y)
        {
            int lo = Math.Min(xa, xb), hi = Math.Max(xa, xb);
            for (int x = lo; x <= hi; x++) l.CarveBrush(x, y);
        }

        static void CarveV(DungeonLayout l, int ya, int yb, int x)
        {
            int lo = Math.Min(ya, yb), hi = Math.Max(ya, yb);
            for (int y = lo; y <= hi; y++) l.CarveBrush(x, y);
        }

        static int FarthestRoom(DungeonLayout layout, int from)
        {
            var pf = new Pathfinder(layout);
            var c = layout.Rooms[from].Center;
            int[,] dist = pf.Distances(c.X, c.Y);
            int best = from, bestDist = -1;
            for (int i = 0; i < layout.Rooms.Count; i++)
            {
                if (i == from) continue;
                var rc = layout.Rooms[i].Center;
                int d = dist[rc.X, rc.Y];
                if (d > bestDist) { bestDist = d; best = i; }
            }
            return best;
        }
    }
}

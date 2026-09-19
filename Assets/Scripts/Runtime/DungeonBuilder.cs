using System.Collections.Generic;
using Hollow.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Hollow
{
    public static class Layers
    {
        /// <summary>Unnamed user layer used for dungeon geometry (camera occlusion, line of sight).</summary>
        public const int World = 8;
        public const int WorldMask = 1 << World;
    }

    /// <summary>Turns a <see cref="DungeonLayout"/> into meshes, colliders and lights.</summary>
    public static class DungeonBuilder
    {
        public const float CellSize = 3f;
        public const float WallHeight = 6f;

        public static Vector3 ToWorld(int x, int y) { return new Vector3((x + 0.5f) * CellSize, 0f, (y + 0.5f) * CellSize); }
        public static Vector3 ToWorld(Cell c) { return ToWorld(c.X, c.Y); }
        public static Cell ToCell(Vector3 p) { return new Cell(Mathf.FloorToInt(p.x / CellSize), Mathf.FloorToInt(p.z / CellSize)); }

        public static GameObject Build(DungeonLayout layout, int floor)
        {
            var root = new GameObject("Dungeon");

            // Palette shifts a little with each floor so floors feel different.
            float hue = Mathf.Repeat(0.58f + floor * 0.07f, 1f);
            Color wallA = Color.HSVToRGB(hue, 0.25f, 0.30f);
            Color wallB = Color.HSVToRGB(hue, 0.20f, 0.55f);
            Color floorA = Color.HSVToRGB(hue, 0.18f, 0.16f);
            Color floorB = Color.HSVToRGB(hue, 0.15f, 0.34f);

            var wallMat = ProcAssets.Lit(Color.white, ProcAssets.StoneTexture(wallA, wallB, 64, floor * 3 + 1, true), 0.05f);
            var floorMat = ProcAssets.Lit(Color.white, ProcAssets.StoneTexture(floorA, floorB, 64, floor * 3 + 2, false), 0.2f);
            var ceilMat = ProcAssets.Lit(new Color(0.05f, 0.05f, 0.07f), null, 0f);

            // Meshes are split into chunks so each chunk is lit by the nearest torches (per-object light limit).
            var floors = new Dictionary<long, List<CombineInstance>>();
            var walls = new Dictionary<long, List<CombineInstance>>();
            Mesh cube = ProcAssets.CubeMesh;

            for (int x = 0; x < layout.Width; x++)
            {
                for (int y = 0; y < layout.Height; y++)
                {
                    Vector3 c = ToWorld(x, y);
                    if (layout.IsFloor(x, y))
                    {
                        AddTo(floors, x, y, new CombineInstance
                        {
                            mesh = cube,
                            transform = Matrix4x4.TRS(c + Vector3.down * 0.2f, Quaternion.identity, new Vector3(CellSize, 0.4f, CellSize))
                        });
                    }
                    else if (TouchesFloor(layout, x, y))
                    {
                        AddTo(walls, x, y, new CombineInstance
                        {
                            mesh = cube,
                            transform = Matrix4x4.TRS(c + Vector3.up * (WallHeight * 0.5f), Quaternion.identity,
                                                      new Vector3(CellSize, WallHeight, CellSize))
                        });
                    }
                }
            }

            foreach (var kv in floors) MakeCombined("Floor_" + kv.Key, root.transform, kv.Value, floorMat);
            foreach (var kv in walls) MakeCombined("Walls_" + kv.Key, root.transform, kv.Value, wallMat);

            // Ceiling: one big slab, no collider.
            var ceil = ProcAssets.Prim(PrimitiveType.Cube, root.transform,
                new Vector3(layout.Width * CellSize * 0.5f, WallHeight + 0.25f, layout.Height * CellSize * 0.5f),
                new Vector3(layout.Width * CellSize, 0.5f, layout.Height * CellSize), ceilMat, false, "Ceiling");
            ceil.layer = Layers.World;

            // Lights and torches: one per room. Start room is cool blue, boss room is red.
            for (int i = 0; i < layout.Rooms.Count; i++)
            {
                var r = layout.Rooms[i];
                Color lc = new Color(1f, 0.72f, 0.42f);
                float intensity = 2.2f;
                if (i == layout.StartRoom) lc = new Color(0.45f, 0.75f, 1f);
                else if (i == layout.BossRoom) { lc = new Color(1f, 0.25f, 0.2f); intensity = 3.2f; }
                CreateTorch(root.transform, ToWorld(r.CenterX, r.CenterY) + Vector3.up * (WallHeight - 1.3f), lc, intensity, 26f);
            }

            return root;
        }

        const int ChunkTiles = 6;

        static void AddTo(Dictionary<long, List<CombineInstance>> dict, int x, int y, CombineInstance ci)
        {
            long key = (long)(x / ChunkTiles) * 1000L + (y / ChunkTiles);
            List<CombineInstance> list;
            if (!dict.TryGetValue(key, out list)) { list = new List<CombineInstance>(); dict[key] = list; }
            list.Add(ci);
        }

        static bool TouchesFloor(DungeonLayout l, int x, int y)
        {
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    if ((dx != 0 || dy != 0) && l.IsFloor(x + dx, y + dy)) return true;
            return false;
        }

        static void MakeCombined(string name, Transform parent, List<CombineInstance> instances, Material mat)
        {
            var go = new GameObject(name);
            go.layer = Layers.World;
            go.transform.SetParent(parent, false);
            if (instances.Count == 0) return;

            var mesh = new Mesh { name = name + "Mesh", indexFormat = IndexFormat.UInt32 };
            mesh.CombineMeshes(instances.ToArray(), true, true);
            mesh.RecalculateBounds();

            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        static void CreateTorch(Transform parent, Vector3 pos, Color color, float intensity, float range)
        {
            var go = new GameObject("Torch");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
            go.AddComponent<TorchFlicker>();

            // Visible glowing brazier below the light.
            var mat = ProcAssets.Unlit(color);
            ProcAssets.Prim(PrimitiveType.Sphere, go.transform, new Vector3(0f, -0.2f, 0f), Vector3.one * 0.35f, mat, false, "Flame");
        }
    }
}

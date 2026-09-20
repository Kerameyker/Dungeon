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

        public static GameObject Build(DungeonLayout layout, int floor, bool town = false)
        {
            var root = new GameObject("Dungeon");

            // Palette shifts a little with each floor so floors feel different.
            // Each biome (5 floors) has its own base hue; floors inside it drift a little.
            var biome = BiomeCatalog.ForFloor(floor);
            float hue = Mathf.Repeat(biome.Hue + ((Mathf.Max(1, floor) - 1) % BiomeCatalog.FloorsPerBiome) * 0.012f, 1f);
            float sat = biome.Sat;
            Color wallA = Color.HSVToRGB(hue, sat, 0.42f);
            Color wallB = Color.HSVToRGB(hue, sat * 0.8f, 0.72f);
            Color floorA = Color.HSVToRGB(hue, sat * 0.72f, 0.26f);
            Color floorB = Color.HSVToRGB(hue, sat * 0.6f, 0.48f);
            Color torchColor = new Color(biome.Torch[0], biome.Torch[1], biome.Torch[2]);

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
                Color lc = torchColor;
                float intensity = 6.0f;
                if (town) { lc = new Color(1f, 0.8f, 0.55f); intensity = 11f; }
                else if (i == layout.StartRoom) lc = new Color(0.45f, 0.75f, 1f);
                else if (i == layout.BossRoom) { lc = new Color(1f, 0.25f, 0.2f); intensity = 7.0f; }
                CreateTorch(root.transform, ToWorld(r.CenterX, r.CenterY) + Vector3.up * (WallHeight - 1.3f), lc, intensity, town ? 50f : 34f);
            }

            if (!town) Decorate(layout, root.transform, floor);
            return root;
        }

        // ------------------------------------------------------------------ decoration

        static void Decorate(DungeonLayout layout, Transform root, int floor)
        {
            var rng = new System.Random(floor * 104729 + 17);
            var deco = new GameObject("Decor").transform;
            deco.SetParent(root, false);

            var pillarMat = ProcAssets.Lit(Color.white, ProcAssets.StoneTexture(new Color(0.16f, 0.17f, 0.2f), new Color(0.4f, 0.42f, 0.48f), 32, floor + 40, false), 0.05f);
            var woodMat = ProcAssets.Lit(new Color(0.36f, 0.22f, 0.11f), null, 0.05f);
            var crateMat = ProcAssets.Lit(new Color(0.45f, 0.32f, 0.17f), null, 0.05f);
            var ironMat = ProcAssets.Lit(new Color(0.12f, 0.12f, 0.14f), null, 0.3f);
            var boneMat = ProcAssets.Lit(new Color(0.85f, 0.82f, 0.7f), null, 0.1f);
            var rubbleMat = ProcAssets.Lit(new Color(0.22f, 0.23f, 0.26f), null, 0.05f);
            var biome = BiomeCatalog.ForFloor(floor);
            int biomeIdx = BiomeCatalog.IndexFor(floor);
            var flameCol = new Color(biome.Torch[0], biome.Torch[1] * 0.85f, biome.Torch[2] * 0.6f);

            for (int i = 0; i < layout.Rooms.Count; i++)
            {
                var r = layout.Rooms[i];
                bool boss = i == layout.BossRoom;
                bool start = i == layout.StartRoom;

                // Corner pillars with flame bowls (two flames per room keeps the particle count low).
                int[] cx = { r.X, r.X + r.W - 1, r.X, r.X + r.W - 1 };
                int[] cy = { r.Y, r.Y, r.Y + r.H - 1, r.Y + r.H - 1 };
                for (int k = 0; k < 4; k++)
                {
                    if (!layout.IsFloor(cx[k], cy[k])) continue;
                    // a corridor touching this corner from outside the room: keep it clear
                    bool mouth = false;
                    for (int d = 0; d < 4; d++)
                    {
                        int nx = cx[k] + (d == 0 ? 1 : d == 1 ? -1 : 0), ny = cy[k] + (d == 2 ? 1 : d == 3 ? -1 : 0);
                        bool inside = nx >= r.X && nx < r.X + r.W && ny >= r.Y && ny < r.Y + r.H;
                        if (!inside && layout.IsFloor(nx, ny)) mouth = true;
                    }
                    if (mouth) continue;
                    Vector3 c = ToWorld(cx[k], cy[k]);
                    // hug the corner: push towards the adjacent walls
                    float sx = layout.IsFloor(cx[k] + 1, cy[k]) ? -1f : 1f;
                    float sz = layout.IsFloor(cx[k], cy[k] + 1) ? -1f : 1f;
                    Vector3 pos = c + new Vector3(sx * 0.9f, 0f, sz * 0.9f);
                    var pillar = ProcAssets.Prim(PrimitiveType.Cube, deco, pos + Vector3.up * 0.9f, new Vector3(0.8f, 1.8f, 0.8f), pillarMat, true, "Pillar");
                    ProcAssets.Prim(PrimitiveType.Cube, deco, pos + Vector3.up * 1.85f, new Vector3(1.0f, 0.14f, 1.0f), ironMat, false, "Bowl");
                    if (k < 2 || boss)
                        Particles.Flame(deco, pos + Vector3.up * 2.0f, boss ? new Color(1f, 0.3f, 0.2f) : (start ? new Color(0.5f, 0.8f, 1f) : flameCol), 0.5f);
                }

                // Props along the walls (never in corridor mouths: only room cells that touch a wall).
                int props = boss || start ? 2 : 3 + rng.Next(4);
                for (int n = 0; n < props; n++)
                {
                    int x = r.X + rng.Next(r.W), y = r.Y + rng.Next(r.H);
                    if (!layout.IsFloor(x, y)) continue;
                    bool wl = !layout.IsFloor(x - 1, y), wr = !layout.IsFloor(x + 1, y);
                    bool wd = !layout.IsFloor(x, y - 1), wu = !layout.IsFloor(x, y + 1);
                    int walls = (wl ? 1 : 0) + (wr ? 1 : 0) + (wd ? 1 : 0) + (wu ? 1 : 0);
                    if (walls != 1) continue;   // corners hold pillars, mouths and open floor stay clear
                    Vector3 c = ToWorld(x, y);
                    Vector3 off = new Vector3(wl ? -0.85f : wr ? 0.85f : (float)(rng.NextDouble() - 0.5),
                                              0f,
                                              wd ? -0.85f : wu ? 0.85f : (float)(rng.NextDouble() - 0.5));
                    Vector3 p = c + off;
                    switch (rng.Next(4))
                    {
                        case 0:
                            ProcAssets.Prim(PrimitiveType.Cylinder, deco, p + Vector3.up * 0.45f, new Vector3(0.7f, 0.45f, 0.7f), woodMat, true, "Barrel");
                            ProcAssets.Prim(PrimitiveType.Cylinder, deco, p + Vector3.up * 0.55f, new Vector3(0.74f, 0.04f, 0.74f), ironMat, false, "Band");
                            break;
                        case 1:
                        {
                            var cr = ProcAssets.Prim(PrimitiveType.Cube, deco, p + Vector3.up * 0.4f, new Vector3(0.8f, 0.8f, 0.8f), crateMat, true, "Crate");
                            cr.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 40f, 0f);
                            break;
                        }
                        case 2:
                            for (int b = 0; b < 5; b++)
                            {
                                var bone = ProcAssets.Prim(PrimitiveType.Cube, deco,
                                    p + new Vector3((float)(rng.NextDouble() - 0.5) * 0.9f, 0.05f, (float)(rng.NextDouble() - 0.5) * 0.9f),
                                    new Vector3(0.06f, 0.06f, 0.35f + (float)rng.NextDouble() * 0.25f), boneMat, false, "Bone");
                                bone.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 180f, 0f);
                            }
                            ProcAssets.Prim(PrimitiveType.Sphere, deco, p + new Vector3(0.1f, 0.13f, 0.1f), Vector3.one * 0.26f, boneMat, false, "Skull");
                            break;
                        default:
                            for (int b = 0; b < 6; b++)
                            {
                                float sz2 = 0.15f + (float)rng.NextDouble() * 0.3f;
                                var rb = ProcAssets.Prim(PrimitiveType.Cube, deco,
                                    p + new Vector3((float)(rng.NextDouble() - 0.5) * 0.9f, sz2 * 0.4f, (float)(rng.NextDouble() - 0.5) * 0.9f),
                                    new Vector3(sz2, sz2 * 0.8f, sz2), rubbleMat, false, "Rubble");
                                rb.transform.rotation = Random.rotation;
                            }
                            break;
                    }
                }

                // Boss room: glowing rune circle on the floor.
                if (boss)
                {
                    Vector3 c = ToWorld(r.CenterX, r.CenterY);
                    ProcAssets.Prim(PrimitiveType.Cylinder, deco, c + Vector3.up * 0.02f, new Vector3(10f, 0.01f, 10f), ProcAssets.Unlit(new Color(0.18f, 0.03f, 0.05f)), false, "RuneDisc");
                    ProcAssets.Prim(PrimitiveType.Cylinder, deco, c + Vector3.up * 0.03f, new Vector3(9f, 0.01f, 9f), ProcAssets.Unlit(new Color(0.07f, 0.01f, 0.02f)), false, "RuneInner");
                    for (int k = 0; k < 8; k++)
                    {
                        float a = k * Mathf.PI * 0.25f;
                        ProcAssets.Prim(PrimitiveType.Cube, deco, c + new Vector3(Mathf.Cos(a) * 4.2f, 0.05f, Mathf.Sin(a) * 4.2f),
                                        new Vector3(0.3f, 0.02f, 0.9f), ProcAssets.Unlit(new Color(0.9f, 0.15f, 0.15f)), false, "Rune")
                            .transform.rotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg + 90f, 0f);
                    }
                }

                if (!start) BiomeDecor(layout, r, deco, rng, biomeIdx, boss);

                // Floating dust motes (tinted by the biome).
                Vector3 mid = ToWorld(r.CenterX, r.CenterY) + Vector3.up * 2f;
                Color dust = biomeIdx == 2 ? new Color(1f, 0.55f, 0.25f, 0.4f) : (biomeIdx == 1 ? new Color(0.7f, 1f, 0.6f, 0.35f) : (biomeIdx == 3 ? new Color(0.8f, 0.6f, 1f, 0.4f) : new Color(0.7f, 0.8f, 1f, 0.35f)));
                Particles.Dust(deco, mid, new Vector3(r.W * CellSize * 0.9f, 3.5f, r.H * CellSize * 0.9f), dust);
            }
        }

        /// <summary>Flat and low props that give each biome its own look. No colliders, so they never block anything.</summary>
        static void BiomeDecor(DungeonLayout layout, Room r, Transform deco, System.Random rng, int biomeIdx, bool boss)
        {
            if (biomeIdx == 0) return;
            int n = boss ? 3 : 3 + rng.Next(3);
            Material mossMat = null, capMat = null, stemMat = null, lavaMat = null, crackMat = null, voidMat = null;
            if (biomeIdx == 1)
            {
                mossMat = ProcAssets.Lit(new Color(0.16f, 0.36f, 0.14f), null, 0.05f);
                capMat = ProcAssets.Lit(new Color(0.5f, 1f, 0.7f), null, 0.3f, new Color(0.15f, 0.6f, 0.35f));
                stemMat = ProcAssets.Lit(new Color(0.85f, 0.9f, 0.8f), null, 0.1f);
            }
            else if (biomeIdx == 2)
            {
                lavaMat = ProcAssets.Unlit(new Color(1f, 0.4f, 0.08f));
                crackMat = ProcAssets.Unlit(new Color(0.9f, 0.25f, 0.05f));
            }
            else voidMat = ProcAssets.Lit(new Color(0.55f, 0.3f, 0.95f), null, 0.6f, new Color(0.35f, 0.15f, 0.7f));

            for (int i = 0; i < n; i++)
            {
                int x = r.X + rng.Next(r.W), y = r.Y + rng.Next(r.H);
                if (!layout.IsFloor(x, y)) continue;
                if (x == r.CenterX && y == r.CenterY) continue;
                Vector3 p = ToWorld(x, y) + new Vector3((float)(rng.NextDouble() - 0.5) * 1.8f, 0f, (float)(rng.NextDouble() - 0.5) * 1.8f);
                if (biomeIdx == 1)
                {
                    var moss = ProcAssets.Prim(PrimitiveType.Cylinder, deco, p + Vector3.up * 0.025f, new Vector3(1.2f + (float)rng.NextDouble(), 0.02f, 1.0f + (float)rng.NextDouble() * 0.8f), mossMat, false, "Moss");
                    moss.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 180f, 0f);
                    for (int m = 0; m < 3; m++)
                    {
                        Vector3 q = p + new Vector3((float)(rng.NextDouble() - 0.5) * 0.9f, 0f, (float)(rng.NextDouble() - 0.5) * 0.9f);
                        float h = 0.18f + (float)rng.NextDouble() * 0.2f;
                        ProcAssets.Prim(PrimitiveType.Cylinder, deco, q + Vector3.up * (h * 0.5f), new Vector3(0.06f, h * 0.5f, 0.06f), stemMat, false, "Stem");
                        ProcAssets.Prim(PrimitiveType.Sphere, deco, q + Vector3.up * h, new Vector3(0.28f, 0.16f, 0.28f), capMat, false, "Cap");
                    }
                }
                else if (biomeIdx == 2)
                {
                    float yaw = (float)rng.NextDouble() * 180f;
                    for (int c = 0; c < 4; c++)
                    {
                        var crack = ProcAssets.Prim(PrimitiveType.Cube, deco, p + new Vector3((c - 1.5f) * 0.35f, 0.03f, (float)(rng.NextDouble() - 0.5) * 0.5f),
                                                    new Vector3(0.08f, 0.02f, 0.6f + (float)rng.NextDouble() * 0.8f), c % 2 == 0 ? lavaMat : crackMat, false, "LavaCrack");
                        crack.transform.rotation = Quaternion.Euler(0f, yaw + c * 22f, 0f);
                    }
                    if (rng.Next(3) == 0) Particles.Flame(deco, p + Vector3.up * 0.2f, new Color(1f, 0.4f, 0.1f), 0.3f);
                }
                else
                {
                    float h = 0.6f + (float)rng.NextDouble() * 0.9f;
                    var crystal = ProcAssets.Prim(PrimitiveType.Cube, deco, p + Vector3.up * (h * 0.5f + 0.2f), new Vector3(0.28f, h, 0.28f), voidMat, false, "VoidCrystal");
                    crystal.transform.rotation = Quaternion.Euler((float)(rng.NextDouble() - 0.5) * 20f, (float)rng.NextDouble() * 90f, 45f * (float)rng.NextDouble());
                    ProcAssets.Prim(PrimitiveType.Cube, deco, p + Vector3.up * 0.02f, new Vector3(0.9f, 0.02f, 0.9f), ProcAssets.Unlit(new Color(0.25f, 0.1f, 0.45f)), false, "VoidGlyph")
                        .transform.rotation = Quaternion.Euler(0f, 45f, 0f);
                }
            }
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
        }
    }
}

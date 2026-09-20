using System;

namespace Hollow.Core
{
    /// <summary>Visual theme of a group of floors. Colors are plain floats so Core stays engine-free.</summary>
    public sealed class BiomeDef
    {
        public string Name;
        public float Hue;                        // wall/floor hue 0..1
        public float Sat;                        // stone saturation
        public float[] Torch;                    // r,g,b of room lights and flames
        public float[] Fog;                      // fog color
        public float FogDensity;
        public float[] Ambient;                  // ambient light
        public float[] EnemyTint;                // multiplied into enemy body colors (white = unchanged)
    }

    public static class BiomeCatalog
    {
        public const int FloorsPerBiome = 5;

        static readonly BiomeDef[] Defs =
        {
            new BiomeDef { Name = "Sunken Crypt", Hue = 0.58f, Sat = 0.25f, Torch = new[] { 1f, 0.72f, 0.42f }, Fog = new[] { 0.06f, 0.08f, 0.12f }, FogDensity = 0.005f, Ambient = new[] { 0.55f, 0.60f, 0.75f }, EnemyTint = new[] { 1f, 1f, 1f } },
            new BiomeDef { Name = "Green Ruin", Hue = 0.33f, Sat = 0.30f, Torch = new[] { 0.75f, 1f, 0.55f }, Fog = new[] { 0.04f, 0.09f, 0.05f }, FogDensity = 0.007f, Ambient = new[] { 0.50f, 0.72f, 0.55f }, EnemyTint = new[] { 0.85f, 1.05f, 0.85f } },
            new BiomeDef { Name = "Ember Depths", Hue = 0.03f, Sat = 0.35f, Torch = new[] { 1f, 0.45f, 0.2f }, Fog = new[] { 0.12f, 0.04f, 0.03f }, FogDensity = 0.007f, Ambient = new[] { 0.78f, 0.52f, 0.42f }, EnemyTint = new[] { 1.1f, 0.85f, 0.8f } },
            new BiomeDef { Name = "Void Stair", Hue = 0.78f, Sat = 0.32f, Torch = new[] { 0.78f, 0.5f, 1f }, Fog = new[] { 0.06f, 0.03f, 0.10f }, FogDensity = 0.008f, Ambient = new[] { 0.60f, 0.50f, 0.82f }, EnemyTint = new[] { 0.9f, 0.85f, 1.1f } },
        };

        public static int IndexFor(int floor)
        {
            int f = Math.Max(1, floor);
            return ((f - 1) / FloorsPerBiome) % Defs.Length;
        }

        public static BiomeDef ForFloor(int floor) { return Defs[IndexFor(floor)]; }
        public static int Count { get { return Defs.Length; } }
    }
}

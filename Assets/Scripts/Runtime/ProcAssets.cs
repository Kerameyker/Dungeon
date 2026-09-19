using System.Collections.Generic;
using UnityEngine;

namespace Hollow
{
    /// <summary>Procedurally generated materials, textures and primitive helpers (no asset files needed).</summary>
    public static class ProcAssets
    {
        static Shader _lit, _unlit;
        static Mesh _cubeMesh;
        static readonly Dictionary<int, Material> UnlitCache = new Dictionary<int, Material>();

        static Shader LitShader
        {
            get
            {
                if (_lit == null)
                {
                    if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null)
                        _lit = Shader.Find("Universal Render Pipeline/Lit");
                    if (_lit == null) _lit = Shader.Find("Standard");
                }
                return _lit;
            }
        }

        static Shader UnlitShader
        {
            get
            {
                if (_unlit == null)
                {
                    if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null)
                        _unlit = Shader.Find("Universal Render Pipeline/Unlit");
                    if (_unlit == null) _unlit = Shader.Find("Unlit/Color");
                }
                return _unlit;
            }
        }

        static void SetColorAll(Material m, Color c)
        {
            m.color = c;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        }

        /// <summary>Creates a NEW lit material (call sites cache it if they want to share it).</summary>
        public static Material Lit(Color color, Texture2D tex = null, float smoothness = 0.15f, Color? emission = null)
        {
            var m = new Material(LitShader);
            SetColorAll(m, color);
            if (tex != null)
            {
                m.mainTexture = tex;
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            }
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
            if (emission.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission.Value);
            }
            return m;
        }

        /// <summary>Shared unlit material for a color (cached by color).</summary>
        public static Material Unlit(Color color)
        {
            Color32 c32 = color;
            int key = (c32.r << 24) | (c32.g << 16) | (c32.b << 8) | c32.a;
            Material m;
            if (UnlitCache.TryGetValue(key, out m) && m != null) return m;
            m = new Material(UnlitShader);
            SetColorAll(m, color);
            UnlitCache[key] = m;
            return m;
        }

        /// <summary>Sets the main color (works with URP and built-in shaders; not affected by variant stripping).</summary>
        public static void Tint(Material m, Color c) { SetColorAll(m, c); }

        public static void SetEmission(Material m, Color c)
        {
            m.EnableKeyword("_EMISSION");
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", c);
        }

        public static Texture2D StoneTexture(Color a, Color b, int size, int seed, bool bricks)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, true);
            var px = new Color[size * size];
            float ox = seed * 13.37f, oy = seed * 7.11f;
            int rowH = Mathf.Max(4, size / 4);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float n = Mathf.PerlinNoise(ox + x * 0.12f, oy + y * 0.12f) * 0.7f +
                              Mathf.PerlinNoise(ox + x * 0.5f, oy + y * 0.5f) * 0.3f;
                    Color c = Color.Lerp(a, b, n);
                    if (bricks)
                    {
                        int row = y / rowH;
                        int off = (row % 2 == 0) ? 0 : size / 4;
                        bool mortar = (y % rowH) < 2 || ((x + off) % (size / 2)) < 2;
                        if (mortar) c *= 0.45f;
                    }
                    c.a = 1f;
                    px[y * size + x] = c;
                }
            }
            t.SetPixels(px);
            t.wrapMode = TextureWrapMode.Repeat;
            t.filterMode = FilterMode.Point;
            t.Apply(true);
            return t;
        }

        public static Mesh CubeMesh
        {
            get
            {
                if (_cubeMesh == null)
                {
                    var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    _cubeMesh = tmp.GetComponent<MeshFilter>().sharedMesh;
                    Object.Destroy(tmp);
                }
                return _cubeMesh;
            }
        }

        /// <summary>Creates a primitive child without a collider (unless asked), with local transform + material.</summary>
        public static GameObject Prim(PrimitiveType type, Transform parent, Vector3 localPos, Vector3 scale,
                                      Material mat, bool keepCollider = false, string name = null)
        {
            var go = GameObject.CreatePrimitive(type);
            if (name != null) go.name = name;
            if (!keepCollider)
            {
                var col = go.GetComponent<Collider>();
                if (col != null) Object.Destroy(col);
            }
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = mat;
            return go;
        }

        public static Material WhiteSpriteMaterial() { return Unlit(Color.white); }

        public static Sprite WhiteSprite()
        {
            var t = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var px = new Color32[16];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
            t.SetPixels32(px);
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}

using UnityEngine;

namespace Hollow
{
    /// <summary>Faces the main camera (quads and TextMesh are readable from their -Z side).</summary>
    public class Billboard : MonoBehaviour
    {
        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 dir = transform.position - cam.transform.position;
            if (dir.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(dir);
        }
    }

    /// <summary>Small glowing cube that flies out and shrinks (hit sparks, dust, death bursts).</summary>
    public class Spark : MonoBehaviour
    {
        Vector3 _vel;
        float _life, _t;
        Vector3 _startScale;

        public static void Burst(Vector3 pos, Color color, int count, float speed = 4f, float size = 0.12f, float life = 0.45f)
        {
            var mat = ProcAssets.Unlit(color);
            for (int i = 0; i < count; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                var col = go.GetComponent<Collider>();
                if (col != null) Destroy(col);
                go.GetComponent<Renderer>().sharedMaterial = mat;
                go.name = "Spark";
                go.transform.position = pos;
                float s = size * Random.Range(0.6f, 1.3f);
                go.transform.localScale = Vector3.one * s;
                go.transform.rotation = Random.rotation;
                var sp = go.AddComponent<Spark>();
                sp._vel = Random.onUnitSphere * speed * Random.Range(0.4f, 1f) + Vector3.up * speed * 0.4f;
                sp._life = life * Random.Range(0.7f, 1.2f);
                sp._startScale = go.transform.localScale;
            }
        }

        void Update()
        {
            _t += Time.deltaTime;
            if (_t >= _life) { Destroy(gameObject); return; }
            _vel.y -= 9.8f * Time.deltaTime;
            transform.position += _vel * Time.deltaTime;
            transform.localScale = _startScale * (1f - _t / _life);
        }
    }

    /// <summary>Floating combat text.</summary>
    public class DamageNumber : MonoBehaviour
    {
        TextMesh _tm;
        float _t;
        float _life = 0.9f;
        Color _color;

        static Font _font;

        public static void Spawn(Vector3 pos, string text, Color color, bool big = false)
        {
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var go = new GameObject("DamageNumber");
            go.transform.position = pos + new Vector3(Random.Range(-0.3f, 0.3f), 0f, Random.Range(-0.3f, 0.3f));
            var tm = go.AddComponent<TextMesh>();
            tm.font = _font;
            tm.text = text;
            tm.fontSize = 64;
            tm.characterSize = big ? 0.11f : 0.07f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            go.GetComponent<MeshRenderer>().sharedMaterial = _font.material;
            var d = go.AddComponent<DamageNumber>();
            d._tm = tm;
            d._color = color;
        }

        void LateUpdate()
        {
            _t += Time.deltaTime;
            if (_t >= _life) { Destroy(gameObject); return; }
            transform.position += Vector3.up * 1.4f * Time.deltaTime;
            var c = _color;
            c.a = 1f - Mathf.Clamp01((_t - 0.4f) / (_life - 0.4f));
            _tm.color = c;
            var cam = Camera.main;
            if (cam != null) transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
        }
    }

    /// <summary>Particle helpers (flames, dust motes) and an expanding ground ring.</summary>
    public static class Particles
    {
        static ParticleSystem Make(string name, Transform parent, Vector3 localPos, Color tint)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var ps = go.AddComponent<ParticleSystem>();
            var r = go.GetComponent<ParticleSystemRenderer>();
            var mat = ProcAssets.SpriteMat(ProcAssets.SoftCircle(), tint);
            if (mat != null) r.sharedMaterial = mat;
            else r.enabled = false;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return ps;
        }

        public static ParticleSystem Flame(Transform parent, Vector3 localPos, Color color, float size)
        {
            var ps = Make("FlameFx", parent, localPos, Color.white);
            var main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.1f);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size * 1.1f);
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 40;
            var em = ps.emission;
            em.rateOverTime = 22f;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Cone;
            sh.angle = 6f;
            sh.radius = size * 0.35f;
            sh.rotation = new Vector3(-90f, 0f, 0f);   // emit upwards
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.4f, 0.1f), 0.6f), new GradientColorKey(new Color(0.4f, 0.1f, 0.05f), 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.9f, 0.15f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var sz = ps.sizeOverLifetime;
            sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));
            return ps;
        }

        /// <summary>Slow floating motes filling a room.</summary>
        public static ParticleSystem Dust(Transform parent, Vector3 localPos, Vector3 boxSize, Color color)
        {
            var ps = Make("DustFx", parent, localPos, Color.white);
            var main = ps.main;
            main.loop = true;
            main.startLifetime = 7f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.12f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.11f);
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 60;
            var em = ps.emission;
            em.rateOverTime = 8f;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = boxSize;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            ps.Simulate(7f, true, true);   // start already filled
            ps.Play();
            return ps;
        }
    }

    /// <summary>Flat ring that expands and fades on the floor (slams, level-ups, charges).</summary>
    public class GroundRing : MonoBehaviour
    {
        Material _mat;
        Color _color;
        float _t, _life, _r0, _r1;

        public static void Spawn(Vector3 pos, float r0, float r1, Color color, float life)
        {
            var mat = ProcAssets.SpriteMat(ProcAssets.RingTexture(), color);
            if (mat == null) return;
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var c = go.GetComponent<Collider>();
            if (c != null) Destroy(c);
            go.name = "GroundRing";
            go.GetComponent<Renderer>().sharedMaterial = mat;
            go.transform.position = new Vector3(pos.x, pos.y + 0.07f, pos.z);
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = new Vector3(r0 * 2f, r0 * 2f, 1f);
            var ring = go.AddComponent<GroundRing>();
            ring._mat = mat; ring._color = color; ring._life = life; ring._r0 = r0; ring._r1 = r1;
        }

        void Update()
        {
            _t += Time.deltaTime;
            float k = Mathf.Clamp01(_t / _life);
            float r = Mathf.Lerp(_r0, _r1, 1f - (1f - k) * (1f - k));
            transform.localScale = new Vector3(r * 2f, r * 2f, 1f);
            var c = _color;
            c.a = _color.a * (1f - k);
            _mat.color = c;
            if (k >= 1f) { Destroy(_mat); Destroy(gameObject); }
        }
    }

    /// <summary>Makes a point light flicker like a torch.</summary>
    public class TorchFlicker : MonoBehaviour
    {
        Light _light;
        float _base;
        float _seed;

        void Start()
        {
            _light = GetComponent<Light>();
            if (_light != null) _base = _light.intensity;
            _seed = Random.value * 100f;
        }

        void Update()
        {
            if (_light == null) return;
            _light.intensity = _base * (0.85f + 0.3f * Mathf.PerlinNoise(_seed, Time.time * 3f));
        }
    }
}

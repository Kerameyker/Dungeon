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

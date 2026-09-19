using UnityEngine;

namespace Hollow
{
    /// <summary>Green orb dropped by enemies; heals the player on touch.</summary>
    public class HpOrb : MonoBehaviour
    {
        Vector3 _basePos;
        float _t;

        public static void Spawn(Vector3 pos)
        {
            var go = new GameObject("HpOrb");
            go.transform.position = pos + Vector3.up * 0.7f;
            var mat = ProcAssets.Unlit(new Color(0.3f, 1f, 0.5f));
            ProcAssets.Prim(PrimitiveType.Sphere, go.transform, Vector3.zero, Vector3.one * 0.35f, mat);
            var l = new GameObject("Glow").AddComponent<Light>();
            l.transform.SetParent(go.transform, false);
            l.type = LightType.Point;
            l.color = new Color(0.3f, 1f, 0.5f);
            l.range = 5f;
            l.intensity = 1.5f;
            var orb = go.AddComponent<HpOrb>();
            orb._basePos = go.transform.position;
        }

        void Update()
        {
            _t += Time.deltaTime;
            transform.position = _basePos + Vector3.up * Mathf.Sin(_t * 3f) * 0.12f;

            var g = Game.Instance;
            if (g == null || g.Player == null || g.Player.IsDead || g.Paused) return;
            Vector3 d = g.Player.transform.position + Vector3.up - transform.position;
            if (d.sqrMagnitude < 1.7f * 1.7f)
            {
                g.Player.Heal(Mathf.RoundToInt(g.Player.MaxHp * 0.15f));
                Sfx.Play(SfxKind.Pickup, transform.position, 0.8f);
                Spark.Burst(transform.position, new Color(0.3f, 1f, 0.5f), 8, 3f);
                Destroy(gameObject);
            }
        }
    }

    /// <summary>Stairway to the next floor; appears after the Floor Guardian falls.</summary>
    public class Stairs : MonoBehaviour
    {
        public static Stairs Spawn(Vector3 pos)
        {
            var go = new GameObject("Stairs");
            go.transform.position = pos;
            var glow = ProcAssets.Unlit(new Color(0.5f, 0.9f, 1f));
            var stone = ProcAssets.Lit(new Color(0.25f, 0.28f, 0.35f));
            // Stepped platform + light beam.
            for (int i = 0; i < 4; i++)
                ProcAssets.Prim(PrimitiveType.Cube, go.transform, new Vector3(0f, 0.12f + i * 0.24f, 0f),
                                new Vector3(3.2f - i * 0.6f, 0.24f, 3.2f - i * 0.6f), stone);
            ProcAssets.Prim(PrimitiveType.Cylinder, go.transform, new Vector3(0f, 3f, 0f), new Vector3(1.2f, 3f, 1.2f), glow);
            var l = new GameObject("Light").AddComponent<Light>();
            l.transform.SetParent(go.transform, false);
            l.transform.localPosition = new Vector3(0f, 2.5f, 0f);
            l.type = LightType.Point;
            l.color = new Color(0.5f, 0.9f, 1f);
            l.range = 22f;
            l.intensity = 3f;
            Sfx.Play(SfxKind.Stairs, pos, 1f);
            return go.AddComponent<Stairs>();
        }

        void Update()
        {
            var g = Game.Instance;
            if (g == null || g.Player == null || g.Player.IsDead || g.Paused) return;
            Vector3 d = g.Player.transform.position - transform.position;
            d.y = 0f;
            if (d.magnitude < 3.4f)
            {
                Hud.Instance.Prompt("Press E  -  ascend to floor " + (g.Floor + 1));
                if (GameInput.InteractPressed) g.NextFloor();
            }
        }
    }
}

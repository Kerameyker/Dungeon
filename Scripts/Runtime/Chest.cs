using Hollow.Core;
using UnityEngine;

namespace Hollow
{
    /// <summary>Treasure chest: press E to open it and get 1-2 good items.</summary>
    public class Chest : MonoBehaviour
    {
        Transform _lid;
        bool _opened;
        int _floor;
        int _seed;
        float _openT;

        public static Chest Spawn(Vector3 pos, float yaw, int floor, int seed)
        {
            var go = new GameObject("Chest");
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            var wood = ProcAssets.Lit(new Color(0.4f, 0.24f, 0.1f), null, 0.1f);
            var gold = ProcAssets.Lit(new Color(0.95f, 0.75f, 0.2f), null, 0.5f, new Color(0.25f, 0.18f, 0.02f));
            ProcAssets.Prim(PrimitiveType.Cube, go.transform, new Vector3(0f, 0.3f, 0f), new Vector3(1.1f, 0.6f, 0.75f), wood, true, "Base");
            ProcAssets.Prim(PrimitiveType.Cube, go.transform, new Vector3(0f, 0.3f, 0.38f), new Vector3(1.14f, 0.12f, 0.04f), gold);
            var lid = new GameObject("Lid").transform;
            lid.SetParent(go.transform, false);
            lid.localPosition = new Vector3(0f, 0.6f, -0.37f);   // hinge at the back
            ProcAssets.Prim(PrimitiveType.Cube, lid, new Vector3(0f, 0.1f, 0.37f), new Vector3(1.1f, 0.22f, 0.75f), wood);
            ProcAssets.Prim(PrimitiveType.Cube, lid, new Vector3(0f, 0.1f, 0.76f), new Vector3(1.14f, 0.24f, 0.04f), gold);
            ProcAssets.Prim(PrimitiveType.Cube, lid, new Vector3(0f, 0.0f, 0.78f), new Vector3(0.14f, 0.2f, 0.06f), gold);   // lock
            var glow = new GameObject("Glow").AddComponent<Light>();
            glow.transform.SetParent(go.transform, false);
            glow.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            glow.type = LightType.Point;
            glow.color = new Color(1f, 0.8f, 0.3f);
            glow.range = 6f;
            glow.intensity = 1.2f;
            var c = go.AddComponent<Chest>();
            c._lid = lid;
            c._floor = floor;
            c._seed = seed;
            return c;
        }

        void Update()
        {
            if (_opened)
            {
                _openT = Mathf.Min(1f, _openT + Time.deltaTime * 3f);
                _lid.localRotation = Quaternion.Euler(-105f * _openT, 0f, 0f);
                return;
            }
            var g = Game.Instance;
            if (g == null || g.Player == null || g.Player.IsDead || g.Paused) return;
            Vector3 d = g.Player.transform.position - transform.position;
            d.y = 0f;
            if (d.magnitude < 2.4f)
            {
                Hud.Instance.Prompt("Press E  -  open chest");
                if (GameInput.InteractPressed) Open();
            }
        }

        void Open()
        {
            _opened = true;
            var game = Game.Instance;
            if (game != null && game.Quests != null) game.Quests.OnChest();
            if (Hud.Instance != null) Hud.Instance.RemoveMapMarkerNear(transform.position, 2.5f);
            Sfx.Play(SfxKind.Pickup, transform.position, 1f);
            Spark.Burst(transform.position + Vector3.up * 0.8f, new Color(1f, 0.85f, 0.3f), 24, 5f, 0.12f, 0.7f);
            var rng = new System.Random(_seed);
            int n = rng.NextDouble() < 0.35 ? 2 : 1;
            for (int i = 0; i < n; i++)
            {
                var item = LootGenerator.RollOf(rng, _floor, LootGenerator.RollSlot(rng), LootGenerator.RollChestRarity(rng));
                Vector2 off = Random.insideUnitCircle.normalized * 1.3f;
                LootDrop.Spawn(transform.position + new Vector3(off.x, 0f, off.y), item);
            }
            var l = GetComponentInChildren<Light>();
            if (l != null) l.enabled = false;
        }
    }

    /// <summary>Healing shrine: press E once to restore 60% of max HP.</summary>
    public class Shrine : MonoBehaviour
    {
        bool _used;
        Material _orbMat;
        Transform _orb;
        Light _light;
        float _t;

        public static Shrine Spawn(Vector3 pos)
        {
            var go = new GameObject("Shrine");
            go.transform.position = pos;
            var stone = ProcAssets.Lit(new Color(0.3f, 0.32f, 0.38f), null, 0.1f);
            ProcAssets.Prim(PrimitiveType.Cylinder, go.transform, new Vector3(0f, 0.15f, 0f), new Vector3(1.4f, 0.15f, 1.4f), stone, true, "Base");
            ProcAssets.Prim(PrimitiveType.Cylinder, go.transform, new Vector3(0f, 0.75f, 0f), new Vector3(0.5f, 0.6f, 0.5f), stone, true, "Pillar");
            var orb = ProcAssets.Prim(PrimitiveType.Sphere, go.transform, new Vector3(0f, 1.6f, 0f), Vector3.one * 0.45f,
                                      ProcAssets.Lit(new Color(0.3f, 1f, 0.7f), null, 0.5f), false, "Orb");
            var l = new GameObject("Glow").AddComponent<Light>();
            l.transform.SetParent(go.transform, false);
            l.transform.localPosition = new Vector3(0f, 1.8f, 0f);
            l.type = LightType.Point;
            l.color = new Color(0.3f, 1f, 0.7f);
            l.range = 9f;
            l.intensity = 1.8f;
            Particles.Flame(go.transform, new Vector3(0f, 1.3f, 0f), new Color(0.4f, 1f, 0.8f), 0.4f);
            var s = go.AddComponent<Shrine>();
            s._orb = orb.transform;
            s._orbMat = orb.GetComponent<Renderer>().sharedMaterial;
            s._light = l;
            return s;
        }

        void Update()
        {
            _t += Time.deltaTime;
            if (_orb != null) _orb.localPosition = new Vector3(0f, 1.6f + Mathf.Sin(_t * 2f) * (_used ? 0.02f : 0.1f), 0f);
            if (_used) return;
            var g = Game.Instance;
            if (g == null || g.Player == null || g.Player.IsDead || g.Paused) return;
            Vector3 d = g.Player.transform.position - transform.position;
            d.y = 0f;
            if (d.magnitude < 2.6f)
            {
                Hud.Instance.Prompt("Press E  -  pray at the shrine (heal 60%)");
                if (GameInput.InteractPressed)
                {
                    _used = true;
                    Hud.Instance.RemoveMapMarkerNear(transform.position, 2.5f);
                    g.Player.Heal(Mathf.RoundToInt(g.Player.MaxHp * 0.6f));
                    Sfx.Play2D(SfxKind.LevelUp, 0.6f);
                    Spark.Burst(g.Player.transform.position + Vector3.up, new Color(0.4f, 1f, 0.8f), 20, 5f, 0.12f, 0.7f);
                    GroundRing.Spawn(transform.position, 0.5f, 4f, new Color(0.4f, 1f, 0.8f, 0.9f), 0.7f);
                    ProcAssets.Tint(_orbMat, new Color(0.15f, 0.2f, 0.2f));
                    if (_light != null) _light.enabled = false;
                }
            }
        }
    }
}

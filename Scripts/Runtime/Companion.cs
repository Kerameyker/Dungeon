using System.Collections.Generic;
using Hollow.Core;
using UnityEngine;

namespace Hollow
{
    /// <summary>
    /// Miri, the fencer who joins after floor 2. She follows the player, fights the nearest awake enemy
    /// and can take over on a "Switch": when a hit staggers an enemy the window opens and G sends her in
    /// for a heavy finisher while the player recovers (cooldowns halved, small heal).
    /// </summary>
    public class Companion : MonoBehaviour
    {
        public const float SwitchWindow = 2.6f;
        public const float SwitchCooldown = 9f;

        public string Name { get { return "Miri"; } }
        public bool SwitchReady { get { return Time.time >= _switchReadyAt; } }
        public bool SwitchWindowOpen { get { return _offered != null && !_offered.IsDead && Time.time < _windowUntil; } }
        public float SwitchCooldownNormalized { get { return SwitchReady ? 0f : Mathf.Clamp01((_switchReadyAt - Time.time) / SwitchCooldown); } }

        CharacterController _cc;
        Transform _visual, _armR, _armL, _legL, _legR, _blade;
        Transform _shadow;
        float _phase, _vy, _attackAt, _swing, _repath;
        float _switchReadyAt, _windowUntil;
        Enemy _offered, _target;
        Enemy _finisherTarget;
        float _finisherAt = -1f;
        readonly List<Cell> _path = new List<Cell>();

        public static Companion Create(Vector3 pos)
        {
            var go = new GameObject("Miri");
            go.transform.position = pos;
            var c = go.AddComponent<Companion>();
            c.Build();
            return c;
        }

        void Build()
        {
            _cc = gameObject.AddComponent<CharacterController>();
            _cc.radius = 0.32f;
            _cc.height = 1.7f;
            _cc.center = new Vector3(0f, 0.85f, 0f);
            _cc.stepOffset = 0.2f;
            _cc.skinWidth = 0.04f;

            var coat = ProcAssets.Lit(new Color(0.75f, 0.2f, 0.28f), null, 0.15f);
            var trim = ProcAssets.Lit(new Color(0.95f, 0.85f, 0.6f), null, 0.4f, new Color(0.3f, 0.25f, 0.1f));
            var dark = ProcAssets.Lit(new Color(0.15f, 0.12f, 0.14f), null, 0.1f);
            var skin = ProcAssets.Lit(new Color(0.92f, 0.75f, 0.64f));
            var hair = ProcAssets.Lit(new Color(0.55f, 0.25f, 0.18f), null, 0.2f);
            var steel = ProcAssets.Lit(new Color(0.85f, 0.92f, 1f), null, 0.85f, new Color(0.15f, 0.25f, 0.3f));

            _visual = new GameObject("Visual").transform;
            _visual.SetParent(transform, false);

            _legL = MakeLeg("LegL", -0.13f, dark);
            _legR = MakeLeg("LegR", 0.13f, dark);

            var body = new GameObject("Body").transform;
            body.SetParent(_visual, false);
            body.localPosition = new Vector3(0f, 0.9f, 0f);
            ProcAssets.Prim(PrimitiveType.Capsule, body, new Vector3(0f, 0.25f, 0f), new Vector3(0.5f, 0.42f, 0.34f), coat, false, "Torso");
            ProcAssets.Prim(PrimitiveType.Cube, body, new Vector3(0f, 0.02f, 0f), new Vector3(0.5f, 0.07f, 0.36f), trim, false, "Belt");
            ProcAssets.Prim(PrimitiveType.Cube, body, new Vector3(0f, -0.2f, 0.02f), new Vector3(0.46f, 0.34f, 0.32f), coat, false, "Skirt");
            ProcAssets.Prim(PrimitiveType.Sphere, body, new Vector3(0f, 0.7f, 0f), Vector3.one * 0.3f, skin, false, "Head");
            ProcAssets.Prim(PrimitiveType.Sphere, body, new Vector3(0f, 0.75f, -0.05f), new Vector3(0.34f, 0.3f, 0.34f), hair, false, "Hair");
            var tail = ProcAssets.Prim(PrimitiveType.Capsule, body, new Vector3(0f, 0.6f, -0.24f), new Vector3(0.1f, 0.22f, 0.1f), hair, false, "Ponytail");
            tail.transform.localRotation = Quaternion.Euler(30f, 0f, 0f);

            _armL = MakeArm("ArmL", -0.3f, body, coat, skin);
            _armR = MakeArm("ArmR", 0.3f, body, coat, skin);

            // rapier in the right hand
            var hand = new GameObject("Weapon").transform;
            hand.SetParent(_armR, false);
            hand.localPosition = new Vector3(0f, -0.42f, 0.05f);
            hand.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ProcAssets.Prim(PrimitiveType.Cube, hand, new Vector3(0f, 0f, 0.55f), new Vector3(0.04f, 0.02f, 1.0f), steel, false, "Blade");
            ProcAssets.Prim(PrimitiveType.Cylinder, hand, new Vector3(0f, 0f, 0.05f), new Vector3(0.22f, 0.015f, 0.22f), trim, false, "Cup");
            _blade = hand;

            ProcAssets.BlobShadow(transform, 1.2f);
            _shadow = transform.Find("BlobShadow");

            var label = new GameObject("Name");
            label.transform.SetParent(transform, false);
            label.transform.localPosition = new Vector3(0f, 2.4f, 0f);
            var tm = label.AddComponent<TextMesh>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tm.font = font;
            tm.text = "Miri";
            tm.fontSize = 56;
            tm.characterSize = 0.06f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = new Color(1f, 0.6f, 0.65f);
            label.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            label.AddComponent<Billboard>();
        }

        Transform MakeLeg(string name, float x, Material mat)
        {
            var hip = new GameObject(name).transform;
            hip.SetParent(_visual, false);
            hip.localPosition = new Vector3(x, 0.62f, 0f);
            ProcAssets.Prim(PrimitiveType.Cube, hip, new Vector3(0f, -0.3f, 0f), new Vector3(0.18f, 0.56f, 0.2f), mat, false, "Leg");
            return hip;
        }

        Transform MakeArm(string name, float x, Transform parent, Material sleeve, Material skin)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = new Vector3(x, 0.42f, 0f);
            ProcAssets.Prim(PrimitiveType.Cube, pivot, new Vector3(0f, -0.2f, 0f), new Vector3(0.13f, 0.4f, 0.14f), sleeve, false, "Arm");
            ProcAssets.Prim(PrimitiveType.Sphere, pivot, new Vector3(0f, -0.42f, 0f), Vector3.one * 0.14f, skin, false, "Hand");
            return pivot;
        }

        // ------------------------------------------------------------------ switch

        /// <summary>Called by the player when a hit staggered an enemy: opens the switch window.</summary>
        public void OfferSwitch(Enemy e)
        {
            if (e == null || e.IsDead) return;
            _offered = e;
            _windowUntil = Time.time + SwitchWindow;
        }

        /// <summary>Miri rushes the staggered enemy for a big hit; the player gets a breather bonus.</summary>
        public bool TrySwitch()
        {
            var p = Game.Instance != null ? Game.Instance.Player : null;
            if (p == null || p.IsDead || !SwitchReady || !SwitchWindowOpen) return false;
            _finisherTarget = _offered;
            _finisherAt = Time.time + 0.35f;
            _switchReadyAt = Time.time + SwitchCooldown;
            _windowUntil = 0f;
            _offered = null;
            // dash in front of the target
            Vector3 to = _finisherTarget.transform.position;
            Vector3 dir = (to - p.transform.position);
            dir.y = 0f;
            dir = dir.sqrMagnitude > 0.01f ? dir.normalized : transform.forward;
            Teleport(to - dir * (_finisherTarget.Radius + 1.1f));
            transform.rotation = Quaternion.LookRotation(dir);
            _swing = 0.35f;
            Sfx.Play2D(SfxKind.Switch);
            Spark.Burst(transform.position + Vector3.up, new Color(1f, 0.5f, 0.6f), 16, 5f, 0.12f, 0.5f);
            GroundRing.Spawn(transform.position, 0.4f, 3f, new Color(1f, 0.55f, 0.65f, 0.9f), 0.5f);
            p.OnSwitchBonus();
            return true;
        }

        public void Teleport(Vector3 pos)
        {
            if (_cc == null) return;
            _cc.enabled = false;
            transform.position = pos;
            _cc.enabled = true;
            _vy = 0f;
            _path.Clear();
        }

        // ------------------------------------------------------------------ update

        void Update()
        {
            var g = Game.Instance;
            if (g == null || g.Player == null || g.Paused) return;
            var p = g.Player;
            float dt = Time.deltaTime;

            if (GameInput.SwitchPressed) TrySwitch();

            if (_finisherAt > 0f && Time.time >= _finisherAt)
            {
                _finisherAt = -1f;
                if (_finisherTarget != null && !_finisherTarget.IsDead)
                {
                    int dmg = CombatMath.Damage(p.Attack, _finisherTarget.Defense, 3.2f, true);
                    _finisherTarget.TakeHit(dmg, true, transform.position, 1.2f);
                    Spark.Burst(_finisherTarget.transform.position + Vector3.up, new Color(1f, 0.6f, 0.7f), 26, 7f, 0.15f, 0.6f);
                    _swing = 0.3f;
                    if (Game.Instance != null && Game.Instance.Rig != null) Game.Instance.Rig.Kick(0.25f);
                }
                _finisherTarget = null;
            }

            Vector3 move = Vector3.zero;
            Vector3 toPlayer = p.transform.position - transform.position;
            toPlayer.y = 0f;
            float pd = toPlayer.magnitude;

            if (pd > 26f) Teleport(p.transform.position - p.transform.forward * 1.5f);

            // target: nearest awake enemy close to the player
            if (_target == null || _target.IsDead || (_target.transform.position - p.transform.position).sqrMagnitude > 14f * 14f) _target = PickTarget(p);

            if (_target != null && !p.IsDead)
            {
                Vector3 td = _target.transform.position - transform.position;
                td.y = 0f;
                float dist = td.magnitude;
                float reach = 1.7f + _target.Radius;
                if (dist > reach) move = Steer(_target.transform.position, dt) * 4.8f;
                else if (Time.time >= _attackAt)
                {
                    _attackAt = Time.time + 1.05f;
                    _swing = 0.28f;
                    int dmg = Mathf.Max(1, CombatMath.Damage(Mathf.RoundToInt(p.Attack * 0.55f), _target.Defense, 1f, false));
                    _target.TakeHit(dmg, false, transform.position, 0f);
                }
                if (td.sqrMagnitude > 0.01f) Face(td, 14f);
            }
            else if (pd > 3.2f)
            {
                move = Steer(p.transform.position, dt) * Mathf.Clamp(pd * 1.2f, 2.5f, 6.5f);
                if (move.sqrMagnitude > 0.01f) Face(move, 10f);
            }
            else if (pd < 1.5f && pd > 0.01f)
            {
                move = -toPlayer.normalized * 2f;
            }

            _vy = _cc.isGrounded ? -1f : _vy - 25f * dt;
            move.y = _vy;
            if (_cc.enabled) _cc.Move(move * dt);

            Animate(dt, new Vector3(move.x, 0f, move.z).magnitude);
        }

        Enemy PickTarget(PlayerController p)
        {
            Enemy best = null;
            float bd = 12f * 12f;
            for (int i = 0; i < Enemy.All.Count; i++)
            {
                var e = Enemy.All[i];
                if (e == null || e.IsDead || !e.Aggro) continue;
                float d = (e.transform.position - p.transform.position).sqrMagnitude;
                if (d < bd) { bd = d; best = e; }
            }
            return best;
        }

        Vector3 Steer(Vector3 goal, float dt)
        {
            Vector3 dir = goal - transform.position;
            dir.y = 0f;
            float dist = dir.magnitude;
            if (dist < 0.01f) return Vector3.zero;
            dir /= dist;
            var g = Game.Instance;
            Vector3 from = transform.position + Vector3.up * 0.9f;
            bool clear = !Physics.Raycast(from, dir, Mathf.Min(dist, 20f), Layers.WorldMask, QueryTriggerInteraction.Ignore);
            if (clear) { _path.Clear(); return dir; }
            _repath -= dt;
            if (_repath <= 0f && g != null && g.Pathfinder != null)
            {
                _repath = 0.5f;
                var a = DungeonBuilder.ToCell(transform.position);
                var b = DungeonBuilder.ToCell(goal);
                g.Pathfinder.FindPath(a.X, a.Y, b.X, b.Y, _path);
            }
            while (_path.Count > 0)
            {
                Vector3 wp = DungeonBuilder.ToWorld(_path[0]);
                Vector3 d = wp - transform.position;
                d.y = 0f;
                if (d.magnitude < 0.8f) { _path.RemoveAt(0); continue; }
                return d.normalized;
            }
            return dir;
        }

        void Face(Vector3 dir, float speed)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), speed * Time.deltaTime);
        }

        void Animate(float dt, float speed)
        {
            _phase += dt * (2f + speed * 1.6f);
            float move = Mathf.Clamp01(speed / 4f);
            float sw = Mathf.Sin(_phase) * 45f * move;
            _legL.localRotation = Quaternion.Euler(sw, 0f, 0f);
            _legR.localRotation = Quaternion.Euler(-sw, 0f, 0f);
            _armL.localRotation = Quaternion.Euler(-sw * 0.8f, 0f, 5f);
            if (_swing > 0f)
            {
                _swing -= dt;
                float t = 1f - Mathf.Clamp01(_swing / 0.3f);
                _armR.localRotation = Quaternion.Euler(Mathf.Lerp(-110f, 30f, t), 0f, 0f);
            }
            else _armR.localRotation = Quaternion.Euler(-20f + sw * 0.4f, 0f, -5f);
            _visual.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Cos(_phase)) * 0.05f * move, 0f);
            if (_shadow != null) _shadow.position = new Vector3(transform.position.x, 0.04f, transform.position.z);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using Hollow.Core;
using UnityEngine;

namespace Hollow
{
    /// <summary>Enemy with self-built visuals, grid pathfinding, telegraphed attacks and a world-space HP bar.</summary>
    public class Enemy : MonoBehaviour
    {
        public static readonly List<Enemy> All = new List<Enemy>();

        public EnemyDef Def;
        public int Hp, MaxHp, Attack, Defense;
        public float Radius;
        public bool IsDead;
        public int Floor;
        public bool IsElite;
        public int BossBars = 1;
        public string BossTitle = "";
        public int Phase;
        public float Scale = 1f;
        public string DisplayName = "";

        enum State { Idle, Chase, Windup, Recover, Stunned }
        State _state = State.Idle;

        CharacterController _cc;
        Transform _visual;
        Transform _weapon;
        readonly List<Material> _bodyMats = new List<Material>();
        readonly List<Color> _bodyBase = new List<Color>();
        Transform _barRoot;
        Transform _barFill;
        const float BarWidth = 1.3f;

        float _attackTimer;
        float _stateTimer;
        float _vy;
        float _flashUntil;
        Vector3 _knock;
        float _knockTimer;
        Vector3 _lunge;
        int _attackCount;
        bool _slamNext;
        GameObject _telegraph;
        Vector3 _attackDir;
        bool _chargeNext;
        bool _chargeHit;
        bool _barrageNext;
        float _immuneUntil;
        float _speedMult = 1f, _cdMult = 1f;
        float _blinkAt;
        Transform _orbit;
        float Speed { get { return Def.Speed * _speedMult; } }
        int Period { get { return Phase >= 1 ? 2 : 3; } }

        // animation
        readonly List<Transform> _legs = new List<Transform>();
        readonly List<float> _legSign = new List<float>();
        Transform _shadow;
        float _phase, _hitTilt;
        Quaternion _weaponRest = Quaternion.Euler(20f, 0f, 0f);

        readonly List<Cell> _path = new List<Cell>();
        float _repathTimer;

        public float HeadHeight { get { return 2.0f * Scale; } }
        public bool Aggro { get { return _state != State.Idle; } }

        // ------------------------------------------------------------------ creation

        public static Enemy Create(EnemyKind kind, int floor, Vector3 pos, bool elite = false)
        {
            var def = EnemyCatalog.Get(kind);
            var go = new GameObject("Enemy_" + def.Name.Replace(" ", ""));
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            var e = go.AddComponent<Enemy>();
            e.Def = def;
            e.Floor = floor;
            e.IsElite = elite && !def.IsBoss;
            e.Scale = def.Scale * (e.IsElite ? 1.25f : 1f);
            e.DisplayName = (e.IsElite ? "Elite " : "") + def.Name;
            float scale = Progression.EnemyScale(floor);
            e.BossBars = def.IsBoss ? BossInfo.Bars(kind, floor) : 1;
            e.BossTitle = def.IsBoss ? BossInfo.Title(kind, floor) : "";
            e.MaxHp = Mathf.RoundToInt(def.BaseHp * scale * (e.IsElite ? 2.5f : 1f) * (e.BossBars > 2 ? 1.5f : 1f));
            e.Hp = e.MaxHp;
            e.Attack = Mathf.RoundToInt(def.BaseAttack * scale * (e.IsElite ? 1.3f : 1f));
            e.Defense = def.Defense + (floor - 1);
            e.Radius = Mathf.Max(0.4f, 0.45f * e.Scale);
            e.Build();
            All.Add(e);
            return e;
        }

        void OnDestroy()
        {
            All.Remove(this);
            if (_telegraph != null) Destroy(_telegraph);
        }

        Material Body(Color c, Color? emission = null, bool flashable = true)
        {
            var m = ProcAssets.Lit(c, null, 0.15f, emission);
            if (flashable) { _bodyMats.Add(m); _bodyBase.Add(c); }
            return m;
        }

        /// <summary>Leg on a hip pivot so it can swing while walking. The cube hangs below the pivot.</summary>
        void AddLeg(Vector3 hip, Vector3 size, Material m, float sign)
        {
            var pivot = new GameObject("Leg").transform;
            pivot.SetParent(_visual, false);
            pivot.localPosition = hip;
            ProcAssets.Prim(PrimitiveType.Cube, pivot, new Vector3(0f, -size.y * 0.5f, 0f), size, m);
            _legs.Add(pivot);
            _legSign.Add(sign);
        }

        void Build()
        {
            _cc = gameObject.AddComponent<CharacterController>();
            _cc.radius = Radius;
            _cc.height = Mathf.Max(1.8f * Scale, Radius * 2f + 0.1f);
            _cc.center = new Vector3(0f, _cc.height * 0.5f, 0f);
            _cc.stepOffset = 0.2f;
            _cc.skinWidth = 0.05f;

            _visual = new GameObject("Visual").transform;
            _visual.SetParent(transform, false);
            _visual.localScale = Vector3.one * Scale;

            var eyes = ProcAssets.Unlit(new Color(1f, 0.15f, 0.1f));

            switch (Def.Kind)
            {
                case EnemyKind.FrenzyBoar:
                {
                    var fur = Body(new Color(0.35f, 0.2f, 0.12f));
                    var tusk = Body(new Color(0.95f, 0.92f, 0.8f), null, false);
                    ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(0f, 0.6f, 0f), new Vector3(0.9f, 0.75f, 1.5f), fur);
                    ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(0f, 0.6f, 0.95f), new Vector3(0.6f, 0.55f, 0.6f), fur);
                    ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(0f, 0.95f, -0.2f), new Vector3(0.3f, 0.2f, 0.9f), Body(new Color(0.22f, 0.12f, 0.08f)));   // mane
                    ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(0.22f, 0.5f, 1.3f), new Vector3(0.07f, 0.07f, 0.3f), tusk);
                    ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(-0.22f, 0.5f, 1.3f), new Vector3(0.07f, 0.07f, 0.3f), tusk);
                    ProcAssets.Prim(PrimitiveType.Sphere, _visual, new Vector3(0.18f, 0.78f, 1.22f), Vector3.one * 0.09f, eyes);
                    ProcAssets.Prim(PrimitiveType.Sphere, _visual, new Vector3(-0.18f, 0.78f, 1.22f), Vector3.one * 0.09f, eyes);
                    for (int i = 0; i < 4; i++)
                        AddLeg(new Vector3(i % 2 == 0 ? -0.3f : 0.3f, 0.35f, i < 2 ? -0.5f : 0.5f), new Vector3(0.2f, 0.35f, 0.2f), fur, (i % 2 == 0) == (i < 2) ? 1f : -1f);
                    break;
                }
                case EnemyKind.BoneSoldier:
                {
                    var bone = Body(new Color(0.85f, 0.82f, 0.7f));
                    var dark = Body(new Color(0.15f, 0.15f, 0.18f), null, false);
                    ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(0f, 1.0f, 0f), new Vector3(0.6f, 0.9f, 0.32f), bone);
                    for (int i = 0; i < 3; i++)   // rib bars
                        ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(0f, 0.85f + i * 0.2f, 0.17f), new Vector3(0.52f, 0.05f, 0.04f), dark);
                    AddLeg(new Vector3(-0.15f, 0.6f, 0f), new Vector3(0.16f, 0.6f, 0.16f), bone, 1f);
                    AddLeg(new Vector3(0.15f, 0.6f, 0f), new Vector3(0.16f, 0.6f, 0.16f), bone, -1f);
                    ProcAssets.Prim(PrimitiveType.Sphere, _visual, new Vector3(0f, 1.7f, 0f), Vector3.one * 0.42f, bone);
                    ProcAssets.Prim(PrimitiveType.Sphere, _visual, new Vector3(0.1f, 1.72f, 0.17f), Vector3.one * 0.09f, eyes);
                    ProcAssets.Prim(PrimitiveType.Sphere, _visual, new Vector3(-0.1f, 1.72f, 0.17f), Vector3.one * 0.09f, eyes);
                    ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(-0.42f, 1.15f, 0f), new Vector3(0.12f, 0.6f, 0.12f), bone);   // free arm
                    _weapon = new GameObject("Weapon").transform;
                    _weapon.SetParent(_visual, false);
                    _weapon.localPosition = new Vector3(0.42f, 1.1f, 0.1f);
                    _weapon.localRotation = _weaponRest;
                    ProcAssets.Prim(PrimitiveType.Cube, _weapon, new Vector3(0f, 0f, 0.55f), new Vector3(0.08f, 0.04f, 1.1f), dark);
                    break;
                }
                case EnemyKind.SkeletonArcher:
                {
                    var bone = Body(new Color(0.8f, 0.78f, 0.66f));
                    var cloth = Body(new Color(0.25f, 0.12f, 0.35f));
                    var wood = Body(new Color(0.4f, 0.26f, 0.12f), null, false);
                    ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(0f, 1.0f, 0f), new Vector3(0.5f, 0.85f, 0.28f), bone);
                    ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(0f, 1.25f, 0.02f), new Vector3(0.58f, 0.45f, 0.34f), cloth);    // hooded cloak
                    AddLeg(new Vector3(-0.12f, 0.6f, 0f), new Vector3(0.13f, 0.6f, 0.13f), bone, 1f);
                    AddLeg(new Vector3(0.12f, 0.6f, 0f), new Vector3(0.13f, 0.6f, 0.13f), bone, -1f);
                    ProcAssets.Prim(PrimitiveType.Sphere, _visual, new Vector3(0f, 1.68f, 0f), Vector3.one * 0.38f, bone);
                    ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(0f, 1.78f, -0.05f), new Vector3(0.46f, 0.3f, 0.42f), cloth);     // hood
                    ProcAssets.Prim(PrimitiveType.Sphere, _visual, new Vector3(0.08f, 1.68f, 0.15f), Vector3.one * 0.08f, eyes);
                    ProcAssets.Prim(PrimitiveType.Sphere, _visual, new Vector3(-0.08f, 1.68f, 0.15f), Vector3.one * 0.08f, eyes);
                    // Bow held out front (drawn back in the windup by _weapon rotation).
                    _weapon = new GameObject("Bow").transform;
                    _weapon.SetParent(_visual, false);
                    _weapon.localPosition = new Vector3(0.35f, 1.15f, 0.25f);
                    _weapon.localRotation = Quaternion.identity;
                    ProcAssets.Prim(PrimitiveType.Cube, _weapon, new Vector3(0f, 0.0f, 0.1f), new Vector3(0.05f, 0.9f, 0.05f), wood);
                    var tipT = ProcAssets.Prim(PrimitiveType.Cube, _weapon, new Vector3(0f, 0.47f, 0f), new Vector3(0.05f, 0.25f, 0.05f), wood);
                    var tipB = ProcAssets.Prim(PrimitiveType.Cube, _weapon, new Vector3(0f, -0.47f, 0f), new Vector3(0.05f, 0.25f, 0.05f), wood);
                    tipT.transform.localRotation = Quaternion.Euler(-25f, 0f, 0f);
                    tipB.transform.localRotation = Quaternion.Euler(25f, 0f, 0f);
                    ProcAssets.Prim(PrimitiveType.Cube, _weapon, new Vector3(0f, 0f, -0.07f), new Vector3(0.015f, 1.0f, 0.015f), ProcAssets.Unlit(new Color(0.9f, 0.9f, 0.8f)));
                    _weaponRest = Quaternion.identity;
                    break;
                }
                case EnemyKind.StoneGolem:
                {
                    var stone = Body(new Color(0.35f, 0.4f, 0.42f));
                    var moss = Body(new Color(0.2f, 0.4f, 0.22f));
                    var glow = ProcAssets.Unlit(new Color(1f, 0.55f, 0.1f));
                    ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(0f, 1.0f, 0f), new Vector3(1.1f, 1.15f, 0.8f), stone);
                    ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(0f, 1.62f, 0.05f), new Vector3(0.9f, 0.18f, 0.7f), moss);
                    ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(0f, 1.8f, 0.05f), new Vector3(0.5f, 0.45f, 0.5f), stone);
                    ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(0.2f, 1.82f, 0.31f), new Vector3(0.12f, 0.08f, 0.04f), glow);
                    ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(-0.2f, 1.82f, 0.31f), new Vector3(0.12f, 0.08f, 0.04f), glow);
                    ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(0f, 1.05f, 0.41f), new Vector3(0.3f, 0.3f, 0.04f), glow);   // glowing core
                    AddLeg(new Vector3(-0.35f, 0.6f, 0f), new Vector3(0.4f, 0.6f, 0.45f), stone, 1f);
                    AddLeg(new Vector3(0.35f, 0.6f, 0f), new Vector3(0.4f, 0.6f, 0.45f), stone, -1f);
                    _weapon = new GameObject("Arms").transform;
                    _weapon.SetParent(_visual, false);
                    _weapon.localPosition = new Vector3(0f, 1.35f, 0f);
                    ProcAssets.Prim(PrimitiveType.Cube, _weapon, new Vector3(-0.8f, -0.1f, 0.1f), new Vector3(0.4f, 1.0f, 0.4f), stone);
                    ProcAssets.Prim(PrimitiveType.Cube, _weapon, new Vector3(0.8f, -0.1f, 0.1f), new Vector3(0.4f, 1.0f, 0.4f), stone);
                    break;
                }
                case EnemyKind.CrimsonKnight:
                {
                    var armor = Body(new Color(0.55f, 0.07f, 0.09f), new Color(0.06f, 0f, 0f));
                    var dark = Body(new Color(0.12f, 0.1f, 0.12f));
                    var trim = Body(new Color(0.8f, 0.65f, 0.25f), null, false);
                    var blade = Body(new Color(0.85f, 0.88f, 0.95f), null, false);
                    var glow = ProcAssets.Unlit(new Color(1f, 0.85f, 0.3f));
                    ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(0f, 1.05f, 0f), new Vector3(0.85f, 1.1f, 0.55f), armor);
                    ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(0f, 0.55f, 0f), new Vector3(0.7f, 0.2f, 0.5f), dark);          // belt
                    ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(0f, 1.1f, 0.3f), new Vector3(0.22f, 0.8f, 0.04f), trim);
                    ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(-0.6f, 1.5f, 0f), new Vector3(0.45f, 0.28f, 0.5f), dark);       // pauldrons
                    ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(0.6f, 1.5f, 0f), new Vector3(0.45f, 0.28f, 0.5f), dark);
                    AddLeg(new Vector3(-0.22f, 0.65f, 0f), new Vector3(0.32f, 0.65f, 0.34f), dark, 1f);
                    AddLeg(new Vector3(0.22f, 0.65f, 0f), new Vector3(0.32f, 0.65f, 0.34f), dark, -1f);
                    ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(0f, 1.82f, 0f), new Vector3(0.5f, 0.55f, 0.5f), armor);         // helm
                    ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(0f, 1.82f, 0.26f), new Vector3(0.38f, 0.06f, 0.04f), glow);     // visor slit
                    ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(0f, 2.22f, -0.1f), new Vector3(0.08f, 0.35f, 0.5f), trim);      // crest
                    var cape = ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(0f, 1.0f, -0.36f), new Vector3(0.9f, 1.4f, 0.05f), Body(new Color(0.35f, 0.03f, 0.05f)));
                    cape.transform.localRotation = Quaternion.Euler(6f, 0f, 0f);
                    _weapon = new GameObject("Weapon").transform;
                    _weapon.SetParent(_visual, false);
                    _weapon.localPosition = new Vector3(0.72f, 1.15f, 0.1f);
                    _weapon.localRotation = _weaponRest;
                    ProcAssets.Prim(PrimitiveType.Cube, _weapon, new Vector3(0f, 0f, 0.95f), new Vector3(0.14f, 0.05f, 1.9f), blade);
                    ProcAssets.Prim(PrimitiveType.Cube, _weapon, new Vector3(0f, 0f, 0.05f), new Vector3(0.55f, 0.08f, 0.1f), trim);
                    // Small shield on the left arm.
                    ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(-0.72f, 1.05f, 0.2f), new Vector3(0.1f, 0.8f, 0.6f), dark);
                    ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(-0.78f, 1.05f, 0.2f), new Vector3(0.04f, 0.5f, 0.3f), trim);
                    break;
                }
                case EnemyKind.Architect:
                {
                    var robe = Body(new Color(0.92f, 0.9f, 0.82f), new Color(0.08f, 0.06f, 0.02f));
                    var dark = Body(new Color(0.08f, 0.08f, 0.13f));
                    var gold = Body(new Color(1f, 0.82f, 0.3f), null, false);
                    var glow = ProcAssets.Unlit(new Color(1f, 0.85f, 0.4f));
                    ProcAssets.Prim(PrimitiveType.Cylinder, _visual, new Vector3(0f, 0.75f, 0f), new Vector3(1.25f, 0.75f, 1.25f), robe);
                    ProcAssets.Prim(PrimitiveType.Cylinder, _visual, new Vector3(0f, 1.55f, 0f), new Vector3(0.85f, 0.55f, 0.85f), robe);
                    ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(0f, 1.3f, 0.42f), new Vector3(0.22f, 1.5f, 0.05f), gold);
                    ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(-0.62f, 1.9f, 0f), new Vector3(0.5f, 0.2f, 0.5f), gold);
                    ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(0.62f, 1.9f, 0f), new Vector3(0.5f, 0.2f, 0.5f), gold);
                    ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(-0.72f, 1.35f, 0.05f), new Vector3(0.28f, 0.9f, 0.28f), robe);
                    ProcAssets.Prim(PrimitiveType.Sphere, _visual, new Vector3(0f, 2.25f, 0f), new Vector3(0.52f, 0.62f, 0.5f), gold);         // mask
                    ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(0f, 2.3f, -0.05f), new Vector3(0.62f, 0.6f, 0.55f), dark);        // hood behind the mask
                    ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(0.11f, 2.28f, 0.25f), new Vector3(0.12f, 0.04f, 0.04f), glow);
                    ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(-0.11f, 2.28f, 0.25f), new Vector3(0.12f, 0.04f, 0.04f), glow);
                    var halo = ProcAssets.Prim(PrimitiveType.Cylinder, _visual, new Vector3(0f, 2.35f, -0.45f), new Vector3(1.7f, 0.02f, 1.7f), glow, false, "Halo");
                    halo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    _orbit = new GameObject("Orbit").transform;
                    _orbit.SetParent(_visual, false);
                    _orbit.localPosition = new Vector3(0f, 1.5f, 0f);
                    for (int i = 0; i < 6; i++)
                    {
                        float a = i * Mathf.PI * 2f / 6f;
                        var bl = ProcAssets.Prim(PrimitiveType.Cube, _orbit, new Vector3(Mathf.Cos(a) * 1.9f, 0f, Mathf.Sin(a) * 1.9f), new Vector3(0.1f, 0.9f, 0.06f), Body(new Color(0.85f, 0.9f, 1f), null, false));
                        bl.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 15f);
                    }
                    _weapon = new GameObject("Scepter").transform;
                    _weapon.SetParent(_visual, false);
                    _weapon.localPosition = new Vector3(0.78f, 1.3f, 0.1f);
                    _weapon.localRotation = _weaponRest;
                    ProcAssets.Prim(PrimitiveType.Cube, _weapon, new Vector3(0f, 0f, 1.0f), new Vector3(0.1f, 0.1f, 2.1f), gold);
                    ProcAssets.Prim(PrimitiveType.Sphere, _weapon, new Vector3(0f, 0f, 2.1f), Vector3.one * 0.32f, glow);
                    break;
                }
                default: // FloorGuardian
                {
                    var armor = Body(new Color(0.22f, 0.08f, 0.16f), new Color(0.05f, 0f, 0.02f));
                    var trim = Body(new Color(0.75f, 0.6f, 0.2f), null, false);
                    var blade = Body(new Color(0.75f, 0.8f, 0.9f), null, false);
                    var glow = ProcAssets.Unlit(new Color(1f, 0.2f, 0.15f));
                    ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(0f, 1.1f, 0f), new Vector3(1.0f, 1.2f, 0.65f), armor);
                    ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(0f, 1.15f, 0.34f), new Vector3(0.3f, 0.9f, 0.05f), trim);
                    ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(-0.7f, 1.55f, 0f), new Vector3(0.5f, 0.3f, 0.5f), trim);
                    ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(0.7f, 1.55f, 0f), new Vector3(0.5f, 0.3f, 0.5f), trim);
                    AddLeg(new Vector3(-0.28f, 0.7f, 0f), new Vector3(0.4f, 0.7f, 0.4f), armor, 1f);
                    AddLeg(new Vector3(0.28f, 0.7f, 0f), new Vector3(0.4f, 0.7f, 0.4f), armor, -1f);
                    ProcAssets.Prim(PrimitiveType.Sphere, _visual, new Vector3(0f, 1.95f, 0f), Vector3.one * 0.55f, armor);
                    ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(0.15f, 1.98f, 0.24f), new Vector3(0.14f, 0.06f, 0.05f), glow);
                    ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(-0.15f, 1.98f, 0.24f), new Vector3(0.14f, 0.06f, 0.05f), glow);
                    var hornL = ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(-0.28f, 2.32f, 0f), new Vector3(0.1f, 0.5f, 0.1f), trim);
                    var hornR = ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(0.28f, 2.32f, 0f), new Vector3(0.1f, 0.5f, 0.1f), trim);
                    hornL.transform.localRotation = Quaternion.Euler(0f, 0f, 20f);
                    hornR.transform.localRotation = Quaternion.Euler(0f, 0f, -20f);
                    _weapon = new GameObject("Weapon").transform;
                    _weapon.SetParent(_visual, false);
                    _weapon.localPosition = new Vector3(0.75f, 1.2f, 0.1f);
                    _weapon.localRotation = _weaponRest;
                    ProcAssets.Prim(PrimitiveType.Cube, _weapon, new Vector3(0f, 0f, 1.1f), new Vector3(0.22f, 0.07f, 2.2f), blade);
                    ProcAssets.Prim(PrimitiveType.Cube, _weapon, new Vector3(0f, 0f, 0.05f), new Vector3(0.6f, 0.1f, 0.12f), trim);
                    break;
                }
            }

            if (!IsElite)
            {
                var tint = BiomeCatalog.ForFloor(Floor).EnemyTint;
                for (int i = 0; i < _bodyMats.Count; i++)
                {
                    var b = _bodyBase[i];
                    _bodyBase[i] = new Color(Mathf.Clamp01(b.r * tint[0]), Mathf.Clamp01(b.g * tint[1]), Mathf.Clamp01(b.b * tint[2]), b.a);
                    ProcAssets.Tint(_bodyMats[i], _bodyBase[i]);
                }
            }
            if (IsElite)
            {
                var gold = new Color(1f, 0.8f, 0.25f);
                for (int i = 0; i < _bodyMats.Count; i++)
                {
                    _bodyBase[i] = Color.Lerp(_bodyBase[i], gold, 0.5f);
                    ProcAssets.Tint(_bodyMats[i], _bodyBase[i]);
                }
                var glowMat = ProcAssets.Unlit(gold);
                ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(0f, 2.5f, 0f), new Vector3(0.28f, 0.28f, 0.28f), glowMat, false, "EliteGem")
                    .transform.localRotation = Quaternion.Euler(45f, 45f, 0f);
            }

            _shadow = ProcAssets.BlobShadow(transform, 1.6f * Scale);
            if (!Def.IsBoss) BuildHpBar();
        }

        void BuildHpBar()
        {
            _barRoot = new GameObject("HpBar").transform;
            _barRoot.SetParent(transform, false);
            _barRoot.localPosition = new Vector3(0f, HeadHeight + (IsElite ? 0.9f : 0.5f), 0f);
            _barRoot.gameObject.AddComponent<Billboard>();

            var bg = ProcAssets.Prim(PrimitiveType.Quad, _barRoot, Vector3.zero, new Vector3(BarWidth + 0.08f, 0.16f, 1f),
                                     ProcAssets.Unlit(new Color(0.05f, 0.05f, 0.08f)));
            var fill = ProcAssets.Prim(PrimitiveType.Quad, _barRoot, new Vector3(0f, 0f, -0.01f), new Vector3(BarWidth, 0.1f, 1f),
                                       ProcAssets.Unlit(IsElite ? new Color(1f, 0.8f, 0.2f) : new Color(0.95f, 0.25f, 0.25f)));
            _barFill = fill.transform;
            _barRoot.gameObject.SetActive(false);   // shown once the enemy has noticed the player
        }

        void UpdateBar()
        {
            if (_barFill == null) return;
            float r = Mathf.Clamp01(Hp / (float)MaxHp);
            _barFill.localScale = new Vector3(BarWidth * r, 0.1f, 1f);
            _barFill.localPosition = new Vector3(-BarWidth * 0.5f + BarWidth * r * 0.5f, 0f, -0.01f);
        }

        // ------------------------------------------------------------------ AI

        void Update()
        {
            if (IsDead) return;
            float dt = Time.deltaTime;
            if (_attackTimer > 0f) _attackTimer -= dt;

            // hit flash
            SetFlash(Time.time < _flashUntil ? new Color(0.7f, 0.7f, 0.7f) : (_state == State.Windup ? new Color(0.9f, 0.1f, 0.05f) : Color.black));

            var g = Game.Instance;
            var player = g != null ? g.Player : null;
            Vector3 planar = Vector3.zero;

            if (_knockTimer > 0f)
            {
                _knockTimer -= dt;
                planar += _knock;
            }

            if (player != null && !player.IsDead)
            {
                Vector3 toP = player.transform.position - transform.position;
                toP.y = 0f;
                float dist = toP.magnitude;
                Vector3 dirToP = dist > 0.001f ? toP / dist : transform.forward;

                switch (_state)
                {
                    case State.Idle:
                        if (dist < (Def.IsBoss ? 18f : 13f)) Alert();
                        break;

                    case State.Chase:
                    {
                        if (Def.Ranged)
                        {
                            bool los = !Physics.Linecast(transform.position + Vector3.up * 1.3f, player.transform.position + Vector3.up * 1.2f,
                                                         Layers.WorldMask, QueryTriggerInteraction.Ignore);
                            if (los && dist <= Def.AttackRange && _attackTimer <= 0f) { StartWindup(dirToP); break; }
                            if (los && dist < 6.5f)
                            {
                                planar -= dirToP * Speed * 1.15f;   // kite away
                                Turn(dirToP, 10f);
                            }
                            else if (!los || dist > 11f)
                            {
                                Vector3 md = ChooseMoveDir(player.transform.position, dirToP, dist, dt);
                                planar += md * Speed;
                                Turn(md, 10f);
                            }
                            else Turn(dirToP, 8f);   // hold position and aim
                            break;
                        }
                        float reach = Def.AttackRange + Radius * 0.5f + PlayerController.Radius;
                        if (Def.Kind == EnemyKind.Architect && _attackTimer <= 0f)
                        {
                            if (Phase >= 1 && dist > 10f && Time.time >= _blinkAt) { Blink(player, dirToP); break; }
                            if ((_attackCount + 1) % 3 == 2 && dist < 15f && dist > 4f) { StartWindup(dirToP); break; }
                        }
                        if (dist <= reach && _attackTimer <= 0f)
                        {
                            StartWindup(dirToP);
                            break;
                        }
                        // The charging boss opens with a dash from a distance.
                        if (Def.Charge && _attackTimer <= 0f && dist > 5f && dist < 13f && _attackCount % Period == Period - 1)
                        {
                            StartWindup(dirToP);
                            break;
                        }
                        Vector3 moveDir = ChooseMoveDir(player.transform.position, dirToP, dist, dt);
                        if (dist > reach * 0.85f) planar += moveDir * Speed;
                        Turn(moveDir, 10f);
                        break;
                    }

                    case State.Windup:
                        _stateTimer -= dt;
                        Turn(dirToP, _slamNext ? 2f : (_chargeNext ? 3f : 6f));   // track the player while telegraphing
                        _attackDir = transform.forward;
                        if (_chargeNext && _telegraph != null)
                        {
                            Vector3 f = transform.forward;
                            _telegraph.transform.position = transform.position + f * 6.5f + Vector3.up * 0.05f;
                            _telegraph.transform.rotation = Quaternion.LookRotation(f);
                        }
                        AnimateWeaponWindup();
                        if (_stateTimer <= 0f) DoAttack(player);
                        break;

                    case State.Recover:
                        _stateTimer -= dt;
                        if (_lunge.sqrMagnitude > 0.01f) planar += _lunge;
                        if (_chargeHit && dist < 2.1f + Radius)
                        {
                            _chargeHit = false;
                            player.Hurt(CombatMath.Damage(Attack, player.Defense, 1.6f, false), transform.position);
                            Spark.Burst(player.transform.position + Vector3.up, new Color(1f, 0.4f, 0.2f), 14, 6f);
                        }
                        if (_stateTimer <= 0f) { _lunge = Vector3.zero; _chargeHit = false; _state = State.Chase; }
                        break;

                    case State.Stunned:
                        _stateTimer -= dt;
                        if (_stateTimer <= 0f) _state = State.Chase;
                        break;
                }
            }

            else if (_telegraph != null) { Destroy(_telegraph); _telegraph = null; }

            if (_cc.enabled)
            {
                _vy = _cc.isGrounded ? -1f : _vy - 25f * dt;
                _cc.Move((planar + Vector3.up * _vy) * dt);
            }
            Animate(dt);
        }

        /// <summary>Walk cycle, bob, attack lean, hit reaction, stun wobble and blob shadow.</summary>
        void Animate(float dt)
        {
            if (_visual == null) return;
            Vector3 v = _cc.velocity;
            v.y = 0f;
            float speed = v.magnitude;
            float move = Mathf.Clamp01(speed / 2.5f);
            _phase += dt * (3f + speed * (Def.Kind == EnemyKind.StoneGolem ? 1.1f : 1.7f));
            float sw = Mathf.Sin(_phase);
            float amp = (Def.Kind == EnemyKind.StoneGolem ? 25f : 42f) * move;
            for (int i = 0; i < _legs.Count; i++)
                _legs[i].localRotation = Quaternion.Euler(sw * amp * _legSign[i], 0f, 0f);

            _hitTilt = Mathf.MoveTowards(_hitTilt, 0f, dt * 5f);
            float lean = speed > 0.5f ? 5f : 0f;
            float bob = Mathf.Abs(Mathf.Cos(_phase)) * 0.06f * move;
            float roll = 0f;
            if (_state == State.Windup) lean = -14f;                      // rear back
            else if (_state == State.Recover) lean = 18f;                 // lunge forward
            else if (_state == State.Stunned) { lean = -8f; roll = Mathf.Sin(Time.time * 30f) * 6f; }
            else if (_state == State.Idle) { bob = Mathf.Sin(Time.time * 1.6f + _phase * 0.01f) * 0.02f; lean = 0f; }
            lean -= _hitTilt * 16f;

            var rot = Quaternion.Euler(lean, 0f, roll);
            _visual.localRotation = Quaternion.Slerp(_visual.localRotation, rot, Mathf.Min(1f, 14f * dt));
            float hover = Def.Kind == EnemyKind.Architect ? 0.45f + Mathf.Sin(Time.time * 1.7f) * 0.15f : 0f;
            _visual.localPosition = new Vector3(0f, bob * Scale + hover, 0f);
            if (_orbit != null) _orbit.Rotate(0f, (60f + 40f * Phase) * dt, 0f, Space.Self);

            if (_shadow != null) _shadow.position = new Vector3(transform.position.x, 0.04f, transform.position.z);
        }

        void Blink(PlayerController player, Vector3 dirToP)
        {
            _blinkAt = Time.time + 7f;
            Vector3 target = player.transform.position - dirToP * 5f;
            if (Physics.Linecast(player.transform.position + Vector3.up, target + Vector3.up, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return;
            Spark.Burst(transform.position + Vector3.up * 1.5f, new Color(1f, 0.85f, 0.4f), 24, 6f, 0.15f, 0.6f);
            _cc.enabled = false;
            transform.position = new Vector3(target.x, transform.position.y, target.z);
            _cc.enabled = true;
            Spark.Burst(transform.position + Vector3.up * 1.5f, new Color(1f, 0.85f, 0.4f), 24, 6f, 0.15f, 0.6f);
            Sfx.Play(SfxKind.Switch, transform.position, 0.8f);
        }

        void EnterPhase(int newPhase)
        {
            Phase = newPhase;
            _immuneUntil = Time.time + 1.5f;
            _state = State.Stunned;
            _stateTimer = 1.5f;
            _lunge = Vector3.zero;
            _chargeHit = false;
            _chargeNext = false;
            _barrageNext = false;
            _slamNext = false;
            if (_telegraph != null) { Destroy(_telegraph); _telegraph = null; }
            if (_weapon != null) _weapon.localRotation = _weaponRest;
            _speedMult = 1f + 0.10f * Phase;
            _cdMult = Mathf.Max(0.6f, 1f - 0.08f * Phase);
            Sfx.Play(SfxKind.PhaseBreak, transform.position, 1f);
            Sfx.Play(SfxKind.BossRoar, transform.position, 0.8f);
            GroundRing.Spawn(transform.position, 1f, 9f, new Color(1f, 0.85f, 0.5f, 0.9f), 0.7f);
            Spark.Burst(transform.position + Vector3.up * Scale, new Color(1f, 0.85f, 0.5f), 40, 8f, 0.18f, 0.8f);
            if (Game.Instance != null && Game.Instance.Rig != null) Game.Instance.Rig.Kick(0.5f);
            if (Hud.Instance != null) Hud.Instance.Toast(DisplayName + " is enraged!", 2f);
            if (Phase >= 2) SummonAdds(2);
        }

        void SummonAdds(int n)
        {
            for (int i = 0; i < n; i++)
            {
                Vector3 pos = transform.position + Vector3.up * 0.05f;
                for (int tries = 0; tries < 6; tries++)
                {
                    float a = Random.value * Mathf.PI * 2f;
                    Vector3 cand = transform.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 3.2f + Vector3.up * 0.05f;
                    var gm = Game.Instance;
                    if (gm == null || gm.Layout == null) { pos = cand; break; }
                    var cell = DungeonBuilder.ToCell(cand);
                    if (gm.Layout.IsFloor(cell.X, cell.Y)) { pos = cand; break; }
                }
                var add = Create(Def.Kind == EnemyKind.CrimsonKnight ? EnemyKind.SkeletonArcher : EnemyKind.BoneSoldier, Floor, pos);
                add.Alert();
                Spark.Burst(pos + Vector3.up, new Color(0.7f, 0.5f, 1f), 12, 4f, 0.12f, 0.5f);
            }
        }

        void Alert()
        {
            if (_state != State.Idle) return;
            _state = State.Chase;
            _repathTimer = 0f;
            if (_barRoot != null) _barRoot.gameObject.SetActive(true);
            UpdateBar();
            if (Def.IsBoss && Game.Instance != null) Game.Instance.OnBossEngaged(this);
        }

        void Turn(Vector3 dir, float speed)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), speed * Time.deltaTime);
        }

        /// <summary>Direct approach when the way is clear, otherwise follow a BFS path along the corridors.</summary>
        Vector3 ChooseMoveDir(Vector3 playerPos, Vector3 dirToPlayer, float dist, float dt)
        {
            var g = Game.Instance;
            bool clear = true;
            Vector3 from = transform.position + Vector3.up * 0.9f;
            Vector3 to = playerPos + Vector3.up * 0.9f;
            if (Physics.Raycast(from, (to - from).normalized, Mathf.Min(dist, 40f), Layers.WorldMask, QueryTriggerInteraction.Ignore))
                clear = false;

            if (clear)
            {
                _path.Clear();
                return dirToPlayer;
            }

            _repathTimer -= dt;
            if (_repathTimer <= 0f && g != null && g.Pathfinder != null)
            {
                _repathTimer = 0.4f;
                var a = DungeonBuilder.ToCell(transform.position);
                var b = DungeonBuilder.ToCell(playerPos);
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
            return dirToPlayer;
        }

        // ------------------------------------------------------------------ attacks

        void StartWindup(Vector3 dirToPlayer)
        {
            _state = State.Windup;
            _attackCount++;
            _chargeNext = Def.Charge && _attackCount % Period == 0;
            _barrageNext = Def.Kind == EnemyKind.Architect && _attackCount % 3 == 2;
            _slamNext = Def.Kind == EnemyKind.Architect ? _attackCount % 3 == 0 : (Def.IsBoss && !Def.Charge && _attackCount % Period == 0);
            _stateTimer = _slamNext ? Def.Telegraph * 1.6f : (_chargeNext ? Def.Telegraph * 1.8f : (_barrageNext ? Def.Telegraph * 1.3f : Def.Telegraph));
            if (_barrageNext) Spark.Burst(transform.position + Vector3.up * 2f, new Color(1f, 0.85f, 0.4f), 14, 2f, 0.12f, 0.5f);
            _attackDir = dirToPlayer;
            if (_chargeNext)
            {
                _telegraph = ProcAssets.Prim(PrimitiveType.Cube, null, transform.position + transform.forward * 6.5f,
                                             new Vector3(2.4f, 0.02f, 13f), ProcAssets.Unlit(new Color(0.9f, 0.1f, 0.1f)), false, "ChargeTelegraph");
            }
            if (_slamNext)
            {
                float r = 7f;
                var tmat = ProcAssets.SpriteMat(ProcAssets.SoftCircle(), new Color(0.95f, 0.1f, 0.1f, 0.5f));
                if (tmat != null)
                {
                    _telegraph = ProcAssets.Prim(PrimitiveType.Quad, null, transform.position + Vector3.up * 0.06f,
                                                 new Vector3(r * 2f, r * 2f, 1f), tmat, false, "SlamTelegraph");
                    _telegraph.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                }
                else
                    _telegraph = ProcAssets.Prim(PrimitiveType.Cylinder, null, transform.position + Vector3.up * 0.05f,
                                                 new Vector3(r * 2f, 0.02f, r * 2f), ProcAssets.Unlit(new Color(0.9f, 0.1f, 0.1f)), false, "SlamTelegraph");
            }
        }

        void AnimateWeaponWindup()
        {
            if (_weapon == null || Def.Ranged) return;
            // raise the weapon during the windup
            _weapon.localRotation = Quaternion.Slerp(_weapon.localRotation, Quaternion.Euler(-70f, 0f, 0f), 8f * Time.deltaTime);
        }

        void DoAttack(PlayerController player)
        {
            if (_telegraph != null) { Destroy(_telegraph); _telegraph = null; }
            _state = State.Recover;
            _stateTimer = 0.55f;
            _attackTimer = Def.AttackCooldown * _cdMult;

            Vector3 fwd = transform.forward;
            fwd.y = 0f;
            Vector3 toP = player.transform.position - transform.position;
            toP.y = 0f;
            float dist = toP.magnitude;
            bool hit = false;
            float mult = 1f;

            if (_slamNext)
            {
                hit = dist <= 7f + PlayerController.Radius;
                mult = 1.5f;
                Spark.Burst(transform.position + Vector3.up * 0.2f, new Color(1f, 0.4f, 0.2f), 40, 9f, 0.2f, 0.7f);
                GroundRing.Spawn(transform.position, 1f, 7.5f, new Color(1f, 0.35f, 0.2f, 0.9f), 0.5f);
                Sfx.Play(SfxKind.Slam, transform.position, 1f);
                if (Game.Instance != null && Game.Instance.Rig != null) Game.Instance.Rig.Kick(0.35f);
            }
            else if (_barrageNext)
            {
                _barrageNext = false;
                int n = Phase >= 2 ? 7 : 5;
                Vector3 origin = transform.position + Vector3.up * 1.8f + fwd * 0.8f;
                for (int i = 0; i < n; i++)
                {
                    float ang = (n == 1 ? 0f : Mathf.Lerp(-32f, 32f, i / (float)(n - 1)));
                    EnemyBolt.Spawn(origin, Quaternion.Euler(0f, ang, 0f) * fwd, Mathf.RoundToInt(Attack * 0.8f));
                }
                Sfx.Play(SfxKind.Skill, transform.position, 0.8f);
                _stateTimer = 0.7f;
                if (_weapon != null) StartCoroutine(WeaponSwing());
                return;
            }
            else if (_chargeNext)
            {
                _chargeNext = false;
                _lunge = fwd * 24f;
                _stateTimer = 0.55f;
                _chargeHit = true;
                Sfx.Play(SfxKind.Swing, transform.position, 1f);
                GroundRing.Spawn(transform.position, 0.5f, 3f, new Color(1f, 0.6f, 0.3f, 0.8f), 0.35f);
                if (_weapon != null) StartCoroutine(WeaponSwing());
                return;
            }
            else
            {
                switch (Def.Kind)
                {
                    case EnemyKind.SkeletonArcher:
                    {
                        Vector3 origin = transform.position + Vector3.up * 1.3f + fwd * 0.5f;
                        Vector3 aim = player.transform.position + Vector3.up * 1.1f - origin;
                        EnemyBolt.Spawn(origin, aim.normalized, Attack);
                        Sfx.Play(SfxKind.Swing, transform.position, 0.5f);
                        _stateTimer = 0.4f;
                        return;
                    }
                    case EnemyKind.FrenzyBoar:
                        _lunge = fwd * 11f;
                        _stateTimer = 0.3f;
                        hit = dist <= Def.AttackRange + Radius + PlayerController.Radius && Vector3.Angle(fwd, toP) < 60f;
                        Sfx.Play(SfxKind.Swing, transform.position, 0.5f);
                        break;
                    case EnemyKind.StoneGolem:
                        hit = dist <= Def.AttackRange + Radius + PlayerController.Radius && Vector3.Angle(fwd, toP) < 75f;
                        mult = 1.2f;
                        Spark.Burst(transform.position + fwd * 2f, new Color(0.6f, 0.6f, 0.6f), 16, 5f, 0.18f);
                        GroundRing.Spawn(transform.position + fwd * 2f, 0.4f, 2.6f, new Color(0.8f, 0.8f, 0.8f, 0.7f), 0.35f);
                        Sfx.Play(SfxKind.Slam, transform.position, 0.6f);
                        if (Game.Instance != null && Game.Instance.Rig != null) Game.Instance.Rig.Kick(0.2f);
                        break;
                    default:
                        hit = dist <= Def.AttackRange + Radius + PlayerController.Radius && Vector3.Angle(fwd, toP) < 65f;
                        Sfx.Play(SfxKind.Swing, transform.position, 0.6f);
                        break;
                }
            }

            if (_weapon != null) StartCoroutine(WeaponSwing());
            if (hit) player.Hurt(CombatMath.Damage(Attack, player.Defense, mult, false), transform.position);
        }

        IEnumerator WeaponSwing()
        {
            Quaternion from = Quaternion.Euler(-70f, 0f, 0f);
            Quaternion to = Quaternion.Euler(70f, 0f, 0f);
            float t = 0f;
            while (t < 0.12f)
            {
                t += Time.deltaTime;
                if (_weapon != null) _weapon.localRotation = Quaternion.Slerp(from, to, t / 0.12f);
                yield return null;
            }
            yield return new WaitForSeconds(0.25f);
            if (_weapon != null) _weapon.localRotation = _weaponRest;
        }

        // ------------------------------------------------------------------ taking damage

        public void TakeHit(int dmg, bool crit, Vector3 fromPos, float stun)
        {
            if (IsDead) return;
            if (Time.time < _immuneUntil)
            {
                DamageNumber.Spawn(transform.position + Vector3.up * (HeadHeight + 0.2f), "IMMUNE", new Color(0.7f, 0.7f, 0.8f));
                return;
            }
            Hp -= dmg;
            _flashUntil = Time.time + 0.09f;
            _hitTilt = 1f;
            DamageNumber.Spawn(transform.position + Vector3.up * (HeadHeight + 0.2f), dmg.ToString(),
                               crit ? new Color(1f, 0.9f, 0.2f) : Color.white, crit);
            Spark.Burst(transform.position + Vector3.up * (Scale * 1.0f), crit ? new Color(1f, 0.9f, 0.3f) : new Color(1f, 0.7f, 0.4f),
                        crit ? 12 : 6, 5f);
            Sfx.Play(crit ? SfxKind.Crit : SfxKind.Hit, transform.position, 0.9f);

            if (_state == State.Idle) Alert();
            UpdateBar();

            Vector3 away = transform.position - fromPos;
            away.y = 0f;
            if (away.sqrMagnitude > 0.001f)
            {
                _knock = away.normalized * (Def.IsBoss ? 0.8f : 4.5f / Mathf.Max(1f, Scale));
                _knockTimer = 0.12f;
            }

            if (!Def.IsBoss && stun > 0f && _state != State.Windup)
            {
                _stateTimer = _state == State.Stunned ? Mathf.Max(_stateTimer, stun) : stun;
                _state = State.Stunned;
            }
            else if (!Def.IsBoss && stun >= 0.4f)
            {
                // heavy hits interrupt telegraphed attacks on regular enemies
                if (_telegraph != null) { Destroy(_telegraph); _telegraph = null; }
                _state = State.Stunned;
                _stateTimer = stun;
                _attackTimer = Def.AttackCooldown * 0.5f;
                if (_weapon != null) _weapon.localRotation = _weaponRest;
            }

            if (Def.IsBoss && Hp > 0)
            {
                int np = BossInfo.BarIndex(Hp, MaxHp, BossBars);
                if (np > Phase) EnterPhase(np);
            }
            if (Hp <= 0) Die();
        }

        void Die()
        {
            IsDead = true;
            if (_telegraph != null) { Destroy(_telegraph); _telegraph = null; }
            if (_barRoot != null) _barRoot.gameObject.SetActive(false);
            _cc.enabled = false;
            Sfx.Play(SfxKind.Die, transform.position, 0.9f);
            Spark.Burst(transform.position + Vector3.up * Scale, new Color(0.5f, 0.9f, 1f), Def.IsBoss ? 60 : 20, 7f, 0.18f, 0.8f);

            var g = Game.Instance;
            if (g != null && g.Player != null)
            {
                int xp = Mathf.RoundToInt(Def.Xp * (1f + 0.25f * (Floor - 1)) * (IsElite ? 2.5f : 1f));
                g.Player.AddXp(xp);
                DamageNumber.Spawn(transform.position + Vector3.up * (HeadHeight + 0.8f), "+" + xp + " XP", new Color(0.5f, 0.9f, 1f));
            }
            if (Random.value < (Def.IsBoss ? 1f : 0.25f)) HpOrb.Spawn(transform.position);

            var lootRng = new System.Random(Random.Range(int.MinValue, int.MaxValue));
            if (g != null && g.Player != null)
                g.Player.AddGold(Economy.GoldDrop(lootRng, Floor, IsElite, Def.IsBoss));
            if (g != null && g.Player != null)
            {
                g.Player.AddShards(Smithing.ShardDrop(lootRng, IsElite, Def.IsBoss));
                if ((IsElite || Def.IsBoss || lootRng.NextDouble() < 0.07) && g.Player.Supplies.AddPotion())
                    DamageNumber.Spawn(transform.position + Vector3.up * (HeadHeight + 1.4f), "+ Potion", new Color(0.4f, 1f, 0.5f));
            }
            int drops = Def.IsBoss ? (BossBars > 2 ? 3 : 2) : (IsElite ? 1 : (LootGenerator.ShouldDrop(lootRng, false) ? 1 : 0));
            for (int i = 0; i < drops; i++)
            {
                Vector2 off = Random.insideUnitCircle * (Def.IsBoss ? 2.2f : 0.8f);
                Item it = IsElite
                    ? LootGenerator.RollOf(lootRng, Floor, LootGenerator.RollSlot(lootRng), LootGenerator.RollEliteRarity(lootRng))
                    : LootGenerator.Roll(lootRng, Floor, Def.IsBoss);
                LootDrop.Spawn(transform.position + new Vector3(off.x, 0f, off.y), it);
            }
            if (g != null) g.OnEnemyKilled(this);
            StartCoroutine(DeathRoutine());
        }

        IEnumerator DeathRoutine()
        {
            // Topple backwards, then shrink away.
            float t = 0f;
            Vector3 start = _visual.localScale;
            Quaternion fallFrom = _visual.localRotation;
            Quaternion fallTo = Quaternion.Euler(-85f, 0f, 0f);
            for (int i = 0; i < _legs.Count; i++) _legs[i].localRotation = Quaternion.identity;
            while (t < 0.75f)
            {
                t += Time.deltaTime;
                float fall = Mathf.Clamp01(t / 0.35f);
                _visual.localRotation = Quaternion.Slerp(fallFrom, fallTo, fall * fall);
                _visual.localPosition = new Vector3(0f, -0.15f * fall * Scale, 0f);
                float shrink = Mathf.Clamp01((t - 0.35f) / 0.4f);
                _visual.localScale = start * Mathf.Max(0.01f, 1f - shrink);
                if (_shadow != null) _shadow.localScale = new Vector3(1.6f * Scale * (1f - shrink), 1.6f * Scale * (1f - shrink), 1f);
                yield return null;
            }
            Destroy(gameObject);
        }

        Color _lastFlash = new Color(-1f, -1f, -1f);

        /// <summary>Tints the body towards <paramref name="c"/> (black = normal look).</summary>
        void SetFlash(Color c)
        {
            if (c == _lastFlash) return;
            _lastFlash = c;
            bool normal = c.r + c.g + c.b < 0.01f;
            for (int i = 0; i < _bodyMats.Count; i++)
                ProcAssets.Tint(_bodyMats[i], normal ? _bodyBase[i] : Color.Lerp(_bodyBase[i], c, 0.65f));
        }
    }
}

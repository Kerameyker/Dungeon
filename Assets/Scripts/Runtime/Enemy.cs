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

        readonly List<Cell> _path = new List<Cell>();
        float _repathTimer;

        public float HeadHeight { get { return 2.0f * Def.Scale; } }
        public bool Aggro { get { return _state != State.Idle; } }

        // ------------------------------------------------------------------ creation

        public static Enemy Create(EnemyKind kind, int floor, Vector3 pos)
        {
            var def = EnemyCatalog.Get(kind);
            var go = new GameObject("Enemy_" + def.Name.Replace(" ", ""));
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            var e = go.AddComponent<Enemy>();
            e.Def = def;
            e.Floor = floor;
            float scale = Progression.EnemyScale(floor);
            e.MaxHp = Mathf.RoundToInt(def.BaseHp * scale);
            e.Hp = e.MaxHp;
            e.Attack = Mathf.RoundToInt(def.BaseAttack * scale);
            e.Defense = def.Defense + (floor - 1);
            e.Radius = Mathf.Max(0.4f, 0.45f * def.Scale);
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

        void Build()
        {
            _cc = gameObject.AddComponent<CharacterController>();
            _cc.radius = Radius;
            _cc.height = Mathf.Max(1.8f * Def.Scale, Radius * 2f + 0.1f);
            _cc.center = new Vector3(0f, _cc.height * 0.5f, 0f);
            _cc.stepOffset = 0.2f;
            _cc.skinWidth = 0.05f;

            _visual = new GameObject("Visual").transform;
            _visual.SetParent(transform, false);
            _visual.localScale = Vector3.one * Def.Scale;

            var eyes = ProcAssets.Unlit(new Color(1f, 0.15f, 0.1f));

            switch (Def.Kind)
            {
                case EnemyKind.FrenzyBoar:
                {
                    var fur = Body(new Color(0.35f, 0.2f, 0.12f));
                    var tusk = Body(new Color(0.95f, 0.92f, 0.8f), null, false);
                    ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(0f, 0.6f, 0f), new Vector3(0.9f, 0.75f, 1.5f), fur);
                    ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(0f, 0.6f, 0.95f), new Vector3(0.6f, 0.55f, 0.6f), fur);
                    ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(0.22f, 0.5f, 1.3f), new Vector3(0.07f, 0.07f, 0.3f), tusk);
                    ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(-0.22f, 0.5f, 1.3f), new Vector3(0.07f, 0.07f, 0.3f), tusk);
                    ProcAssets.Prim(PrimitiveType.Sphere, _visual, new Vector3(0.18f, 0.78f, 1.22f), Vector3.one * 0.09f, eyes);
                    ProcAssets.Prim(PrimitiveType.Sphere, _visual, new Vector3(-0.18f, 0.78f, 1.22f), Vector3.one * 0.09f, eyes);
                    for (int i = 0; i < 4; i++)
                        ProcAssets.Prim(PrimitiveType.Cube, _visual,
                            new Vector3(i % 2 == 0 ? -0.3f : 0.3f, 0.15f, i < 2 ? -0.5f : 0.5f), new Vector3(0.2f, 0.3f, 0.2f), fur);
                    break;
                }
                case EnemyKind.BoneSoldier:
                {
                    var bone = Body(new Color(0.85f, 0.82f, 0.7f));
                    var dark = Body(new Color(0.15f, 0.15f, 0.18f), null, false);
                    ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(0f, 1.0f, 0f), new Vector3(0.6f, 0.9f, 0.32f), bone);
                    ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(-0.15f, 0.3f, 0f), new Vector3(0.16f, 0.6f, 0.16f), bone);
                    ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(0.15f, 0.3f, 0f), new Vector3(0.16f, 0.6f, 0.16f), bone);
                    ProcAssets.Prim(PrimitiveType.Sphere, _visual, new Vector3(0f, 1.7f, 0f), Vector3.one * 0.42f, bone);
                    ProcAssets.Prim(PrimitiveType.Sphere, _visual, new Vector3(0.1f, 1.72f, 0.17f), Vector3.one * 0.09f, eyes);
                    ProcAssets.Prim(PrimitiveType.Sphere, _visual, new Vector3(-0.1f, 1.72f, 0.17f), Vector3.one * 0.09f, eyes);
                    _weapon = new GameObject("Weapon").transform;
                    _weapon.SetParent(_visual, false);
                    _weapon.localPosition = new Vector3(0.42f, 1.1f, 0.1f);
                    _weapon.localRotation = Quaternion.Euler(20f, 0f, 0f);
                    ProcAssets.Prim(PrimitiveType.Cube, _weapon, new Vector3(0f, 0f, 0.55f), new Vector3(0.08f, 0.04f, 1.1f), dark);
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
                    ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(-0.35f, 0.3f, 0f), new Vector3(0.4f, 0.6f, 0.45f), stone);
                    ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(0.35f, 0.3f, 0f), new Vector3(0.4f, 0.6f, 0.45f), stone);
                    _weapon = new GameObject("Arms").transform;
                    _weapon.SetParent(_visual, false);
                    _weapon.localPosition = new Vector3(0f, 1.35f, 0f);
                    ProcAssets.Prim(PrimitiveType.Cube, _weapon, new Vector3(-0.8f, -0.1f, 0.1f), new Vector3(0.4f, 1.0f, 0.4f), stone);
                    ProcAssets.Prim(PrimitiveType.Cube, _weapon, new Vector3(0.8f, -0.1f, 0.1f), new Vector3(0.4f, 1.0f, 0.4f), stone);
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
                    ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(-0.28f, 0.35f, 0f), new Vector3(0.4f, 0.7f, 0.4f), armor);
                    ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(0.28f, 0.35f, 0f), new Vector3(0.4f, 0.7f, 0.4f), armor);
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
                    _weapon.localRotation = Quaternion.Euler(20f, 0f, 0f);
                    ProcAssets.Prim(PrimitiveType.Cube, _weapon, new Vector3(0f, 0f, 1.1f), new Vector3(0.22f, 0.07f, 2.2f), blade);
                    ProcAssets.Prim(PrimitiveType.Cube, _weapon, new Vector3(0f, 0f, 0.05f), new Vector3(0.6f, 0.1f, 0.12f), trim);
                    break;
                }
            }

            if (!Def.IsBoss) BuildHpBar();
        }

        void BuildHpBar()
        {
            _barRoot = new GameObject("HpBar").transform;
            _barRoot.SetParent(transform, false);
            _barRoot.localPosition = new Vector3(0f, HeadHeight + 0.5f, 0f);
            _barRoot.gameObject.AddComponent<Billboard>();

            var bg = ProcAssets.Prim(PrimitiveType.Quad, _barRoot, Vector3.zero, new Vector3(BarWidth + 0.08f, 0.16f, 1f),
                                     ProcAssets.Unlit(new Color(0.05f, 0.05f, 0.08f)));
            var fill = ProcAssets.Prim(PrimitiveType.Quad, _barRoot, new Vector3(0f, 0f, -0.01f), new Vector3(BarWidth, 0.1f, 1f),
                                       ProcAssets.Unlit(new Color(0.95f, 0.25f, 0.25f)));
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
                        float reach = Def.AttackRange + Radius * 0.5f + PlayerController.Radius;
                        if (dist <= reach && _attackTimer <= 0f)
                        {
                            StartWindup(dirToP);
                            break;
                        }
                        Vector3 moveDir = ChooseMoveDir(player.transform.position, dirToP, dist, dt);
                        if (dist > reach * 0.85f) planar += moveDir * Def.Speed;
                        Turn(moveDir, 10f);
                        break;
                    }

                    case State.Windup:
                        _stateTimer -= dt;
                        Turn(dirToP, _slamNext ? 2f : 6f);   // track the player while telegraphing
                        _attackDir = transform.forward;
                        AnimateWeaponWindup();
                        if (_stateTimer <= 0f) DoAttack(player);
                        break;

                    case State.Recover:
                        _stateTimer -= dt;
                        if (_lunge.sqrMagnitude > 0.01f) planar += _lunge;
                        if (_stateTimer <= 0f) { _lunge = Vector3.zero; _state = State.Chase; }
                        break;

                    case State.Stunned:
                        _stateTimer -= dt;
                        if (_stateTimer <= 0f) _state = State.Chase;
                        break;
                }
            }

            if (_cc.enabled)
            {
                _vy = _cc.isGrounded ? -1f : _vy - 25f * dt;
                _cc.Move((planar + Vector3.up * _vy) * dt);
            }
        }

        void Alert()
        {
            if (_state != State.Idle) return;
            _state = State.Chase;
            _repathTimer = 0f;
            if (_barRoot != null) _barRoot.gameObject.SetActive(true);
            UpdateBar();
            if (Def.IsBoss && Hud.Instance != null) Hud.Instance.SetBoss(this);
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
            _slamNext = Def.IsBoss && _attackCount % 3 == 0;
            _stateTimer = _slamNext ? Def.Telegraph * 1.6f : Def.Telegraph;
            _attackDir = dirToPlayer;
            if (_slamNext)
            {
                float r = 7f;
                _telegraph = ProcAssets.Prim(PrimitiveType.Cylinder, null, transform.position + Vector3.up * 0.05f,
                                             new Vector3(r * 2f, 0.02f, r * 2f), ProcAssets.Unlit(new Color(0.9f, 0.1f, 0.1f)), false, "SlamTelegraph");
            }
        }

        void AnimateWeaponWindup()
        {
            if (_weapon == null) return;
            // raise the weapon during the windup
            _weapon.localRotation = Quaternion.Slerp(_weapon.localRotation, Quaternion.Euler(-70f, 0f, 0f), 8f * Time.deltaTime);
        }

        void DoAttack(PlayerController player)
        {
            if (_telegraph != null) { Destroy(_telegraph); _telegraph = null; }
            _state = State.Recover;
            _stateTimer = 0.55f;
            _attackTimer = Def.AttackCooldown;

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
                Sfx.Play(SfxKind.Slam, transform.position, 1f);
                if (Game.Instance != null && Game.Instance.Rig != null) Game.Instance.Rig.Kick(0.35f);
            }
            else
            {
                switch (Def.Kind)
                {
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
            if (_weapon != null) _weapon.localRotation = Quaternion.Euler(20f, 0f, 0f);
        }

        // ------------------------------------------------------------------ taking damage

        public void TakeHit(int dmg, bool crit, Vector3 fromPos, float stun)
        {
            if (IsDead) return;
            Hp -= dmg;
            _flashUntil = Time.time + 0.09f;
            DamageNumber.Spawn(transform.position + Vector3.up * (HeadHeight + 0.2f), dmg.ToString(),
                               crit ? new Color(1f, 0.9f, 0.2f) : Color.white, crit);
            Spark.Burst(transform.position + Vector3.up * (Def.Scale * 1.0f), crit ? new Color(1f, 0.9f, 0.3f) : new Color(1f, 0.7f, 0.4f),
                        crit ? 12 : 6, 5f);
            Sfx.Play(crit ? SfxKind.Crit : SfxKind.Hit, transform.position, 0.9f);

            if (_state == State.Idle) Alert();
            UpdateBar();

            Vector3 away = transform.position - fromPos;
            away.y = 0f;
            if (away.sqrMagnitude > 0.001f)
            {
                _knock = away.normalized * (Def.IsBoss ? 0.8f : 4.5f / Mathf.Max(1f, Def.Scale));
                _knockTimer = 0.12f;
            }

            if (!Def.IsBoss && stun > 0f && _state != State.Windup)
            {
                _state = State.Stunned;
                _stateTimer = stun;
            }
            else if (!Def.IsBoss && stun >= 0.4f)
            {
                // heavy hits interrupt telegraphed attacks on regular enemies
                if (_telegraph != null) { Destroy(_telegraph); _telegraph = null; }
                _state = State.Stunned;
                _stateTimer = stun;
                _attackTimer = Def.AttackCooldown * 0.5f;
                if (_weapon != null) _weapon.localRotation = Quaternion.Euler(20f, 0f, 0f);
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
            Spark.Burst(transform.position + Vector3.up * Def.Scale, new Color(0.5f, 0.9f, 1f), Def.IsBoss ? 60 : 20, 7f, 0.18f, 0.8f);

            var g = Game.Instance;
            if (g != null && g.Player != null)
            {
                int xp = Mathf.RoundToInt(Def.Xp * (1f + 0.25f * (Floor - 1)));
                g.Player.AddXp(xp);
                DamageNumber.Spawn(transform.position + Vector3.up * (HeadHeight + 0.8f), "+" + xp + " XP", new Color(0.5f, 0.9f, 1f));
            }
            if (Random.value < (Def.IsBoss ? 1f : 0.25f)) HpOrb.Spawn(transform.position);

            var lootRng = new System.Random(Random.Range(int.MinValue, int.MaxValue));
            int drops = Def.IsBoss ? 2 : (LootGenerator.ShouldDrop(lootRng, false) ? 1 : 0);
            for (int i = 0; i < drops; i++)
            {
                Vector2 off = Random.insideUnitCircle * (Def.IsBoss ? 2.2f : 0.8f);
                LootDrop.Spawn(transform.position + new Vector3(off.x, 0f, off.y), LootGenerator.Roll(lootRng, Floor, Def.IsBoss));
            }
            if (g != null) g.OnEnemyKilled(this);
            StartCoroutine(DeathRoutine());
        }

        IEnumerator DeathRoutine()
        {
            float t = 0f;
            Vector3 start = _visual.localScale;
            while (t < 0.45f)
            {
                t += Time.deltaTime;
                float k = 1f - t / 0.45f;
                _visual.localScale = start * Mathf.Max(0.01f, k);
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

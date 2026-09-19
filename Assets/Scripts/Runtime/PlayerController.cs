using System.Collections;
using System.Collections.Generic;
using Hollow.Core;
using UnityEngine;

namespace Hollow
{
    /// <summary>
    /// The player: builds its own body and sword from primitives, handles movement, dodge roll,
    /// the 3-hit light combo and the sword skills.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        public const float Radius = 0.4f;

        // --- stats ---
        public int Level = 1;
        public int Xp;
        public int Hp = 100;
        public int MaxHp = 100;
        public int Attack = 10;
        public int Defense = 2;
        public bool IsDead;

        // --- gear ---
        public readonly Equipment Gear = new Equipment();
        public readonly Inventory Bag = new Inventory();
        public float CritChance { get { return CombatMath.CritChance + Gear.BonusCrit; } }

        // --- lock-on ---
        public Enemy LockTarget { get; private set; }
        Transform _lockMarker;

        public Cooldown[] SkillCd;
        public Cooldown DodgeCd = new Cooldown(0.9f);

        // --- references ---
        CharacterController _cc;
        Transform _visual;
        Transform _sword;
        TrailRenderer _trail;
        CameraRig _rig;
        Material _bodyMat;
        Color _bodyBase;

        // --- state ---
        readonly ComboChain _combo = new ComboChain();
        Coroutine _action;
        Coroutine _swing;
        float _actionUntil;
        bool _cancelable;
        float _attackBufferUntil;
        Vector3 _actionVel;
        float _invuln;
        float _vy;
        bool _dodging;
        float _flashUntil;
        static readonly List<Enemy> HitScratch = new List<Enemy>();

        public bool Busy { get { return Time.time < _actionUntil || _dodging; } }
        public bool Invulnerable { get { return _invuln > 0f; } }

        // ------------------------------------------------------------------ setup

        public static PlayerController Create(CameraRig rig)
        {
            var go = new GameObject("Player");
            var p = go.AddComponent<PlayerController>();
            p._rig = rig;
            p.Build();
            p.RecalcStats(true);
            return p;
        }

        void Build()
        {
            _cc = gameObject.AddComponent<CharacterController>();
            _cc.height = 1.8f;
            _cc.radius = Radius;
            _cc.center = new Vector3(0f, 0.9f, 0f);
            _cc.stepOffset = 0.3f;
            _cc.skinWidth = 0.05f;

            _visual = new GameObject("Visual").transform;
            _visual.SetParent(transform, false);

            _bodyBase = new Color(0.10f, 0.13f, 0.24f);
            _bodyMat = ProcAssets.Lit(_bodyBase, null, 0.3f);
            var accent = ProcAssets.Lit(new Color(0.1f, 0.8f, 1f), null, 0.3f, new Color(0f, 0.5f, 0.7f));
            var hair = ProcAssets.Lit(new Color(0.05f, 0.05f, 0.07f));
            var skin = ProcAssets.Lit(new Color(0.95f, 0.78f, 0.65f));

            ProcAssets.Prim(PrimitiveType.Capsule, _visual, new Vector3(0f, 0.95f, 0f), new Vector3(0.62f, 0.55f, 0.5f), _bodyMat, false, "Torso");
            ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(0f, 0.95f, 0.22f), new Vector3(0.12f, 0.9f, 0.05f), accent, false, "CoatTrim");
            ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(-0.15f, 0.3f, 0f), new Vector3(0.22f, 0.6f, 0.25f), _bodyMat, false, "LegL");
            ProcAssets.Prim(PrimitiveType.Cube, _visual, new Vector3(0.15f, 0.3f, 0f), new Vector3(0.22f, 0.6f, 0.25f), _bodyMat, false, "LegR");
            ProcAssets.Prim(PrimitiveType.Sphere, _visual, new Vector3(0f, 1.62f, 0f), Vector3.one * 0.38f, skin, false, "Head");
            ProcAssets.Prim(PrimitiveType.Sphere, _visual, new Vector3(0f, 1.7f, -0.03f), new Vector3(0.42f, 0.34f, 0.42f), hair, false, "Hair");

            // Sword on a pivot at the right hand. Blade points along local +Z.
            _sword = new GameObject("SwordPivot").transform;
            _sword.SetParent(_visual, false);
            _sword.localPosition = new Vector3(0.42f, 1.0f, 0.2f);
            _sword.localRotation = SwordRest;
            var bladeMat = ProcAssets.Lit(new Color(0.85f, 0.95f, 1f), null, 0.8f, new Color(0.1f, 0.35f, 0.5f));
            var hiltMat = ProcAssets.Lit(new Color(0.25f, 0.15f, 0.1f));
            ProcAssets.Prim(PrimitiveType.Cube, _sword, new Vector3(0f, 0f, 0.7f), new Vector3(0.09f, 0.025f, 1.15f), bladeMat, false, "Blade");
            ProcAssets.Prim(PrimitiveType.Cube, _sword, new Vector3(0f, 0f, 0.05f), new Vector3(0.28f, 0.05f, 0.06f), accent, false, "Guard");
            ProcAssets.Prim(PrimitiveType.Cube, _sword, new Vector3(0f, 0f, -0.12f), new Vector3(0.05f, 0.05f, 0.25f), hiltMat, false, "Hilt");

            var tip = new GameObject("Tip").transform;
            tip.SetParent(_sword, false);
            tip.localPosition = new Vector3(0f, 0f, 1.25f);
            _trail = tip.gameObject.AddComponent<TrailRenderer>();
            _trail.time = 0.16f;
            _trail.widthMultiplier = 0.45f;
            _trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            _trail.sharedMaterial = ProcAssets.Unlit(new Color(0.4f, 0.9f, 1f));
            _trail.emitting = false;
            _trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // Small personal light so the player is always readable.
            var l = new GameObject("PlayerLight").AddComponent<Light>();
            l.transform.SetParent(transform, false);
            l.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            l.type = LightType.Point;
            l.color = new Color(0.8f, 0.9f, 1f);
            l.range = 9f;
            l.intensity = 1.1f;

            // Lock-on marker: a small glowing diamond floating above the target (world space, billboarded).
            var markerRoot = new GameObject("LockMarker");
            markerRoot.AddComponent<Billboard>();
            var diamond = ProcAssets.Prim(PrimitiveType.Quad, markerRoot.transform, Vector3.zero, new Vector3(0.36f, 0.36f, 1f),
                                          ProcAssets.Unlit(new Color(0.5f, 0.95f, 1f)), false, "Diamond");
            diamond.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            _lockMarker = markerRoot.transform;
            markerRoot.SetActive(false);

            SkillCd = new Cooldown[SkillCatalog.All.Length];
            for (int i = 0; i < SkillCd.Length; i++) SkillCd[i] = new Cooldown(SkillCatalog.All[i].Cooldown);
        }

        static readonly Quaternion SwordRest = Quaternion.Euler(25f, 0f, 0f);

        // ------------------------------------------------------------------ stats

        public void RecalcStats(bool fullHeal)
        {
            MaxHp = Progression.MaxHp(Level) + Gear.BonusMaxHp;
            Attack = Progression.Attack(Level) + Gear.BonusAttack;
            Defense = Progression.Defense(Level) + Gear.BonusDefense;
            if (fullHeal) Hp = MaxHp;
            Hp = Mathf.Min(Hp, MaxHp);
        }

        public void AddXp(int amount)
        {
            int oldLevel = Level;
            int gained = Progression.AddXp(ref Level, ref Xp, amount);
            if (gained > 0)
            {
                RecalcStats(true);
                Sfx.Play2D(SfxKind.LevelUp);
                Spark.Burst(transform.position + Vector3.up, new Color(1f, 0.9f, 0.4f), 24, 6f, 0.15f, 0.7f);
                string msg = "LEVEL UP!  Lv " + Level;
                foreach (var s in SkillCatalog.All)
                    if (s.UnlockLevel > oldLevel && s.UnlockLevel <= Level) msg += "\nNew skill: " + s.Name;
                Hud.Instance.Toast(msg, 3f);
            }
        }

        public void Heal(int amount)
        {
            Hp = Mathf.Min(MaxHp, Hp + amount);
            DamageNumber.Spawn(transform.position + Vector3.up * 2.2f, "+" + amount, new Color(0.4f, 1f, 0.5f));
        }

        public void Teleport(Vector3 pos)
        {
            _cc.enabled = false;
            transform.position = pos;
            _cc.enabled = true;
            _vy = 0f;
        }

        public void Respawn(Vector3 pos)
        {
            EndAction();
            ClearLock();
            IsDead = false;
            _invuln = 1.5f;
            RecalcStats(true);
            _visual.localRotation = Quaternion.identity;
            _visual.localPosition = Vector3.zero;
            Teleport(pos);
        }

        // ------------------------------------------------------------------ gear

        /// <summary>Picks up an item: auto-equips into an empty slot, otherwise goes to the backpack. False if the backpack is full.</summary>
        public bool AddItem(Item item)
        {
            string label = ItemUi.Colored(item);
            var worn = Gear.Get(item.Slot);
            if (worn == null)
            {
                Gear.Equip(item);
                int oldMax = MaxHp;
                RecalcStats(false);
                Hp = Mathf.Min(MaxHp, Hp + (MaxHp - oldMax));
                Hud.Instance.Toast("Equipped  " + label, 2.5f);
                Sfx.Play2D(SfxKind.Pickup);
                return true;
            }
            if (!Bag.Add(item)) return false;
            string extra = item.Score > worn.Score ? "   <color=#7CFF8A>(upgrade! press I)</color>" : "";
            Hud.Instance.Toast("Picked up  " + label + extra, 2.5f);
            Sfx.Play2D(SfxKind.Pickup);
            return true;
        }

        public void EquipFromBag(int index)
        {
            var item = Bag.RemoveAt(index);
            if (item == null) return;
            var old = Gear.Equip(item);
            if (old != null) Bag.Add(old);   // a slot was just freed, so this always fits
            int oldMax = MaxHp;
            RecalcStats(false);
            if (MaxHp > oldMax) Hp += MaxHp - oldMax;
            Hp = Mathf.Min(Hp, MaxHp);
            Sfx.Play2D(SfxKind.Pickup, 0.6f);
        }

        public void DiscardFromBag(int index)
        {
            Bag.RemoveAt(index);
        }

        // ------------------------------------------------------------------ lock-on

        void HandleLockOn()
        {
            if (LockTarget != null &&
                (LockTarget.IsDead || (LockTarget.transform.position - transform.position).sqrMagnitude > 22f * 22f))
                ClearLock();

            if (GameInput.LockOnPressed)
            {
                if (LockTarget != null) ClearLock();
                else SetLock(FindTarget(null));
            }
            else if (GameInput.SwitchTargetPressed && LockTarget != null)
            {
                var next = FindTarget(LockTarget);
                if (next != null) SetLock(next);
            }
            UpdateMarker();
        }

        void SetLock(Enemy e)
        {
            LockTarget = e;
            if (_rig != null) _rig.LockTarget = e;
            if (e != null) Sfx.Play2D(SfxKind.Swing, 0.3f);
        }

        void ClearLock()
        {
            if (LockTarget == null && (_rig == null || _rig.LockTarget == null)) return;
            LockTarget = null;
            if (_rig != null) _rig.LockTarget = null;
        }

        /// <summary>Finds a target: in front of the camera when locking, or the nearest neighbour when switching.</summary>
        Enemy FindTarget(Enemy exclude)
        {
            Enemy best = null;
            float bestScore = float.MaxValue;
            Vector3 camF = _rig != null ? _rig.transform.forward : transform.forward;
            camF.y = 0f;
            camF = camF.sqrMagnitude > 0.001f ? camF.normalized : Vector3.forward;
            Vector3 eye = transform.position + Vector3.up * 1.2f;

            foreach (var e in Enemy.All)
            {
                if (e == null || e.IsDead || e == exclude) continue;
                Vector3 to = e.transform.position - transform.position;
                to.y = 0f;
                float d = to.magnitude;
                if (d > 18f) continue;
                if (Physics.Linecast(eye, e.transform.position + Vector3.up * 1.2f, Layers.WorldMask)) continue;

                float score;
                if (exclude != null)
                    score = (e.transform.position - exclude.transform.position).sqrMagnitude;
                else
                {
                    float ang = Vector3.Angle(camF, to);
                    if (ang > 100f) continue;
                    score = d + ang * 0.15f;
                }
                if (score < bestScore) { bestScore = score; best = e; }
            }
            return best;
        }

        void UpdateMarker()
        {
            if (_lockMarker == null) return;
            bool show = LockTarget != null && !LockTarget.IsDead;
            if (_lockMarker.gameObject.activeSelf != show) _lockMarker.gameObject.SetActive(show);
            if (show)
                _lockMarker.position = LockTarget.transform.position +
                                       Vector3.up * (LockTarget.HeadHeight + 0.95f + Mathf.Sin(Time.time * 4f) * 0.06f);
        }

        void OnDestroy()
        {
            if (_lockMarker != null) Destroy(_lockMarker.gameObject);
        }

        // ------------------------------------------------------------------ damage taken

        public void Hurt(int damage, Vector3 fromPos)
        {
            if (IsDead || _invuln > 0f) return;
            Hp -= damage;
            _invuln = 0.45f;
            _flashUntil = Time.time + 0.12f;
            DamageNumber.Spawn(transform.position + Vector3.up * 2.1f, damage.ToString(), new Color(1f, 0.35f, 0.3f), true);
            Sfx.Play(SfxKind.Hurt, transform.position);
            if (Hud.Instance != null) Hud.Instance.Flash();
            if (_rig != null) _rig.Kick(0.12f);
            if (Hp <= 0)
            {
                Hp = 0;
                Die();
            }
        }

        void Die()
        {
            IsDead = true;
            EndAction();
            Sfx.Play(SfxKind.Die, transform.position);
            Spark.Burst(transform.position + Vector3.up, new Color(0.4f, 0.8f, 1f), 30, 6f, 0.15f, 0.8f);
            _visual.localRotation = Quaternion.Euler(-80f, 0f, 0f);   // fall backwards
            _visual.localPosition = new Vector3(0f, 0.2f, 0f);
            Game.Instance.OnPlayerDied();
        }

        // ------------------------------------------------------------------ update

        void Update()
        {
            if (Game.Instance != null && Game.Instance.Paused) return;   // inventory open
            float dt = Time.deltaTime;
            for (int i = 0; i < SkillCd.Length; i++) SkillCd[i].Tick(dt);
            DodgeCd.Tick(dt);
            if (_invuln > 0f) _invuln -= dt;

            // Damage flash on the body.
            PlayerFlash(Time.time < _flashUntil);

            if (IsDead)
            {
                ClearLock();
                UpdateMarker();
                ApplyGravityOnly(dt);
                return;
            }

            HandleLockOn();
            HandleActions();
            HandleMovement(dt);
        }

        void PlayerFlash(bool on)
        {
            ProcAssets.Tint(_bodyMat, on ? new Color(0.8f, 0.15f, 0.15f) : _bodyBase);
        }

        void ApplyGravityOnly(float dt)
        {
            if (!_cc.enabled) return;
            _vy = _cc.isGrounded ? -1f : _vy - 25f * dt;
            _cc.Move(new Vector3(0f, _vy * dt, 0f));
        }

        void HandleActions()
        {
            // Dodge
            if (GameInput.DodgePressed && DodgeCd.Ready && !_dodging && (!Busy || _cancelable))
            {
                Vector2 m = GameInput.Move;
                Vector3 dir = m.sqrMagnitude > 0.01f ? CameraRelative(m) : -transform.forward;
                BeginDodge(dir);
                return;
            }

            // Skills (may cancel a light attack)
            for (int i = 0; i < SkillCatalog.All.Length; i++)
            {
                if (!GameInput.SkillPressed(i)) continue;
                var def = SkillCatalog.All[i];
                if (!SkillCatalog.IsUnlocked(def, Level))
                {
                    Hud.Instance.Toast(def.Name + " unlocks at level " + def.UnlockLevel, 1.5f);
                    continue;
                }
                if (!SkillCd[i].Ready) continue;
                if (_dodging || (Busy && !_cancelable)) continue;
                SkillCd[i].Start();
                BeginAction(SkillRoutine(def), def.LockTime, false);
                return;
            }

            // Light attack (with a small input buffer)
            if (GameInput.AttackPressed && Cursor.lockState == CursorLockMode.Locked) _attackBufferUntil = Time.time + 0.25f;
            if (Time.time < _attackBufferUntil && !Busy)
            {
                _attackBufferUntil = 0f;
                int step = _combo.Next(Time.time);
                float lockTime = step == 2 ? 0.55f : 0.36f;
                BeginAction(LightAttackRoutine(step), lockTime, true);
            }
        }

        void HandleMovement(float dt)
        {
            Vector3 planar = Vector3.zero;

            if (_actionVel.sqrMagnitude > 0.001f)
            {
                planar = _actionVel;
            }
            else if (!Busy)
            {
                Vector2 m = GameInput.Move;
                if (m.sqrMagnitude > 0.01f)
                {
                    Vector3 dir = CameraRelative(m);
                    float speed = GameInput.Sprint ? 8.2f : 5.8f;
                    planar = dir * speed;
                    if (LockTarget == null)
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 14f * dt);
                }
            }

            // While locked on, always face the target (strafing).
            if (LockTarget != null && !Busy)
            {
                Vector3 look = LockTarget.transform.position - transform.position;
                look.y = 0f;
                if (look.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look), 16f * dt);
            }

            _vy = _cc.isGrounded ? -1f : _vy - 25f * dt;
            _cc.Move((planar + Vector3.up * _vy) * dt);

            // keep exactly on the ground plane if we somehow drift
            if (transform.position.y < -1f) Teleport(new Vector3(transform.position.x, 0.1f, transform.position.z));
        }

        Vector3 CameraRelative(Vector2 input)
        {
            float yaw = _rig != null ? _rig.Yaw : 0f;
            Vector3 v = Quaternion.Euler(0f, yaw, 0f) * new Vector3(input.x, 0f, input.y);
            v.y = 0f;
            return v.normalized;
        }

        // ------------------------------------------------------------------ actions

        void BeginAction(IEnumerator routine, float lockTime, bool cancelable)
        {
            EndAction();
            _actionUntil = Time.time + lockTime;
            _cancelable = cancelable;
            _action = StartCoroutine(routine);
        }

        void EndAction()
        {
            if (_action != null) StopCoroutine(_action);
            _action = null;
            if (_swing != null) StopCoroutine(_swing);
            _swing = null;
            _actionVel = Vector3.zero;
            _dodging = false;
            _cancelable = false;
            _actionUntil = 0f;
            if (_trail != null) _trail.emitting = false;
            if (_sword != null) _sword.localRotation = SwordRest;
            if (_visual != null && !IsDead) _visual.localRotation = Quaternion.identity;
        }

        /// <summary>Direction to attack: towards input (or facing), assisted towards the nearest enemy in front.</summary>
        Vector3 AimDirection()
        {
            if (LockTarget != null && !LockTarget.IsDead)
            {
                Vector3 lt = LockTarget.transform.position - transform.position;
                lt.y = 0f;
                if (lt.sqrMagnitude > 0.01f) return lt.normalized;
            }
            Vector2 m = GameInput.Move;
            Vector3 dir = m.sqrMagnitude > 0.01f ? CameraRelative(m) : transform.forward;
            Enemy best = null;
            float bestScore = float.MaxValue;
            foreach (var e in Enemy.All)
            {
                if (e == null || e.IsDead) continue;
                Vector3 to = e.transform.position - transform.position;
                to.y = 0f;
                float d = to.magnitude;
                if (d > 7f || d < 0.01f) continue;
                float ang = Vector3.Angle(dir, to);
                if (ang > 70f) continue;
                float score = d + ang * 0.05f;
                if (score < bestScore) { bestScore = score; best = e; }
            }
            if (best != null)
            {
                Vector3 to = best.transform.position - transform.position;
                to.y = 0f;
                dir = to.normalized;
            }
            return dir;
        }

        void Face(Vector3 dir)
        {
            if (dir.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(dir);
        }

        void BeginDodge(Vector3 dir)
        {
            EndAction();
            DodgeCd.Start();
            _dodging = true;
            _actionUntil = Time.time + 0.34f;
            _action = StartCoroutine(DodgeRoutine(dir));
        }

        IEnumerator DodgeRoutine(Vector3 dir)
        {
            const float dur = 0.34f;
            _invuln = Mathf.Max(_invuln, 0.42f);
            Face(dir);
            _actionVel = dir * 12.5f;
            Sfx.Play(SfxKind.Swing, transform.position, 0.4f);
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                _visual.localRotation = Quaternion.Euler(360f * (t / dur), 0f, 0f);
                yield return null;
            }
            _visual.localRotation = Quaternion.identity;
            _actionVel = Vector3.zero;
            _dodging = false;
            _action = null;
        }

        IEnumerator SwingSword(Quaternion from, Quaternion to, float dur)
        {
            _trail.emitting = true;
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / dur));
                _sword.localRotation = Quaternion.Slerp(from, to, k);
                yield return null;
            }
            _sword.localRotation = to;
        }

        IEnumerator LightAttackRoutine(int step)
        {
            Vector3 dir = AimDirection();
            Face(dir);
            _actionVel = dir * 3.5f;
            Sfx.Play(SfxKind.Swing, transform.position, 0.7f);

            Quaternion a, b;
            if (step == 0) { a = Quaternion.Euler(0f, 75f, 0f); b = Quaternion.Euler(0f, -75f, 0f); }
            else if (step == 1) { a = Quaternion.Euler(0f, -75f, 0f); b = Quaternion.Euler(0f, 75f, 0f); }
            else { a = Quaternion.Euler(-115f, 0f, 0f); b = Quaternion.Euler(65f, 0f, 0f); }

            float swingDur = step == 2 ? 0.22f : 0.16f;
            yield return null;
            _swing = StartCoroutine(SwingSword(a, b, swingDur));
            yield return new WaitForSeconds(swingDur * 0.6f);

            float mult = ComboChain.Multipliers[step];
            HitCone(transform.position, dir, 2.9f, step == 2 ? 130f : 110f, mult, step == 2 ? 0.5f : 0.15f, null);
            if (step == 2 && _rig != null) _rig.Kick(0.1f);
            _actionVel = Vector3.zero;

            yield return _swing;
            _trail.emitting = false;
            yield return new WaitForSeconds(0.12f);
            _sword.localRotation = SwordRest;
            _action = null;
        }

        IEnumerator SkillRoutine(SkillDef def)
        {
            Vector3 dir = AimDirection();
            Face(dir);
            Sfx.Play(SfxKind.Skill, transform.position);
            switch (def.Kind)
            {
                case SkillKind.Arc:
                {
                    yield return SwingSword(Quaternion.Euler(0f, 100f, 0f), Quaternion.Euler(0f, -100f, 0f), 0.16f);
                    HitCone(transform.position, dir, def.Range, def.Angle, def.Multiplier, def.Stun, null);
                    Spark.Burst(transform.position + dir * 2f + Vector3.up, new Color(0.5f, 0.9f, 1f), 12, 5f);
                    if (_rig != null) _rig.Kick(0.12f);
                    break;
                }
                case SkillKind.Vertical:
                {
                    _actionVel = dir * 6f;
                    yield return SwingSword(Quaternion.Euler(-130f, 0f, 0f), Quaternion.Euler(70f, 0f, 0f), 0.2f);
                    _actionVel = Vector3.zero;
                    HitCone(transform.position, dir, def.Range, def.Angle, def.Multiplier, def.Stun, null);
                    Vector3 impact = transform.position + dir * 2.5f + Vector3.up * 0.2f;
                    Spark.Burst(impact, new Color(1f, 0.85f, 0.4f), 22, 6f, 0.14f);
                    Sfx.Play(SfxKind.Slam, impact, 0.6f);
                    if (_rig != null) _rig.Kick(0.25f);
                    break;
                }
                case SkillKind.Thrust:
                {
                    _invuln = Mathf.Max(_invuln, 0.3f);
                    _sword.localRotation = Quaternion.identity;
                    _trail.emitting = true;
                    _actionVel = dir * 24f;
                    var already = new HashSet<Enemy>();
                    float t = 0f;
                    while (t < 0.26f)
                    {
                        HitCone(transform.position, dir, 2.4f, 70f, def.Multiplier, def.Stun, already);
                        t += Time.deltaTime;
                        yield return null;
                    }
                    _actionVel = Vector3.zero;
                    if (_rig != null) _rig.Kick(0.15f);
                    break;
                }
                case SkillKind.Flurry:
                {
                    _trail.emitting = true;
                    float total = 1.0f;
                    int hits = Mathf.Max(1, def.Hits);
                    float interval = total / hits;
                    float t = 0f;
                    int done = 0;
                    while (t < total)
                    {
                        t += Time.deltaTime;
                        // spin the whole body
                        _visual.localRotation = Quaternion.Euler(0f, 720f * (t / total), 0f);
                        _sword.localRotation = Quaternion.Euler(10f, 0f, 0f);
                        while (done < hits && t >= (done + 1) * interval * 0.85f)
                        {
                            done++;
                            HitCone(transform.position, dir, def.Range, def.Angle, def.Multiplier, def.Stun, null);
                            Spark.Burst(transform.position + Vector3.up, new Color(1f, 0.55f, 0.2f), 8, 5f);
                            Sfx.Play(SfxKind.Swing, transform.position, 0.6f);
                        }
                        yield return null;
                    }
                    _visual.localRotation = Quaternion.identity;
                    if (_rig != null) _rig.Kick(0.2f);
                    break;
                }
            }

            _trail.emitting = false;
            yield return new WaitForSeconds(0.15f);
            _sword.localRotation = SwordRest;
            _actionVel = Vector3.zero;
            _action = null;
        }

        // ------------------------------------------------------------------ hit detection

        /// <summary>Damages every enemy inside a cone. Returns the number hit. Skips enemies in <paramref name="exclude"/> and adds hits to it.</summary>
        int HitCone(Vector3 origin, Vector3 dir, float range, float angle, float mult, float stun, HashSet<Enemy> exclude)
        {
            HitScratch.Clear();
            foreach (var e in Enemy.All)
            {
                if (e == null || e.IsDead) continue;
                if (exclude != null && exclude.Contains(e)) continue;
                Vector3 to = e.transform.position - origin;
                to.y = 0f;
                float dist = to.magnitude;
                if (dist > range + e.Radius) continue;
                if (angle < 359f && dist > 0.01f)
                {
                    // widen the cone a little for big enemies
                    float slack = Mathf.Atan2(e.Radius, Mathf.Max(dist, 0.1f)) * Mathf.Rad2Deg;
                    if (Vector3.Angle(dir, to) > angle * 0.5f + slack) continue;
                }
                HitScratch.Add(e);
            }

            int count = 0;
            for (int i = 0; i < HitScratch.Count; i++)
            {
                var e = HitScratch[i];
                if (exclude != null) exclude.Add(e);
                bool crit = CombatMath.IsCrit(Random.value, CritChance);
                int dmg = CombatMath.Damage(Attack, e.Defense, mult, crit);
                e.TakeHit(dmg, crit, transform.position, stun);
                count++;
            }
            if (count > 0 && _rig != null) _rig.Kick(0.05f);
            return count;
        }
    }
}

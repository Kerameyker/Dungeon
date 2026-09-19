using System;

namespace Hollow.Core
{
    public static class CombatMath
    {
        public const float CritChance = 0.10f;
        public const float CritMultiplier = 1.5f;

        public static bool IsCrit(float roll01) { return roll01 < CritChance; }

        /// <summary>Crit check with a custom chance (base chance + gear bonus), capped at 75%.</summary>
        public static bool IsCrit(float roll01, float chance) { return roll01 < Math.Min(0.75f, chance); }

        /// <summary>attack * multiplier * 100/(100+defense), crit x1.5, never below 1.</summary>
        public static int Damage(int attack, int defense, float multiplier, bool crit)
        {
            double raw = attack * (double)multiplier * (100.0 / (100.0 + Math.Max(0, defense)));
            if (crit) raw *= CritMultiplier;
            return Math.Max(1, (int)Math.Round(raw));
        }
    }

    public static class Progression
    {
        public const int MaxLevel = 50;

        public static int XpForNextLevel(int level)
        {
            return (int)Math.Round(60.0 * Math.Pow(Math.Max(1, level), 1.5));
        }

        public static int MaxHp(int level) { return 100 + 15 * (level - 1); }
        public static int Attack(int level) { return 10 + 3 * (level - 1); }
        public static int Defense(int level) { return 2 + (level - 1); }

        /// <summary>Enemy HP / attack multiplier for a floor (floor 1 = 1.0).</summary>
        public static float EnemyScale(int floor)
        {
            return 1f + 0.35f * (Math.Max(1, floor) - 1);
        }

        public static int EnemyCount(int floor)
        {
            return Math.Min(6 + 2 * Math.Max(1, floor), 20);
        }

        /// <summary>Adds XP, handling multiple level-ups. Returns the number of levels gained.</summary>
        public static int AddXp(ref int level, ref int xp, int gained)
        {
            int levels = 0;
            xp += Math.Max(0, gained);
            while (level < MaxLevel && xp >= XpForNextLevel(level))
            {
                xp -= XpForNextLevel(level);
                level++;
                levels++;
            }
            if (level >= MaxLevel) xp = 0;
            return levels;
        }
    }

    public enum SkillKind { Arc, Vertical, Thrust, Flurry }

    public sealed class SkillDef
    {
        public string Id;
        public string Name;
        public SkillKind Kind;
        public int UnlockLevel;
        public float Cooldown;
        public float Multiplier;
        public int Hits;
        public float Range;
        public float Angle;      // total cone angle in degrees (360 = all around)
        public float LockTime;   // seconds the player is committed to the skill
        public float Stun;       // seconds of stun applied to hit enemies
    }

    public static class SkillCatalog
    {
        public static readonly SkillDef[] All =
        {
            new SkillDef { Id = "crescent_cut", Name = "Crescent Cut", Kind = SkillKind.Arc, UnlockLevel = 1,
                           Cooldown = 3f, Multiplier = 1.8f, Hits = 1, Range = 3.4f, Angle = 140f, LockTime = 0.45f, Stun = 0.2f },
            new SkillDef { Id = "skyfall_slash", Name = "Skyfall Slash", Kind = SkillKind.Vertical, UnlockLevel = 1,
                           Cooldown = 5f, Multiplier = 2.2f, Hits = 1, Range = 3.6f, Angle = 90f, LockTime = 0.6f, Stun = 0.8f },
            new SkillDef { Id = "lancing_dash", Name = "Lancing Dash", Kind = SkillKind.Thrust, UnlockLevel = 2,
                           Cooldown = 7f, Multiplier = 2.6f, Hits = 1, Range = 6f, Angle = 40f, LockTime = 0.5f, Stun = 0.5f },
            new SkillDef { Id = "ember_flurry", Name = "Ember Flurry", Kind = SkillKind.Flurry, UnlockLevel = 4,
                           Cooldown = 12f, Multiplier = 0.8f, Hits = 5, Range = 3.2f, Angle = 360f, LockTime = 1.25f, Stun = 0.15f },
        };

        public static bool IsUnlocked(SkillDef skill, int playerLevel)
        {
            return playerLevel >= skill.UnlockLevel;
        }
    }

    /// <summary>Simple cooldown timer.</summary>
    public sealed class Cooldown
    {
        public float Duration;
        public float Remaining { get; private set; }

        public Cooldown(float duration) { Duration = duration; }

        public bool Ready { get { return Remaining <= 0f; } }
        public float Normalized { get { return Duration > 0f ? Math.Min(1f, Math.Max(0f, Remaining / Duration)) : 0f; } }

        public void Start() { Remaining = Duration; }
        public void Reset() { Remaining = 0f; }
        public void Tick(float dt) { if (Remaining > 0f) Remaining = Math.Max(0f, Remaining - dt); }
    }

    /// <summary>Tracks the 3-hit light attack chain.</summary>
    public sealed class ComboChain
    {
        public const float Window = 0.9f;
        public static readonly float[] Multipliers = { 1.0f, 1.1f, 1.6f };

        int _last = -1;
        float _lastTime = float.NegativeInfinity;

        /// <summary>Call when the player attacks; returns the combo step (0..2).</summary>
        public int Next(float now)
        {
            int idx = (_last >= 0 && now - _lastTime <= Window) ? (_last + 1) % Multipliers.Length : 0;
            _last = idx;
            _lastTime = now;
            return idx;
        }

        public void Reset() { _last = -1; _lastTime = float.NegativeInfinity; }
    }

    public enum EnemyKind { FrenzyBoar, BoneSoldier, StoneGolem, FloorGuardian }

    public sealed class EnemyDef
    {
        public EnemyKind Kind;
        public string Name;
        public int BaseHp;
        public int BaseAttack;
        public int Defense;
        public float Speed;
        public float AttackRange;
        public float AttackCooldown;
        public float Telegraph;
        public int Xp;
        public float Scale;
        public bool IsBoss;
    }

    public static class EnemyCatalog
    {
        static readonly EnemyDef[] Defs =
        {
            new EnemyDef { Kind = EnemyKind.FrenzyBoar, Name = "Frenzy Boar", BaseHp = 45, BaseAttack = 9, Defense = 0, Speed = 5.4f,
                           AttackRange = 1.7f, AttackCooldown = 1.1f, Telegraph = 0.30f, Xp = 14, Scale = 0.9f },
            new EnemyDef { Kind = EnemyKind.BoneSoldier, Name = "Bone Soldier", BaseHp = 70, BaseAttack = 12, Defense = 3, Speed = 3.4f,
                           AttackRange = 2.1f, AttackCooldown = 1.6f, Telegraph = 0.45f, Xp = 20, Scale = 1.0f },
            new EnemyDef { Kind = EnemyKind.StoneGolem, Name = "Stone Golem", BaseHp = 160, BaseAttack = 22, Defense = 8, Speed = 2.2f,
                           AttackRange = 2.6f, AttackCooldown = 2.6f, Telegraph = 0.80f, Xp = 42, Scale = 1.5f },
            new EnemyDef { Kind = EnemyKind.FloorGuardian, Name = "Floor Guardian", BaseHp = 900, BaseAttack = 26, Defense = 10, Speed = 3.0f,
                           AttackRange = 3.2f, AttackCooldown = 2.2f, Telegraph = 0.70f, Xp = 250, Scale = 2.4f, IsBoss = true },
        };

        public static EnemyDef Get(EnemyKind kind)
        {
            for (int i = 0; i < Defs.Length; i++)
                if (Defs[i].Kind == kind) return Defs[i];
            throw new ArgumentOutOfRangeException("kind");
        }

        /// <summary>Picks a non-boss enemy type. Golems only appear from floor 2. roll in [0,1).</summary>
        public static EnemyKind PickForFloor(int floor, double roll)
        {
            if (floor < 2) return roll < 0.5 ? EnemyKind.FrenzyBoar : EnemyKind.BoneSoldier;
            if (roll < 0.35) return EnemyKind.FrenzyBoar;
            if (roll < 0.75) return EnemyKind.BoneSoldier;
            return EnemyKind.StoneGolem;
        }
    }
}

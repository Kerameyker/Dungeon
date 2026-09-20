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
        public float Duration = 1f;   // Flurry: total spin time
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
        public void Set(float remaining) { Remaining = Math.Max(0f, remaining); }
        public void Halve() { Remaining *= 0.5f; }
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

    public enum EnemyKind { FrenzyBoar, BoneSoldier, StoneGolem, FloorGuardian, SkeletonArcher, CrimsonKnight, Architect }

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
        public bool Ranged;        // keeps its distance and shoots
        public bool Charge;        // boss that telegraphs and performs a charging dash
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
            new EnemyDef { Kind = EnemyKind.SkeletonArcher, Name = "Skeleton Archer", BaseHp = 40, BaseAttack = 11, Defense = 0, Speed = 3.4f,
                           AttackRange = 15f, AttackCooldown = 2.3f, Telegraph = 0.55f, Xp = 26, Scale = 1.0f, Ranged = true },
            new EnemyDef { Kind = EnemyKind.CrimsonKnight, Name = "Crimson Knight", BaseHp = 720, BaseAttack = 24, Defense = 8, Speed = 4.2f,
                           AttackRange = 2.8f, AttackCooldown = 1.5f, Telegraph = 0.45f, Xp = 260, Scale = 2.0f, IsBoss = true, Charge = true },
            new EnemyDef { Kind = EnemyKind.Architect, Name = "The Architect", BaseHp = 1400, BaseAttack = 30, Defense = 12, Speed = 3.6f,
                           AttackRange = 3.4f, AttackCooldown = 1.8f, Telegraph = 0.6f, Xp = 900, Scale = 2.2f, IsBoss = true, Ranged = false },
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
            if (roll < 0.28) return EnemyKind.FrenzyBoar;
            if (roll < 0.58) return EnemyKind.BoneSoldier;
            if (roll < 0.78) return EnemyKind.SkeletonArcher;
            return EnemyKind.StoneGolem;
        }

        /// <summary>Odd floors are guarded by the Floor Guardian, even floors by the Crimson Knight.</summary>
        public static EnemyKind BossForFloor(int floor)
        {
            if (floor == BossInfo.FinalFloor) return EnemyKind.Architect;
            return floor % 2 == 0 ? EnemyKind.CrimsonKnight : EnemyKind.FloorGuardian;
        }
    }

    /// <summary>Boss presentation data: how many health bars a boss has and what it is called.</summary>
    public static class BossInfo
    {
        public const int FinalFloor = 20;

        public static bool IsMilestone(int floor) { return floor > 0 && floor % 5 == 0; }

        public static int Bars(EnemyKind kind, int floor)
        {
            if (kind == EnemyKind.Architect) return 5;
            return IsMilestone(floor) ? 4 : 2;
        }

        static readonly string[] Titles =
        {
            "Vanguard of the Sunken Gate", "Warden of the Green Ruin", "Ember Tyrant of the Deep", "Herald of the Void Stair"
        };

        /// <summary>Title shown under milestone bosses (empty for regular bosses).</summary>
        public static string Title(EnemyKind kind, int floor)
        {
            if (kind == EnemyKind.Architect) return "Creator of the Spire";
            if (!IsMilestone(floor)) return "";
            return Titles[((floor / 5) - 1 + Titles.Length * 100) % Titles.Length];
        }

        /// <summary>Bar index (0 = first) for the current hp; the boss "breaks" a bar each time this increases.</summary>
        public static int BarIndex(int hp, int maxHp, int bars)
        {
            if (hp <= 0) return bars - 1;
            float per = maxHp / (float)bars;
            int used = (int)((maxHp - hp) / per);
            return Math.Min(bars - 1, Math.Max(0, used));
        }

        /// <summary>Fill (0..1) of the bar currently being depleted.</summary>
        public static float BarFill(int hp, int maxHp, int bars)
        {
            if (hp <= 0) return 0f;
            float per = maxHp / (float)bars;
            int idx = BarIndex(hp, maxHp, bars);
            float remaining = hp - (bars - 1 - idx) * per;
            return Math.Min(1f, Math.Max(0f, remaining / per));
        }
    }
}

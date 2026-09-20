using System;

namespace Hollow.Core
{
    public enum WeaponType { Sword = 0, Rapier = 1, GreatBlade = 2, Dagger = 3 }

    public sealed class WeaponDef
    {
        public WeaponType Type;
        public string Name;
        public string Blurb;
        public float SwingSpeed;   // animation speed multiplier (higher = faster swings)
        public float Range;        // light attack reach
        public float Arc;          // light attack cone (degrees)
        public float[] Combo;      // damage multipliers of the 3-hit chain
        public float CritBonus;    // added crit chance while wielded
        public float DodgeCooldown;
    }

    public static class WeaponCatalog
    {
        static readonly WeaponDef[] Defs =
        {
            new WeaponDef { Type = WeaponType.Sword, Name = "One-handed Sword", Blurb = "Balanced. Good reach and speed.",
                            SwingSpeed = 1.0f, Range = 2.9f, Arc = 110f, Combo = new[] { 1.0f, 1.1f, 1.6f }, CritBonus = 0f, DodgeCooldown = 0.9f },
            new WeaponDef { Type = WeaponType.Rapier, Name = "Rapier", Blurb = "Fast thrusts, long reach, high crit.",
                            SwingSpeed = 1.3f, Range = 3.3f, Arc = 60f, Combo = new[] { 0.8f, 0.9f, 1.5f }, CritBonus = 0.08f, DodgeCooldown = 0.8f },
            new WeaponDef { Type = WeaponType.GreatBlade, Name = "Great Blade", Blurb = "Slow, heavy, staggers enemies.",
                            SwingSpeed = 0.75f, Range = 3.5f, Arc = 150f, Combo = new[] { 1.5f, 1.7f, 2.6f }, CritBonus = 0f, DodgeCooldown = 1.15f },
            new WeaponDef { Type = WeaponType.Dagger, Name = "Dagger", Blurb = "Very fast, short reach, quick dodges.",
                            SwingSpeed = 1.6f, Range = 2.3f, Arc = 100f, Combo = new[] { 0.6f, 0.65f, 1.1f }, CritBonus = 0.12f, DodgeCooldown = 0.55f },
        };

        public static WeaponDef Get(WeaponType t) { return Defs[(int)t]; }
    }

    /// <summary>Skill sets per weapon type, plus the unique dual-blade skill.</summary>
    public static class SkillSets
    {
        static readonly SkillDef[][] Sets =
        {
            SkillCatalog.All, // Sword
            new[]
            {
                new SkillDef { Id = "piercing_line", Name = "Piercing Line", Kind = SkillKind.Thrust, UnlockLevel = 1, Cooldown = 2.5f, Multiplier = 1.7f, Hits = 1, Range = 5f, Angle = 30f, LockTime = 0.4f, Stun = 0.3f },
                new SkillDef { Id = "needle_rain", Name = "Needle Rain", Kind = SkillKind.Flurry, UnlockLevel = 2, Cooldown = 6f, Multiplier = 0.55f, Hits = 6, Range = 3.4f, Angle = 120f, LockTime = 1.0f, Stun = 0.1f, Duration = 0.9f },
                new SkillDef { Id = "comet_lunge", Name = "Comet Lunge", Kind = SkillKind.Thrust, UnlockLevel = 4, Cooldown = 8f, Multiplier = 2.5f, Hits = 1, Range = 9f, Angle = 30f, LockTime = 0.55f, Stun = 0.6f },
                new SkillDef { Id = "meteor_fang", Name = "Meteor Fang", Kind = SkillKind.Flurry, UnlockLevel = 6, Cooldown = 14f, Multiplier = 0.7f, Hits = 8, Range = 3.4f, Angle = 360f, LockTime = 1.3f, Stun = 0.15f, Duration = 1.2f },
            },
            new[]
            {
                new SkillDef { Id = "wide_sweep", Name = "Wide Sweep", Kind = SkillKind.Arc, UnlockLevel = 1, Cooldown = 4f, Multiplier = 2.1f, Hits = 1, Range = 4.2f, Angle = 200f, LockTime = 0.6f, Stun = 0.4f },
                new SkillDef { Id = "earthsplitter", Name = "Earthsplitter", Kind = SkillKind.Vertical, UnlockLevel = 2, Cooldown = 6f, Multiplier = 3.0f, Hits = 1, Range = 4f, Angle = 110f, LockTime = 0.8f, Stun = 1.2f },
                new SkillDef { Id = "bulwark_charge", Name = "Bulwark Charge", Kind = SkillKind.Thrust, UnlockLevel = 4, Cooldown = 8f, Multiplier = 2.2f, Hits = 1, Range = 5f, Angle = 60f, LockTime = 0.6f, Stun = 1.0f },
                new SkillDef { Id = "titans_wrath", Name = "Titan's Wrath", Kind = SkillKind.Flurry, UnlockLevel = 6, Cooldown = 15f, Multiplier = 1.6f, Hits = 3, Range = 4f, Angle = 360f, LockTime = 1.3f, Stun = 0.8f, Duration = 1.1f },
            },
            new[]
            {
                new SkillDef { Id = "shadow_stab", Name = "Shadowstep Stab", Kind = SkillKind.Thrust, UnlockLevel = 1, Cooldown = 2.5f, Multiplier = 1.6f, Hits = 1, Range = 4.5f, Angle = 30f, LockTime = 0.35f, Stun = 0.25f },
                new SkillDef { Id = "fan_of_cuts", Name = "Fan of Cuts", Kind = SkillKind.Arc, UnlockLevel = 2, Cooldown = 3.5f, Multiplier = 1.5f, Hits = 1, Range = 3f, Angle = 150f, LockTime = 0.4f, Stun = 0.2f },
                new SkillDef { Id = "viper_rush", Name = "Viper Rush", Kind = SkillKind.Thrust, UnlockLevel = 4, Cooldown = 7f, Multiplier = 2.4f, Hits = 1, Range = 7f, Angle = 35f, LockTime = 0.45f, Stun = 0.5f },
                new SkillDef { Id = "thousand_cuts", Name = "Thousand Cuts", Kind = SkillKind.Flurry, UnlockLevel = 6, Cooldown = 13f, Multiplier = 0.4f, Hits = 12, Range = 3f, Angle = 360f, LockTime = 1.4f, Stun = 0.1f, Duration = 1.3f },
            },
        };

        public static readonly SkillDef Unique = new SkillDef
        {
            Id = "twin_tempest", Name = "Twin Tempest", Kind = SkillKind.Flurry, UnlockLevel = 1, Cooldown = 25f, Multiplier = 0.55f,
            Hits = 10, Range = 3.8f, Angle = 360f, LockTime = 1.7f, Stun = 0.2f, Duration = 1.5f
        };

        public static SkillDef[] For(WeaponType t) { return Sets[(int)t]; }

        /// <summary>The hotbar for a weapon: its four skills, plus Twin Tempest once the dual blades are unlocked.</summary>
        public static SkillDef[] Hotbar(WeaponType t, bool dualUnlocked)
        {
            var basic = For(t);
            if (!dualUnlocked) return basic;
            var list = new SkillDef[basic.Length + 1];
            Array.Copy(basic, list, basic.Length);
            list[basic.Length] = Unique;
            return list;
        }
    }
}

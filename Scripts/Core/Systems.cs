using System;
using System.Collections.Generic;

namespace Hollow.Core
{
    public enum SmithResult { Success, Failed, FailedDowngrade, MaxLevel, NotEnoughGold, NotEnoughShards }

    /// <summary>Blacksmith upgrades: +1..+10, costs gold and Ember Shards, higher levels can fail.</summary>
    public static class Smithing
    {
        public const int MaxUpgrade = 10;
        public const float BonusPerLevel = 0.08f;   // +8% of the base stats per level

        static readonly float[] Chance = { 1.0f, 1.0f, 0.95f, 0.85f, 0.70f, 0.55f, 0.40f, 0.30f, 0.20f, 0.12f };

        public static float SuccessChance(int currentLevel)
        {
            if (currentLevel < 0) currentLevel = 0;
            if (currentLevel >= MaxUpgrade) return 0f;
            return Chance[currentLevel];
        }

        public static int GoldCost(Item item)
        {
            return 60 * (item.Upgrade + 1) * (1 + (int)item.Rarity);
        }

        public static int ShardCost(Item item) { return item.Upgrade + 1; }

        /// <summary>Tries one upgrade. Pays the cost, even on failure. From +5 up, a failure drops the item one level.</summary>
        public static SmithResult Attempt(Item item, Random rng, ref int gold, ref int shards)
        {
            if (item.Upgrade >= MaxUpgrade) return SmithResult.MaxLevel;
            int g = GoldCost(item), s = ShardCost(item);
            if (gold < g) return SmithResult.NotEnoughGold;
            if (shards < s) return SmithResult.NotEnoughShards;
            gold -= g;
            shards -= s;
            if (rng.NextDouble() < SuccessChance(item.Upgrade))
            {
                item.Upgrade++;
                return SmithResult.Success;
            }
            if (item.Upgrade >= 5)
            {
                item.Upgrade--;
                return SmithResult.FailedDowngrade;
            }
            return SmithResult.Failed;
        }

        /// <summary>Ember Shards dropped by an enemy.</summary>
        public static int ShardDrop(Random rng, bool elite, bool boss)
        {
            if (boss) return 4 + rng.Next(0, 3);
            if (elite) return 1 + rng.Next(0, 2);
            return rng.NextDouble() < 0.12 ? 1 : 0;
        }
    }

    /// <summary>Skills level up with use; each mastery level adds damage and trims the cooldown.</summary>
    public sealed class SkillMastery
    {
        public const int MaxLevel = 5;
        static readonly int[] Thresholds = { 10, 30, 60, 110, 180 };
        public readonly Dictionary<string, int> Uses = new Dictionary<string, int>();

        /// <summary>Records a use. Returns true if this use raised the mastery level.</summary>
        public bool AddUse(string id)
        {
            int before = Level(id);
            int n;
            Uses.TryGetValue(id, out n);
            Uses[id] = n + 1;
            return Level(id) > before;
        }

        public int Level(string id)
        {
            int n;
            if (!Uses.TryGetValue(id, out n)) return 0;
            int lvl = 0;
            for (int i = 0; i < Thresholds.Length; i++) if (n >= Thresholds[i]) lvl = i + 1;
            return lvl;
        }

        public int UsesToNext(string id)
        {
            int lvl = Level(id);
            if (lvl >= MaxLevel) return 0;
            int n;
            Uses.TryGetValue(id, out n);
            return Thresholds[lvl] - n;
        }

        public float DamageMult(string id) { return 1f + 0.06f * Level(id); }
        public float CooldownMult(string id) { return 1f - 0.04f * Level(id); }
    }

    /// <summary>Potions and crystals the player carries.</summary>
    public sealed class Consumables
    {
        public const int PotionCap = 15, CrystalCap = 10, ReviveCap = 3;
        public const int PotionPrice = 40, CrystalPrice = 90, RevivePrice = 300;
        public const float PotionHeal = 0.40f;
        public const float ReviveHeal = 0.50f;

        public int Potions, ReturnCrystals, ReviveTokens;

        public bool AddPotion(int n = 1) { if (Potions + n > PotionCap) return false; Potions += n; return true; }
        public bool AddCrystal(int n = 1) { if (ReturnCrystals + n > CrystalCap) return false; ReturnCrystals += n; return true; }
        public bool AddRevive(int n = 1) { if (ReviveTokens + n > ReviveCap) return false; ReviveTokens += n; return true; }
        public bool UsePotion() { if (Potions <= 0) return false; Potions--; return true; }
        public bool UseCrystal() { if (ReturnCrystals <= 0) return false; ReturnCrystals--; return true; }
        public bool UseRevive() { if (ReviveTokens <= 0) return false; ReviveTokens--; return true; }
    }

    /// <summary>Kill counts and scouted (studied) bosses.</summary>
    public sealed class Bestiary
    {
        public readonly Dictionary<string, int> Kills = new Dictionary<string, int>();
        public readonly HashSet<string> Scouted = new HashSet<string>();

        public void RecordKill(EnemyKind kind)
        {
            string k = kind.ToString();
            int n;
            Kills.TryGetValue(k, out n);
            Kills[k] = n + 1;
        }

        public int KillsOf(EnemyKind kind)
        {
            int n;
            Kills.TryGetValue(kind.ToString(), out n);
            return n;
        }

        /// <summary>Studying a boss (defeating it once) gives +10% damage against that boss kind.</summary>
        public bool Scout(EnemyKind kind) { return Scouted.Add(kind.ToString()); }
        public bool IsScouted(EnemyKind kind) { return Scouted.Contains(kind.ToString()); }
        public float DamageMultVs(EnemyKind kind) { return IsScouted(kind) ? 1.10f : 1f; }
    }
}

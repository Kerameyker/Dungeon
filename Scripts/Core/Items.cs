using System;
using System.Collections.Generic;

namespace Hollow.Core
{
    public enum Rarity { Common = 0, Uncommon = 1, Rare = 2, Epic = 3, Legendary = 4 }
    public enum ItemSlot { Weapon = 0, Armor = 1, Trinket = 2 }

    /// <summary>A piece of gear. Public fields so it can be serialized with JsonUtility for saves.</summary>
    [Serializable]
    public sealed class Item
    {
        public string Name;
        public ItemSlot Slot;
        public Rarity Rarity;
        public int ItemLevel;
        public int Attack;
        public int Defense;
        public int MaxHp;
        public float Crit;   // additive crit chance, e.g. 0.03 = +3%
        public WeaponType WType;   // weapons only
        public int Style;          // index of the base look (blade shape, armor set, trinket type)
        public int Upgrade;        // blacksmith level 0..Smithing.MaxUpgrade
        public int Seed;           // small random value for cosmetic variation

        public float UpgradeMult { get { return 1f + Smithing.BonusPerLevel * Upgrade; } }
        public int EffAttack { get { return (int)Math.Round(Attack * UpgradeMult); } }
        public int EffDefense { get { return (int)Math.Round(Defense * UpgradeMult); } }
        public int EffMaxHp { get { return (int)Math.Round(MaxHp * UpgradeMult); } }
        public string DisplayName { get { return Upgrade > 0 ? Name + " +" + Upgrade : Name; } }

        /// <summary>Rough power value used to compare two items (includes blacksmith upgrades).</summary>
        public float Score
        {
            get { return EffAttack * 1.0f + EffDefense * 1.2f + EffMaxHp * 0.2f + Crit * 300f; }
        }
    }

    public static class LootGenerator
    {
        static readonly float[] RarityMult = { 1.0f, 1.25f, 1.6f, 2.1f, 3.0f };

        static readonly string[][] WeaponNames =
        {
            new[] { "Iron Blade", "Steel Saber", "Runic Longsword", "Wind Cutter" },      // Sword
            new[] { "Moonlit Rapier", "Silver Needle", "Thorn Fencer", "Glass Stinger" },  // Rapier
            new[] { "Titan Cleaver", "Bulwark Edge", "Ruin Claymore", "Dusk Greatblade" }, // GreatBlade
            new[] { "Night Fang", "Viper Knife", "Ash Kris", "Whisper Dirk" },             // Dagger
        };
        static readonly string[] ArmorNames = { "Leather Coat", "Chain Mail", "Knight's Plate", "Warden Cloak", "Scale Vest" };
        static readonly string[] TrinketNames = { "Copper Ring", "Silver Amulet", "Lucky Charm", "Ember Pendant", "Spire Sigil" };

        static readonly string[][] Prefixes =
        {
            new[] { "Worn", "Plain", "Simple" },
            new[] { "Sturdy", "Fine", "Tempered" },
            new[] { "Runed", "Gleaming", "Honed" },
            new[] { "Astral", "Warden's", "Radiant" },
            new[] { "Mythic", "Spire-forged", "Sovereign" },
        };

        public static float Multiplier(Rarity r) { return RarityMult[(int)r]; }

        public static bool ShouldDrop(Random rng, bool boss)
        {
            return boss || rng.NextDouble() < 0.30;
        }

        /// <summary>Normal enemies mostly drop commons; bosses always drop Rare or better.</summary>
        public static Rarity RollRarity(Random rng, bool boss)
        {
            double r = rng.NextDouble();
            if (boss)
            {
                if (r < 0.60) return Rarity.Rare;
                if (r < 0.92) return Rarity.Epic;
                return Rarity.Legendary;
            }
            if (r < 0.60) return Rarity.Common;
            if (r < 0.88) return Rarity.Uncommon;
            if (r < 0.97) return Rarity.Rare;
            if (r < 0.995) return Rarity.Epic;
            return Rarity.Legendary;
        }

        /// <summary>Elite enemies drop better loot than normal ones.</summary>
        public static Rarity RollEliteRarity(Random rng)
        {
            double r = rng.NextDouble();
            if (r < 0.45) return Rarity.Uncommon;
            if (r < 0.85) return Rarity.Rare;
            if (r < 0.98) return Rarity.Epic;
            return Rarity.Legendary;
        }

        /// <summary>Treasure chests: never Common, decent chance of Epic or better.</summary>
        public static Rarity RollChestRarity(Random rng)
        {
            double r = rng.NextDouble();
            if (r < 0.30) return Rarity.Uncommon;
            if (r < 0.75) return Rarity.Rare;
            if (r < 0.95) return Rarity.Epic;
            return Rarity.Legendary;
        }

        public static ItemSlot RollSlot(Random rng)
        {
            double r = rng.NextDouble();
            if (r < 0.40) return ItemSlot.Weapon;
            if (r < 0.75) return ItemSlot.Armor;
            return ItemSlot.Trinket;
        }

        public static Item Roll(Random rng, int floor, bool boss)
        {
            return RollOf(rng, floor, RollSlot(rng), RollRarity(rng, boss));
        }

        public static Item RollOf(Random rng, int floor, ItemSlot slot, Rarity rarity)
        {
            floor = Math.Max(1, floor);
            float mult = RarityMult[(int)rarity];
            var item = new Item { Slot = slot, Rarity = rarity, ItemLevel = floor };

            string[] bases;
            if (slot == ItemSlot.Weapon)
            {
                item.WType = (WeaponType)rng.Next(4);
                bases = WeaponNames[(int)item.WType];
            }
            else bases = slot == ItemSlot.Armor ? ArmorNames : TrinketNames;
            string prefix = Prefixes[(int)rarity][rng.Next(Prefixes[(int)rarity].Length)];
            item.Style = rng.Next(bases.Length);
            item.Seed = rng.Next(1, 1000000);
            item.Name = prefix + " " + bases[item.Style];

            switch (slot)
            {
                case ItemSlot.Weapon:
                    item.Attack = Stat(rng, 4f + 2.5f * floor, mult);
                    if (item.WType == WeaponType.Rapier || item.WType == WeaponType.Dagger) item.Crit = 0.01f * (1 + (int)rarity);
                    break;
                case ItemSlot.Armor:
                    item.Defense = Stat(rng, 2f + 1.5f * floor, mult);
                    item.MaxHp = Stat(rng, 8f + 5f * floor, mult);
                    break;
                default:
                    item.Crit = 0.01f + 0.015f * (int)rarity;
                    item.Attack = Stat(rng, (1f + floor) * 0.5f, mult);
                    item.MaxHp = Stat(rng, 5f + 3f * floor, mult);
                    break;
            }
            return item;
        }

        static int Stat(Random rng, float baseValue, float mult)
        {
            double variance = 0.9 + 0.2 * rng.NextDouble();
            return Math.Max(1, (int)Math.Round(baseValue * mult * variance));
        }
    }

    /// <summary>What the player currently wears: one item per slot.</summary>
    public sealed class Equipment
    {
        public readonly Item[] Slots = new Item[3];

        public Item Get(ItemSlot slot) { return Slots[(int)slot]; }

        /// <summary>Removes and returns the item in a slot (or null).</summary>
        public Item Unequip(ItemSlot slot)
        {
            var old = Slots[(int)slot];
            Slots[(int)slot] = null;
            return old;
        }

        /// <summary>The equipped weapon type (Sword when nothing is wielded).</summary>
        public WeaponType Weapon { get { var w = Slots[(int)ItemSlot.Weapon]; return w == null ? WeaponType.Sword : w.WType; } }

        /// <summary>Equips the item and returns whatever it replaced (or null).</summary>
        public Item Equip(Item item)
        {
            var old = Slots[(int)item.Slot];
            Slots[(int)item.Slot] = item;
            return old;
        }

        public int BonusAttack { get { return Sum(i => i.EffAttack); } }
        public int BonusDefense { get { return Sum(i => i.EffDefense); } }
        public int BonusMaxHp { get { return Sum(i => i.EffMaxHp); } }

        public float BonusCrit
        {
            get
            {
                float c = 0f;
                foreach (var i in Slots) if (i != null) c += i.Crit;
                return c;
            }
        }

        int Sum(Func<Item, int> f)
        {
            int total = 0;
            foreach (var i in Slots) if (i != null) total += f(i);
            return total;
        }
    }

    /// <summary>Backpack with a fixed capacity.</summary>
    public sealed class Inventory
    {
        public const int Capacity = 20;
        public readonly List<Item> Items = new List<Item>();

        public bool IsFull { get { return Items.Count >= Capacity; } }

        public bool Add(Item item)
        {
            if (item == null || IsFull) return false;
            Items.Add(item);
            return true;
        }

        public Item RemoveAt(int index)
        {
            if (index < 0 || index >= Items.Count) return null;
            var it = Items[index];
            Items.RemoveAt(index);
            return it;
        }
    }
}

using System;

namespace Hollow.Core
{
    /// <summary>Gold economy: drops, selling and the merchant's price list.</summary>
    public static class Economy
    {
        public static int GoldDrop(Random rng, int floor, bool elite, bool boss)
        {
            int f = Math.Max(1, floor);
            int g = 4 + 2 * f + rng.Next(0, f + 4);
            if (elite) g *= 3;
            if (boss) g *= 8;
            return g;
        }

        /// <summary>What the merchant pays for an item (better rarity and power = more).</summary>
        public static int SellValue(Item item)
        {
            if (item == null) return 0;
            double v = item.Score * (1.0 + 0.6 * (int)item.Rarity);
            return Math.Max(1, (int)Math.Round(v));
        }

        /// <summary>Price of one random item from the merchant.</summary>
        public static int BuyPrice(int maxFloor) { return 100 + 60 * Math.Max(1, maxFloor); }
    }

    /// <summary>Everything that is persisted between sessions (serialized with JsonUtility).</summary>
    [Serializable]
    public sealed class SaveData
    {
        public int Version = 2;
        public int Level = 1;
        public int Xp;
        public int Gold;
        public int Shards;
        public int Potions, ReturnCrystals, ReviveTokens;
        public int MaxFloor = 1;
        public int Seed;
        public bool Hardcore;
        // JsonUtility cannot store null elements, so empty gear slots become items with no Name.
        public Item[] Gear = new Item[3];
        public Item[] Bag = new Item[0];
        public string[] Flags = new string[0];
        public string[] MasteryIds = new string[0];
        public int[] MasteryUses = new int[0];
        public string[] QuestIds = new string[0];
        public int[] QuestProgress = new int[0];
        public string[] QuestsCompleted = new string[0];
        public string[] ScoutedBosses = new string[0];

        public static bool IsEmpty(Item it) { return it == null || string.IsNullOrEmpty(it.Name); }
    }
}

using System;
using System.Collections.Generic;

namespace Hollow.Core
{
    public enum QuestKind { Slay, SlayArchers, SlayElites, OpenChests, ReachFloor, DefeatBoss, EarnGold }

    public sealed class QuestDef
    {
        public string Id, Title, Description;
        public QuestKind Kind;
        public int Count;
        public int MinFloor;       // appears on the board once this floor has been reached
        public int RewardGold, RewardShards;
        public int RewardPotions;
    }

    [Serializable]
    public sealed class QuestState
    {
        public string Id;
        public int Progress;
        public bool Done;
    }

    public static class QuestCatalog
    {
        public static readonly QuestDef[] All =
        {
            new QuestDef { Id = "q_slay10", Title = "Thinning the Herd", Description = "Defeat 10 monsters.", Kind = QuestKind.Slay, Count = 10, MinFloor = 1, RewardGold = 80, RewardShards = 1 },
            new QuestDef { Id = "q_chest2", Title = "Fortune Hunter", Description = "Open 2 treasure chests.", Kind = QuestKind.OpenChests, Count = 2, MinFloor = 1, RewardGold = 100, RewardPotions = 2 },
            new QuestDef { Id = "q_floor3", Title = "Up the Stairs", Description = "Reach floor 3.", Kind = QuestKind.ReachFloor, Count = 3, MinFloor = 1, RewardGold = 120, RewardShards = 2 },
            new QuestDef { Id = "q_boss1", Title = "Guardian's Toll", Description = "Defeat a floor boss.", Kind = QuestKind.DefeatBoss, Count = 1, MinFloor = 1, RewardGold = 150, RewardShards = 2 },
            new QuestDef { Id = "q_archer5", Title = "Bows Down", Description = "Defeat 5 Skeleton Archers.", Kind = QuestKind.SlayArchers, Count = 5, MinFloor = 2, RewardGold = 160, RewardPotions = 2 },
            new QuestDef { Id = "q_gold300", Title = "Coin Collector", Description = "Earn 300 gold from monsters.", Kind = QuestKind.EarnGold, Count = 300, MinFloor = 2, RewardGold = 100, RewardShards = 3 },
            new QuestDef { Id = "q_elite2", Title = "Gilded Trouble", Description = "Defeat 2 elite monsters.", Kind = QuestKind.SlayElites, Count = 2, MinFloor = 3, RewardGold = 220, RewardShards = 3 },
            new QuestDef { Id = "q_slay30", Title = "Cleaning House", Description = "Defeat 30 monsters.", Kind = QuestKind.Slay, Count = 30, MinFloor = 3, RewardGold = 260, RewardShards = 3 },
            new QuestDef { Id = "q_floor6", Title = "Beyond the Crypt", Description = "Reach floor 6.", Kind = QuestKind.ReachFloor, Count = 6, MinFloor = 4, RewardGold = 300, RewardShards = 4 },
            new QuestDef { Id = "q_boss3", Title = "Three Falls", Description = "Defeat 3 floor bosses.", Kind = QuestKind.DefeatBoss, Count = 3, MinFloor = 4, RewardGold = 400, RewardShards = 5 },
            new QuestDef { Id = "q_chest5", Title = "Vault Raider", Description = "Open 5 treasure chests.", Kind = QuestKind.OpenChests, Count = 5, MinFloor = 5, RewardGold = 350, RewardPotions = 3 },
            new QuestDef { Id = "q_floor11", Title = "Molten Heights", Description = "Reach floor 11.", Kind = QuestKind.ReachFloor, Count = 11, MinFloor = 8, RewardGold = 600, RewardShards = 6 },
            new QuestDef { Id = "q_elite6", Title = "Gold Hunters", Description = "Defeat 6 elite monsters.", Kind = QuestKind.SlayElites, Count = 6, MinFloor = 8, RewardGold = 700, RewardShards = 6 },
            new QuestDef { Id = "q_floor16", Title = "The Last Stair", Description = "Reach floor 16.", Kind = QuestKind.ReachFloor, Count = 16, MinFloor = 12, RewardGold = 1200, RewardShards = 8 },
        };

        public static QuestDef Get(string id)
        {
            for (int i = 0; i < All.Length; i++) if (All[i].Id == id) return All[i];
            return null;
        }
    }

    /// <summary>The player's accepted quests and completed history.</summary>
    public sealed class QuestLog
    {
        public const int MaxActive = 3;
        public readonly List<QuestState> Active = new List<QuestState>();
        public readonly HashSet<string> Completed = new HashSet<string>();

        public bool IsActive(string id) { return Find(id) != null; }

        QuestState Find(string id)
        {
            for (int i = 0; i < Active.Count; i++) if (Active[i].Id == id) return Active[i];
            return null;
        }

        /// <summary>Quests that can be picked up now.</summary>
        public List<QuestDef> Available(int maxFloor)
        {
            var list = new List<QuestDef>();
            foreach (var q in QuestCatalog.All)
                if (q.MinFloor <= maxFloor && !Completed.Contains(q.Id) && !IsActive(q.Id)) list.Add(q);
            return list;
        }

        public bool Accept(string id, int currentMaxFloor)
        {
            var def = QuestCatalog.Get(id);
            if (def == null || Active.Count >= MaxActive || Completed.Contains(id) || IsActive(id)) return false;
            var st = new QuestState { Id = id };
            // "reach floor" quests count the highest floor already reached
            if (def.Kind == QuestKind.ReachFloor) st.Progress = Math.Min(def.Count, currentMaxFloor);
            st.Done = st.Progress >= def.Count;
            Active.Add(st);
            return true;
        }

        void Add(QuestKind kind, int amount)
        {
            foreach (var st in Active)
            {
                var def = QuestCatalog.Get(st.Id);
                if (def == null || def.Kind != kind || st.Done) continue;
                st.Progress = Math.Min(def.Count, st.Progress + amount);
                if (st.Progress >= def.Count) st.Done = true;
            }
        }

        public void OnKill(EnemyKind kind, bool elite, bool boss)
        {
            if (boss) { Add(QuestKind.DefeatBoss, 1); return; }
            Add(QuestKind.Slay, 1);
            if (kind == EnemyKind.SkeletonArcher) Add(QuestKind.SlayArchers, 1);
            if (elite) Add(QuestKind.SlayElites, 1);
        }

        public void OnChest() { Add(QuestKind.OpenChests, 1); }
        public void OnGold(int amount) { Add(QuestKind.EarnGold, amount); }

        public void OnFloorReached(int floor)
        {
            foreach (var st in Active)
            {
                var def = QuestCatalog.Get(st.Id);
                if (def == null || def.Kind != QuestKind.ReachFloor || st.Done) continue;
                st.Progress = Math.Max(st.Progress, Math.Min(def.Count, floor));
                if (st.Progress >= def.Count) st.Done = true;
            }
        }

        /// <summary>Hands in a finished quest. Returns its definition (for the rewards) or null.</summary>
        public QuestDef Claim(string id)
        {
            var st = Find(id);
            if (st == null || !st.Done) return null;
            Active.Remove(st);
            Completed.Add(id);
            return QuestCatalog.Get(id);
        }
    }
}

using System.Collections.Generic;
using Hollow.Core;
using NUnit.Framework;

namespace Hollow.Tests
{
    public class DungeonTests
    {
        [Test]
        public void EveryRoomIsReachableFromStart_ForManySeeds()
        {
            for (int seed = 0; seed < 200; seed++)
            {
                var layout = DungeonGenerator.Generate(seed);
                Assert.GreaterOrEqual(layout.Rooms.Count, 4, "seed " + seed);
                var start = layout.Rooms[layout.StartRoom].Center;
                var dist = new Pathfinder(layout).Distances(start.X, start.Y);
                foreach (var r in layout.Rooms)
                    Assert.GreaterOrEqual(dist[r.CenterX, r.CenterY], 0, "unreachable room, seed " + seed);
            }
        }

        [Test]
        public void OuterBorderIsAlwaysWall()
        {
            for (int seed = 0; seed < 100; seed++)
            {
                var l = DungeonGenerator.Generate(seed);
                for (int x = 0; x < l.Width; x++)
                {
                    Assert.IsFalse(l.IsFloor(x, 0));
                    Assert.IsFalse(l.IsFloor(x, l.Height - 1));
                }
                for (int y = 0; y < l.Height; y++)
                {
                    Assert.IsFalse(l.IsFloor(0, y));
                    Assert.IsFalse(l.IsFloor(l.Width - 1, y));
                }
            }
        }

        [Test]
        public void SameSeedGivesSameLayout()
        {
            var a = DungeonGenerator.Generate(1234);
            var b = DungeonGenerator.Generate(1234);
            Assert.AreEqual(a.Rooms.Count, b.Rooms.Count);
            for (int x = 0; x < a.Width; x++)
                for (int y = 0; y < a.Height; y++)
                    Assert.AreEqual(a.Tiles[x, y], b.Tiles[x, y]);
        }

        [Test]
        public void BossRoomIsFarthestFromStart()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                var l = DungeonGenerator.Generate(seed);
                Assert.AreNotEqual(l.StartRoom, l.BossRoom);
                var s = l.Rooms[l.StartRoom].Center;
                var dist = new Pathfinder(l).Distances(s.X, s.Y);
                int bossDist = dist[l.Rooms[l.BossRoom].CenterX, l.Rooms[l.BossRoom].CenterY];
                for (int i = 0; i < l.Rooms.Count; i++)
                    Assert.GreaterOrEqual(bossDist, dist[l.Rooms[i].CenterX, l.Rooms[i].CenterY]);
            }
        }

        [Test]
        public void PathIsContiguousAndEndsAtGoal()
        {
            var l = DungeonGenerator.Generate(7);
            var pf = new Pathfinder(l);
            var s = l.Rooms[l.StartRoom].Center;
            var g = l.Rooms[l.BossRoom].Center;
            var path = new List<Cell>();
            Assert.IsTrue(pf.FindPath(s.X, s.Y, g.X, g.Y, path));
            Assert.AreEqual(g.X, path[path.Count - 1].X);
            Assert.AreEqual(g.Y, path[path.Count - 1].Y);

            int px = s.X, py = s.Y;
            foreach (var c in path)
            {
                Assert.IsTrue(l.IsFloor(c.X, c.Y));
                Assert.AreEqual(1, System.Math.Abs(c.X - px) + System.Math.Abs(c.Y - py));
                px = c.X; py = c.Y;
            }
            // BFS path length must equal BFS distance.
            var dist = pf.Distances(s.X, s.Y);
            Assert.AreEqual(dist[g.X, g.Y], path.Count);
        }

        [Test]
        public void PathToWallFails()
        {
            var l = DungeonGenerator.Generate(3);
            var s = l.Rooms[0].Center;
            Assert.IsFalse(new Pathfinder(l).FindPath(s.X, s.Y, 0, 0, new List<Cell>()));
        }
    }

    public class CombatTests
    {
        [Test]
        public void DamageFollowsFormula()
        {
            Assert.AreEqual(10, CombatMath.Damage(10, 0, 1f, false));
            Assert.AreEqual(5, CombatMath.Damage(10, 100, 1f, false));   // 50% mitigation
            Assert.AreEqual(15, CombatMath.Damage(10, 0, 1f, true));     // crit x1.5
            Assert.AreEqual(18, CombatMath.Damage(10, 0, 1.8f, false));
        }

        [Test]
        public void DamageNeverBelowOne()
        {
            Assert.AreEqual(1, CombatMath.Damage(1, 10000, 0.1f, false));
            Assert.AreEqual(1, CombatMath.Damage(0, 0, 1f, false));
        }

        [Test]
        public void CritRollBoundary()
        {
            Assert.IsTrue(CombatMath.IsCrit(0.0f));
            Assert.IsTrue(CombatMath.IsCrit(0.099f));
            Assert.IsFalse(CombatMath.IsCrit(0.10f));
            Assert.IsFalse(CombatMath.IsCrit(0.99f));
        }
    }

    public class ProgressionTests
    {
        [Test]
        public void XpCurveIsIncreasing()
        {
            for (int l = 1; l < Progression.MaxLevel; l++)
                Assert.Greater(Progression.XpForNextLevel(l + 1), Progression.XpForNextLevel(l));
        }

        [Test]
        public void AddXpHandlesMultipleLevelUps()
        {
            int level = 1, xp = 0;
            int need1 = Progression.XpForNextLevel(1);
            int need2 = Progression.XpForNextLevel(2);
            int gained = Progression.AddXp(ref level, ref xp, need1 + need2 + 5);
            Assert.AreEqual(2, gained);
            Assert.AreEqual(3, level);
            Assert.AreEqual(5, xp);
        }

        [Test]
        public void AddXpCapsAtMaxLevel()
        {
            int level = Progression.MaxLevel, xp = 0;
            Assert.AreEqual(0, Progression.AddXp(ref level, ref xp, 1000000));
            Assert.AreEqual(Progression.MaxLevel, level);
        }

        [Test]
        public void StatsAndFloorScaling()
        {
            Assert.AreEqual(100, Progression.MaxHp(1));
            Assert.AreEqual(115, Progression.MaxHp(2));
            Assert.AreEqual(1f, Progression.EnemyScale(1), 0.0001f);
            Assert.AreEqual(1.35f, Progression.EnemyScale(2), 0.0001f);
            Assert.AreEqual(8, Progression.EnemyCount(1));
            Assert.AreEqual(20, Progression.EnemyCount(50));
        }

        [Test]
        public void GolemsOnlyFromFloorTwo()
        {
            for (double r = 0; r < 1; r += 0.01)
                Assert.AreNotEqual(EnemyKind.StoneGolem, EnemyCatalog.PickForFloor(1, r));
            Assert.AreEqual(EnemyKind.StoneGolem, EnemyCatalog.PickForFloor(2, 0.9));
        }
    }

    public class SkillTests
    {
        [Test]
        public void CatalogIsSane()
        {
            Assert.AreEqual(4, SkillCatalog.All.Length);
            int prevUnlock = 0;
            foreach (var s in SkillCatalog.All)
            {
                Assert.Greater(s.Cooldown, 0f);
                Assert.Greater(s.Multiplier, 0f);
                Assert.GreaterOrEqual(s.Hits, 1);
                Assert.GreaterOrEqual(s.UnlockLevel, prevUnlock);
                prevUnlock = s.UnlockLevel;
            }
            Assert.IsTrue(SkillCatalog.IsUnlocked(SkillCatalog.All[0], 1));
            Assert.IsFalse(SkillCatalog.IsUnlocked(SkillCatalog.All[3], 3));
            Assert.IsTrue(SkillCatalog.IsUnlocked(SkillCatalog.All[3], 4));
        }

        [Test]
        public void CooldownTicksDownAndReadies()
        {
            var cd = new Cooldown(2f);
            Assert.IsTrue(cd.Ready);
            cd.Start();
            Assert.IsFalse(cd.Ready);
            Assert.AreEqual(1f, cd.Normalized, 0.0001f);
            cd.Tick(1f);
            Assert.AreEqual(0.5f, cd.Normalized, 0.0001f);
            cd.Tick(5f);
            Assert.IsTrue(cd.Ready);
            Assert.AreEqual(0f, cd.Remaining);
        }

        [Test]
        public void ComboAdvancesWithinWindowAndResetsAfter()
        {
            var c = new ComboChain();
            Assert.AreEqual(0, c.Next(0f));
            Assert.AreEqual(1, c.Next(0.5f));
            Assert.AreEqual(2, c.Next(1.0f));
            Assert.AreEqual(0, c.Next(1.5f));          // wraps after 3 hits
            Assert.AreEqual(0, c.Next(10f));           // window expired
            Assert.AreEqual(1, c.Next(10.4f));
            c.Reset();
            Assert.AreEqual(0, c.Next(10.6f));
        }
    }

    public class LootTests
    {
        [Test]
        public void SameSeedGivesSameItem()
        {
            var a = LootGenerator.Roll(new System.Random(5), 3, false);
            var b = LootGenerator.Roll(new System.Random(5), 3, false);
            Assert.AreEqual(a.Name, b.Name);
            Assert.AreEqual(a.Attack, b.Attack);
            Assert.AreEqual(a.Defense, b.Defense);
        }

        [Test]
        public void BossLootIsAlwaysRareOrBetter()
        {
            var rng = new System.Random(1);
            for (int i = 0; i < 500; i++)
                Assert.GreaterOrEqual((int)LootGenerator.Roll(rng, 2, true).Rarity, (int)Rarity.Rare);
        }

        [Test]
        public void HigherRarityAndFloorMeansStrongerWeapons()
        {
            float common = 0f, legendary = 0f, floor1 = 0f, floor10 = 0f;
            for (int i = 0; i < 200; i++)
            {
                var rng = new System.Random(i);
                common += LootGenerator.RollOf(rng, 5, ItemSlot.Weapon, Rarity.Common).Attack;
                legendary += LootGenerator.RollOf(rng, 5, ItemSlot.Weapon, Rarity.Legendary).Attack;
                floor1 += LootGenerator.RollOf(rng, 1, ItemSlot.Weapon, Rarity.Rare).Attack;
                floor10 += LootGenerator.RollOf(rng, 10, ItemSlot.Weapon, Rarity.Rare).Attack;
            }
            Assert.Greater(legendary, common * 2f);
            Assert.Greater(floor10, floor1 * 2f);
        }

        [Test]
        public void ItemsAlwaysHavePositiveStatsForTheirSlot()
        {
            var rng = new System.Random(9);
            for (int i = 0; i < 500; i++)
            {
                var it = LootGenerator.Roll(rng, 1 + i % 12, i % 10 == 0);
                Assert.IsFalse(string.IsNullOrEmpty(it.Name));
                if (it.Slot == ItemSlot.Weapon) Assert.Greater(it.Attack, 0);
                if (it.Slot == ItemSlot.Armor) { Assert.Greater(it.Defense, 0); Assert.Greater(it.MaxHp, 0); }
                if (it.Slot == ItemSlot.Trinket) Assert.Greater(it.Crit, 0f);
            }
        }

        [Test]
        public void DropRateIsAboutThirtyPercent()
        {
            var rng = new System.Random(123);
            int drops = 0;
            for (int i = 0; i < 10000; i++) if (LootGenerator.ShouldDrop(rng, false)) drops++;
            Assert.Greater(drops, 2700);
            Assert.Less(drops, 3300);
            Assert.IsTrue(LootGenerator.ShouldDrop(rng, true));
        }

        [Test]
        public void EquipSwapsAndTotalsAreSummed()
        {
            var eq = new Equipment();
            var sword1 = new Item { Name = "A", Slot = ItemSlot.Weapon, Attack = 10 };
            var sword2 = new Item { Name = "B", Slot = ItemSlot.Weapon, Attack = 20 };
            var armor = new Item { Name = "C", Slot = ItemSlot.Armor, Defense = 5, MaxHp = 30 };
            var ring = new Item { Name = "D", Slot = ItemSlot.Trinket, Crit = 0.05f, Attack = 2 };

            Assert.IsNull(eq.Equip(sword1));
            eq.Equip(armor);
            eq.Equip(ring);
            Assert.AreEqual(12, eq.BonusAttack);
            Assert.AreEqual(5, eq.BonusDefense);
            Assert.AreEqual(30, eq.BonusMaxHp);
            Assert.AreEqual(0.05f, eq.BonusCrit, 0.0001f);

            var replaced = eq.Equip(sword2);
            Assert.AreEqual("A", replaced.Name);
            Assert.AreEqual(22, eq.BonusAttack);
        }

        [Test]
        public void InventoryRespectsCapacity()
        {
            var inv = new Inventory();
            for (int i = 0; i < Inventory.Capacity; i++) Assert.IsTrue(inv.Add(new Item { Name = "x" + i }));
            Assert.IsTrue(inv.IsFull);
            Assert.IsFalse(inv.Add(new Item { Name = "overflow" }));
            Assert.AreEqual("x3", inv.RemoveAt(3).Name);
            Assert.IsFalse(inv.IsFull);
            Assert.IsNull(inv.RemoveAt(99));
        }

        [Test]
        public void CritChanceOverloadIsCapped()
        {
            Assert.IsTrue(CombatMath.IsCrit(0.5f, 0.6f));
            Assert.IsFalse(CombatMath.IsCrit(0.9f, 5f));   // capped at 75%
            Assert.IsTrue(CombatMath.IsCrit(0.7f, 5f));
        }
    }
}

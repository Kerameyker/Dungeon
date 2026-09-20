using System;
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

    public class ContentTests
    {
        [Test]
        public void ArchersAppearOnlyFromFloorTwo()
        {
            bool seenFloor1 = false, seenFloor2 = false;
            for (double r = 0; r < 1; r += 0.01)
            {
                if (EnemyCatalog.PickForFloor(1, r) == EnemyKind.SkeletonArcher) seenFloor1 = true;
                if (EnemyCatalog.PickForFloor(2, r) == EnemyKind.SkeletonArcher) seenFloor2 = true;
            }
            Assert.IsFalse(seenFloor1);
            Assert.IsTrue(seenFloor2);
        }

        [Test]
        public void EveryKindHasADefinitionAndBossesAlternate()
        {
            foreach (EnemyKind k in System.Enum.GetValues(typeof(EnemyKind)))
            {
                var d = EnemyCatalog.Get(k);
                Assert.Greater(d.BaseHp, 0);
                Assert.Greater(d.Speed, 0f);
                Assert.Greater(d.AttackRange, 0f);
            }
            Assert.IsTrue(EnemyCatalog.Get(EnemyCatalog.BossForFloor(1)).IsBoss);
            Assert.IsTrue(EnemyCatalog.Get(EnemyCatalog.BossForFloor(2)).IsBoss);
            Assert.AreEqual(EnemyKind.FloorGuardian, EnemyCatalog.BossForFloor(1));
            Assert.AreEqual(EnemyKind.CrimsonKnight, EnemyCatalog.BossForFloor(2));
            Assert.IsTrue(EnemyCatalog.Get(EnemyKind.SkeletonArcher).Ranged);
            Assert.IsTrue(EnemyCatalog.Get(EnemyKind.CrimsonKnight).Charge);
        }

        [Test]
        public void EliteAndChestRaritiesAreNeverCommon()
        {
            var rng = new System.Random(77);
            for (int i = 0; i < 1000; i++)
            {
                Assert.GreaterOrEqual((int)LootGenerator.RollEliteRarity(rng), (int)Rarity.Uncommon);
                Assert.GreaterOrEqual((int)LootGenerator.RollChestRarity(rng), (int)Rarity.Uncommon);
            }
        }
    }

    public class EconomyTests
    {
        [Test]
        public void GoldGrowsWithFloorAndEliteBoss()
        {
            var rng = new System.Random(1);
            int a = Economy.GoldDrop(new System.Random(5), 1, false, false);
            int b = Economy.GoldDrop(new System.Random(5), 10, false, false);
            Assert.Greater(b, a);
            Assert.AreEqual(Economy.GoldDrop(new System.Random(5), 3, false, false) * 3, Economy.GoldDrop(new System.Random(5), 3, true, false));
            Assert.Greater(Economy.GoldDrop(rng, 3, false, true), Economy.GoldDrop(new System.Random(5), 3, true, false));
        }

        [Test]
        public void SellValueRisesWithRarity()
        {
            var common = new Item { Name = "a", Rarity = Rarity.Common, Attack = 10 };
            var epic = new Item { Name = "b", Rarity = Rarity.Epic, Attack = 10 };
            Assert.Greater(Economy.SellValue(epic), Economy.SellValue(common));
            Assert.AreEqual(0, Economy.SellValue(null));
            Assert.GreaterOrEqual(Economy.SellValue(new Item { Name = "c" }), 1);
        }

        [Test]
        public void BuyPriceRisesAndSaveDataDefaults()
        {
            Assert.Greater(Economy.BuyPrice(5), Economy.BuyPrice(1));
            var s = new SaveData();
            Assert.AreEqual(1, s.Level);
            Assert.AreEqual(3, s.Gear.Length);
            Assert.IsTrue(SaveData.IsEmpty(s.Gear[0]));
            Assert.IsTrue(SaveData.IsEmpty(new Item()));
            Assert.IsFalse(SaveData.IsEmpty(new Item { Name = "x" }));
        }
    }

    public class WeaponTests
    {
        [Test]
        public void EveryWeaponHasFourValidSkillsAndDualAddsOne()
        {
            foreach (WeaponType t in Enum.GetValues(typeof(WeaponType)))
            {
                var set = SkillSets.For(t);
                Assert.AreEqual(4, set.Length, t.ToString());
                var ids = new HashSet<string>();
                foreach (var sk in set)
                {
                    Assert.IsTrue(ids.Add(sk.Id), "duplicate id " + sk.Id);
                    Assert.Greater(sk.Cooldown, 0f);
                    Assert.Greater(sk.Multiplier, 0f);
                    Assert.Greater(sk.Hits, 0);
                    Assert.Greater(sk.Duration, 0f);
                }
                Assert.AreEqual(4, SkillSets.Hotbar(t, false).Length);
                Assert.AreEqual(5, SkillSets.Hotbar(t, true).Length);
                Assert.AreEqual(3, WeaponCatalog.Get(t).Combo.Length);
            }
        }

        [Test]
        public void LootRollsWeaponTypesAndStyles()
        {
            var rng = new Random(3);
            var seen = new HashSet<WeaponType>();
            for (int n = 0; n < 200; n++)
            {
                var it = LootGenerator.RollOf(rng, 3, ItemSlot.Weapon, Rarity.Rare);
                seen.Add(it.WType);
                Assert.IsTrue(it.Style >= 0 && it.Style < 4);
                Assert.Greater(it.Seed, 0);
            }
            Assert.AreEqual(4, seen.Count);
        }

        [Test]
        public void UpgradesRaiseEffectiveStatsAndTotals()
        {
            var w = new Item { Name = "w", Slot = ItemSlot.Weapon, Attack = 100 };
            Assert.AreEqual(100, w.EffAttack);
            w.Upgrade = 5;
            Assert.AreEqual(140, w.EffAttack);
            var eq = new Equipment();
            eq.Equip(w);
            Assert.AreEqual(140, eq.BonusAttack);
            Assert.AreEqual("w +5", w.DisplayName);
            Assert.AreEqual(WeaponType.Sword, new Equipment().Weapon);
        }
    }

    public class SystemsTests
    {
        [Test]
        public void SmithingCostsAndOutcomes()
        {
            var it = new Item { Name = "x", Slot = ItemSlot.Weapon, Attack = 10, Rarity = Rarity.Rare };
            int gold = 10000, shards = 100;
            Assert.AreEqual(SmithResult.Success, Smithing.Attempt(it, new Random(1), ref gold, ref shards));   // +0 always succeeds
            Assert.AreEqual(1, it.Upgrade);
            Assert.Less(gold, 10000);
            Assert.Less(shards, 100);

            int g0 = 0, s0 = 100;
            Assert.AreEqual(SmithResult.NotEnoughGold, Smithing.Attempt(it, new Random(1), ref g0, ref s0));
            int g1 = 10000, s1 = 0;
            Assert.AreEqual(SmithResult.NotEnoughShards, Smithing.Attempt(it, new Random(1), ref g1, ref s1));

            it.Upgrade = Smithing.MaxUpgrade;
            Assert.AreEqual(SmithResult.MaxLevel, Smithing.Attempt(it, new Random(1), ref gold, ref shards));
        }

        [Test]
        public void SmithingHighLevelFailureDowngradesAndChanceFalls()
        {
            Assert.Greater(Smithing.SuccessChance(1), Smithing.SuccessChance(8));
            var it = new Item { Name = "x", Slot = ItemSlot.Armor, Defense = 10, Upgrade = 9 };
            int gold = 100000000, shards = 1000000, downgrades = 0, success = 0;
            for (int n = 0; n < 200; n++)
            {
                it.Upgrade = 9;
                var r = Smithing.Attempt(it, new Random(n), ref gold, ref shards);
                if (r == SmithResult.FailedDowngrade) { downgrades++; Assert.AreEqual(8, it.Upgrade); }
                if (r == SmithResult.Success) success++;
            }
            Assert.Greater(downgrades, 100);
            Assert.Greater(success, 5);
            var low = new Item { Name = "l", Upgrade = 2 };
            // failures below +5 never downgrade
            for (int n = 0; n < 200; n++)
            {
                low.Upgrade = 3;
                var r = Smithing.Attempt(low, new Random(n), ref gold, ref shards);
                Assert.AreNotEqual(SmithResult.FailedDowngrade, r);
            }
        }

        [Test]
        public void MasteryLevelsUpWithUse()
        {
            var m = new SkillMastery();
            Assert.AreEqual(0, m.Level("a"));
            bool leveled = false;
            for (int i = 0; i < 10; i++) leveled |= m.AddUse("a");
            Assert.IsTrue(leveled);
            Assert.AreEqual(1, m.Level("a"));
            Assert.AreEqual(1.06f, m.DamageMult("a"), 0.001f);
            Assert.Less(m.CooldownMult("a"), 1f);
            for (int i = 0; i < 500; i++) m.AddUse("a");
            Assert.AreEqual(SkillMastery.MaxLevel, m.Level("a"));
            Assert.AreEqual(0, m.UsesToNext("a"));
        }

        [Test]
        public void ConsumablesRespectCaps()
        {
            var c = new Consumables();
            for (int i = 0; i < Consumables.PotionCap; i++) Assert.IsTrue(c.AddPotion());
            Assert.IsFalse(c.AddPotion());
            Assert.IsTrue(c.UsePotion());
            Assert.IsFalse(new Consumables().UseRevive());
        }

        [Test]
        public void BestiaryScoutingGivesBonusOnce()
        {
            var b = new Bestiary();
            Assert.AreEqual(1f, b.DamageMultVs(EnemyKind.CrimsonKnight));
            Assert.IsTrue(b.Scout(EnemyKind.CrimsonKnight));
            Assert.IsFalse(b.Scout(EnemyKind.CrimsonKnight));
            Assert.AreEqual(1.10f, b.DamageMultVs(EnemyKind.CrimsonKnight), 0.001f);
            b.RecordKill(EnemyKind.FrenzyBoar);
            Assert.AreEqual(1, b.KillsOf(EnemyKind.FrenzyBoar));
        }
    }

    public class QuestStoryTests
    {
        [Test]
        public void QuestProgressAndClaim()
        {
            var log = new QuestLog();
            Assert.IsTrue(log.Accept("q_slay10", 1));
            Assert.IsFalse(log.Accept("q_slay10", 1));
            for (int i = 0; i < 9; i++) log.OnKill(EnemyKind.FrenzyBoar, false, false);
            Assert.IsNull(log.Claim("q_slay10"));
            log.OnKill(EnemyKind.BoneSoldier, false, false);
            var def = log.Claim("q_slay10");
            Assert.IsNotNull(def);
            Assert.IsTrue(log.Completed.Contains("q_slay10"));
            Assert.IsFalse(log.Accept("q_slay10", 1));
        }

        [Test]
        public void QuestLimitAndFloorQuests()
        {
            var log = new QuestLog();
            Assert.IsTrue(log.Accept("q_slay10", 1));
            Assert.IsTrue(log.Accept("q_chest2", 1));
            Assert.IsTrue(log.Accept("q_floor3", 1));
            Assert.IsFalse(log.Accept("q_boss1", 1));   // max 3 active
            log.OnFloorReached(3);
            Assert.IsNotNull(log.Claim("q_floor3"));
            Assert.IsTrue(log.Available(1).Count > 0);
            // reach-floor quests count progress already made
            var l2 = new QuestLog();
            l2.Accept("q_floor3", 5);
            Assert.IsNotNull(l2.Claim("q_floor3"));
        }

        [Test]
        public void EveryQuestHasValidData()
        {
            var ids = new HashSet<string>();
            foreach (var q in QuestCatalog.All)
            {
                Assert.IsTrue(ids.Add(q.Id));
                Assert.Greater(q.Count, 0);
                Assert.Greater(q.RewardGold, 0);
            }
        }

        [Test]
        public void StoryBeatsAreOrderedAndRequirementsResolve()
        {
            var ids = new HashSet<string>();
            foreach (var b in StoryCatalog.Beats)
            {
                Assert.IsTrue(ids.Add(b.Id), "dup " + b.Id);
                Assert.Greater(b.Lines.Length, 0);
                if (b.Requires != null) Assert.IsTrue(ids.Contains(b.Requires) || StoryCatalog.Get(b.Requires) != null, b.Id);
            }
            var flags = new StoryFlags();
            var first = StoryCatalog.Next(StoryTrigger.TownEnter, 0, 1, flags);
            Assert.AreEqual("prologue", first.Id);
            flags.Add("prologue");
            Assert.IsNull(StoryCatalog.Next(StoryTrigger.TownEnter, 0, 1, flags));
            Assert.AreEqual("first_steps", StoryCatalog.Next(StoryTrigger.FloorEnter, 1, 1, flags).Id);
            Assert.IsNull(StoryCatalog.Next(StoryTrigger.BossKilled, 2, 2, flags) == null ? null : StoryCatalog.Next(StoryTrigger.BossKilled, 3, 3, flags));
            Assert.AreEqual("meet_miri", StoryCatalog.Next(StoryTrigger.BossKilled, 2, 2, flags).Id);
            // final beats only after the reveal
            Assert.IsNull(StoryCatalog.Next(StoryTrigger.BossKilled, 20, 20, flags));
            Assert.AreEqual(BossInfo.FinalFloor, 20);
        }

        [Test]
        public void BossBarsAndBiomes()
        {
            Assert.AreEqual(5, BossInfo.Bars(EnemyKind.Architect, 20));
            Assert.AreEqual(4, BossInfo.Bars(EnemyKind.FloorGuardian, 5));
            Assert.AreEqual(2, BossInfo.Bars(EnemyKind.CrimsonKnight, 4));
            Assert.AreEqual(EnemyKind.Architect, EnemyCatalog.BossForFloor(20));
            Assert.AreEqual("", BossInfo.Title(EnemyKind.CrimsonKnight, 4));
            Assert.AreNotEqual("", BossInfo.Title(EnemyKind.FloorGuardian, 5));

            Assert.AreEqual(0, BossInfo.BarIndex(1000, 1000, 4));
            Assert.AreEqual(1f, BossInfo.BarFill(1000, 1000, 4), 0.001f);
            Assert.AreEqual(1, BossInfo.BarIndex(700, 1000, 4));
            Assert.AreEqual(0.8f, BossInfo.BarFill(700, 1000, 4), 0.001f);
            Assert.AreEqual(3, BossInfo.BarIndex(100, 1000, 4));
            Assert.AreEqual(0f, BossInfo.BarFill(0, 1000, 4));

            Assert.AreEqual(0, BiomeCatalog.IndexFor(1));
            Assert.AreEqual(0, BiomeCatalog.IndexFor(5));
            Assert.AreEqual(1, BiomeCatalog.IndexFor(6));
            Assert.AreEqual(3, BiomeCatalog.IndexFor(20));
            Assert.AreEqual(0, BiomeCatalog.IndexFor(21));
        }
    }
}

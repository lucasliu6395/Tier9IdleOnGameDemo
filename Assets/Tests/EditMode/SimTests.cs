using NUnit.Framework;
using Tier9.Content;
using Tier9.Core;
using Tier9.Sim;
using UnityEngine;

namespace Tier9.Tests
{
    public class SimTests
    {
        static AccountState MakeAccount(TaskType task, string target)
        {
            var acc = AccountState.CreateNew();
            var ch = AccountState.CreateCharacter("Tester");
            ch.SetTask(task, target);
            acc.characters.Add(ch);
            return acc;
        }

        static AccountState Clone(AccountState acc) =>
            JsonUtility.FromJson<AccountState>(JsonUtility.ToJson(acc));

        [Test]
        public void XpCurve_IsMonotonicallyIncreasing()
        {
            for (int level = 1; level < 100; level++)
            {
                Assert.Greater(XpCurve.ToNext(level + 1), XpCurve.ToNext(level));
                Assert.Greater(XpCurve.SkillToNext(level + 1), XpCurve.SkillToNext(level));
            }
        }

        [Test]
        public void CombatSim_ProducesKillsXpAndCoins()
        {
            var acc = MakeAccount(TaskType.Combat, "meadow_edge");
            var result = AfkSimulator.SimulateChunked(acc, acc.characters[0], 3600);
            Assert.Greater(result.kills, 0);
            Assert.Greater(result.classXp, 0);
            Assert.Greater(result.coins, 0);
            Assert.Greater(acc.characters[0].GetKills("meadow_edge"), 0);
        }

        [Test]
        public void GatherSim_ProducesResourcesAndSkillXp()
        {
            var acc = MakeAccount(TaskType.Mining, "copper_vein");
            var result = AfkSimulator.SimulateChunked(acc, acc.characters[0], 3600);
            Assert.Greater(acc.GetItemCount("copper_ore"), 0);
            Assert.Greater(result.skillXp, 0);
        }

        [Test]
        public void OfflineChunks_MatchLiveTicks()
        {
            const double hours = 2;
            var offlineAcc = MakeAccount(TaskType.Combat, "meadow_edge");
            var liveAcc = Clone(offlineAcc);

            var offline = AfkSimulator.SimulateChunked(offlineAcc, offlineAcc.characters[0], hours * 3600);

            var live = new AfkResult();
            for (int i = 0; i < hours * 3600; i++)
                live.Merge(AfkSimulator.Simulate(liveAcc, liveAcc.characters[0], 1.0));

            Assert.Greater(live.kills, 0);
            Assert.That(offline.kills, Is.EqualTo(live.kills).Within(2).Percent,
                "offline and live kill totals should match");
            Assert.That(offline.classXp, Is.EqualTo(live.classXp).Within(2).Percent,
                "offline and live XP totals should match");
            Assert.That(offlineAcc.coins, Is.EqualTo(liveAcc.coins).Within(2).Percent,
                "offline and live coin totals should match");
        }

        [Test]
        public void ZoneUnlock_RequiresKillQuota()
        {
            var acc = MakeAccount(TaskType.Idle, "");
            var ch = acc.characters[0];
            var zones = ContentDatabase.Zones;

            bool ZoneUnlocked(string zoneId)
            {
                int idx = ContentDatabase.ZoneIndex(zoneId);
                if (idx <= 0) return idx == 0;
                var prev = zones[idx - 1];
                return ch.GetKills(prev.id) >= prev.killsToNext;
            }

            Assert.IsTrue(ZoneUnlocked(zones[0].id), "first zone is always open");
            Assert.IsFalse(ZoneUnlocked(zones[1].id), "second zone locked at 0 kills");
            ch.AddKills(zones[0].id, zones[0].killsToNext);
            Assert.IsTrue(ZoneUnlocked(zones[1].id), "second zone opens at quota");
        }

        [Test]
        public void StampsAndTalents_BoostStats()
        {
            var acc = MakeAccount(TaskType.Idle, "");
            var ch = acc.characters[0];
            var baseline = StatCalculator.Compute(acc, ch);

            acc.stamps.Add(new StampLevel { stampId = "sword_stamp", level = 5 });
            var withStamp = StatCalculator.Compute(acc, ch);
            Assert.Greater(withStamp.damage, baseline.damage, "damage stamp raises damage account-wide");

            ch.level = 10;
            ch.talents.Add(new TalentRank { talentId = "sharp_stick", rank = 5 });
            var withTalent = StatCalculator.Compute(acc, ch);
            Assert.Greater(withTalent.damage, withStamp.damage, "flat damage talent raises damage");
        }

        [Test]
        public void Equipment_ChangesRates()
        {
            var acc = MakeAccount(TaskType.Combat, "meadow_edge");
            var ch = acc.characters[0];
            var mon = ContentDatabase.Monster("puffshroom");

            double bare = AfkSimulator.KillsPerSecond(StatCalculator.Compute(acc, ch), mon);
            ch.weaponId = "iron_blade";
            double armed = AfkSimulator.KillsPerSecond(StatCalculator.Compute(acc, ch), mon);
            Assert.Greater(armed, bare, "a weapon speeds up kills");

            double bareMine = AfkSimulator.GatherPerSecond(StatCalculator.Compute(acc, ch), ContentDatabase.Node("copper_vein"));
            ch.pickId = "iron_pick";
            double toolMine = AfkSimulator.GatherPerSecond(StatCalculator.Compute(acc, ch), ContentDatabase.Node("copper_vein"));
            Assert.Greater(toolMine, bareMine, "a pickaxe speeds up mining");
        }

        [Test]
        public void SaveRoundtrip_PreservesState()
        {
            var acc = MakeAccount(TaskType.Combat, "meadow_edge");
            acc.coins = 123.45;
            acc.AddItem("copper_ore", 42);
            acc.stamps.Add(new StampLevel { stampId = "sword_stamp", level = 3 });
            acc.characters[0].level = 7;
            acc.characters[0].talents.Add(new TalentRank { talentId = "sharp_stick", rank = 2 });

            var loaded = Clone(acc);
            Assert.AreEqual(acc.coins, loaded.coins, 0.0001);
            Assert.AreEqual(42, loaded.GetItemCount("copper_ore"));
            Assert.AreEqual(3, loaded.GetStampLevel("sword_stamp"));
            Assert.AreEqual(7, loaded.characters[0].level);
            Assert.AreEqual(2, loaded.characters[0].GetTalentRank("sharp_stick"));
            Assert.AreEqual(TaskType.Combat, loaded.characters[0].task);
            Assert.AreEqual("meadow_edge", loaded.characters[0].taskTargetId);
        }

        [Test]
        public void ContentDatabase_ReferencesAreValid()
        {
            foreach (var z in ContentDatabase.Zones)
                Assert.NotNull(ContentDatabase.Monster(z.monsterId), $"zone {z.id} monster exists");
            foreach (var m in ContentDatabase.Monsters)
                Assert.NotNull(ContentDatabase.Item(m.dropItemId), $"monster {m.id} drop exists");
            foreach (var n in ContentDatabase.Nodes)
                Assert.NotNull(ContentDatabase.Item(n.itemId), $"node {n.id} yield exists");
            foreach (var r in ContentDatabase.Recipes)
            {
                Assert.NotNull(ContentDatabase.Item(r.resultItemId), $"recipe {r.id} result exists");
                foreach (var c in r.cost)
                    Assert.NotNull(ContentDatabase.Item(c.itemId), $"recipe {r.id} cost item exists");
            }
            foreach (var s in ContentDatabase.Stamps)
                Assert.NotNull(ContentDatabase.Item(s.costItemId), $"stamp {s.id} cost item exists");
            foreach (var c in ContentDatabase.Classes)
                foreach (var t in c.talentIds)
                    Assert.NotNull(ContentDatabase.Talent(t), $"class {c.id} talent {t} exists");
        }
    }
}

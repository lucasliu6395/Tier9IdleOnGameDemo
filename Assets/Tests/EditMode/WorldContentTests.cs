using NUnit.Framework;
using Tier9.Content;
using Tier9.Core;
using Tier9.Sim;
using UnityEngine;

namespace Tier9.Tests
{
    public class WorldContentTests
    {
        static AccountState MakeAccount(TaskType task, string target)
        {
            var acc = AccountState.CreateNew();
            var ch = AccountState.CreateCharacter("Tester");
            ch.SetTask(task, target);
            acc.characters.Add(ch);
            return acc;
        }

        [Test]
        public void LiveKillRewards_MatchSimRewards()
        {
            // Simulate an hour of AFK combat...
            var simAcc = MakeAccount(TaskType.Combat, "meadow_edge");
            var simResult = AfkSimulator.SimulateChunked(simAcc, simAcc.characters[0], 3600);
            Assert.Greater(simResult.kills, 0);

            // ...then hand a fresh identical character the same number of kills through
            // the live-play path. Totals must match: same code, same rewards.
            var liveAcc = MakeAccount(TaskType.Combat, "meadow_edge");
            var liveCh = liveAcc.characters[0];
            var zone = ContentDatabase.Zone("meadow_edge");
            var mon = ContentDatabase.Monster(zone.monsterId);
            var liveResult = new AfkResult();
            var stats = StatCalculator.Compute(liveAcc, liveCh);
            AfkSimulator.ApplyKillRewards(liveAcc, liveCh, zone, mon, simResult.kills, stats, liveResult);

            Assert.AreEqual(simResult.kills, liveResult.kills);
            Assert.AreEqual(simResult.classXp, liveResult.classXp, simResult.classXp * 0.001,
                "same kills must grant the same XP in live play and AFK sim");
            Assert.AreEqual(simAcc.coins, liveAcc.coins, simAcc.coins * 0.001,
                "same kills must grant the same coins in live play and AFK sim");
            Assert.AreEqual(simAcc.characters[0].GetKills("meadow_edge"), liveCh.GetKills("meadow_edge"));
        }

        [Test]
        public void DeferredLoot_MatchesInstantRewards_OnceCredited()
        {
            // Physical drop pickups defer crediting coins/items until the player collects
            // them, using ApplyKillXpAndComputeLoot instead of ApplyKillRewards. Once that
            // loot is credited (simulating a pickup), the account must end up identical to
            // the instant-crediting AFK path for the same number of kills.
            var zone = ContentDatabase.Zone("meadow_edge");
            var mon = ContentDatabase.Monster(zone.monsterId);
            const long kills = 40;

            var instantAcc = MakeAccount(TaskType.Combat, "meadow_edge");
            var instantCh = instantAcc.characters[0];
            var instantStats = StatCalculator.Compute(instantAcc, instantCh);
            var instantResult = new AfkResult();
            AfkSimulator.ApplyKillRewards(instantAcc, instantCh, zone, mon, kills, instantStats, instantResult);

            var deferredAcc = MakeAccount(TaskType.Combat, "meadow_edge");
            var deferredCh = deferredAcc.characters[0];
            double coinsBeforePickup = deferredAcc.coins; // CreateNew() seeds starting coins
            var deferredStats = StatCalculator.Compute(deferredAcc, deferredCh);
            var deferredResult = new AfkResult();
            var (coins, dropItemId, dropCount) = AfkSimulator.ApplyKillXpAndComputeLoot(deferredCh, zone, mon, kills, deferredStats, deferredResult);

            // Nothing credited yet — the "pickup" hasn't happened.
            Assert.AreEqual(coinsBeforePickup, deferredAcc.coins);
            Assert.AreEqual(0, deferredAcc.GetItemCount(mon.dropItemId));

            // Now simulate collecting the drop.
            if (coins > 0) deferredAcc.coins += coins;
            if (dropCount > 0) deferredAcc.AddItem(dropItemId, dropCount);

            Assert.AreEqual(instantCh.level, deferredCh.level);
            Assert.AreEqual(instantCh.xp, deferredCh.xp, 0.0001);
            Assert.AreEqual(instantAcc.coins, deferredAcc.coins, 0.0001);
            Assert.AreEqual(instantAcc.GetItemCount(mon.dropItemId), deferredAcc.GetItemCount(mon.dropItemId));
            Assert.AreEqual(instantCh.GetKills(zone.id), deferredCh.GetKills(zone.id));
        }

        [Test]
        public void ActiveGathering_OutpacesAfkGathering()
        {
            var acc = MakeAccount(TaskType.Mining, "copper_vein");
            var stats = StatCalculator.Compute(acc, acc.characters[0]);
            var node = ContentDatabase.Node("copper_vein");
            Assert.Greater(AfkSimulator.RawGatherPerSecond(stats, node),
                AfkSimulator.GatherPerSecond(stats, node),
                "active gathering skips the AFK-rate penalty");
        }

        [Test]
        public void Maps_ReferencesAreValid()
        {
            Assert.NotNull(ContentDatabase.Map("town"), "town map exists");
            foreach (var map in ContentDatabase.Maps)
            {
                if (map.IsCombat)
                {
                    Assert.NotNull(ContentDatabase.Monster(map.monsterId), $"map {map.id} monster exists");
                    Assert.NotNull(ContentDatabase.Zone(map.id), $"combat map {map.id} matches a zone id");
                    Assert.Greater(map.enemyCount, 0, $"combat map {map.id} spawns enemies");
                }
                if (map.IsSkill)
                {
                    Assert.NotNull(ContentDatabase.Node(map.nodeId), $"map {map.id} node exists");
                    Assert.AreEqual(map.id, map.nodeId, $"skill map {map.id} id matches its node id");
                    Assert.Greater(map.nodePositions.Count, 0, $"skill map {map.id} places nodes");
                }
                foreach (var portal in map.portals)
                {
                    var target = ContentDatabase.Map(portal.targetMapId);
                    Assert.NotNull(target, $"map {map.id} portal target {portal.targetMapId} exists");
                    Assert.That(portal.x, Is.InRange(0f, map.length), $"map {map.id} portal x inside map");
                }
                foreach (var station in map.stations)
                    Assert.IsTrue(station.kind == "anvil" || station.kind == "shop" || station.kind == "stamps",
                        $"map {map.id} station kind '{station.kind}' valid");
            }

            foreach (var zone in ContentDatabase.Zones)
                Assert.NotNull(ContentDatabase.Map(zone.id), $"zone {zone.id} has a walkable map");
            foreach (var node in ContentDatabase.Nodes)
                Assert.NotNull(ContentDatabase.Map(node.id), $"node {node.id} has a walkable map");
        }

        [Test]
        public void TravelTo_SetsTaskFromMap()
        {
            var acc = MakeAccount(TaskType.Idle, "");
            var ch = acc.characters[0];

            // TravelTo lives on GameManager (a MonoBehaviour), so verify the mapping rule directly.
            Assert.AreEqual("town", GameManager.MapIdForCharacter(ch), "idle characters are in town");
            ch.SetTask(TaskType.Combat, "meadow_edge");
            Assert.AreEqual("meadow_edge", GameManager.MapIdForCharacter(ch));
            ch.SetTask(TaskType.Mining, "copper_vein");
            Assert.AreEqual("copper_vein", GameManager.MapIdForCharacter(ch));
            ch.SetTask(TaskType.Combat, "no_such_zone");
            Assert.AreEqual("town", GameManager.MapIdForCharacter(ch), "unknown targets fall back to town");
        }

        [Test]
        public void BossContent_IsWiredUp()
        {
            var boss = ContentDatabase.Monster("mother_glowcap");
            Assert.NotNull(boss);
            Assert.IsTrue(boss.isBoss);
            Assert.Greater(boss.contactDamage, 0, "the boss can hurt the player");
            Assert.NotNull(ContentDatabase.Zone("glowcap_lair"));
            Assert.NotNull(ContentDatabase.Map("glowcap_lair"));

            var depths = ContentDatabase.Zone("fungal_depths");
            Assert.Greater(depths.killsToNext, 0, "the lair is gated behind a Fungal Depths quota");
        }
    }
}

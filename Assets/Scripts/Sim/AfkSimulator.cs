using System;
using Tier9.Content;
using Tier9.Core;

namespace Tier9.Sim
{
    /// <summary>
    /// The single simulation code path: the live 1-second tick and offline catch-up
    /// both run through Simulate(), so online and offline gains always match.
    /// </summary>
    public static class AfkSimulator
    {
        public const double MaxOfflineSeconds = 24 * 3600;
        public const double ChunkSeconds = 60;
        public const double AttacksPerSecond = 0.5;
        public const double RespawnSeconds = 3.0;
        public const double GatherPerHourCap = 400.0;

        /// <summary>Simulate a long span in chunks so level-ups raise stats as time passes.</summary>
        public static AfkResult SimulateChunked(AccountState acc, CharacterState ch, double seconds)
        {
            var total = new AfkResult();
            double remaining = Math.Min(seconds, MaxOfflineSeconds);
            while (remaining > 0)
            {
                double step = Math.Min(ChunkSeconds, remaining);
                total.Merge(Simulate(acc, ch, step));
                remaining -= step;
            }
            return total;
        }

        public static AfkResult Simulate(AccountState acc, CharacterState ch, double seconds)
        {
            var r = new AfkResult { seconds = seconds };
            if (seconds <= 0) return r;
            switch (ch.task)
            {
                case TaskType.Combat: SimulateCombat(acc, ch, seconds, r); break;
                case TaskType.Mining:
                case TaskType.Choppin: SimulateGather(acc, ch, seconds, r); break;
            }
            return r;
        }

        public static double KillsPerSecond(ComputedStats stats, MonsterDef mon)
        {
            double dps = Math.Max(0.01, stats.damage * AttacksPerSecond);
            double killTime = mon.hp / dps + RespawnSeconds;
            double rate = 1.0 / killTime;
            if (mon.reqDefense > 0 && stats.defense < mon.reqDefense)
                rate *= 1.0 - 0.5 * (mon.reqDefense - stats.defense) / mon.reqDefense;
            return rate * stats.afkRate;
        }

        /// <summary>AFK gather rate (background characters only — live gathering is hit-based, see PlayerController2D).</summary>
        public static double GatherPerSecond(ComputedStats stats, NodeDef node)
        {
            double eff = node.skill == SkillType.Mining ? stats.miningEff : stats.choppinEff;
            double perHour = GatherPerHourCap * eff / (eff + node.difficulty);
            return perHour / 3600.0 * stats.afkRate;
        }

        /// <summary>
        /// Applies kill counts and XP (always instant) and returns the coin/item loot
        /// the kills earned, without crediting it. AFK play credits that loot immediately
        /// (see ApplyKillRewards); live world-mode instead spawns physical drop pickups
        /// with it, so a kill's loot only reaches the account once it is collected.
        /// </summary>
        public static (double coins, string dropItemId, long dropCount) ApplyKillXpAndComputeLoot(
            CharacterState ch, ZoneDef zone, MonsterDef mon, long kills, ComputedStats stats, AfkResult r)
        {
            if (kills <= 0) return (0, null, 0);
            r.kills += kills;
            ch.AddKills(zone.id, kills);

            ApplyClassXp(ch, kills * mon.xp * stats.xpMult, r);

            double coins = kills * mon.coinAvg * stats.coinMult;

            ch.dropCarry += kills * mon.dropChance * stats.dropMult;
            long drops = (long)Math.Floor(ch.dropCarry);
            ch.dropCarry -= drops;

            return (coins, mon.dropItemId, drops);
        }

        /// <summary>
        /// Applies everything a batch of kills yields (kill counts, XP, coins, drops) instantly.
        /// Used by the AFK sim, where nobody is standing there to walk over physical loot.
        /// </summary>
        public static void ApplyKillRewards(AccountState acc, CharacterState ch, ZoneDef zone, MonsterDef mon, long kills, ComputedStats stats, AfkResult r)
        {
            var (coins, dropItemId, drops) = ApplyKillXpAndComputeLoot(ch, zone, mon, kills, stats, r);
            if (coins > 0)
            {
                acc.coins += coins;
                r.coins += coins;
            }
            if (drops > 0)
            {
                acc.AddItem(dropItemId, drops);
                r.AddItem(dropItemId, drops);
            }
        }

        /// <summary>Applies gathered resources + skill XP. Shared by the AFK sim and live gathering.</summary>
        public static void ApplyGatherRewards(AccountState acc, CharacterState ch, NodeDef node, long gathered, ComputedStats stats, AfkResult r)
        {
            if (gathered <= 0) return;
            acc.AddItem(node.itemId, gathered);
            r.AddItem(node.itemId, gathered);
            ApplySkillXp(ch, node.skill, gathered * node.xpPerItem * stats.xpMult, r);
        }

        static void SimulateCombat(AccountState acc, CharacterState ch, double seconds, AfkResult r)
        {
            var zone = ContentDatabase.Zone(ch.taskTargetId);
            var mon = zone == null ? null : ContentDatabase.Monster(zone.monsterId);
            if (mon == null) return;

            var stats = StatCalculator.Compute(acc, ch);
            ch.killCarry += KillsPerSecond(stats, mon) * seconds;
            long kills = (long)Math.Floor(ch.killCarry);
            ch.killCarry -= kills;
            ApplyKillRewards(acc, ch, zone, mon, kills, stats, r);
        }

        static void SimulateGather(AccountState acc, CharacterState ch, double seconds, AfkResult r)
        {
            var node = ContentDatabase.Node(ch.taskTargetId);
            if (node == null) return;
            if ((node.skill == SkillType.Mining) != (ch.task == TaskType.Mining)) return;
            if (ch.GetSkillLevel(node.skill) < node.reqSkillLevel) return;

            var stats = StatCalculator.Compute(acc, ch);
            ch.resourceCarry += GatherPerSecond(stats, node) * seconds;
            long gathered = (long)Math.Floor(ch.resourceCarry);
            ch.resourceCarry -= gathered;
            ApplyGatherRewards(acc, ch, node, gathered, stats, r);
        }

        static void ApplyClassXp(CharacterState ch, double xp, AfkResult r)
        {
            if (xp <= 0) return;
            r.classXp += xp;
            ch.xp += xp;
            while (ch.xp >= XpCurve.ToNext(ch.level))
            {
                ch.xp -= XpCurve.ToNext(ch.level);
                ch.level++;
                r.classLevelsGained++;
            }
        }

        static void ApplySkillXp(CharacterState ch, SkillType skill, double xp, AfkResult r)
        {
            if (xp <= 0) return;
            r.skillXp += xp;
            if (skill == SkillType.Mining)
            {
                ch.miningXp += xp;
                while (ch.miningXp >= XpCurve.SkillToNext(ch.miningLevel))
                {
                    ch.miningXp -= XpCurve.SkillToNext(ch.miningLevel);
                    ch.miningLevel++;
                    r.skillLevelsGained++;
                }
            }
            else
            {
                ch.choppinXp += xp;
                while (ch.choppinXp >= XpCurve.SkillToNext(ch.choppinLevel))
                {
                    ch.choppinXp -= XpCurve.SkillToNext(ch.choppinLevel);
                    ch.choppinLevel++;
                    r.skillLevelsGained++;
                }
            }
        }
    }
}

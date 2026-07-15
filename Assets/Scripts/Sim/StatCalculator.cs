using Tier9.Content;
using Tier9.Core;

namespace Tier9.Sim
{
    public class ComputedStats
    {
        public double str, agi, wis, luk;
        public double mainStat;
        public double damage;
        public double defense;
        public double maxHp;
        public double miningEff;
        public double choppinEff;
        public double afkRate;   // multiplier applied to all AFK gain rates (0.6 base)
        public double xpMult;
        public double dropMult;
        public double coinMult;
    }

    public static class StatCalculator
    {
        public const double BaseAfkRate = 0.60;
        public const double MaxAfkRate = 1.20;

        public static ComputedStats Compute(AccountState acc, CharacterState ch)
        {
            var cls = ContentDatabase.Class(ch.classId) ?? ContentDatabase.Class("beginner");
            var s = new ComputedStats();

            int lv = ch.level - 1;
            s.str = cls.baseStr + cls.growStr * lv;
            s.agi = cls.baseAgi + cls.growAgi * lv;
            s.wis = cls.baseWis + cls.growWis * lv;
            s.luk = cls.baseLuk + cls.growLuk * lv;
            switch (cls.mainStat)
            {
                case StatType.STR: s.mainStat = s.str; break;
                case StatType.AGI: s.mainStat = s.agi; break;
                case StatType.WIS: s.mainStat = s.wis; break;
                default: s.mainStat = s.luk; break;
            }

            // Aggregate percentage/flat effects from talents and account stamps.
            double dmgPct = 0, flatDmg = 0, miningPct = 0, choppinPct = 0, skillPct = 0;
            double afkPct = 0, xpPct = 0, dropPct = 0, coinPct = 0;

            foreach (var tr in ch.talents)
            {
                var def = ContentDatabase.Talent(tr.talentId);
                if (def == null || tr.rank <= 0) continue;
                double v = def.valuePerRank * tr.rank;
                switch (def.effect)
                {
                    case EffectType.DamagePct: dmgPct += v; break;
                    case EffectType.FlatDamage: flatDmg += v; break;
                    case EffectType.MiningEffPct: miningPct += v; break;
                    case EffectType.ChoppinEffPct: choppinPct += v; break;
                    case EffectType.SkillEffPct: skillPct += v; break;
                    case EffectType.AfkRatePct: afkPct += v; break;
                    case EffectType.XpPct: xpPct += v; break;
                    case EffectType.DropPct: dropPct += v; break;
                    case EffectType.CoinPct: coinPct += v; break;
                }
            }

            foreach (var sl in acc.stamps)
            {
                var def = ContentDatabase.Stamp(sl.stampId);
                if (def == null || sl.level <= 0) continue;
                double v = def.valuePerLevel * sl.level;
                switch (def.effect)
                {
                    case EffectType.DamagePct: dmgPct += v; break;
                    case EffectType.FlatDamage: flatDmg += v; break;
                    case EffectType.MiningEffPct: miningPct += v; break;
                    case EffectType.ChoppinEffPct: choppinPct += v; break;
                    case EffectType.SkillEffPct: skillPct += v; break;
                    case EffectType.AfkRatePct: afkPct += v; break;
                    case EffectType.XpPct: xpPct += v; break;
                    case EffectType.DropPct: dropPct += v; break;
                    case EffectType.CoinPct: coinPct += v; break;
                }
            }

            var weapon = ContentDatabase.Item(ch.weaponId);
            var armor = ContentDatabase.Item(ch.armorId);
            var pick = ContentDatabase.Item(ch.pickId);
            var axe = ContentDatabase.Item(ch.axeId);

            double weaponDmg = weapon?.damage ?? 0;
            double pickPower = pick?.toolPower ?? 0;
            double axePower = axe?.toolPower ?? 0;

            s.damage = (4 + weaponDmg + flatDmg) * (1 + s.mainStat * 0.02) * (1 + dmgPct / 100.0);
            s.defense = armor?.defense ?? 0;
            s.maxHp = 10 + 3 * ch.level + (armor?.hp ?? 0);

            s.miningEff = (8 + pickPower * 4 + (ch.miningLevel - 1) * 2 + s.str * 0.3)
                          * (1 + (miningPct + skillPct) / 100.0);
            s.choppinEff = (8 + axePower * 4 + (ch.choppinLevel - 1) * 2 + s.wis * 0.3)
                           * (1 + (choppinPct + skillPct) / 100.0);

            s.afkRate = System.Math.Min(MaxAfkRate, BaseAfkRate * (1 + afkPct / 100.0));
            s.xpMult = 1 + xpPct / 100.0;
            s.dropMult = 1 + dropPct / 100.0;
            s.coinMult = 1 + coinPct / 100.0;
            return s;
        }
    }
}

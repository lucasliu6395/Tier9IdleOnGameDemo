using System;
using System.Collections.Generic;

namespace Tier9.Content
{
    public enum ItemType { Material, Weapon, Armor, Pickaxe, Axe }
    public enum SkillType { Mining, Choppin }
    public enum TaskType { Idle, Combat, Mining, Choppin }
    public enum StatType { STR, AGI, WIS, LUK }

    public enum EffectType
    {
        DamagePct,
        FlatDamage,
        MiningEffPct,
        ChoppinEffPct,
        SkillEffPct,
        AfkRatePct,
        XpPct,
        DropPct,
        CoinPct
    }

    [Serializable]
    public class ItemStack
    {
        public string itemId;
        public long count;

        public ItemStack() { }
        public ItemStack(string itemId, long count) { this.itemId = itemId; this.count = count; }
    }

    public class MonsterDef
    {
        public string id;
        public string name;
        public double hp;
        public double xp;
        public double coinAvg;
        public string dropItemId;
        public double dropChance;   // average drops per kill
        public double reqDefense;   // below this, kill rate is penalized
    }

    public class ZoneDef
    {
        public string id;
        public string name;
        public string monsterId;
        public long killsToNext;    // kills required here to unlock the next zone
    }

    public class NodeDef
    {
        public string id;
        public string name;
        public SkillType skill;
        public double difficulty;
        public string itemId;
        public double xpPerItem;
        public int reqSkillLevel;
    }

    public class ItemDef
    {
        public string id;
        public string name;
        public ItemType type;
        public double damage;
        public double defense;
        public double hp;
        public double toolPower;
        public double sellPrice;
        public double buyPrice;     // 0 = not sold in shop
    }

    public class RecipeDef
    {
        public string id;
        public string resultItemId;
        public List<ItemStack> cost = new List<ItemStack>();
    }

    public class StampDef
    {
        public string id;
        public string name;
        public EffectType effect;
        public double valuePerLevel;
        public double baseCostCoins;
        public string costItemId;
        public long baseCostItems;
        public int maxLevel;
    }

    public class TalentDef
    {
        public string id;
        public string name;
        public string desc;
        public EffectType effect;
        public double valuePerRank;
        public int maxRank;
    }

    public class ClassDef
    {
        public string id;
        public string name;
        public StatType mainStat;
        public double baseStr, baseAgi, baseWis, baseLuk;
        public double growStr, growAgi, growWis, growLuk;
        public List<string> talentIds = new List<string>();
    }
}

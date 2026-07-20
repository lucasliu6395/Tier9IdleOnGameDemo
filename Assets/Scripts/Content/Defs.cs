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
        public double dropChance;    // average drops per kill
        public double reqDefense;    // below this, kill rate is penalized
        public double contactDamage; // 0 = harmless to touch (basic mobs)
        public bool isBoss;          // bigger sprite, chases the player, slower respawn
        public float moveSpeed = 1.2f;
        public float spriteScale = 1f;
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

    // ---- Walkable platformer maps ----

    public class PlatformSpec
    {
        public float x, y, w;   // left edge, top height, width (1 unit tall)

        public PlatformSpec() { }
        public PlatformSpec(float x, float y, float w) { this.x = x; this.y = y; this.w = w; }
    }

    public class StationSpec
    {
        public string kind;     // "anvil" | "shop" | "stamps"
        public float x;

        public StationSpec() { }
        public StationSpec(string kind, float x) { this.kind = kind; this.x = x; }
    }

    public class PortalSpec
    {
        public string targetMapId;
        public float x;
        public string label;

        public PortalSpec() { }
        public PortalSpec(string targetMapId, float x, string label)
        {
            this.targetMapId = targetMapId; this.x = x; this.label = label;
        }
    }

    /// <summary>
    /// A walkable side-scrolling map. Combat maps set monsterId/enemyCount (id matches the
    /// ZoneDef id); skill maps set nodeId/nodePositions (id matches the NodeDef id);
    /// the town map has stations. Portals connect maps.
    /// </summary>
    public class MapDef
    {
        public string id;
        public string name;
        public float length = 30;
        public string bgHex = "#1a2030";
        public string monsterId;
        public int enemyCount;
        public string nodeId;
        public List<float> nodePositions = new List<float>();
        public List<PlatformSpec> platforms = new List<PlatformSpec>();
        public List<StationSpec> stations = new List<StationSpec>();
        public List<PortalSpec> portals = new List<PortalSpec>();

        public bool IsCombat => !string.IsNullOrEmpty(monsterId);
        public bool IsSkill => !string.IsNullOrEmpty(nodeId);
    }
}

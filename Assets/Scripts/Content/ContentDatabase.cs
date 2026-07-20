using System.Collections.Generic;
using System.Linq;

namespace Tier9.Content
{
    /// <summary>
    /// All game content, defined in code. Sprites are looked up by content id from
    /// Resources/Sprites, with generated placeholders when art is missing.
    /// </summary>
    public static class ContentDatabase
    {
        public static readonly List<MonsterDef> Monsters = new List<MonsterDef>
        {
            new MonsterDef { id = "puffshroom", name = "Puffshroom", hp = 15, xp = 2, coinAvg = 1.2, dropItemId = "puff_spore", dropChance = 0.45, reqDefense = 0, contactDamage = 0, moveSpeed = 1.0f },
            new MonsterDef { id = "hopper", name = "Hopper", hp = 45, xp = 5, coinAvg = 3.0, dropItemId = "hopper_leg", dropChance = 0.40, reqDefense = 4, contactDamage = 0, moveSpeed = 1.6f },
            new MonsterDef { id = "pebblit", name = "Pebblit", hp = 130, xp = 13, coinAvg = 7.5, dropItemId = "pebble_shard", dropChance = 0.35, reqDefense = 10, contactDamage = 2, moveSpeed = 0.9f, spriteScale = 1.2f },
            new MonsterDef { id = "glowcap", name = "Glowcap", hp = 380, xp = 32, coinAvg = 18.0, dropItemId = "glow_dust", dropChance = 0.30, reqDefense = 22, contactDamage = 4, moveSpeed = 1.1f, spriteScale = 1.35f },
            new MonsterDef { id = "mother_glowcap", name = "Mother Glowcap", hp = 2500, xp = 150, coinAvg = 90.0, dropItemId = "glow_dust", dropChance = 3.0, reqDefense = 22, contactDamage = 8, isBoss = true, moveSpeed = 0.8f, spriteScale = 2.6f },
        };

        public static readonly List<ZoneDef> Zones = new List<ZoneDef>
        {
            new ZoneDef { id = "meadow_edge", name = "Meadow Edge", monsterId = "puffshroom", killsToNext = 100 },
            new ZoneDef { id = "croak_hollow", name = "Croak Hollow", monsterId = "hopper", killsToNext = 250 },
            new ZoneDef { id = "boulder_pass", name = "Boulder Pass", monsterId = "pebblit", killsToNext = 500 },
            new ZoneDef { id = "fungal_depths", name = "Fungal Depths", monsterId = "glowcap", killsToNext = 500 },
            new ZoneDef { id = "glowcap_lair", name = "Glowcap Lair", monsterId = "mother_glowcap", killsToNext = 0 },
        };

        public static readonly List<MapDef> Maps = new List<MapDef>
        {
            new MapDef
            {
                id = "town", name = "Spore Town", length = 26, bgHex = "#232b3d",
                stations = { new StationSpec("anvil", 8), new StationSpec("shop", 12), new StationSpec("stamps", 16) },
                platforms = { new PlatformSpec(10, 2.5f, 4) },
                portals =
                {
                    new PortalSpec("copper_vein", 2, "Mine"),
                    new PortalSpec("oak_tree", 5, "Forest"),
                    new PortalSpec("meadow_edge", 24, "Meadow Edge"),
                }
            },
            new MapDef
            {
                id = "meadow_edge", name = "Meadow Edge", length = 34, bgHex = "#1d2b22",
                monsterId = "puffshroom", enemyCount = 4,
                platforms = { new PlatformSpec(9, 2.2f, 4), new PlatformSpec(18, 3.2f, 3), new PlatformSpec(26, 2.2f, 4) },
                portals = { new PortalSpec("town", 1, "Town"), new PortalSpec("croak_hollow", 33, "Croak Hollow") }
            },
            new MapDef
            {
                id = "croak_hollow", name = "Croak Hollow", length = 36, bgHex = "#1c2d2a",
                monsterId = "hopper", enemyCount = 4,
                platforms = { new PlatformSpec(8, 2.4f, 3), new PlatformSpec(16, 3.4f, 4), new PlatformSpec(27, 2.4f, 3) },
                portals = { new PortalSpec("meadow_edge", 1, "Meadow Edge"), new PortalSpec("boulder_pass", 35, "Boulder Pass") }
            },
            new MapDef
            {
                id = "boulder_pass", name = "Boulder Pass", length = 38, bgHex = "#2b2620",
                monsterId = "pebblit", enemyCount = 5,
                platforms = { new PlatformSpec(7, 2.2f, 3), new PlatformSpec(14, 3.6f, 3), new PlatformSpec(22, 2.6f, 4), new PlatformSpec(31, 3.2f, 3) },
                portals = { new PortalSpec("croak_hollow", 1, "Croak Hollow"), new PortalSpec("fungal_depths", 37, "Fungal Depths") }
            },
            new MapDef
            {
                id = "fungal_depths", name = "Fungal Depths", length = 38, bgHex = "#241d33",
                monsterId = "glowcap", enemyCount = 5,
                platforms = { new PlatformSpec(9, 2.4f, 4), new PlatformSpec(19, 3.4f, 3), new PlatformSpec(28, 2.4f, 4) },
                portals = { new PortalSpec("boulder_pass", 1, "Boulder Pass"), new PortalSpec("glowcap_lair", 37, "Glowcap Lair") }
            },
            new MapDef
            {
                id = "glowcap_lair", name = "Glowcap Lair", length = 24, bgHex = "#170f22",
                monsterId = "mother_glowcap", enemyCount = 1,
                platforms = { new PlatformSpec(4, 2.6f, 3), new PlatformSpec(17, 2.6f, 3) },
                portals = { new PortalSpec("fungal_depths", 1, "Fungal Depths") }
            },
            new MapDef
            {
                id = "copper_vein", name = "Copper Mine", length = 22, bgHex = "#2a2320",
                nodeId = "copper_vein", nodePositions = { 8, 12, 16 },
                platforms = { new PlatformSpec(11, 2.4f, 3) },
                portals = { new PortalSpec("town", 1, "Town"), new PortalSpec("iron_vein", 21, "Deep Mine") }
            },
            new MapDef
            {
                id = "iron_vein", name = "Deep Mine", length = 20, bgHex = "#221d24",
                nodeId = "iron_vein", nodePositions = { 8, 13 },
                portals = { new PortalSpec("copper_vein", 1, "Copper Mine") }
            },
            new MapDef
            {
                id = "oak_tree", name = "Oak Grove", length = 22, bgHex = "#1c2a1c",
                nodeId = "oak_tree", nodePositions = { 8, 12, 16 },
                platforms = { new PlatformSpec(11, 2.4f, 3) },
                portals = { new PortalSpec("town", 1, "Town"), new PortalSpec("birch_tree", 21, "Birch Thicket") }
            },
            new MapDef
            {
                id = "birch_tree", name = "Birch Thicket", length = 20, bgHex = "#22301f",
                nodeId = "birch_tree", nodePositions = { 8, 13 },
                portals = { new PortalSpec("oak_tree", 1, "Oak Grove") }
            },
        };

        public static readonly List<NodeDef> Nodes = new List<NodeDef>
        {
            new NodeDef { id = "copper_vein", name = "Copper Vein", skill = SkillType.Mining, difficulty = 25, itemId = "copper_ore", xpPerItem = 3, reqSkillLevel = 1 },
            new NodeDef { id = "iron_vein", name = "Iron Vein", skill = SkillType.Mining, difficulty = 95, itemId = "iron_ore", xpPerItem = 8, reqSkillLevel = 8 },
            new NodeDef { id = "oak_tree", name = "Oak Tree", skill = SkillType.Choppin, difficulty = 25, itemId = "oak_log", xpPerItem = 3, reqSkillLevel = 1 },
            new NodeDef { id = "birch_tree", name = "Birch Tree", skill = SkillType.Choppin, difficulty = 95, itemId = "birch_log", xpPerItem = 8, reqSkillLevel = 8 },
        };

        public static readonly List<ItemDef> Items = new List<ItemDef>
        {
            // Materials
            new ItemDef { id = "puff_spore", name = "Puff Spore", type = ItemType.Material, sellPrice = 1 },
            new ItemDef { id = "hopper_leg", name = "Hopper Leg", type = ItemType.Material, sellPrice = 2 },
            new ItemDef { id = "pebble_shard", name = "Pebble Shard", type = ItemType.Material, sellPrice = 4 },
            new ItemDef { id = "glow_dust", name = "Glow Dust", type = ItemType.Material, sellPrice = 9 },
            new ItemDef { id = "copper_ore", name = "Copper Ore", type = ItemType.Material, sellPrice = 2 },
            new ItemDef { id = "iron_ore", name = "Iron Ore", type = ItemType.Material, sellPrice = 5 },
            new ItemDef { id = "oak_log", name = "Oak Log", type = ItemType.Material, sellPrice = 1 },
            new ItemDef { id = "birch_log", name = "Birch Log", type = ItemType.Material, sellPrice = 3 },

            // Weapons
            new ItemDef { id = "stick", name = "Sturdy Stick", type = ItemType.Weapon, damage = 3, sellPrice = 10, buyPrice = 50 },
            new ItemDef { id = "wooden_sword", name = "Wooden Sword", type = ItemType.Weapon, damage = 8, sellPrice = 25 },
            new ItemDef { id = "copper_sword", name = "Copper Sword", type = ItemType.Weapon, damage = 18, sellPrice = 60 },
            new ItemDef { id = "iron_blade", name = "Iron Blade", type = ItemType.Weapon, damage = 40, sellPrice = 150 },

            // Armor
            new ItemDef { id = "spore_vest", name = "Spore Vest", type = ItemType.Armor, defense = 5, hp = 20, sellPrice = 20 },
            new ItemDef { id = "hopper_tunic", name = "Hopper Tunic", type = ItemType.Armor, defense = 12, hp = 45, sellPrice = 50 },
            new ItemDef { id = "pebble_plate", name = "Pebble Plate", type = ItemType.Armor, defense = 25, hp = 90, sellPrice = 120 },

            // Tools
            new ItemDef { id = "flint_pick", name = "Flint Pickaxe", type = ItemType.Pickaxe, toolPower = 3, sellPrice = 15, buyPrice = 100 },
            new ItemDef { id = "copper_pick", name = "Copper Pickaxe", type = ItemType.Pickaxe, toolPower = 8, sellPrice = 40 },
            new ItemDef { id = "iron_pick", name = "Iron Pickaxe", type = ItemType.Pickaxe, toolPower = 20, sellPrice = 100 },
            new ItemDef { id = "flint_axe", name = "Flint Axe", type = ItemType.Axe, toolPower = 3, sellPrice = 15, buyPrice = 100 },
            new ItemDef { id = "copper_axe", name = "Copper Axe", type = ItemType.Axe, toolPower = 8, sellPrice = 40 },
            new ItemDef { id = "iron_axe", name = "Iron Axe", type = ItemType.Axe, toolPower = 20, sellPrice = 100 },
        };

        public static readonly List<RecipeDef> Recipes = new List<RecipeDef>
        {
            new RecipeDef { id = "r_wooden_sword", resultItemId = "wooden_sword", cost = { new ItemStack("oak_log", 15) } },
            new RecipeDef { id = "r_copper_sword", resultItemId = "copper_sword", cost = { new ItemStack("copper_ore", 20), new ItemStack("oak_log", 5) } },
            new RecipeDef { id = "r_iron_blade", resultItemId = "iron_blade", cost = { new ItemStack("iron_ore", 25), new ItemStack("birch_log", 10) } },
            new RecipeDef { id = "r_spore_vest", resultItemId = "spore_vest", cost = { new ItemStack("puff_spore", 20) } },
            new RecipeDef { id = "r_hopper_tunic", resultItemId = "hopper_tunic", cost = { new ItemStack("hopper_leg", 25), new ItemStack("puff_spore", 10) } },
            new RecipeDef { id = "r_pebble_plate", resultItemId = "pebble_plate", cost = { new ItemStack("pebble_shard", 30) } },
            new RecipeDef { id = "r_copper_pick", resultItemId = "copper_pick", cost = { new ItemStack("copper_ore", 15) } },
            new RecipeDef { id = "r_iron_pick", resultItemId = "iron_pick", cost = { new ItemStack("iron_ore", 20) } },
            new RecipeDef { id = "r_copper_axe", resultItemId = "copper_axe", cost = { new ItemStack("copper_ore", 15) } },
            new RecipeDef { id = "r_iron_axe", resultItemId = "iron_axe", cost = { new ItemStack("iron_ore", 20) } },
        };

        public static readonly List<StampDef> Stamps = new List<StampDef>
        {
            new StampDef { id = "sword_stamp", name = "Sword Stamp", effect = EffectType.DamagePct, valuePerLevel = 2, baseCostCoins = 100, costItemId = "puff_spore", baseCostItems = 5, maxLevel = 20 },
            new StampDef { id = "pick_stamp", name = "Pickaxe Stamp", effect = EffectType.MiningEffPct, valuePerLevel = 2, baseCostCoins = 150, costItemId = "copper_ore", baseCostItems = 5, maxLevel = 20 },
            new StampDef { id = "axe_stamp", name = "Axe Stamp", effect = EffectType.ChoppinEffPct, valuePerLevel = 2, baseCostCoins = 150, costItemId = "oak_log", baseCostItems = 5, maxLevel = 20 },
            new StampDef { id = "clock_stamp", name = "Clock Stamp", effect = EffectType.AfkRatePct, valuePerLevel = 1.5, baseCostCoins = 300, costItemId = "hopper_leg", baseCostItems = 3, maxLevel = 20 },
            new StampDef { id = "book_stamp", name = "Book Stamp", effect = EffectType.XpPct, valuePerLevel = 2, baseCostCoins = 250, costItemId = "hopper_leg", baseCostItems = 4, maxLevel = 20 },
            new StampDef { id = "clover_stamp", name = "Clover Stamp", effect = EffectType.DropPct, valuePerLevel = 2, baseCostCoins = 400, costItemId = "pebble_shard", baseCostItems = 3, maxLevel = 20 },
        };

        public static readonly List<TalentDef> Talents = new List<TalentDef>
        {
            // Beginner
            new TalentDef { id = "sharp_stick", name = "Sharp Stick", desc = "+1 base damage per rank", effect = EffectType.FlatDamage, valuePerRank = 1, maxRank = 20 },
            new TalentDef { id = "idle_soul", name = "Idle Soul", desc = "+1% AFK gain rate per rank", effect = EffectType.AfkRatePct, valuePerRank = 1, maxRank = 10 },
            // Warrior
            new TalentDef { id = "power_strikes", name = "Power Strikes", desc = "+2% damage per rank", effect = EffectType.DamagePct, valuePerRank = 2, maxRank = 20 },
            new TalentDef { id = "iron_grip", name = "Iron Grip", desc = "+2% mining efficiency per rank", effect = EffectType.MiningEffPct, valuePerRank = 2, maxRank = 20 },
            new TalentDef { id = "bulk_up", name = "Bulk Up", desc = "+2 base damage per rank", effect = EffectType.FlatDamage, valuePerRank = 2, maxRank = 20 },
            // Archer
            new TalentDef { id = "true_shot", name = "True Shot", desc = "+2% damage per rank", effect = EffectType.DamagePct, valuePerRank = 2, maxRank = 20 },
            new TalentDef { id = "keen_eye", name = "Keen Eye", desc = "+2% drop rate per rank", effect = EffectType.DropPct, valuePerRank = 2, maxRank = 20 },
            new TalentDef { id = "coin_shot", name = "Coin Shot", desc = "+2% coins per rank", effect = EffectType.CoinPct, valuePerRank = 2, maxRank = 20 },
            // Mage
            new TalentDef { id = "arcane_bolt", name = "Arcane Bolt", desc = "+2% damage per rank", effect = EffectType.DamagePct, valuePerRank = 2, maxRank = 20 },
            new TalentDef { id = "attunement", name = "Attunement", desc = "+1.5% all skill efficiency per rank", effect = EffectType.SkillEffPct, valuePerRank = 1.5, maxRank = 20 },
            new TalentDef { id = "wise_one", name = "Wise One", desc = "+2% XP gain per rank", effect = EffectType.XpPct, valuePerRank = 2, maxRank = 20 },
        };

        public static readonly List<ClassDef> Classes = new List<ClassDef>
        {
            new ClassDef
            {
                id = "beginner", name = "Beginner", mainStat = StatType.LUK,
                baseStr = 4, baseAgi = 4, baseWis = 4, baseLuk = 4,
                growStr = 0.5, growAgi = 0.5, growWis = 0.5, growLuk = 0.5,
                talentIds = { "sharp_stick", "idle_soul" }
            },
            new ClassDef
            {
                id = "warrior", name = "Warrior", mainStat = StatType.STR,
                baseStr = 8, baseAgi = 4, baseWis = 3, baseLuk = 4,
                growStr = 2.0, growAgi = 0.5, growWis = 0.3, growLuk = 0.4,
                talentIds = { "sharp_stick", "idle_soul", "power_strikes", "iron_grip", "bulk_up" }
            },
            new ClassDef
            {
                id = "archer", name = "Archer", mainStat = StatType.AGI,
                baseStr = 4, baseAgi = 8, baseWis = 3, baseLuk = 5,
                growStr = 0.5, growAgi = 2.0, growWis = 0.3, growLuk = 0.5,
                talentIds = { "sharp_stick", "idle_soul", "true_shot", "keen_eye", "coin_shot" }
            },
            new ClassDef
            {
                id = "mage", name = "Mage", mainStat = StatType.WIS,
                baseStr = 3, baseAgi = 4, baseWis = 8, baseLuk = 4,
                growStr = 0.3, growAgi = 0.5, growWis = 2.0, growLuk = 0.5,
                talentIds = { "sharp_stick", "idle_soul", "arcane_bolt", "attunement", "wise_one" }
            },
        };

        static Dictionary<string, MonsterDef> _monsters;
        static Dictionary<string, ZoneDef> _zones;
        static Dictionary<string, MapDef> _maps;
        static Dictionary<string, NodeDef> _nodes;
        static Dictionary<string, ItemDef> _items;
        static Dictionary<string, RecipeDef> _recipes;
        static Dictionary<string, StampDef> _stamps;
        static Dictionary<string, TalentDef> _talents;
        static Dictionary<string, ClassDef> _classes;

        static ContentDatabase()
        {
            _monsters = Monsters.ToDictionary(m => m.id);
            _zones = Zones.ToDictionary(z => z.id);
            _maps = Maps.ToDictionary(m => m.id);
            _nodes = Nodes.ToDictionary(n => n.id);
            _items = Items.ToDictionary(i => i.id);
            _recipes = Recipes.ToDictionary(r => r.id);
            _stamps = Stamps.ToDictionary(s => s.id);
            _talents = Talents.ToDictionary(t => t.id);
            _classes = Classes.ToDictionary(c => c.id);
        }

        public static MonsterDef Monster(string id) => id != null && _monsters.TryGetValue(id, out var v) ? v : null;
        public static ZoneDef Zone(string id) => id != null && _zones.TryGetValue(id, out var v) ? v : null;
        public static MapDef Map(string id) => id != null && _maps.TryGetValue(id, out var v) ? v : null;
        public static NodeDef Node(string id) => id != null && _nodes.TryGetValue(id, out var v) ? v : null;
        public static ItemDef Item(string id) => id != null && _items.TryGetValue(id, out var v) ? v : null;
        public static RecipeDef Recipe(string id) => id != null && _recipes.TryGetValue(id, out var v) ? v : null;
        public static StampDef Stamp(string id) => id != null && _stamps.TryGetValue(id, out var v) ? v : null;
        public static TalentDef Talent(string id) => id != null && _talents.TryGetValue(id, out var v) ? v : null;
        public static ClassDef Class(string id) => id != null && _classes.TryGetValue(id, out var v) ? v : null;

        public static int ZoneIndex(string id) => Zones.FindIndex(z => z.id == id);
    }
}

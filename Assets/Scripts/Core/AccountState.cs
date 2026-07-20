using System;
using System.Collections.Generic;
using Tier9.Content;

namespace Tier9.Core
{
    [Serializable]
    public class StampLevel
    {
        public string stampId;
        public int level;
    }

    [Serializable]
    public class TalentRank
    {
        public string talentId;
        public int rank;
    }

    [Serializable]
    public class KillCount
    {
        public string zoneId;
        public long kills;
    }

    [Serializable]
    public class CharacterState
    {
        public string name = "Hero";
        public string classId = "beginner";
        public string spriteId = "";   // appearance chosen at creation ("player_*")
        public int level = 1;
        public double xp;

        public int miningLevel = 1;
        public double miningXp;
        public int choppinLevel = 1;
        public double choppinXp;

        public List<TalentRank> talents = new List<TalentRank>();
        public List<KillCount> killCounts = new List<KillCount>();

        public string weaponId = "";
        public string armorId = "";
        public string pickId = "";
        public string axeId = "";

        public TaskType task = TaskType.Idle;
        public string taskTargetId = "";

        // Fractional progress carried between simulation steps so short ticks
        // and long offline chunks produce the same totals.
        public double killCarry;
        public double dropCarry;
        public double resourceCarry;

        public long GetKills(string zoneId)
        {
            var k = killCounts.Find(x => x.zoneId == zoneId);
            return k?.kills ?? 0;
        }

        public void AddKills(string zoneId, long amount)
        {
            if (amount <= 0) return;
            var k = killCounts.Find(x => x.zoneId == zoneId);
            if (k == null) killCounts.Add(new KillCount { zoneId = zoneId, kills = amount });
            else k.kills += amount;
        }

        public int GetTalentRank(string talentId)
        {
            var t = talents.Find(x => x.talentId == talentId);
            return t?.rank ?? 0;
        }

        public void AddTalentRank(string talentId)
        {
            var t = talents.Find(x => x.talentId == talentId);
            if (t == null) talents.Add(new TalentRank { talentId = talentId, rank = 1 });
            else t.rank++;
        }

        public int SpentTalentPoints()
        {
            int total = 0;
            foreach (var t in talents) total += t.rank;
            return total;
        }

        public int AvailableTalentPoints() => Math.Max(0, level - 1 - SpentTalentPoints());

        public int GetSkillLevel(SkillType skill) => skill == SkillType.Mining ? miningLevel : choppinLevel;

        public void SetTask(TaskType newTask, string targetId)
        {
            task = newTask;
            taskTargetId = targetId ?? "";
            killCarry = dropCarry = resourceCarry = 0;
        }

        public string GetEquipped(ItemType slot)
        {
            switch (slot)
            {
                case ItemType.Weapon: return weaponId;
                case ItemType.Armor: return armorId;
                case ItemType.Pickaxe: return pickId;
                case ItemType.Axe: return axeId;
                default: return "";
            }
        }

        public void SetEquipped(ItemType slot, string itemId)
        {
            switch (slot)
            {
                case ItemType.Weapon: weaponId = itemId ?? ""; break;
                case ItemType.Armor: armorId = itemId ?? ""; break;
                case ItemType.Pickaxe: pickId = itemId ?? ""; break;
                case ItemType.Axe: axeId = itemId ?? ""; break;
            }
        }
    }

    [Serializable]
    public class AccountState
    {
        public const int MaxCharacters = 3;

        public int version = 1;
        public double coins;
        public List<ItemStack> storage = new List<ItemStack>();
        public List<StampLevel> stamps = new List<StampLevel>();
        public List<CharacterState> characters = new List<CharacterState>();
        public int selectedCharacter;
        public long lastSeenUtcTicks;

        public long GetItemCount(string itemId)
        {
            var s = storage.Find(x => x.itemId == itemId);
            return s?.count ?? 0;
        }

        public void AddItem(string itemId, long amount)
        {
            if (amount == 0 || string.IsNullOrEmpty(itemId)) return;
            var s = storage.Find(x => x.itemId == itemId);
            if (s == null)
            {
                if (amount > 0) storage.Add(new ItemStack(itemId, amount));
            }
            else
            {
                s.count += amount;
                if (s.count <= 0) storage.Remove(s);
            }
        }

        public bool HasItems(List<ItemStack> cost)
        {
            foreach (var c in cost)
                if (GetItemCount(c.itemId) < c.count) return false;
            return true;
        }

        public bool TakeItems(List<ItemStack> cost)
        {
            if (!HasItems(cost)) return false;
            foreach (var c in cost) AddItem(c.itemId, -c.count);
            return true;
        }

        public int GetStampLevel(string stampId)
        {
            var s = stamps.Find(x => x.stampId == stampId);
            return s?.level ?? 0;
        }

        public void IncStampLevel(string stampId)
        {
            var s = stamps.Find(x => x.stampId == stampId);
            if (s == null) stamps.Add(new StampLevel { stampId = stampId, level = 1 });
            else s.level++;
        }

        public CharacterState Selected =>
            characters.Count == 0 ? null : characters[Math.Clamp(selectedCharacter, 0, characters.Count - 1)];

        public static AccountState CreateNew()
        {
            return new AccountState
            {
                coins = 25,
                lastSeenUtcTicks = DateTime.UtcNow.Ticks
            };
        }

        public static CharacterState CreateCharacter(string name, string spriteId = "")
        {
            return new CharacterState
            {
                name = string.IsNullOrWhiteSpace(name) ? "Hero" : name.Trim(),
                spriteId = spriteId ?? ""
            };
        }
    }
}

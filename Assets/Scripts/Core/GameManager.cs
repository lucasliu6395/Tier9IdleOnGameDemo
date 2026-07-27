using System;
using System.Collections.Generic;
using Tier9.Content;
using Tier9.Sim;
using UnityEngine;

namespace Tier9.Core
{
    public class AfkReportEntry
    {
        public string characterName;
        public AfkResult result;
    }

    public class AfkReport
    {
        public double seconds;
        public List<AfkReportEntry> entries = new List<AfkReportEntry>();

        public bool HasGains
        {
            get
            {
                foreach (var e in entries)
                    if (!e.result.IsEmpty) return true;
                return false;
            }
        }
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }

        public AccountState Account { get; private set; }

        /// <summary>The character currently controlled in the world; skipped by the AFK tick
        /// because they earn through live gameplay instead.</summary>
        public CharacterState LiveCharacter { get; set; }

        public event Action StateChanged;
        public event Action<AfkReport> AfkCompleted;
        public event Action<string> Toast;
        /// <summary>Fired when a character's map/task changes and the world should rebuild.</summary>
        public event Action TravelChanged;

        const float SaveIntervalSeconds = 20f;
        float _tickAccum;
        float _saveAccum;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (I != null) return;
            var go = new GameObject("Tier9Game");
            DontDestroyOnLoad(go);
            go.AddComponent<GameManager>();
            go.AddComponent<Tier9.UI.UiRoot>();
            go.AddComponent<Tier9.World.WorldRunner>();
        }

        void Awake()
        {
            I = this;
            Account = SaveSystem.Load() ?? AccountState.CreateNew();
            if (Account.EnsureCharacterSprites()) SaveSystem.Save(Account);
        }

        void Start()
        {
            if (Account.lastSeenUtcTicks > 0)
            {
                double elapsed = (DateTime.UtcNow - new DateTime(Account.lastSeenUtcTicks, DateTimeKind.Utc)).TotalSeconds;
                if (elapsed >= 60) RunAfk(elapsed, notify: true);
            }
            NotifyChanged();
        }

        void Update()
        {
            _tickAccum += UnityEngine.Time.deltaTime;
            bool ticked = false;
            while (_tickAccum >= 1f)
            {
                _tickAccum -= 1f;
                foreach (var ch in Account.characters)
                {
                    if (ch == LiveCharacter) continue;
                    AfkSimulator.Simulate(Account, ch, 1.0);
                }
                ticked = true;
            }
            if (ticked) NotifyChanged();

            _saveAccum += UnityEngine.Time.deltaTime;
            if (_saveAccum >= SaveIntervalSeconds)
            {
                _saveAccum = 0f;
                SaveSystem.Save(Account);
            }
        }

        void OnApplicationQuit() => SaveSystem.Save(Account);

        void OnApplicationPause(bool paused)
        {
            if (paused) SaveSystem.Save(Account);
        }

        public void NotifyChanged() => StateChanged?.Invoke();

        void ShowToast(string message) => Toast?.Invoke(message);

        /// <summary>Simulate `seconds` of AFK time for every character (offline catch-up and debug time-skip).</summary>
        public void RunAfk(double seconds, bool notify)
        {
            seconds = Math.Min(seconds, AfkSimulator.MaxOfflineSeconds);
            var report = new AfkReport { seconds = seconds };
            foreach (var ch in Account.characters)
            {
                report.entries.Add(new AfkReportEntry
                {
                    characterName = ch.name,
                    result = AfkSimulator.SimulateChunked(Account, ch, seconds)
                });
            }
            SaveSystem.Save(Account);
            NotifyChanged();
            if (notify && report.HasGains) AfkCompleted?.Invoke(report);
        }

        // ---- Player actions (called from UI) ----

        public void SelectCharacter(int index)
        {
            if (index < 0 || index >= Account.characters.Count) return;
            if (Account.selectedCharacter == index) return;
            Account.selectedCharacter = index;
            TravelChanged?.Invoke();
            NotifyChanged();
        }

        public bool CreateCharacter(string name, string spriteId = "")
        {
            if (Account.characters.Count >= AccountState.MaxCharacters) return false;
            Account.characters.Add(AccountState.CreateCharacter(name, spriteId));
            Account.selectedCharacter = Account.characters.Count - 1;
            SaveSystem.Save(Account);
            TravelChanged?.Invoke();
            NotifyChanged();
            return true;
        }

        public void AssignTask(CharacterState ch, TaskType task, string targetId)
        {
            ch.SetTask(task, targetId);
            SaveSystem.Save(Account);
            NotifyChanged();
        }

        /// <summary>The walkable map a character is on, derived from their task ("where you stand is what you do").</summary>
        public static string MapIdForCharacter(CharacterState ch)
        {
            switch (ch.task)
            {
                case TaskType.Combat: return ContentDatabase.Map(ch.taskTargetId) != null ? ch.taskTargetId : "town";
                case TaskType.Mining:
                case TaskType.Choppin: return ContentDatabase.Map(ch.taskTargetId) != null ? ch.taskTargetId : "town";
                default: return "town";
            }
        }

        /// <summary>Move a character to a map; their AFK task follows their location.</summary>
        public void TravelTo(CharacterState ch, string mapId)
        {
            var map = ContentDatabase.Map(mapId);
            if (map == null) return;
            if (map.IsCombat)
            {
                ch.SetTask(TaskType.Combat, mapId);
            }
            else if (map.IsSkill)
            {
                var node = ContentDatabase.Node(map.nodeId);
                ch.SetTask(node.skill == SkillType.Mining ? TaskType.Mining : TaskType.Choppin, map.nodeId);
            }
            else
            {
                ch.SetTask(TaskType.Idle, "");
            }
            SaveSystem.Save(Account);
            TravelChanged?.Invoke();
            NotifyChanged();
        }

        public bool IsZoneUnlocked(CharacterState ch, string zoneId)
        {
            int idx = ContentDatabase.ZoneIndex(zoneId);
            if (idx <= 0) return idx == 0;
            var prev = ContentDatabase.Zones[idx - 1];
            return ch.GetKills(prev.id) >= prev.killsToNext;
        }

        public bool Craft(RecipeDef recipe)
        {
            if (recipe == null || !Account.TakeItems(recipe.cost)) return false;
            Account.AddItem(recipe.resultItemId, 1);
            var item = ContentDatabase.Item(recipe.resultItemId);
            ShowToast($"Crafted {item?.name ?? recipe.resultItemId}!");
            SaveSystem.Save(Account);
            NotifyChanged();
            return true;
        }

        public bool BuyItem(ItemDef item)
        {
            if (item == null || item.buyPrice <= 0 || Account.coins < item.buyPrice) return false;
            Account.coins -= item.buyPrice;
            Account.AddItem(item.id, 1);
            ShowToast($"Bought {item.name}");
            SaveSystem.Save(Account);
            NotifyChanged();
            return true;
        }

        public bool SellItem(string itemId, long count)
        {
            var item = ContentDatabase.Item(itemId);
            long owned = Account.GetItemCount(itemId);
            if (item == null || owned <= 0) return false;
            count = Math.Min(count, owned);
            Account.AddItem(itemId, -count);
            double gained = item.sellPrice * count;
            Account.coins += gained;
            ShowToast($"Sold {count}x {item.name} for {Fmt.N(gained)} coins");
            SaveSystem.Save(Account);
            NotifyChanged();
            return true;
        }

        public double StampCoinCost(StampDef stamp, int level) => Math.Floor(stamp.baseCostCoins * Math.Pow(1.6, level));

        public long StampItemCost(StampDef stamp, int level) => (long)Math.Ceiling(stamp.baseCostItems * Math.Pow(1.35, level));

        public bool UpgradeStamp(StampDef stamp)
        {
            if (stamp == null) return false;
            int level = Account.GetStampLevel(stamp.id);
            if (level >= stamp.maxLevel) return false;
            double coinCost = StampCoinCost(stamp, level);
            long itemCost = StampItemCost(stamp, level);
            if (Account.coins < coinCost || Account.GetItemCount(stamp.costItemId) < itemCost) return false;
            Account.coins -= coinCost;
            Account.AddItem(stamp.costItemId, -itemCost);
            Account.IncStampLevel(stamp.id);
            ShowToast($"{stamp.name} is now Lv {level + 1} (account-wide)");
            SaveSystem.Save(Account);
            NotifyChanged();
            return true;
        }

        public bool Equip(CharacterState ch, string itemId)
        {
            var item = ContentDatabase.Item(itemId);
            if (item == null || item.type == ItemType.Material) return false;
            if (Account.GetItemCount(itemId) <= 0) return false;

            string previous = ch.GetEquipped(item.type);
            Account.AddItem(itemId, -1);
            if (!string.IsNullOrEmpty(previous)) Account.AddItem(previous, 1);
            ch.SetEquipped(item.type, itemId);
            SaveSystem.Save(Account);
            NotifyChanged();
            return true;
        }

        public bool Unequip(CharacterState ch, ItemType slot)
        {
            string current = ch.GetEquipped(slot);
            if (string.IsNullOrEmpty(current)) return false;
            Account.AddItem(current, 1);
            ch.SetEquipped(slot, "");
            SaveSystem.Save(Account);
            NotifyChanged();
            return true;
        }

        public bool SpendTalentPoint(CharacterState ch, TalentDef talent)
        {
            if (talent == null || ch.AvailableTalentPoints() <= 0) return false;
            if (ch.GetTalentRank(talent.id) >= talent.maxRank) return false;
            var cls = ContentDatabase.Class(ch.classId);
            if (cls == null || !cls.talentIds.Contains(talent.id)) return false;
            ch.AddTalentRank(talent.id);
            SaveSystem.Save(Account);
            NotifyChanged();
            return true;
        }

        public const int PromoteLevel = 5;

        public bool Promote(CharacterState ch, string newClassId)
        {
            if (ch.classId != "beginner" || ch.level < PromoteLevel) return false;
            var cls = ContentDatabase.Class(newClassId);
            if (cls == null || newClassId == "beginner") return false;
            ch.classId = newClassId;
            ShowToast($"{ch.name} became a {cls.name}!");
            SaveSystem.Save(Account);
            NotifyChanged();
            return true;
        }

        public void ResetSave()
        {
            SaveSystem.DeleteSave();
            Account = AccountState.CreateNew();
            NotifyChanged();
        }
    }
}

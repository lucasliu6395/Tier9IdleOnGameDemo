using System.Collections.Generic;
using Tier9.Content;

namespace Tier9.Sim
{
    /// <summary>Summary of what one character gained over a simulated span of time.</summary>
    public class AfkResult
    {
        public double seconds;
        public long kills;
        public double classXp;
        public int classLevelsGained;
        public double skillXp;
        public int skillLevelsGained;
        public double coins;
        public List<ItemStack> items = new List<ItemStack>();

        public bool IsEmpty =>
            kills == 0 && classXp <= 0 && skillXp <= 0 && coins <= 0 && items.Count == 0;

        public void AddItem(string itemId, long count)
        {
            if (count <= 0) return;
            var s = items.Find(x => x.itemId == itemId);
            if (s == null) items.Add(new ItemStack(itemId, count));
            else s.count += count;
        }

        public void Merge(AfkResult other)
        {
            if (other == null) return;
            seconds += other.seconds;
            kills += other.kills;
            classXp += other.classXp;
            classLevelsGained += other.classLevelsGained;
            skillXp += other.skillXp;
            skillLevelsGained += other.skillLevelsGained;
            coins += other.coins;
            foreach (var s in other.items) AddItem(s.itemId, s.count);
        }
    }
}

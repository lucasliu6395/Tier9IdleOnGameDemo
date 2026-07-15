using System.Text;
using Tier9.Content;
using Tier9.Core;
using Tier9.Sim;
using UnityEngine.UIElements;

namespace Tier9.UI
{
    public class WorldScreen : ScreenBase
    {
        protected override string StructureKey()
        {
            var ch = Ch;
            if (ch == null) return "none";
            var sb = new StringBuilder();
            sb.Append(Account.selectedCharacter).Append(':').Append(ch.task).Append(':').Append(ch.taskTargetId);
            foreach (var z in ContentDatabase.Zones)
                sb.Append(Gm.IsZoneUnlocked(ch, z.id) ? '1' : '0');
            return sb.ToString();
        }

        protected override void Build(VisualElement host)
        {
            var ch = Ch;
            if (ch == null) return;

            var scroll = new ScrollView();
            host.Add(scroll);

            // Current activity summary
            var header = Card($"{ch.name} — {UiRoot.TaskSummary(ch)}");
            if (ch.task == TaskType.Combat)
            {
                var zone = ContentDatabase.Zone(ch.taskTargetId);
                var mon = zone == null ? null : ContentDatabase.Monster(zone.monsterId);
                if (mon != null)
                {
                    var battle = Row(Icon("class_" + ch.classId, "icon-l"), Text("⚔", "battle-vs"), Icon(mon.id, "icon-l"));
                    battle.AddToClassList("battle-view");
                    header.Add(battle);

                    var rates = Text("", "muted");
                    var kills = Text("", "muted");
                    header.Add(rates);
                    header.Add(kills);
                    Bind(() =>
                    {
                        var stats = StatCalculator.Compute(Account, ch);
                        double kps = AfkSimulator.KillsPerSecond(stats, mon);
                        rates.text = $"Kills {Fmt.PerHour(kps)}   •   XP {Fmt.PerHour(kps * mon.xp * stats.xpMult)}   •   Coins {Fmt.PerHour(kps * mon.coinAvg * stats.coinMult)}   •   AFK rate {stats.afkRate:P0}";
                        kills.text = $"{mon.name}s defeated here: {Fmt.N(ch.GetKills(zone.id))}";
                    });
                }
            }
            else
            {
                header.Add(Text("Assign this character to a zone below to start earning AFK combat gains.", "muted"));
            }
            scroll.Add(header);

            // Zone cards
            for (int i = 0; i < ContentDatabase.Zones.Count; i++)
            {
                var zone = ContentDatabase.Zones[i];
                var mon = ContentDatabase.Monster(zone.monsterId);
                bool unlocked = Gm.IsZoneUnlocked(ch, zone.id);
                var card = Card();
                card.AddToClassList(unlocked ? "zone-card" : "zone-card--locked");

                var top = Row(
                    Icon(mon.id, "icon-m"),
                    Text($"{zone.name}", "zone-name"),
                    Spacer());
                card.Add(top);
                card.Add(Text($"{mon.name} — HP {Fmt.N(mon.hp)}, {Fmt.N(mon.xp)} XP" +
                              (mon.reqDefense > 0 ? $", needs ~{Fmt.N(mon.reqDefense)} defense" : ""), "muted"));

                if (!unlocked)
                {
                    var prev = ContentDatabase.Zones[i - 1];
                    var lockLabel = Text("", "locked-text");
                    card.Add(lockLabel);
                    Bind(() =>
                    {
                        long have = ch.GetKills(prev.id);
                        lockLabel.text = $"🔒 Defeat {Fmt.N(System.Math.Max(0, prev.killsToNext - have))} more in {prev.name} to unlock";
                    });
                }
                else
                {
                    var preview = Text("", "muted");
                    card.Add(preview);
                    Bind(() =>
                    {
                        var stats = StatCalculator.Compute(Account, ch);
                        double kps = AfkSimulator.KillsPerSecond(stats, mon);
                        string quota = zone.killsToNext > 0
                            ? $"   •   Unlock progress {Fmt.N(ch.GetKills(zone.id))}/{Fmt.N(zone.killsToNext)}"
                            : "";
                        preview.text = $"Your rate: {Fmt.PerHour(kps)} kills{quota}";
                    });

                    bool isHere = ch.task == TaskType.Combat && ch.taskTargetId == zone.id;
                    if (isHere)
                    {
                        var here = Text("⚔ Fighting here", "active-tag");
                        card.Add(here);
                        card.Add(new Button(() => Gm.AssignTask(ch, TaskType.Idle, "")) { text = "Return to Town" });
                    }
                    else
                    {
                        var fight = new Button(() => Gm.AssignTask(ch, TaskType.Combat, zone.id)) { text = "AFK Fight Here" };
                        fight.AddToClassList("btn-primary");
                        card.Add(fight);
                    }
                }
                scroll.Add(card);
            }
        }
    }
}

using System.Text;
using Tier9.Content;
using Tier9.Core;
using Tier9.Sim;
using UnityEngine.UIElements;

namespace Tier9.UI
{
    public class SkillsScreen : ScreenBase
    {
        protected override string StructureKey()
        {
            var ch = Ch;
            if (ch == null) return "none";
            var sb = new StringBuilder();
            sb.Append(Account.selectedCharacter).Append(':').Append(ch.task).Append(':').Append(ch.taskTargetId);
            foreach (var n in ContentDatabase.Nodes)
                sb.Append(ch.GetSkillLevel(n.skill) >= n.reqSkillLevel ? '1' : '0');
            return sb.ToString();
        }

        protected override void Build(VisualElement host)
        {
            var ch = Ch;
            if (ch == null) return;

            var scroll = new ScrollView();
            host.Add(scroll);

            // Skill level header
            var header = Card($"{ch.name}'s Skills");
            header.Add(BuildSkillBar("⛏ Mining", SkillType.Mining));
            header.Add(BuildSkillBar("🪓 Choppin", SkillType.Choppin));
            scroll.Add(header);

            foreach (var skill in new[] { SkillType.Mining, SkillType.Choppin })
            {
                var section = Text(skill == SkillType.Mining ? "Mining Nodes" : "Choppin Trees", "section-title");
                scroll.Add(section);

                foreach (var node in ContentDatabase.Nodes)
                {
                    if (node.skill != skill) continue;
                    var card = Card();
                    bool available = ch.GetSkillLevel(node.skill) >= node.reqSkillLevel;
                    card.AddToClassList(available ? "zone-card" : "zone-card--locked");

                    var item = ContentDatabase.Item(node.itemId);
                    card.Add(Row(Icon(node.itemId, "icon-m"), Text(node.name, "zone-name"), Spacer()));
                    card.Add(Text($"Yields {item?.name ?? node.itemId} • {Fmt.N(node.xpPerItem)} XP each", "muted"));

                    if (!available)
                    {
                        card.Add(Text($"🔒 Requires {node.skill} level {node.reqSkillLevel}", "locked-text"));
                    }
                    else
                    {
                        var preview = Text("", "muted");
                        card.Add(preview);
                        var nodeRef = node;
                        Bind(() =>
                        {
                            var stats = StatCalculator.Compute(Account, ch);
                            double gps = AfkSimulator.GatherPerSecond(stats, nodeRef);
                            preview.text = $"Your rate: {Fmt.PerHour(gps)} {item?.name ?? "items"}   •   XP {Fmt.PerHour(gps * nodeRef.xpPerItem * stats.xpMult)}";
                        });

                        var taskType = skill == SkillType.Mining ? TaskType.Mining : TaskType.Choppin;
                        bool isHere = ch.task == taskType && ch.taskTargetId == node.id;
                        if (isHere)
                        {
                            card.Add(Text(skill == SkillType.Mining ? "⛏ You are here" : "🪓 You are here", "active-tag"));
                            card.Add(new Button(() =>
                            {
                                Gm.TravelTo(ch, "town");
                                UiRoot.I.CloseMenu();
                            })
                            { text = "Return to Town" });
                        }
                        else
                        {
                            var go = new Button(() =>
                            {
                                Gm.TravelTo(ch, node.id);
                                UiRoot.I.CloseMenu();
                            })
                            { text = "Travel Here" };
                            go.AddToClassList("btn-primary");
                            card.Add(go);
                        }
                    }
                    scroll.Add(card);
                }
            }
        }

        VisualElement BuildSkillBar(string label, SkillType skill)
        {
            var ch = Ch;
            var container = new VisualElement();
            var (bar, fill, barLabel) = Bar(skill == SkillType.Mining ? "bar-fill--mining" : "bar-fill--choppin");
            var title = Text("", "skill-title");
            container.Add(title);
            container.Add(bar);
            Bind(() =>
            {
                int level = ch.GetSkillLevel(skill);
                double xp = skill == SkillType.Mining ? ch.miningXp : ch.choppinXp;
                double next = Sim.XpCurve.SkillToNext(level);
                title.text = $"{label}  Lv {level}";
                fill.style.width = UnityEngine.UIElements.Length.Percent((float)(xp / next * 100.0));
                barLabel.text = $"{Fmt.N(xp)} / {Fmt.N(next)} XP";
            });
            return container;
        }
    }
}

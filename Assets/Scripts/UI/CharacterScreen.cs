using System.Collections.Generic;
using System.Text;
using Tier9.Content;
using Tier9.Core;
using Tier9.Sim;
using UnityEngine.UIElements;

namespace Tier9.UI
{
    public class CharacterScreen : ScreenBase
    {
        protected override string StructureKey()
        {
            var ch = Ch;
            if (ch == null) return "none";
            var sb = new StringBuilder();
            sb.Append(Account.selectedCharacter).Append(':').Append(ch.classId).Append(':').Append(ch.spriteId).Append(':');
            sb.Append(ch.classId == "beginner" && ch.level >= GameManager.PromoteLevel ? "promo" : "-").Append(':');
            sb.Append(ch.weaponId).Append(',').Append(ch.armorId).Append(',').Append(ch.pickId).Append(',').Append(ch.axeId).Append(':');
            foreach (var s in Account.storage)
            {
                var def = ContentDatabase.Item(s.itemId);
                if (def != null && def.type != ItemType.Material) sb.Append(s.itemId).Append(',');
            }
            return sb.ToString();
        }

        protected override void Build(VisualElement host)
        {
            var ch = Ch;
            if (ch == null) return;
            var cls = ContentDatabase.Class(ch.classId);

            var scroll = new ScrollView();
            host.Add(scroll);

            // Identity + XP
            var idCard = Card();
            var levelLabel = Text("", "zone-name");
            idCard.Add(Row(Icon("class_" + ch.classId, "icon-l"), levelLabel, Spacer()));
            var (bar, fill, barLabel) = Bar("bar-fill--xp");
            idCard.Add(bar);
            Bind(() =>
            {
                levelLabel.text = $"{ch.name} — Lv {ch.level} {cls?.name}";
                double next = XpCurve.ToNext(ch.level);
                fill.style.width = Length.Percent((float)(ch.xp / next * 100.0));
                barLabel.text = $"{Fmt.N(ch.xp)} / {Fmt.N(next)} XP";
            });
            scroll.Add(idCard);

            // Appearance
            var lookCard = Card("Appearance");
            lookCard.Add(Text("Pick this character's look. Drop player_*.png in Resources/Sprites for more.", "muted"));
            var lookRow = new VisualElement();
            lookRow.AddToClassList("row");
            foreach (var spriteId in SpriteLibrary.PlayerSpriteIds())
            {
                string id = spriteId;
                var pick = new Button(() => Gm.SetAppearance(ch, id));
                pick.AddToClassList("sprite-pick");
                if (ch.spriteId == id) pick.AddToClassList("sprite-pick--active");
                pick.style.backgroundImage = new StyleBackground(SpriteLibrary.Get(id));
                lookRow.Add(pick);
            }
            lookCard.Add(lookRow);
            scroll.Add(lookCard);

            // Promotion
            if (ch.classId == "beginner" && ch.level >= GameManager.PromoteLevel)
            {
                var promo = Card("🎓 Choose a class!");
                promo.Add(Text("This is permanent and unlocks new talents.", "muted"));
                var row = new VisualElement();
                row.AddToClassList("row");
                foreach (var classId in new[] { "warrior", "archer", "mage" })
                {
                    string id = classId;
                    var def = ContentDatabase.Class(id);
                    var btn = new Button(() => Gm.Promote(ch, id)) { text = def.name };
                    btn.AddToClassList("btn-primary");
                    row.Add(btn);
                }
                promo.Add(row);
                scroll.Add(promo);
            }

            // Stats
            var statsCard = Card("Stats");
            var statsLabel = new Label();
            statsLabel.AddToClassList("stats-text");
            statsCard.Add(statsLabel);
            Bind(() =>
            {
                var s = StatCalculator.Compute(Account, ch);
                statsLabel.text =
                    $"STR {Fmt.N(s.str)}    AGI {Fmt.N(s.agi)}    WIS {Fmt.N(s.wis)}    LUK {Fmt.N(s.luk)}\n" +
                    $"⚔ Damage {Fmt.N(s.damage)}      🛡 Defense {Fmt.N(s.defense)}      ❤ HP {Fmt.N(s.maxHp)}\n" +
                    $"⛏ Mining eff {Fmt.N(s.miningEff)}      🪓 Choppin eff {Fmt.N(s.choppinEff)}\n" +
                    $"⏳ AFK rate {s.afkRate:P0}      ✨ XP x{s.xpMult:0.##}      🎁 Drops x{s.dropMult:0.##}      🪙 Coins x{s.coinMult:0.##}";
            });
            scroll.Add(statsCard);

            // Equipment
            var equipCard = Card("Equipment");
            equipCard.Add(BuildSlot(ch, ItemType.Weapon, "Weapon"));
            equipCard.Add(BuildSlot(ch, ItemType.Armor, "Armor"));
            equipCard.Add(BuildSlot(ch, ItemType.Pickaxe, "Pickaxe"));
            equipCard.Add(BuildSlot(ch, ItemType.Axe, "Axe"));
            scroll.Add(equipCard);

            // Talents
            var talentCard = Card("Talents");
            var pointsLabel = Text("", "gold");
            talentCard.Add(pointsLabel);
            Bind(() => pointsLabel.text = $"Points available: {ch.AvailableTalentPoints()}  (1 per level)");

            if (cls != null)
            {
                foreach (var talentId in cls.talentIds)
                {
                    var def = ContentDatabase.Talent(talentId);
                    if (def == null) continue;
                    var row = new VisualElement();
                    row.AddToClassList("row");
                    row.AddToClassList("talent-row");
                    var rankLabel = Text("", "gold");
                    var plus = new Button(() => Gm.SpendTalentPoint(ch, def)) { text = "+" };
                    plus.AddToClassList("talent-plus");
                    var info = new VisualElement();
                    info.AddToClassList("grow");
                    info.Add(Text(def.name, "talent-name"));
                    info.Add(Text(def.desc, "muted"));
                    row.Add(info);
                    row.Add(rankLabel);
                    row.Add(plus);
                    Bind(() =>
                    {
                        int rank = ch.GetTalentRank(def.id);
                        rankLabel.text = $"{rank}/{def.maxRank}";
                        plus.SetEnabled(ch.AvailableTalentPoints() > 0 && rank < def.maxRank);
                    });
                    talentCard.Add(row);
                }
            }
            scroll.Add(talentCard);
        }

        VisualElement BuildSlot(CharacterState ch, ItemType slot, string label)
        {
            const string none = "(none)";
            var options = new List<string> { none };
            var idsByName = new Dictionary<string, string>();

            string equippedId = ch.GetEquipped(slot);
            var equipped = ContentDatabase.Item(equippedId);
            if (equipped != null)
            {
                options.Add(equipped.name);
                idsByName[equipped.name] = equipped.id;
            }
            foreach (var stack in Account.storage)
            {
                var def = ContentDatabase.Item(stack.itemId);
                if (def == null || def.type != slot) continue;
                if (!idsByName.ContainsKey(def.name))
                {
                    options.Add(def.name);
                    idsByName[def.name] = def.id;
                }
            }

            var dropdown = new DropdownField(label, options, equipped != null ? options.IndexOf(equipped.name) : 0);
            dropdown.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue == none) Gm.Unequip(ch, slot);
                else if (idsByName.TryGetValue(evt.newValue, out var id) && id != ch.GetEquipped(slot)) Gm.Equip(ch, id);
            });
            return dropdown;
        }
    }
}

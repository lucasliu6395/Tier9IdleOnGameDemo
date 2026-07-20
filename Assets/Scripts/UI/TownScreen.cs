using System.Text;
using Tier9.Content;
using Tier9.Core;
using UnityEngine.UIElements;

namespace Tier9.UI
{
    public class TownScreen : ScreenBase
    {
        string _subTab = "anvil";

        /// <summary>Used by town stations in the world to open directly to their tab.</summary>
        public void SetSubTab(string kind)
        {
            if (kind == "anvil" || kind == "shop" || kind == "stamps") _subTab = kind;
        }

        protected override string StructureKey()
        {
            var sb = new StringBuilder();
            sb.Append(Account.selectedCharacter).Append(':').Append(_subTab).Append(':');
            foreach (var s in Account.storage) sb.Append(s.itemId).Append(',');
            return sb.ToString();
        }

        protected override void Build(VisualElement host)
        {
            var tabs = new VisualElement();
            tabs.AddToClassList("subtab-bar");
            foreach (var (id, label) in new[] { ("anvil", "🔨 Anvil"), ("shop", "🛒 Shop"), ("stamps", "📜 Stamps") })
            {
                string tabId = id;
                var btn = new Button(() => { _subTab = tabId; Refresh(); }) { text = label };
                btn.AddToClassList("tab-btn");
                if (tabId == _subTab) btn.AddToClassList("tab-btn--active");
                tabs.Add(btn);
            }
            host.Add(tabs);

            var scroll = new ScrollView();
            host.Add(scroll);

            switch (_subTab)
            {
                case "anvil": BuildAnvil(scroll); break;
                case "shop": BuildShop(scroll); break;
                case "stamps": BuildStamps(scroll); break;
            }
        }

        static string ItemStatText(ItemDef item)
        {
            if (item == null) return "";
            var parts = new StringBuilder();
            if (item.damage > 0) parts.Append($"+{Fmt.N(item.damage)} damage  ");
            if (item.defense > 0) parts.Append($"+{Fmt.N(item.defense)} defense  ");
            if (item.hp > 0) parts.Append($"+{Fmt.N(item.hp)} HP  ");
            if (item.toolPower > 0) parts.Append($"+{Fmt.N(item.toolPower)} tool power  ");
            return parts.ToString().TrimEnd();
        }

        void BuildAnvil(VisualElement parent)
        {
            parent.Add(Text("Craft gear from gathered materials, then equip it on the Character tab.", "muted"));
            foreach (var recipe in ContentDatabase.Recipes)
            {
                var r = recipe;
                var result = ContentDatabase.Item(r.resultItemId);
                var card = Card();
                card.Add(Row(Icon(r.resultItemId, "icon-m"), Text(result?.name ?? r.resultItemId, "zone-name"), Spacer()));
                card.Add(Text(ItemStatText(result), "muted"));

                var costLabel = new Label();
                costLabel.AddToClassList("muted");
                card.Add(costLabel);

                var craft = new Button(() => Gm.Craft(r)) { text = "Craft" };
                craft.AddToClassList("btn-primary");
                card.Add(craft);

                Bind(() =>
                {
                    var sb = new StringBuilder("Cost: ");
                    foreach (var c in r.cost)
                    {
                        var def = ContentDatabase.Item(c.itemId);
                        long have = Account.GetItemCount(c.itemId);
                        string color = have >= c.count ? "#9fdc7f" : "#e57373";
                        sb.Append($"<color={color}>{def?.name ?? c.itemId} {Fmt.N(have)}/{Fmt.N(c.count)}</color>   ");
                    }
                    costLabel.text = sb.ToString();
                    craft.SetEnabled(Account.HasItems(r.cost));
                });
                parent.Add(card);
            }
        }

        void BuildShop(VisualElement parent)
        {
            parent.Add(Text("Buy", "section-title"));
            foreach (var item in ContentDatabase.Items)
            {
                if (item.buyPrice <= 0) continue;
                var i = item;
                var card = Card();
                card.Add(Row(Icon(i.id, "icon-m"), Text(i.name, "zone-name"), Spacer(), Text($"🪙 {Fmt.N(i.buyPrice)}", "gold")));
                string stats = ItemStatText(i);
                if (stats.Length > 0) card.Add(Text(stats, "muted"));
                var buy = new Button(() => Gm.BuyItem(i)) { text = "Buy" };
                buy.AddToClassList("btn-primary");
                card.Add(buy);
                Bind(() => buy.SetEnabled(Account.coins >= i.buyPrice));
                parent.Add(card);
            }

            parent.Add(Text("Sell from Storage", "section-title"));
            if (Account.storage.Count == 0)
            {
                parent.Add(Text("Nothing in storage yet — go fight or gather!", "muted"));
                return;
            }
            foreach (var stack in Account.storage)
            {
                var itemId = stack.itemId;
                var def = ContentDatabase.Item(itemId);
                if (def == null) continue;
                var card = Card();
                var countLabel = Text("", "muted");
                card.Add(Row(Icon(itemId, "icon-m"), Text(def.name, "zone-name"), Spacer(), countLabel));
                var row = Row(
                    new Button(() => Gm.SellItem(itemId, 1)) { text = $"Sell 1 (🪙 {Fmt.N(def.sellPrice)})" },
                    new Button(() => Gm.SellItem(itemId, long.MaxValue)) { text = "Sell All" });
                card.Add(row);
                Bind(() => countLabel.text = $"x{Fmt.N(Account.GetItemCount(itemId))}");
                parent.Add(card);
            }
        }

        void BuildStamps(VisualElement parent)
        {
            parent.Add(Text("Stamps are permanent, account-wide bonuses shared by every character.", "muted"));
            foreach (var stamp in ContentDatabase.Stamps)
            {
                var s = stamp;
                var card = Card();
                var levelLabel = Text("", "gold");
                card.Add(Row(Icon(s.id, "icon-m"), Text(s.name, "zone-name"), Spacer(), levelLabel));
                var effectLabel = Text("", "muted");
                card.Add(effectLabel);
                var costLabel = new Label();
                costLabel.AddToClassList("muted");
                card.Add(costLabel);
                var upgrade = new Button(() => Gm.UpgradeStamp(s)) { text = "Upgrade" };
                upgrade.AddToClassList("btn-primary");
                card.Add(upgrade);

                Bind(() =>
                {
                    int level = Account.GetStampLevel(s.id);
                    levelLabel.text = $"Lv {level}/{s.maxLevel}";
                    effectLabel.text = $"{EffectName(s.effect)}: +{Fmt.N(s.valuePerLevel * level)}% now, +{Fmt.N(s.valuePerLevel)}% per level";
                    if (level >= s.maxLevel)
                    {
                        costLabel.text = "MAX level!";
                        upgrade.SetEnabled(false);
                        return;
                    }
                    double coinCost = Gm.StampCoinCost(s, level);
                    long itemCost = Gm.StampItemCost(s, level);
                    var matDef = ContentDatabase.Item(s.costItemId);
                    long have = Account.GetItemCount(s.costItemId);
                    string coinColor = Account.coins >= coinCost ? "#9fdc7f" : "#e57373";
                    string matColor = have >= itemCost ? "#9fdc7f" : "#e57373";
                    costLabel.text = $"Next: <color={coinColor}>🪙 {Fmt.N(coinCost)}</color>  +  <color={matColor}>{matDef?.name} {Fmt.N(have)}/{Fmt.N(itemCost)}</color>";
                    upgrade.SetEnabled(Account.coins >= coinCost && have >= itemCost);
                });
                parent.Add(card);
            }
        }

        static string EffectName(EffectType effect)
        {
            switch (effect)
            {
                case EffectType.DamagePct: return "Damage";
                case EffectType.FlatDamage: return "Base damage";
                case EffectType.MiningEffPct: return "Mining efficiency";
                case EffectType.ChoppinEffPct: return "Choppin efficiency";
                case EffectType.SkillEffPct: return "All skill efficiency";
                case EffectType.AfkRatePct: return "AFK gain rate";
                case EffectType.XpPct: return "XP gain";
                case EffectType.DropPct: return "Drop rate";
                case EffectType.CoinPct: return "Coin gain";
                default: return effect.ToString();
            }
        }
    }
}

using System.Text;
using Tier9.Content;
using Tier9.Core;
using UnityEngine.UIElements;

namespace Tier9.UI
{
    /// <summary>
    /// The account's shared item storage, grouped by category. Materials are view-only;
    /// gear can be equipped onto (or unequipped from) the selected character in place.
    /// </summary>
    public class InventoryScreen : ScreenBase
    {
        static readonly (string title, ItemType[] types, bool gear)[] Sections =
        {
            ("Materials", new[] { ItemType.Material }, false),
            ("Weapons", new[] { ItemType.Weapon }, true),
            ("Armor", new[] { ItemType.Armor }, true),
            ("Tools", new[] { ItemType.Pickaxe, ItemType.Axe }, true),
        };

        protected override string StructureKey()
        {
            var ch = Ch;
            var sb = new StringBuilder();
            sb.Append(Account.selectedCharacter).Append(':');
            if (ch != null)
                sb.Append(ch.weaponId).Append(',').Append(ch.armorId).Append(',')
                  .Append(ch.pickId).Append(',').Append(ch.axeId).Append(':');
            // Structure depends on WHICH items are present; counts update via Bind.
            foreach (var s in Account.storage) sb.Append(s.itemId).Append(',');
            return sb.ToString();
        }

        protected override void Build(VisualElement host)
        {
            var ch = Ch;
            var scroll = new ScrollView();
            host.Add(scroll);

            scroll.Add(Text(ch != null
                ? $"Shared account storage — equip gear onto {ch.name}."
                : "Shared account storage.", "muted"));

            foreach (var (title, types, gear) in Sections)
                BuildSection(scroll, title, types, gear, ch);
        }

        void BuildSection(VisualElement parent, string title, ItemType[] types, bool gear, CharacterState ch)
        {
            var card = Card(title);
            bool any = false;

            // Currently-equipped gear for these slots (equipped items live on the character, not storage).
            if (gear && ch != null)
            {
                foreach (var slot in types)
                {
                    var def = ContentDatabase.Item(ch.GetEquipped(slot));
                    if (def == null) continue;
                    any = true;
                    var slotRef = slot;
                    var row = Row(Icon(def.id, "icon-m"), Info(def, "⭐ Equipped   " + StatText(def)), Spacer());
                    row.Add(new Button(() => Gm.Unequip(ch, slotRef)) { text = "Unequip" });
                    card.Add(row);
                }
            }

            // Unequipped stock in storage.
            foreach (var stack in Account.storage)
            {
                var def = ContentDatabase.Item(stack.itemId);
                if (def == null || System.Array.IndexOf(types, def.type) < 0) continue;
                any = true;

                var itemId = def.id;
                var countLabel = Text("", "gold");
                var row = Row(Icon(def.id, "icon-m"), Info(def, StatText(def)), Spacer(), countLabel);
                if (gear && ch != null)
                {
                    var equip = new Button(() => Gm.Equip(ch, itemId)) { text = "Equip" };
                    equip.AddToClassList("btn-primary");
                    row.Add(equip);
                }
                card.Add(row);
                Bind(() => countLabel.text = $"x{Fmt.N(Account.GetItemCount(itemId))}");
            }

            if (!any) card.Add(Text("Empty.", "muted"));
            parent.Add(card);
        }

        static VisualElement Info(ItemDef def, string sub)
        {
            var info = new VisualElement();
            info.AddToClassList("grow");
            info.Add(Text(def.name, "zone-name"));
            if (!string.IsNullOrEmpty(sub)) info.Add(Text(sub, "muted"));
            return info;
        }

        static string StatText(ItemDef def)
        {
            var sb = new StringBuilder();
            if (def.damage > 0) sb.Append($"+{Fmt.N(def.damage)} dmg  ");
            if (def.defense > 0) sb.Append($"+{Fmt.N(def.defense)} def  ");
            if (def.hp > 0) sb.Append($"+{Fmt.N(def.hp)} HP  ");
            if (def.toolPower > 0) sb.Append($"+{Fmt.N(def.toolPower)} tool  ");
            if (def.sellPrice > 0) sb.Append($"🪙 {Fmt.N(def.sellPrice)}");
            return sb.ToString().TrimEnd();
        }
    }
}

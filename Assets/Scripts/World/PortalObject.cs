using Tier9.Content;
using Tier9.Core;
using Tier9.UI;
using UnityEngine;

namespace Tier9.World
{
    /// <summary>A doorway to another map. Locked portals show their requirement.</summary>
    public class PortalObject : MonoBehaviour
    {
        PortalSpec _spec;
        bool _unlocked;
        string _lockReason;

        public void Init(PortalSpec spec, CharacterState ch)
        {
            _spec = spec;
            ComputeLock(ch);

            var col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.6f, 2.4f);

            var spriteGo = new GameObject("sprite");
            spriteGo.transform.SetParent(transform, false);
            spriteGo.transform.localScale = new Vector3(1.1f, 2.0f, 1f);
            var sr = spriteGo.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteLibrary.Get("portal");
            sr.sortingOrder = 1;
            if (!_unlocked) sr.color = new Color(1f, 1f, 1f, 0.35f);

            var target = ContentDatabase.Map(_spec.targetMapId);
            string title = _spec.label ?? target?.name ?? _spec.targetMapId;
            if (_unlocked)
            {
                WorldText.Create(transform, new Vector3(0, 1.9f, 0), $"[E] {title}",
                    new Color(0.6f, 0.9f, 1f), 0.9f, 6);
            }
            else
            {
                WorldText.Create(transform, new Vector3(0, 2.2f, 0), $"{title} (locked)",
                    new Color(1f, 0.45f, 0.4f), 0.85f, 6);
                WorldText.Create(transform, new Vector3(0, 1.6f, 0), _lockReason,
                    new Color(0.85f, 0.6f, 0.55f), 0.7f, 6);
            }
        }

        void ComputeLock(CharacterState ch)
        {
            _unlocked = true;
            _lockReason = "";
            var target = ContentDatabase.Map(_spec.targetMapId);
            if (target == null || ch == null) return;

            if (target.IsCombat)
            {
                if (!GameManager.I.IsZoneUnlocked(ch, target.id))
                {
                    _unlocked = false;
                    int idx = ContentDatabase.ZoneIndex(target.id);
                    var prev = idx > 0 ? ContentDatabase.Zones[idx - 1] : null;
                    long remaining = prev == null ? 0 : System.Math.Max(0, prev.killsToNext - ch.GetKills(prev.id));
                    _lockReason = prev == null ? "Locked" : $"Defeat {Fmt.N(remaining)} more in {prev.name}";
                }
            }
            else if (target.IsSkill)
            {
                var node = ContentDatabase.Node(target.nodeId);
                if (node != null && ch.GetSkillLevel(node.skill) < node.reqSkillLevel)
                {
                    _unlocked = false;
                    _lockReason = $"Requires {node.skill} Lv {node.reqSkillLevel}";
                }
            }
        }

        public void Interact(CharacterState ch)
        {
            ComputeLock(ch);
            if (!_unlocked)
            {
                FloatyText.Spawn(transform.position + Vector3.up * 1.2f, _lockReason, new Color(1f, 0.45f, 0.4f));
                return;
            }
            GameManager.I.TravelTo(ch, _spec.targetMapId);
        }
    }
}

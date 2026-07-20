using Tier9.Content;
using Tier9.UI;
using UnityEngine;

namespace Tier9.World
{
    /// <summary>A mineable rock / choppable tree. Press E nearby to start auto-gathering;
    /// moving away or jumping stops it.</summary>
    public class GatherNode : MonoBehaviour
    {
        public NodeDef Def { get; private set; }

        SpriteRenderer _sr;
        float _pulse;

        public void Init(NodeDef def)
        {
            Def = def;

            var col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.8f, 1.8f);

            var spriteGo = new GameObject("sprite");
            spriteGo.transform.SetParent(transform, false);
            spriteGo.transform.localScale = Vector3.one * 1.5f;
            _sr = spriteGo.AddComponent<SpriteRenderer>();
            _sr.sprite = SpriteLibrary.Get(def.id);
            _sr.sortingOrder = 1;

            string verb = def.skill == SkillType.Mining ? "Mine" : "Chop";
            WorldText.Create(transform, new Vector3(0, 1.6f, 0), $"[E] {verb} {def.name}",
                new Color(0.75f, 0.8f, 0.9f), 0.85f, 6);
        }

        public void Pulse() => _pulse = 1f;

        void Update()
        {
            if (_pulse > 0f)
            {
                _pulse -= Time.deltaTime * 4f;
                float s = 1.5f * (1f + 0.18f * Mathf.Max(0, _pulse));
                _sr.transform.localScale = new Vector3(s, s, 1f);
            }
        }
    }
}

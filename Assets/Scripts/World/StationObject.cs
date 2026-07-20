using Tier9.UI;
using UnityEngine;

namespace Tier9.World
{
    /// <summary>A town station (anvil/shop/stamps). Press E nearby to open its menu.</summary>
    public class StationObject : MonoBehaviour
    {
        string _kind;

        static string DisplayName(string kind)
        {
            switch (kind)
            {
                case "anvil": return "Anvil";
                case "shop": return "Shop";
                case "stamps": return "Stamps";
                default: return kind;
            }
        }

        public void Init(string kind)
        {
            _kind = kind;

            var col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(2.0f, 2.0f);

            var spriteGo = new GameObject("sprite");
            spriteGo.transform.SetParent(transform, false);
            spriteGo.transform.localScale = Vector3.one * 1.6f;
            var sr = spriteGo.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteLibrary.Get("station_" + kind);
            sr.sortingOrder = 1;

            WorldText.Create(transform, new Vector3(0, 1.7f, 0), $"[E] {DisplayName(kind)}",
                new Color(1f, 0.85f, 0.5f), 0.9f, 6);
        }

        public void Interact()
        {
            if (UiRoot.I != null) UiRoot.I.OpenTownStation(_kind);
        }
    }
}

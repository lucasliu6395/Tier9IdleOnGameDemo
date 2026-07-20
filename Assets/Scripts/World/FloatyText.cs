using UnityEngine;

namespace Tier9.World
{
    /// <summary>World-space text helpers: static labels (prompts) and rising fade-out popups.</summary>
    public static class WorldText
    {
        static Font _font;

        public static Font DefaultFont
        {
            get
            {
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _font;
            }
        }

        public static TextMesh Create(Transform parent, Vector3 localPos, string text, Color color, float size = 1f, int sortingOrder = 10)
        {
            var go = new GameObject("text");
            go.transform.SetParent(parent, worldPositionStays: false);
            go.transform.localPosition = localPos;
            var tm = go.AddComponent<TextMesh>();
            tm.font = DefaultFont;
            tm.text = text;
            tm.color = color;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontSize = 48;
            tm.characterSize = 0.045f * size;
            var mr = go.GetComponent<MeshRenderer>();
            mr.material = tm.font.material;
            mr.sortingOrder = sortingOrder;
            return tm;
        }
    }

    public class FloatyText : MonoBehaviour
    {
        TextMesh _tm;
        float _life;
        const float Duration = 1.0f;

        public static void Spawn(Vector3 worldPos, string text, Color color, float size = 1f)
        {
            var go = new GameObject("floaty");
            go.transform.position = worldPos + new Vector3(Random.Range(-0.2f, 0.2f), 0, 0);
            var f = go.AddComponent<FloatyText>();
            f._tm = WorldText.Create(go.transform, Vector3.zero, text, color, size, 20);
        }

        void Update()
        {
            _life += Time.deltaTime;
            transform.position += Vector3.up * 1.4f * Time.deltaTime;
            if (_tm != null)
            {
                var c = _tm.color;
                c.a = Mathf.Clamp01(1f - _life / Duration);
                _tm.color = c;
            }
            if (_life >= Duration) Destroy(gameObject);
        }
    }
}

using Tier9.UI;
using UnityEngine;

namespace Tier9.World
{
    /// <summary>A short-lived slash crescent that sweeps and fades where the player swings.</summary>
    public class SlashVfx : MonoBehaviour
    {
        const float Life = 0.16f;

        SpriteRenderer _sr;
        float _t;
        int _dir;

        public static void Spawn(Vector3 pos, int facing)
        {
            var go = new GameObject("slash");
            go.transform.position = pos;
            var v = go.AddComponent<SlashVfx>();
            v._dir = facing;
            v._sr = go.AddComponent<SpriteRenderer>();
            v._sr.sprite = SpriteLibrary.Get("fx_slash");
            v._sr.sortingOrder = 15;
            v._sr.flipX = facing < 0;
        }

        void Update()
        {
            _t += Time.deltaTime;
            float k = Mathf.Clamp01(_t / Life);
            float s = Mathf.Lerp(0.7f, 1.5f, k);
            transform.localScale = new Vector3(s, s, 1f);
            transform.localRotation = Quaternion.Euler(0f, 0f, _dir * Mathf.Lerp(45f, -35f, k));
            var c = _sr.color;
            c.a = 1f - k;
            _sr.color = c;
            if (_t >= Life) Destroy(gameObject);
        }
    }
}

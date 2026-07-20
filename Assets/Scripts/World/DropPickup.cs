using Tier9.UI;
using UnityEngine;

namespace Tier9.World
{
    /// <summary>
    /// A coin or item drop that pops out of a kill, falls, settles, and auto-collects
    /// (with a short-range magnet pull) once the player gets close enough.
    /// </summary>
    public class DropPickup : MonoBehaviour
    {
        const float MagnetRadius = 2.6f;
        const float CollectRadius = 0.45f;
        const float MagnetSpeed = 10f;
        const float Gravity = -16f;

        double _coins;
        string _itemId;
        long _count;
        float _vx, _vy;
        float _groundY;
        bool _settled;
        float _age;
        Transform _sprite;

        public static void SpawnCoins(Vector3 at, double amount) => Spawn(at, "coin_pickup", 0.55f, amount, null, 0);
        public static void SpawnItem(Vector3 at, string itemId, long count) => Spawn(at, itemId, 0.8f, 0, itemId, count);

        static void Spawn(Vector3 at, string spriteId, float scale, double coins, string itemId, long count)
        {
            var player = WorldRunner.I != null ? WorldRunner.I.Player : null;
            if (player == null) return;

            var go = new GameObject("Drop_" + spriteId);
            go.transform.SetParent(player.transform.parent, worldPositionStays: false);
            go.transform.position = at + new Vector3(0, 0.3f, 0);
            go.AddComponent<DropPickup>().Init(spriteId, scale, coins, itemId, count);
        }

        void Init(string spriteId, float scale, double coins, string itemId, long count)
        {
            _coins = coins;
            _itemId = itemId;
            _count = count;
            _groundY = transform.position.y - 0.3f; // settle back near the kill's feet
            _vx = Random.Range(-1.2f, 1.2f);
            _vy = Random.Range(3.2f, 4.4f);

            var spriteGo = new GameObject("sprite");
            spriteGo.transform.SetParent(transform, false);
            spriteGo.transform.localScale = Vector3.one * scale;
            var sr = spriteGo.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteLibrary.Get(spriteId);
            sr.sortingOrder = 2;
            _sprite = spriteGo.transform;
        }

        void Update()
        {
            _age += Time.deltaTime;

            if (!_settled)
            {
                _vy += Gravity * Time.deltaTime;
                transform.position += new Vector3(_vx * Time.deltaTime, _vy * Time.deltaTime, 0);
                if (transform.position.y <= _groundY)
                {
                    transform.position = new Vector3(transform.position.x, _groundY, 0);
                    _settled = true;
                }
            }
            else if (_sprite != null)
            {
                _sprite.localPosition = new Vector3(0, 0.08f * Mathf.Sin(_age * 3f), 0);
            }

            var player = WorldRunner.I != null ? WorldRunner.I.Player : null;
            if (player == null) return;

            float dist = Vector2.Distance(transform.position, player.transform.position);
            if (dist <= CollectRadius)
            {
                Collect();
            }
            else if (_settled && dist <= MagnetRadius)
            {
                Vector3 dir = (player.transform.position - transform.position).normalized;
                transform.position += dir * MagnetSpeed * Time.deltaTime * (1f - dist / MagnetRadius);
            }
        }

        void Collect()
        {
            WorldRunner.I.OnDropCollected(_coins, _itemId, _count, transform.position);
            Destroy(gameObject);
        }
    }
}

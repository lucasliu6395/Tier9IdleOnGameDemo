using Tier9.Content;
using Tier9.Core;
using Tier9.UI;
using UnityEngine;

namespace Tier9.World
{
    /// <summary>Patrolling monster. Bosses chase the player; monsters with contactDamage
    /// hurt on touch. Deaths route rewards through WorldRunner (shared with the AFK sim).</summary>
    public class EnemyController : MonoBehaviour
    {
        MonsterDef _def;
        MapController _map;
        float _spawnX;
        double _hp;
        int _dir = 1;
        float _flash;

        Rigidbody2D _rb;
        BoxCollider2D _bodyCol;
        SpriteRenderer _sr;
        Transform _hpFill;

        public void Init(MonsterDef def, MapController map, float spawnX)
        {
            _def = def;
            _map = map;
            _spawnX = spawnX;
            _hp = def.hp;
            _dir = Random.value < 0.5f ? -1 : 1;

            _rb = gameObject.AddComponent<Rigidbody2D>();
            _rb.freezeRotation = true;
            _rb.gravityScale = 3f;

            float s = def.spriteScale;
            _bodyCol = gameObject.AddComponent<BoxCollider2D>();
            _bodyCol.size = new Vector2(0.8f * s, 0.85f * s);
            _bodyCol.offset = new Vector2(0, 0.05f * s);

            var trigger = gameObject.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(0.95f * s, 1.0f * s);

            var spriteGo = new GameObject("sprite");
            spriteGo.transform.SetParent(transform, false);
            spriteGo.transform.localScale = Vector3.one * s;
            _sr = spriteGo.AddComponent<SpriteRenderer>();
            _sr.sprite = SpriteLibrary.Get(def.id);
            _sr.sortingOrder = 2;

            BuildHpBar(s);

            var player = WorldRunner.I != null ? WorldRunner.I.Player : null;
            if (player != null) IgnorePlayerCollision(player);
        }

        public void IgnorePlayerCollision(PlayerController2D player)
        {
            if (player != null && player.BodyCollider != null && _bodyCol != null)
                Physics2D.IgnoreCollision(player.BodyCollider, _bodyCol);
        }

        void BuildHpBar(float scale)
        {
            var bar = new GameObject("hpbar");
            bar.transform.SetParent(transform, false);
            bar.transform.localPosition = new Vector3(0, 0.75f * scale + 0.25f, 0);

            var bg = new GameObject("bg");
            bg.transform.SetParent(bar.transform, false);
            bg.transform.localScale = new Vector3(1.1f, 0.14f, 1f);
            var bgSr = bg.AddComponent<SpriteRenderer>();
            bgSr.sprite = SpriteLibrary.Solid(new Color(0.08f, 0.09f, 0.12f));
            bgSr.sortingOrder = 4;

            var fill = new GameObject("fill");
            fill.transform.SetParent(bar.transform, false);
            fill.transform.localScale = new Vector3(1.04f, 0.09f, 1f);
            var fillSr = fill.AddComponent<SpriteRenderer>();
            fillSr.sprite = SpriteLibrary.Solid(new Color(0.85f, 0.3f, 0.3f));
            fillSr.sortingOrder = 5;
            _hpFill = fill.transform;
        }

        void UpdateHpBar()
        {
            if (_hpFill == null) return;
            float frac = Mathf.Clamp01((float)(_hp / _def.hp));
            _hpFill.localScale = new Vector3(1.04f * frac, 0.09f, 1f);
            _hpFill.localPosition = new Vector3(-1.04f * (1f - frac) / 2f, 0, 0);
        }

        void FixedUpdate()
        {
            var player = WorldRunner.I != null ? WorldRunner.I.Player : null;
            if (_def.isBoss && player != null)
            {
                float dx = player.transform.position.x - transform.position.x;
                if (Mathf.Abs(dx) > 0.4f && Mathf.Abs(dx) < 10f) _dir = dx > 0 ? 1 : -1;
            }
            else
            {
                float ahead = _dir * (_bodyCol.size.x / 2f + 0.25f);
                Vector2 frontFoot = (Vector2)transform.position + new Vector2(ahead, 0);
                if (!HitsSolid(frontFoot, Vector2.down, 1.6f)) _dir = -_dir;           // cliff ahead
                else if (HitsSolid((Vector2)transform.position + new Vector2(0, 0.1f), new Vector2(_dir, 0), _bodyCol.size.x / 2f + 0.3f)) _dir = -_dir; // wall ahead
            }
            _rb.linearVelocity = new Vector2(_dir * _def.moveSpeed, _rb.linearVelocity.y);
        }

        bool HitsSolid(Vector2 origin, Vector2 dir, float dist)
        {
            foreach (var hit in Physics2D.RaycastAll(origin, dir, dist))
            {
                if (hit.collider == null || hit.collider.isTrigger) continue;
                if (hit.collider.transform.IsChildOf(transform)) continue;
                if (hit.collider.GetComponentInParent<EnemyController>() != null) continue;
                if (hit.collider.GetComponentInParent<PlayerController2D>() != null) continue;
                return true;
            }
            return false;
        }

        void OnTriggerStay2D(Collider2D other)
        {
            if (_def.contactDamage <= 0) return;
            var player = other.GetComponentInParent<PlayerController2D>();
            if (player != null) player.TakeContactDamage(_def.contactDamage);
        }

        public void TakeHit(double damage, int fromFacing)
        {
            _hp -= damage;
            _flash = 1f;
            UpdateHpBar();
            FloatyText.Spawn(transform.position + Vector3.up * (0.6f * _def.spriteScale),
                $"-{Fmt.N(damage)}", new Color(1f, 0.9f, 0.5f));
            _rb.AddForce(new Vector2(fromFacing * 2.0f, 2.2f), ForceMode2D.Impulse);

            if (_hp <= 0)
            {
                WorldRunner.I.OnEnemyKilled(_def, transform.position);
                _map.ScheduleRespawn(_def, _spawnX);
                Destroy(gameObject);
            }
        }

        void Update()
        {
            if (_flash > 0f)
            {
                _flash -= Time.deltaTime * 4f;
                _sr.color = Color.Lerp(Color.white, new Color(1f, 0.4f, 0.4f), Mathf.Max(0, _flash));
            }
            if (_sr != null) _sr.flipX = _dir < 0;
        }
    }
}

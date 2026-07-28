using System;
using Tier9.Content;
using Tier9.Core;
using Tier9.Sim;
using Tier9.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Tier9.World
{
    /// <summary>
    /// Keyboard platformer controls for the live character:
    /// A/D or arrows move, Space/W jump, F attack (hits enemies and gather nodes alike),
    /// E interact (portals/stations).
    /// </summary>
    public class PlayerController2D : MonoBehaviour
    {
        const float MoveSpeed = 5.5f;
        const float JumpVelocity = 12.5f;
        const float AttackCooldown = 0.45f;
        const float InteractRange = 1.8f;
        const float ContactIFrames = 1.0f;
        const float RegenDelay = 3.0f;
        const float RegenPerSecond = 0.03f;   // fraction of max HP

        CharacterState _ch;
        ComputedStats _stats;
        Rigidbody2D _rb;
        BoxCollider2D _col;
        SpriteRenderer _sr;
        float _spawnX;
        int _facing = 1;
        float _attackTimer;
        float _lastHurtTime = -99f;
        float _invulnUntil;
        float _swingPunch;
        float _lunge;
        CharacterAnimator _anim;
        string _animId;

        public double CurrentHp { get; private set; }
        public double MaxHp => _stats == null ? 10 : _stats.maxHp;
        public BoxCollider2D BodyCollider => _col;

        public void Init(CharacterState ch, float spawnX)
        {
            _ch = ch;
            _spawnX = spawnX;

            _rb = gameObject.AddComponent<Rigidbody2D>();
            _rb.freezeRotation = true;
            _rb.gravityScale = 3.5f;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            _col = gameObject.AddComponent<BoxCollider2D>();
            _col.size = new Vector2(0.7f, 1.25f);

            var spriteGo = new GameObject("sprite");
            spriteGo.transform.SetParent(transform, false);
            spriteGo.transform.localScale = Vector3.one * 1.3f;
            _sr = spriteGo.AddComponent<SpriteRenderer>();
            _sr.sortingOrder = 3;

            // The body is always a player-character sprite, never a class weapon-icon.
            _animId = string.IsNullOrEmpty(_ch.spriteId) ? AccountState.DefaultPlayerSprites[0] : _ch.spriteId;
            _sr.sprite = SpriteLibrary.Get(_animId);
            _anim = spriteGo.AddComponent<CharacterAnimator>();
            _anim.Setup(_sr, _animId);

            RefreshStats();
            CurrentHp = MaxHp;
            GameManager.I.StateChanged += RefreshStats;

            // Walk through enemies bodily (their hit-triggers still work)
            foreach (var enemy in UnityEngine.Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
                enemy.IgnorePlayerCollision(this);
        }

        void OnDestroy()
        {
            if (GameManager.I != null) GameManager.I.StateChanged -= RefreshStats;
        }

        void RefreshStats()
        {
            _stats = StatCalculator.Compute(GameManager.I.Account, _ch);
            if (CurrentHp > MaxHp) CurrentHp = MaxHp;
            // Reflect a live appearance change (Character tab): reload the animation set.
            if (_anim != null)
            {
                var id = string.IsNullOrEmpty(_ch.spriteId) ? AccountState.DefaultPlayerSprites[0] : _ch.spriteId;
                if (id != _animId) { _anim.Setup(_sr, id); _animId = id; }
            }
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || _stats == null) return;

            float move = 0f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move += 1f;
            bool jumpPressed = kb.spaceKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame;
            bool attackPressed = kb.fKey.wasPressedThisFrame || kb.jKey.wasPressedThisFrame;
            bool interactPressed = kb.eKey.wasPressedThisFrame;

            bool grounded = IsGrounded();

            var v = _rb.linearVelocity;
            v.x = move * MoveSpeed;
            if (jumpPressed && grounded) v.y = JumpVelocity;
            _rb.linearVelocity = v;

            if (move != 0)
            {
                _facing = move > 0 ? 1 : -1;
                _sr.flipX = _facing < 0;
            }

            _attackTimer -= Time.deltaTime;
            if (attackPressed && _attackTimer <= 0f) DoAttack();

            if (interactPressed) TryInteract();

            // Locomotion animation state
            string clip = !grounded
                ? (_rb.linearVelocity.y > 0.1f ? "jump" : "fall")
                : (Mathf.Abs(move) > 0.01f ? "run" : "idle");
            _anim.SetState(clip);

            // Passive regen when unhurt for a while
            if (Time.time - _lastHurtTime > RegenDelay && CurrentHp < MaxHp)
                CurrentHp = Math.Min(MaxHp, CurrentHp + MaxHp * RegenPerSecond * Time.deltaTime);

            // Swing feedback: a quick forward lunge + scale pop (animator owns the frames)
            if (_lunge > 0f) _lunge -= Time.deltaTime * 6f;
            if (_swingPunch > 0f) _swingPunch -= Time.deltaTime * 5f;
            float lunge = Mathf.Max(0f, _lunge);
            _sr.transform.localPosition = new Vector3(_facing * 0.28f * lunge, 0f, 0f);
            _sr.transform.localScale = Vector3.one * (1.3f + 0.12f * Mathf.Max(0f, _swingPunch));

            _sr.color = Time.time < _invulnUntil && Mathf.PingPong(Time.time * 8f, 1f) > 0.5f
                ? new Color(1f, 1f, 1f, 0.45f)
                : Color.white;
        }

        bool IsGrounded()
        {
            var b = _col.bounds;
            var hits = Physics2D.BoxCastAll(b.center, new Vector2(b.size.x * 0.9f, 0.1f), 0f, Vector2.down, b.extents.y);
            foreach (var h in hits)
            {
                if (h.collider == null || h.collider.isTrigger) continue;
                if (h.collider.transform.IsChildOf(transform)) continue;
                if (h.collider.GetComponentInParent<EnemyController>() != null) continue;
                return true;
            }
            return false;
        }

        void DoAttack()
        {
            _attackTimer = AttackCooldown;
            _swingPunch = 1f;
            _lunge = 1f;
            SlashVfx.Spawn((Vector2)transform.position + new Vector2(_facing * 0.95f, 0.25f), _facing);
            Vector2 center = (Vector2)transform.position + new Vector2(_facing * 0.95f, 0.25f);
            var hits = Physics2D.OverlapBoxAll(center, new Vector2(1.9f, 1.5f), 0f);
            foreach (var hit in hits)
            {
                var enemy = hit.GetComponentInParent<EnemyController>();
                if (enemy != null) { enemy.TakeHit(_stats.damage, _facing); continue; }

                var node = hit.GetComponentInParent<GatherNode>();
                if (node != null) GatherHit(node);
            }
        }

        void GatherHit(GatherNode node)
        {
            if (_ch.GetSkillLevel(node.Def.skill) < node.Def.reqSkillLevel)
            {
                FloatyText.Spawn(node.transform.position + Vector3.up * 1.2f,
                    $"Requires {node.Def.skill} Lv {node.Def.reqSkillLevel}", new Color(1f, 0.45f, 0.4f));
                return;
            }
            node.Pulse();
            WorldRunner.I.OnGathered(node.Def, 1, node.transform.position);
        }

        void TryInteract()
        {
            var hits = Physics2D.OverlapCircleAll(transform.position, InteractRange);
            Component best = null;
            float bestDist = float.MaxValue;
            foreach (var hit in hits)
            {
                Component c = hit.GetComponentInParent<PortalObject>();
                if (c == null) c = hit.GetComponentInParent<StationObject>();
                if (c == null) continue;
                float d = Vector2.Distance(transform.position, c.transform.position);
                if (d < bestDist) { bestDist = d; best = c; }
            }
            switch (best)
            {
                case PortalObject portal: portal.Interact(_ch); break;
                case StationObject station: station.Interact(); break;
            }
        }

        // ---- damage ----

        public void TakeContactDamage(double rawDamage)
        {
            if (rawDamage <= 0 || Time.time < _invulnUntil || _stats == null) return;
            double dmg = rawDamage * 50.0 / (50.0 + _stats.defense);
            CurrentHp -= dmg;
            _lastHurtTime = Time.time;
            _invulnUntil = Time.time + ContactIFrames;
            FloatyText.Spawn(transform.position + Vector3.up * 1.2f, $"-{Fmt.N(dmg)}", new Color(1f, 0.35f, 0.3f));
            if (CurrentHp <= 0) Die();
        }

        void Die()
        {
            CurrentHp = MaxHp;
            _invulnUntil = Time.time + 2f;
            transform.position = new Vector3(_spawnX, 1.5f, 0);
            _rb.linearVelocity = Vector2.zero;
            FloatyText.Spawn(transform.position + Vector3.up * 1.4f, "Knocked out!", new Color(1f, 0.5f, 0.3f), 1.3f);
        }
    }
}

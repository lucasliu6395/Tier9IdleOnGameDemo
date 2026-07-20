using Tier9.Content;
using Tier9.Core;
using Tier9.Sim;
using UnityEngine;

namespace Tier9.World
{
    /// <summary>
    /// Owns the walkable world: builds the map the selected character is on, hands them
    /// live control (the AFK sim skips them), and routes live kills/gathers through the
    /// same reward code the sim uses.
    /// </summary>
    public class WorldRunner : MonoBehaviour
    {
        public static WorldRunner I { get; private set; }

        public MapDef CurrentMap { get; private set; }
        public PlayerController2D Player { get; private set; }

        /// <summary>Gains earned by live play since entering the current map (HUD display).</summary>
        public AfkResult Session { get; private set; } = new AfkResult();

        MapController _map;
        string _lastMapId;

        void Awake() => I = this;

        void Start()
        {
            GameManager.I.TravelChanged += Rebuild;
            Rebuild();
        }

        void OnDestroy()
        {
            if (GameManager.I != null) GameManager.I.TravelChanged -= Rebuild;
        }

        public void Rebuild()
        {
            if (_map != null)
            {
                Destroy(_map.gameObject);
                _map = null;
                Player = null;
            }
            Session = new AfkResult();

            var ch = GameManager.I.Account.Selected;
            GameManager.I.LiveCharacter = ch;
            if (ch == null)
            {
                CurrentMap = null;
                return;
            }

            CurrentMap = ContentDatabase.Map(GameManager.MapIdForCharacter(ch)) ?? ContentDatabase.Map("town");

            float spawnX = 1.5f;
            if (_lastMapId != null)
            {
                foreach (var p in CurrentMap.portals)
                    if (p.targetMapId == _lastMapId)
                        spawnX = Mathf.Clamp(p.x, 2f, CurrentMap.length - 2f);
            }

            var go = new GameObject("Map_" + CurrentMap.id);
            _map = go.AddComponent<MapController>();
            _map.Build(CurrentMap, ch, spawnX);
            Player = _map.Player;
            _lastMapId = CurrentMap.id;
        }

        /// <summary>Called by EnemyController when the live player lands the killing blow.</summary>
        public void OnEnemyKilled(MonsterDef mon, Vector3 at)
        {
            var gm = GameManager.I;
            var ch = gm.Account.Selected;
            var zone = ContentDatabase.Zone(ch.taskTargetId);
            if (zone == null) return;

            var stats = StatCalculator.Compute(gm.Account, ch);
            int levelBefore = ch.level;
            AfkSimulator.ApplyKillRewards(gm.Account, ch, zone, mon, 1, stats, Session);

            FloatyText.Spawn(at + Vector3.up * 0.6f, $"+{Fmt.N(mon.xp * stats.xpMult)} XP", new Color(1f, 0.85f, 0.4f));
            if (ch.level > levelBefore)
                FloatyText.Spawn(Player.transform.position + Vector3.up * 1.6f, "LEVEL UP!", new Color(0.6f, 1f, 0.5f), 1.6f);
            gm.NotifyChanged();
        }

        /// <summary>Called by the player's gathering loop for each resource batch collected.</summary>
        public void OnGathered(NodeDef node, long count, Vector3 at)
        {
            var gm = GameManager.I;
            var ch = gm.Account.Selected;
            var stats = StatCalculator.Compute(gm.Account, ch);
            int skillBefore = ch.GetSkillLevel(node.skill);
            AfkSimulator.ApplyGatherRewards(gm.Account, ch, node, count, stats, Session);

            var item = ContentDatabase.Item(node.itemId);
            FloatyText.Spawn(at + Vector3.up * 0.8f, $"+{count} {item?.name}", new Color(0.65f, 0.9f, 1f));
            if (ch.GetSkillLevel(node.skill) > skillBefore)
                FloatyText.Spawn(at + Vector3.up * 1.6f, $"{node.skill} LEVEL UP!", new Color(0.6f, 1f, 0.5f), 1.5f);
            gm.NotifyChanged();
        }
    }
}

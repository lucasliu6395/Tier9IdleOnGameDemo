using System.Collections;
using Tier9.Content;
using Tier9.Core;
using Tier9.UI;
using UnityEngine;

namespace Tier9.World
{
    /// <summary>Builds a walkable map (ground, platforms, spawns, portals, stations, nodes)
    /// entirely from a MapDef, and drives the follow camera.</summary>
    public class MapController : MonoBehaviour
    {
        public MapDef Def { get; private set; }
        public PlayerController2D Player { get; private set; }

        Camera _cam;
        float _camHalfWidth;

        public void Build(MapDef def, CharacterState ch, float playerSpawnX)
        {
            Def = def;

            BuildGroundBox(0, def.length);
            foreach (var p in def.platforms) BuildPlatform(p);

            if (def.IsCombat)
            {
                var mon = ContentDatabase.Monster(def.monsterId);
                for (int i = 0; i < def.enemyCount; i++)
                {
                    float x = Mathf.Lerp(7f, def.length - 3f, def.enemyCount <= 1 ? 0.5f : i / (float)(def.enemyCount - 1));
                    SpawnEnemy(mon, x);
                }
            }

            if (def.IsSkill)
            {
                var node = ContentDatabase.Node(def.nodeId);
                foreach (float x in def.nodePositions) SpawnNode(node, x);
            }

            foreach (var s in def.stations) SpawnStation(s);
            foreach (var p in def.portals) SpawnPortal(p, ch);

            // Player
            var playerGo = new GameObject("Player");
            playerGo.transform.SetParent(transform, false);
            playerGo.transform.position = new Vector3(playerSpawnX, 1.5f, 0);
            Player = playerGo.AddComponent<PlayerController2D>();
            Player.Init(ch, playerSpawnX);

            SetupCamera(def);
        }

        // ---- geometry ----

        static SpriteRenderer MakeSprite(Transform parent, string spriteId, Vector3 pos, Vector3 scale, int order)
        {
            var go = new GameObject(spriteId);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteLibrary.Get(spriteId);
            sr.sortingOrder = order;
            return sr;
        }

        void BuildGroundBox(float startX, float width)
        {
            var go = new GameObject("Ground");
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(startX + width / 2f, -0.5f, 0);
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(width, 1f);
            MakeSprite(go.transform, "tile_ground", Vector3.zero, new Vector3(width, 1f, 1f), 0);

            // Invisible side walls so nobody walks off the map
            foreach (float x in new[] { startX - 0.5f, startX + width + 0.5f })
            {
                var wall = new GameObject("Wall");
                wall.transform.SetParent(transform, false);
                wall.transform.position = new Vector3(x, 4f, 0);
                var wcol = wall.AddComponent<BoxCollider2D>();
                wcol.size = new Vector2(1f, 12f);
            }
        }

        void BuildPlatform(PlatformSpec p)
        {
            var go = new GameObject("Platform");
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(p.x + p.w / 2f, p.y - 0.25f, 0);
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(p.w, 0.5f);
            col.usedByEffector = true;
            var eff = go.AddComponent<PlatformEffector2D>();
            eff.useOneWay = true;
            eff.surfaceArc = 160f;
            MakeSprite(go.transform, "tile_platform", Vector3.zero, new Vector3(p.w, 0.5f, 1f), 0);
        }

        // ---- spawns ----

        public void SpawnEnemy(MonsterDef mon, float x)
        {
            var go = new GameObject("Enemy_" + mon.id);
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(x, 1.5f, 0);
            go.AddComponent<EnemyController>().Init(mon, this, x);
        }

        public void ScheduleRespawn(MonsterDef mon, float x)
        {
            StartCoroutine(RespawnAfter(mon, x, mon.isBoss ? 10f : 3f));
        }

        IEnumerator RespawnAfter(MonsterDef mon, float x, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (this != null && gameObject != null) SpawnEnemy(mon, x);
        }

        void SpawnNode(NodeDef node, float x)
        {
            var go = new GameObject("Node_" + node.id);
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(x, 0.7f, 0);
            go.AddComponent<GatherNode>().Init(node);
        }

        void SpawnStation(StationSpec s)
        {
            var go = new GameObject("Station_" + s.kind);
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(s.x, 0.8f, 0);
            go.AddComponent<StationObject>().Init(s.kind);
        }

        void SpawnPortal(PortalSpec p, CharacterState ch)
        {
            var go = new GameObject("Portal_" + p.targetMapId);
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(Mathf.Clamp(p.x, 1f, Def.length - 1f), 1.0f, 0);
            go.AddComponent<PortalObject>().Init(p, ch);
        }

        // ---- camera ----

        void SetupCamera(MapDef def)
        {
            _cam = Camera.main;
            if (_cam == null)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                _cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }
            _cam.orthographic = true;
            _cam.orthographicSize = 5f;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            if (ColorUtility.TryParseHtmlString(def.bgHex, out var bg)) _cam.backgroundColor = bg;
            _camHalfWidth = _cam.orthographicSize * _cam.aspect;
        }

        void LateUpdate()
        {
            if (_cam == null || Player == null) return;
            _camHalfWidth = _cam.orthographicSize * _cam.aspect;
            Vector3 p = Player.transform.position;
            float x = Def.length <= _camHalfWidth * 2
                ? Def.length / 2f
                : Mathf.Clamp(p.x, _camHalfWidth, Def.length - _camHalfWidth);
            float y = Mathf.Max(3.4f, p.y + 0.5f);
            _cam.transform.position = new Vector3(x, y, -10f);
        }
    }
}

using System;
using System.Collections.Generic;
using Tier9.UI;
using UnityEngine;

namespace Tier9.World
{
    /// <summary>
    /// Plays sliced Pixel Adventure frames (Resources/Sprites/Anim/&lt;spriteId&gt;__&lt;clip&gt;__NN)
    /// on a SpriteRenderer, driven by a locomotion state. Falls back to the character's static
    /// sprite when no animation frames exist (e.g. a user-added player_* look).
    /// </summary>
    public class CharacterAnimator : MonoBehaviour
    {
        const float Fps = 12f;

        // Loaded once per session: spriteId -> clip name -> ordered frames.
        static Dictionary<string, Dictionary<string, List<Sprite>>> _library;

        SpriteRenderer _sr;
        Dictionary<string, List<Sprite>> _clips;
        string _state = "idle";
        float _t;

        static void EnsureLibrary()
        {
            if (_library != null) return;
            _library = new Dictionary<string, Dictionary<string, List<Sprite>>>();
            foreach (var sprite in Resources.LoadAll<Sprite>("Sprites/Anim"))
            {
                var parts = sprite.name.Split(new[] { "__" }, StringSplitOptions.None);
                if (parts.Length < 3) continue;
                string id = parts[0], clip = parts[1];
                if (!_library.TryGetValue(id, out var byClip))
                    _library[id] = byClip = new Dictionary<string, List<Sprite>>();
                if (!byClip.TryGetValue(clip, out var frames))
                    byClip[clip] = frames = new List<Sprite>();
                frames.Add(sprite);
            }
            foreach (var byClip in _library.Values)
                foreach (var frames in byClip.Values)
                    frames.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        }

        public void Setup(SpriteRenderer sr, string spriteId)
        {
            _sr = sr;
            EnsureLibrary();
            _library.TryGetValue(spriteId, out _clips);
            if (_clips == null || _clips.Count == 0)
            {
                // No animation frames for this look — show its static sprite instead.
                _clips = new Dictionary<string, List<Sprite>> { ["idle"] = new List<Sprite> { SpriteLibrary.Get(spriteId) } };
            }
            _state = "idle";
            _t = 0f;
        }

        public void SetState(string state)
        {
            if (_clips == null || state == _state || !_clips.ContainsKey(state)) return;
            _state = state;
            _t = 0f;
        }

        void LateUpdate()
        {
            if (_sr == null || _clips == null) return;
            if ((!_clips.TryGetValue(_state, out var frames) || frames.Count == 0) &&
                (!_clips.TryGetValue("idle", out frames) || frames.Count == 0))
                return;
            _t += Time.deltaTime * Fps;
            _sr.sprite = frames[(int)_t % frames.Count];
        }
    }
}

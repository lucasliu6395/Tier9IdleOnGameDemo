using System;
using System.Collections.Generic;
using Tier9.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tier9.UI
{
    /// <summary>
    /// A screen builds its element tree once per "structure" change and updates
    /// dynamic labels/buttons in place every tick, so scroll position and pressed
    /// buttons survive the 1-second simulation refresh.
    /// </summary>
    public abstract class ScreenBase
    {
        protected GameManager Gm => GameManager.I;
        protected AccountState Account => Gm.Account;
        protected CharacterState Ch => Gm.Account.Selected;

        VisualElement _host;
        string _structureKey;
        readonly List<Action> _updaters = new List<Action>();

        public void Attach(VisualElement host)
        {
            _host = host;
            _structureKey = null;
            Refresh();
        }

        public void Detach()
        {
            _host = null;
            _updaters.Clear();
        }

        public void Refresh()
        {
            if (_host == null) return;
            string key = StructureKey();
            if (key != _structureKey)
            {
                _structureKey = key;
                _updaters.Clear();
                _host.Clear();
                Build(_host);
            }
            foreach (var u in _updaters) u();
        }

        /// <summary>Register an in-place updater; it also runs immediately.</summary>
        protected void Bind(Action update)
        {
            _updaters.Add(update);
            update();
        }

        protected abstract string StructureKey();
        protected abstract void Build(VisualElement host);

        // ---- small element helpers shared by screens ----

        protected static VisualElement Row(params VisualElement[] children)
        {
            var row = new VisualElement();
            row.AddToClassList("row");
            foreach (var c in children) row.Add(c);
            return row;
        }

        protected static Label Text(string text, params string[] classes)
        {
            var l = new Label(text);
            foreach (var c in classes) l.AddToClassList(c);
            return l;
        }

        protected static VisualElement Icon(string spriteId, string sizeClass)
        {
            var el = new VisualElement();
            el.AddToClassList(sizeClass);
            el.AddToClassList("icon");
            el.style.backgroundImage = new StyleBackground(SpriteLibrary.Get(spriteId));
            return el;
        }

        protected static VisualElement Card(string title = null)
        {
            var card = new VisualElement();
            card.AddToClassList("card");
            if (!string.IsNullOrEmpty(title)) card.Add(Text(title, "card-title"));
            return card;
        }

        protected static VisualElement Spacer()
        {
            var s = new VisualElement();
            s.AddToClassList("grow");
            return s;
        }

        /// <summary>A labeled progress bar; returns the fill element for width updates.</summary>
        protected static (VisualElement bar, VisualElement fill, Label label) Bar(string barClass)
        {
            var bar = new VisualElement();
            bar.AddToClassList("bar");
            var fill = new VisualElement();
            fill.AddToClassList("bar-fill");
            if (!string.IsNullOrEmpty(barClass)) fill.AddToClassList(barClass);
            var label = new Label("");
            label.AddToClassList("bar-label");
            bar.Add(fill);
            bar.Add(label);
            return (bar, fill, label);
        }
    }
}

using System.Collections.Generic;
using Tier9.Content;
using Tier9.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tier9.UI
{
    public class UiRoot : MonoBehaviour
    {
        UIDocument _doc;
        VisualElement _root;
        VisualElement _screenHost;
        VisualElement _overlay;
        VisualElement _toastHost;
        VisualElement _chrome;

        Label _coinsLabel;
        VisualElement _charBar;
        VisualElement _tabBar;

        readonly Dictionary<string, ScreenBase> _screens = new Dictionary<string, ScreenBase>();
        readonly List<(string id, string label)> _tabs = new List<(string, string)>
        {
            ("world", "World"),
            ("skills", "Skills"),
            ("town", "Town"),
            ("character", "Character"),
        };
        string _activeTab = "world";
        string _chromeKey;
        bool _createDialogOpen;

        void Start()
        {
            _doc = gameObject.AddComponent<UIDocument>();
            var ps = Resources.Load<PanelSettings>("UI/GamePanelSettings");
            if (ps == null)
            {
                Debug.LogWarning("[Tier9] PanelSettings asset missing; open the project in the editor once to generate it.");
                ps = ScriptableObject.CreateInstance<PanelSettings>();
            }
            ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            ps.referenceResolution = new Vector2Int(1280, 720);
            _doc.panelSettings = ps;

            _root = _doc.rootVisualElement;
            var sheet = Resources.Load<StyleSheet>("UI/game");
            if (sheet != null) _root.styleSheets.Add(sheet);
            _root.AddToClassList("app-root");

            _screens["world"] = new WorldScreen();
            _screens["skills"] = new SkillsScreen();
            _screens["town"] = new TownScreen();
            _screens["character"] = new CharacterScreen();

            BuildChrome();

            GameManager.I.StateChanged += OnStateChanged;
            GameManager.I.AfkCompleted += ShowAfkPopup;
            GameManager.I.Toast += ShowToast;
            OnStateChanged();
        }

        void OnDestroy()
        {
            if (GameManager.I == null) return;
            GameManager.I.StateChanged -= OnStateChanged;
            GameManager.I.AfkCompleted -= ShowAfkPopup;
            GameManager.I.Toast -= ShowToast;
        }

        void BuildChrome()
        {
            _root.Clear();
            _chrome = new VisualElement();
            _chrome.AddToClassList("chrome");

            // Top bar
            var top = new VisualElement();
            top.AddToClassList("top-bar");
            var title = new Label("TIER9 IDLE");
            title.AddToClassList("game-title");
            _coinsLabel = new Label("");
            _coinsLabel.AddToClassList("coins");
            var devBtn = new Button(ToggleDebugPanel) { text = "DEV" };
            devBtn.AddToClassList("dev-btn");
            var spacer = new VisualElement();
            spacer.AddToClassList("grow");
            top.Add(title);
            top.Add(_coinsLabel);
            top.Add(spacer);
            top.Add(devBtn);

            _tabBar = new VisualElement();
            _tabBar.AddToClassList("tab-bar");

            _screenHost = new VisualElement();
            _screenHost.AddToClassList("screen-host");

            _charBar = new VisualElement();
            _charBar.AddToClassList("char-bar");

            _chrome.Add(top);
            _chrome.Add(_tabBar);
            _chrome.Add(_screenHost);
            _chrome.Add(_charBar);

            _toastHost = new VisualElement();
            _toastHost.AddToClassList("toast-host");
            _toastHost.pickingMode = PickingMode.Ignore;

            _overlay = new VisualElement();
            _overlay.AddToClassList("overlay");
            _overlay.style.display = DisplayStyle.None;

            _root.Add(_chrome);
            _root.Add(_toastHost);
            _root.Add(_overlay);
        }

        void OnStateChanged()
        {
            var acc = GameManager.I.Account;

            if (acc.characters.Count == 0)
            {
                if (!_createDialogOpen) ShowCreateCharacterDialog(firstCharacter: true);
                return;
            }

            _coinsLabel.text = $"🪙 {Fmt.N(acc.coins)}";

            string chromeKey = $"{acc.characters.Count}:{acc.selectedCharacter}:{_activeTab}";
            if (chromeKey != _chromeKey)
            {
                _chromeKey = chromeKey;
                RebuildTabBar();
                RebuildCharBar();
                _screens[_activeTab].Attach(_screenHost);
            }
            else
            {
                UpdateCharBarLabels();
                _screens[_activeTab].Refresh();
            }
        }

        void RebuildTabBar()
        {
            _tabBar.Clear();
            foreach (var (id, label) in _tabs)
            {
                string tabId = id;
                var btn = new Button(() => SwitchTab(tabId)) { text = label };
                btn.AddToClassList("tab-btn");
                if (tabId == _activeTab) btn.AddToClassList("tab-btn--active");
                _tabBar.Add(btn);
            }
        }

        readonly List<Label> _charSlotLabels = new List<Label>();

        void RebuildCharBar()
        {
            _charBar.Clear();
            _charSlotLabels.Clear();
            var acc = GameManager.I.Account;
            for (int i = 0; i < AccountState.MaxCharacters; i++)
            {
                int index = i;
                if (index < acc.characters.Count)
                {
                    var ch = acc.characters[index];
                    var btn = new Button(() => { GameManager.I.SelectCharacter(index); });
                    btn.AddToClassList("char-slot");
                    if (index == acc.selectedCharacter) btn.AddToClassList("char-slot--active");
                    var icon = new VisualElement();
                    icon.AddToClassList("icon");
                    icon.AddToClassList("icon-s");
                    icon.style.backgroundImage = new StyleBackground(SpriteLibrary.Get("class_" + ch.classId));
                    var label = new Label("");
                    label.AddToClassList("char-slot-label");
                    btn.Add(icon);
                    btn.Add(label);
                    _charSlotLabels.Add(label);
                    _charBar.Add(btn);
                }
                else
                {
                    var btn = new Button(() => ShowCreateCharacterDialog(firstCharacter: false)) { text = "+ New Character" };
                    btn.AddToClassList("char-slot");
                    btn.AddToClassList("char-slot--empty");
                    _charSlotLabels.Add(null);
                    _charBar.Add(btn);
                }
            }
            UpdateCharBarLabels();
        }

        void UpdateCharBarLabels()
        {
            var acc = GameManager.I.Account;
            for (int i = 0; i < _charSlotLabels.Count && i < acc.characters.Count; i++)
            {
                if (_charSlotLabels[i] == null) continue;
                var ch = acc.characters[i];
                _charSlotLabels[i].text = $"{ch.name}  Lv {ch.level}\n{TaskSummary(ch)}";
            }
        }

        public static string TaskSummary(CharacterState ch)
        {
            switch (ch.task)
            {
                case TaskType.Combat:
                    var zone = ContentDatabase.Zone(ch.taskTargetId);
                    return zone == null ? "Idle" : $"⚔ {zone.name}";
                case TaskType.Mining:
                    var node = ContentDatabase.Node(ch.taskTargetId);
                    return node == null ? "Idle" : $"⛏ {node.name}";
                case TaskType.Choppin:
                    var node2 = ContentDatabase.Node(ch.taskTargetId);
                    return node2 == null ? "Idle" : $"🪓 {node2.name}";
                default:
                    return "🏠 In Town";
            }
        }

        void SwitchTab(string tabId)
        {
            if (_activeTab == tabId) return;
            _screens[_activeTab].Detach();
            _activeTab = tabId;
            OnStateChanged();
        }

        // ---- Overlay / popups ----

        void OpenOverlay(VisualElement content)
        {
            _overlay.Clear();
            _overlay.style.display = DisplayStyle.Flex;
            _overlay.Add(content);
        }

        void CloseOverlay()
        {
            _overlay.Clear();
            _overlay.style.display = DisplayStyle.None;
            _createDialogOpen = false;
        }

        void ShowAfkPopup(AfkReport report)
        {
            var popup = new VisualElement();
            popup.AddToClassList("popup");

            popup.Add(new Label("Welcome back!") { name = "afk-title" });
            popup.ElementAt(0).AddToClassList("popup-title");
            popup.Add(new Label($"Your characters worked for {Fmt.Time(report.seconds)}"));

            var scroll = new ScrollView();
            scroll.AddToClassList("popup-scroll");
            foreach (var entry in report.entries)
            {
                var card = new VisualElement();
                card.AddToClassList("card");
                card.Add(new Label(entry.characterName) { style = { unityFontStyleAndWeight = FontStyle.Bold } });
                var r = entry.result;
                if (r.IsEmpty)
                {
                    card.Add(new Label("Rested in town."));
                }
                else
                {
                    if (r.kills > 0) card.Add(new Label($"⚔ {Fmt.N(r.kills)} kills"));
                    if (r.classXp > 0) card.Add(new Label($"✨ {Fmt.N(r.classXp)} class XP" + (r.classLevelsGained > 0 ? $"  (+{r.classLevelsGained} levels!)" : "")));
                    if (r.skillXp > 0) card.Add(new Label($"🛠 {Fmt.N(r.skillXp)} skill XP" + (r.skillLevelsGained > 0 ? $"  (+{r.skillLevelsGained} levels!)" : "")));
                    if (r.coins > 0) card.Add(new Label($"🪙 {Fmt.N(r.coins)} coins"));
                    foreach (var item in r.items)
                    {
                        var def = ContentDatabase.Item(item.itemId);
                        var row = new VisualElement();
                        row.AddToClassList("row");
                        var icon = new VisualElement();
                        icon.AddToClassList("icon");
                        icon.AddToClassList("icon-s");
                        icon.style.backgroundImage = new StyleBackground(SpriteLibrary.Get(item.itemId));
                        row.Add(icon);
                        row.Add(new Label($"{def?.name ?? item.itemId} x{Fmt.N(item.count)}"));
                        card.Add(row);
                    }
                }
                scroll.Add(card);
            }
            popup.Add(scroll);

            var claim = new Button(CloseOverlay) { text = "Claim!" };
            claim.AddToClassList("btn-primary");
            popup.Add(claim);
            OpenOverlay(popup);
        }

        void ShowCreateCharacterDialog(bool firstCharacter)
        {
            _createDialogOpen = true;
            var popup = new VisualElement();
            popup.AddToClassList("popup");
            var title = new Label(firstCharacter ? "Create your first character" : "Create a character");
            title.AddToClassList("popup-title");
            popup.Add(title);
            popup.Add(new Label("New characters start as Beginners and can pick a class at level 5."));

            var nameField = new TextField("Name") { value = "Hero" };
            popup.Add(nameField);

            var buttons = new VisualElement();
            buttons.AddToClassList("row");
            var create = new Button(() =>
            {
                GameManager.I.CreateCharacter(nameField.value);
                CloseOverlay();
            })
            { text = "Create" };
            create.AddToClassList("btn-primary");
            buttons.Add(create);
            if (!firstCharacter)
            {
                buttons.Add(new Button(CloseOverlay) { text = "Cancel" });
            }
            popup.Add(buttons);
            OpenOverlay(popup);
        }

        bool _debugOpen;

        void ToggleDebugPanel()
        {
            if (_debugOpen)
            {
                CloseOverlay();
                _debugOpen = false;
                return;
            }
            _debugOpen = true;
            var popup = new VisualElement();
            popup.AddToClassList("popup");
            var title = new Label("Dev Tools");
            title.AddToClassList("popup-title");
            popup.Add(title);
            popup.Add(new Label("Time skips run the same AFK simulation used offline."));

            popup.Add(new Button(() => GameManager.I.RunAfk(60, notify: true)) { text = "⏩ Skip 1 minute" });
            popup.Add(new Button(() => GameManager.I.RunAfk(3600, notify: true)) { text = "⏩ Skip 1 hour" });
            popup.Add(new Button(() => GameManager.I.RunAfk(8 * 3600, notify: true)) { text = "⏩ Skip 8 hours" });
            popup.Add(new Button(() =>
            {
                GameManager.I.Account.coins += 1000;
                GameManager.I.NotifyChanged();
            })
            { text = "🪙 +1000 coins" });

            var reset = new Button(() =>
            {
                GameManager.I.ResetSave();
                _debugOpen = false;
                CloseOverlay();
            })
            { text = "🗑 Reset save (immediate!)" };
            reset.AddToClassList("btn-danger");
            popup.Add(reset);

            var close = new Button(() =>
            {
                _debugOpen = false;
                CloseOverlay();
            })
            { text = "Close" };
            close.AddToClassList("btn-primary");
            popup.Add(close);
            OpenOverlay(popup);
        }

        void ShowToast(string message)
        {
            var toast = new Label(message);
            toast.AddToClassList("toast");
            _toastHost.Add(toast);
            toast.schedule.Execute(() => toast.RemoveFromHierarchy()).StartingIn(2800);
            while (_toastHost.childCount > 4) _toastHost.RemoveAt(0);
        }
    }
}

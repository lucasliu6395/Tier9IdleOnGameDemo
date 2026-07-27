using System.Collections.Generic;
using Tier9.Content;
using Tier9.Core;
using Tier9.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Tier9.UI
{
    /// <summary>
    /// Transparent UI shell over the walkable world: top bar + tab bar + char bar chrome,
    /// a gameplay HUD, and the four game screens as dismissable overlay menus.
    /// </summary>
    public class UiRoot : MonoBehaviour
    {
        public static UiRoot I { get; private set; }

        UIDocument _doc;
        VisualElement _root;
        VisualElement _menuLayer;
        VisualElement _menuHost;
        Label _menuTitle;
        VisualElement _popupLayer;
        VisualElement _toastHost;

        Label _coinsLabel;
        VisualElement _charBar;
        VisualElement _tabBar;

        // HUD
        VisualElement _hud;
        VisualElement _hpFill;
        Label _hpLabel;
        Label _mapLabel;
        Label _sessionLabel;

        readonly Dictionary<string, ScreenBase> _screens = new Dictionary<string, ScreenBase>();
        readonly List<(string id, string label)> _tabs = new List<(string, string)>
        {
            ("world", "World"),
            ("skills", "Skills"),
            ("town", "Town"),
            ("items", "Items"),
            ("character", "Character"),
        };
        string _openMenu;
        string _charBarKey;
        bool _createDialogOpen;

        void Start()
        {
            I = this;
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
            _screens["items"] = new InventoryScreen();
            _screens["character"] = new CharacterScreen();

            BuildChrome();

            GameManager.I.StateChanged += OnStateChanged;
            GameManager.I.AfkCompleted += ShowAfkPopup;
            GameManager.I.Toast += ShowToast;
            OnStateChanged();

            _root.schedule.Execute(UpdateHud).Every(120);
        }

        void OnDestroy()
        {
            if (GameManager.I == null) return;
            GameManager.I.StateChanged -= OnStateChanged;
            GameManager.I.AfkCompleted -= ShowAfkPopup;
            GameManager.I.Toast -= ShowToast;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                if (_openMenu != null) CloseMenu();
            }
        }

        void BuildChrome()
        {
            _root.Clear();

            var chrome = new VisualElement();
            chrome.AddToClassList("chrome");
            chrome.pickingMode = PickingMode.Ignore;

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
            foreach (var (id, label) in _tabs)
            {
                string tabId = id;
                var btn = new Button(() => ToggleMenu(tabId)) { text = label };
                btn.AddToClassList("tab-btn");
                _tabBar.Add(btn);
            }

            // Transparent middle: world shows through; HUD floats here
            var middle = new VisualElement();
            middle.AddToClassList("world-middle");
            middle.pickingMode = PickingMode.Ignore;

            _hud = new VisualElement();
            _hud.AddToClassList("hud");
            _hud.pickingMode = PickingMode.Ignore;
            _mapLabel = new Label("");
            _mapLabel.AddToClassList("hud-map");
            var hpBar = new VisualElement();
            hpBar.AddToClassList("bar");
            hpBar.AddToClassList("hud-hp");
            _hpFill = new VisualElement();
            _hpFill.AddToClassList("bar-fill");
            _hpFill.AddToClassList("bar-fill--hp");
            _hpLabel = new Label("");
            _hpLabel.AddToClassList("bar-label");
            hpBar.Add(_hpFill);
            hpBar.Add(_hpLabel);
            _sessionLabel = new Label("");
            _sessionLabel.AddToClassList("hud-session");
            _hud.Add(_mapLabel);
            _hud.Add(hpBar);
            _hud.Add(_sessionLabel);

            var hint = new Label("A/D move   ·   Space jump   ·   F attack/gather   ·   E interact   ·   Esc close menu");
            hint.AddToClassList("hud-hint");
            hint.pickingMode = PickingMode.Ignore;

            middle.Add(_hud);
            middle.Add(hint);

            _charBar = new VisualElement();
            _charBar.AddToClassList("char-bar");

            chrome.Add(top);
            chrome.Add(_tabBar);
            chrome.Add(middle);
            chrome.Add(_charBar);

            // Menu overlay (screens)
            _menuLayer = new VisualElement();
            _menuLayer.AddToClassList("overlay");
            _menuLayer.style.display = DisplayStyle.None;
            var menuPanel = new VisualElement();
            menuPanel.AddToClassList("menu-panel");
            var header = new VisualElement();
            header.AddToClassList("row");
            _menuTitle = new Label("");
            _menuTitle.AddToClassList("popup-title");
            var headerSpacer = new VisualElement();
            headerSpacer.AddToClassList("grow");
            var close = new Button(CloseMenu) { text = "✕" };
            close.AddToClassList("menu-close");
            header.Add(_menuTitle);
            header.Add(headerSpacer);
            header.Add(close);
            _menuHost = new VisualElement();
            _menuHost.AddToClassList("menu-host");
            menuPanel.Add(header);
            menuPanel.Add(_menuHost);
            _menuLayer.Add(menuPanel);

            _toastHost = new VisualElement();
            _toastHost.AddToClassList("toast-host");
            _toastHost.pickingMode = PickingMode.Ignore;

            _popupLayer = new VisualElement();
            _popupLayer.AddToClassList("overlay");
            _popupLayer.style.display = DisplayStyle.None;

            _root.Add(chrome);
            _root.Add(_menuLayer);
            _root.Add(_toastHost);
            _root.Add(_popupLayer);
        }

        // ---- menus ----

        public void ToggleMenu(string tabId)
        {
            if (_openMenu == tabId) CloseMenu();
            else OpenMenu(tabId);
        }

        public void OpenMenu(string tabId)
        {
            if (!_screens.ContainsKey(tabId)) return;
            if (_openMenu != null) _screens[_openMenu].Detach();
            _openMenu = tabId;
            foreach (var (id, label) in _tabs)
                if (id == tabId) _menuTitle.text = label;
            _menuLayer.style.display = DisplayStyle.Flex;
            _menuHost.Clear();
            _screens[tabId].Attach(_menuHost);
            RefreshTabHighlight();
        }

        public void CloseMenu()
        {
            if (_openMenu != null) _screens[_openMenu].Detach();
            _openMenu = null;
            _menuHost.Clear();
            _menuLayer.style.display = DisplayStyle.None;
            RefreshTabHighlight();
        }

        public void OpenTownStation(string kind)
        {
            if (_screens["town"] is TownScreen town) town.SetSubTab(kind);
            OpenMenu("town");
        }

        void RefreshTabHighlight()
        {
            int i = 0;
            foreach (var child in _tabBar.Children())
            {
                bool active = _openMenu == _tabs[i].id;
                child.EnableInClassList("tab-btn--active", active);
                i++;
            }
        }

        // ---- state sync ----

        void OnStateChanged()
        {
            var acc = GameManager.I.Account;

            if (acc.characters.Count == 0)
            {
                if (!_createDialogOpen) ShowCreateCharacterDialog(firstCharacter: true);
                return;
            }

            _coinsLabel.text = $"🪙 {Fmt.N(acc.coins)}";

            string key = $"{acc.characters.Count}:{acc.selectedCharacter}:{acc.Selected?.spriteId}";
            if (key != _charBarKey)
            {
                _charBarKey = key;
                RebuildCharBar();
            }
            else
            {
                UpdateCharBarLabels();
            }

            if (_openMenu != null) _screens[_openMenu].Refresh();
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
                    var btn = new Button(() => GameManager.I.SelectCharacter(index));
                    btn.AddToClassList("char-slot");
                    if (index == acc.selectedCharacter) btn.AddToClassList("char-slot--active");
                    var icon = new VisualElement();
                    icon.AddToClassList("icon");
                    icon.AddToClassList("icon-s");
                    string spriteId = string.IsNullOrEmpty(ch.spriteId) ? "class_" + ch.classId : ch.spriteId;
                    icon.style.backgroundImage = new StyleBackground(SpriteLibrary.Get(spriteId));
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
                string live = i == acc.selectedCharacter ? "🎮 " : "";
                _charSlotLabels[i].text = $"{ch.name}  Lv {ch.level}\n{live}{TaskSummary(ch)}";
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

        // ---- HUD ----

        void UpdateHud()
        {
            var runner = WorldRunner.I;
            var player = runner != null ? runner.Player : null;
            if (player == null || runner.CurrentMap == null)
            {
                _hud.style.display = DisplayStyle.None;
                return;
            }
            _hud.style.display = DisplayStyle.Flex;
            _mapLabel.text = runner.CurrentMap.name;

            float frac = Mathf.Clamp01((float)(player.CurrentHp / System.Math.Max(1.0, player.MaxHp)));
            _hpFill.style.width = Length.Percent(frac * 100f);
            _hpLabel.text = $"❤ {Fmt.N(player.CurrentHp)} / {Fmt.N(player.MaxHp)}";

            var s = runner.Session;
            string session = "";
            if (s.kills > 0) session += $"⚔ {Fmt.N(s.kills)}   ";
            if (s.classXp > 0) session += $"✨ {Fmt.N(s.classXp)} XP   ";
            if (s.skillXp > 0) session += $"🛠 {Fmt.N(s.skillXp)} XP   ";
            if (s.coins > 0) session += $"🪙 {Fmt.N(s.coins)}";
            _sessionLabel.text = session.Length == 0 ? "" : "This visit:   " + session;
        }

        // ---- popups ----

        void OpenPopup(VisualElement content)
        {
            _popupLayer.Clear();
            _popupLayer.style.display = DisplayStyle.Flex;
            _popupLayer.Add(content);
        }

        void ClosePopup()
        {
            _popupLayer.Clear();
            _popupLayer.style.display = DisplayStyle.None;
            _createDialogOpen = false;
        }

        void ShowAfkPopup(AfkReport report)
        {
            var popup = new VisualElement();
            popup.AddToClassList("popup");

            var title = new Label("Welcome back!");
            title.AddToClassList("popup-title");
            popup.Add(title);
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

            var claim = new Button(ClosePopup) { text = "Claim!" };
            claim.AddToClassList("btn-primary");
            popup.Add(claim);
            OpenPopup(popup);
        }

        void ShowCreateCharacterDialog(bool firstCharacter)
        {
            _createDialogOpen = true;
            var popup = new VisualElement();
            popup.AddToClassList("popup");
            var title = new Label(firstCharacter ? "Create your first character" : "Create a character");
            title.AddToClassList("popup-title");
            popup.Add(title);
            popup.Add(new Label("Beginners can pick a class at level 5. Drop player_*.png files into Resources/Sprites to add your own looks."));

            var nameField = new TextField("Name") { value = "Hero" };
            popup.Add(nameField);

            popup.Add(new Label("Appearance:"));
            var spriteIds = SpriteLibrary.PlayerSpriteIds();
            string selectedSprite = spriteIds.Count > 0 ? spriteIds[0] : "";
            var pickRow = new VisualElement();
            pickRow.AddToClassList("row");
            var pickButtons = new List<Button>();
            foreach (var spriteId in spriteIds)
            {
                string id = spriteId;
                Button pick = null;
                pick = new Button(() =>
                {
                    selectedSprite = id;
                    foreach (var b in pickButtons) b.EnableInClassList("sprite-pick--active", b == pick);
                });
                pick.AddToClassList("sprite-pick");
                pick.style.backgroundImage = new StyleBackground(SpriteLibrary.Get(id));
                pickButtons.Add(pick);
                pickRow.Add(pick);
            }
            if (pickButtons.Count > 0) pickButtons[0].AddToClassList("sprite-pick--active");
            popup.Add(pickRow);

            var buttons = new VisualElement();
            buttons.AddToClassList("row");
            var create = new Button(() =>
            {
                GameManager.I.CreateCharacter(nameField.value, selectedSprite);
                ClosePopup();
            })
            { text = "Create" };
            create.AddToClassList("btn-primary");
            buttons.Add(create);
            if (!firstCharacter)
            {
                buttons.Add(new Button(ClosePopup) { text = "Cancel" });
            }
            popup.Add(buttons);
            OpenPopup(popup);
        }

        bool _debugOpen;

        void ToggleDebugPanel()
        {
            if (_debugOpen)
            {
                ClosePopup();
                _debugOpen = false;
                return;
            }
            _debugOpen = true;
            var popup = new VisualElement();
            popup.AddToClassList("popup");
            var title = new Label("Dev Tools");
            title.AddToClassList("popup-title");
            popup.Add(title);
            popup.Add(new Label("Time skips run the same AFK simulation used offline (all characters, including you)."));

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
                ClosePopup();
                if (WorldRunner.I != null) WorldRunner.I.Rebuild();
            })
            { text = "🗑 Reset save (immediate!)" };
            reset.AddToClassList("btn-danger");
            popup.Add(reset);

            var close = new Button(() =>
            {
                _debugOpen = false;
                ClosePopup();
            })
            { text = "Close" };
            close.AddToClassList("btn-primary");
            popup.Add(close);
            OpenPopup(popup);
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

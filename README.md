# Tier9 Idle — an IdleOn-style demo

A small, self-contained Unity demo inspired by *Legends of Idleon*: walk your
2D character through **side-scrolling platform maps** with roaming monsters,
fight with manual attacks, mine and chop at resource nodes, and hop between
maps through portals. Up to **3 characters** — whoever you're not controlling
keeps earning via AFK simulation in real time, **including while the game is
closed**. Spend the haul in town on crafting, shop gear, and account-wide
**stamp** upgrades.

**Controls:** A/D or ←/→ move · Space/W jump · **F attack** · **E interact**
(portals, town stations, resource nodes) · Esc closes menus.

All mechanics, names, and content are original; no IdleOn assets are used.

## Requirements

- Unity **6000.5.3f1** (any Unity 6 version should work)

## Run it

1. Open the project in Unity Hub / the Unity editor.
2. Press **Play**. Any scene works — the game bootstraps itself at runtime
   (a `Assets/Scenes/Main.unity` is auto-created for convenience).
3. Create your first character and go fight some Puffshrooms.

Everything is built at runtime from code — there are no prefabs or scene wiring
to maintain.

## The demo loop

| System | What it does |
| --- | --- |
| **The world** | 10 walkable maps: Spore Town (hub with Anvil/Shop/Stamps stations), 4 combat zones, the Glowcap Lair boss arena, and 4 skilling spots — all connected by portals. Where your character stands *is* their AFK task. |
| **Active combat** | Attack with F; kills grant class XP instantly, and pop out **physical coin/item drops** that fall, settle, and get magnet-pulled into you as you approach — no menu required. Rewards are earned at **full rate** (AFK earns 60%). Basic mobs are harmless to touch; bigger ones (Pebblits, Glowcaps) deal contact damage, and the **Mother Glowcap** boss chases you. Kill quotas unlock the next zone's portal per character. |
| **Skilling** | Face a mining/choppin node and press F, just like attacking — each connecting swing yields one resource on the same ~0.45s cooldown as combat, with a `+1 Ore`/`+1 Log`-style floaty text. Deeper nodes need higher skill levels. |
| **Characters** | 3 slots; you control one while the others AFK at their own locations simultaneously. Pick a name + appearance at creation; Beginners choose Warrior / Archer / Mage at level 5. 1 talent point per level. |
| **Town** | Anvil (craft gear from materials), Shop (buy basics, sell loot), Stamps (permanent account-wide % bonuses with scaling costs) — walk up and press E, or use the Town menu. |
| **Offline gains** | On launch, elapsed real time (up to 24h) runs through the *same* simulation code as AFK play, then a claim popup summarizes what everyone earned. |

Tip: the **DEV** button (top-right) opens dev tools with time skips (+1m/+1h/+8h)
so you can demo AFK progression instantly, plus a save reset.

## Adding your own art

Drop images into `Assets/Resources/Sprites/`, named after the content id
(e.g. `puffshroom.png`). They import as sprites automatically and replace the
generated placeholder squares.

**See [SPRITE_KIT.md](SPRITE_KIT.md)** for a curated set of free, cohesive art
packs (from itch.io) and an exact id-by-id rename map to drop them in.

Ids:

- **Monsters:** `puffshroom`, `hopper`, `pebblit`, `glowcap`, `mother_glowcap` (boss)
- **Player looks:** `player_a` … `player_d` (add more with any `player_*` name — they appear in character creation automatically)
- **World:** `tile_ground`, `tile_platform`, `portal`, `station_anvil`, `station_shop`, `station_stamps`, `coin_pickup`
- **Resource nodes:** `copper_vein`, `iron_vein`, `oak_tree`, `birch_tree`
- **Materials:** `puff_spore`, `hopper_leg`, `pebble_shard`, `glow_dust`, `copper_ore`, `iron_ore`, `oak_log`, `birch_log`
- **Weapons:** `stick`, `wooden_sword`, `copper_sword`, `iron_blade`
- **Armor:** `spore_vest`, `hopper_tunic`, `pebble_plate`
- **Tools:** `flint_pick`, `copper_pick`, `iron_pick`, `flint_axe`, `copper_axe`, `iron_axe`
- **Stamps:** `sword_stamp`, `pick_stamp`, `axe_stamp`, `clock_stamp`, `book_stamp`, `clover_stamp`
- **Class icons:** `class_beginner`, `class_warrior`, `class_archer`, `class_mage`

## Project layout

```
Assets/Scripts/
  Core/     GameManager (tick loop, actions), AccountState, SaveSystem, Fmt
  Sim/      AfkSimulator (one reward path for live + AFK + offline), StatCalculator, XpCurve
  Content/  ContentDatabase — ALL game content/balance lives here (incl. map layouts)
  World/    platformer layer: maps, player controller, enemies, portals, stations, nodes
  UI/       UI Toolkit chrome/HUD/menus, built from code + Resources/UI/game.uss
Assets/Tests/EditMode/   simulation + content integrity tests
Assets/Editor/           asset bootstrap (PanelSettings, URP, input actions, scene)
```

Balance tuning is all in `Assets/Scripts/Content/ContentDatabase.cs` (monsters,
zones, items, recipes, stamps, talents) and the formulas in
`Assets/Scripts/Sim/StatCalculator.cs` / `AfkSimulator.cs`.

## Saves

JSON at `%USERPROFILE%\AppData\LocalLow\DefaultCompany\Tier9IdleOnGameDemo\save.json`.
Autosaves every 20 seconds and on quit. Delete the file (or use DEV → Reset save)
to start over.

## Tests

Window → General → **Test Runner** → EditMode, or headless:

```
Unity.exe -batchmode -projectPath . -runTests -testPlatform EditMode -testResults results.xml
```

Tests cover the XP curve, combat/gather simulation, offline-vs-live consistency,
zone unlocks, stamp/talent/equipment stat effects, save roundtrips, and content
reference integrity.

## Credits

All game code, content, and mechanics are original. The bundled placeholder art
is generated at runtime.

If you install the recommended art from [SPRITE_KIT.md](SPRITE_KIT.md), honor
each pack's license. In particular:

- **Pixel Adventure 1 & 2** by Pixel Frog — CC0 (no attribution required).
- **Shikashi's Fantasy Icons Pack** by Matt Firth (shikashipx) — **CC-BY 4.0,
  attribution required**, incorporating art from game-icons.net. If you use it,
  keep this credit line.

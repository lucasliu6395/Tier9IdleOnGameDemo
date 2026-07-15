# Tier9 Idle — an IdleOn-style demo

A small, self-contained Unity demo inspired by *Legends of Idleon*: create up to
**3 characters**, assign each one to **AFK combat** or **gathering skills**, and
they keep earning XP, coins, and loot in real time — **including while the game
is closed**. Spend the haul in town on crafting, shop gear, and account-wide
**stamp** upgrades.

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
| **AFK combat** (World tab) | 4 zones with rising monster HP. Kill rate is derived from your damage; kills grant class XP, coins, and material drops. Kill quotas unlock the next zone per character. |
| **Skilling** (Skills tab) | Mining and Choppin nodes. Gather rate is derived from skill efficiency; higher skill levels and better tools mean faster gathering. |
| **Characters** | 3 slots, each character AFKs on its own task simultaneously. Beginners pick Warrior / Archer / Mage at level 5. 1 talent point per level. |
| **Town** | Anvil (craft gear from materials), Shop (buy basics, sell loot), Stamps (permanent account-wide % bonuses with scaling costs). |
| **Offline gains** | On launch, elapsed real time (up to 24h) runs through the *same* simulation as live play, then a claim popup summarizes what everyone earned. |

Tip: the **DEV** button (top-right) opens dev tools with time skips (+1m/+1h/+8h)
so you can demo AFK progression instantly, plus a save reset.

## Adding your own art

Drop images into `Assets/Resources/Sprites/`, named after the content id
(e.g. `puffshroom.png`). They import as sprites automatically and replace the
generated placeholder squares. Ids:

- **Monsters:** `puffshroom`, `hopper`, `pebblit`, `glowcap`
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
  Sim/      AfkSimulator (one code path for live + offline), StatCalculator, XpCurve
  Content/  ContentDatabase — ALL game content/balance lives here
  UI/       UI Toolkit screens, built from code + Resources/UI/game.uss
Assets/Tests/EditMode/   simulation tests
Assets/Editor/           asset bootstrap (PanelSettings), sprite import rules
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

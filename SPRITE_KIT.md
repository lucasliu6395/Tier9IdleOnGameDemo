# Sprite Drop-In Kit

This is a curated shopping list + rename map for giving Tier9 Idle real art, using
only free 2D assets from the approved sources. It is built around **one cohesive
world style + one cohesive icon style**.

> ✅ **Status: wired in.** Pixel Adventure 1 (players/rock enemy/terrain/portal) and
> Shikashi's Fantasy Icons (all items/tools/stamps/class icons/stations + mushroom
> monster, recolored cyan/purple for the glowcaps) have been sliced from their
> sheets into `Assets/Resources/Sprites/` at 64×64. Still on placeholders:
> **`hopper`** (no free frog sprite in these packs — drop a `hopper.png` to fill it).
> The tables below are kept as reference / for swapping art later.

## How this works

The game loads every sprite **by id** from `Assets/Resources/Sprites/`, with a
generated colored-square placeholder for anything missing (see
`Assets/Scripts/UI/SpriteLibrary.cs`). So the entire workflow is:

1. Download a recommended pack from its page (you have to do this — the stores are
   login/checkout-gated; I can't download for you).
2. Drop the individual PNGs into `Assets/Resources/Sprites/`.
3. **Rename each file to the matching id below**, e.g. `puffshroom.png`.
4. Unity auto-imports anything in that folder as a Sprite (handled by
   `Assets/Editor/Tier9EditorTools.cs`), so it just appears in-game.

You do **not** need to fill every id. Anything you skip stays a tidy colored
placeholder, so you can adopt art incrementally.

> ⚠️ **Verify each pack's license on its own page before shipping.** Licenses and
> prices can change, and a couple of "free" search hits turned out to be paid.
> The notes below reflect what each page said at time of research.

---

## Recommended packs

### 1. World art (characters, enemies, terrain) — one cohesive style
**Pixel Adventure 1 — by Pixel Frog** · **License: CC0** (no attribution
required) · **Free** (name-your-price, $0 is fine).
- Pixel Adventure 1: https://pixelfrog-assets.itch.io/pixel-adventure-1

Covers your **4 player looks, all 5 monsters + boss, and the ground/platform
tiles** in a single consistent pixel style — all from this one free pack. PA1
ships 4 playable characters (Ninja Frog, Mask Dude, Pink Man, Virtual Guy) and
~20 animated enemies (Mushroom, Plant, Slime, Bunny, Chicken, Rock/Rocky, etc.),
so you have plenty to map from. Use the **idle frame** of each animation as the
static sprite (or wire up animation later).

> **Note:** its sequel, *Pixel Adventure 2* (https://pixelfrog-assets.itch.io/pixel-adventure-2),
> is **paid** ($5 minimum) and only adds 20 more enemies. You don't need it — PA1
> alone covers this game. Grab it later only if you want extra enemy variety.

### 2. Inventory icons (materials, gear, tools, coins) — one cohesive style
**Shikashi's Fantasy Icons Pack — by shikashipx** · **License: CC-BY 4.0
(attribution REQUIRED)** · Free (name-your-price, $0 ok).
- https://shikashipx.itch.io/shikashis-fantasy-icons-pack

284 icons at 32×32 covering materials (wood/stone/ore/gold/gems), 28 weapons,
26 armor pieces, tools (pickaxe/shovel), and coin stacks — enough for the game's
**entire item + stamp + class-icon roster** from one set.

> **You must credit this pack if you use it.** Add this line to the game's
> credits / README:
> `Item icons: Shikashi's Fantasy Icons Pack by Matt Firth (shikashipx), CC-BY 4.0, incorporating art from game-icons.net`

### 3. (Optional) Nature & town props — fills the last gap
The two packs above don't include resource nodes (trees, ore veins) or town
stations (anvil, shop, stamps board). Grab one small CC0 nature/props pack to
match. Browse and pick one whose style sits closest to Pixel Frog:
- CC0 nature tag: https://itch.io/game-assets/assets-cc0/tag-nature
- e.g. "Nature Landscapes Free Pixel Art": https://free-game-assets.itch.io/nature-landscapes-free-pixel-art

If you'd rather not add a third pack, you can stand these in with icons from
Shikashi's set (it has ore, wood, and pickaxe icons) — they'll read fine as
small world objects — or just leave them as placeholders.

---

## Rename map (id → what to use)

### Players — from Pixel Adventure 1 (idle frame of each character)
| Rename to | Use |
|---|---|
| `player_a.png` | Ninja Frog |
| `player_b.png` | Mask Dude |
| `player_c.png` | Pink Man |
| `player_d.png` | Virtual Guy |

### Monsters + boss — from Pixel Adventure enemies
| Rename to | Suggested enemy | Notes |
|---|---|---|
| `puffshroom.png` | Mushroom | direct match |
| `hopper.png` | Bunny / Chicken | pick a small hopping enemy |
| `pebblit.png` | Rock / "Rocky" | rocky look |
| `glowcap.png` | Slime or a 2nd mushroom | tint bluish for "glow" |
| `mother_glowcap.png` | largest enemy you like | the game scales it up 2.6× as a boss |

### Class icons — reuse a representative sprite
| Rename to | Use |
|---|---|
| `class_beginner.png` | a player character face/idle |
| `class_warrior.png` | sword icon (Shikashi) |
| `class_archer.png` | bow icon (Shikashi) |
| `class_mage.png` | staff icon (Shikashi) |

### World tiles & objects
| Rename to | Source | Use |
|---|---|---|
| `tile_ground.png` | Pixel Adventure Terrain | a solid ground tile (tiles horizontally) |
| `tile_platform.png` | Pixel Adventure Terrain | a platform-edge tile |
| `portal.png` | Pixel Adventure | the End/Checkpoint flag or a door |
| `coin_pickup.png` | Shikashi | gold coin |
| `station_anvil.png` | props pack (#3) | anvil |
| `station_shop.png` | props pack (#3) | stall / sign |
| `station_stamps.png` | props pack (#3) | board / sign |

### Resource nodes — props pack (#3), or Shikashi stand-ins
| Rename to | Use |
|---|---|
| `copper_vein.png` | rock with copper flecks |
| `iron_vein.png` | rock with iron flecks |
| `oak_tree.png` | leafy tree |
| `birch_tree.png` | pale/birch tree |

### Materials — from Shikashi's Fantasy Icons
| Rename to | Suggested icon |
|---|---|
| `puff_spore.png` | spore / herb / feather |
| `hopper_leg.png` | meat / monster claw |
| `pebble_shard.png` | stone |
| `glow_dust.png` | gem / glowing dust |
| `copper_ore.png` | copper ore |
| `iron_ore.png` | iron ore |
| `oak_log.png` | wood / log |
| `birch_log.png` | wood / log (lighter) |

### Weapons — from Shikashi's Fantasy Icons
| Rename to | Suggested icon |
|---|---|
| `stick.png` | club / plain staff |
| `wooden_sword.png` | basic sword |
| `copper_sword.png` | mid sword |
| `iron_blade.png` | strong sword |

### Armor — from Shikashi's Fantasy Icons
| Rename to | Suggested icon |
|---|---|
| `spore_vest.png` | cloth tunic |
| `hopper_tunic.png` | leather tunic |
| `pebble_plate.png` | plate/metal chest |

### Tools — from Shikashi's Fantasy Icons
| Rename to | Suggested icon |
|---|---|
| `flint_pick.png` | pickaxe (basic) |
| `copper_pick.png` | pickaxe (mid) |
| `iron_pick.png` | pickaxe (strong) |
| `flint_axe.png` | axe (basic) |
| `copper_axe.png` | axe (mid) |
| `iron_axe.png` | axe (strong) |

### Stamps — from Shikashi's Fantasy Icons
| Rename to | Suggested icon |
|---|---|
| `sword_stamp.png` | sword |
| `pick_stamp.png` | pickaxe |
| `axe_stamp.png` | axe |
| `clock_stamp.png` | hourglass |
| `book_stamp.png` | book / scroll |
| `clover_stamp.png` | leaf / clover |

---

## Import notes

- **Individual PNGs are easiest.** If a pack ships one file per sprite, just rename
  and drop in — done.
- **Sprite sheets need slicing.** For a packed sheet, in Unity: select it →
  Inspector → Sprite Mode **Multiple** → open **Sprite Editor** → **Slice** → then
  either reference the sub-sprite you want or export the single cell you need and
  rename it to the id. (The auto-importer sets everything in `Resources/Sprites/`
  to Sprite type for you; you only change Single→Multiple for sheets.)
- **Transparency:** use PNGs with alpha so the placeholder background doesn't show.
- Tiles (`tile_ground`, `tile_platform`) are stretched horizontally in code, so a
  single seamless tile works fine.

## Sources
- [Pixel Adventure 1 — Pixel Frog (CC0, free)](https://pixelfrog-assets.itch.io/pixel-adventure-1)
- [Pixel Adventure 2 — Pixel Frog (CC0, **paid** $5 min — optional)](https://pixelfrog-assets.itch.io/pixel-adventure-2)
- [Shikashi's Fantasy Icons Pack — shikashipx (CC-BY 4.0)](https://shikashipx.itch.io/shikashis-fantasy-icons-pack)
- [itch.io CC0 nature assets](https://itch.io/game-assets/assets-cc0/tag-nature)
- [Kenney — CC0 game assets (alternative anchor)](https://kenney.nl/assets)

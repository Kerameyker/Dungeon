# Hollow Spire — Game Design Brief (vertical slice)

**Genre:** 3D action dungeon crawler, third person, PC.
**Engine:** Unity 6 (6000.x), C#.
**Inspiration:** floor-by-floor tower climbing and a hotbar of "sword skills" in the style of VR-MMO anime. The game is an original work: original names, world and characters.

## Core loop
Enter a floor (procedural labyrinth of rooms and corridors) -> fight monsters -> gain XP and levels -> unlock and use sword skills -> find the boss room -> defeat the Floor Guardian -> a stairway appears -> press E -> next floor, harder.

## Player verbs
- Move (WASD), sprint (Shift), dodge roll with invulnerability frames (Space).
- Light attack: 3-hit combo (LMB). Timing window keeps the chain going.
- Sword skills (keys 1-4), each with a cooldown and a distinct movement and hit pattern.
- Interact (E) on the stairway; Esc frees the mouse.

## Sword skills
| Key | Name | Unlock | Pattern | Multiplier | Cooldown |
|---|---|---|---|---|---|
| 1 | Crescent Cut | Lv 1 | 140° horizontal arc | x1.8 | 3.0 s |
| 2 | Skyfall Slash | Lv 1 | hop + vertical overhead, stuns | x2.2 | 5.0 s |
| 3 | Lancing Dash | Lv 2 | long forward dash thrust | x2.6 | 7.0 s |
| 4 | Ember Flurry | Lv 4 | 5 fast hits in 360° | 5 x x0.8 | 12.0 s |

## Enemies
- **Frenzy Boar:** fast, low HP, short attack.
- **Bone Soldier:** balanced melee.
- **Stone Golem:** slow, tanky, heavy hit with long telegraph.
- **Floor Guardian (boss):** big HP pool, melee + ground slam with red telegraph ring.

Enemies telegraph attacks (flash red) so dodge rolling is meaningful. Pathfinding is grid BFS on the dungeon layout.

## Progression and balance (see `Progression.cs`)
- XP to next level: `60 * level^1.5`.
- Player: HP `100 + 15*(L-1)`, Attack `10 + 3*(L-1)`, Defense `2 + (L-1)`.
- Enemy scaling per floor: HP and attack `x (1 + 0.35*(floor-1))`.
- Damage: `attack * multiplier * 100/(100+defense)`, crits (10%) x1.5, min 1.
- Enemies per floor: `min(6 + 2*floor, 20)`, plus 1 boss. HP orbs drop with 25% chance and heal 15%.

## Dungeon generation
Random non-overlapping rooms on a 48x48 grid, joined in a chain by 2-wide L-shaped corridors (always connected). Start room = first room; boss room = room farthest from start by walking distance.

## Presentation
Low-poly, all geometry from primitives; procedurally generated textures (noise + brick lines), procedurally synthesized sound effects and ambient music. Dark, foggy dungeon lit by torches; cyan/white HUD with a minimap that reveals as you explore.

## MVP scope (this build)
Included: everything above, HUD, minimap, damage numbers, death and respawn, endless floors.
Added in update 1: lock-on targeting (Q / MMB, Tab switches), gear and loot (5 rarities, 3 slots, backpack of 20, keyboard inventory on I).
Not yet: NPC town/hub, save/load, story, gamepad.

## Loot rules (see `Items.cs`)
- Drop chance 30% per normal enemy; bosses drop 2 items, always Rare or better.
- Rarity weights (normal): Common 60%, Uncommon 28%, Rare 9%, Epic 2.5%, Legendary 0.5%. Boss: Rare 60%, Epic 32%, Legendary 8%.
- Stat multiplier by rarity: x1.0 / 1.25 / 1.6 / 2.1 / 3.0, scaled by floor; +-10% variance.
- Weapon: ATK. Armor: DEF and HP. Trinket: crit chance (+1% base, +1.5% per rarity step), small ATK and HP.
- Player stats = level stats + gear bonuses. Crit chance = 10% + gear (capped at 75%).

## Death rule
Dying respawns you at the start of the current floor with full HP; the floor is regenerated from the same seed (enemies reset), XP and levels are kept.

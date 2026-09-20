# API contract for the "big update" (internal, read before touching Runtime)

The Unity project is generated without a Unity editor, so every file must compile the first time. Follow these signatures EXACTLY;
other files (owned by other people) call them. If a signature here is impossible, keep your file compiling and describe the deviation in your report.

Namespaces: Runtime code is `namespace Hollow`, pure logic is `namespace Hollow.Core` (see Assets/Scripts/Core/*.cs; read Items.cs, Weapons.cs,
Systems.cs, Quests.cs, Story.cs, GameRules.cs, Biomes.cs, Save.cs). Do not use `using System;` together with `using UnityEngine;` unless you qualify
`UnityEngine.Random`/`UnityEngine.Object`. UI uses legacy uGUI `Text` (font: `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")`), overlay canvas 1920x1080 reference.
No external assets: everything is code-generated (textures via Texture2D, meshes via ProcAssets.Prim, sounds via synthesis). Existing helpers: ProcAssets
(Lit/Unlit/Tint/Prim/SoftCircle/SpriteMat/BlobShadow), Particles.Flame/Dust, Spark.Burst, GroundRing.Spawn, DamageNumber.Spawn, Billboard.

## Player (PlayerController) members the UI/visual code may READ
```csharp
public int Level, Xp, Hp, MaxHp, Attack, Defense, Gold, Shards;
public float CritChance { get; }
public readonly Consumables Supplies;            // Potions, ReturnCrystals, ReviveTokens
public readonly Equipment Gear;                   // Gear.Slots[0..2], Gear.Weapon (WeaponType)
public readonly Inventory Bag;                    // Bag.Items (List<Item>), Inventory.Capacity == 20
public WeaponType CurrentWeapon { get; }
public SkillDef[] Skills;                         // hotbar: length 4, or 5 when the dual-blade skill is unlocked
public Cooldown[] SkillCd;                        // same length as Skills
public int SkillMasteryLevel(int skillIndex);     // 0..5
public float PotionCooldownNormalized { get; }    // 0 = ready, 1 = just used
public bool DualUnlocked { get; }
public Enemy LockTarget { get; }
public string HeroName { get; }                   // "Wanderer"
```
## Enemy members
```csharp
public string DisplayName; public int Hp, MaxHp; public bool IsElite; public EnemyDef Def;
public int BossBars;        // 1 for normal enemies, 2..5 for bosses
public string BossTitle;    // "" or e.g. "Vanguard of the Sunken Gate"
public int Phase;           // 0-based bar index for bosses
public bool IsDead; public float HeadHeight;
```
Boss bar math is in Core: `BossInfo.BarIndex(hp,maxHp,bars)` and `BossInfo.BarFill(hp,maxHp,bars)`.
## Game members
```csharp
Game.Instance; public int Floor; public int MaxFloor; public bool InTown; public bool Paused;
public string BiomeName { get; }
public Companion Companion { get; }     // null until Miri joins
public QuestLog Quests { get; }
public bool Hardcore { get; }
```
`Companion` (MonoBehaviour): `string Name`, `bool SwitchReady`, `bool SwitchWindowOpen` (an enemy is staggered and a switch is possible now), `float SwitchCooldownNormalized` (0 ready..1).
## Existing statics kept: `ItemUi.Colored(item)`, `ItemUi.Stats(item)`, `ItemUi.RarityColor(rarity)` (in LootDrop.cs). Use `item.DisplayName` (includes "+N") for names.

---
## A. Hud.cs (owner: HUD agent) — full rewrite, keep `public class Hud : MonoBehaviour` with `public static Hud Instance` and `public static void Create()`
Keep these existing methods (others already call them): `Toast(string text, float seconds = 2.5f)` (rich text ok), `Prompt(string text)` (shown for the current frame window, as today),
`Flash()`, `SetFloor(int floor)` (floor <= 0 means TOWN), `SetBoss(Enemy boss)`, `ClearBoss()`, `ShowDeath(bool)`, `MarkStairs(Vector3 worldPos)`, `SetMinimap(DungeonLayout layout)`,
`ShowInventory(bool)`, `RefreshInventory(PlayerController p, int cursor)`, `int InventoryRowAt(Vector2 screenPos)`.
NEW methods to add (exact signatures):
```csharp
public void SetBiome(string name);                                   // small text under the floor number
public void ShowBanner(string title, string subtitle, float seconds); // big centered banner (floor entry, act change)
public enum MapMarkerKind { Chest, Shrine, Boss, Gate }
public void ClearMapMarkers();  public void AddMapMarker(Vector3 worldPos, MapMarkerKind kind);  // minimap icons; RemoveMapMarkerNear(Vector3 pos) optional
public void SetQuestTracker(string richText);                        // top-right under minimap, "" hides
public void ShowDialogue(string speaker, string text, Color speakerColor, bool hasMore); // bottom dialogue box, "[E] continue" if hasMore else "[E] close"
public void HideDialogue();
public sealed class MenuRow { public string Text; public Sprite Icon; public Color Border = Color.clear; public bool Dim; }  // (declare inside namespace Hollow, NOT nested)
public void ShowMenu(string title, string help);                      // opens the centered list panel (shop, blacksmith, quest board, ending choice)
public void SetMenu(System.Collections.Generic.List<MenuRow> rows, int cursor, string detail, string footer); // up to 14 rows; detail = rich text block on the right; footer = one line (gold etc.)
public int MenuRowAt(Vector2 screenPos);                              // row index under the mouse or -1
public void HideMenu();
public int InventoryGearSlotAt(Vector2 screenPos);                    // 0 weapon, 1 armor, 2 trinket under mouse in the inventory doll, else -1
public bool AnyPanelOpen { get; }                                     // inventory/menu/dialogue visible
```
HUD content: (1) player frame: name, level, HP bar with number, XP bar, ATK/DEF/CRIT, gold, shards, weapon type; (2) skill bar of `p.Skills.Length` (4 or 5) slots built lazily/rebuilt when the length changes: skill icon
(`ItemIcons.SkillIcon(SkillDef, WeaponType)`), key label 1-5, radial cooldown + seconds, locked overlay "Lv N" if `p.Level < skill.UnlockLevel`, mastery pips (`p.SkillMasteryLevel(i)`), tooltip-less; (3) consumable slots: potion (H, count, cooldown fill),
return crystal (T, count), revive token (count, passive); (4) target frame for `p.LockTarget` (name, elite marker, HP bar); (5) boss frame: name, title, N segmented bars using `BossInfo`, drawn as N stacked/segment bars with the active one filled, "phase" pips;
(6) companion status ("Miri", switch ready indicator / "SWITCH! [G]" pulse when `Companion.SwitchWindowOpen && SwitchReady`); (7) minimap with player arrow, room reveal (as now), stairs marker, map markers; floor label + biome; (8) toast, prompt, banner, damage-vignette flash, death overlay
("YOU DIED\nR  respawn on this floor        T  return to town", or in hardcore the text passed by ShowDeath is fixed, keep as is);
(9) dialogue box; (10) generic menu panel; (11) inventory (see below). Style: dark glass panels with thin cyan accent lines, rarity-colored borders, crisp text, subtle animation (pulses, smooth bar lerp). Mouse AND keyboard/gamepad friendly (all mouse features have cursor navigation via `Game`).

Inventory redesign: full-screen-ish panel. Left: an equipment "doll" area with 3 slots (Weapon/Armor/Trinket) drawn as framed squares with `ItemIcons.Get(item)` or `ItemIcons.SlotSilhouette(slot)` when empty, rarity-colored border, item name (DisplayName, rarity color) under each, plus the stat totals (Level, HP, ATK, DEF, CRIT, weapon type + short blurb from `WeaponCatalog.Get(p.CurrentWeapon).Blurb`).
Right: 5x4 grid of backpack cells (index = row*5+col) with icons and rarity borders, count "n / 20". The cell at `cursor` is highlighted; below the grid a detail card for the selected item: icon, name, slot, rarity, stats lines (ItemUi.Stats), and a comparison vs the equipped item of the same slot
(green +N / red -N power, using `Score`). `InventoryRowAt` returns the bag index (0..19) of the cell under the mouse (only cells with items), `InventoryGearSlotAt` the doll slot. Help line: "Mouse: click item = equip, right click = discard, click gear = unequip     Keys: WASD/Enter/X     I close".

## B. ItemIcons.cs and HeroGear.cs (owner: GEAR agent) — NEW files
```csharp
public static class ItemIcons {
  public static Sprite Get(Item item);                      // 48x48 pixel-art style icon, cached by (slot, WType, Style, Rarity, Seed%N); border/background by rarity, glyph by slot/type/style
  public static Sprite SlotSilhouette(ItemSlot slot);       // dim empty-slot glyph
  public enum ConsumableIcon { Potion, Crystal, Revive, Shard, Gold }
  public static Sprite Consumable(ConsumableIcon kind);
  public static Sprite SkillIcon(SkillDef skill, WeaponType weapon);  // glyph by SkillKind (arc slash, vertical slash, thrust lines, flurry star), tinted per weapon type; unique skill (Id "twin_tempest") gets a crossed-blades glyph
  public static Color RarityColor(Rarity r);                // Common grey-white, Uncommon green, Rare blue, Epic purple, Legendary orange-gold (same hues as ItemUi)
}
public class HeroGear : MonoBehaviour {
  public static HeroGear Attach(GameObject playerRoot, Transform visual, Transform bodyRoot, Transform legL, Transform legR, Transform armL, Transform swordPivot, Material bodyMat);
  public void Refresh(Equipment gear, bool dualUnlocked);   // rebuilds ALL gear visuals; safe to call any time, cheap enough to call on every equip
  public Color BodyColor { get; }                           // new base color for the coat/leg material (player tints _bodyMat with it)
  public float TipZ { get; }                                // local Z of the weapon tip inside swordPivot (for the trail)
}
```
Player rig (already built by PlayerController): `visual` root; `bodyRoot` (hip-height pivot) has children "Torso"(capsule),"CoatTrim","Belt","Head","Hair","ArmL"(pivot, arm cube + hand),"Tail"(pivot, coat tail); `legL`/`legR` are hip pivots with cube "Leg" and "Boot";
`swordPivot` has children "Blade","Guard","Hilt","HandR","ArmR","Tip"(trail anchor at z=1.25). HeroGear must hide (disable renderers of) Blade/Guard/Hilt and build its own weapon under swordPivot in a child "GearWeapon" (blade along +Z from the hand, hand at origin, like the placeholder),
armor pieces under bodyRoot/legs in children named "Gear_*" (destroy and rebuild on Refresh), off-hand blade under armL as "GearOffhand" only when `dualUnlocked`.
Appearance rules: weapon shape by `WType` and `Style` (Sword: 4 blade shapes; Rapier: slim blade + cup/swept guard; GreatBlade: very large broad blade, two-hand look; Dagger: short blade, kris/curved variants) with blade color/emission by rarity; Rare+ glowing edge, Epic+ small floating sparks (Particles.Flame or Spark-like, cheap), Legendary bright aura.
Armor by `Style` (0 Leather Coat, 1 Chain Mail, 2 Knight's Plate, 3 Warden Cloak (hood + cape), 4 Scale Vest) and rarity: Common plain, Uncommon trim, Rare glowing seams, Epic pauldrons/spikes + emissive trim, Legendary aura ring and cape/wings of light. No armor = the plain starting coat.
Trinket by `Style` (ring on left hand, amulet, charm on belt, ember pendant with flame, spire sigil floating behind) with rarity glow. `Upgrade` level adds an extra subtle glow accent (+5, +10 brighter). Keep cost low: <= ~40 primitives total, no colliders, no lights (lights are limited).

## C. Sfx.cs (owner: AUDIO agent)
Keep every existing public member of `Sfx` and `SfxKind`. ADD SfxKind values (append, do not reorder): `UiClick, UiOpen, UiClose, Coin, Switch, Door, Potion, Upgrade, UpgradeFail, Blip, Crystal, BossRoar, PhaseBreak, Chime, Sheath`.
ADD `public static AudioClip TownLoop()`, `public static AudioClip TowerLoop()` (may just return the existing ambient loop), `public static AudioClip BossLoop()`, `public static AudioClip FinalBossLoop()`; each a seamless synthesized loop (22050 or 44100 Hz mono, 16-40 s, generated once and cached; keep generation fast, < 300 ms each ideally).
Moods: town = warm, calm, gentle plucked/pad chords; tower = dark ambient drone with sparse tones (existing); boss = tense low pulse + rising minor arpeggio + percussion; final boss = bigger, choir-like pad + heavy pulse. Effects should feel distinct and satisfying, no clipping (peak <= 0.9).

## D. GameInput.cs (owner: PAD agent)
Keep all existing members (both the `#if ENABLE_INPUT_SYSTEM` branch and the legacy branch). ADD to both branches: `SkillPressed(int index)` for 0..4, `bool SwitchPressed` (G / pad), `bool PotionPressed` (H / pad), `bool MenuBackPressed` (Esc or pad B; note Esc is also EscapePressed), `bool UsingGamepad { get; }` (true if the last input came from a gamepad), `bool MenuClickPressed` (left mouse or pad A, used by menus).
Gamepad mapping (new Input System `Gamepad.current`; legacy branch: only keyboard/mouse + the `Input.GetAxis("Horizontal/Vertical")` are NOT to be relied on for pads, so the legacy branch may return false/zero for pad-only features):
left stick = Move (also menu navigation); right stick = Look (scale so `Look` is comparable to mouse "legacy axis units per frame" (mouse is about 0.1 per pixel; a full stick deflection should turn about 180 degrees per second at default sensitivity; multiply by Time.deltaTime), deadzone 0.15);
south (A) = jump (and Confirm in menus); east (B) = dodge (and MenuBack in menus); west (X) = attack; north (Y) = interact; left shoulder = skill 1, right shoulder = skill 2, left trigger = skill 3, right trigger = skill 4 (RT also attack? NO: attack is X only), dpad up = skill 5;
right stick click = lock-on; dpad right = SwitchTarget; dpad left = Potion; dpad down = Switch; start = inventory; select/back = return to town; west (X) = Discard in menus; south = Buy/confirm in menus.
`MenuUp/Down/Left/Right` also react to dpad and the left stick (with a repeat delay so one flick = one step). `Sprint` = left stick click. Existing keyboard bindings must all keep working. Cursor lock is handled elsewhere.

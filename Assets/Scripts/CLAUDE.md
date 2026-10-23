# CLAUDE.md

Guidance for Claude Code working in `Assets/Scripts` (Stormfall — 2D Unity top-down RPG, ~29K LOC, 184 scripts).

**No build/test CLI** — everything runs through the Unity Editor. Don't try to compile or run tests.

**Plugins**: EasySave 3 (ES3), A* Pathfinding, Pixel Crushers Dialogue, ProCamera2D, TextMesh Pro, URP, HighlightPlus2D, RainMaker.

## Coding Conventions

- Methods/fields (public + private): `camelCase` — `damageCharacter()`, `enemiesInCombat`
- Classes/enums: `PascalCase` — `PlayerController`, `enum itemType { Gun, Throwable }`
- Constants: `UPPER_CASE` — `SMALL_SHAKE = "Small Shake"`
- Saves: `[ES3Serializable]` to include, `[ES3NonSerializable]` to exclude

## Character Hierarchy

```
Entity              — dialogue, faction (0–9), portraits
└── Character       — movement, 4-dir anim, footsteps, sliding
    ├── NonCombatChar — A* NPCs (Owen, Kipling, b3lt)
    └── CombatChar   — health orbs (3 mods), bleeding, death, grenades, stun
        ├── Critter  — direct child of CombatChar (NOT Enemy); hares/birds
        └── Humanoid — guns, shields, bullets, recoil, muzzle flash
            ├── PlayerController  — input + abilities + alignment (~2500 LOC)
            └── Enemy             — AI state machine + pathfinding (~1054 LOC)
                ├── SoldierBehavior   — behaviorType 0=aggressive, 1=strategic
                ├── BurrowerBehavior  — overrides flee() to no-op
                └── SentinelBehavior
```

Locations: `Character Hierarchy/`, enemies in `Enemy Scripts/`, player in `Player Controller/`, NPCs in `NPC/`.

## Direction Convention

`TEMLib.getDirection()` → **0=Up, 1=Right, 2=Down, 3=Left** (cardinal quadrants). Used everywhere — animation, AI, projectile spread, facing. Don't reorder.

## Hardcoded Child Indices (don't reorder prefab children)

- `Character.Start()`: 0=chest, 1=head, 2=legs
- `Humanoid.Start()`: 3=gun, 4=standShadow, 6=layShadow, 7=shieldAnimator
- `PlayerController.heads[4]`: `transform.GetChild(1).GetChild(0–3)`
- `Critter.Awake()`: 8=anglerhareLight
- `SoldierBehavior.Start()`: 8=personalLight, 9=helmetLight

## Two Event Systems

**`GameEvents.cs`** — pub/sub for gameplay (`event Action<T>` + invoke wrappers). **Subscribe in `OnEnable()`, unsubscribe in `OnDisable()`.** Categories: combat, status, equipment, actions (reload/shot/slide), system (scene/menu/title), state flags (`enemiesInCombat`, `isLowHealthState`, `isLowAmmoState`). Look up exact names in source.

**`EventManager.cs`** — imperative environment/cinematic commands (no subscribers, direct calls). Camera shakes, `hitFreeze()`, `playerDeath()`, indoor/outdoor, rain, dialogue typewriter skip.

⚠️ Some `GameEvents` "reset" fires (e.g. `infestationEnd`) aren't idempotent — guard toggle-style handlers so they don't flip state the wrong way.

## Save System (ES3)

**Files**: `<slot>.es3` (cross-scene) + `<slot>_<scene>.es3` (per-scene). Single save kind — no Checkpoint/Camp suffixes.

**Per-scene file also stores**: `LastCampPositionInScene` (set on camp rest) and `SceneFirstEntryPosition` (locked on first entry per slot).

**Invariants**:
- Save flow: `ES3AutoSaveMgr.Save()` → cross-scene singletons (Player, Inventory, Dialogue, alignment, sprites, timestamp). Background-flushed; `WaitForActiveSave()` drains.
- Load flow: `returnSlotName()` resolves slot → `ES3.LoadInto<T>` singletons → `ES3AutoSaveMgr.Load()` → fires `previousSceneDataLoaded`.
- **Camp save ordering**: camp savepoints write camp position **after** `saveSceneData() + WaitForActiveSave()` to avoid sync-vs-background race.
- **Dream world**: inventory keyed `InventoryManager<sceneName>` (scene-scoped, not shared).
- **Death respawn (Dark Souls style)**: `EventManager.deathPostProcessTransition` overrides position to `getRespawnPositionForScene()` (last camp → first-entry), `prepareForRespawnSave()` reverses death state, then save + reload. Save-before-reload means load reads alive state — no death branch in load.
- **`AutoSaveBehavior`**: Supply Cache (`isCamp=false`, one-time, `usedUp` permanent) vs Camp/Powered Chest (`isCamp=true`, 5s cooldown, records last camp pos). Saves disabled during combat (`onAggro`/`onUnaggro` toggles collider).

⚠️ **Don't call ES3 directly from UI scripts** — add an accessor on `SaveManager` and call that.

## Scene Transition

`LevelLoader.changeScene()` → autosave + `WaitForActiveSave()` → fade audio/black → `LoadSceneAsync` → `sceneReady()` (load scene data, register first-entry position, fire `sceneTransitionComplete`).

## Inventory

- `InventoryManager`: `inventory[INVENTORY_SIZE]`, `equippedArmor[3]` (0=helmet, 1=chest, 2=legs), `ammoArray[3]` (0=rifle, 1=pistol/∞, 2=shotgun)
- **Hotbar size is one constant.** `INVENTORY_SIZE = 6` drives `QUICK_USE_1_SLOT = SIZE-2` and `MEDKIT_SLOT = SIZE-1`. Layout: slots 0–3 are weapons (keys `1`–`4`), then the quick-use pair — slot 4 (`X`) and slot 5 (`Z`, medkit-locked). Number keys are generated from `QUICK_USE_1_SLOT`, so no input change is needed to resize
- **Resizing also requires prefab work**: both `Canvas .prefab` hotbars (`Hotbar` and `Inventory Panel/Hotbar (1)`) need a matching slot child inserted before their trailing `Separator`. `HotbarUIBehavior`/`InventoryMenu` throw in `Awake` if prefab child count and `INVENTORY_SIZE` disagree
- Slot UI is resolved by sibling index, never by name or serialized index — `DroppableSlot.slotNumber` is a derived property (`GetSiblingIndex() + 1`), and the separator must stay the **last** child
- ES3 replaces the `inventory` array wholesale on load with one sized to the save, so `updateEquippedItemsAfterLoad()` calls `normalizeInventoryLength()` first. It's a crash guard, not a migration — entries keep their original indices
- **Crafting mats** (`craftMatArray[3]`: 0=cloth, 1=chem, 2=scrap) are stored in **quarter-units** (4 = one whole unit in UI). Carry caps are tuned per material in whole units via `CRAFT_MAT_CAP_UNITS` in `InventoryManager`; always check via `isCraftingMatFull()`/`getCraftingMatCap()`, never a literal. No prefab change needed until caps hit two-digit units
- `enum itemType`: Gun, Throwable, Healable, Lantern, Armor, Conductor, Empty, Component, Tech, RifleAmmo, ShotgunAmmo, Cloth, Chemical, Scrap
- Rarity 0–5: craftable, common, uncommon, rare, exceptional, unique
- `Gun`: `gType` 0=Traditional, 1=Lancer, 2=ElectroSpike, 3=ShockBaton; `mType` 0=Rifle, 1=Pistol(∞), 2=Shotgun. Carries 1–3 `Charm`s — drive stats and display name (`namePrefix/nameCore/nameSuffix`)
- `CharmBehaviorManager` listens to `GameEvents` and applies effects (`"explosive"`, `"charge_laser"`, `"slow_time_reload"`, etc.)
- `LootMaster.cs`: factory (`createGun/Armor/Throwable/Healable`, `spawnItem`); sprite arrays `itemModels[25]`, `outlines[25]`

## Player Abilities

| Key | Ability | | Key | Ability |
|---|---|---|---|---|
| Mouse0 | Shoot | | Shift | Slide (cooldown) |
| Mouse1/Q | Shock Baton | | Tab | Menu |
| Space | Shield Wall | | R | Reload |
| C | Overload (AoE) | | E | Interact |
| T | Turret | | X/Z | Quick Use 1/2 |
| F | Kipling (needs `hasKipling`) | | | |

**Alignment**: `Vector2 alignmentArray` → `alignmentQuadrant` (Center / OrderSelf / FreedomSelf / FreedomCommunity / OrderCommunity). Drives dialogue/NPC reactions.

**Damage**: 0.1s i-frames per hit; blood vignette + screen shake; distinct audio for shield vs health.

## Animation

- Direction-locked states keyed by 0–3
- `AnimationEventController` → `Humanoid` callbacks: `unfreezeAnimation→tryAnimUnlockAfterGun`, `getUp→stopSliding`, `finishRecoil→doneRecoiling`, `finishDying→characterDeath`
- `Character.LateUpdate().checkForGameBreakingBugs()` is an intentional every-frame desync guard — **don't remove**
- Spritesheets loaded from `Resources/` into `Dictionary<string, Sprite>`, cached by sheet name

## Enemy AI

- `enum State { Idle, Engage, Reposition }`; virtual `engage/reposition/idle` overridden per subclass; `changeState()` runs one-time setup
- A* `Seeker`, repath every 1s (`InvokeRepeating("updatePath")`), waypoints in `path.vectorPath`
- Act scaling: `health *= ((act * 0.2f) + 1)`
- HUD: call `EnemyHUDController.Initialize/OnDamaged/OnRecharging/SetVisible` (some legacy UI code remains in `Enemy.cs` ~937–1049)

### Enemy Death VFX Rule

`Enemy.respawnFromDeath()` resets logical state but cannot re-stage GameObjects destroyed or detached during death. Any GameObject created/activated as part of a death sequence **must** be a world-rooted clone: `Instantiate(template, pos, rot, ObjectPooler.instance.transform)`. Never a long-lived child of the enemy that gets `Destroy`d or reparented. Inactive templates may live in the prefab; don't activate the template directly. See `BurrowerBehavior.triggerDeath`, `SentinelBehavior.detonationCountdown`.

## Story Beats

`StoryBeatManager.startNewBeat()` toggles GameObject ranges via `toggleBeatFinalIndex[]`. `beatIndex` serialized; on scene load, all beats up to current index are replayed to restore state.

## Singleton Pattern

```csharp
public static ManagerName instance;
void Awake() {
    if (instance == null && this != instance) instance = this;
    else Destroy(gameObject);
}
```

## Key Files

| Script | Location | Role |
|---|---|---|
| `TEMLib.cs` | `_Library Routines/` | `getDirection`, `getSmoothLerpT`, `AddExplosionForce` |
| `GameEvents.cs` | `Game Management/` | Pub/sub event dispatcher |
| `EventManager.cs` | `Game Management/` | Imperative env/cinematic commands |
| `SaveManager.cs` | `Game Management/` | ES3 save/load orchestration |
| `LevelLoader.cs` | `Game Management/` | Scene transitions with fade |
| `InventoryManager.cs` | `Game Management/` | Hotbar + armor + ammo |
| `LootMaster.cs` | `Game Management/Inventory/` | Item factory + pickups |
| `CharmBehaviorManager.cs` | `Game Management/Inventory/` | Charm effects via events |
| `AudioManager.cs` | `Game Management/Sound/` | Sound playback |
| `StoryBeatManager.cs` | `Game Management/Story Beats/` | Quest/story progression |
| `CanvasMaster.cs` | `Game Management/UI/` | Vignette, letterbox, checkpoint text |
| `HUDUIBehavior.cs` | `Game Management/UI/` | Ammo + magazine visualization |
| `MouseManager.cs` | `Game Management/` | Cursor mode (pointer ↔ reticle) |
| `EnemyHUDController.cs` | `Character Hierarchy/` | Enemy health/shield bars |
| `AnimationEventController.cs` | `Character Hierarchy/` | Anim clip events → Humanoid |

## Don't "Fix" These (Intentional)

- **`PlayerController` monolith** (~2500 LOC): input + actor + abilities + alignment fused on purpose
- **`checkForGameBreakingBugs()`**: every-frame desync guard
- **`SoldierBehavior.behaviorType` int switch**: personality system, not missing polymorphism
- **`ObjectPooler.cs`**: misnamed — currently serves as `DontDestroyOnLoad` parent
- **Legacy UI in `Enemy.cs`**: partial migration to `EnemyHUDController` is in progress

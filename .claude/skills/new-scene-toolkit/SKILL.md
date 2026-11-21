---
name: new-scene-toolkit
description: Generate a rough-draft Unity scene (tile blockout, NPCs, props, barks, beat triggers, beat manager) for a new act/level by hand-authoring scene YAML from _TEMPLATE SCENE.unity with the Python scripts in this skill. Use when asked to build, mock up or block out a new scene/level/act, add a scene to the build, or place many objects/tiles in a scene without opening the Unity Editor. Pair with the scene-barks-and-beats and dialogue-db-authoring skills.
---

# New scene toolkit (hand-generated Unity scene YAML)

Python library that builds `Assets/Scenes/<Scene>.unity` by copying `_TEMPLATE SCENE.unity` and **appending whole blocks**
(prefab instances, plain GameObjects, tile data). No Unity Editor, no build CLI. Scripts live in `scripts/` next to this file.
Python: `/c/Python314/python` (Git Bash) — the bare `python3` alias is a Store stub and does not work.

## Files
| Script | Role |
|---|---|
| `scenegen.py` | `SceneBuilder`: `inst`, `npc`, `plain_go`, `group`, `bark_with_trigger`, `beat_trigger`, `make_beat_manager_prefab`, `set_tilemap`, `set_player_start`, `finalize`; helpers `rect_outline/hline/vline/reachable` |
| `tiles.py` | `harvest(scene, tilemap, tile_name, name2guid)` reuses a (tile, sprite, matrix) triple already painted in a donor scene; `tilemap_body`/`replace_tilemap_block` |
| `prefabinfo.py` | `info("Env/Obstacles/Crate.prefab")` -> (guid, root transform id, root GO id, root components) — the ids PrefabInstance mods target |
| `dialogue_lib.py` | `DialogueAuthor`: actors, quests, barks, conversations -> appended to the Dialogue Database (see dialogue-db-authoring skill) |
| `validate_scene.py` / `validate_dialogue.py` | structural validators (run these every time) |
| `example_scene.py` | minimal spec; **copy it** as the starting point for a real scene |

## Workflow
1. **Gather** the narrative, location list, who/what is where, which beats gate what. Ask the user about genuine design forks (scene split, new systems) — everything else use sensible defaults.
2. **Find donors**: blockout style = `Act1_OldTown` (flat `base_tileset_13` ground on `Tilemap_GROUND`, solid `metal_walls_15` / `Wilds_Wood_House_26` on `Tilemap_DYNAMIC`). `Act1_Owens` is fully-arted (use it for indoor props / tiles to borrow). Never `Read` scenes (2-60 MB): grep/sed/python only.
3. **Copy `example_scene.py`** to a scratch dir, replace the LAYOUT section. Register every prefab you place (`b.register_prefab(key, "path under Assets/Prefabs")`). Keep a layout table of tile coordinates; **1 tile = 1.5 world units**.
4. **Run** with `--out-dir <scratch>` first (writes nothing into the project), `validate_scene.py` it, then re-run with `--into-project`.
5. **Dialogue**: build actors/quests/barks/conversations with `DialogueAuthor`, `save(marker="<SceneName>/")`, then `validate_dialogue.py "<SceneName>/"`.
6. **Code** (beat manager subclass etc.): see scene-barks-and-beats. New `.cs` needs a `.meta` with a fresh guid (MonoImporter block, copy an existing `.cs.meta`) **before** the scene references it, and a manual `Assembly-CSharp.csproj` `<Compile Include>` line (gitignored; for the compile check only).
7. **Build settings**: append the scene to `ProjectSettings/EditorBuildSettings.asset` (enabled, path, scene `.meta` guid) — append at the end so New Game index is unchanged.
8. **Compile check**: PowerShell `dotnet build Assembly-CSharp.csproj --no-restore -p:LangVersion=9.0` (Git Bash mangles `/p:` — use `-p:` or PowerShell).
9. **Independent review** with the `scene-yaml-reviewer` agent (Codex second opinion) before handing over.
10. **Tell the user the Editor steps** (below), and ask them to press Play from the new scene and paste any Console errors: structural validation can't see runtime type/lifecycle errors, and fixing them is an expected final loop (patch the generator spec AND the scene in place — the user may have saved the scene in Unity, so check the file's mtime/size first and prefer in-place patches over regenerating).

## Hard-won facts (don't rediscover)
- **Tilemaps**: template Tilemap objects are `Tilemap_GROUND/_DYNAMIC/_METAL_GROUND/_INDOOR_FLOOR/_OVERHEAD/_OVERHEAD_NOCOLLIDERS/_Roof_Low/_Roof_High/_WATER/_WATER_BLOCKING`. Colliders (+Composite) are on DYNAMIC, OVERHEAD, METAL_GROUND, INDOOR_FLOOR, WATER*. DYNAMIC = walls/fences. Tile arrays are rewritten wholesale by `set_tilemap`; per-cell `m_AllTileFlags: 1073741825`, object index `65535`.
- **Rule tiles** pick sprites per cell by neighbours; the harvester takes the most common one. Fine for blockout, not for art.
- **Prefab roots**: use the *base* prefab. `Template_Non Combat Char.prefab` is the talkable NPC. `...NO SPEAK Variant.prefab` is a true variant: its instance target ids are derived/xor'd and can't be read from files — avoid variants; make ambient NPCs with `SceneBuilder.npc(talk=False)`, which puts the five talk components (DialogueSystemTrigger, DialogueActor, Interactable, HighlightTrigger2D, HighlightEffect2D) in `m_RemovedComponents`, exactly like the variant does. **Never just set `m_Enabled: 0` on `Interactable`**: it still receives `OnTriggerExit2D` (e.g. when a Beat Trigger deactivates over it) and throws a NullReferenceException on its uninitialised outline. For a guard that must *bark*, use `talk=False, bark_only=True` (keeps DialogueActor + DialogueSystemTrigger, needed for the bark UI). Check variant-ness: if `prefabinfo.info()` raises IndexError there's no real root Transform.
- **Object-reference field types matter**: `DialogueSystemTrigger.barker` is a **Transform**. A GameObject ref resolves in YAML but Unity throws `MissingReferenceException: The variable barker ... doesn't exist anymore` and the bark falls back to the bark object itself ("speaker has no bark UI"). The template already has a stripped Player Transform (`370354940`, `SceneBuilder.player_transform_ref()`); NPC barkers use `inst.t_ref()`. `validate_scene.py` type-checks `barker`; when you add a new ref field, check its C# field type.
- **Scene refs into prefab instances need "stripped" blocks** (`Inst.go_ref/t_ref/mb_ref`), never the raw prefab fileID. A stripped MonoBehaviour needs the script guid.
- **Modification paths**: `m_LocalPosition.x`, `m_Name`, `m_IsActive` (on the root GO id), component fields by name, arrays `field.Array.size` + `field.Array.data[i]`, UnityEvent calls `eventToTrigger.m_PersistentCalls.m_Calls.Array.data[0].m_Target|m_TargetAssemblyTypeName|m_MethodName|m_Mode`. A modification with an object ref has `value: ` empty and `objectReference: {fileID: N}`.
- **Groups**: child PrefabInstance gets `m_TransformParent: <group transform id>`; the group Transform must list the children's *stripped transform ids* in `m_Children` (`SceneBuilder.group` does this).
- **fileIDs**: generated objects use 7_000_000_000_001+ (validate_scene.py treats [7.0e12, 7.1e12) as "ours"). Template objects (e.g. Player instance `8518940669788288203`) are not ours.
- **Template contents**: Canvas, @GameMaster, @SaveManager, @A*, Camera, Player, @Dialogue Manager, Canvas (Worldspace), Grid+tilemaps. No beat manager, no NPCs. Player instance position is set via `set_player_start`.
- **No SceneRoots block** in 2020.1: new root objects need nothing extra (m_RootOrder 0 is fine).
- **ES3**: beat/trigger prefabs carry `ES3Prefab` (`prefabId`, `localRefs` `_Keys`/`_Values`) + `ES3AutoSave`. `make_beat_manager_prefab` clones the Oldtown one with fresh ids (non-negative 63-bit values). `ES3AutoSave.componentsToSave` must list the beat-manager component or `beatMatrix` is not saved.
- **A\* graph** is binary inside the `@A*` instance: it still covers the *template's* area. The user must open the scene and rescan/resize it.
- **Music/ambience** are per-scene `@GameMaster` `sounds` overrides — a song must be added to the scene's overrides before it can play.
- **LootPlacer** (`Items/LootPlacer.prefab`): `itemType` (0=Gun), `gunModel` (0 rifle, 1 pistol, 2 shotgun), `rarity`; `objsToEnableOnPickup[]` enables objects on pickup — use an inactive Beat Trigger with `on_enable=True` to advance a beat on pickup.
- **Existing actors** in the Dialogue DB include Guardian(12), Laborer(23), Hans(21), Scholar(38) etc.; `DialogueActor.actor` must match an actor *Name*.
- **Localization**: location names shown on save slots come from the `UI Translation Table.asset` (keys like `OLDTOWN`; grep the table for your scene's location name and add a key if missing). Dialogue lines are not localized.
- **Save/scene tables**: `SaveManager` `SCENENAME_TO_LOCATIONNAME_DICT` = fast-travel map locations; scenes that are saved in but not travelable go in `NON_TRAVELABLE_SCENENAME_TO_LOCATIONNAME_DICT`. `act` defaults to 1 and scales enemy health — don't change it for a standalone or prologue-style scene.

## Editor steps to hand to the user (cannot be done from files)
1. Open the new scene once and save (Unity reserializes; new `.meta` guids import).
2. A\* Pathfinding: resize/rescan the graph for the new level bounds.
3. ES3: let the reference manager refresh (Tools > Easy Save 3) if prompted; tick any new `[ES3Serializable]` fields on manual-save types (PlayerController/InventoryManager/RecordingsMenu/dialogue) in the ES3 Types window.
4. Tune colliders/lighting/art, add music overrides, rewrite placeholder dialogue.

## Gotchas observed
- `dotnet build ... /p:LangVersion=9.0` from Git Bash fails with "Only one project can be specified" (path conversion): use PowerShell or `-p:`.
- `beatTrigger` with `autoDeactivate=1` deactivates after the first entry **even if the target method rejects the call** (e.g. wrong beat). Use `one_shot=False` for guarded manager methods.
- PyYAML isn't installed: `python -m pip install pyyaml --target <temp dir>` and set `PYTHONPATH` (validators degrade gracefully without it).
- Don't `Write` over a file you haven't `Read`; for repeated generation write to scratch then copy.

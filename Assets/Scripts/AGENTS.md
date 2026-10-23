# AGENTS.md

Guidance for Codex when working in `Assets/Scripts`.

## Project Shape

- Stormfall is a 2D Unity top-down RPG. This folder contains game logic; run/verify behavior in the Unity Editor, not a CLI test runner.
- Key plugins used in scripts: EasySave 3, A* Pathfinding Project, Pixel Crushers Dialogue System, ProCamera2D, TextMesh Pro, URP, HighlightPlus2D, RainMaker.

## Code Style

- Follow existing Unity C# style: classes/enums `PascalCase`; methods and fields `camelCase`; constants `UPPER_CASE`.
- Use `[ES3Serializable]` and `[ES3NonSerializable]` intentionally because they affect save files.
- Preserve current architecture unless a task explicitly asks otherwise; `PlayerController` is intentionally large, some Enemy UI code is intentionally still in `Enemy.cs`, and `ObjectPooler` is currently a persistent parent despite its name.

## High-Risk Conventions

- `TEMLib.getDirection()` returns `0=Up`, `1=Right`, `2=Down`, `3=Left`; this drives animation, AI, facing, and projectile behavior.
- Be careful with `transform.GetChild(index)` assumptions. Prefab child order is relied on by Character, Humanoid, PlayerController, Critter, and SoldierBehavior.
- Subscribe to `GameEvents` in `OnEnable()` and unsubscribe in `OnDisable()`. Use `EventManager` for direct cinematic/environment commands.
- Do not remove `Character.checkForGameBreakingBugs()`; it is an intentional per-frame desync guard.

## Saves And Scene Flow

- `SaveManager` uses ES3 cross-scene files (`<slot>.es3`) and per-scene files (`<slot>_<scene>.es3`); wait for active saves before scene loads or ordering-sensitive writes.
- Death respawn saves an alive state before reloading the current scene. Do not add a separate death-respawn branch in load without checking this flow.
- Camp savepoints write camp position after `saveSceneData()` and `WaitForActiveSave()` to avoid flush races.
- Dream world inventory is scene-scoped as `InventoryManager<sceneName>`.

## Enemy Death VFX

- Anything created during an enemy death sequence must be a world-rooted clone under `ObjectPooler.instance.transform`; never activate or destroy long-lived prefab children as the VFX object.

## Line Endings

- Treat LF and CRLF as equivalent in this workspace. Do not normalize, modify, or report line endings during implementation or review.

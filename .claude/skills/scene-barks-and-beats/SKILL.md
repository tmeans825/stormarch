---
name: scene-barks-and-beats
description: Conventions for story beats, beat triggers, tutorial barks, NPC conversations and objective alerts in this Unity project (sceneBeatManager subclasses, Beat Trigger prefab, DialogueSystemTrigger barks), plus the reusable runtime pieces (PlayerProfileApplier, TimeFreezeController, CanvasMaster.fadeBlack, CrateDropZone). Use when adding story beats, tutorial popups, scripted moments, quest/objective hooks, or a beat manager to any scene, or when reviewing such code.
---

# Beats, barks and scripted moments

## Beat system (the live one)
`sceneBeatManager` (`Assets/Scripts/Game Management/Story Beats/sceneBeatManager.cs`) — one subclass per scene, a prefab in `Prefabs/Game Management/Beat Managers/`.
`StoryBeatManager.cs` is dead code (nothing references it). Template: `templates/SceneBeatManagerTemplate.cs.txt`.

- `beatMatrix` = int[] of independent **beat lines**; document every line/value in `initializeData()` comments (see Owens/Oldtown/Wilds).
- API: `getBeatProgress(line)`, `progressBeat(line)`, `setBeat(line, v)` (both flag the autosaver). Dialogue reads `getBeatProgress(n)` (Lua).
- **`loadBeats()` replays on every load → restore end-state only.** No cinematics, quest alerts, sounds, fades, healing, music, or `progressBeat` in it. Split each beat into `restoreX()` (called from `loadBeats`) and `playXTransition()` (called from the event method, guarded by the current beat so it's one-shot).
- **Event methods guard on the current beat** (`if (beatMatrix[0] != N) return;`) so repeated trigger entry / reload can't replay them.
- **Singleton hazard**: `sceneBeatManager` is `DontDestroyOnLoad` and later managers `Destroy` themselves, and the base never unsubscribes. A manager that must not outlive its scene should subscribe to `GameEvents.onReturnToTitle` and (unsubscribe `onPreviousSceneDataLoaded`, null `instance`, `Destroy(gameObject)`). Mind that `returnToTitle()` followed by `LevelLoader.loadLevel("TitleScreen")` must be called in the *same synchronous method* (a destroyed coroutine host can't continue).
- Subscribe to `GameEvents` in `OnEnable`, unsubscribe in `OnDisable`, and **null-check `GameEvents.current`** (Awake order isn't guaranteed; retry in `Start`). Reset-style events aren't idempotent: guard toggle handlers.
- ES3: the beat prefab's `ES3AutoSave.componentsToSave` must include the manager component. Scene/Transform refs on it are `[ES3NonSerializable]`.
- Quest/objective alerts are **not automatic**: call `QuestLog.StartQuest/SetQuestState/SetQuestEntryState` (or dialogue `SetQuestState(...)` scripts). `ObjectiveDialogueManagerBehavior` then shows the alert.

## Beat Trigger (`Prefabs/Game Management/Beat Trigger.prefab`, `beatTrigger.cs`)
BoxCollider2D trigger + `UnityEvent`; type 0 = player enters, 1 = `onUnaggro`, 2 = on enable. Persistence is the GameObject's saved active state (ES3AutoSave `saveActive`).
`autoDeactivate=1` → fires once then deactivates **even if the invoked method rejected the call**. So: tutorial barks `one_shot=True`; guarded manager methods `one_shot=False`.
`delayForInvoke` uses scaled time. Commit the beat before the trigger deactivates.

## Tutorial barks ("popups")
There is no tutorial-popup system: a bark is an **inactive GameObject with a `DialogueSystemTrigger`** (`trigger: 64` = OnEnable, `barkSource: 1` = conversation, `barker` = the Player or an NPC **Transform** (not the GameObject: a GO ref gives `MissingReferenceException ... barker doesn't exist anymore`); an NPC barker must keep its `DialogueActor` for the bark UI) that a Beat Trigger enables with `GameObject.SetActive(true)`.
`scenegen.SceneBuilder.bark_with_trigger(...)` builds exactly that. Conversation titles: `<Scene>/Tutorial/<Topic>`, text `<i>...</i>`; key names via `[lua(getKey('Interact'))]` — valid names are the `DEFAULT_BUTTON_KEYS` in `SettingsMenu.cs` (Interact, Slide, Reload, Shield Wall, Overload, Turret, Deploy Firebug, Recall Firebug, Quick Use 1/2, Shock Baton, Crafting, Menu, Deploy Kipling, No Conductor). Movement and shooting aren't in that table — write them as plain text.
DialogueTriggerEvent values: OnBarkEnd 1, OnConversationEnd 2, OnSequenceEnd 4, OnTriggerEnter 8, OnStart 16, **OnUse 32** (NPC talk), **OnEnable 64**, OnTriggerExit 128.

## NPCs
`Template_Non Combat Char.prefab` (`NonCombatChar` : `Entity`). Useful API: `startMoving(Vector2)`, `startFollowingPlayer()/stopFollowingPlayer()`, `templateLook` (None, NormalCitizen, PoorCitizen, Wolf, Guardian, Sentinel, Scholar — randomized appearance), `startingDirection` (0 up,1 right,2 down,3 left). Named characters keep `templateLook: none`. `canRescue` teleports a lagging follower. Looping crowds: `EventManager.startLoopingPath`.
Combat prefabs (Guardians `Enemies/Guardian Prefabsa/`, `Sentinel`, `Wolvolin`) are aggressive; for non-hostile "guards" use NPCs with `templateLook: guardian`. `Wolvolin` is `SentinelBehavior` with `enemyType 1`.

## Lua hooks available to dialogue (`MyLuaFunctions.cs`)
`getBeatProgress(line)`, `recordOutcome(id,"value")` / `checkOutcome` / `hasOutcome` (**string values, write-once**, ES3-saved), `updateAlignment(purpose, affinity)` (purpose +Self/−Community, affinity +Order/−Freedom, steps of 10), `getKey('Action')`, `discoverLocation`, camp functions, and scene-specific hooks you add (e.g. a `startSomething()` hook that calls the scene's beat manager). To expose a new one: add method + `Lua.RegisterFunction(...)` in `OnEnable`.

## Reusable runtime pieces (generic, in `Assets/Scripts/Game Management/`)
| Piece | Use |
|---|---|
| `PlayerProfileApplier` | per-scene protagonist override: sets dialogue `FirstName`/`LastName` (+ Player actor `Display Name`), optional hair/skin sheets; restores defaults on `onReturnToTitle`/destroy. Save slot name comes from `SaveManager.getSlotPlayerDisplayName`. Default (no applier) = Jaden Hayes |
| `TimeFreezeController` | `freeze(realtimeTimeout)` → near-zero timeScale, player can aim; ends on `onBulletCreate` (not `onShotAttempt`), timeout, death or title return; restores the *prior* timeScale/fixedDeltaTime. End it **before** any scene transition (LevelLoader fades use scaled time) |
| `CanvasMaster.fadeBlack(bool toBlack, float seconds)` | unscaled fade coroutine (`yield return`); existing `cutToBlack()` is a fixed 5 s scaled wait |
| `CrateDropZone` (`Story Beats/`) | trigger volume counting `Pushable` crates inside it; recomputes the count so reload/respawn can't double-count; reports to a beat manager |
| `LootPlacer` | placed pickups; `objsToEnableOnPickup` fires beats on pickup |

## Scripted-moment recipes
- *Cutscene dialogue then advance*: `DialogueManager.StartConversation(title)`, wait `isConversationActive` true then false, then continue (the start isn't instant).
- *Long wait / fade*: `PlayerController.instance.freezeChar(true)` (ref-counted: release exactly once, use try/finally), `yield return CanvasMaster.instance.fadeBlack(true, s)`, `WaitForSecondsRealtime`.
- *Return to title*: `SaveManager.instance.WaitForActiveSave(); GameEvents.current.returnToTitle(); LevelLoader.instance.loadLevel("TitleScreen");`
- *Replayed state vs one-shot*: see "loadBeats" above; write the restore path first.

---
name: dialogue-db-authoring
description: Add actors, quests, tutorial barks and branching conversations (with Lua conditions, scripts, outcome flags and alignment effects) to the Pixel Crushers Dialogue Database for this project by generating the asset YAML with dialogue_lib.py. Use when writing placeholder or final dialogue for a new scene, adding NPC conversations/choices, quests/objectives, or checking dialogue database conventions.
---

# Dialogue database authoring

Single database: `Assets/Plugins/Pixel Crushers/Dialogue System/Dialogue Database.asset` (~100k lines of text YAML, LF endings; git-LFS tracked). Don't touch the `(Auto-Backup)` copy. Never `Read` it whole: use `scripts/dialogue_lib.py` (in the **new-scene-toolkit** skill: `.claude/skills/new-scene-toolkit/scripts/`).

## Using the library
```python
import sys; sys.path.insert(0, ".claude/skills/new-scene-toolkit/scripts")
from dialogue_lib import *
d = DialogueAuthor()
guide = d.actor("Guide", "A helpful local")      # new actor; d.actor_id("Guardian") reuses an existing one
d.quest("Quest Name", "description", "success text", ["entry 1", "entry 2"])
d.bark("Scene/Tutorial/Move", "Move with W, A, S and D.")
d.conversation("Scene/Guide/Intro", guide, [
    start(1, 8),                                       # START links to entries 1 and 8 (first valid one by cond wins, in link order)
    E(1, guide, "NPC line.", cond="getBeatProgress(0) < 3", links=[2]),
    E(2, PLAYER, "Player line.", menu="Button text", script='recordOutcome("choice","a")', links=[3]),
    E(3, guide, "Reaction."),
])
d.save(marker="Scene/")        # refuses to double-insert; then: python validate_dialogue.py "Scene/" [--db <path>]
```
Ids are allocated automatically (next free actor/item/conversation id; entry ids are per conversation, START = 0).

## Conventions
- **Titles**: `<Scene>/Act N/<Character>/<Topic>` in existing content (`Oldtown/Act 1/Misc NPC/Breadline NPC`, `Wilds/Act 1/Tutorial/FirebugDeploy`). Pick the act number of the scene being built. Barks: `.../Tutorial/<Topic>`, `.../<Character>/Barks`.
- **NPC conversation**: conversation Actor = Player(1), Conversant = NPC actor. An NPC line's `E(actor=npc)`, a player choice `E(actor=PLAYER, menu=...)`. Tutorial/thought barks are player lines wrapped in `<i>…</i>`.
- **The Player actor's Display Name** is "Jaden Hayes"; dialogue variables `FirstName` (**has a trailing space**: "Jaden ") and `LastName`. Scenes with another protagonist use `PlayerProfileApplier` — don't edit the Player actor.
- **Existing actors** (name → id): Player 1, Owen 10, b3lt 11, Guardian 12, Sentinel 13, Kipling 14, Melina 15, Milo 17, Entity 18, Wolvolin 20, Hans 21, Sound Pole 22, Laborer 23, Null 25, Notice Board 26, Hank Alers 27, Thorvald 29, Ashland 30, The Hermit 31, Storm Caller 33, Stranger 35, Itri 36, Box 37, Scholar 38, Dr. Lucia Kress 39, Apostate Agent 40 (+ any added since: use `d.actor_ids`). `DialogueActor.actor` on an NPC must equal the actor *Name*.
- **Branch with `cond=` Lua**; START links are tried in order. Common conditions: `getBeatProgress(0) >= 5`, `hasOutcome("id")`, `checkOutcome("id","value")`, `hasSelfAlignment()`, `Dialog[thisID].SimStatus ~= "WasDisplayed"` (one-shot options), `CurrentQuestState("Q") == "active"`.
- **Choice consequences with `script=`**: `recordOutcome("id","value")`, `updateAlignment(purpose, affinity)` (±10 steps; purpose +Self/−Community, affinity +Order/−Freedom), `SetQuestState("Q","active")`, `SetQuestEntryState("Q", n, "success")`, plus any function registered in `MyLuaFunctions.cs`.
- **Outcome flags are strings and write-once** (`NotableOutcomesTracker`): pick explicit values (`"gaveAll"`, `"bartered"`) rather than 0/1 and don't plan to overwrite. They're saved (ES3 `NotableOutcomes`) and readable from C# via `NotableOutcomesTracker.instance`.
- **Quests** are Items with `Is Item = FALSE`, States `unassigned/active/success/failure`; entries are sub-objectives. The objective alert fires only when code/dialogue changes the state explicitly.
- **Text**: placeholder lines are fine; the user rewrites them. Keep line length reasonable (lines show in a subtitle panel). Dialogue is English only (UI strings are in `UI Translation Table.asset`).
- `[lua(getKey('Interact'))]` inserts the user's current binding (valid names: see scene-barks-and-beats).

## Validation
`validate_dialogue.py "<prefix>"` parses the whole DB with PyYAML (install with `pip install pyyaml --target <tmp>`, set `PYTHONPATH`), checks unique ids and, for conversations whose title starts with the prefix, that every link and actor resolves. It does **not** check Lua syntax: eyeball conditions against `MyLuaFunctions.cs` signatures.

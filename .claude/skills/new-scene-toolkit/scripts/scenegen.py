"""Scene generator library: builds a Unity scene (text YAML) from _TEMPLATE SCENE.unity.

Usage pattern (see example_scene.py):
    b = SceneBuilder("Act2_Foo")            # copies the template in memory
    b.register_prefab("npc", "Characters/Template_Non Combat Char.prefab")
    ... place things with b.inst / b.npc / b.bark / b.beat_trigger / b.plain_go ...
    b.set_tilemap("Tilemap_GROUND", cells, tiles)
    b.finalize(write=True)                  # integrity checks, then writes the scene (+ .meta if new)

Everything new is APPENDED to the template as whole blocks; only tilemap data and the Player start are edited in place.
New fileIDs are allocated from 7_000_000_000_001 upward (checked against existing anchors in finalize()).
"""
import os, re, uuid
import _paths
import prefabinfo
import tiles as T

TILE = 1.5  # world units per tile (every tilemap object in the template has scale 1.5)

# ---- project facts (stable ids from the shipped prefabs; see SKILL.md for how they were found)
NPC_PREFAB = "Characters/Template_Non Combat Char.prefab"      # the TALKABLE base prefab (the "NO SPEAK Variant" has variant-derived ids: don't use)
NPC_NCC = "2673823558022642480"        # NonCombatChar
NPC_TRIGGER = "7594462463266746330"    # DialogueSystemTrigger (trigger 32 = OnUse)
NPC_ACTOR = "2745346963984961871"      # DialogueActor
NPC_INTERACTABLE = "1542847629374921908"
NCC_SCRIPT = "191f700562f1bff4e9b7089520219976"
DST_SCRIPT = "c593457cd8105e148906690e1707c592"   # DialogueSystemTrigger
DB_GUID = "702dd648f5775bd4695265d3f31568c0"      # Dialogue Database.asset
BEATTRIGGER_PREFAB = "Game Management/Beat Trigger.prefab"
BT_COLLIDER = "975367626764123389"
BT_COMPONENT = "7194682196334299099"
PLAYER_GUID = "c81ad2288b452bd4fa6fbd6dd13e34de"
PLAYER_INSTANCE = "8518940669788288203"            # the Player PrefabInstance already in the template
PLAYER_TRANSFORM_STRIPPED = 370354940              # stripped Player root *Transform* already in the template (what barkers must point at)
NPC_HIGHLIGHT_TRIGGER = "7641866334711932886"      # HighlightTrigger2D on the NPC prefab
NPC_HIGHLIGHT_EFFECT = "7220398518399332031"       # HighlightEffect2D on the NPC prefab
OLDTOWN_BEAT_MANAGER = "Game Management/Beat Managers/Oldtown Act 1 Beat Manager.prefab"
OLDTOWN_BM_SCRIPT = "40f2059565a151448b1d2f1ab1f95c47"

LOOKS = {"none": 0, "citizen": 1, "poor": 2, "wolf": 3, "guardian": 4, "sentinel": 5, "scholar": 6}  # NonCombatChar.templateLook


def fnum(v):
    s = ("%.4f" % v).rstrip("0").rstrip(".")
    return s if s not in ("-0", "") else "0"


class Inst:
    """One PrefabInstance being built. Use .mod() for property overrides, .go_ref()/.t_ref()/.mb_ref() for scene refs into it."""

    def __init__(self, b, key, name, tx, ty, parent_t=0, active=True):
        self.b, self.key, self.p = b, key, b.pf[key]
        self.id = b.nid()
        self.mods, self.refs, self.parent_t = [], {}, parent_t
        self.removed = []   # component fileIDs (in the source prefab) to remove from this instance
        self.mod(self.p["rootT"], "m_LocalPosition.x", fnum(tx * TILE))
        self.mod(self.p["rootT"], "m_LocalPosition.y", fnum(ty * TILE))
        self.mod(self.p["rootT"], "m_LocalPosition.z", "0")
        self.mod(self.p["rootGO"], "m_Name", name)
        if not active:
            self.mod(self.p["rootGO"], "m_IsActive", "0")

    def mod(self, target, path, value, ref=0):
        self.mods.append((target, path, value, ref))

    # Scene objects can't point into a prefab instance directly: they point at a "stripped" stand-in block.
    def go_ref(self):
        if "go" not in self.refs:
            self.refs["go"] = self.b.stripped(1, "GameObject", self.p["rootGO"], self.p["guid"], self.id)
        return self.refs["go"]

    def t_ref(self):
        if "t" not in self.refs:
            self.refs["t"] = self.b.stripped(4, "Transform", self.p["rootT"], self.p["guid"], self.id)
        return self.refs["t"]

    def mb_ref(self, src_mb, script_guid):
        k = "mb" + str(src_mb)
        if k not in self.refs:
            extra = ("  m_GameObject: {fileID: 0}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n"
                     "  m_Script: {fileID: 11500000, guid: %s, type: 3}\n  m_Name: \n  m_EditorClassIdentifier: \n" % script_guid)
            self.refs[k] = self.b.stripped(114, "MonoBehaviour", src_mb, self.p["guid"], self.id, extra)
        return self.refs[k]

    def emit(self):
        g = self.p["guid"]
        lines = ["--- !u!1001 &%d" % self.id, "PrefabInstance:", "  m_ObjectHideFlags: 0", "  serializedVersion: 2",
                 "  m_Modification:", "    m_TransformParent: {fileID: %d}" % self.parent_t, "    m_Modifications:"]
        for target, path, value, ref in self.mods:
            lines += ["    - target: {fileID: %s, guid: %s, type: 3}" % (target, g), "      propertyPath: %s" % path,
                      "      value: %s" % value, "      objectReference: {fileID: %d}" % ref]
        if self.removed:
            lines.append("    m_RemovedComponents:")
            lines += ["    - {fileID: %s, guid: %s, type: 3}" % (c, g) for c in self.removed]
        else:
            lines.append("    m_RemovedComponents: []")
        lines.append("  m_SourcePrefab: {fileID: 100100000, guid: %s, type: 3}" % g)
        self.b.add("\n".join(lines))


class SceneBuilder:
    def __init__(self, scene_name, template="_TEMPLATE SCENE.unity", out_dir=None):
        self.assets = _paths.ASSETS
        self.scene_name = scene_name
        self.template_path = os.path.join(self.assets, "Scenes", template)
        self.out_path = os.path.join(out_dir or os.path.join(self.assets, "Scenes"), scene_name + ".unity")
        self.scene = open(self.template_path, encoding="utf8").read()
        self._next = 7000000000001
        self.blocks, self.instances, self.pf = [], [], {}
        self.register_prefab("npc", NPC_PREFAB)
        self.register_prefab("beattrigger", BEATTRIGGER_PREFAB)

    # ---------------------------------------------------------------- low level
    def nid(self):
        v = self._next
        self._next += 1
        return v

    def add(self, block):
        self.blocks.append(block if block.endswith("\n") else block + "\n")

    def register_prefab(self, key, rel_path):
        """rel_path is relative to Assets/Prefabs. Must be a plain prefab with a real root Transform."""
        meta, rt, rgo, comps = prefabinfo.info(rel_path)
        self.pf[key] = {"guid": meta, "rootT": rt, "rootGO": rgo, "comps": dict((n, c) for c, n in comps)}

    def register_generated_prefab(self, key, guid, root_t, root_go):
        """for a prefab this run wrote itself (e.g. a beat manager) whose ids are already known"""
        self.pf[key] = {"guid": guid, "rootT": root_t, "rootGO": root_go, "comps": {}}

    def stripped(self, kind_id, kind_name, src_id, guid, pi_id, extra=""):
        i = self.nid()
        self.add("--- !u!%s &%d stripped\n%s:\n  m_CorrespondingSourceObject: {fileID: %s, guid: %s, type: 3}\n"
                 "  m_PrefabInstance: {fileID: %d}\n  m_PrefabAsset: {fileID: 0}\n%s" % (kind_id, i, kind_name, src_id, guid, pi_id, extra))
        return i

    def inst(self, key, name, tx, ty, parent_t=0, active=True):
        i = Inst(self, key, name, tx, ty, parent_t, active)
        self.instances.append(i)
        return i

    # ---------------------------------------------------------------- plain (non-prefab) objects
    def plain_go(self, name, tx, ty, components=(), active=True, parent_t=0):
        """GameObject + Transform + the given component blocks (see box_collider()/mono()). Returns (go_id, transform_id)."""
        go, tr = self.nid(), self.nid()
        cids = [self.nid() for _ in components]
        lines = ["--- !u!1 &%d" % go, "GameObject:", "  m_ObjectHideFlags: 0", "  m_CorrespondingSourceObject: {fileID: 0}",
                 "  m_PrefabInstance: {fileID: 0}", "  m_PrefabAsset: {fileID: 0}", "  serializedVersion: 6", "  m_Component:",
                 "  - component: {fileID: %d}" % tr] + ["  - component: {fileID: %d}" % c for c in cids] + [
                 "  m_Layer: 0", "  m_Name: %s" % name, "  m_TagString: Untagged", "  m_Icon: {fileID: 0}", "  m_NavMeshLayer: 0",
                 "  m_StaticEditorFlags: 0", "  m_IsActive: %d" % (1 if active else 0)]
        self.add("\n".join(lines))
        self.add("\n".join(self._transform(tr, go, tx, ty, parent_t, [])))
        for cid, body in zip(cids, components):
            self.add(body.replace("@ID@", str(cid)).replace("@GO@", str(go)))
        return go, tr

    def _transform(self, tr, go, tx, ty, parent_t, children):
        t = ["--- !u!4 &%d" % tr, "Transform:", "  m_ObjectHideFlags: 0", "  m_CorrespondingSourceObject: {fileID: 0}",
             "  m_PrefabInstance: {fileID: 0}", "  m_PrefabAsset: {fileID: 0}", "  m_GameObject: {fileID: %d}" % go,
             "  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}", "  m_LocalPosition: {x: %s, y: %s, z: 0}" % (fnum(tx * TILE), fnum(ty * TILE)),
             "  m_LocalScale: {x: 1, y: 1, z: 1}"]
        t += (["  m_Children:"] + ["  - {fileID: %d}" % k for k in children]) if children else ["  m_Children: []"]
        t += ["  m_Father: {fileID: %d}" % parent_t, "  m_RootOrder: 0", "  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}"]
        return t

    @staticmethod
    def box_collider(w_tiles, h_tiles, trigger=True):
        return ("--- !u!61 &@ID@\nBoxCollider2D:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n"
                "  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: @GO@}\n  m_Enabled: 1\n  m_Density: 1\n  m_Material: {fileID: 0}\n"
                "  m_IsTrigger: %d\n  m_UsedByEffector: 0\n  m_UsedByComposite: 0\n  m_Offset: {x: 0, y: 0}\n  m_SpriteTilingProperty:\n"
                "    border: {x: 0, y: 0, z: 0, w: 0}\n    pivot: {x: 0, y: 0}\n    oldSize: {x: 0, y: 0}\n    newSize: {x: 0, y: 0}\n"
                "    adaptiveTilingThreshold: 0\n    drawMode: 0\n    adaptiveTiling: 0\n  m_AutoTiling: 0\n  serializedVersion: 2\n"
                "  m_Size: {x: %s, y: %s}\n  m_EdgeRadius: 0\n" % (1 if trigger else 0, fnum(w_tiles * TILE), fnum(h_tiles * TILE)))

    @staticmethod
    def mono(script_guid, fields=""):
        """fields = already-indented YAML lines ("  fieldName: value\\n"). Strings with leading/trailing spaces must be avoided."""
        return ("--- !u!114 &@ID@\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n"
                "  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: @GO@}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n"
                "  m_Script: {fileID: 11500000, guid: %s, type: 3}\n  m_Name: \n  m_EditorClassIdentifier: \n%s" % (script_guid, fields))

    def group(self, name, member_factory, active=True):
        """Empty parent GO holding prefab-instance children. member_factory(parent_transform_id) -> list[Inst]. Returns (go_id, transform_id, members)."""
        tr, go = self.nid(), self.nid()
        members = member_factory(tr)
        kids = [m.t_ref() for m in members]
        self.add("\n".join(["--- !u!1 &%d" % go, "GameObject:", "  m_ObjectHideFlags: 0", "  m_CorrespondingSourceObject: {fileID: 0}",
                            "  m_PrefabInstance: {fileID: 0}", "  m_PrefabAsset: {fileID: 0}", "  serializedVersion: 6", "  m_Component:",
                            "  - component: {fileID: %d}" % tr, "  m_Layer: 0", "  m_Name: %s" % name, "  m_TagString: Untagged",
                            "  m_Icon: {fileID: 0}", "  m_NavMeshLayer: 0", "  m_StaticEditorFlags: 0", "  m_IsActive: %d" % (1 if active else 0)]))
        self.add("\n".join(self._transform(tr, go, 0, 0, 0, kids)))
        return go, tr, members

    # ---------------------------------------------------------------- gameplay helpers
    def player_transform_ref(self):
        """Barkers (DialogueSystemTrigger.barker) are TRANSFORM fields: a GameObject ref reads as 'doesn't exist anymore'.
        The template already contains a stripped Player Transform; reuse it."""
        return PLAYER_TRANSFORM_STRIPPED

    def npc(self, name, tx, ty, look="none", actor="", conv="", direction=3, talk=True, bark_only=False, parent_t=0, active=True):
        """NPC from Template_Non Combat Char. actor = Dialogue DB actor Name; conv = conversation title (OnUse).
        talk=False  -> ambient NPC: the talk components are REMOVED (m_RemovedComponents), exactly like the project's own NO SPEAK variant.
                       (Never just disable Interactable: its OnTriggerExit2D still fires and hits a null outline -> NullReferenceException.)
        bark_only=True (with talk=False) -> can't be interacted with but keeps DialogueActor + DialogueSystemTrigger so it can be a bark's barker (needs the bark UI)."""
        i = self.inst("npc", name, tx, ty, parent_t=parent_t, active=active)
        i.mod(NPC_NCC, "templateLook", str(LOOKS[look]))
        i.mod(NPC_NCC, "startingDirection", str(direction))
        if talk and conv:
            if actor:
                i.mod(NPC_ACTOR, "actor", actor)
            i.mod(NPC_TRIGGER, "trigger", "32")       # DialogueTriggerEvent.OnUse
            i.mod(NPC_TRIGGER, "conversation", conv)
        elif bark_only:
            if actor:
                i.mod(NPC_ACTOR, "actor", actor)
            i.removed = [NPC_INTERACTABLE, NPC_HIGHLIGHT_TRIGGER, NPC_HIGHLIGHT_EFFECT]
        else:
            i.removed = [NPC_TRIGGER, NPC_ACTOR, NPC_INTERACTABLE, NPC_HIGHLIGHT_TRIGGER, NPC_HIGHLIGHT_EFFECT]
        return i

    @staticmethod
    def bark_fields(conversation, barker_transform_ref, db_guid=DB_GUID):
        """DialogueSystemTrigger that barks a conversation when its object is enabled (trigger 64 = OnEnable).
        barker_transform_ref must be a TRANSFORM id: player_transform_ref(), or npc_inst.t_ref() (that NPC needs DialogueActor => talk=True or bark_only=True)."""
        return ("  trigger: 64\n  condition:\n    luaConditions: []\n    questConditions: []\n    acceptedTags: []\n    acceptedGameObjects: []\n"
                "    luaWizardIndex: -1\n    lastEvaluationValue: 0\n  setQuestState: 0\n  questName: \n  questState: 0\n  setQuestEntryState: 0\n"
                "  questEntryNumber: 1\n  questEntryState: 0\n  luaCode: \n  sequence: \n  sequenceSpeaker: {fileID: 0}\n  sequenceListener: {fileID: 0}\n"
                "  waitOneFrameOnStartOrEnable: 1\n  alertMessage: \n  textTable: {fileID: 0}\n  alertDuration: 0\n  sendMessages: []\n"
                "  barkSource: 1\n  barkConversation: %s\n  barkText: \n  barkTextSequence: \n  barker: {fileID: %d}\n  barkTarget: {fileID: 0}\n"
                "  barkOrder: 2\n  allowBarksDuringConversations: 0\n  skipBarkIfNoValidEntries: 0\n  cacheBarkLines: 0\n  conversation: \n"
                "  conversationConversant: {fileID: 0}\n  conversationActor: {fileID: 0}\n  startConversationEntryID: 0\n  exclusive: 0\n"
                "  skipIfNoValidEntries: 0\n  stopConversationOnTriggerExit: 0\n  stopConversationIfTooFar: 0\n  maxConversationDistance: 5\n"
                "  monitorConversationDistanceFrequency: 1\n  showCursorDuringConversation: 0\n  pauseGameDuringConversation: 0\n"
                "  setActiveActions: []\n  setEnabledActions: []\n  setAnimatorStateActions: []\n  onExecute:\n    m_PersistentCalls:\n      m_Calls: []\n"
                "  useConversationTitlePicker: 1\n  useBarkTitlePicker: 1\n  useQuestNamePicker: 1\n"
                "  selectedDatabase: {fileID: 11400000, guid: %s, type: 2}\n" % (conversation, barker_transform_ref, db_guid))

    def beat_trigger(self, name, tx, ty, w, h, set_active_go=None, method=None, target_mb=None, target_type=None,
                     one_shot=True, active=True, on_enable=False):
        """Beat Trigger prefab instance with ONE UnityEvent call:
             set_active_go=<GO id>             -> GameObject.SetActive(true) on that object (tutorial barks)
             method="foo", target_mb=<MB ref>, target_type="MyBeatManager, Assembly-CSharp" -> calls MyBeatManager.foo()
           one_shot=True auto-deactivates after the first entry EVEN IF the target method rejects the call (guards!). Use False for guarded methods.
           on_enable=True makes it fire when the (initially inactive) object is enabled, e.g. LootPlacer.objsToEnableOnPickup."""
        i = self.inst("beattrigger", name, tx, ty, active=active)
        i.mod(BT_COLLIDER, "m_Size.x", fnum(w * TILE))
        i.mod(BT_COLLIDER, "m_Size.y", fnum(h * TILE))
        i.mod(BT_COMPONENT, "autoDeactivate", "1" if one_shot else "0")
        if on_enable:
            i.mod(BT_COMPONENT, "type", "2")
        base = "eventToTrigger.m_PersistentCalls.m_Calls.Array.data[0]."
        if set_active_go is not None:
            i.mod(BT_COMPONENT, base + "m_Target", "", set_active_go)
        else:
            i.mod(BT_COMPONENT, base + "m_Target", "", target_mb)
            i.mod(BT_COMPONENT, base + "m_TargetAssemblyTypeName", target_type)
            i.mod(BT_COMPONENT, base + "m_MethodName", method)
            i.mod(BT_COMPONENT, base + "m_Mode", "1")
        return i

    def bark_with_trigger(self, label, bark_tx, bark_ty, conversation, tx, ty, w, h, barker_transform_ref=None):
        """inactive bark object + one-shot Beat Trigger that enables it. barker (a TRANSFORM ref) defaults to the player."""
        barker = barker_transform_ref or self.player_transform_ref()
        go, _ = self.plain_go("Bark - " + label, bark_tx, bark_ty, [self.mono(DST_SCRIPT, self.bark_fields(conversation, barker))], active=False)
        return self.beat_trigger("Bark Trigger - " + label, tx, ty, w, h, set_active_go=go)

    # ---------------------------------------------------------------- beat manager prefab (copy of an existing one with a new script)
    def make_beat_manager_prefab(self, display_name, script_guid, out_prefab_path):
        """Writes <out_prefab_path> (+ .meta) cloned from the Oldtown beat manager prefab with fresh ids/ES3 ids.
        Returns (prefab_guid, root_transform_id, root_go_id, mono_id) and registers it as pf['bm']."""
        import random
        t = open(os.path.join(self.assets, "Prefabs", OLDTOWN_BEAT_MANAGER), encoding="utf8").read()
        ids = {}
        for old in ("397866437605075692", "1805992341928376859", "5661070181192412450", "666868793959714028", "7180649849208787506"):
            ids[old] = str(random.randint(10 ** 17, 9 * 10 ** 18))
            t = t.replace(old, ids[old])
        t = t.replace(OLDTOWN_BM_SCRIPT, script_guid)
        t = t.replace("m_Name: Oldtown Act 1 Beat Manager", "m_Name: " + display_name)
        t = t.replace("  beatMatrix: \n  skipToEnd: 0\n", "  beatMatrix: \n")  # skipToEnd belongs to the Oldtown subclass
        t = re.sub(r"prefabId: \d+", "prefabId: %d" % random.randint(10 ** 17, 9 * 10 ** 18), t)
        t = re.sub(r"_Values: [0-9a-f]+", "_Values: " + "".join("%016x" % random.getrandbits(63) for _ in range(5)), t)
        guid = uuid.uuid4().hex
        open(out_prefab_path, "w", encoding="utf8", newline="\n").write(t)
        open(out_prefab_path + ".meta", "w", encoding="utf8", newline="\n").write(
            "fileFormatVersion: 2\nguid: %s\nPrefabImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n" % guid)
        root_t, root_go, mono_id = ids["1805992341928376859"], ids["397866437605075692"], ids["5661070181192412450"]
        self.register_generated_prefab("bm", guid, root_t, root_go)
        return guid, root_t, root_go, mono_id

    # ---------------------------------------------------------------- tilemaps / player
    def tilemap_fileid(self, go_name):
        for blk in T.split_blocks(self.scene):
            m = re.match(r"!u!1 &(\d+)\n", blk)
            if m and re.search(r"m_Name: %s\n" % re.escape(go_name), blk):
                for c in re.findall(r"- component: \{fileID: (\d+)\}", blk):
                    if re.search(r"--- !u!1839735485 &%s\n" % c, self.scene):
                        return c
        raise RuntimeError("no Tilemap on object " + go_name)

    def set_tilemap(self, go_name, cells, tile_specs):
        """cells: {(x, y): tile_key}; tile_specs: {tile_key: T.harvest(...) result}. Replaces that tilemap's tile data."""
        body, origin, size = T.tilemap_body(cells, tile_specs)
        self.scene = T.replace_tilemap_block(self.scene, self.tilemap_fileid(go_name), body, origin, size)
        return origin, size

    def set_player_start(self, tx, ty):
        ps = self.scene.index("--- !u!1001 &%s\n" % PLAYER_INSTANCE)
        pe = self.scene.index("\n--- ", ps + 10) + 1
        blk = self.scene[ps:pe]
        for axis, val in (("x", tx * TILE), ("y", ty * TILE)):
            blk, n = re.subn(r"(propertyPath: m_LocalPosition\.%s\n      value: )[^\n]*" % axis, r"\g<1>%s" % fnum(val), blk, count=1)
            assert n == 1, "player position modification not found in template"
        self.scene = self.scene[:ps] + blk + self.scene[pe:]

    # ---------------------------------------------------------------- output
    def finalize(self, write=True):
        for i in self.instances:
            i.emit()
        scene = self.scene if self.scene.endswith("\n") else self.scene + "\n"
        new_part = "".join(self.blocks)
        scene += new_part
        anchors = re.findall(r"(?m)^--- !u!\d+ &(\d+)", scene)
        assert len(anchors) == len(set(anchors)), "duplicate fileID anchors"
        aset = set(anchors)
        bad = sorted({m.group(1) for m in re.finditer(r"\{fileID: (\d+)\}", new_part) if m.group(1) != "0" and m.group(1) not in aset})
        assert not bad, "unresolved fileID references in new blocks: %s" % bad[:10]
        if write:
            os.makedirs(os.path.dirname(self.out_path), exist_ok=True)
            open(self.out_path, "w", encoding="utf8", newline="\n").write(scene)
            meta = self.out_path + ".meta"
            if not os.path.exists(meta):
                open(meta, "w", encoding="utf8", newline="\n").write(
                    "fileFormatVersion: 2\nguid: %s\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n" % uuid.uuid4().hex)
        return scene


def reachable(blocked, ground, start, targets):
    """BFS over tile cells. blocked/ground are sets/dicts of (x,y). Returns {target_name: bool}."""
    from collections import deque
    seen, q = {start}, deque([start])
    while q:
        x, y = q.popleft()
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            n = (x + dx, y + dy)
            if n not in seen and n not in blocked and n in ground:
                seen.add(n)
                q.append(n)
    return dict((name, p in seen) for name, p in targets.items())


def rect_outline(cells, x0, y0, x1, y1, key, gaps=()):
    for x in range(x0, x1 + 1):
        for y in range(y0, y1 + 1):
            if (x in (x0, x1) or y in (y0, y1)) and (x, y) not in gaps:
                cells[(x, y)] = key


def hline(cells, x0, x1, y, key, gaps=()):
    for x in range(x0, x1 + 1):
        if (x, y) not in gaps:
            cells[(x, y)] = key


def vline(cells, x, y0, y1, key, gaps=()):
    for y in range(y0, y1 + 1):
        if (x, y) not in gaps:
            cells[(x, y)] = key

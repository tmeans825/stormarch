"""Authoring helpers for the Pixel Crushers Dialogue Database (Assets/Plugins/Pixel Crushers/Dialogue System/Dialogue Database.asset).

    d = DialogueAuthor()                                  # reads the DB, learns the next free ids
    guide = d.actor("Guide", "A helpful local")             # -> actor id (reuse an existing actor with d.actor_id("Guardian"))
    d.quest("Find the Key", "desc", "done text", ["step 1", "step 2"])
    d.bark("Act2/Tutorial/Move", "Move with W, A, S and D.")                          # player thought bubble
    d.conversation("Act2/Guide/Intro", guide, [start(1), E(1, guide, "Hi.", links=[2]), E(2, PLAYER, "Hey.")])
    d.save(marker="Act2/")                                # idempotent: refuses if any title starting with marker already exists

Entry conventions: START is E(0, ...) via start(*links). NPC lines: E(id, npc_actor, text). Player choices: E(id, PLAYER, text, menu="button text").
Lua in cond= / script= may use: getBeatProgress(line), recordOutcome(id,"value"), checkOutcome(id,"value"), hasOutcome(id), updateAlignment(purpose, affinity),
getKey('Interact') inside [lua(...)] in text, and any function registered in MyLuaFunctions.cs. Outcomes are STRING values and write-once.
"""
import os, re
import _paths

DB_PATH = os.path.join(_paths.ASSETS, "Plugins", "Pixel Crushers", "Dialogue System", "Dialogue Database.asset")
PLAYER = 1


def q(s):
    """YAML-safe scalar (single-quote anything with special characters)"""
    if s == "":
        return ""
    if re.search(r"[:#\[\]{}&*!|>'\"%@`,?]", s) or s != s.strip() or s[0] in "-?:":
        return "'" + s.replace("'", "''") + "'"
    return s


class E:
    def __init__(self, id_, actor, text, menu="", cond="", script="", links=(), conversant=PLAYER):
        self.id, self.actor, self.text, self.menu, self.cond, self.script = id_, actor, text, menu, cond, script
        self.links, self.conversant = list(links), conversant


def start(*links):
    return E(0, PLAYER, "", links=links, conversant=-1)


def _field(title, value, ts="CustomFieldType_Text", type_=0):
    return "      - title: %s\n        value: %s\n        type: %d\n        typeString: %s\n" % (title, value, type_, ts)


def _entry(conv_id, e, row):
    is_start = e.id == 0
    s = "    - id: %d\n      fields:\n" % e.id
    s += _field("Title", "START" if is_start else "New Dialogue Entry", "")
    s += _field("Pictures", "'[]'", "CustomFieldType_Files", 3) + _field("Description", "", "")
    s += _field("Actor", e.actor, "CustomFieldType_Actor", 5) + _field("Conversant", e.conversant, "CustomFieldType_Actor", 5)
    s += _field("Menu Text", q(e.menu), "") + _field("Dialogue Text", q(e.text), "") + _field("Parenthetical", "", "")
    s += _field("Audio Files", "'[]'", "CustomFieldType_Files", 3) + _field("Video File", "", "")
    s += _field("Sequence", "None()" if is_start else "", "") + _field("canvasRect", "", "") + _field("EventGuid", "", "")
    s += "      conversationID: %d\n      isRoot: 0\n      isGroup: 0\n      nodeColor: \n      delaySimStatus: 0\n" % conv_id
    s += "      falseConditionAction: Block\n      conditionPriority: 2\n"
    if e.links:
        s += "      outgoingLinks:\n"
        for l in e.links:
            s += ("      - originConversationID: %d\n        originDialogueID: %d\n        destinationConversationID: %d\n"
                  "        destinationDialogueID: %d\n        isConnector: 0\n        priority: 2\n" % (conv_id, e.id, conv_id, l))
    else:
        s += "      outgoingLinks: []\n"
    s += "      conditionsString: %s\n      userScript: %s\n" % (q(e.cond), q(e.script))
    s += "      onExecute:\n        m_PersistentCalls:\n          m_Calls: []\n"
    s += "      canvasRect:\n        serializedVersion: 2\n        x: %d\n        y: %d\n        width: 160\n        height: 30\n" % (30 + 170 * (row % 4), 20 + 90 * (row // 4))
    return s


class DialogueAuthor:
    def __init__(self, path=DB_PATH):
        self.path = path
        self.txt = open(path, encoding="utf8", newline="").read()
        assert "\r" not in self.txt, "database has CRLF line endings; adjust the writer"
        self.actor_ids = self._ids("actors")
        self.next_actor = max(self.actor_ids.values()) + 1
        self.next_item = max(self._ids("items").values(), default=0) + 1
        self.next_conv = max(self._ids("conversations", names=False)) + 1
        self.actors_txt, self.quests_txt, self.convs_txt, self.titles = "", "", "", []

    def _section(self, name):
        lines = self.txt.split("\n")
        start = next(i for i, l in enumerate(lines) if l == "  %s:" % name)
        end = next(i for i in range(start + 1, len(lines)) if re.match(r"^  \w+:", lines[i]))
        return lines[start:end]

    def _ids(self, section, names=True):
        lines = self._section(section)
        ids = [int(m.group(1)) for l in lines for m in [re.match(r"^  - id: (\d+)$", l)] if m]
        if not names:
            return ids
        out, cur = {}, None
        for i, l in enumerate(lines):
            m = re.match(r"^  - id: (\d+)$", l)
            if m:
                cur = int(m.group(1))
            if l.strip() == "- title: Name" and cur is not None:
                out[lines[i + 1].split("value:", 1)[1].strip()] = cur
        return out

    def actor_id(self, name):
        return self.actor_ids[name]

    def actor(self, name, description=""):
        if name in self.actor_ids:
            return self.actor_ids[name]
        i = self.next_actor
        self.next_actor += 1
        self.actor_ids[name] = i
        self.actors_txt += ("  - id: %d\n    fields:\n    - title: Name\n      value: %s\n      type: 0\n      typeString: \n"
                            "    - title: Pictures\n      value: '[]'\n      type: 3\n      typeString: CustomFieldType_Files\n"
                            "    - title: Description\n      value: %s\n      type: 0\n      typeString: \n"
                            "    - title: IsPlayer\n      value: FALSE\n      type: 2\n      typeString: CustomFieldType_Boolean\n"
                            "    - title: Display Name\n      value: %s\n      type: 0\n      typeString: \n"
                            "    portrait: {fileID: 0}\n    spritePortrait: {fileID: 0}\n    alternatePortraits: []\n    spritePortraits: []\n" % (i, q(name), q(description), q(name)))
        return i

    def quest(self, name, description, success, entries, group="PRIMARY"):
        i = self.next_item
        self.next_item += 1
        s = ("  - id: %d\n    fields:\n    - title: Name\n      value: %s\n      type: 0\n      typeString: \n"
             "    - title: Pictures\n      value: '[]'\n      type: 3\n      typeString: CustomFieldType_Files\n"
             "    - title: Description\n      value: %s\n      type: 0\n      typeString: \n"
             "    - title: Is Item\n      value: FALSE\n      type: 2\n      typeString: CustomFieldType_Boolean\n"
             "    - title: Success Description\n      value: %s\n      type: 0\n      typeString: \n"
             "    - title: Failure Description\n      value: \n      type: 0\n      typeString: \n"
             "    - title: State\n      value: unassigned\n      type: 0\n      typeString: \n"
             "    - title: Group\n      value: %s\n      type: 0\n      typeString: \n"
             "    - title: Entry Count\n      value: %d\n      type: 1\n      typeString: CustomFieldType_Number\n" % (i, q(name), q(description), q(success), group, len(entries)))
        for n, e in enumerate(entries, 1):
            s += ("    - title: Entry %d State\n      value: unassigned\n      type: 0\n      typeString: \n"
                  "    - title: Entry %d\n      value: %s\n      type: 0\n      typeString: \n" % (n, n, q(e)))
        s += ("    - title: Trackable\n      value: FALSE\n      type: 2\n      typeString: CustomFieldType_Boolean\n"
              "    - title: Abandonable\n      value: FALSE\n      type: 2\n      typeString: CustomFieldType_Boolean\n")
        self.quests_txt += s
        return i

    def conversation(self, title, conversant, entries):
        cid = self.next_conv
        self.next_conv += 1
        self.titles.append(title)
        s = "  - id: %d\n    fields:\n" % cid
        s += "    - title: Title\n      value: %s\n      type: 0\n      typeString: CustomFieldType_Text\n" % title
        s += "    - title: Pictures\n      value: '[]'\n      type: 3\n      typeString: CustomFieldType_Files\n"
        s += "    - title: Description\n      value: \n      type: 0\n      typeString: CustomFieldType_Text\n"
        s += "    - title: Actor\n      value: %d\n      type: 5\n      typeString: CustomFieldType_Actor\n" % PLAYER
        s += "    - title: Conversant\n      value: %d\n      type: 5\n      typeString: CustomFieldType_Actor\n" % conversant
        s += ("    overrideSettings:\n      useOverrides: 0\n      overrideSubtitleSettings: 0\n      showNPCSubtitlesDuringLine: 1\n"
              "      showNPCSubtitlesWithResponses: 1\n      showPCSubtitlesDuringLine: 0\n      skipPCSubtitleAfterResponseMenu: 0\n"
              "      subtitleCharsPerSecond: 30\n      minSubtitleSeconds: 2\n      continueButton: 0\n      overrideSequenceSettings: 0\n"
              "      defaultSequence: \n      defaultPlayerSequence: \n      defaultResponseMenuSequence: \n      overrideInputSettings: 0\n"
              "      alwaysForceResponseMenu: 1\n      includeInvalidEntries: 0\n      responseTimeout: 0\n      cancelSubtitle:\n"
              "        key: 27\n        buttonName: \n      cancelConversation:\n        key: 27\n        buttonName: \n")
        s += "    nodeColor: \n    dialogueEntries:\n"
        for row, e in enumerate(entries):
            s += _entry(cid, e, row)
        s += "    canvasScrollPosition: {x: 0, y: 0}\n    canvasZoom: 1\n"
        self.convs_txt += s
        return cid

    def bark(self, title, text, speaker=PLAYER):
        """single-line bark. speaker=PLAYER renders as an italic thought; otherwise the actor id speaks it."""
        line = "<i>%s</i>" % text if speaker == PLAYER else text
        return self.conversation(title, -1 if speaker == PLAYER else speaker,
                                 [start(1), E(1, speaker, line, conversant=1 if speaker == PLAYER else PLAYER)])

    def save(self, marker):
        """Insert accumulated actors/quests/conversations. marker: title prefix used to refuse a double insert."""
        if marker in self.txt:
            raise SystemExit("already inserted (found %r in the database)" % marker)

        def insert_before(t, anchor, block):
            assert t.count(anchor) == 1, "anchor %r found %d times" % (anchor, t.count(anchor))
            i = t.index(anchor)
            return t[:i] + block + t[i:]
        t = self.txt
        if self.actors_txt:
            t = insert_before(t, "\n  items:\n", "\n" + self.actors_txt.rstrip("\n"))
        if self.quests_txt:
            t = insert_before(t, "\n  locations: []\n", "\n" + self.quests_txt.rstrip("\n"))
        if self.convs_txt:
            t = insert_before(t, "\n  syncInfo:\n", "\n" + self.convs_txt.rstrip("\n"))
        open(self.path, "w", encoding="utf8", newline="").write(t)
        print("inserted %d conversations" % len(self.titles))

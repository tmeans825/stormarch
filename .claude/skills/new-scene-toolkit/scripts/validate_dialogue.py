"""Validate the Dialogue Database after inserting content: parses as YAML, ids unique, every entry link and actor resolves.

    python validate_dialogue.py [title-prefix] [--db <path>]     e.g. "Act2/"  (only those conversations are link/actor checked; ids are checked for all)
"""
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import dialogue_lib

try:
    import yaml
except ImportError:
    sys.exit("PyYAML required: python -m pip install pyyaml --target <tmp dir> ; set PYTHONPATH to it")

argv = sys.argv[1:]
db_path = dialogue_lib.DB_PATH
if "--db" in argv:
    i = argv.index("--db")
    db_path = argv[i + 1]
    del argv[i:i + 2]
prefix = argv[0] if argv else ""
txt = open(db_path, encoding="utf8").read()
d = yaml.safe_load(txt.split("--- !u!114 &11400000\n", 1)[1])["MonoBehaviour"]
acts, items, convs = d["actors"], d["items"], d["conversations"]
print("actors %d, items %d, conversations %d" % (len(acts), len(items), len(convs)))
for name, lst in (("actors", acts), ("items", items), ("conversations", convs)):
    ids = [x["id"] for x in lst]
    assert len(ids) == len(set(ids)), "duplicate ids in " + name
actor_ids = {a["id"] for a in acts}
bad = 0
title = lambda c: next(f["value"] for f in c["fields"] if f["title"] == "Title")
checked = [c for c in convs if title(c).startswith(prefix)] if prefix else []
for c in checked:
    eids = {e["id"] for e in c["dialogueEntries"]}
    for e in c["dialogueEntries"]:
        if e["conversationID"] != c["id"]:
            print("WRONG conversationID", title(c), e["id"]); bad += 1
        if int(next(f["value"] for f in e["fields"] if f["title"] == "Actor")) not in actor_ids:
            print("UNKNOWN ACTOR", title(c), e["id"]); bad += 1
        for l in e["outgoingLinks"]:
            if l["destinationDialogueID"] not in eids:
                print("BAD LINK", title(c), e["id"], l["destinationDialogueID"]); bad += 1
print("checked %d conversations, %d problem(s)" % (len(checked), bad))
sys.exit(1 if bad else 0)

"""Validate a hand-generated Unity scene or prefab (text YAML) before opening it in Unity.

    python validate_scene.py <path.unity|path.prefab> [--ours-from-id 7000000000000]

Checks: header, every block parses as YAML (PyYAML), no duplicate anchors, every {fileID: N} in blocks we authored resolves
to an anchor, every PrefabInstance modification `target` and stripped m_CorrespondingSourceObject exists in its source prefab.
PyYAML is optional-but-recommended:  python -m pip install pyyaml --target <some temp dir>  and set PYTHONPATH to it.
"""
import os, re, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import _paths

OURS_MIN, OURS_MAX = 7000000000000, 7100000000000   # range SceneBuilder allocates from


def prefab_guid_map():
    g2p = {}
    for dp, dn, fn in os.walk(os.path.join(_paths.ASSETS, "Prefabs")):
        for f in fn:
            if f.endswith(".prefab.meta"):
                g = re.search(r"guid: (\w+)", open(os.path.join(dp, f), encoding="utf8").read()).group(1)
                g2p[g] = os.path.join(dp, f[:-5])
    return g2p


def main(path):
    txt = open(path, encoding="utf8").read()
    problems = []
    if not txt.startswith("%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n"):
        problems.append("bad file header")
    marks = [m.start() for m in re.finditer(r"(?m)^--- ", txt)] + [len(txt)]
    try:
        import yaml
    except ImportError:
        yaml = None
        print("NOTE: PyYAML not importable; skipping per-block YAML parse")
    seen, anchors_ours = {}, []
    blocks = []
    for a, b in zip(marks, marks[1:]):
        blk = txt[a:b]
        head, _, body = blk[4:].partition("\n")
        m = re.match(r"!u!(\d+) &(-?\d+)( stripped)?$", head)
        if not m:
            problems.append("bad block header: " + head[:60])
            continue
        if m.group(2) in seen:
            problems.append("duplicate anchor " + m.group(2))
        seen[m.group(2)] = m.group(1)
        blocks.append((m.group(1), m.group(2), head, blk))
        if yaml is not None:
            try:
                d = yaml.safe_load(body)
                if not isinstance(d, dict) or len(d) != 1:
                    problems.append("block is not a single-root mapping: " + head)
            except Exception as e:
                problems.append("YAML error in %s: %s" % (head, str(e)[:120]))
    aset = set(seen)
    g2p = prefab_guid_map()
    cache = {}

    def anchors(g):
        if g not in cache:
            cache[g] = set(re.findall(r"(?m)^--- !u!\d+ &(-?\d+)", open(g2p[g], encoding="utf8").read()))
        return cache[g]

    # Type checks for object-reference fields that are easy to get wrong (a ref to the wrong *kind* of object resolves fine in
    # YAML but Unity reports "The variable X doesn't exist anymore"). Class id 4 = Transform, 1 = GameObject.
    for kind, aid, head, blk in blocks:
        for m in re.finditer(r"(?m)^  barker: \{fileID: (-?\d+)\}", blk):   # DialogueSystemTrigger.barker is a Transform
            if m.group(1) != "0" and seen.get(m.group(1)) not in ("4", "224"):
                problems.append("barker in %s points at %s which is a class %s block, not a Transform" % (head, m.group(1), seen.get(m.group(1))))

    n_ours = 0
    for kind, aid, head, blk in blocks:
        if not (OURS_MIN <= int(aid) < OURS_MAX):
            continue
        n_ours += 1
        for m in re.finditer(r"\{fileID: (\d+)\}", blk):
            if m.group(1) != "0" and m.group(1) not in aset:
                problems.append("unresolved ref %s in %s" % (m.group(1), head))
        if kind == "1001":
            for t in re.finditer(r"target: \{fileID: (-?\d+), guid: (\w+), type: 3\}", blk):
                if t.group(2) in g2p and t.group(1) not in anchors(t.group(2)):
                    problems.append("PrefabInstance %s targets %s which is not in %s" % (aid, t.group(1), os.path.basename(g2p[t.group(2)])))
        if "stripped" in head:
            s = re.search(r"m_CorrespondingSourceObject: \{fileID: (-?\d+), guid: (\w+), type: 3\}", blk)
            if s and s.group(2) in g2p and s.group(1) not in anchors(s.group(2)):
                problems.append("stripped %s source %s not in %s" % (aid, s.group(1), os.path.basename(g2p[s.group(2)])))
    print("blocks: %d (authored by generator: %d)" % (len(blocks), n_ours))
    for p in problems[:40]:
        print("PROBLEM:", p)
    print("OK" if not problems else "%d problem(s)" % len(problems))
    return 0 if not problems else 1


if __name__ == "__main__":
    if len(sys.argv) < 2:
        sys.exit(__doc__)
    sys.exit(main(sys.argv[1]))

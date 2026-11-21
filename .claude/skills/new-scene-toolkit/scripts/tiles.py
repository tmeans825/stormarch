"""Tile harvesting + tilemap block writer. Reuses (tile asset, sprite, matrix) triples already painted in donor scenes."""
import re, os
from _paths import ASSETS as ROOT
IDENTITY = {"e00": "1", "e01": "0", "e02": "0", "e03": "0", "e10": "0", "e11": "1", "e12": "0", "e13": "0",
            "e20": "0", "e21": "0", "e22": "1", "e23": "0", "e30": "0", "e31": "0", "e32": "0", "e33": "1"}


def tile_name_to_guid():
    d = {}
    for dp, dn, fn in os.walk(os.path.join(ROOT, "Art", "Environments", "Tilemaps")):
        for f in fn:
            if f.endswith(".asset.meta"):
                t = open(os.path.join(dp, f), encoding="utf8").read()
                m = re.search(r"guid: (\w+)", t)
                if m:
                    d.setdefault(f[:-11], m.group(1))
    return d


def split_blocks(txt):
    return re.split(r"(?m)^--- ", txt)


def tilemap_blocks(scene_path):
    """yield (gameobject name, tilemap block text) for each Tilemap in a scene"""
    txt = open(scene_path, encoding="utf8").read()
    gos, tms = {}, []
    for b in split_blocks(txt):
        m = re.match(r"!u!(\d+) &(\d+)", b)
        if not m:
            continue
        if m.group(1) == "1":
            n = re.search(r"m_Name: (.*)", b)
            gos[m.group(2)] = n.group(1) if n else ""
        elif m.group(1) == "1839735485":
            tms.append(b)
    for b in tms:
        g = re.search(r"m_GameObject: \{fileID: (\d+)\}", b).group(1)
        yield gos.get(g, "?"), b


def parse_arrays(b):
    assets = re.findall(r"- m_RefCount: (\d+)\n    m_Data: (\{[^\n]*\})",
                        b.split("m_TileAssetArray:")[1].split("m_TileSpriteArray:")[0])
    sprites = re.findall(r"- m_RefCount: (\d+)\n    m_Data: (\{[^\n]*\})",
                         b.split("m_TileSpriteArray:")[1].split("m_TileMatrixArray:")[0])
    mats = []
    mtxt = b.split("m_TileMatrixArray:")[1].split("m_TileColorArray:")[0]
    for mm in re.finditer(r"- m_RefCount: (\d+)\n    m_Data:\n((?:      e\d\d: [^\n]*\n)+)", mtxt):
        mats.append(dict(re.findall(r"(e\d\d): ([^\n]*)", mm.group(2))))
    return assets, sprites, mats


def harvest(scene_file, tilemap_name, tile_name, name2guid):
    """most common (sprite ref, matrix) used for a given tile asset on a given tilemap of a donor scene"""
    guid = name2guid[tile_name]
    path = os.path.join(ROOT, "Scenes", scene_file)
    for tm_name, b in tilemap_blocks(path):
        if tm_name != tilemap_name:
            continue
        assets, sprites, mats = parse_arrays(b)
        tile_idx = [i for i, (rc, d) in enumerate(assets) if guid in d and int(rc) > 0]
        if not tile_idx:
            continue
        counts = {}
        for m in re.finditer(r"m_TileIndex: (\d+)\n      m_TileSpriteIndex: (\d+)\n      m_TileMatrixIndex: (\d+)", b):
            ti, si, mi = int(m.group(1)), int(m.group(2)), int(m.group(3))
            if ti in tile_idx:
                counts[(si, mi)] = counts.get((si, mi), 0) + 1
        if not counts:
            continue
        (si, mi), n = max(counts.items(), key=lambda kv: kv[1])
        return {"guid": guid, "sprite": sprites[si][1], "matrix": mats[mi] if mi < len(mats) else IDENTITY, "name": tile_name}
    raise RuntimeError("tile %s not found on %s in %s" % (tile_name, tilemap_name, scene_file))


def is_identity(m):
    return all(float(m[k]) == float(v) for k, v in IDENTITY.items())


def tilemap_body(cells, tiles):
    """cells: dict {(x,y): tile_key}; tiles: dict key -> harvested spec. Returns (body_text, origin, size)."""
    keys = sorted(set(cells.values()))
    asset_idx = {k: i for i, k in enumerate(keys)}
    mats = [IDENTITY]
    mat_idx = {}
    for k in keys:
        m = tiles[k]["matrix"]
        if is_identity(m):
            mat_idx[k] = 0
        else:
            mat_idx[k] = len(mats)
            mats.append(m)
    counts = {k: 0 for k in keys}
    out = ["  m_Tiles:"]
    xs = [c[0] for c in cells]
    ys = [c[1] for c in cells]
    for (x, y) in sorted(cells, key=lambda c: (c[1], c[0])):
        k = cells[(x, y)]
        counts[k] += 1
        i = asset_idx[k]
        out.append("  - first: {x: %d, y: %d, z: 0}" % (x, y))
        out.append("    second:")
        out.append("      serializedVersion: 2")
        out.append("      m_TileIndex: %d" % i)
        out.append("      m_TileSpriteIndex: %d" % i)
        out.append("      m_TileMatrixIndex: %d" % mat_idx[k])
        out.append("      m_TileColorIndex: 0")
        out.append("      m_TileObjectToInstantiateIndex: 65535")
        out.append("      m_AllTileFlags: 1073741825")
    out.append("  m_AnimatedTiles: {}")
    out.append("  m_TileAssetArray:")
    for k in keys:
        out.append("  - m_RefCount: %d" % counts[k])
        out.append("    m_Data: {fileID: 11400000, guid: %s, type: 2}" % tiles[k]["guid"])
    out.append("  m_TileSpriteArray:")
    for k in keys:
        out.append("  - m_RefCount: %d" % counts[k])
        out.append("    m_Data: %s" % tiles[k]["sprite"])
    out.append("  m_TileMatrixArray:")
    per_mat = [0] * len(mats)
    for k in keys:
        per_mat[mat_idx[k]] += counts[k]
    for i, m in enumerate(mats):
        out.append("  - m_RefCount: %d" % per_mat[i])
        out.append("    m_Data:")
        for e in ["e00", "e01", "e02", "e03", "e10", "e11", "e12", "e13", "e20", "e21", "e22", "e23", "e30", "e31", "e32", "e33"]:
            out.append("      %s: %s" % (e, m[e]))
    out.append("  m_TileColorArray:")
    out.append("  - m_RefCount: %d" % len(cells))
    out.append("    m_Data: {r: 1, g: 1, b: 1, a: 1}")
    out.append("  m_TileObjectToInstantiateArray: []")
    origin = (min(xs), min(ys))
    size = (max(xs) - min(xs) + 1, max(ys) - min(ys) + 1)
    return "\n".join(out) + "\n", origin, size


def replace_tilemap_block(scene_txt, tilemap_fileid, body, origin, size):
    """rewrite one Tilemap block's tile data. Everything outside the data span is left exactly as the template had it."""
    start = scene_txt.index("--- !u!1839735485 &%s\n" % tilemap_fileid)
    nxt = scene_txt.find("\n--- ", start + 10)
    block = scene_txt[start:nxt + 1] if nxt != -1 else scene_txt[start:]
    head, rest = block.split("  m_Tiles:", 1)
    # data span ends right before m_AnimationFrameRate
    tail = "  m_AnimationFrameRate:" + rest.split("  m_AnimationFrameRate:", 1)[1]
    tail = re.sub(r"  m_Origin: \{[^\n]*\}", "  m_Origin: {x: %d, y: %d, z: 0}" % origin, tail, count=1)
    tail = re.sub(r"  m_Size: \{[^\n]*\}", "  m_Size: {x: %d, y: %d, z: 1}" % size, tail, count=1)
    new_block = head + body + tail
    return scene_txt[:start] + new_block + scene_txt[start + len(block):]

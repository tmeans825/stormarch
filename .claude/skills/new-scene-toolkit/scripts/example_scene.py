"""Minimal scene spec = the template to copy for a real scene. Builds a walled room with a door, an NPC, a pushable crate, a tutorial bark,
and (optionally) a beat manager + a beat trigger that calls it.

    python example_scene.py --out-dir <scratch dir>                         # safe smoke test: writes nothing into the project
    python example_scene.py --name Act2_Foo --into-project                  # real run: Assets/Scenes/Act2_Foo.unity (+ .meta)
    python example_scene.py ... --beat-script "Assets/Scripts/Game Management/Story Beats/MyBeatManager.cs" --beat-class MyBeatManager --beat-method onEnterRoom

A real scene is: copy this file, replace the LAYOUT section, keep the structure (tiles -> player -> objects -> beat wiring -> finalize -> validate).
"""
import argparse, os, re, sys, tempfile
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import scenegen as S
import tiles as T

ap = argparse.ArgumentParser()
ap.add_argument("--name", default="Example_Scene")
ap.add_argument("--out-dir")
ap.add_argument("--into-project", action="store_true")
ap.add_argument("--beat-script", help="path to a sceneBeatManager subclass .cs (its .meta guid is used)")
ap.add_argument("--beat-class", default="MyBeatManager")
ap.add_argument("--beat-method", default="onEnterRoom")
args = ap.parse_args()
if not args.into_project and not args.out_dir:
    args.out_dir = tempfile.mkdtemp(prefix="scenegen_")

b = S.SceneBuilder(args.name, out_dir=None if args.into_project else args.out_dir)

# ------------------------------------------------------------------ LAYOUT (tile coordinates; 1 tile = 1.5 world units)
b.register_prefab("crate", "Environment/Obstacles/Crate.prefab")

# Tiles: reuse what the OldTown blockout already painted (a flat ground tile on GROUND, a solid wall tile on DYNAMIC).
n2g = T.tile_name_to_guid()
tiles = {
    "ground": T.harvest("Act1_OldTown.unity", "Tilemap_GROUND", "base_tileset_13", n2g),
    "wall": T.harvest("Act1_OldTown.unity", "Tilemap_DYNAMIC", "metal_walls_15", n2g),
}
ground = {(x, y): "ground" for x in range(-6, 40) for y in range(-14, 15)}
walls = {}
S.rect_outline(walls, -6, -14, 39, 14, "wall")                                # map perimeter (DYNAMIC has the colliders)
S.rect_outline(walls, 0, -8, 20, 8, "wall", gaps=[(20, -1), (20, 0), (20, 1)])  # a room with an east door
b.set_tilemap("Tilemap_GROUND", ground, tiles)
b.set_tilemap("Tilemap_DYNAMIC", walls, tiles)
b.set_player_start(4, 0)

# Objects
b.inst("crate", "Crate 1", 8, -3)
guide = b.npc("Guide", 12, 2, look="citizen", actor="Laborer", conv="Example/Guide/Hello")   # actor/conversation must exist in the Dialogue DB
b.bark_with_trigger("Move", 4, 2, "Example/Tutorial/Move", 5, 0, 6, 6)                      # inactive bark object + one-shot trigger that enables it
b.npc("Worker", 10, -4, look="poor", talk=False)                                             # ambient NPC: talk components removed (never just disabled)
guard = b.npc("Guard", 14, 4, look="guardian", actor="Guardian", talk=False, bark_only=True)  # can't be talked to, but can be a bark's barker
b.bark_with_trigger("Guard Barks", 14, 4, "Example/Guard/Barks", 13, 3, 4, 4, barker_transform_ref=guard.t_ref())   # barker = a TRANSFORM ref

# Optional beat manager wiring
if args.beat_script:
    meta = open(os.path.join(S._paths.project_root(), args.beat_script) + ".meta", encoding="utf8").read()
    script_guid = re.search(r"guid: (\w+)", meta).group(1)
    prefab_path = os.path.join(b.assets, "Prefabs", "Game Management", "Beat Managers", args.beat_class + ".prefab")
    if not args.into_project:
        prefab_path = os.path.join(args.out_dir, args.beat_class + ".prefab")
    guid, root_t, root_go, mono_id = b.make_beat_manager_prefab(args.beat_class, script_guid, prefab_path)
    bm = b.inst("bm", args.beat_class, 0, 0)
    bm_mb = bm.mb_ref(mono_id, script_guid)       # stripped stand-in other objects can call into
    # a guarded, repeatable trigger (one_shot=False because the method rejects calls made at the wrong beat)
    b.beat_trigger("Beat - Enter Room", 10, 0, 4, 6, method=args.beat_method, target_mb=bm_mb,
                   target_type="%s, Assembly-CSharp" % args.beat_class, one_shot=False)
    # scene refs INTO a manager field: bm.mod(mono_id, "someField", "", other_inst.go_ref())  /  array: "arr.Array.size", "arr.Array.data[0]"

# Sanity: everything we placed must be reachable from the player start
blocked = set(walls)
print(S.reachable(blocked, set(ground), (4, 0), {"guide": (12, 2), "outside door": (30, 0), "crate": (8, -3)}))

# ------------------------------------------------------------------ OUTPUT
b.finalize(write=True)
print("wrote", b.out_path)
print("next: python validate_scene.py \"%s\"" % b.out_path)

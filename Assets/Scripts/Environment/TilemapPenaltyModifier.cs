using UnityEngine;
using UnityEngine.Tilemaps;
using Pathfinding;

/*
 * Applies an A* penalty to the cells a tilemap actually paints, instead of to its bounding box.
 *
 * This replaces GraphUpdateScene on tilemap-shaped penalty volumes. GraphUpdateScene with an
 * empty points list resolves its area from Collider2D.bounds and leaves guo.shape null, so
 * GraphUpdateObject.Apply hits every node in one axis-aligned rectangle. For a compact box
 * volume that is correct; for a river winding across the map it penalises all the dry land in
 * between. Water covering 5% of its own bounding box was penalising the whole middle of the
 * Wilds and pushing every path around it.
 *
 * The mask is snapshotted off the tilemap here, on the Unity thread, and the update object only
 * reads plain arrays and vectors afterwards. GridGraph currently declares UnityThread for graph
 * updates so a live Tilemap reference would happen to work, but the snapshot also turns a
 * per-node HasTile call into an array index, which matters when a scan walks ~120k nodes.
 */

public class TilemapPenaltyModifier : GraphModifier
{
    [SerializeField] private Tilemap targetTilemap;
    [SerializeField] private int penaltyDelta = 200;
    [SerializeField] private bool modifyTag = true;
    [SerializeField] private int setTag = 2;
    [SerializeField] private bool applyOnStart = true;
    [SerializeField] private bool applyOnScan = true;

    // Mirrors GraphUpdateScene: a scan during Awake already applied us, so Start must not repeat it.
    private bool firstApplied;

    protected override void Reset()
    {
        base.Reset();
        targetTilemap = GetComponent<Tilemap>();
    }

    void Start()
    {
        // Edit-mode Start would stack another penaltyDelta outside of a scan. GraphUpdateScene
        // guards this the same way.
        if (!Application.isPlaying) return;

        if (!firstApplied && applyOnStart) apply();
    }

    public override void OnPostScan()
    {
        if (applyOnScan) apply();
    }

    /// <summary>
    /// Pushes the penalty onto every node sitting under a painted cell.
    /// Not idempotent - each call adds another penaltyDelta. A scan resets penalties first, which
    /// is why applyOnScan is safe to leave on; any other re-application needs a paired removal.
    /// </summary>
    public void apply()
    {
        if (AstarPath.active == null || targetTilemap == null) return;

        BoundsInt cells = targetTilemap.cellBounds;
        int cellsWide = cells.size.x;
        int cellsHigh = cells.size.y;
        if (cellsWide <= 0 || cellsHigh <= 0) return;

        bool[] mask = buildCellMask(cells, cellsWide, cellsHigh);
        if (mask == null) return;

        // Derive the cell-to-world basis from the tilemap itself rather than reconstructing it from
        // the Grid cell size and the tilemap scale, which are separate transforms here.
        Vector3 cellOrigin = targetTilemap.CellToWorld(new Vector3Int(cells.xMin, cells.yMin, 0));
        Vector3 stepX = targetTilemap.CellToWorld(new Vector3Int(cells.xMin + 1, cells.yMin, 0)) - cellOrigin;
        Vector3 stepY = targetTilemap.CellToWorld(new Vector3Int(cells.xMin, cells.yMin + 1, 0)) - cellOrigin;
        if (stepX.sqrMagnitude <= Mathf.Epsilon || stepY.sqrMagnitude <= Mathf.Epsilon) return;

        var guo = new TilemapMaskedGraphUpdate(
            worldBoundsOf(cellOrigin, stepX, stepY, cellsWide, cellsHigh),
            cellOrigin, stepX, stepY, cellsWide, cellsHigh, mask);

        guo.addPenalty = penaltyDelta;
        guo.modifyTag = this.modifyTag;
        guo.setTag = this.setTag;
        // Penalty and tag only - nothing here changes walkability, so there is no physics to
        // recalculate and no erosion region to redo.
        guo.updatePhysics = false;
        guo.updateErosion = false;

        AstarPath.active.UpdateGraphs(guo);
        firstApplied = true;
    }

    /// <summary>Flattens the painted cells into a row-major bitmask. Null if nothing is painted.</summary>
    private bool[] buildCellMask(BoundsInt cells, int cellsWide, int cellsHigh)
    {
        TileBase[] block = targetTilemap.GetTilesBlock(cells);
        bool[] mask = new bool[cellsWide * cellsHigh];
        int cellsDeep = Mathf.Max(1, cells.size.z);
        bool anyPainted = false;

        // GetTilesBlock is indexed x + width * (y + height * z). A tile on any z layer counts.
        for (int z = 0; z < cellsDeep; z++)
        {
            for (int y = 0; y < cellsHigh; y++)
            {
                for (int x = 0; x < cellsWide; x++)
                {
                    int source = x + cellsWide * (y + cellsHigh * z);
                    if (source >= block.Length || block[source] == null) continue;

                    mask[y * cellsWide + x] = true;
                    anyPainted = true;
                }
            }
        }

        return anyPainted ? mask : null;
    }

    private static Bounds worldBoundsOf(Vector3 cellOrigin, Vector3 stepX, Vector3 stepY, int cellsWide, int cellsHigh)
    {
        Vector3 spanX = stepX * cellsWide;
        Vector3 spanY = stepY * cellsHigh;

        Bounds bounds = new Bounds(cellOrigin, Vector3.zero);
        bounds.Encapsulate(cellOrigin + spanX);
        bounds.Encapsulate(cellOrigin + spanY);
        bounds.Encapsulate(cellOrigin + spanX + spanY);

        // A flat XY volume would contain no nodes when tested in 3D. Same fix-up GraphUpdateScene
        // applies to 2D collider bounds.
        bounds.size = new Vector3(bounds.size.x, bounds.size.y, Mathf.Max(bounds.size.z, 1f));
        return bounds;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (targetTilemap == null) targetTilemap = GetComponent<Tilemap>();

        // Silent when the tilemap is simply absent - that is the normal prefab-stage case.
        if (GetComponent<GraphUpdateScene>() != null)
        {
            Debug.LogWarning("TilemapPenaltyModifier shares a GameObject with a GraphUpdateScene; " +
                "both will apply and the penalty will be doubled. Remove the GraphUpdateScene.", this);
        }

        if (setTag < 0 || setTag > 31)
        {
            Debug.LogWarning("TilemapPenaltyModifier setTag must be 0-31; A* stores tags in 5 bits.", this);
            setTag = Mathf.Clamp(setTag, 0, 31);
        }
    }
#endif

    /// <summary>
    /// Bounds-limited update that additionally rejects nodes outside the painted cells.
    /// Holds only value data so it stays safe if graph updates ever move off the Unity thread.
    /// </summary>
    private class TilemapMaskedGraphUpdate : GraphUpdateObject
    {
        private readonly bool[] mask;
        private readonly Vector3 cellOrigin;
        private readonly Vector3 invX;
        private readonly Vector3 invY;
        private readonly int cellsWide;
        private readonly int cellsHigh;

        public TilemapMaskedGraphUpdate(Bounds bounds, Vector3 cellOrigin, Vector3 stepX, Vector3 stepY,
            int cellsWide, int cellsHigh, bool[] mask) : base(bounds)
        {
            this.cellOrigin = cellOrigin;
            // Rectangular grid, so the axes are independent and a projection recovers the cell.
            this.invX = stepX / stepX.sqrMagnitude;
            this.invY = stepY / stepY.sqrMagnitude;
            this.cellsWide = cellsWide;
            this.cellsHigh = cellsHigh;
            this.mask = mask;
        }

        public override void Apply(GraphNode node)
        {
            Vector3 offset = (Vector3)node.position - cellOrigin;
            int cellX = Mathf.FloorToInt(Vector3.Dot(offset, invX));
            int cellY = Mathf.FloorToInt(Vector3.Dot(offset, invY));

            if (cellX < 0 || cellY < 0 || cellX >= cellsWide || cellY >= cellsHigh) return;
            if (!mask[cellY * cellsWide + cellX]) return;

            base.Apply(node);
        }
    }
}

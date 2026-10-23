using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using Pathfinding;

/**
    DESCRIPTION: Catches A* penalty volumes that silently apply to a bounding box.

    GraphUpdateScene resolves its area from `points`. When that list is empty it falls back to a
    collider or renderer, and unless the collider is a PolygonCollider2D it builds a
    GraphUpdateObject with no shape - so the penalty lands on every node in one axis-aligned
    rectangle rather than on the collider outline. For a BoxCollider2D that is exactly right. For a
    tilemap or a circle it is not, and nothing in the editor says so.

    PolygonCollider2D is flagged too, and deliberately: in 4.2.15 GraphUpdateScene.Apply builds its
    shape from the empty `points` field instead of the `pts` array it computes one line earlier, so
    the polygon path is broken as well and quietly degrades to the same rectangle. Adding a
    PolygonCollider2D is not a fix.

    USAGE:
      Stormfall/A* Penalty Volumes/Audit Open Scenes   - report degraded volumes
      Stormfall/A* Penalty Volumes/Count Penalised Nodes - coarse before/after signal

    The audit also runs on scene open, and stays silent when nothing is wrong.
**/

public static class AstarPenaltyVolumeAudit
{
    private const string MENU_ROOT = "Stormfall/A* Penalty Volumes/";

    // -------------------------------------------------------------------------
    // Entry points
    // -------------------------------------------------------------------------

    [MenuItem(MENU_ROOT + "Audit Open Scenes")]
    public static void auditOpenScenesVerbose()
    {
        int problems = auditOpenScenes(true);
        if (problems == 0) Debug.Log("A* penalty volume audit: no degraded volumes found.");
    }

    [InitializeOnLoadMethod]
    private static void hookSceneOpened()
    {
        EditorSceneManager.sceneOpened += (scene, mode) => auditOpenScenes(false);
    }

    // -------------------------------------------------------------------------
    // Audit
    // -------------------------------------------------------------------------

    /// <summary>Reports degraded volumes. Returns how many were found. Quiet when clean.</summary>
    private static int auditOpenScenes(bool verbose)
    {
        int problems = 0;

        foreach (GraphUpdateScene volume in findInLoadedScenes<GraphUpdateScene>())
        {
            string collider = degradedColliderName(volume);
            if (collider == null) continue;

            problems++;
            Debug.LogWarning(
                "A* penalty volume \"" + pathOf(volume.gameObject) + "\" has no points and a " + collider +
                ", so its penalty (" + volume.penaltyDelta + ") applies to the whole bounding box, " +
                "not the collider outline. Use TilemapPenaltyModifier for tilemaps, or author the " +
                "points list. Adding a PolygonCollider2D does not fix this.", volume);
        }

        foreach (TilemapPenaltyModifier modifier in findInLoadedScenes<TilemapPenaltyModifier>())
        {
            // Read through SerializedObject rather than widening the component API for an editor check.
            SerializedProperty tilemap = new SerializedObject(modifier).FindProperty("targetTilemap");
            if (tilemap != null && tilemap.objectReferenceValue != null) continue;

            problems++;
            Debug.LogWarning("TilemapPenaltyModifier on \"" + pathOf(modifier.gameObject) +
                "\" has no target tilemap, so it applies nothing.", modifier);
        }

        if (verbose && problems > 0)
        {
            Debug.LogWarning("A* penalty volume audit: " + problems + " problem(s) found.");
        }
        return problems;
    }

    /// <summary>
    /// Name of the collider that makes this volume degrade to its AABB, or null when it is fine.
    /// A BoxCollider2D or a plain renderer bound is not a problem - a rectangle is the honest
    /// answer for those.
    /// </summary>
    private static string degradedColliderName(GraphUpdateScene volume)
    {
        if (volume.points != null && volume.points.Length > 0) return null;

        // An Enemy adopts its penalty volume as a data holder: it disables the component and
        // applies the penalty itself (Enemy.applyGraphPenalty), so GraphUpdateScene never writes.
        // GetComponentInParent has no includeInactive overload in this Unity version.
        Enemy[] owners = volume.GetComponentsInParent<Enemy>(true);
        if (owners.Length > 0 && owners[0].GetComponentInChildren<GraphUpdateScene>(true) == volume) return null;

        if (volume.GetComponent<TilemapCollider2D>() != null) return "TilemapCollider2D";
        if (volume.GetComponent<CircleCollider2D>() != null) return "CircleCollider2D";
        if (volume.GetComponent<CapsuleCollider2D>() != null) return "CapsuleCollider2D";
        if (volume.GetComponent<PolygonCollider2D>() != null) return "PolygonCollider2D";

        return null;
    }

    // -------------------------------------------------------------------------
    // Diagnostic
    // -------------------------------------------------------------------------

    /// <summary>
    /// Counts nodes carrying any penalty, per graph. Coarse on purpose: it cannot tell a water
    /// penalty from an enemy one, so treat the order of magnitude as the signal, not the number.
    /// </summary>
    [MenuItem(MENU_ROOT + "Count Penalised Nodes")]
    public static void countPenalisedNodes()
    {
        if (AstarPath.active == null || AstarPath.active.graphs == null)
        {
            Debug.LogWarning("No active AstarPath with scanned graphs in the scene.");
            return;
        }

        StringBuilder report = new StringBuilder("Penalised nodes per graph:");

        foreach (NavGraph graph in AstarPath.active.graphs)
        {
            if (graph == null) continue;

            int penalised = 0;
            int total = 0;
            uint highest = 0;

            graph.GetNodes(node =>
            {
                total++;
                if (node.Penalty <= 0) return;

                penalised++;
                if (node.Penalty > highest) highest = node.Penalty;
            });

            report.Append("\n  ").Append(graph.name).Append(": ").Append(penalised)
                  .Append(" / ").Append(total).Append(" nodes, highest penalty ").Append(highest);
        }

        Debug.Log(report.ToString());
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Every component of this type in the loaded scenes, inactive ones included, with prefab
    /// assets filtered out.
    /// </summary>
    private static List<T> findInLoadedScenes<T>() where T : Component
    {
        List<T> found = new List<T>();

        foreach (T candidate in Resources.FindObjectsOfTypeAll<T>())
        {
            if (EditorUtility.IsPersistent(candidate)) continue;
            if ((candidate.hideFlags & HideFlags.HideAndDontSave) != 0) continue;
            if (!candidate.gameObject.scene.IsValid()) continue;

            found.Add(candidate);
        }
        return found;
    }

    private static string pathOf(GameObject target)
    {
        string path = target.name;
        for (Transform parent = target.transform.parent; parent != null; parent = parent.parent)
        {
            path = parent.name + "/" + path;
        }
        return path;
    }
}

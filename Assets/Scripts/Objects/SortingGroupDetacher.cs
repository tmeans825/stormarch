using UnityEngine;
using UnityEngine.Rendering;

// Reparents this object out from under any ancestor SortingGroup on Start, preserving world
// position. A descendant renderer cannot opt out of an ancestor SortingGroup, so wiring placed
// on the Floor sorting layer under a puzzle's Dynamic Depth SortingGroup gets dragged up and
// renders over the player. Hopping above the group lets the renderer's own layer apply.
// No-op when there is no ancestor SortingGroup (e.g. Button / Pressure Plate).
public class SortingGroupDetacher : MonoBehaviour
{
    void Start()
    {
        detachFromAncestorSortingGroup();
    }

    void detachFromAncestorSortingGroup()
    {
        Transform newParent = transform.parent;
        while (newParent != null)
        {
            SortingGroup ancestorGroup = newParent.GetComponentInParent<SortingGroup>();
            if (ancestorGroup == null) break;
            newParent = ancestorGroup.transform.parent;
        }

        if (newParent != transform.parent)
            transform.SetParent(newParent, true); // preserve world position
    }
}

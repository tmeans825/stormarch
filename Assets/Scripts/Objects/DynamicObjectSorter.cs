using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicObjectSorter : MonoBehaviour
{
    public bool hasTrail = false;
    private int lastY;
    private void Awake()
    {
        lastY = (int)transform.position.y;
        updateSortingLayers(lastY);
    }

    private void LateUpdate()
    {
        int currentY = (int)transform.position.y;
        if (currentY != lastY)
        {
            updateSortingLayers(currentY - lastY);
            lastY = currentY;
        }
    }

    private void updateSortingLayers(int dY)
    {
        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr.sortingLayerName == "Dynamic Depth")
            {
                sr.sortingOrder = sr.sortingOrder + (-3 * dY);
            }
        }

        if(hasTrail)
        {
            TrailRenderer tr = GetComponentInChildren<TrailRenderer>(true);
            tr.sortingOrder = tr.sortingOrder + (-3 * dY);
        }
    }
}

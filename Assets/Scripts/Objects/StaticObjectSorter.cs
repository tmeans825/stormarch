using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaticObjectSorter : MonoBehaviour
{
    void Awake()
    {
        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr.sortingLayerName == "Dynamic Depth")
            {
                sr.sortingOrder = (-3 * (int)transform.position.y);
            }
        }

        foreach(ParticleSystemRenderer ps in GetComponentsInChildren<ParticleSystemRenderer>(true))
        {
            if(ps.sortingLayerName == "Dynamic Depth")
                ps.sortingOrder = (-3 * (int)transform.position.y);
        }
    }
}

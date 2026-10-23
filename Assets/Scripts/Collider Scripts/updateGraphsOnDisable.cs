using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class updateGraphsOnDisable : MonoBehaviour
{
    private void OnDisable()
    {
        GetComponent<Collider2D>().enabled = false;
        AstarPath.active.UpdateGraphs(GetComponent<Collider2D>().bounds);
    }
}

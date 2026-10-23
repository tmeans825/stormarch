using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OwenAnimationEvents : MonoBehaviour
{
    public void beginHammering()
    {
        HomesteadTerrainHelper.instance.triggerBuilding();
    }

    public void endHammering()
    {
        OwenBehavior.instance.triggerReturn();
    }
}

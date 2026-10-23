using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lantern : InventoryItem
{
    public float fuel = 1f;

    public Lantern()
    {
        iType = itemType.Lantern;
    }
    public void createLantern(float f)
    {
        iType = itemType.Lantern;
        fuel = f;
    }
}

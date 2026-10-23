using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Conductor : InventoryItem
{
    public int charges = 3;

    public Conductor()
    {
        this.iType = itemType.Conductor;
    }

    // Start is called before the first frame update
    public void createConductor(int c)
    {
        this.iType = itemType.Conductor;
        charges = c;
        rarity = 3;
    }
}

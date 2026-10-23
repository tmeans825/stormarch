using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Throwable : InventoryItem
{
    public enum throwableType
    {
        Grenade,
        Firebomb
    }

    public throwableType tType;

    public Throwable()
    {
        iType = itemType.Throwable;
    }

    public void createThrowable(throwableType t)
    {
        iType = itemType.Throwable;
        tType = t;
        maxCount = 3;

        if (t == throwableType.Grenade)
            rarity = 2;
        else
            rarity = 3;
    }
}

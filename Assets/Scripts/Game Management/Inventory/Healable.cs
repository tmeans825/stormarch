using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Healable : InventoryItem
{
    public enum healableType
    {
        Bandage,
        Medkit
    }

    public healableType hType;

    public Healable()
    {
        iType = itemType.Healable;
    }

    public void createHealable(healableType t)
    {
        iType = itemType.Healable;
        hType = t;

        if (t == healableType.Medkit)
        {
            maxCount = InventoryManager.medkitMaxStack;
            rarity = 2;
        }
        else
        {
            maxCount = 3;
            rarity = 1;
        }
    }
}

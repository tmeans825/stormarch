using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryItem
{
    public enum itemType
    {
        Gun,
        Throwable,
        Healable,
        Lantern,
        Armor,
        Conductor,
        Empty,
        Component,
        Tech,
        RifleAmmo,
        ShotgunAmmo,
        Cloth,
        Chemical,
        Scrap
    }

    public itemType iType = itemType.Empty;

    public int count = 1;
    public int maxCount = 1;

    public int rarity = 1;

    public Sprite itemModel;
    public Sprite outlineModel;
}

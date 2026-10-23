using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    // Single source of truth for the hotbar size. The last two slots are the quick use
    // pair; the final one is medkit-locked. Bumping INVENTORY_SIZE moves both, and every
    // consumer follows — but the Canvas prefab hotbars must gain a matching slot child.
    public const int INVENTORY_SIZE = 6;
    public const int QUICK_USE_1_SLOT = INVENTORY_SIZE - 2;
    public const int MEDKIT_SLOT = INVENTORY_SIZE - 1; // also quick use 2
    public static int medkitMaxStack = 2; // upgrade hook: flip to 3 via applyMedkitStackUpgrade()

    // Crafting materials are stored in quarter-units (4 = one whole cloth/chem/scrap in the UI).
    // Carry caps are tuned here in WHOLE units, per material (index = craftMatArray index:
    // 0 cloth, 1 chem, 2 scrap). Whole units keep "full" off a partial fill. Not saved (ES3
    // type lists craftMatArray only), so retuning always applies to existing saves.
    public const int CRAFT_MAT_QUARTERS_PER_UNIT = 4;
    private static readonly int[] CRAFT_MAT_CAP_UNITS = new int[3] { 6, 6, 6 };

    public int[] ammoArray = new int[3] { 20, 20, 20 };
    public int equippedItemIndex = 0;
    public int[] craftMatArray = new int[3] { 0, 0, 0 };

    [System.NonSerialized]
    [ES3NonSerializable]
    public static InventoryManager instance;

    [ES3Serializable]
    private InventoryItem[] inventory;
    [ES3Serializable]
    private Armor[] equippedArmor;
    private AudioManager am;

    // Start is called before the first frame update
    void Awake()
    {
        if (instance == null && this != instance)
            instance = this;
        else
            Destroy(gameObject);

        //DontDestroyOnLoad(gameObject);

        inventory = new InventoryItem[INVENTORY_SIZE];
        equippedArmor = new Armor[3];

        for (int i = 0; i < INVENTORY_SIZE; i++)
        {
            inventory[i] = new InventoryItem();
            inventory[i].iType = InventoryItem.itemType.Empty;
        }

        for (int i = 0; i < 3; i++)
        {
            equippedArmor[i] = new Armor();
            equippedArmor[i].iType = InventoryItem.itemType.Empty;
        }       
    }

    public void Start()
    {
        updateHUD(ammoArray[0], 0);
        StartCoroutine(waitEquip()); // equips starter armor
        GameEvents.current.onReturnToTitle += returningToTitle;
    }

    private void returningToTitle()
    {
        GameEvents.current.onReturnToTitle -= returningToTitle;
        this.transform.parent = ObjectPooler.instance.transform;
    }

    public int getNewEquipSlot(InventoryItem item) // TODO: Handle stacking too.
    {
        if (isMedkitItem(item))
            return MEDKIT_SLOT;

        if (item.maxCount > 1) // check to stack first
        {
            for (int i = 0; i < inventory.Length; i++)
            {
                if (i == MEDKIT_SLOT) continue;
                if (isSameStackableType(inventory[i], item) && inventory[i].count < inventory[i].maxCount)
                    return i;
            }
        }

        // if we get here, it's either not stackable or we don't have a similar stackable type
        // first, check if current slot is empty
        if (equippedItemIndex != MEDKIT_SLOT && inventory[equippedItemIndex].iType == InventoryItem.itemType.Empty)
            return equippedItemIndex;

        // next, check for any available slot
        for (int i = 0; i < inventory.Length; i++)
        {
            if (i == MEDKIT_SLOT) continue;
            if (inventory[i].iType == InventoryItem.itemType.Empty)
                return i;
        }

        // otherwise, we're equipping over the current slot (but never the medkit slot)
        return equippedItemIndex == MEDKIT_SLOT ? 0 : equippedItemIndex;
    }

    public bool isMedkitItem(InventoryItem item)
    {
        if (item == null || item.iType != InventoryItem.itemType.Healable)
            return false;
        Healable h = item as Healable;
        return h != null && h.hType == Healable.healableType.Medkit;
    }

    public bool canPlaceInSlot(int slot, InventoryItem item)
    {
        if (item == null || item.iType == InventoryItem.itemType.Empty)
            return true;
        return slot == MEDKIT_SLOT ? isMedkitItem(item) : !isMedkitItem(item);
    }

    public bool isMedkitStackFull()
    {
        if (!isMedkitItem(inventory[MEDKIT_SLOT]))
            return false;
        Healable h = inventory[MEDKIT_SLOT] as Healable;
        return h.count >= h.maxCount;
    }

    public void applyMedkitStackUpgrade()
    {
        medkitMaxStack = 3;
        if (isMedkitItem(inventory[MEDKIT_SLOT]))
        {
            Healable h = inventory[MEDKIT_SLOT] as Healable;
            h.maxCount = medkitMaxStack;
        }
    }

    private bool isSameStackableType(InventoryItem item1, InventoryItem item2)
    {
        if(item1.iType == InventoryItem.itemType.Healable && item2.iType == InventoryItem.itemType.Healable)
        {
            Healable healable1 = item1 as Healable;
            Healable healable2 = item2 as Healable;
            if (healable1.hType == healable2.hType)
                return true;
        }
        else if(item1.iType == InventoryItem.itemType.Throwable && item2.iType == InventoryItem.itemType.Throwable)
        {
            Throwable throwable1 = item1 as Throwable;
            Throwable throwable2 = item2 as Throwable;
            if (throwable1.tType == throwable2.tType)
                return true;
        }

        return false;
    }

    public InventoryItem getEquipped()
    {
        return inventory[equippedItemIndex];
    }

    public InventoryItem getEquipped(int index)
    {
        return inventory[index];
    }

    public Armor getEquippedArmor(int type)
    {
        return equippedArmor[type];
    }

    public int changeEquipped(int index, bool isRelative, bool actionKey)
    {
        int relativeStep = index < 0 ? -1 : 1;

        if (isRelative)
            index = equippedItemIndex + index;

        if (index < 0)
            index = inventory.Length - 1;
        if (index >= inventory.Length)
            index = 0;

        if (!actionKey)
        {
            if (isRelative)
            {
                int attempts = 0;
                while (attempts < inventory.Length && isEmptyMedkitSlot(index))
                {
                    index += relativeStep;
                    if (index < 0)
                        index = inventory.Length - 1;
                    if (index >= inventory.Length)
                        index = 0;
                    attempts++;
                }

                if (attempts >= inventory.Length)
                    return equippedItemIndex;
            }
            else if (isEmptyMedkitSlot(index))
                return equippedItemIndex;
        }

        equippedItemIndex = index;

        if (!actionKey)
        {
            HotbarUIBehavior.instance.selectSlot(index);
        }

        if (inventory[index].iType == InventoryItem.itemType.Gun)
        {
            Gun g = inventory[index] as Gun;
            updateHUD(getAmmo(g.mType), g.mType);
        }
        else
            HUDUIBehavior.instance.hideHUB();

        return index;
    }

    private bool isEmptyMedkitSlot(int index)
    {
        return index == MEDKIT_SLOT && inventory[index].iType == InventoryItem.itemType.Empty;
    }

    public void useBullet(int t, int bulletCount)
    {
        if (t == -1)
            return; // nothing to do for shitty bullets

        PlayerController.instance.equippedGun.bulletsInMag -= 1; // changed from bulletCount to this

        if (PlayerController.instance.equippedGun.bulletsInMag < 0)
            PlayerController.instance.equippedGun.bulletsInMag = 0;

        updateHUD(ammoArray[t], t);

        //for(int i = 0; i < bulletCount; i++)
            HUDUIBehavior.instance.useBulletAnim();

        GameEvents.current.onAmmoChange(ammoArray[0] + ammoArray[1] + ammoArray[2]);
    }

    public void addBullet(int t, int bulletCount)
    {
        ammoArray[t] += bulletCount;
        updateHUD(ammoArray[t], t);
        GameEvents.current.onAmmoChange(ammoArray[0] + ammoArray[1] + ammoArray[2]);

        SmallAlertsBehavior.instance.displayAmmoAlert(t, bulletCount);
    }

    public void reload(int t)
    {
        reload(t, PlayerController.instance.equippedGun.magSize - PlayerController.instance.equippedGun.bulletsInMag);
    }

    public void reload(int t, int amountToReload)
    {
        if(t == 1) // pistol
        {
            PlayerController.instance.equippedGun.bulletsInMag = PlayerController.instance.equippedGun.magSize;
        }
        else if (ammoArray[t] < amountToReload)
        {
            PlayerController.instance.equippedGun.bulletsInMag += ammoArray[t];
            ammoArray[t] = 0;
        }
        else
        {        
            PlayerController.instance.equippedGun.bulletsInMag = PlayerController.instance.equippedGun.magSize;
            ammoArray[t] -= amountToReload;
        }

        updateHUD(ammoArray[t], t);
        PlayerController.instance.playFinalReloadSound();
    }

    public void changeAmmoReserve(int changeAmount)
    {
        int gunType = PlayerController.instance.equippedGun.mType;

        ammoArray[gunType] += changeAmount;

        if (ammoArray[gunType] < 0)
            ammoArray[gunType] = 0;
        else
        {
            updateHUD(ammoArray[gunType], gunType);
            PlayerController.instance.playFinalReloadSound();
        }
    }

    public int getAmmo(int t)
    {
        if (t == 1)
            return 1; // pistol has infinite ammo, so return 1 to avoid divide by zero errors and such

        return ammoArray[t];
    }

    public void addCraftingMat(int t, int count)
    {
        craftMatArray[t] += count;

        if (craftMatArray[t] >= getCraftingMatCap(t))
            craftMatArray[t] = getCraftingMatCap(t);

        SmallAlertsBehavior.instance.displayCraftMatAlert(t, count);
    }

    public void reduceCraftingMat(int t, int count)
    {
        craftMatArray[t] -= count;

        if (craftMatArray[t] < 0)
            craftMatArray[t] = 0;
    }

    public int getCraftingMatCount(int t)
    {
        return craftMatArray[t];
    }

    public int getCraftingMatCap(int t) // in quarter-units, same as craftMatArray
    {
        return CRAFT_MAT_CAP_UNITS[t] * CRAFT_MAT_QUARTERS_PER_UNIT;
    }

    public bool isCraftingMatFull(int t)
    {
        return craftMatArray[t] >= getCraftingMatCap(t);
    }

    public bool equip(int index, InventoryItem item, bool notSwappingSlots)
    {
        bool placeHolder = true;
        return equip(index, item, notSwappingSlots, ref placeHolder);
    }

    public bool equip(int index, InventoryItem item, bool notSwappingSlots, ref bool areLeftovers)
    {
        bool quick = true;

        if (!canPlaceInSlot(index, item))
            return true; // silently no-op invalid placements (e.g. non-medkit on the medkit slot)

        if (index == MEDKIT_SLOT && isMedkitItem(item) && isMedkitStackFull())
            return true; // refuse before fall-through unequip would destroy the existing full stack

        if (inventory[index].count < inventory[index].maxCount && isSameStackableType(inventory[index], item)) // only if we already have a stackable
        {
            int leftover = addStackable(index, item.count);
            item.count = leftover;
            if (leftover <= 0)
                areLeftovers = false;
            HotbarUIBehavior.instance.updateBeanCounter(inventory[index].count, inventory[index].maxCount, index);
        }
        else
        {
            //Debug.Log("couldn't find existing slot to stack!");
            if (inventory[index].iType != InventoryItem.itemType.Empty)
            {
                unequip(index, notSwappingSlots);
                quick = false;
            }

            inventory[index] = LootMaster.instance.createNewItemCopy(item);
            HotbarUIBehavior.instance.equip(index, item.itemModel, item.outlineModel);
            HotbarUIBehavior.instance.updateBeanCounter(inventory[index].count, inventory[index].maxCount, index);

            if (inventory[index].maxCount > 1)
            {
                item.count = 0; // item we are picking up is exhausted as it has been fully copied into inventory (since stacking is handled above)
            }
        }
        return quick;
        //changeEquipped(index, false, false); 
    }

    // ES3 replaces the inventory array reference wholesale with one sized to the saved
    // data (ES3ArrayType.Read allocates, SetPrivateField assigns), so a save written at a
    // different slot count silently resizes us. Re-normalize before anything indexes by
    // MEDKIT_SLOT. Entries keep their original indices — this is a crash guard, not a
    // migration.
    public void normalizeInventoryLength()
    {
        if (inventory == null || inventory.Length != INVENTORY_SIZE)
        {
            InventoryItem[] resized = new InventoryItem[INVENTORY_SIZE];
            for (int i = 0; i < INVENTORY_SIZE; i++)
            {
                if (inventory != null && i < inventory.Length)
                    resized[i] = inventory[i];
            }
            inventory = resized;
        }

        for (int i = 0; i < inventory.Length; i++)
        {
            if (inventory[i] == null)
            {
                inventory[i] = new InventoryItem();
                inventory[i].iType = InventoryItem.itemType.Empty;
            }
        }

        if (equippedItemIndex < 0 || equippedItemIndex >= inventory.Length)
            equippedItemIndex = 0;
    }

    public void updateEquippedItemsAfterLoad()
    {
        normalizeInventoryLength();

        // WEAPONS
        for (int i = 0; i < inventory.Length; i++)
        {
            if (inventory[i].iType != InventoryItem.itemType.Empty)
            {
                HotbarUIBehavior.instance.equip(i, inventory[i].itemModel, inventory[i].outlineModel);
                HotbarUIBehavior.instance.updateBeanCounter(inventory[i].count, inventory[i].maxCount, i);
            }
        }

        if (inventory[MEDKIT_SLOT].iType == InventoryItem.itemType.Empty)
            HotbarUIBehavior.instance.unequip(MEDKIT_SLOT); // restore faded medkit placeholder after load

        changeEquipped(0, false, false);
        PlayerController.instance.switchEquipped(0, false, false, false);

        // ARMOR
        for (int i = 0; i < equippedArmor.Length; i++)
        {
            if ((equippedArmor[i].iType != InventoryItem.itemType.Empty))
            {
                Armor a = equippedArmor[i];
                adjustArmorStatsSpritesAndHUD(a);
            }
        }

        InventoryMenu.instance.updateInventory();
    }

    public bool equip(int index, InventoryItem item)
    {
        return equip(index, item, true);
    }

    public void unequip(int index, bool swap)
    {
        unequip(index, swap, false);
    }

    public void unequip(int index, bool swap, bool draggableDropped)
    {
        HotbarUIBehavior.instance.updateBeanCounter(1, inventory[index].maxCount, index);

        if (swap || draggableDropped)
            LootMaster.instance.spawnItem(inventory[index], PlayerController.instance.transform.position - new Vector3(0f, 1f, 0f), true); // spawn at feet
        if (!swap)
            inventory[index].iType = InventoryItem.itemType.Empty; // used up
        
        HotbarUIBehavior.instance.unequip(index);
    }

    public void swapItemSlots(int index1, int index2)
    {
        if (index1 == index2)
            return;

        if (!canPlaceInSlot(index1, inventory[index2]) || !canPlaceInSlot(index2, inventory[index1]))
            return; // reject swap that would violate the medkit slot lock

        InventoryItem oldIndex1Item = LootMaster.instance.createNewItemCopy(inventory[index1]);
        InventoryItem oldIndex2Item = LootMaster.instance.createNewItemCopy(inventory[index2]);

        bool leftovers = true;
        equip(index2, oldIndex1Item, false, ref leftovers);

        unequip(index1, false);

        if (leftovers)
            equip(index1, oldIndex2Item, false);
    }

    public void unequipArmor(int index)
    {
        LootMaster.instance.spawnItem(equippedArmor[index], PlayerController.instance.transform.position - new Vector3(0f, 1f, 0f), true); // spawn at feet
    }

    public void useStackable(int slot)
    {
        InventoryItem item = inventory[slot];
        item.count--;
        if (item.count <= 0)
            unequip(slot, false);
        HotbarUIBehavior.instance.updateBeanCounter(item.count, item.maxCount, slot);
    }

    public int addStackable(int slot, int amt) // returns leftovers
    {
        InventoryItem item = inventory[slot];
        int leftover = 0;

        for (int i = 0; i < amt; i++)
        {
            if (item.count < item.maxCount)
                item.count++;
            else
                leftover++;
        }

        HotbarUIBehavior.instance.updateBeanCounter(item.count, item.maxCount, slot);
        return leftover;
    }

    public bool equipArmor(Armor a)
    {
        bool quick = true;
        if (equippedArmor[a.aType].iType != InventoryItem.itemType.Empty)
        {
            if (a.aType == 1)
                GameEvents.current.chestUnequip(equippedArmor[a.aType]);

            unequipArmor(a.aType);
            quick = false;
        }

        adjustArmorStatsSpritesAndHUD(a);

        if (a.aType == 1)
            GameEvents.current.chestEquip(a);

        return quick;
    }

    public void recalculateMaxShields()
    {
        float armorShieldSum = 0f;
        for (int i = 0; i < equippedArmor.Length; i++)
        {
            if (equippedArmor[i] != null && equippedArmor[i].iType != InventoryItem.itemType.Empty)
                armorShieldSum += equippedArmor[i].shields;
        }

        // Turret deployment temporarily reduces max shields by 1
        if (PlayerController.instance.turretShieldCostActive)
            armorShieldSum -= 1f;

        PlayerController.instance.maxShields = armorShieldSum;

        if (PlayerController.instance.shields > PlayerController.instance.maxShields)
            PlayerController.instance.shields = PlayerController.instance.maxShields;

        HealthUIBehavior.instance.updateMaxShields();
    }

    private void adjustArmorStatsSpritesAndHUD(Armor a)
    {
        equippedArmor[a.aType] = a;
        recalculateMaxShields();

        switch (a.aType)
        {
            case 0:
                PlayerController.instance.changeHairSprites(a.spritesheetName);
                break;
            case 1:
                PlayerController.instance.chestSpriteSheetName = a.spritesheetName;
                break;
            case 2:
                PlayerController.instance.legsSpriteSheetName = a.spritesheetName;
                break;
            default:
                Debug.LogWarning("Armor has unrecognized type. Cannot update spritesheet.");
                break;
        }
    }

    private void updateHUD(int count, int t)
    {
        InventoryItem equipped = getEquipped();
        if (equipped.iType == InventoryItem.itemType.Gun)
        {
            Gun g = equipped as Gun;
            if (g.mType == t)
                HUDUIBehavior.instance.refreshHUD(count, t);
        }
        else
            HUDUIBehavior.instance.hideHUB();
    }

    private IEnumerator waitEquip()
    {
        yield return new WaitForSeconds(0.1f);

        if (equippedArmor[1].iType == InventoryItem.itemType.Empty)
        {
            Armor chest = new Armor();
            chest = LootMaster.instance.createArmor(1, Armor.armorWeight.Medium, 0.75f, "chest_spacesuit_blue", 1, 1, true);
            equipArmor(chest);
        }

        if (equippedArmor[2].iType == InventoryItem.itemType.Empty)
        {
            Armor legs = new Armor();
            legs = LootMaster.instance.createArmor(2, Armor.armorWeight.Light, 0.25f, "legs_spacesuit_blue", 1, 1);
            equipArmor(legs);
        }
    }
}

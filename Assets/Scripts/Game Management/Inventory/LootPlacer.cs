using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LootPlacer : MonoBehaviour
{
    public InventoryItem.itemType itemType;
    [Tooltip("0 = Craftable, 1 = Common, 2 = Uncommon, 3 = Rare, 4 = Exceptional, 5 = Unique")]
    [Range(0, 5)]
    public int rarity = 0;

    [Header("Gun")]
    [Tooltip("0 - rifle, 1 - pistol, 2 - shotgun")]
    [Range(0, 2)]
    public int gunModel;
    public bool isShockBaton;

    [Header("Throwable")]
    public Throwable.throwableType throwableType;

    [Header("Healable")]
    public Healable.healableType healableType;

    [Header("Armor")]
    [Tooltip("0 - helmet, 1 - chest, 2 - legs")]
    [Range(0, 2)]
    public int armorType;
    public Armor.armorWeight armorWeight;
    public float shield;
    public string spriteSheet;

    [Header("Conductor")]
    public int conductorCharges = 3;

    [Header("Events / Beats")]
    public GameObject[] objsToEnableOnPickup;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(mainBehavior());
    }

    private IEnumerator mainBehavior()
    {
        yield return new WaitForSeconds(Random.Range(0.01f, 0.1f));
        InventoryItem item = new InventoryItem();
        bool failed = false;
        switch (itemType)
        {
            case InventoryItem.itemType.Gun:
                if (isShockBaton)
                    item = LootMaster.instance.createBaton();
                else
                    item = DropManager.instance.createGunByModelAndRarity(gunModel, rarity);
                break;
            case InventoryItem.itemType.Throwable:
                if (throwableType == Throwable.throwableType.Grenade)
                    item = LootMaster.instance.createGrenade();
                else if (throwableType == Throwable.throwableType.Firebomb)
                    item = LootMaster.instance.createFirebomb();
                break;
            case InventoryItem.itemType.Lantern:
                item = LootMaster.instance.createLantern(1f);
                break;
            case InventoryItem.itemType.Healable:
                if (healableType == Healable.healableType.Medkit)
                {
                    Debug.LogWarning("Stray medkit placer skipped — medkits are craft-only now. Placer: " + gameObject.name, gameObject);
                    failed = true;
                    break;
                }
                item = LootMaster.instance.createHealable(healableType);
                break;
            case InventoryItem.itemType.Armor:
                item = DropManager.instance.createArmorOfTypeAndRarity(armorType, rarity);
                break;
            case InventoryItem.itemType.Conductor:
                item = LootMaster.instance.createConductor(conductorCharges);
                break;
            case InventoryItem.itemType.Component:
                item = LootMaster.instance.createComponent(1);
                break;
            default:
                Debug.LogWarning("Invalid item type specified - an item was not spawned.");
                failed = true;
                break;
        }

        if (!failed)
        {
            LootMaster.instance.spawnItem(item, transform.position, false, true, objsToEnableOnPickup);
            this.enabled = false;
        }
        //Destroy(this.gameObject);
        gameObject.SetActive(false);
    }    

    
}

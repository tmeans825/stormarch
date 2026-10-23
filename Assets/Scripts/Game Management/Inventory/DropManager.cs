using ES3Types;
using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO.Ports;
using System.Linq;
using UnityEngine;
using static dropCategories;

public class DropManager : MonoBehaviour
{
    [Tooltip("Cumulative. Size is 4. Nodes: 0 = Common, 1 = Uncommon, 2 = Rare, 3 = Exceptional")]
    public float[] rarityPercentages;

    public string[] armorColors = new string[] { "blue", "orange" };

    [System.NonSerialized]
    public static DropManager instance;

    private System.Data.DataSet dataSet;
    private DistributionUtility distributor;

    private bool isLowHealth = false;
    private bool isLowAmmo = true;

    private float[,] chestShieldArray;
    private float[,] legsHelmShieldArray;

    private void Awake()
    {
        if (instance == null && this != instance)
            instance = this;
        else
        {
            Destroy(gameObject);
        }
    }


    private void Start()
    {
        dataSet = new DataSet();
        distributor = new DistributionUtility();

        generateTemplates();
        generateClassifications();

        initializeArmorArrays();

        GameEvents.current.signalLowHealth += onLowHealthSignal;
        GameEvents.current.signalLowAmmo += onLowAmmoSignal;
    }

    private void initializeArmorArrays()
    {
        legsHelmShieldArray = new float[3, 4] {
            { 0.25f, 0.50f, 0.75f, 1.00f }, // Tier 1
            { 0.50f, 0.75f, 1.00f, 1.25f }, // Tier 2
            { 0.75f, 1.00f, 1.25f, 2.00f }  // Tier 3
            //^Light, Med,  Heavy, Unique
        };

        chestShieldArray = new float[3, 4] {
            { 0.00f, 0.75f, 1.50f, 1.00f }, // Tier 1
            { 0.00f, 1.00f, 2.00f, 1.50f }, // Tier 2
            { 0.00f, 1.25f, 2.50f, 2.00f }  // Tier 3
            //^Light, Med,  Heavy, Unique
        };
    }

    public void spawnTestItems()
    {
        StartCoroutine(spawnTestItemsAfterSeconds(0.2f));
    }

    private void onLowHealthSignal(bool signal)
    {
        isLowHealth = signal;
    }

    private void onLowAmmoSignal(bool signal)
    {
        isLowAmmo = signal;
    }

    // ------------------------
    // Selection Code (Tables)
    // ------------------------
    private void generateTemplates()
    {
        // Generics to use for loot gen
        Healable bandage = new Healable();
        bandage.hType = Healable.healableType.Bandage;

        // Medkits are craft-only — no world drops.

        Throwable grenade = new Throwable();
        grenade.tType = Throwable.throwableType.Grenade;

        Throwable firebomb = new Throwable();
        firebomb.tType = Throwable.throwableType.Firebomb;

        InventoryItem tech = new InventoryItem();
        tech.iType = InventoryItem.itemType.Tech;

        InventoryItem rifleAmmo = new InventoryItem();
        rifleAmmo.iType = InventoryItem.itemType.RifleAmmo;

        InventoryItem shotgunAmmo = new InventoryItem();
        shotgunAmmo.iType = InventoryItem.itemType.ShotgunAmmo;

        InventoryItem component = new InventoryItem();
        component.iType = InventoryItem.itemType.Component;

        InventoryItem cloth = new InventoryItem();
        cloth.iType = InventoryItem.itemType.Cloth;

        InventoryItem chem = new InventoryItem();
        chem.iType = InventoryItem.itemType.Chemical;

        InventoryItem scrap = new InventoryItem();
        scrap.iType = InventoryItem.itemType.Scrap;


        // Table Columns:   ID, Item, Count Range , Count Distribution, Base Percentage, Advantage

        // Soldier
        MakeTemplateTable("T_Soldier");
        dataSet.Tables["T_Soldier"].Rows.Add(new object[] { 1, new Gun(), new Pair { First = 1, Second = 1 }, DistributionUtility.distribution.Uniform, 3, 0 });
        dataSet.Tables["T_Soldier"].Rows.Add(new object[] { 2, new Armor(), new Pair { First = 1, Second = 1 }, DistributionUtility.distribution.Uniform, 2, 0 });
        dataSet.Tables["T_Soldier"].Rows.Add(new object[] { 3, bandage, new Pair { First = 2, Second = 2 }, DistributionUtility.distribution.LowerCommon, 4, 10 });
        dataSet.Tables["T_Soldier"].Rows.Add(new object[] { 4, grenade, new Pair { First = 2, Second = 2 }, DistributionUtility.distribution.LowerCommon, 3, 0 });
        dataSet.Tables["T_Soldier"].Rows.Add(new object[] { 6, firebomb, new Pair { First = 2, Second = 2 }, DistributionUtility.distribution.LowerCommon, 1, 0 });
        dataSet.Tables["T_Soldier"].Rows.Add(new object[] { 7, component, new Pair { First = 1, Second = 3 }, DistributionUtility.distribution.Normal, 95, 0 });
        dataSet.Tables["T_Soldier"].Rows.Add(new object[] { 8, rifleAmmo, new Pair { First = 2, Second = 6 }, DistributionUtility.distribution.Normal, 10, 15 });
        dataSet.Tables["T_Soldier"].Rows.Add(new object[] { 9, tech, new Pair { First = 1, Second = 1 }, DistributionUtility.distribution.Normal, 1, 0 });
        dataSet.Tables["T_Soldier"].Rows.Add(new object[] { 10, shotgunAmmo, new Pair { First = 1, Second = 2 }, DistributionUtility.distribution.Normal, 10, 15 });
        dataSet.Tables["T_Soldier"].Rows.Add(new object[] { 11, cloth, new Pair { First = 1, Second = 2 }, DistributionUtility.distribution.LowerCommon, 10, 0 });
        dataSet.Tables["T_Soldier"].Rows.Add(new object[] { 12, chem, new Pair { First = 1, Second = 4 }, DistributionUtility.distribution.Normal, 5, 0 });
        dataSet.Tables["T_Soldier"].Rows.Add(new object[] { 13, scrap, new Pair { First = 1, Second = 2 }, DistributionUtility.distribution.LowerCommon, 25, 20 });
        /*      
        Healable bandage = new Healable();
        bandage.createHealable(Healable.healableType.Bandage, 0);
        dataSet.Tables["T_Soldier"].Rows.Add(new object[] { 3, bandage, new Pair { First = 1, Second = 3 }, DistributionUtility.distribution.Uniform, 80, 0 });*/

        // Animal
        MakeTemplateTable("T_Wolvolin");
        dataSet.Tables["T_Wolvolin"].Rows.Add(new object[] { 1, component, new Pair { First = 1, Second = 3 }, DistributionUtility.distribution.Normal, 95, 0 });
        dataSet.Tables["T_Wolvolin"].Rows.Add(new object[] { 2, scrap, new Pair { First = 1, Second = 2 }, DistributionUtility.distribution.LowerCommon, 10, 10 });


        // Boss

        // Supply Cache
        MakeTemplateTable("T_Cache");
        dataSet.Tables["T_Cache"].Rows.Add(new object[] { 1, cloth, new Pair { First = 1, Second = 2 }, DistributionUtility.distribution.LowerCommon, 25, 0 });
        dataSet.Tables["T_Cache"].Rows.Add(new object[] { 2, chem, new Pair { First = 1, Second = 4 }, DistributionUtility.distribution.Normal, 12, 0 });
        dataSet.Tables["T_Cache"].Rows.Add(new object[] { 3, bandage, new Pair { First = 2, Second = 2 }, DistributionUtility.distribution.LowerCommon, 4, 10 });
        dataSet.Tables["T_Cache"].Rows.Add(new object[] { 4, grenade, new Pair { First = 2, Second = 2 }, DistributionUtility.distribution.LowerCommon, 3, 0 });
        dataSet.Tables["T_Cache"].Rows.Add(new object[] { 6, firebomb, new Pair { First = 2, Second = 2 }, DistributionUtility.distribution.LowerCommon, 1, 0 });
        dataSet.Tables["T_Cache"].Rows.Add(new object[] { 7, component, new Pair { First = 1, Second = 3 }, DistributionUtility.distribution.Normal, 95, 0 });
        dataSet.Tables["T_Cache"].Rows.Add(new object[] { 8, rifleAmmo, new Pair { First = 2, Second = 6 }, DistributionUtility.distribution.Normal, 10, 15 });
        dataSet.Tables["T_Cache"].Rows.Add(new object[] { 9, tech, new Pair { First = 1, Second = 1 }, DistributionUtility.distribution.Normal, 1, 0 });
        dataSet.Tables["T_Cache"].Rows.Add(new object[] { 10, shotgunAmmo, new Pair { First = 1, Second = 2 }, DistributionUtility.distribution.Normal, 10, 15 });

        // Crate
        MakeTemplateTable("T_Crate");
        dataSet.Tables["T_Crate"].Rows.Add(new object[] { 1, bandage, new Pair { First = 1, Second = 1 }, DistributionUtility.distribution.LowerCommon, 4, 10 });
        dataSet.Tables["T_Crate"].Rows.Add(new object[] { 2, component, new Pair { First = 1, Second = 3 }, DistributionUtility.distribution.Normal, 100, 0 });
        dataSet.Tables["T_Crate"].Rows.Add(new object[] { 3, rifleAmmo, new Pair { First = 4, Second = 8 }, DistributionUtility.distribution.Normal, 2, 10 });
        dataSet.Tables["T_Crate"].Rows.Add(new object[] { 4, shotgunAmmo, new Pair { First = 1, Second = 2 }, DistributionUtility.distribution.Normal, 2, 5 });
        dataSet.Tables["T_Crate"].Rows.Add(new object[] { 5, cloth, new Pair { First = 1, Second = 2 }, DistributionUtility.distribution.LowerCommon, 20, 0 });
        dataSet.Tables["T_Crate"].Rows.Add(new object[] { 6, chem, new Pair { First = 1, Second = 4 }, DistributionUtility.distribution.Normal, 10, 0 });

        // Chest
        MakeTemplateTable("T_Chest");
        dataSet.Tables["T_Chest"].Rows.Add(new object[] { 1, new Gun(), new Pair { First = 1, Second = 1 }, DistributionUtility.distribution.Uniform, 99, 0 });
        dataSet.Tables["T_Chest"].Rows.Add(new object[] { 2, new Armor(), new Pair { First = 1, Second = 1 }, DistributionUtility.distribution.Uniform, 50, 0 });
        dataSet.Tables["T_Chest"].Rows.Add(new object[] { 3, bandage, new Pair { First = 1, Second = 2 }, DistributionUtility.distribution.LowerCommon, 4, 10 });
        dataSet.Tables["T_Chest"].Rows.Add(new object[] { 4, grenade, new Pair { First = 1, Second = 2 }, DistributionUtility.distribution.LowerCommon, 3, 0 });
        dataSet.Tables["T_Chest"].Rows.Add(new object[] { 6, firebomb, new Pair { First = 2, Second = 2 }, DistributionUtility.distribution.LowerCommon, 1, 0 });
        dataSet.Tables["T_Chest"].Rows.Add(new object[] { 7, rifleAmmo, new Pair { First = 2, Second = 6 }, DistributionUtility.distribution.Normal, 20, 20 });
        dataSet.Tables["T_Chest"].Rows.Add(new object[] { 8, tech, new Pair { First = 1, Second = 1 }, DistributionUtility.distribution.Normal, 100, 0 });
        dataSet.Tables["T_Chest"].Rows.Add(new object[] { 9, shotgunAmmo, new Pair { First = 1, Second = 2 }, DistributionUtility.distribution.Normal, 20, 20 });
        dataSet.Tables["T_Chest"].Rows.Add(new object[] { 10, cloth, new Pair { First = 1, Second = 2 }, DistributionUtility.distribution.LowerCommon, 10, 0 });
        dataSet.Tables["T_Chest"].Rows.Add(new object[] { 11, chem, new Pair { First = 1, Second = 4 }, DistributionUtility.distribution.HigherCommon, 50, 0 });

    }

    private void MakeTemplateTable(string tableName)
    {
        // Create a new DataTable.
        System.Data.DataTable table = new DataTable(tableName);

        // Set up Columns
        table.Columns.Add("ID", typeof(int));
        table.Columns.Add("Item", typeof(InventoryItem));
        table.Columns.Add("Count", typeof(Pair));
        table.Columns.Add("Count Distribution", typeof(DistributionUtility.distribution));
        table.Columns.Add("Base Percentage", typeof(int));
        table.Columns.Add("Advantage", typeof(int));

        // Make the ID column the primary key column.
        table.PrimaryKey = new DataColumn[] { table.Columns["ID"] };

        // Add the new DataTable to the DataSet.
        dataSet.Tables.Add(table);
    }

    private void generateClassifications()
    {
        // We may not need to do this, unless you need to override values likle count, etc.
        // Guardian

        // Marauder

        // Legion

        // Wolvolin

        // Sentinel

        // Mech

        // Pirate Boss

        // Ziven Wyrick
    }

    private void overrideValue(string tableName, int rowID, string columnName, dynamic overrideValue)
    {
        DataRow foundRow = dataSet.Tables[tableName].Rows.Find(rowID);
        if (foundRow != null)
        {
            foundRow[columnName] = overrideValue;
        }
        else
        {
            Debug.Log("Row " + rowID + " not found in " + tableName + " table.");
        }
    }

    /// <summary>
    /// Creates a table and adds it to the dataset, as a copy of a template.
    /// </summary>
    /// <param name="newTableName"></param>
    /// <param name="templateName"></param>
    private void createFromTemplate(string newTableName, string templateName)
    {
        DataTable newTable = new DataTable(newTableName);
        newTable = dataSet.Tables[templateName].Copy();

        dataSet.Tables.Add(newTable);
    }

    // ------------------------
    // Generation Code
    // ------------------------
    public void generateDrop(dropCategories.classification classification, Vector3 loc, bool downForceOnly = false)
    {
        string tableName;

        switch (classification)
        {
            case dropCategories.classification.Guardian:
                tableName = "T_Soldier";
                break;
            case dropCategories.classification.Crate:
                tableName = "T_Crate";
                break;
            case dropCategories.classification.Cache:
                tableName = "T_Cache";
                break;
            case dropCategories.classification.PoweredChest:
                tableName = "T_Chest";
                break;
            case dropCategories.classification.Wolvolin:
                tableName = "T_Wolvolin";
                break;
            default:
                Debug.LogWarning("No tablename defined for the classification; dropping generic soldier loot");
                tableName = "T_Soldier";
                break;
        }

        List<InventoryItem> itemsToSpawn = new List<InventoryItem>();

        foreach (DataRow row in dataSet.Tables[tableName].Rows)
        {
            int chance = (int)row["Base Percentage"];
            InventoryItem item = (InventoryItem)row["Item"];

            int roll = UnityEngine.Random.Range(0, 101);

            // process and use advantage
            if ((isLowHealth && item.iType == InventoryItem.itemType.Healable)) 
            {               
                chance += (int)row["Advantage"];
            }

            // no scrap advantage once the player is above 75% of the scrap cap
            bool scrapNearCap = item.iType == InventoryItem.itemType.Scrap
                && InventoryManager.instance.getCraftingMatCount(2) > InventoryManager.instance.getCraftingMatCap(2) * 0.75f;

            if(isLowAmmo && !scrapNearCap && (item.iType == InventoryItem.itemType.RifleAmmo || item.iType == InventoryItem.itemType.ShotgunAmmo || item.iType == InventoryItem.itemType.Scrap))
            {
                chance += (int)row["Advantage"];
            }

            if (roll <= chance)
            {
                int count;
                Pair countRange = (Pair)row["Count"];
                if ((countRange.First - countRange.Second) != 0)
                {
                    DistributionUtility.distribution countDist = (DistributionUtility.distribution)row["Count Distribution"];
                    count = distributor.generateIntValue(countRange.First, countRange.Second, countDist);

                    if (count == 0)
                        continue;
                }
                else
                    count = countRange.First;


                //for (int i = 0; i < count; i++)
                //{
                switch (item.iType) // For items that need stats - generate them
                {
                    case InventoryItem.itemType.Gun:
                        item = createRandomGun();
                        break;
                    case InventoryItem.itemType.Armor:
                        item = createRandomArmor();
                        break;
                    case InventoryItem.itemType.Healable:
                        // Medkits are craft-only — Bandage is the only Healable that drops.
                        item = LootMaster.instance.createHealable(Healable.healableType.Bandage);
                        break;
                    case InventoryItem.itemType.Throwable:
                        Throwable throwable = item as Throwable;
                        if (throwable.tType == Throwable.throwableType.Grenade)
                            item = LootMaster.instance.createGrenade();
                        else
                            item = LootMaster.instance.createFirebomb();
                        break;
                    case InventoryItem.itemType.Tech:
                        item = LootMaster.instance.createTech(count);
                        break;
                    case InventoryItem.itemType.Component:
                        item = LootMaster.instance.createComponent(count);
                        break;
                    case InventoryItem.itemType.RifleAmmo:
                        item = LootMaster.instance.createAmmo(0, count);
                        break;
                    case InventoryItem.itemType.ShotgunAmmo:
                        item = LootMaster.instance.createAmmo(2, count);
                        break;
                    default:
                        break;
                }

                    item.count = count;

                    itemsToSpawn.Add(item);

                    //if (item.iType == InventoryItem.itemType.Tech)
                        //break; // we spawn tech all at once, not incrementally, bc particle system
                //}
            }
        }
        // if you want to process the items now, this is your chance

        // now.. spawn the items
        foreach (InventoryItem item in itemsToSpawn)
            LootMaster.instance.spawnItem(item, loc, true, false, downForceOnly);
    }

    // ------------------------
    // Creation Code
    // ------------------------
    private Gun createRandomGun()
    {
        // models - 0 - rifle, 1 - pistol, 2 - shotgun
        int mType = (int)distributor.generateIntValue(0, 2, DistributionUtility.distribution.Uniform);
        int rarity = DistributionUtility.GetRarity(rarityPercentages);

        return createGunByModelAndRarity(mType, rarity);
    }

    public Gun createGunByModelAndRarity(int mType, int rarity)
    {
        int tier = RollTierForAct(SaveManager.instance.act);

        // set variables based on tier + model
        gunStats stats = getHardCodedGunValues(tier, mType);

        Gun gunToReturn = LootMaster.instance.createGun(mType, stats.fspeed, stats.bspeed, stats.dam, stats.dam2, stats.range, stats.crit, stats.bullets, rarity, tier, stats.magSize);
        GameEvents.current.gunCreate(gunToReturn);

        return gunToReturn;
    }

    private gunStats getHardCodedGunValues(int tier, int mType)
    {
        gunStats statsToReturn = new gunStats();

        switch (mType)
        {
            case 0: // rifle
                statsToReturn.fspeed = 0.3f - 0.05f * tier;
                statsToReturn.bspeed = 24f + tier;
                statsToReturn.dam = 15 + 1 * tier;
                statsToReturn.dam2 = 20 + 2 * tier;
                statsToReturn.range = 20 + tier * 3f;
                statsToReturn.crit = 0.10f + 0.02f * tier;
                statsToReturn.bullets = 1;
                statsToReturn.magSize = 15;
                break;

            case 1: // pistol
                statsToReturn.fspeed = 0.5f - 0.05f * tier;
                statsToReturn.bspeed = 18f + tier;
                statsToReturn.dam = 6 + 1 * tier;
                statsToReturn.dam2 = 8 + 2 * tier;
                statsToReturn.range = 10 + tier * 2f;
                statsToReturn.crit = 0.10f + 0.02f * tier;
                statsToReturn.bullets = 1;
                statsToReturn.magSize = 9;
                break;

            default: // shotgun
                statsToReturn.fspeed = 0.75f - 0.05f * tier;
                statsToReturn.bspeed = 21f + tier;
                statsToReturn.dam = 8 + tier;
                statsToReturn.dam2 = 10 + tier;
                statsToReturn.range = 6 + tier;
                statsToReturn.crit = 0.10f + 0.02f * tier;
                statsToReturn.bullets = 5;
                statsToReturn.magSize = 3;
                break;
        }

        return statsToReturn;
    }


    public Armor createRandomArmor()
    {
        int aType = distributor.generateIntValue(0, 2, DistributionUtility.distribution.Uniform); // greaves, chest, helm
        int rarity = DistributionUtility.GetRarity(rarityPercentages);

        return createArmorOfTypeAndRarity(aType, rarity);
    }

    public Armor createArmorOfTypeAndRarity(int aType, int rarity, Armor.armorWeight? forcedWeight = null, List<Charm> forcedCharms = null)
    {
        int tier = RollTierForAct(SaveManager.instance.act);

        if (rarity < 2) // Armor starts at uncommon
            rarity = 2;

        Armor.armorWeight aWeight = forcedWeight ?? getArmorWeight(rarity, aType);

        string spritesheetName = getArmorSpritesheetName(aType, aWeight, tier);


        // Set shields
        float shields = 0f;

        getHardCodedArmorValues(aWeight, aType, tier, ref shields);

        return LootMaster.instance.createArmor(aType, aWeight, shields, spritesheetName, rarity, tier, false, forcedCharms);
    }

    private void getHardCodedArmorValues(Armor.armorWeight aWeight, int aType, int tier, ref float shields)
    {
        int weightInt = convertWeightToInt(aWeight);

        if (aType == 1)
            shields = chestShieldArray[tier-1, weightInt];
        else
            shields = legsHelmShieldArray[tier-1, weightInt];
    }

    private string getArmorSpritesheetName(int aType, Armor.armorWeight aWeight, int tier) // To do - update based on weight/tier
    {
        switch (aType)
        {
            case 0:
                return getHairName(aWeight, tier);
            case 1:
                return getChestName(aWeight, tier);
            case 2:
                return getLegName(aWeight);
            default:
                return getChestName(aWeight, tier);
        }
    }

    private string getLegName(Armor.armorWeight aWeight)
    {
        string spritesheetName = "legs_" + convertWeightToString(aWeight);

        switch (aWeight)
        {
            case(Armor.armorWeight.Heavy):
                return appendRandomColor(spritesheetName, armorColors);
            default:
                return spritesheetName;
        }
    }

    private string getHairName(Armor.armorWeight aWeight, int tier)
    {
        string spritesheetName = "hair_" + convertWeightToString(aWeight) + "_tier" + tier;

        switch (aWeight)
        {
            case Armor.armorWeight.Light:
            case Armor.armorWeight.Heavy:
                return appendRandomColor(spritesheetName, armorColors);
            case Armor.armorWeight.Medium:
                if (tier < 3) // tier 1 and 2 of medium don't have color
                    return spritesheetName;
                else
                    return appendRandomColor(spritesheetName, armorColors);
            default:
                return spritesheetName + "_unique";
        }
    }

    private string getChestName(Armor.armorWeight aWeight, int tier)
    {
        string spritesheetName = "chest_" + convertWeightToString(aWeight) + "_tier" + tier;
        spritesheetName = appendRandomColor(spritesheetName, armorColors);
        return spritesheetName;
    }

    private string appendRandomColor(string spritesheetName, string[] colors)
    {
        int colorIndex = UnityEngine.Random.Range(0, colors.Length);
        return spritesheetName + "_" + colors[colorIndex];
    }

    private int convertWeightToInt(Armor.armorWeight weight)
    {
        switch (weight)
        {
            case Armor.armorWeight.Light:
                return 0;
            case Armor.armorWeight.Medium:
                return 1;
            case Armor.armorWeight.Heavy:
                return 2;
            default:
                return 1;
        }
    }

    private string convertWeightToString(Armor.armorWeight weight)
    {
        switch (weight)
        {
            case Armor.armorWeight.Light:
                return "light";
            case Armor.armorWeight.Medium:
                return "medium";
            case Armor.armorWeight.Heavy:
                return "heavy";
            default:
                return "unique";
        }
    }

    private Armor.armorWeight getArmorWeight(int rarity, int aType)
    {
        if (aType == 1) // For chest, weight is randomly assigned between the 3
        {       
            switch (UnityEngine.Random.Range(0, 3))
            {
                case 0:
                    return Armor.armorWeight.Light;
                case 1:
                    return Armor.armorWeight.Medium;
                case 2:
                    return Armor.armorWeight.Heavy;
                default:
                    return Armor.armorWeight.Medium;
            }
        }
        else // For others, it is based on the rarity
        {
            switch (rarity)
            {
                case 0:
                case 1:
                case 2:
                    return Armor.armorWeight.Light;
                case 3:
                    return Armor.armorWeight.Medium;
                case 4:
                case 5:
                    return Armor.armorWeight.Heavy;
                default:
                    return Armor.armorWeight.Medium;
            }
        }
    }

    private int RollTierForAct(int act)
    {
        // Define tier weights per Act
        Dictionary<int, float> weights = new Dictionary<int, float>();

        switch (act)
        {
            case 1:
                weights = new Dictionary<int, float> {
                { 1, 1f },
            };
                break;

            case 2:
                weights = new Dictionary<int, float> {
                { 1, 0.25f },
                { 2, 0.75f },
            };
                break;

            case 3:
                weights = new Dictionary<int, float> {
                { 2, 0.25f },
                { 3, 0.75f }
            };
                break;

            default:
                Debug.LogWarning("Invalid act number. Defaulting to Act 1 tier table.");
                return RollTierForAct(1);
        }

        // Roll a random value between 0 and 1
        float roll = UnityEngine.Random.value;
        float cumulative = 0f;

        foreach (var kvp in weights.OrderBy(k => k.Key)) // Ensure consistent tier ordering
        {
            cumulative += kvp.Value;
            if (roll <= cumulative)
                return kvp.Key;
        }

        // Fallback in case of rounding error
        return weights.Keys.Last();
    }

    /// <summary>
    /// Calculate whether we've maxxed out all stats
    /// </summary>
    /// <param name="loopVar">Pass by reference counter</param>
    /// <param name="loopMax">Number of go-to casese to track</param>
    /// <returns>False if a loop has been hit</returns>
    private bool infiniteLoopCalc(ref int loopVar, int loopMax)
    {
        loopVar++;
        if (loopVar >= loopMax)
            return false;

        return true;
    }

    // ------------------------
    // TESTS (TO DO - Remove)
    // ------------------------

    private IEnumerator spawnTestItemsAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        TESTSAMPLESARMOR();
        TESTSAMPLESGUNS();
        TESTCONSUMABLES();
    }

    private void TESTSAMPLESARMOR()
    {
        Armor[] armorToSpawn = new Armor[3];

        for (int aType = 0; aType < 3; aType++)
        {
            int rarity = DistributionUtility.GetRarity(rarityPercentages);
            Armor.armorWeight? forcedWeight = null;
            List<Charm> forcedCharms = null;

            // Only chest (aType 1) carries charms, so the override only applies there.
            if (aType == 1
                && GameDevTestOverrides.instance != null
                && GameDevTestOverrides.instance.TryGetForcedTestArmorCharms(out forcedCharms, out int? rarityOverride, out Armor.armorWeight? weightOverride))
            {
                rarity = rarityOverride.Value;
                forcedWeight = weightOverride;
            }

            armorToSpawn[aType] = createArmorOfTypeAndRarity(aType, rarity, forcedWeight, forcedCharms);
        }

        foreach (Armor armor in armorToSpawn)
            LootMaster.instance.spawnItem(armor, PlayerController.instance.transform.position, true, false, false);
    }

    private void TESTSAMPLESGUNS()
    {
        Gun[] gunsToSpawn = new Gun[3];

        for (int mType = 0; mType < 3; mType++)
        {
            int tier = RollTierForAct(SaveManager.instance.act);
            gunStats stats = getHardCodedGunValues(tier, mType);
            int rarity = DistributionUtility.GetRarity(rarityPercentages);
            List<Charm> forcedCharms = null;

            if (GameDevTestOverrides.instance != null
                && GameDevTestOverrides.instance.TryGetForcedTestGunCharms(mType, out forcedCharms, out int? rarityOverride))
            {
                rarity = rarityOverride.Value;
            }

            gunsToSpawn[mType] = LootMaster.instance.createGun(mType, stats.fspeed, stats.bspeed, stats.dam, stats.dam2, stats.range, stats.crit, stats.bullets, rarity, tier, stats.magSize, forcedCharms); // Charms handled by LootMaster
            GameEvents.current.gunCreate(gunsToSpawn[mType]);
        }

        foreach(Gun gun in gunsToSpawn)
            LootMaster.instance.spawnItem(gun, PlayerController.instance.transform.position, true, false, false);
    }

    private void TESTCONSUMABLES()
    {
        LootMaster.instance.spawnItem(LootMaster.instance.createHealable(Healable.healableType.Bandage), PlayerController.instance.transform.position, true);
        LootMaster.instance.spawnItem(LootMaster.instance.createHealable(Healable.healableType.Medkit), PlayerController.instance.transform.position, true);
        LootMaster.instance.spawnItem(LootMaster.instance.createHealable(Healable.healableType.Medkit), PlayerController.instance.transform.position, true);
        LootMaster.instance.spawnItem(LootMaster.instance.createCraftableMat(0, 9), PlayerController.instance.transform.position, true);
        LootMaster.instance.spawnItem(LootMaster.instance.createCraftableMat(1, 9), PlayerController.instance.transform.position, true);
        LootMaster.instance.spawnItem(LootMaster.instance.createCraftableMat(2, 9), PlayerController.instance.transform.position, true);
    }    
}

// Other classes/structs for this behavior

public class dropCategories
{
    public enum classification
    {
        Guardian,
        Marauder,
        Legion,
        Wolvolin,
        Sentinel,
        Mech,
        PirateBoss,
        ZivenWyrick,
        Crate,
        Cache,
        PoweredChest
    }
}

public class DistributionUtility
{
    private static System.Random random = new System.Random();

    /// <summary>
    /// Given a min, max, and distribution, calculate a random float.
    /// Cast the return as (int) if you want to round to a whole number.
    /// If you want to use rarities, use overloaded function with 3 arrays.
    /// </summary>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <param name="dist"></param>
    /// <returns></returns>
    public float generateFloatValue(float min, float max, distribution dist)
    {
        return GetRandomNumber(min, max, dist);
    }

    /// <summary>
    /// Overloaded version of generateValue to use Rarities
    /// </summary>
    /// <param name="minArrayByRarity">Contain 1 value for each potential rarity</param>
    /// <param name="maxArrayByRarity">Contain 1 value for each potential rarity</param>
    /// <param name="rarityPercentages">Contain 1 value for each potential rarity, CUMULATIVE</param>
    /// <param name="rarity">Output/Pass by reference - assigned rarity of the item</param>
    /// <returns></returns>
    public float generateFloatValue(float[] minArrayByRarity, float[] maxArrayByRarity, float[] rarityPercentages, ref int rarity, int act)
    {
        rarity = GetRarity(rarityPercentages);
        return RandomNormal(minArrayByRarity[rarity+act], maxArrayByRarity[rarity+act]);
    }

    public int generateIntValue(int min, int max, distribution dist)
    {
        return (int)Math.Round(generateFloatValue(min-0.499f, max+0.499f, dist));
    }

    public int generateIntValue(float[] minArrayByRarity, float[] maxArrayByRarity, float[] rarityPercentages, ref int rarity, int act)
    {
        return (int)Math.Round(generateFloatValue(minArrayByRarity, maxArrayByRarity, rarityPercentages, ref rarity, act));
    }

    // Generate a random value within the specified range with a normal distribution
    private static float RandomNormal(float min, float max)
    {
        double u1 = 1.0 - random.NextDouble(); // Uniform(0,1] random doubles
        double u2 = 1.0 - random.NextDouble();
        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                              Math.Sin(2.0 * Math.PI * u2); // Random normal(0,1)

        // Scale and shift the value to the desired range
        float mean = (min + max) / 2.0f; // Calculate the mean
        float stdDev = (max - min) / 6.0f; // Calculate the standard deviation
        float result = (float)(mean + stdDev * randStdNormal);

        return Mathf.Clamp(result, min, max); // Ensure the result is within the range
    }

    // Generate a random value within the specified range.
    // If probabilityPower is above 1, lower values will be more common than higher values.
    // If it's between 0 to 1, higher values will be more common than lower values.
    // If it's 1, the results will be in a general randomness.
    private static float GetRandomNumber(float min, float max, distribution distributionType)
    {
        if (distributionType == distribution.Uniform)
        {
            // Generate a uniform distribution
            return UnityEngine.Random.Range(min, max);
        }
        else if (distributionType == distribution.LowerCommon)
        {
            // Generate a distribution skewed towards higher values
            double randomDouble = random.NextDouble();
            double skew = 1.75f; // Adjust skew factor as needed
            float result = (float)(min + (max - min) * Math.Pow(randomDouble, skew));
            return Mathf.Clamp(result, min, max);
        }
        else if (distributionType == distribution.HigherCommon)
        {
            // Generate a distribution skewed towards lower values
            double randomDouble = random.NextDouble();
            double skew = 0.5; // Adjust skew factor as needed
            float result = (float)(min + (max - min) * Math.Pow(randomDouble, skew));
            return Mathf.Clamp(result, min, max);
        }
        else if (distributionType == distribution.Normal)
        {
            return RandomNormal(min, max);
        }
        else
        {
            throw new ArgumentException("Invalid distributionType");
        }
    }

    /// <summary>
    /// Returns a rarity based on cumulative percentages. 
    /// </summary>
    /// <param name="rarityPercentages"></param>
    /// <returns>Returns selected node + 1 (b/c we assume craftable is hard-coded).</returns>
    public static int GetRarity(float[] rarityPercentages) // returns + 1 to rarity b/c we assume craftable is hard-coded
    {
        // 1 - common (white) , 2 - uncommon (green) , 3 - rare (blue) , 4 - exceptional (purple) , 5 - unique (orange)
        int roll = UnityEngine.Random.Range(0, 101);

        for(int i = 0; i < rarityPercentages.Length; i++)
        {
            if (i == 0)
            {
                if (roll <= rarityPercentages[i])
                    return i + 1;
            }
            else if (i < (rarityPercentages.Length - 1))
            {
                if ((roll > rarityPercentages[i-1]) && (roll <= rarityPercentages[i]))
                {
                    return i + 1;
                }
            }
            else
                return i + 1;
        }

        return 1; // fallback, should never happen
    }

    public enum distribution
    {
        Uniform,
        Normal,
        LowerCommon,
        HigherCommon
    }
}

public struct Pair
{
    public int First { get; set; }
    public int Second { get; set; }
}

public sealed class gunStats
{
    public float fspeed;
    public float bspeed;
    public float dam;
    public float dam2;
    public float range;
    public float crit;
    public int bullets;
    public int magSize;
}


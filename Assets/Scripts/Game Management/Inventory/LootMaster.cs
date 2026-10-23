using Com.LuisPedroFonseca.ProCamera2D.TopDownShooter;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using UnityEngine;

public class LootMaster : MonoBehaviour
{
    [Header("Item Model Sprites")]
    [Tooltip("Trad: 0 Rifle, 1 Pistol, 2 Shotgun, Lancer: 3 Rifle, 4 Pistol, Electro: 5 Rifle, 6 Shotgun, 7 Grenade, 8 Firebomb, 9 Bandage, 10 Medkit, 11 Lantern, 12 Chest, 13 Legs, 14 Conductor, 15 trad ammo, 16 electrospike ammo, 17 lancer ammo, 18 tech, 19 cloth, 20 chem, 21 scrap, 22 baton, 23 Lancer Shotgun, 24 Electro Pistol")]
    public Sprite[] itemModels;
    public Sprite[] outlines;

    public GameObject loot;
    public ParticleSystem componentSpawner;

    public float slideSpeed = 5f;

    private ParticleSystem.Burst burst;


    [System.NonSerialized]
    public static LootMaster instance;

    private CharmDatabase charmDatabase;
    private List<Charm> weaponCharms = new List<Charm>();
    private List<Charm> armorCharms = new List<Charm>();

    void Awake()
    {
        if (instance == null && this != instance)
            instance = this;
        else
            Destroy(this);

        burst = componentSpawner.emission.GetBurst(0);

        charmDatabase = CharmLoader.LoadCharmDatabase();

        weaponCharms = charmDatabase.charms
            .Where(c => c.appliesTo == Charm.appliesToWeaponString)
            .ToList();
        armorCharms = charmDatabase.charms
            .Where(c => c.appliesTo == Charm.appliesToArmorString)
            .ToList();
    }

    public void spawnItem(InventoryItem item, Vector3 pos, bool addForce)
    {
        spawnItem(item, pos, addForce, false);
    }

   
    public void spawnItem(InventoryItem item, Vector3 pos, bool addForce, bool madeByLootPlacer)
    {
        spawnItem(item, pos, addForce, madeByLootPlacer, false);
    }

    public void spawnItem(InventoryItem item, Vector3 pos, bool addForce, bool madeByLootPlacer, GameObject[] toEnable)
    {
        spawnItem(item, pos, addForce, madeByLootPlacer, false, toEnable);
    }

    public void spawnItem(InventoryItem item, Vector3 pos, bool addForce, bool madeByLootPlacer, bool downForceOnly)
    {
        spawnItem(item, pos, addForce, madeByLootPlacer, downForceOnly, null);
    }

    public void spawnItem(InventoryItem item, Vector3 pos, bool addForce, bool madeByLootPlacer, bool downForceOnly, GameObject[] toEnable)
    {
        GameObject newItem;

        if (item.iType != InventoryItem.itemType.Component)
        {
            newItem = Instantiate(loot, pos, Quaternion.identity);

            if (downForceOnly) // temp remove colliders for chest drops
            {
                newItem.GetComponent<Loot>().initializeChestDrop();             
            }
        }
        else
        {
            // Special handling for components
            componentSpawner.transform.position = pos;
            burst.count = item.count;
            componentSpawner.emission.SetBurst(0, burst);
            componentSpawner.Play();
            return;
        }
        //InventoryItem itemCopy = createNewItemCopy(item);

        if (item.iType == InventoryItem.itemType.Armor || madeByLootPlacer)
            newItem.GetComponent<Loot>().item = item;
        else
            newItem.GetComponent<Loot>().item = createNewItemCopy(item);

        InventoryItem.itemType tempType = newItem.GetComponent<Loot>().item.iType;

        if (tempType == InventoryItem.itemType.Healable || tempType == InventoryItem.itemType.Conductor || tempType == InventoryItem.itemType.Cloth || tempType == InventoryItem.itemType.Chemical || tempType == InventoryItem.itemType.Scrap)
        {
            // Healable healable = newItem.GetComponent<Loot>().item as Healable;

            //if (healable.hType == Healable.healableType.Bandage)
            newItem.transform.localScale *= 0.8f;
        }



        if (addForce)
        {
            addItemDropForce(newItem, downForceOnly);
        }

        newItem.GetComponent<Loot>().objsToEnableOnPickup = toEnable;
    }

        public InventoryItem createNewItemCopy(InventoryItem oldItem)
    {
        InventoryItem item = new InventoryItem();
        InventoryItem.itemType itemType = oldItem.iType;
        switch (itemType)
        {
            case InventoryItem.itemType.Gun:
                Gun oldGun = oldItem as Gun;
                Gun newGun = new Gun();
                newGun.createGun(oldGun.gType, oldGun.mType, oldGun.fireSpeed, oldGun.bulletSpeed,
                    oldGun.damage[0], oldGun.damage[1], oldGun.range, oldGun.critChance,
                    oldGun.bulletCount, oldGun.rarity, oldGun.tier, oldGun.magSize, oldGun.charms);
                newGun.kickBackAmount = oldGun.kickBackAmount;
                newGun.bulletsInMag = oldGun.bulletsInMag;
                newGun.gunName = oldGun.gunName;
                LootMaster.instance.updateGunAppearance(newGun);
                item = newGun;
                break;
            case InventoryItem.itemType.Throwable:
                Throwable throwable = oldItem as Throwable;
                if (throwable.tType == Throwable.throwableType.Grenade)
                    item = LootMaster.instance.createGrenade();
                else if (throwable.tType == Throwable.throwableType.Firebomb)
                    item = LootMaster.instance.createFirebomb();
                break;
            case InventoryItem.itemType.Lantern:
                Lantern lantern = oldItem as Lantern;
                item = LootMaster.instance.createLantern(lantern.fuel);
                break;
            case InventoryItem.itemType.Healable:
                Healable healable = oldItem as Healable;
                item = LootMaster.instance.createHealable(healable.hType);
                break;
            case InventoryItem.itemType.Armor:
                Armor armor = oldItem as Armor;
                item = LootMaster.instance.createArmor(armor.aType, armor.aWeight, armor.shields, armor.spritesheetName, armor.rarity, armor.tier);
                break;
            case InventoryItem.itemType.Conductor:
                Conductor conductor = oldItem as Conductor;
                item = LootMaster.instance.createConductor(conductor.charges);
                break;
            case InventoryItem.itemType.Tech:
                item = LootMaster.instance.createTech(oldItem.count);
                break;
            case InventoryItem.itemType.RifleAmmo:
                item = LootMaster.instance.createAmmo(0, item.count);
                break;
            case InventoryItem.itemType.ShotgunAmmo:
                item = LootMaster.instance.createAmmo(2, item.count);
                break;
            case InventoryItem.itemType.Cloth:
                item = LootMaster.instance.createCraftableMat(0, item.count);
                break;
            case InventoryItem.itemType.Chemical:
                item = LootMaster.instance.createCraftableMat(1, item.count);
                break;
            case InventoryItem.itemType.Scrap:
                item = LootMaster.instance.createCraftableMat(2, item.count);
                break;
            default:
                Debug.LogWarning("Invalid item type specified - an item was not spawned. Item type: " + itemType);
                item.iType = InventoryItem.itemType.Empty;
                break;
        }

        item.count = oldItem.count;
        return item;
    }

    private float getUniqueTrait(Gun gun)
    {
        switch(gun.gType)
        {
            case 0:
                return gun.bulletSpeed;
            case 1:
                return gun.critDmg;
            case 2:
                return gun.aoeSize;
            default:
                Debug.LogWarning("Unable to create gun, because I couldn't find the right gun type.");
                return 0.0f;
        }
    }

    public void addItemDropForce(GameObject item, bool downForceOnly = false)
    {
        float x = Random.Range(-1f, 1f);
        float y;

        if (!downForceOnly)
            y = Random.Range(-1f, 1f);
        else
            y = Random.Range(-1.5f, -0.5f);

        item.transform.GetComponent<Rigidbody2D>().AddRelativeForce(new Vector3(x, y, 0.0f) * slideSpeed);
    }

    public Gun createGun(int model, float fspeed, float bspeed, float dam, float dam2, float rng, float crit, int bullets, int rarity, int tier, int mag, List<Charm> charmsToAssign = null)
    {
        Gun g = new Gun();

        if(charmsToAssign == null)
            charmsToAssign = CharmLoader.GetCharmsForWeapon(rarity, weaponCharms);

        // Type determines electro v lancer. TODO: figure out how we're dealing w the pistol/shotgun limitations. Maybe just remove all pistols?
        int type = CharmBehaviorManager.instance.getGunTypeFromCharms(charmsToAssign);

        g.createGun(type, model, fspeed, bspeed, dam, dam2, rng, crit, bullets, rarity, tier, mag, charmsToAssign);
        updateGunAppearance(g);
            
        return g;
    }

    public Gun createBaton()
    {
        Gun g = new Gun();
        g.createGun(3, 1, 1, 20f, 0, 0, 10, 0, 1, 2, 1, 1, null);
        updateGunAppearance(g);

        return g;
    }    

    public void updateGunAppearance(Gun g)
    {
        int index = getGunSpriteIndex(g.gType, g.mType);

        g.itemModel = itemModels[index];

        if (index == 23) // Lancer Shotgun - exception cases for late add gun types
            g.outlineModel = outlines[14];
        else if (index == 24) // Electro Pistol
            g.outlineModel = outlines[15];
        else if (index < outlines.Length)
            g.outlineModel = outlines[index];
    }

    public InventoryItem createLantern(float fuel)
    {
        Lantern lantern = new Lantern();
        lantern.createLantern(fuel);
        lantern.iType = InventoryItem.itemType.Lantern;
        lantern.itemModel = itemModels[11];
        lantern.outlineModel = outlines[11];
        return lantern;
    }

    public Throwable createGrenade()
    {
        Throwable grenade = new Throwable();
        grenade.createThrowable(Throwable.throwableType.Grenade);
        grenade.count = 1;
        grenade.itemModel = itemModels[7];
        grenade.outlineModel = outlines[7];
        return grenade;
    }

    public Throwable createFirebomb()
    {
        Throwable firebomb = new Throwable();
        firebomb.createThrowable(Throwable.throwableType.Firebomb);
        firebomb.count = 1;
        firebomb.itemModel = itemModels[8];
        firebomb.outlineModel = outlines[8];
        return firebomb;
    }

    public Healable createHealable(Healable.healableType ht)
    {
        int healIndex;
        if (ht == Healable.healableType.Bandage)
            healIndex = 9;
        else
            healIndex = 10;

        Healable healable = new Healable();
        healable.createHealable(ht);
        healable.count = 1;
        healable.itemModel = itemModels[healIndex];
        healable.outlineModel = outlines[healIndex];
        return healable;
    }

    public Conductor createConductor(int charges)
    {
        Conductor conductor = new Conductor();
        conductor.createConductor(charges);
        conductor.itemModel = itemModels[14];
        conductor.outlineModel = outlines[12];          
        return conductor;
    }

    public Armor createArmor(int type, Armor.armorWeight weight, float shield, string spriteSheet, int rarity, int tier, bool skipCharms = false, List<Charm> charmsToAssign = null)
    {
        Armor armor = new Armor();
        List<Charm> charms = new List<Charm>();

        if (type == 1) // only chest gets charms
        {
            if (charmsToAssign != null)
                charms = charmsToAssign;
            else if (!skipCharms)
                charms = CharmLoader.GetCharmsForArmor(rarity, armorCharms, weight, tier);
        }

        armor.createArmor(type, weight, shield, spriteSheet, rarity, tier, charms);
        armor.itemModel = getArmorSprite(type, armor.spritesheetName); 
        return armor;
    }

    public InventoryItem createTech(int count)
    {
        InventoryItem tech = new InventoryItem(); // Tech is used as a shell here, different handling
        tech.count = count;
        tech.iType = InventoryItem.itemType.Tech;
        tech.itemModel = itemModels[18];
        tech.rarity = 3;

        return tech;
    }

    public InventoryItem createComponent(int count)
    {
        InventoryItem comp = new InventoryItem(); // Tech is used as a shell here, different handling
        comp.count = count;
        comp.iType = InventoryItem.itemType.Component;

        return comp;
    }

    public InventoryItem createAmmo(int ammoType, int count)
    {
        InventoryItem ammo = new InventoryItem();
        switch (ammoType)
        {
            case 0:
                ammo.iType = InventoryItem.itemType.RifleAmmo;
                ammo.itemModel = itemModels[15];
                ammo.rarity = 1;
                break;
            case 2:
                ammo.iType = InventoryItem.itemType.ShotgunAmmo;
                ammo.itemModel = itemModels[16];
                ammo.rarity = 2;
                break;
            /*case 2:
                ammo.iType = InventoryItem.itemType.LancerAmmo;
                ammo.itemModel = itemModels[17];
                ammo.rarity = 3;
                break;*/
            default:
                break;
        }

        ammo.count = count;
        return ammo;
    }

    public InventoryItem createCraftableMat(int matType, int count)
    {
        InventoryItem mat = new InventoryItem();
        switch(matType)
        {
            case 0:
                mat.iType = InventoryItem.itemType.Cloth;
                mat.itemModel = itemModels[19];
                break;
            case 1:
                mat.iType = InventoryItem.itemType.Chemical;
                mat.itemModel = itemModels[20];
                break;
            case 2:
                mat.iType = InventoryItem.itemType.Scrap;
                mat.itemModel = itemModels[21];
                break;
            default:
                break;
        }
        mat.rarity = 0;
        mat.count = count;

        return mat;
    }

    public Sprite getArmorSprite(int type, string spritesheetName)
    {
        if (type == 0)
        {
            var sprites = Resources.LoadAll<Sprite>("Hair/" + spritesheetName);
            if(sprites == null)
            {
                Debug.LogError("You tried to reference a spritesheet that does not exist. Name: Hair/" + spritesheetName);
            }
            return sprites[2];
        }
        else if (type == 1)
            return itemModels[12];
        else
            return itemModels[13];

    }

    private int getGunSpriteIndex(int gun, int model)
    {
        switch (gun)
        {
            case 0: // trad
                switch (model)
                {
                    case 0: // rifle
                        return 0;
                    case 1: // pistol
                        return 1;
                    case 2: // shotgun
                        return 2;
                    default:
                        return 0;
                }
            case 1: // lancer
                switch (model)
                {
                    case 0: // rifle
                        return 3;
                    case 1: // pistol
                        return 4;
                    case 2: // shotgun
                        return 23;
                    default:
                        return 3;
                }
            case 2: // electro
                switch (model)
                {
                    case 0: // rifle
                        return 5;
                    case 1: // pistol
                        return 24;
                    case 2: // shotgun
                        return 6;
                    default:
                        return 5;
                }
            case 3:
                return 22; // baton
            default:
                return 0;
        }
    }

    public string getUIName(InventoryItem item)
    {
        switch (item.iType)
        {
            case (InventoryItem.itemType.Armor):
                return getArmorName(item as Armor);
            case (InventoryItem.itemType.Gun):
                return getGunName(item as Gun);
            case (InventoryItem.itemType.Lantern):
                return "Lantern";
            case (InventoryItem.itemType.Healable):
                Healable healable = item as Healable;
                if (healable.hType == Healable.healableType.Bandage)
                    return "Bandages";
                else
                    return "Medkit";
            case (InventoryItem.itemType.Throwable):
                Throwable throwable = item as Throwable;
                if (throwable.tType == Throwable.throwableType.Firebomb)
                    return "Firebomb";
                else
                    return "Grenade";
            case (InventoryItem.itemType.Conductor):
                return "Conductor";
            case (InventoryItem.itemType.RifleAmmo):
                return "Rifle Ammo";
            case (InventoryItem.itemType.ShotgunAmmo):
                return "Shotgun Ammo";
            case (InventoryItem.itemType.Tech):
                return "Advanced Tech";
            case (InventoryItem.itemType.Cloth):
                return "Cloth";
            case (InventoryItem.itemType.Chemical):
                return "Chemicals";
            case (InventoryItem.itemType.Scrap):
                return "Scrap";
            default:
                Debug.LogWarning("Couldn't find item field name on mouseover...");
                return "";
        }
    }

    private string getGunName(Gun gun)
    {
        switch (gun.gType)
        {
            case 0: // trad
                return getGunNameBasedOnModel(gun.mType, "Rifle", "Pistol", "Shotgun");
            case 1: // lancer
                return getGunNameBasedOnModel(gun.mType, "Sniper", "Hand Cannon", "");
            case 2: // electro
                return getGunNameBasedOnModel(gun.mType, "Model R", "", "Model S");
            case 3: // baton
                return "Shock Baton";
            default:
                return "GUN TYPE ERROR";
        }
    }

    private string getArmorName(Armor armor)
    {
        switch (armor.aType)
        {
            case 0:
                return "Helmet";
            case 1:
                return "Chestplate";
            case 2:
                return "Greaves";
            default:
                return "ARMOR TYPE ERROR";
        }
    }

    private string getGunNameBasedOnModel(int model, string Rifle, string Pistol, string Shotgun)
    {
        switch (model)
        {
            case 0: // rifle
                return Rifle;
            case 1: // pistol
                return Pistol;
            case 2: // shotgun
                return Shotgun;
            default:
                return "GUN MODEL ERROR";
        }
    }
}

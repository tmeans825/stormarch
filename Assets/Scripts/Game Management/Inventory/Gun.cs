using PixelCrushers;
using PixelCrushers.DialogueSystem;
using PixelCrushers.Wrappers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Gun : InventoryItem 
{
    // 0 - trad, 1 - lancer, 2 - electro, 3 - shockbaton
    public int gType;
    // 0 - rifle, 1 - pistol, 2 - shotgun
    public int mType;

    public int tier;

    // Common traits
    public float fireSpeed;
    public float[] damage;
    public float range;
    public float critChance;
    public float bulletSpeed;
    public float critDmg = 0.0f;
    public float aoeSize;
    public float stunLength;

    public int bulletCount;

    public int magSize;

    public int bulletsInMag;

    public List<Charm> charms = new List<Charm>();

    public string gunName = "";

    public float kickBackAmount = 0.0f;

    private float originalFireSpeed;
    private Coroutine speedUpCoroutine;

    public Gun()
    {
        iType = itemType.Gun;
    }

    /// <summary>
    /// Create a gun object.
    /// </summary>
    /// <param name="type">0 - Traditional, 1 - Lancer, 2 - ElectroSpike, 3 - Shock Baton</param>
    /// <param name="model">0 - Rifle, 1 - Pistol, 2 - Shotgun</param>
    /// <param name="fspeed">Speed between shots - higher is slower.</param>
    /// <param name="bspeed">Speed of bullet - higher is faster.</param>
    /// <param name="dam">Lower bound of damage.</param>
    /// <param name="dam2">Uper bound of damage.</param>
    /// <param name="rng">Distance bullet travels before being destroyed.</param>
    /// <param name="crit">Chance to score a critical strike on a hit.</param>
    /// <param name="bullets">Number of bullets fired per shot.</param>
    /// <param name="r">Weapon rarity.</param>
    /// <param name="t">Weapon Tier</param>
    /// <param name="mag">Size of magazine</param>
    /// <param name="charms"> List of charms to apply to the gun.</param>
    public void createGun(int type, int model, float fspeed, float bspeed, float dam, float dam2, float rng, float crit, int bullets, int r, int t, int mag, List<Charm> charmsToAdd)
    {
        tier = t;

        iType = itemType.Gun;

        gType = type;

        fireSpeed = (float)System.Math.Round(fspeed, 2);
        originalFireSpeed = fireSpeed;
        bulletSpeed = (float)System.Math.Round(bspeed, 2);
        damage = new float[2];
        damage[0] = (float)System.Math.Round(dam, 1);
        damage[1] = (float)System.Math.Round(dam2, 1);
        range = (float)System.Math.Round(rng, 2);

        critChance = crit;

        bulletCount = bullets;

        mType = model;

        rarity = r;

        magSize = mag;

        bulletsInMag = mag;

        charms = charmsToAdd ?? new List<Charm>();

        gunName = GenerateGunName(this);

        switch(mType)
        {
            case 0: // Rifle
                kickBackAmount = 0.2f;
                break;
            case 1: // Pistol
                kickBackAmount = 0.1f;
                break;
            case 2: // Shotgun
                kickBackAmount = 0.3f;
                break;
            default:
                kickBackAmount = 0.2f;
                break;
        }
    }

    private string GenerateGunName(Gun gun)
    {
        PixelCrushers.TextTable textTable = DialogueManager.instance.gameObject.GetComponent<PixelCrushers.UILocalizationManager>().textTable;
        if (textTable == null)
            Debug.LogError("Couldn't find text table for gun names. UI elements will now be fucked up");

        var sortedCharms = (gun?.charms ?? new List<Charm>())
          .Where(c => c != null)
          .OrderBy(c => UnityEngine.Random.value)
          .ToList();

        string prefix = sortedCharms.Count > 0 && !string.IsNullOrWhiteSpace(sortedCharms[0].namePrefix)
            ? textTable.GetFieldText(sortedCharms[0].namePrefix)
            : "Standard";

        string core = sortedCharms.Count > 1 && !string.IsNullOrWhiteSpace(sortedCharms[1].nameCore)
            ? textTable.GetFieldText(sortedCharms[1].nameCore)
            : "PLACEHOLDER";

        string suffix = sortedCharms.Count > 2 && !string.IsNullOrWhiteSpace(sortedCharms[2].nameSuffix)
            ? textTable.GetFieldText(sortedCharms[2].nameSuffix)
            : "";

        return $"{prefix} {core} {suffix}".Trim();
    }

    public void speedUpNextFire()
    {
        if (originalFireSpeed <= 0f)
            originalFireSpeed = fireSpeed;

        if (fireSpeed > (originalFireSpeed * 0.3f)) // cap at 30% original
        {
            if (fireSpeed == originalFireSpeed) // first shot normal fire speed; halve it each time after
                fireSpeed *= 0.999f;
            else
                fireSpeed *= 0.9f;
        }

        if(speedUpCoroutine != null)
            PlayerController.instance.StopCoroutine(speedUpCoroutine);

        speedUpCoroutine = PlayerController.instance.StartCoroutine(speedUpFireLoop());
    }

    private IEnumerator speedUpFireLoop()
    {
        float timeToExit = fireSpeed + 0.1f;
        float timePassed = 0;

        while (timePassed < timeToExit)
        {    
            timePassed += Time.deltaTime;
            yield return null;
        }

        fireSpeed = originalFireSpeed;
    }
}

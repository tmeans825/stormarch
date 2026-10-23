using PixelCrushers.DialogueSystem;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Armor : InventoryItem
{
    // 0 - helmet, 1 - chest, 2 - legs
    public int aType;
    public armorWeight aWeight;

    public enum armorWeight
    {
        Light,
        Medium,
        Heavy
    }

    public int tier;

    // Common traits
    public float shields = 0f;

    public string spritesheetName;

    public List<Charm> charms = new List<Charm>();
    public string armorName = "";

    public Armor()
    {
        iType = itemType.Armor;
    }

    public void createArmor(int type, Armor.armorWeight weight, float s, string sprite, int r, int t, List<Charm> charmsToAdd)
    {
        iType = itemType.Armor;

        aType = type;
        aWeight = weight;

        tier = t;

        shields = (float)System.Math.Round(s, 2);

        spritesheetName = sprite;

        rarity = r;

        charms = charmsToAdd ?? new List<Charm>();
        armorName = generateArmorName(this);
    }

    private string generateArmorName(Armor armor)
    {
        PixelCrushers.TextTable textTable = DialogueManager.instance.gameObject.GetComponent<PixelCrushers.UILocalizationManager>().textTable;
        if (textTable == null)
            Debug.LogError("Couldn't find text table for armor names. UI elements will now be fucked up");

        var sortedCharms = (armor?.charms ?? new List<Charm>())
          .Where(c => c != null)
          .OrderBy(c => UnityEngine.Random.value)
          .ToList();

        string prefix = sortedCharms.Count > 0 && !string.IsNullOrWhiteSpace(sortedCharms[0].namePrefix)
            ? textTable.GetFieldText(sortedCharms[0].namePrefix)
            : "";

        string core = getCoreArmorName(armor);

        string suffix = sortedCharms.Count > 1 && !string.IsNullOrWhiteSpace(sortedCharms[1].nameSuffix)
            ? textTable.GetFieldText(sortedCharms[1].nameSuffix)
            : "";

        return $"{prefix} {core} {suffix}".Trim();
    }

    private string getCoreArmorName(Armor armor)
    {
        switch(armor.aType)
        {
            case 0:
                return getHelmetName(armor);
            case 1:
                return getChestName(armor);
            case 2:
                return getLegsName(armor);
            default:
                return "Armor";
        }
    }

    private string getHelmetName(Armor armor)
    {
        switch(armor.aWeight)
        {
            case armorWeight.Light:
                return "Veil";
            case armorWeight.Medium:
                return "Helmet Rig";
            case armorWeight.Heavy:
                return "Warplate Helm";
            default:
                return "Helmet Rig";
        }
    }

    private string getChestName(Armor armor)
    {
        switch (armor.aWeight)
        {
            case armorWeight.Light:
                return "Vest";
            case armorWeight.Medium:
                return "Plating";
            case armorWeight.Heavy:
                return "Carapace";
            default:
                return "Plating";
        }
    }   

    private string getLegsName(Armor armor)
    {
        switch (armor.aWeight)
        {
            case armorWeight.Light:
                return "Leggings";
            case armorWeight.Medium:
                return "Greaves";
            case armorWeight.Heavy:
                return "Heavy Legplates";
            default:
                return "Legplate";
        }
    }


}

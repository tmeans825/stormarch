using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using UnityEngine;
using System.Linq;
using System.Runtime.CompilerServices;

[System.Serializable]
public class Charm
{
    public string id;
    public string displayName;
    public string charmName;
    public string lore;
    public string type;
    public int budgetCost;
    public string namePrefix;
    public string nameCore;
    public string nameSuffix;
    public string appliesTo;

    // For referencing localization keys
    public string BaseKey => $"Charm/{id}";
    public string DisplayNameKey => $"{BaseKey}/displayName";
    public string CharmNameKey => $"{BaseKey}/charmName";
    public string LoreKey => $"{BaseKey}/lore";
    public string PrefixKey => $"{BaseKey}/namePrefix";
    public string CoreKey => $"{BaseKey}/nameCore";
    public string SuffixKey => $"{BaseKey}/nameSuffix";

    public static string appliesToWeaponString = "weapon";
    public static string appliesToArmorString = "armor";

    public enum CharmType
    {
        Projectile,
        Utility,
        Curse,
        Behavior,
        Armor
    }

    public CharmType GetCharmType(Charm c)
    {
        return (CharmType)System.Enum.Parse(typeof(CharmType), c.type);
    }
}

[System.Serializable]
public class CharmDatabase
{
    public List<Charm> charms;
}

public class CharmLoader : MonoBehaviour
{
    public static CharmDatabase LoadCharmDatabase()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("charms"); // Do not include ".json" extension
        if (jsonFile == null)
        {
            Debug.LogError("Could not find charms.json in Resources!");
            return null;
        }

        CharmDatabase db = JsonUtility.FromJson<CharmDatabase>(jsonFile.text);

#if UNITY_EDITOR
        var validIds = new HashSet<string>(db.charms.Select(c => c.id));
        foreach (var id in CharmCompatibility.allReferencedIds())
            if (!validIds.Contains(id))
                Debug.LogError($"CharmCompatibility references unknown charm id '{id}' — fix CharmCompatibility.exclusionGroups or charms.json");
#endif

        return db;
    }

    public static List<Charm> GetCharmsForWeapon(int rarity, List<Charm> weaponCharms)
    {
        int budget = GetWeaponCharmBudgetForRarity(rarity);
        int charmCount = (rarity > 2) ? 3 : 2; // Rare guns get 3 charms, rest get 2

        return GenerateCharms(rarity, weaponCharms, budget, charmCount);
    }

    public static List<Charm> GetCharmsForArmor(int rarity, List<Charm> armorCharms, Armor.armorWeight aWeight, int tier)
    {
        int budget = GetArmorCharmBudget(rarity, aWeight, tier); 
        int charmCount = 2;

        bool skipFirstCurse = (aWeight == Armor.armorWeight.Light); // Light armor doesn't get curses

        return GenerateCharms(rarity, armorCharms, budget, charmCount, skipFirstCurse, true, true);
    }

    private static List<Charm> GenerateCharms(int rarity, List<Charm> allCharms, int budget, int charmCount, bool skipFirstCurse = false, bool allowDuplicateTypes = false, bool unlimitedCurse = false)
    {
        if (allCharms == null || allCharms.Count == 0)
        {
            Debug.LogWarning("Charm list is empty or null.");
            return new List<Charm>();
        }

        var possibleCurseCharms = allCharms
            .Where(c => c.type == Charm.CharmType.Curse.ToString() 
                && (unlimitedCurse || c.budgetCost >= GetCharmCurseMaxForRarity(rarity)))
            .ToList();

        var allPositiveCharms = allCharms.Where(c => c.type != Charm.CharmType.Curse.ToString()).ToList();
        var selected = new List<Charm>();
        int spentBudget = 0;

        // First, add curse
        if (!skipFirstCurse)
        {
            var curseCharm = getRandomCharm(possibleCurseCharms); // TODO - don't add curse if light armor
            if (curseCharm == null)
            {
                Debug.LogWarning("Failed to select a curse charm.");
            }
            else
            {
                selected.Add(curseCharm);
                spentBudget += curseCharm.budgetCost;
            }
        }

        // Next, add middle positive charms, leaving at least 1 in the budget for next charms
        while (selected.Count < charmCount - 1)
        {
            int remainingBudget = budget - spentBudget - (charmCount - selected.Count - 1);

            var possiblePositiveCharms = allPositiveCharms
                .Where(c => c.budgetCost <= remainingBudget && canSelectCharm(c, selected, allowDuplicateTypes))
                .ToList();

            var charm = getRandomCharm(possiblePositiveCharms);
            if (charm == null)
            {
                Debug.LogWarning("Failed to select a middle positive charm.");
                break;
            }

            selected.Add(charm);
            spentBudget += charm.budgetCost;
        }

        // Add final charm that will max out the budget, or get as close as possible. Can be curse or positive - curse should only get called for heavy chests
        int finalRemainingBudget = budget - spentBudget;
        List<Charm> finalPossibleCharms;
        List<Charm> charmListToPullFrom;

        if (finalRemainingBudget > 0)
            charmListToPullFrom = allPositiveCharms;
        else if (finalRemainingBudget < 0)
            charmListToPullFrom = possibleCurseCharms;
        else
        {
            Debug.LogWarning("Hit exact budget with one charm left, ending selection early.");
            return selected; // Budget is exactly met, return what we have
        }

        finalPossibleCharms = charmListToPullFrom
                .Where(c => c.budgetCost == finalRemainingBudget && canSelectCharm(c, selected, allowDuplicateTypes))
                .ToList();

        if (finalPossibleCharms.Count == 0)
            finalPossibleCharms = charmListToPullFrom
                .Where(c => c.budgetCost == finalRemainingBudget + 1 && canSelectCharm(c, selected, allowDuplicateTypes))
                .ToList();

        if (finalPossibleCharms.Count == 0)
            finalPossibleCharms = charmListToPullFrom
                .Where(c => c.budgetCost == finalRemainingBudget - 1 && canSelectCharm(c, selected, allowDuplicateTypes))
                .ToList();

        if (finalPossibleCharms.Count > 0)
        {
            var charm = getRandomCharm(finalPossibleCharms);
            if (charm != null)
            {
                selected.Add(charm);
                spentBudget += charm.budgetCost;
            }
        }
        else
            Debug.LogWarning("No possible final charms to add within budget, ending selection early.");

        return selected;
    }

    private static Charm getRandomCharm(List<Charm> possibleCharms)
    {
        if (possibleCharms == null || possibleCharms.Count == 0)
            return null;

        return possibleCharms[UnityEngine.Random.Range(0, possibleCharms.Count)];
    }

    private static bool canSelectCharm(Charm c, List<Charm> selected, bool allowDuplicateTypes)
    {
        if (selected.Any(s => s.id == c.id)) return false;
        if (!allowDuplicateTypes && selected.Any(s => s.type == c.type)) return false;
        if (!CharmCompatibility.isCompatibleWithSelection(c, selected)) return false;
        return true;
    }

    private static int GetWeaponCharmBudgetForRarity(int rarity)
    {
        switch (rarity)
        {
            case 0: // Craftable
                return 0;
            case 1: // Common
                return 0;
            case 2: // Uncommon
                return 1;
            case 3: // Rare
                return 2;
            case 4: // Exceptional
                return 3;
            default:
                Debug.LogWarning("Invalid rarity passed to GetCharmBudgetForRarity. Defaulting to Common.");
                return 0;
        }
    }

    private static int GetArmorCharmBudget(int rarity, Armor.armorWeight aWeight, int tier)
    {
        switch(aWeight)
        {
            case Armor.armorWeight.Light:
                return GetLightArmorBudget(tier, rarity);
            case Armor.armorWeight.Medium:
                return GetHardcodedArmorBudget(new int[] { -1, -1, -1, 0, 1 }, rarity);
            case Armor.armorWeight.Heavy:
                return GetHardcodedArmorBudget(new int[] { -4, -4, -4, -3, -2 }, rarity); 
            default:
                Debug.LogWarning("Invalid armor weight passed to GetArmorCharmBudget. Defaulting to 0.");
                return 0;
        }
    }

    private static int GetLightArmorBudget(int tier, int rarity)
    {
        switch(tier)
        {
            case 1:
                return GetHardcodedArmorBudget(new int[] { 2, 2, 2, 3, 4 }, rarity);
            case 2:
                return GetHardcodedArmorBudget(new int[] { 3, 3, 3, 4, 5 }, rarity);
            case 3:      
            default:
                return GetHardcodedArmorBudget(new int[] { 4, 4, 4, 5, 6 }, rarity);
        }
    }

    private static int GetHardcodedArmorBudget(int[] budgetArray, int rarity)
    {
        if (rarity < 0 || rarity >= budgetArray.Length)
        {
            Debug.LogWarning("Invalid rarity passed to GetHardcodedArmorBudget. Defaulting to Exceptional.");
            return budgetArray[3];
        }
        return budgetArray[rarity];
    }


    private static int GetCharmCurseMaxForRarity(int rarity)
    {
        switch (rarity)
        {
            case 0: // Craftable
                return -2;
            case 1: // Common - max positive swing is a -2 curse to +2 charm
                return -2;
            case 2: // Uncommon - max positive swing is a -2 curse to +3 charm
                return -2;
            case 3: // Rare - max positive swing is a -4 curse to a +1 charm and +5 charm
                return -4;
            case 4: // Exceptional - max positive swing is a -6 curse to a +1 charm and +8 charm, which doesn't exist.. so +6 or +7 charm
                return -6;
            default:
                return -6;
        }
    }

}

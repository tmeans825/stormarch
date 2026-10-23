using System.Collections.Generic;
using UnityEngine;

// Static data table for the alignment identity / abilities framework.
// Follows the project's "content lives in code" convention (see LootMaster, skillTree) -
// no ScriptableObjects. All player-facing strings are LocalizeUI field keys; the
// Dialogue System localization DB needs matching entries (editor content task).
//
// Axis convention (PlayerController.alignmentArray): x = purpose (Self+ / Community-),
// y = affinity (Order+ / Freedom-). Each quadrant surfaces 3 abilities: one purpose-pole
// ability and one affinity-pole ability (both SHARED with the adjacent quadrant on that
// pole), plus one Cross ability unique to the quadrant (needs both thresholds). The Center
// instead runs the Negotiator path, unlocking one ability per act spent centered.

public enum AlignmentUnlockKind { PurposePole, AffinityPole, Cross, Negotiator }
public enum AlignmentPole { None, Self, Community, Order, Freedom }

public class AlignmentAbility
{
    public string id;
    public string nameKey;
    public string descKey;
    public string lockHintKey;
    public string iconResource; // Resources/ path, loaded by name (mirrors spritesheet convention)
    public AlignmentUnlockKind kind;
    public AlignmentPole purposePole;   // Self / Community for PurposePole + Cross
    public AlignmentPole affinityPole;  // Order / Freedom  for AffinityPole + Cross
    public int purposeThreshold;
    public int affinityThreshold;
    public int negotiatorTier;          // Negotiator only: unlock when act >= tier + 1

    public AlignmentAbility(string id, string nameKey, string descKey, string lockHintKey, string iconResource,
        AlignmentUnlockKind kind, AlignmentPole purposePole, AlignmentPole affinityPole,
        int purposeThreshold, int affinityThreshold, int negotiatorTier)
    {
        this.id = id;
        this.nameKey = nameKey;
        this.descKey = descKey;
        this.lockHintKey = lockHintKey;
        this.iconResource = iconResource;
        this.kind = kind;
        this.purposePole = purposePole;
        this.affinityPole = affinityPole;
        this.purposeThreshold = purposeThreshold;
        this.affinityThreshold = affinityThreshold;
        this.negotiatorTier = negotiatorTier;
    }
}

public static class AlignmentData
{
    public const int POLE_THRESHOLD = 60;  // axis value to unlock a pole ability
    public const int CROSS_THRESHOLD = 60; // both axes required for a cross ability

    // ----- Ability definitions (8 quadrant + 3 negotiator) -----
    // Placeholder content/thresholds; effects are stubbed this pass. Tune freely.
    public static readonly AlignmentAbility[] allAbilities =
    {
        // Purpose poles (shared by the two quadrants on that side)
        new AlignmentAbility("self",      "AlignAbility_Self_Name",      "AlignAbility_Self_Desc",      "AlignAbility_Self_Lock",      "AlignmentIcons/self",
            AlignmentUnlockKind.PurposePole, AlignmentPole.Self,      AlignmentPole.None, POLE_THRESHOLD, 0, 0),
        new AlignmentAbility("community", "AlignAbility_Community_Name", "AlignAbility_Community_Desc", "AlignAbility_Community_Lock", "AlignmentIcons/community",
            AlignmentUnlockKind.PurposePole, AlignmentPole.Community, AlignmentPole.None, POLE_THRESHOLD, 0, 0),

        // Affinity poles (shared by the two quadrants on that side)
        new AlignmentAbility("order",   "AlignAbility_Order_Name",   "AlignAbility_Order_Desc",   "AlignAbility_Order_Lock",   "AlignmentIcons/order",
            AlignmentUnlockKind.AffinityPole, AlignmentPole.None, AlignmentPole.Order,   0, POLE_THRESHOLD, 0),
        new AlignmentAbility("freedom", "AlignAbility_Freedom_Name", "AlignAbility_Freedom_Desc", "AlignAbility_Freedom_Lock", "AlignmentIcons/freedom",
            AlignmentUnlockKind.AffinityPole, AlignmentPole.None, AlignmentPole.Freedom, 0, POLE_THRESHOLD, 0),

        // Cross abilities (unique per quadrant; need BOTH thresholds)
        new AlignmentAbility("cross_ordercommunity",   "AlignAbility_CrossOC_Name", "AlignAbility_CrossOC_Desc", "AlignAbility_CrossOC_Lock", "AlignmentIcons/cross_oc",
            AlignmentUnlockKind.Cross, AlignmentPole.Community, AlignmentPole.Order,   CROSS_THRESHOLD, CROSS_THRESHOLD, 0),
        new AlignmentAbility("cross_orderself",        "AlignAbility_CrossOS_Name", "AlignAbility_CrossOS_Desc", "AlignAbility_CrossOS_Lock", "AlignmentIcons/cross_os",
            AlignmentUnlockKind.Cross, AlignmentPole.Self,      AlignmentPole.Order,   CROSS_THRESHOLD, CROSS_THRESHOLD, 0),
        new AlignmentAbility("cross_freedomself",      "AlignAbility_CrossFS_Name", "AlignAbility_CrossFS_Desc", "AlignAbility_CrossFS_Lock", "AlignmentIcons/cross_fs",
            AlignmentUnlockKind.Cross, AlignmentPole.Self,      AlignmentPole.Freedom, CROSS_THRESHOLD, CROSS_THRESHOLD, 0),
        new AlignmentAbility("cross_freedomcommunity", "AlignAbility_CrossFC_Name", "AlignAbility_CrossFC_Desc", "AlignAbility_CrossFC_Lock", "AlignmentIcons/cross_fc",
            AlignmentUnlockKind.Cross, AlignmentPole.Community, AlignmentPole.Freedom, CROSS_THRESHOLD, CROSS_THRESHOLD, 0),

        // Negotiator (Center) - one per act spent centered, from Act 2
        new AlignmentAbility("negotiator_1", "AlignAbility_Negotiator1_Name", "AlignAbility_Negotiator1_Desc", "AlignAbility_Negotiator1_Lock", "AlignmentIcons/negotiator1",
            AlignmentUnlockKind.Negotiator, AlignmentPole.None, AlignmentPole.None, 0, 0, 1),
        new AlignmentAbility("negotiator_2", "AlignAbility_Negotiator2_Name", "AlignAbility_Negotiator2_Desc", "AlignAbility_Negotiator2_Lock", "AlignmentIcons/negotiator2",
            AlignmentUnlockKind.Negotiator, AlignmentPole.None, AlignmentPole.None, 0, 0, 2),
        new AlignmentAbility("negotiator_3", "AlignAbility_Negotiator3_Name", "AlignAbility_Negotiator3_Desc", "AlignAbility_Negotiator3_Lock", "AlignmentIcons/negotiator3",
            AlignmentUnlockKind.Negotiator, AlignmentPole.None, AlignmentPole.None, 0, 0, 3),
    };

    // ----- Quadrant -> displayed ability trio (pole ids are shared across quadrants) -----
    public static readonly Dictionary<PlayerController.alignmentQuadrant, string[]> quadrantAbilityIds =
        new Dictionary<PlayerController.alignmentQuadrant, string[]>
    {
        { PlayerController.alignmentQuadrant.OrderSelf,        new[] { "self",      "order",   "cross_orderself" } },
        { PlayerController.alignmentQuadrant.FreedomSelf,      new[] { "self",      "freedom", "cross_freedomself" } },
        { PlayerController.alignmentQuadrant.FreedomCommunity, new[] { "community", "freedom", "cross_freedomcommunity" } },
        { PlayerController.alignmentQuadrant.OrderCommunity,   new[] { "community", "order",   "cross_ordercommunity" } },
    };

    // Center shows the Negotiator track instead of a quadrant trio.
    public static readonly string[] negotiatorAbilityIds = { "negotiator_1", "negotiator_2", "negotiator_3" };

    // ----- Identity title + quote keys per quadrant (Center handled in AlignmentManager) -----
    public static readonly Dictionary<PlayerController.alignmentQuadrant, string> quadrantTitleKey =
        new Dictionary<PlayerController.alignmentQuadrant, string>
    {
        { PlayerController.alignmentQuadrant.Center,           "AlignTitle_Center" },
        { PlayerController.alignmentQuadrant.OrderSelf,        "AlignTitle_OrderSelf" },
        { PlayerController.alignmentQuadrant.FreedomSelf,      "AlignTitle_FreedomSelf" },
        { PlayerController.alignmentQuadrant.FreedomCommunity, "AlignTitle_FreedomCommunity" },
        { PlayerController.alignmentQuadrant.OrderCommunity,   "AlignTitle_OrderCommunity" },
    };
    public const string NEGOTIATOR_TITLE_KEY = "AlignTitle_Negotiator";

    public static readonly Dictionary<PlayerController.alignmentQuadrant, string> quadrantQuoteKey =
        new Dictionary<PlayerController.alignmentQuadrant, string>
    {
        { PlayerController.alignmentQuadrant.Center,           "AlignQuote_Center" },
        { PlayerController.alignmentQuadrant.OrderSelf,        "AlignQuote_OrderSelf" },
        { PlayerController.alignmentQuadrant.FreedomSelf,      "AlignQuote_FreedomSelf" },
        { PlayerController.alignmentQuadrant.FreedomCommunity, "AlignQuote_FreedomCommunity" },
        { PlayerController.alignmentQuadrant.OrderCommunity,   "AlignQuote_OrderCommunity" },
    };

    // ----- Lookup -----
    private static readonly Dictionary<string, AlignmentAbility> byId = new Dictionary<string, AlignmentAbility>();

    static AlignmentData()
    {
        foreach (AlignmentAbility a in allAbilities)
            byId[a.id] = a;

#if UNITY_EDITOR
        validate();
#endif
    }

    public static AlignmentAbility get(string id)
    {
        if (id != null && byId.TryGetValue(id, out AlignmentAbility a))
            return a;
        return null;
    }

#if UNITY_EDITOR
    // Editor-only guard for the stringly-typed coupling between this table, the quadrant
    // maps, and the LocalizeUI keys. Surfaces typos in-editor instead of silently at runtime.
    private static void validate()
    {
        var seen = new HashSet<string>();
        foreach (AlignmentAbility a in allAbilities)
        {
            if (!seen.Add(a.id))
                Debug.LogError($"AlignmentData: duplicate ability id '{a.id}'.");
            if (string.IsNullOrEmpty(a.nameKey) || string.IsNullOrEmpty(a.descKey) || string.IsNullOrEmpty(a.lockHintKey))
                Debug.LogError($"AlignmentData: ability '{a.id}' has an empty LocalizeUI key.");
        }

        foreach (var kv in quadrantAbilityIds)
        {
            if (kv.Value == null || kv.Value.Length != 3)
                Debug.LogError($"AlignmentData: quadrant {kv.Key} must map to exactly 3 abilities.");
            else
                foreach (string id in kv.Value)
                    if (get(id) == null)
                        Debug.LogError($"AlignmentData: quadrant {kv.Key} references unknown ability id '{id}'.");
        }

        foreach (string id in negotiatorAbilityIds)
            if (get(id) == null)
                Debug.LogError($"AlignmentData: negotiator list references unknown ability id '{id}'.");
    }
#endif
}

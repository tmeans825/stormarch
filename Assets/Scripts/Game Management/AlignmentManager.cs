using UnityEngine;

// Static logic for the alignment abilities framework. Holds no state of its own - the
// unlocked-ability set lives on PlayerController (ES3-saved). Static (not a scene
// singleton) on purpose: reconcile is driven by explicit calls from PlayerController and
// the alignment panel, which avoids any event-ordering race with currentAlignment being
// recomputed during load (see PlayerController.previousSceneDataLoaded).
public static class AlignmentManager
{
    // Evaluate every ability's unlock condition against current alignment + act and add
    // newly-satisfied ones to the saved set. Idempotent: only NEW unlocks act.
    //   notify = true  -> dialogue-driven threshold crossings, fire the unlock popup.
    //   notify = false -> load / backfill / panel-open, stay silent (no retroactive spam).
    public static void reconcileUnlocks(bool notify)
    {
        PlayerController pc = PlayerController.instance;
        if (pc == null)
            return;
        if (pc.unlockedAlignmentAbilities == null)
            pc.unlockedAlignmentAbilities = new System.Collections.Generic.List<string>();

        // Derive the quadrant straight from alignmentArray rather than the cached
        // currentAlignment, so reconcile is correct even if called before it is refreshed.
        Vector2 a = pc.alignmentArray;
        PlayerController.alignmentQuadrant quad = pc.getAlignmentOfVec(a);
        int act = (SaveManager.instance != null) ? SaveManager.instance.act : 1;

        bool anyNewUnlock = false;

        foreach (AlignmentAbility ability in AlignmentData.allAbilities)
        {
            if (pc.unlockedAlignmentAbilities.Contains(ability.id))
                continue;
            if (!isConditionMet(ability, a, quad, act))
                continue;

            pc.unlockedAlignmentAbilities.Add(ability.id);
            applyAbilityEffect(ability.id);
            anyNewUnlock = true;
        }

        // One generic toast for the whole pass (collapses simultaneous pole + cross unlocks).
        // Silent path (notify == false: load / backfill / panel open) never notifies.
        if (notify && anyNewUnlock && SmallAlertsBehavior.instance != null)
            SmallAlertsBehavior.instance.displayAbilityUnlock();
    }

    private static bool isConditionMet(AlignmentAbility ability, Vector2 a, PlayerController.alignmentQuadrant quad, int act)
    {
        switch (ability.kind)
        {
            case AlignmentUnlockKind.PurposePole:
                return poleMet(ability.purposePole, a);
            case AlignmentUnlockKind.AffinityPole:
                return poleMet(ability.affinityPole, a);
            case AlignmentUnlockKind.Cross:
                return poleMet(ability.purposePole, a) && poleMet(ability.affinityPole, a);
            case AlignmentUnlockKind.Negotiator:
                // Stay-in-Center, act-gated. Monotonic: once added it never leaves the set,
                // so dropping out of Center keeps earned negotiator abilities.
                return quad == PlayerController.alignmentQuadrant.Center && act >= ability.negotiatorTier + 1;
            default:
                return false;
        }
    }

    private static bool poleMet(AlignmentPole pole, Vector2 a)
    {
        switch (pole)
        {
            case AlignmentPole.Self:      return a.x >= AlignmentData.POLE_THRESHOLD;
            case AlignmentPole.Community: return a.x <= -AlignmentData.POLE_THRESHOLD;
            case AlignmentPole.Order:     return a.y >= AlignmentData.POLE_THRESHOLD;
            case AlignmentPole.Freedom:   return a.y <= -AlignmentData.POLE_THRESHOLD;
            default:                      return false;
        }
    }

    // ----- UI query helpers -----

    public static bool isUnlocked(string id)
    {
        PlayerController pc = PlayerController.instance;
        return pc != null && pc.unlockedAlignmentAbilities != null && pc.unlockedAlignmentAbilities.Contains(id);
    }

    // The 3 abilities to display for a quadrant; Center shows the Negotiator track.
    public static AlignmentAbility[] getDisplayAbilities(PlayerController.alignmentQuadrant quad)
    {
        string[] ids = quad == PlayerController.alignmentQuadrant.Center
            ? AlignmentData.negotiatorAbilityIds
            : (AlignmentData.quadrantAbilityIds.TryGetValue(quad, out string[] q) ? q : null);

        if (ids == null)
            return new AlignmentAbility[0];

        AlignmentAbility[] ret = new AlignmentAbility[ids.Length];
        for (int i = 0; i < ids.Length; i++)
            ret[i] = AlignmentData.get(ids[i]);
        return ret;
    }

    public static string getTitleKey(PlayerController.alignmentQuadrant quad, int act)
    {
        if (quad == PlayerController.alignmentQuadrant.Center && act >= 2)
            return AlignmentData.NEGOTIATOR_TITLE_KEY;
        return AlignmentData.quadrantTitleKey.TryGetValue(quad, out string k) ? k : AlignmentData.quadrantTitleKey[PlayerController.alignmentQuadrant.Center];
    }

    public static string getQuoteKey(PlayerController.alignmentQuadrant quad)
    {
        return AlignmentData.quadrantQuoteKey.TryGetValue(quad, out string k) ? k : AlignmentData.quadrantQuoteKey[PlayerController.alignmentQuadrant.Center];
    }

    // TODO: gameplay effects are intentionally stubbed this pass (infrastructure only).
    // Wire real effects here keyed by ability id when the design is ready.
    private static void applyAbilityEffect(string id)
    {
    }
}

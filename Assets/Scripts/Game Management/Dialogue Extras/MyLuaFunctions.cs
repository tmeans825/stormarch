using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PixelCrushers.DialogueSystem;

public class MyLuaFunctions : MonoBehaviour
{
    void OnEnable()
    {
        Lua.RegisterFunction("updateAlignment", this, SymbolExtensions.GetMethodInfo(() => updateAlignment(1, 1)));
        Lua.RegisterFunction("checkQuadrant", this, SymbolExtensions.GetMethodInfo(() => checkQuadrant()));
        Lua.RegisterFunction("needsToCreateFlag", this, SymbolExtensions.GetMethodInfo(() => needsToCreateFlag()));
        Lua.RegisterFunction("getBeatProgress", this, SymbolExtensions.GetMethodInfo(() => getBeatProgress((double)0)));
        Lua.RegisterFunction("recordOutcome", this, SymbolExtensions.GetMethodInfo(() => recordOutcome("", "")));
        Lua.RegisterFunction("checkOutcome", this, SymbolExtensions.GetMethodInfo(() => checkOutcome("", "")));
        Lua.RegisterFunction("hasOutcome", this, SymbolExtensions.GetMethodInfo(() => hasOutcome("")));
        Lua.RegisterFunction("beginCampRest", this, SymbolExtensions.GetMethodInfo(() => beginCampRest()));
        Lua.RegisterFunction("finishCampRest", this, SymbolExtensions.GetMethodInfo(() => finishCampRest()));
        Lua.RegisterFunction("isCamping", this, SymbolExtensions.GetMethodInfo(() => isCamping()));
        Lua.RegisterFunction("summonCamp", this, SymbolExtensions.GetMethodInfo(() => summonCamp()));
        Lua.RegisterFunction("getKey", this, SymbolExtensions.GetMethodInfo(() => getKey("")));
        Lua.RegisterFunction("hasSelfAlignment", this, SymbolExtensions.GetMethodInfo(() => hasSelfAlignment()));
        Lua.RegisterFunction("hasOrderAlignment", this, SymbolExtensions.GetMethodInfo(() => hasOrderAlignment()));
        Lua.RegisterFunction("hasFreedomAlignment", this, SymbolExtensions.GetMethodInfo(() => hasFreedomAlignment()));
        Lua.RegisterFunction("hasCommunityAlignment", this, SymbolExtensions.GetMethodInfo(() => hasCommunityAlignment()));
    }

    void OnDisable()
    {
        // Note: If this script is on your Dialogue Manager & the Dialogue Manager is configured
        // as Don't Destroy On Load (on by default), don't unregister Lua functions.
        Lua.UnregisterFunction("updateAlignment"); // <-- Only if not on Dialogue Manager.
    }

    public void updateAlignment(double purpose, double affinity)
    {
        PlayerController.instance.updateAlignment(((int)purpose), ((int)affinity));
        if (DialogueAutosaver.instance != null) DialogueAutosaver.instance.flagImportant();
    }

    public PlayerController.alignmentQuadrant checkQuadrant()
    {
        return PlayerController.instance.currentAlignment;
    }

    public bool hasSelfAlignment()
    {
        return (PlayerController.instance.currentAlignment==PlayerController.alignmentQuadrant.FreedomSelf || PlayerController.instance.currentAlignment==PlayerController.alignmentQuadrant.OrderSelf);
    }

    public bool hasOrderAlignment()
    {
        return (PlayerController.instance.currentAlignment==PlayerController.alignmentQuadrant.OrderSelf || PlayerController.instance.currentAlignment==PlayerController.alignmentQuadrant.OrderCommunity);
    }

    public bool hasFreedomAlignment()
    {
        return (PlayerController.instance.currentAlignment==PlayerController.alignmentQuadrant.FreedomSelf || PlayerController.instance.currentAlignment==PlayerController.alignmentQuadrant.FreedomCommunity);
    }

    public bool hasCommunityAlignment()
    {
        return (PlayerController.instance.currentAlignment==PlayerController.alignmentQuadrant.FreedomCommunity || PlayerController.instance.currentAlignment==PlayerController.alignmentQuadrant.OrderCommunity);
    }

    // CONDITIONS
    public bool needsToCreateFlag()
    {
        if (PlayerController.instance.flagDesignInt == -1 && PlayerController.instance.homesteadTier >= 1)
            return true;

        return false;
    }

    public int getBeatProgress(double beatLine)
    {
        if (sceneBeatManager.instance == null)
            Debug.LogError("No scene beat manager exists, but you're trying to reference beat progress.");

        return sceneBeatManager.instance.getBeatProgress((int)beatLine);
    }

    // Notable Outcomes
    public void recordOutcome(string outcomeId, string outcomeValue)
    {
        NotableOutcomesTracker.instance.recordOutcome(outcomeId, outcomeValue);
        if (DialogueAutosaver.instance != null) DialogueAutosaver.instance.flagImportant();
    }

    public bool checkOutcome(string outcomeId, string expectedValue)
    {
        return NotableOutcomesTracker.instance.checkOutcome(outcomeId, expectedValue);
    }

    public bool hasOutcome(string outcomeId)
    {
        return NotableOutcomesTracker.instance.hasOutcome(outcomeId);
    }

    // Camp
    public void beginCampRest()
    {
        EventManager.instance.startCamping();
    }

    public void finishCampRest()
    {
        EventManager.instance.endCamping();
    }

    public bool isCamping()
    {
        return EventManager.instance.isCampingNow();
    }

    // Summons/moves the single Deployed Camp to the Camp Summon Rock the player is interacting with.
    // The rock is the conversation's conversant (DialogueSystemTrigger resolves the conversant to its
    // own transform when none is assigned); selectedTouchable is a fallback.
    public void summonCamp()
    {
        Transform t = DialogueManager.currentConversant;
        CampSummonRock rock = (t != null) ? t.GetComponent<CampSummonRock>() : null;

        if (rock == null && ObjectSelector.current != null && ObjectSelector.current.selectedTouchable != null)
            rock = ObjectSelector.current.selectedTouchable.GetComponent<CampSummonRock>();

        if (rock != null)
            rock.summonCampHere();
#if UNITY_EDITOR
        else
            Debug.LogWarning("summonCamp(): couldn't resolve a CampSummonRock from the conversation.");
#endif
    }

    // Keybinds — lets dialogue print the player's live key for an action, e.g.
    // "Press [lua(getKey('Deploy Firebug'))] to deploy a firebug." Reads the same merged
    // keybind store the game uses (defaults + saved rebinds), so it never goes stale.
    // Returns the raw KeyCode name (e.g. "G", "Mouse1", "LeftShift").
    public string getKey(string actionName)
    {
        if (SettingsMenu.tryGetDisplayKeyBinding(actionName, out KeyCode keyCode))
            return keyCode.ToString();

#if UNITY_EDITOR
        Debug.LogWarning($"getKey: no keybinding named '{actionName}'. " +
            "It must exactly match a DEFAULT_BUTTON_KEYS entry in SettingsMenu.");
#endif
        return actionName; // fallback keeps the line readable if a token is mistyped
    }
}

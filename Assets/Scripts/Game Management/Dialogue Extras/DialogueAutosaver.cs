using System.Collections;
using PixelCrushers.DialogueSystem;
using UnityEngine;

public class DialogueAutosaver : MonoBehaviour
{
    [System.NonSerialized]
    public static DialogueAutosaver instance;

    private bool inConversation = false;
    private bool importantThingHappened = false;
    private bool saveQueued = false;
    private int convToken = 0;

    void Awake()
    {
        if (instance == null && this != instance)
            instance = this;
        else
            Destroy(gameObject);
    }

    void OnConversationStart(Transform actor)
    {
        convToken++;
        inConversation = true;
        importantThingHappened = false;
        saveQueued = false;
    }

    void OnConversationEnd(Transform actor)
    {
        if (saveQueued) return;
        saveQueued = true;
        StartCoroutine(deferredCheckAndSave(convToken, importantThingHappened));
    }

    // Defers one frame so post-conversation Lua scripts and same-frame flagImportant()
    // calls still register before we evaluate. The snapshot ensures that if a chained
    // follow-up conversation starts and resets shared state before this coroutine
    // resumes, the original conversation's important flag isn't lost. Long-running
    // coroutines that progress beats seconds later (e.g.
    // OwensAct1BeatManager.beginGuardianCountdown) are still missed by design —
    // catching those would require listening to progressBeat directly.
    private IEnumerator deferredCheckAndSave(int myToken, bool snapshotImportant)
    {
        yield return null;

        bool shouldSave = snapshotImportant || (myToken == convToken && importantThingHappened);

        if (shouldSave)
            trySaveNow();

        if (myToken == convToken)
        {
            inConversation = false;
            importantThingHappened = false;
            saveQueued = false;
        }
    }

    public void flagImportant()
    {
        if (inConversation) importantThingHappened = true;
    }

    // Sent by QuestLog via DialogueManager.BroadcastMessage whenever a quest's overall
    // state or an entry's state changes (Lua SetQuestState/SetQuestEntryState and all
    // C# QuestLog setters funnel through the same broadcast). Changes fired outside a
    // conversation are ignored by flagImportant's inConversation guard.
    void OnQuestStateChange(string questName)
    {
        flagImportant();
    }

    void OnQuestEntryStateChange(QuestEntryArgs args)
    {
        flagImportant();
    }

    private void trySaveNow()
    {
        if (GameEvents.current.enemiesInCombat > 0) return;
        if (PlayerController.instance.isBleeding) return;
        if (PlayerController.instance.isDead) return;

        SaveManager.instance.saveSceneData();
        Debug.Log("[DialogueAutosaver] saved after dialogue");
    }
}

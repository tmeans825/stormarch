using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PixelCrushers.DialogueSystem;

public class ObjectiveDialogueManagerBehavior : MonoBehaviour
{
    public void OnQuestEntryStateChange(QuestEntryArgs args)
    {
        string title = args.questName;
        int entry = args.entryNumber;

        if(QuestLog.GetQuestState(title) == QuestState.Success)
        {
            SmallAlertsBehavior.instance.displayObjectiveAlert("OBJECTIVE COMPLETE");
        }
        else if(entry == 1 && QuestLog.GetQuestEntryState(title, entry) == QuestState.Active)
        {
            SmallAlertsBehavior.instance.displayObjectiveAlert("NEW OBJECTIVE ADDED");
        }
        else
        {
            SmallAlertsBehavior.instance.displayObjectiveAlert("OBJECTIVE UPDATED");
        }

    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PixelCrushers.DialogueSystem;

public class RecorderBehavior : MonoBehaviour
{
    public int id;
    public bool recording = false;

    private void Start()
    {
        GameEvents.current.onNoteRecordingPickup += noteRecordingPickup;
        GameEvents.current.onPreviousSceneDataLoaded += previousSceneDataLoaded;       
    }

    private void previousSceneDataLoaded()
    {
        tryDeactivate();
    }

    public void use()
    {
        string conversation = GetComponent<DialogueSystemTrigger>().conversation;
        RecordingsMenu.instance.addRecording(id,conversation);

        if (recording)
            AudioMaster.instance.PlaySound("Recording");
        else
            AudioMaster.instance.PlaySound("Paper");

        GameEvents.current.noteRecordingPickup();
        DialogueIndicatorManager.instance.setSubtitleFontMode(recording ? SubtitleFontMode.Default : SubtitleFontMode.Note);
        DialogueManager.instance.StartConversation(conversation);
        tryDeactivate();
    }

    private void noteRecordingPickup()
    {        
        tryDeactivate();
    }

    private void tryDeactivate()
    {        
        if (RecordingsMenu.instance.recordingUnlockedMatrix[id])
        {
            GameEvents.current.onNoteRecordingPickup -= noteRecordingPickup;
            GameEvents.current.onPreviousSceneDataLoaded -= previousSceneDataLoaded;
            gameObject.SetActive(false);
        }
    }
}

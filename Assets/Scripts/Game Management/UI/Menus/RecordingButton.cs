using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using PixelCrushers.DialogueSystem;


public class RecordingButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public int recordingID;
    public bool isRecording = false;
    public bool isMyra = false;

    [System.NonSerialized]
    public string conversationName;

    private Button button;

    private void Awake()
    {
        /*textTable = DialogueManager.instance.gameObject.GetComponent<UILocalizationManager>().textTable;
        if (textTable == null)
            Debug.LogError("Couldn't find text table. UI elements will now be fucked up");*/

        button = GetComponent<Button>();

        button.interactable = RecordingsMenu.instance.recordingUnlockedMatrix[recordingID];

        button.onClick.AddListener(TaskOnClick);
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button.IsInteractable())
        {
            RecordingsMenu.instance.updateMouseoverText(conversationName);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (button.IsInteractable())
            RecordingsMenu.instance.turnOffText();
    }

    public void unlockButton(string conversation)
    {
        conversationName = conversation;
        button.interactable = true;
    }

    public bool isInteractable()
    {
        return button.interactable;
    }

    void TaskOnClick()
    {
        if (PlayerController.instance.isInCombat)
            return;

        if(isRecording)
            AudioMaster.instance.PlaySound("Recording");
        else
            AudioMaster.instance.PlaySound("Paper");

        DialogueIndicatorManager.instance.setSubtitleFontMode(isRecording ? SubtitleFontMode.Default : SubtitleFontMode.Note);
        DialogueManager.instance.StartConversation(conversationName);
    }
}


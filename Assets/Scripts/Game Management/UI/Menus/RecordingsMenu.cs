using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using PixelCrushers;
using PixelCrushers.DialogueSystem;

public class RecordingsMenu : MonoBehaviour
{
    [System.NonSerialized]
    [ES3NonSerializable]
    public static RecordingsMenu instance;

    public GameObject nameTextObj;
    public GameObject buttonParent;

    public Sprite recordingButtonSprite;
    public Sprite noteButtonSprite;
    [ES3NonSerializable]
    public Sprite lockedButtonSprite;

    //private TextMeshProUGUI nameText;
    private LocalizeUI localize;

    [System.NonSerialized]
    [ES3Serializable]
    public bool[] recordingUnlockedMatrix;
    [System.NonSerialized]
    [ES3Serializable]
    public string[] conversationMatrix;

    // Start is called before the first frame update
    private void Awake()
    {
        if (instance == null && this != instance)
        {
            instance = this;
        }
        else
            Destroy(this);

        //nameText = nameTextObj.GetComponent<TextMeshProUGUI>();
        localize = nameTextObj.GetComponent<LocalizeUI>();

        int maxID = 0;
        foreach (RecordingButton thisButton in getButtons())
            maxID = Mathf.Max(maxID, thisButton.recordingID);

        recordingUnlockedMatrix = new bool[maxID + 1];
        conversationMatrix = new string[recordingUnlockedMatrix.Length];

        foreach (RecordingButton thisButton in getButtons())
            applyButtonSprite(thisButton);
    }

    private void Start()
    {
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        foreach (RecordingButton thisButton in getButtons())
            refreshButton(thisButton);
    }

    private RecordingButton[] getButtons()
    {
        return buttonParent.GetComponentsInChildren<RecordingButton>(true);
    }

    private void refreshButton(RecordingButton thisButton)
    {
        if (recordingUnlockedMatrix[thisButton.recordingID] && !thisButton.isInteractable())
            thisButton.unlockButton(conversationMatrix[thisButton.recordingID]);

        applyButtonSprite(thisButton);
    }

    private void applyButtonSprite(RecordingButton thisButton)
    {
        Sprite sprite;
        if (!recordingUnlockedMatrix[thisButton.recordingID])
            sprite = lockedButtonSprite;
        else
            sprite = thisButton.isRecording ? recordingButtonSprite : noteButtonSprite;

        thisButton.GetComponent<Image>().sprite = sprite;
    }

        public void updateMouseoverText(string txt)
    {
        localize.SetFieldName(txt);
        nameTextObj.SetActive(true);
        //nameText.text = txt;
        
    }

    public void turnOffText()
    {
        nameTextObj.SetActive(false);
    }

    public void addRecording(int ID, string conversation)
    {
        recordingUnlockedMatrix[ID] = true;
        conversationMatrix[ID] = conversation;

        foreach (RecordingButton thisButton in getButtons())
        {
            if (thisButton.recordingID == ID)
            {
                refreshButton(thisButton);
                break;
            }
        }
    }

    public void filterButtons()
    {
        AudioMaster.instance.PlaySound("UI Hotbar Ping");
        foreach (RecordingButton thisButton in getButtons())
            thisButton.gameObject.SetActive(thisButton.isRecording && thisButton.isMyra);
    }

    public void unfilterButtons()
    {
        AudioMaster.instance.PlaySound("UI Hotbar Ping");
        foreach (RecordingButton thisButton in getButtons())
            thisButton.gameObject.SetActive(true);
    }
}

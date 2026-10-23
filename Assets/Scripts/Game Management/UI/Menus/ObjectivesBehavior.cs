using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PixelCrushers.DialogueSystem;

public class ObjectivesBehavior : MonoBehaviour
{
    public StandardUIQuestLogWindow missionLog;
    public Text buttonText;
    private bool isActiveShowing = true;

    private void Start()
    {
        gameObject.SetActive(false);
    }

    public void clickityClackityHereComesTheSmackity()
    {
        AudioMaster.instance.PlaySound("UI Hotbar Ping");
    }


    public void toggleObjectiveView()
    {
        isActiveShowing = !isActiveShowing;

        if (isActiveShowing)
        {
            buttonText.text = "Active";
            missionLog.ClickShowActiveQuestsButton();
        }
        else
        {
            buttonText.text = "Complete";
            missionLog.ClickShowCompletedQuestsButton();
        }

        AudioMaster.instance.PlaySound("UI Hotbar Ping");

    }

    private void OnEnable()
    {
        missionLog.Open();
    }

    private void OnDisable()
    {
        missionLog.Close();
    }
}

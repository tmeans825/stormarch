using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PixelCrushers;

public class ObjectiveAlertsBehavior : MonoBehaviour
{
    public Animator backdropAnimator;
    public Animator textAnimator;
    public LocalizeUI localizeUI;

    [System.NonSerialized]
    public static ObjectiveAlertsBehavior instance;

    private bool isPlaying = false;

    // STOP! THIS APPEARS TO BE DEPRECATED. SMALLALERTSBEHAVIOR IS USED INSTEAD.
    /*
     * 
     * 
     * 
     * 
     * 
     * 
     * 
     * 
     * 
     * 
     * 
     */

    private void Start()
    {
        if (instance == null && this != instance)
            instance = this;
        else
            Destroy(this);
    }

    public void displayObjectiveAlert(string text)
    {
        if (!isPlaying)
        {
            textAnimator.gameObject.SetActive(true);
            backdropAnimator.gameObject.SetActive(true);

            isPlaying = true;
            localizeUI.fieldName = text;
            localizeUI.UpdateText();

            textAnimator.SetTrigger("ObjectiveUpdate");
            backdropAnimator.SetTrigger("ObjectiveUpdate");

            if (text == "OBJECTIVE COMPLETE")
                AudioMaster.instance.PlaySound("Objective Complete");
            else
                AudioMaster.instance.PlaySound("Objective Updated");

            StartCoroutine(donePlaying());
        }
    }

    public IEnumerator donePlaying()
    {
        yield return new WaitForSeconds(1f);
        Debug.Log("Done Playing");
        isPlaying = false;
    }
}

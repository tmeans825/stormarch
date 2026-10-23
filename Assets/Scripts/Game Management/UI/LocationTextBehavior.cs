using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using PixelCrushers;
using PixelCrushers.DialogueSystem.Wrappers;
using UnityEngine.SceneManagement;

public class LocationTextBehavior : MonoBehaviour
{
    public float delayBeforeAnimTime = 1f;
    public float delayBeforeTypeWriterTime = 1f;
    private TextMeshProUGUI tmp;
    private Animator anim;
    private LocalizeUI localize;
    private TextMeshProTypewriterEffect typewrite;

    [System.NonSerialized]
    public static LocationTextBehavior instance;

    void Start()
    {
        if (instance == null && this != instance)
            instance = this;
        else
            Destroy(this);

        tmp = GetComponentInChildren<TextMeshProUGUI>();
        anim = GetComponentInChildren<Animator>();
        localize = GetComponentInChildren<LocalizeUI>();
        GameEvents.current.onSceneTransitionComplete += sceneTransitionComplete;
        StopAllCoroutines();
        transform.GetChild(0).gameObject.SetActive(false);
    }

    private void sceneTransitionComplete()
    {
        showLocation(SaveManager.instance.getCurrentLocationName());
    }

    /// <summary>
    /// Shows the location popup for a text table key. Restarts cleanly if a popup is already showing.
    /// </summary>
    public void showLocation(string locationKey)
    {
        StopAllCoroutines();
        transform.GetChild(0).gameObject.SetActive(false);

        localize.fieldName = locationKey;
        localize.UpdateText();
        anim.SetTrigger("Reset");
        StartCoroutine(delayCoroutine());
    }

    public void beginAnim()
    {
        StopAllCoroutines();
        StartCoroutine(animCoroutine());
    }

    private IEnumerator animCoroutine()
    {
        yield return new WaitForSeconds(delayBeforeAnimTime);
        anim.SetTrigger("Begin");
        yield return new WaitForSeconds(5);
        //Debug.Log("turning this bitch off");
        transform.GetChild(0).gameObject.SetActive(false);
    }

    private IEnumerator delayCoroutine()
    {
        yield return new WaitForSeconds(delayBeforeTypeWriterTime);
        transform.GetChild(0).gameObject.SetActive(true);
    }
 
}

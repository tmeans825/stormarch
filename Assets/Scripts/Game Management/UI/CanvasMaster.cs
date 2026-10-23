using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Com.LuisPedroFonseca.ProCamera2D;
using TMPro;

public class CanvasMaster : MonoBehaviour
{
    [System.NonSerialized]
    public static CanvasMaster instance;

    public Image bloodVignette;

    public TextMeshProUGUI checkpointText;

    public GameObject letterBox;

    public GameObject cutToBlackObj;

    [System.NonSerialized]
    public bool isCutscenePlaying = false;

    private Animator letterAnimTop;
    private Animator letterAnimBottom;

    void Awake()
    {
        if (instance == null && this != instance)
            instance = this;
        else
            Destroy(gameObject);

        if(bloodVignette == null)
            bloodVignette = transform.GetChild(2).GetComponent<Image>();
        //DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        GameEvents.current.onReturnToTitle += returningToTitle;

        letterAnimTop = letterBox.transform.GetChild(0).GetComponent<Animator>();
        letterAnimBottom = letterBox.transform.GetChild(1).GetComponent<Animator>();
    }

    public void HandleCinematicStarted()
    {
        letterBox.SetActive(false);
        letterBox.SetActive(true);
        isCutscenePlaying = true;
    }

    public void shutOffHUD()
    {
        HUDUIBehavior.instance.hideHUB();
        HotbarUIBehavior.instance.gameObject.SetActive(false);
        MouseManager.instance.hideMouse();
    }

    public void HandleCinematicComplete()
    {
        letterAnimTop.Play("Letterbox_Top_Exit");
        letterAnimBottom.Play("Letterbox_Bottom_Exit");
        isCutscenePlaying = false;
    }

    public void reEnableHUD()
    {
        HotbarUIBehavior.instance.gameObject.SetActive(true);
        MouseManager.instance.showMouse();
    }

    public void cutToBlack()
    {
        StartCoroutine(cutToBlackWithTiming());
    }

    private IEnumerator cutToBlackWithTiming()
    {
        cutToBlackObj.SetActive(false); // just in case
        cutToBlackObj.SetActive(true);
        yield return new WaitForSeconds(5f);
        cutToBlackObj.SetActive(false); // just in case
    }

    private void returningToTitle()
    {
        GameEvents.current.onReturnToTitle -= returningToTitle;
        this.transform.parent = ObjectPooler.instance.transform;
    }

    public void showCheckpointText()
    {
        StartCoroutine(fadeTextOut(checkpointText, 3f));
    }

    private IEnumerator fadeTextOut(TextMeshProUGUI text, float timeToHold)
    {

        text.gameObject.SetActive(true);
        text.CrossFadeAlpha(1f, 0.01f, false);
        yield return new WaitForSeconds(timeToHold + 0.01f);
        text.CrossFadeAlpha(0f, 0.5f, false);
        yield return new WaitForSeconds(0.5f);
        text.gameObject.SetActive(false);
    }
}

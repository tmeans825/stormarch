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

    // Unscaled fade of the cut-to-black object, so it still runs while Time.timeScale is slowed or frozen.
    // Yield on this from a coroutine; fading to black leaves the object active and opaque until fadeFromBlack.
    public IEnumerator fadeBlack(bool toBlack, float duration)
    {
        CanvasGroup group = cutToBlackObj.GetComponent<CanvasGroup>();
        if (group == null)
            group = cutToBlackObj.AddComponent<CanvasGroup>();

        float start = toBlack ? 0f : 1f;
        float end = toBlack ? 1f : 0f;

        group.alpha = start;
        cutToBlackObj.SetActive(true);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(start, end, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        group.alpha = end;
        if (!toBlack)
            cutToBlackObj.SetActive(false);
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ProgressBarUIBehavior : MonoBehaviour
{
    public Sprite darkFill;
    public Sprite lightFill;
    public Sprite darkerFill;

    public float fadeSpeed = 0.5f;

    [System.NonSerialized]
    public static ProgressBarUIBehavior instance;

    private GameObject progressBar;
    private Image progressBarFill;
    private Image progressBarFrame;

    private void Awake()
    {
        if (instance == null && this != instance)
            instance = this;
        else
            Destroy(this);

        progressBar = transform.gameObject;
        progressBarFill = progressBar.transform.GetChild(0).GetComponent<Image>();
        progressBarFrame = progressBar.transform.GetChild(1).GetComponent<Image>();

        progressBarFill = TEMLib.ChangeAlpha(progressBarFill, 0.0f);
        progressBarFrame = TEMLib.ChangeAlpha(progressBarFrame, 0.0f);
    }

    public void startProgress()
    {
        StopAllCoroutines();
        progressBarFill.fillAmount = 0.0f;
        progressBarFill.CrossFadeAlpha(1f, 0, true);
        progressBarFrame.CrossFadeAlpha(1f, 0, true);
        progressBarFill.canvasRenderer.SetAlpha(1f);
        progressBarFill = TEMLib.ChangeAlpha(progressBarFill, 1.0f);
        progressBarFill.sprite = darkFill;
        progressBarFrame.canvasRenderer.SetAlpha(1f);
        progressBarFrame = TEMLib.ChangeAlpha(progressBarFrame, 1.0f);
    }

    public void updateProgress(float percentage)
    {
        if (percentage >= 1f)
        {
            StartCoroutine(done(lightFill));
            percentage = 1f;
        }
        progressBarFill.fillAmount = percentage;
    }

    public void interruptProgress()
    {
        StartCoroutine(done(darkerFill));
    }

    private IEnumerator done(Sprite fill)
    {
        progressBarFill.sprite = fill;
        yield return new WaitForSeconds(0.1f);
        progressBarFill.CrossFadeAlpha(0f, fadeSpeed, true);
        progressBarFrame.CrossFadeAlpha(0f, fadeSpeed, true);
    }
}

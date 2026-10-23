using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SlotAnimation : MonoBehaviour
{
    public float targetX = 37f;
    public float extendDuration = 0.5f;
    public float collapseDuration = 1f;
    public float fadeOutSpeed = 0.5f;
    public float fadeInSpeed = 0.25f;
    public float fadedOutAlpha = 0.5f;
    private bool isAnimating = false;
    private Vector3 targetVect;
    private Vector3 startingVect;
    private Image image;
    private Image slideImage;
    private Image outlineImage;
    private Image[] beanImages = new Image[3];
    private TextMeshProUGUI text;
    private Vector3 velocity = Vector3.zero;
    private RectTransform slidePos;

    private void Start()
    {
        slidePos = GetComponent<RectTransform>();
        startingVect = slidePos.anchoredPosition;
        targetVect = new Vector3(targetX, slidePos.anchoredPosition.y);
        image = gameObject.GetComponent<Image>();

        if (transform.childCount > 0)
        {
            slideImage = transform.GetChild(0).GetComponent<Image>();
            outlineImage = slideImage.gameObject.transform.GetChild(0).GetComponent<Image>();
            text = transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            beanImages[0] = transform.GetChild(2).GetChild(0).GetComponent<Image>();
            beanImages[1] = transform.GetChild(2).GetChild(1).GetComponent<Image>();
            beanImages[2] = transform.GetChild(2).GetChild(2).GetComponent<Image>();
        }
    }

    public void collapseAnimation(bool keepVisible)
    {
        StopAllCoroutines();
        float alpha = 0.0f;
        if (keepVisible)
            alpha = fadedOutAlpha;
        StartCoroutine(lerpPos(targetVect, collapseDuration));
        image.CrossFadeAlpha(alpha, fadeOutSpeed, false);
        if (slideImage != null)
        {
            slideImage.CrossFadeAlpha(alpha, fadeOutSpeed, false);
            outlineImage.CrossFadeAlpha(alpha, fadeOutSpeed, false);
            text.CrossFadeAlpha(alpha, fadeOutSpeed, false);
            beanImages[0].CrossFadeAlpha(alpha, fadeOutSpeed, false);
            beanImages[1].CrossFadeAlpha(alpha, fadeOutSpeed, false);
            beanImages[2].CrossFadeAlpha(alpha, fadeOutSpeed, false);
        }
    }

    public void extendAnimation()
    {
        StopAllCoroutines();
        isAnimating = true;
        StartCoroutine(lerpPos(startingVect, extendDuration));
        image.CrossFadeAlpha(1.0f, fadeInSpeed, false);
        if (slideImage != null)
        {
            slideImage.CrossFadeAlpha(1.0f, fadeInSpeed, false);
            outlineImage.CrossFadeAlpha(1.0f, fadeInSpeed, false);
            text.CrossFadeAlpha(1.0f, fadeInSpeed, false);
            beanImages[0].CrossFadeAlpha(1.0f, fadeInSpeed, false);
            beanImages[1].CrossFadeAlpha(1.0f, fadeInSpeed, false);
            beanImages[2].CrossFadeAlpha(1.0f, fadeInSpeed, false);
        }
    }

    private IEnumerator lerpPos(Vector3 targetPosition, float duration)
    {
        float time = 0;
        Vector3 startPosition = slidePos.anchoredPosition;

        while (time < duration)
        {
            float t = time / duration;
            t = Mathf.Sin(t * Mathf.PI * 0.5f);
            slidePos.anchoredPosition = Vector3.Lerp(startPosition, targetPosition, t);
            time += Time.deltaTime;
            yield return null;
        }

        slidePos.anchoredPosition = targetPosition;
    }
}

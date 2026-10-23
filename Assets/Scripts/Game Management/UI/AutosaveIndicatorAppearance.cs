using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AutosaveIndicatorAppearance : MonoBehaviour
{
    private Image image;

    void Awake()
    {
        image = GetComponent<Image>();
        Color startColor = image.color;
        startColor.a = 0.0f;
        image.color = startColor;
    }

    void OnEnable()
    {
        GameEvents.current.onAutosaveIndicatorRequested += displaySave;
    }

    void displaySave()
    {
        if(!this.gameObject.activeSelf) { return; }
        if(PlayerController.instance.isDead) { return; }

        StartCoroutine(Lifespan());
    }

    private IEnumerator Lifespan()
    {
        for (int cycle = 0; cycle < 1; cycle++)
        {
            // Fade in
            float time = 0.0f;
            while (time < 1.5f)
            {
                float t = TEMLib.getSmoothLerpT(time, 1.5f);
                Color currentColor = image.color;
                currentColor.a = Mathf.Lerp(0.0f, 1.0f, t);
                image.color = currentColor;
                time += Time.deltaTime;
                yield return null;
            }

            Color finalColor = image.color;
            finalColor.a = 1.0f;
            image.color = finalColor;

            // Fade out
            time = 0.0f;
            while (time < 1.5f)
            {
                float t = TEMLib.getSmoothLerpT(time, 1.5f);
                Color currentColor = image.color;
                currentColor.a = Mathf.Lerp(1.0f, 0.0f, t);
                image.color = currentColor;
                time += Time.deltaTime;
                yield return null;
            }

            finalColor = image.color;
            finalColor.a = 0.0f;
            image.color = finalColor;
        }
    }

    void OnDisable()
    {
        GameEvents.current.onAutosaveIndicatorRequested -= displaySave;
    }
}

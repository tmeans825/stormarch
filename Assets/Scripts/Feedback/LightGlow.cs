using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

public class LightGlow : MonoBehaviour
{
    public float maxIntensity = 1f;
    public float minIntensity = 0.5f;

    public float fadeTime = 1f;

    private bool isFadingOut = true;
    private Light2D light;

    // Start is called before the first frame update
    void Start()
    {
        light = GetComponent<Light2D>();

        StartCoroutine(changeBrightnessLerp(maxIntensity, minIntensity));
    }

    private IEnumerator changeBrightnessLerp( float startIntensity, float endIntensity)
    {
        float time = 0.0f;

        while (time < fadeTime)
        {
            float t = TEMLib.getSmoothLerpT(time, fadeTime);
            light.intensity = Mathf.Lerp(startIntensity, endIntensity, t);
            time += Time.deltaTime;

            yield return null;
        }

        startAlternateFade();
    }

    private void startAlternateFade()
    {
        isFadingOut = !isFadingOut;

        if(isFadingOut)
            StartCoroutine(changeBrightnessLerp(maxIntensity, minIntensity));
        else
            StartCoroutine(changeBrightnessLerp(minIntensity, maxIntensity));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

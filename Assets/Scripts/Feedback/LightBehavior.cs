using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

public class LightBehavior : MonoBehaviour
{
    public float targetIntensity = 0.0f;
    public float fadeInTime;
    public float holdTime = 0.0f;
    public float fadeOutTime;
    public bool destroyAfter = false;
    public bool manualTrigger = false;

    private Light2D light;
    private bool fadeIn = false;
    private bool hold = false;
    private bool fadeOut = false;
    private float timePassed = 0.0f;
    void Start()
    {
        light = transform.GetComponent<Light2D>();
        if(targetIntensity == 0.0f)
            targetIntensity = light.intensity;

        if (!manualTrigger)
        {
            trigger();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (fadeIn)
        {
            light.intensity += targetIntensity * (Time.deltaTime/fadeInTime);
            timePassed += Time.deltaTime;
            if (timePassed >= fadeInTime)
            {
                fadeIn = false;
                hold = true;
            }
        }

        if(hold)
        {
            if (holdTime > 0.0f)
            {
                hold = false;
                StartCoroutine(holdForSeconds());
            }
            else
            {
                hold = false;
                fadeOut = true;
            }
        }

        if (fadeOut && light.intensity > 0.0f)
        {
            light.intensity += -1f*targetIntensity * (Time.deltaTime / fadeOutTime);
            timePassed += Time.deltaTime;
            if (timePassed >= fadeOutTime || light.intensity < 0.0f)
            {
                fadeOut = false;
                light.intensity = 0.0f;
                if (destroyAfter)
                    Destroy(this.gameObject);
            }
        }
    }

    public void trigger()
    {
        StopAllCoroutines();
        fadeIn = false;
        fadeOut = false;
        hold = false;
        if (fadeInTime > 0f)
        {
            if(light != null)
                light.intensity = 0.0f;
            fadeIn = true;
        }
        if (holdTime > 0f)
            hold = true;
        else if (fadeOutTime > 0f)
            fadeOut = true;
    }

    private IEnumerator holdForSeconds()
    {
        yield return new WaitForSeconds(holdTime);
        fadeOut = true;
        timePassed = 0.0f;
    }

    // For manual triggering only
    public IEnumerator fadeInAndOut(float minLuminosity, float maxLuminosity, bool fadeIn, float duration)
    {
        float counter = 0f;

        //Set Values depending on if fadeIn or fadeOut
        float a, b;

        if (fadeIn)
        {
            a = minLuminosity;
            b = maxLuminosity;
        }
        else
        {
            a = maxLuminosity;
            b = minLuminosity;
        }

        float currentIntensity = light.intensity;

        while (counter < duration)
        {
            counter += Time.unscaledDeltaTime;

            light.intensity = Mathf.Lerp(a, b, counter / duration);

            yield return null;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using DigitalRuby.RainMaker;

public class LightWeatherManager : MonoBehaviour
{
    public RainScript2D rainScript;
    public GameObject globalLight;
    public Color lightningColor;
    public float lightningIntensity = 3f;
    public float testRainSpeed = 0.02f;
    public float lightningLength = 0.1f;
    public float lightningDelay = 0.1f;
    public GameObject amObj;

    private Light2D lightScript;
    private float originalIntensity;
    private Color originalColor;
    private bool isThundering = false;
    private bool hasInstantiated = false;
    private float originalGlobalLightIntensity;

    [System.NonSerialized]
    public static LightWeatherManager instance;

    private Coroutine activeRainCoroutine;
    private Coroutine activeLightCoroutine;

    // Rain hide/show is intensity-driven, not enabled-toggle: disabling RainScript2D only freezes
    // it (RainMaker stops particles + audio inside its own Update/CheckForRainChange). Going indoors
    // sets intensity to 0 so the plugin actually stops; we restore on exit.
    private bool rainHidden;
    private float rainIntensityBeforeHide;
    private float activeRainTarget; // the intensity the current rain/fade is heading toward

    void Awake()
    {
        if (instance == null && this != instance)
            instance = this;
        else
            Destroy(this);


        lightScript = globalLight.GetComponent<Light2D>();
        originalColor = lightScript.color;
        originalGlobalLightIntensity = lightScript.intensity;
        rainScript.RainIntensity = 0;
    }

    public void beginTempestWeather()
    {
        if (rainHidden)
        {
            // Suppress only the rain visual while indoors; it'll snap in on exit. Non-rain
            // tempest effects (lightning, dimmed light) still play.
            activeRainTarget = rainIntensityBeforeHide = 0.4f;
        }
        else
        {
            activeRainTarget = 0.4f;
            activeRainCoroutine = StartCoroutine(fadeInRain(0.4f, 5f, 10f));
        }
        triggerLightning();
        setGlobalLight(0.3f);
    }

    public void endTempestWeather()
    {
        stopRain();
        setGlobalLight(originalGlobalLightIntensity);
    }

    public void setCampLight()
    {
        setGlobalLight(0.2f, 0.1f);
        lightScript.color = new Color(0.85f, 0.6f, 0.4f);
    }

    public void startRain(float intensity)
    {
        setRainIntensity(intensity);
    }

    public void stopRain()
    {
        if (activeRainCoroutine != null)
        {
            StopCoroutine(activeRainCoroutine);
            activeRainCoroutine = null;
        }
        activeRainTarget = 0f;
        if (rainHidden)
        {
            // Storm ended while indoors — don't let exit restore old rain.
            rainIntensityBeforeHide = 0f;
            return;
        }
        setRainIntensity(0);
    }

    public void hideRain()
    {
        if (rainHidden) return;
        rainHidden = true;
        if (activeRainCoroutine != null)
        {
            StopCoroutine(activeRainCoroutine);
            activeRainCoroutine = null;
        }
        rainIntensityBeforeHide = activeRainTarget; // snap to intended target on exit
        rainScript.RainIntensity = 0f; // component stays enabled so RainMaker stops particles + audio
    }

    public void showRain()
    {
        if (!rainHidden) return;
        rainHidden = false;
        if (rainIntensityBeforeHide > 0f)
        {
            configureRain();
            rainScript.RainIntensity = rainIntensityBeforeHide;
        }
    }

    public void configureRain()
    {
        if (!hasInstantiated)
        {
            AudioMaster.instance.configureRain();
            hasInstantiated = true;
        }
    }

    public void setRainIntensity(float intensity)
    {
        activeRainTarget = intensity;
        if (rainHidden)
        {
            // Redirect writes to the stored target so nothing leaks visually while indoors.
            rainIntensityBeforeHide = intensity;
            return;
        }
        configureRain();
        rainScript.RainIntensity = intensity;
    }

    public IEnumerator fadeInRain(float targetIntensity, float delay, float duration)
    {
        activeRainTarget = targetIntensity;
        if (rainHidden)
        {
            rainIntensityBeforeHide = targetIntensity;
            activeRainCoroutine = null;
            yield break;
        }

        yield return new WaitForSeconds(delay);
        configureRain();
        float elapsedTime = 0f;
        float startingIntensity = rainScript.RainIntensity;

        while (elapsedTime < duration)
        {
            if (rainHidden)
            {
                rainIntensityBeforeHide = targetIntensity;
                activeRainCoroutine = null;
                yield break;
            }
            rainScript.RainIntensity = Mathf.Lerp(startingIntensity, targetIntensity, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        rainScript.RainIntensity = targetIntensity;
        activeRainCoroutine = null; // clear so the field doesn't go stale after natural completion
    }

    public void triggerLightning()
    {
        StartCoroutine(thunder());
    }

    public void setGlobalLight(float intensity, float fadeDuration = 5f)
    {
        if (activeLightCoroutine != null)
        {
            StopCoroutine(activeLightCoroutine);
            activeLightCoroutine = null;
        }

        if(intensity < lightScript.intensity)
            activeLightCoroutine = StartCoroutine(fadeOutGlobalLight(intensity, fadeDuration));
        else
            activeLightCoroutine = StartCoroutine(fadeInGlobalLight(intensity, fadeDuration));
     }

    public IEnumerator thunder()
    {
        if(!isThundering)
        {
            originalIntensity = lightScript.intensity;
            isThundering = true;
            lightScript.color = lightningColor;
            lightScript.intensity = lightningIntensity;
            yield return new WaitForSeconds(lightningLength);
            lightScript.color = originalColor;
            lightScript.intensity = originalIntensity;
            yield return new WaitForSeconds(lightningDelay);
            lightScript.color = lightningColor;
            lightScript.intensity = lightningIntensity;
            yield return new WaitForSeconds(lightningLength);
            lightScript.color = originalColor;
            lightScript.intensity = originalIntensity;
            AudioMaster.instance.PlaySound("Thunder" + Random.Range(1, 3));
            isThundering = false;
        }
    }

    public IEnumerator fadeOutGlobalLight(float newIntensity, float duration)
     {
         float elapsedTime = 0f;
         float startingIntensity = lightScript.intensity;

         while (elapsedTime < duration)
         {
             lightScript.intensity = Mathf.Lerp(startingIntensity, newIntensity, elapsedTime / duration);
             elapsedTime += Time.deltaTime;
             yield return null;
         }

         lightScript.intensity = newIntensity;
     }

     public IEnumerator fadeInGlobalLight(float targetIntensity, float duration)
     {
         float elapsedTime = 0f;
         float startingIntensity = lightScript.intensity;

         while (elapsedTime < duration)
         {
             lightScript.intensity = Mathf.Lerp(startingIntensity, targetIntensity, elapsedTime / duration);
             elapsedTime += Time.deltaTime;
             yield return null;
         }

         lightScript.intensity = targetIntensity;
     }
        
}



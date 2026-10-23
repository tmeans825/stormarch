using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Com.LuisPedroFonseca.ProCamera2D;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class InfestationUIBehavior : MonoBehaviour
{
    public Color darkFill;
    public Color lightFill;
    public string tensionSoundName;
    private float fadeSpeed = 0.5f;
    private GameObject progressBar;
    private Image progressBarFill;
    private Image progressBarFrame;
    private Image bugIcon;

    // tension effect settings
    private float zoomAmount = 5f;
    private float maxVignetteIntensity = 0.2f;
    private float maxChromaticIntensity = 0.4f;
    private float baseRebaseLambda = 10f; // how fast baseScreenSize chases a location change mid-fill
    private bool tensionActive;           // true while a fill owns the camera (fillAmount > 0)

    // tension effect cached references
    private float baseScreenSize;
    private float baseVignetteIntensity;
    private float baseChromaticIntensity;
    private Vignette vignette;
    private ChromaticAberration chromAberration;
    private Sound tensionSound;
    private AudioManager audioManager;
    private ProCamera2D proCamera;

    // Start is called before the first frame update
    void Start()
    {
        progressBar = transform.gameObject;
        progressBarFill = progressBar.transform.GetChild(0).GetComponent<Image>();
        progressBarFrame = progressBar.transform.GetChild(1).GetComponent<Image>();
        bugIcon = progressBar.transform.GetChild(2).GetComponent<Image>();
        audioManager = GetComponent<AudioManager>();

        progressBarFill = TEMLib.ChangeAlpha(progressBarFill, 0.0f);
        progressBarFrame = TEMLib.ChangeAlpha(progressBarFrame, 0.0f);
        bugIcon = TEMLib.ChangeAlpha(bugIcon, 0.0f);

        PlayerController.instance.GetComponent<InfestationHandler>().onInfestationMeterChange += updateInfestationBar;

        progressBarFill.fillAmount = 0f;

        // cache tension effect baselines (camera base is captured per-fill in startNewBarFill,
        // since the indoor/outdoor zoom level isn't known until tension actually begins)
        proCamera = ProCamera2D.Instance;

        VolumeProfile postProcess = proCamera.GetComponent<Volume>().profile;
        postProcess.TryGet<Vignette>(out vignette);
        postProcess.TryGet<ChromaticAberration>(out chromAberration);
        baseVignetteIntensity = (float)vignette.intensity;
        baseChromaticIntensity = (float)chromAberration.intensity;

        if (!string.IsNullOrEmpty(tensionSoundName))
            tensionSound = audioManager.getSound(tensionSoundName);
    }

    void updateInfestationBar(float value)
    {
        value = value / 100f; // make a percentage

        if(progressBarFill.fillAmount < value) // bar is increasing
        {
            if(progressBarFill.fillAmount == 0f)
            {
                startNewBarFill();
            }

            progressBarFill.color = lightFill;
        }
        else // bar is same or decreasing
        {
            progressBarFill.color = darkFill;
        }

        progressBarFill.fillAmount = value;

        applyTensionEffects(value);

        if(progressBarFill.fillAmount == 0f || progressBarFill.fillAmount == 1f)
        {
            fadeOutBar();

            if(progressBarFill.fillAmount == 1f) // only remove effects if bar is empty
                return;
            
            if (tensionSound != null)
                tensionSound.source.Stop();

            resetTensionEffects();
        }
    }

    void applyTensionEffects(float t)
    {
        // Track the base toward the current location's base size so crossing the indoor/outdoor
        // threshold mid-fill eases instead of stepping (frame-rate independent). No-op when the
        // location is unchanged, since contextualBase already equals baseScreenSize.
        if (EventManager.instance != null)
        {
            float contextualBase = EventManager.instance.getBaseCamSize();
            baseScreenSize = Mathf.Lerp(baseScreenSize, contextualBase, 1f - Mathf.Exp(-baseRebaseLambda * Time.deltaTime));
        }

        proCamera.UpdateScreenSize(Mathf.Lerp(baseScreenSize, baseScreenSize - zoomAmount, t));
        vignette.intensity.Override(Mathf.Lerp(baseVignetteIntensity, maxVignetteIntensity, t));
        chromAberration.intensity.Override(Mathf.Lerp(baseChromaticIntensity, maxChromaticIntensity, t));

        if (tensionSound != null)
        {
            tensionSound.source.volume = Mathf.Lerp(0f, 2f, t);
        }
    }

    void startNewBarFill()
    {
        // tension begins: take ownership of the camera zoom from the indoor trigger (so the two don't
        // fight) and capture the base size for the player's current location (indoor or outdoor).
        tensionActive = true;

        if (EventManager.instance != null)
        {
            EventManager.instance.pauseIndoorZoom();
            baseScreenSize = EventManager.instance.getBaseCamSize();
        }
        else
        {
            baseScreenSize = proCamera.ScreenSizeInWorldCoordinates.y * 0.5f; // fallback: live size
        }

        progressBarFill.fillAmount = 0.0f;
        progressBarFill.CrossFadeAlpha(1f, 0, true);
        progressBarFrame.CrossFadeAlpha(1f, 0, true);
        bugIcon.CrossFadeAlpha(0.66f, 0, true);
        progressBarFill.canvasRenderer.SetAlpha(1f);
        progressBarFill = TEMLib.ChangeAlpha(progressBarFill, 1.0f);
        progressBarFrame.canvasRenderer.SetAlpha(1f);
        progressBarFrame = TEMLib.ChangeAlpha(progressBarFrame, 1.0f);
        bugIcon.canvasRenderer.SetAlpha(1f);
        bugIcon = TEMLib.ChangeAlpha(bugIcon, 0.66f);

        if (tensionSound != null)
        {
            tensionSound.source.volume = 0f;
            tensionSound.source.Play();
        }
    }

    void fadeOutBar()
    {
        progressBarFill.CrossFadeAlpha(0f, fadeSpeed, true);
        progressBarFrame.CrossFadeAlpha(0f, fadeSpeed, true);
        bugIcon.CrossFadeAlpha(0f, fadeSpeed, true);
    }

    void resetTensionEffects()
    {
        if(proCamera == null)
            return;

        // No instant UpdateScreenSize here: the meter drains gradually, so applyTensionEffects has
        // already eased the camera back to the base size by the time we reach 0. Resuming lets the
        // indoor zoom trigger glide from the current size (an instant write would risk a pop).
        vignette.intensity.Override(baseVignetteIntensity);
        chromAberration.intensity.Override(baseChromaticIntensity);

        if (EventManager.instance != null)
            EventManager.instance.resumeIndoorZoom();

        tensionActive = false;
    }

    void OnDestroy()
    {
        if(PlayerController.instance != null)
        {
            PlayerController.instance.GetComponent<InfestationHandler>().onInfestationMeterChange -= updateInfestationBar;
        }

        bool wasActive = tensionActive; // capture before resetTensionEffects() clears it

        if (vignette != null)
            resetTensionEffects();

        // Authoritative restore on abrupt teardown (not reached via the gradual drain). Only when a fill
        // actually owned the camera — otherwise baseScreenSize is its default and we'd zero the size.
        // Instant is fine since the object is going away; covers the outdoors case where no resumed
        // trigger would correct a left-over tension zoom.
        if (wasActive && proCamera != null)
            proCamera.UpdateScreenSize(EventManager.instance != null ? EventManager.instance.getBaseCamSize() : baseScreenSize);

        if (tensionSound != null && tensionSound.source.isPlaying)
            tensionSound.source.Stop();
    }
}

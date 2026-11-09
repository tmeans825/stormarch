using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PixelCrushers;
using TMPro;

// Reusable on-screen banner + countdown for timed missions. Lives on the Canvas prefab, so any
// scene can call MissionCountdown.instance.startCountdown(textTableKey, seconds, onExpired).
// Saves and scene changes are blocked while it runs.
public class MissionCountdown : MonoBehaviour
{
    [System.NonSerialized]
    public static MissionCountdown instance;

    public TextMeshProUGUI bannerText;
    public LocalizeUI bannerLocalize;
    public TextMeshProUGUI timerText;

    public float bannerHoldTime = 2.5f;
    public float bannerFadeTime = 0.5f;
    public float urgentThreshold = 10f;
    public Color urgentColor = Color.red;

    public static bool isRunning
    {
        get { return instance != null && instance.running; }
    }

    private bool running = false;
    private float secondsRemaining;
    private int displayedSeconds = -1;
    private Action expiredCallback;
    private Color defaultTimerColor;
    private Coroutine bannerRoutine;
    private GameEvents subscribedEvents;
    private string activeTensionSong;
    private string previousAmbientSong;

    void Awake()
    {
        if (instance == null && this != instance)
            instance = this;
        else
            Destroy(this);

        defaultTimerColor = timerText.color;
    }

    void OnEnable()
    {
        trySubscribe();
    }

    void Start()
    {
        trySubscribe(); // GameEvents may not have been awake yet in OnEnable
    }

    void OnDisable()
    {
        if (subscribedEvents != null)
        {
            subscribedEvents.onPlayerDeath -= endCountdown;
            subscribedEvents.onReturnToTitle -= endCountdown;
            subscribedEvents = null;
        }

        // State only: the UI children may already be destroyed during scene teardown.
        running = false;
        expiredCallback = null;
    }

    void Update()
    {
        if (!running)
            return;

        secondsRemaining -= Time.deltaTime;

        if (secondsRemaining <= 0f)
        {
            // Clear state before invoking so the callback can only ever fire once.
            Action callback = expiredCallback;
            endCountdown();
            callback?.Invoke();
            return;
        }

        updateTimerDisplay();
    }

    // tensionSong must be in this scene's AudioMaster list. It replaces the ambient song until the
    // countdown is stopped. Give it a priority below combat music if fights shouldn't interrupt it.
    public void startCountdown(string bannerKey, float seconds, Action onExpired, string tensionSong = null)
    {
        if (running)
        {
            Debug.LogWarning("A mission countdown is already running, ignoring new one.");
            return;
        }

        if (!string.IsNullOrEmpty(tensionSong))
        {
            activeTensionSong = tensionSong;
            previousAmbientSong = AudioMaster.instance.sceneAmbientSongName;
            AudioMaster.instance.setSceneAmbient(tensionSong);
            AudioMaster.instance.playAmbient(); // setSceneAmbient alone waits out any combat music
        }

        secondsRemaining = seconds;
        expiredCallback = onExpired;
        running = true;

        displayedSeconds = -1;
        timerText.color = defaultTimerColor;
        timerText.gameObject.SetActive(true);
        updateTimerDisplay();

        if (bannerRoutine != null)
            StopCoroutine(bannerRoutine);
        bannerRoutine = StartCoroutine(showBanner(bannerKey));

        AudioMaster.instance.PlaySound("Objective Updated");
    }

    // For when the mission ends with the scene carrying on (e.g. the player got out).
    public void stopCountdown()
    {
        string tensionSong = activeTensionSong;
        string ambientToRestore = previousAmbientSong;

        endCountdown();

        if (!string.IsNullOrEmpty(tensionSong))
        {
            AudioMaster.instance.setSceneAmbient(ambientToRestore);
            AudioMaster.instance.stopMusicAndResumeAmbientIfActive(tensionSong);
        }
    }

    // Leaves the music alone: on death or a return to title, the scene reload resets it.
    private void endCountdown()
    {
        running = false;
        expiredCallback = null;
        activeTensionSong = null;
        previousAmbientSong = null;

        if (bannerRoutine != null)
        {
            StopCoroutine(bannerRoutine);
            bannerRoutine = null;
        }

        bannerText.gameObject.SetActive(false);
        timerText.gameObject.SetActive(false);
    }

    private void trySubscribe()
    {
        GameEvents ge = GameEvents.current;
        if (ge == null || subscribedEvents != null)
            return;

        subscribedEvents = ge;
        ge.onPlayerDeath += endCountdown;
        ge.onReturnToTitle += endCountdown;
    }

    private void updateTimerDisplay()
    {
        int seconds = Mathf.CeilToInt(secondsRemaining);
        if (seconds == displayedSeconds)
            return;

        displayedSeconds = seconds;
        timerText.text = string.Format("{0}:{1:00}", seconds / 60, seconds % 60);

        if (seconds <= urgentThreshold)
            timerText.color = urgentColor;
    }

    private IEnumerator showBanner(string bannerKey)
    {
        bannerText.text = bannerKey; // LocalizeUI's first update waits for its Start, a frame late
        bannerLocalize.fieldName = bannerKey;
        bannerText.gameObject.SetActive(true);
        bannerLocalize.UpdateText();
        bannerText.CrossFadeAlpha(1f, 0.01f, false);

        yield return new WaitForSeconds(bannerHoldTime);

        bannerText.CrossFadeAlpha(0f, bannerFadeTime, false);

        yield return new WaitForSeconds(bannerFadeTime);

        bannerText.gameObject.SetActive(false);
        bannerRoutine = null;
    }
}

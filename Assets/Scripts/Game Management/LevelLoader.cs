using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;


public class LevelLoader : MonoBehaviour
{
    public Animator transition;
    
    private AudioMixer audioMixer;

    public float transitionTime = 1.0f;
    public float fadeOutTime = 0.5f;

    [System.NonSerialized]
    public static LevelLoader instance;

    void Awake()
    {
        if (instance == null && this != instance)
            instance = this;
        else
            Destroy(this);
    }

    public void Start()
    {
        StartCoroutine(sceneReady());
    }

    private IEnumerator sceneReady()
    {
        audioMixer = SettingsMenu.instance.audioMixer;
        string sceneName = SceneManager.GetActiveScene().name;

        fadeVolumeIn();

        yield return 0;

        if (sceneName != "TitleScreen") // load in scene data
        {
            yield return null;
            SaveManager.instance.loadSceneData(sceneName);
            // Lock in this scene's first-entry position for death-respawn fallback.
            // The helper is idempotent — only writes if the key isn't already true.
            SaveManager.instance.recordFirstEntryPositionForSceneIfMissing(
                PlayerController.instance.transform.position);
        }
        else
        {
            SaveManager.instance.hasLoaded = false;
            SaveManager.instance.clearPendingArrival();
        }

        GameEvents.current.sceneTransitionComplete();
        //Debug.Log("Scene transition complete");
    }

    public void changeScene(string locationName)
    {
        string sceneName = SaveManager.instance.getCurrentActFilenameFromLocation(locationName);
        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SaveManager.instance.saveSceneDataWithoutAutosaveIndicator();
            // Make sure the background flush finishes before scene unload starts —
            // otherwise the write can be orphaned by SceneManager.LoadSceneAsync.
            // The 1s fade in KickoffLoad usually means this wait is a no-op.
            SaveManager.instance.WaitForActiveSave();
            SaveManager.instance.setPendingArrival(locationName); // after the save, so it can't be captured
            loadLevel(sceneName);
        }
        else
            Debug.LogWarning("Scene does not exist with name " + locationName);
    }

    public void loadLevel(string levelName)
    {
        loadLevel(levelName, null);
    }

    // onScreenCovered runs after the fade-to-black completes and BEFORE LoadSceneAsync
    // begins. Use this for work that visually shouldn't be seen (e.g. death-respawn
    // teleporting the player + saving) so the operation is hidden behind the fade.
    public void loadLevel(string levelName, System.Action onScreenCovered)
    {
        fadeVolumeOut();
        StartCoroutine(KickoffLoad(levelName, onScreenCovered));
    }

    IEnumerator KickoffLoad(string levelName, System.Action onScreenCovered)
    {
        transition.SetTrigger("Start");

        yield return new WaitForSeconds(transitionTime);

        onScreenCovered?.Invoke();

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(levelName);

        // Wait until the asynchronous scene fully loads
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        StartCoroutine(sceneReady());

        transition.SetTrigger("Loaded");
    }
    public void fadeVolumeOut()
    {
        StartCoroutine(StartFade(audioMixer, "volume", fadeOutTime, 0f));
    }

    public void fadeVolumeIn()
    {
        audioMixer.SetFloat("volume", Mathf.Log10(0.0001f) * 20);
        //Debug.Log("Target Vol = " + SettingsMenu.instance.getVolume());
        StartCoroutine(StartFade(audioMixer, "volume", fadeOutTime, SettingsMenu.instance.getVolume()));
    }

    private IEnumerator StartFade(AudioMixer audioMixer, string exposedParam, float duration, float targetVolume)
    {
        float currentTime = 0;
        float currentVol;
        audioMixer.GetFloat(exposedParam, out currentVol);
        currentVol = Mathf.Pow(10, currentVol / 20);
        float targetValue = Mathf.Clamp(targetVolume, 0.0001f, 1);
        float newVol = 0f;
        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            newVol = Mathf.Lerp(currentVol, targetValue, currentTime / duration);
            audioMixer.SetFloat(exposedParam, Mathf.Log10(newVol) * 20);
            //Debug.Log("AudioMixer audio = " + newVol);
            yield return null;
        }
        //Debug.Log("AudioMixer audio = " + newVol);
        yield break;
    }
}

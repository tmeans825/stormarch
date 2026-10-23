using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Com.LuisPedroFonseca.ProCamera2D;
using UnityEngine.Tilemaps;
using UnityEngine.Experimental.Rendering.Universal;
using DigitalRuby.RainMaker;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using PixelCrushers.DialogueSystem;


/* 
 * DO I NEED TO COMMUNICATE BETWEEN MULTIPLE GAME OBJECTS IN A DYNAMIC WAY? IF YES, USE GAME EVENTS INSTEAD.
 * THIS SCRIPT IS USED FOR TRIGGERING GLOBAL EVENTS THAT DO NOT NEED TO BE SUBSCRIBED TO BY OTHER GAME OBJECTS.
 */

public class EventManager : MonoBehaviour
{
    [System.NonSerialized]
    public static EventManager instance;

    public GameObject amObj;
    public GameObject indoorZoomObj;
    public Tilemap roofMap1;
    public Tilemap roofMap2;
    public TextMeshProTypewriterEffect typewriter;
    public StandardDialogueUI dialogueUI;
    public float fadeDuration = 0.5f; // Duration of the fade effect in seconds


    private Coroutine currentFadeCoroutine; // Current coroutine that's fading the Tilemap

    private bool hitFrozen = false;
    private Coroutine hitFreezeEnum;

    private enum DialogueInputState { Typing, Ready }
    private DialogueInputState m_dialogueInputState = DialogueInputState.Typing;
    private int m_readyOnFrame = -1;

    private VolumeProfile mainPostProcess;

    [ES3Serializable]
    private bool isTempest = false;

    private bool isCamping = false;
    private bool isIndoors = false;

    private ProCamera2DTriggerZoom indoorZoomTrigger; // resolved from indoorZoomObj in Start()

    const string MEDIUM_SHAKE = "Medium Shake";
    const string SMALL_SHAKE = "Small Shake";
    const string TINY_SHAKE = "Tiny Shake";

    private void Awake()
    {
        if (instance == null && this != instance)
            instance = this;
        else
            Destroy(this);
    }

    // Start is called before the first frame update
    void Start()
    {
        mainPostProcess = ProCamera2D.Instance.GetComponent<Volume>().profile;

        if (indoorZoomObj != null)
            indoorZoomTrigger = indoorZoomObj.GetComponent<ProCamera2DTriggerZoom>();

        typewriter.onBegin.AddListener(onTypewriterBegin);
        typewriter.onEnd.AddListener(onTypewriterEnd);

        GameEvents.current.onPreviousSceneDataLoaded += loadInEvents;
    }

    // Update is called once per frame
    void Update()
    {
        // Typing: keep button hidden each frame (overrides Dialogue System's ShowContinueButton)
        if (m_dialogueInputState == DialogueInputState.Typing)
        {
            DialogueIndicatorManager.instance.continueButton.SetActive(false);
        }
        // Ready: show button starting 1 frame after typewriter ended (only if no player choices)
        else if (m_dialogueInputState == DialogueInputState.Ready && Time.frameCount > m_readyOnFrame)
        {
            var state = DialogueManager.currentConversationState;
            bool hasPlayerChoices = state != null && state.hasPCResponses;

            if (!hasPlayerChoices)
            {
                DialogueIndicatorManager.instance.continueButton.SetActive(true);
            }

            // First frame button is visible + chained NPC → deselect so space can't fire it via EventSystem
            if (Time.frameCount == m_readyOnFrame + 1)
            {
                if (state != null && state.hasNPCResponse && !state.hasPCResponses
                    && EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
                {
                    EventSystem.current.SetSelectedGameObject(null);
                }
            }
        }
    }
    

    // ----------------- Public Events
    public void startTempest()
    {
        isTempest = true;
    }

    public void endTempest()
    {
        isTempest = false;
    }

    public bool isTempestActive()
    {
        return isTempest;
    }

    public void startCamping()
    {
        isCamping = true;
        GameEvents.current.campingBegin();
        StartCoroutine(fadeAndEndInfestation(true));
    }

    public void endCamping()
    {
        isCamping = false;
        SaveManager.instance.campSave();
    }

    public bool isCampingNow()
    {
        return isCamping;
    }

    public void startLoopingPath(NonCombatChar character, Vector3 target, float loopTime)
    {       
        StartCoroutine(loopPathAndRandomizeAppearance(character, loopTime, character.transform.position, target));
    }

    public void shakeScreenMedium()
    {
        ProCamera2DShake.Instance.Shake(MEDIUM_SHAKE);
    }

    public void shakeScreenSmall()
    {
        ProCamera2DShake.Instance.Shake(SMALL_SHAKE);
    }

    public void shakeScreenTiny()
    {
        ProCamera2DShake.Instance.Shake(TINY_SHAKE);
    }

    public void enteredIndoors()
    {
        if (isIndoors) return; // idempotent — defense-in-depth against duplicate transition calls
        isIndoors = true;

        indoorZoomObj.transform.parent = PlayerController.instance.transform;
        indoorZoomObj.transform.localPosition = Vector3.zero;
        FadeOut();

        LightWeatherManager.instance.hideRain();
    }

    public void exitedIndoors()
    {
        if (!isIndoors) return;
        isIndoors = false;

        indoorZoomObj.transform.parent = this.transform;
        indoorZoomObj.transform.localPosition = Vector3.zero;
        FadeIn();

        LightWeatherManager.instance.showRain();
    }

    // --- Indoor-zoom coordination for systems that temporarily own the camera zoom (e.g. infestation
    // tension). While such a system is active it pauses this trigger so the two don't fight over
    // ProCamera2D's screen size, then resumes it. See ProCamera2DTriggerZoom.pauseZoom/resumeZoom.
    public bool isPlayerIndoors()
    {
        return isIndoors;
    }

    public void pauseIndoorZoom()
    {
        if (indoorZoomTrigger != null)
            indoorZoomTrigger.pauseZoom();
    }

    public void resumeIndoorZoom()
    {
        if (indoorZoomTrigger != null)
            indoorZoomTrigger.resumeZoom();
    }

    // The base (un-tensioned) half-height screen size for the player's current location: the settled
    // indoor zoom when indoors, the outdoor size otherwise.
    public float getBaseCamSize()
    {
        if (indoorZoomTrigger != null)
            return isIndoors ? indoorZoomTrigger.ZoomedInCamSize : indoorZoomTrigger.StartCamSize;

        return ProCamera2D.Instance.ScreenSizeInWorldCoordinates.y * 0.5f; // fallback if unassigned
    }

    public void hitFreeze()
    {
        if (hitFrozen)
            return;

       hitFreezeEnum = StartCoroutine(freezeFrame());
    }

    public void playerDeath()
    {
        if (hitFrozen)
            StopAllCoroutines();

        if(hitFreezeEnum != null)
            StopCoroutine(hitFreezeEnum);

        hitFrozen = false;

        StartCoroutine(slowdownForPlayerDeath());
    }

    private void onTypewriterBegin()
    {
        m_dialogueInputState = DialogueInputState.Typing;
        DialogueIndicatorManager.instance.continueButton.SetActive(false);
    }

    private void onTypewriterEnd()
    {
        m_dialogueInputState = DialogueInputState.Ready;
        m_readyOnFrame = Time.frameCount;
        // Button shown by Update() next frame — NOT here
    }

    public void skipTypewriterEffect()
    {
        // TYPING: stop typewriter only — don't advance, don't pass to events
        if (m_dialogueInputState == DialogueInputState.Typing)
        {
            if (typewriter.isPlaying)
            {
                typewriter.Stop();
                DialogueIndicatorManager.instance.hideSpacebarHelptext();
            }
            return;
        }

        // READY: typewriter is done
        // Guard: if typewriter is already playing, a new line started via button click — don't interfere
        if (typewriter.isPlaying) return;

        // Chained NPC guard: must use continue button (arrow keys → select → space/click)
        var state = DialogueManager.currentConversationState;
        if (state != null && state.hasNPCResponse && !state.hasPCResponses)
            return;

        // Non-chained: advance dialogue
        dialogueUI.OnContinue();
    }

    public void otherButtonPressedDuringDialogue()
    {
        if (typewriter.isPlaying)
        {
            DialogueIndicatorManager.instance.displaySpacebarHelptext();
        }
    }

    public void startRain(float intensity)
    {
        LightWeatherManager.instance.startRain(intensity);
    }

    public void updateRainIntensity(float intensity)
    {
        LightWeatherManager.instance.setRainIntensity(intensity);
    }

    public void stopRain()
    {
        LightWeatherManager.instance.stopRain();
    }

    public void thunder()
    {
        LightWeatherManager.instance.triggerLightning();
    }

    public IEnumerator fadeAndEndInfestation(bool goToCamp = false)
    {
        CanvasMaster.instance.cutToBlack();
        yield return new WaitForSeconds(0.6f); // wait for fade
        // Guarded: camping is the common caller and usually runs with no tempest active,
        // so an unconditional fire would push end-of-tempest state onto listeners that
        // never saw a beginning.
        GameEvents.current.tryEndInfestation();

        if(goToCamp)
        {
            // Camp settles the weather itself rather than relying on the tempest-end path,
            // which no longer runs when there was no tempest to end.
            LightWeatherManager.instance.stopRain();
            LightWeatherManager.instance.setCampLight();
            AudioMaster.instance.PlayMusic("Camping Crickets", 2.0f);
        }
    }


    // ----------- Private Helper Functions
    private IEnumerator loopPathAndRandomizeAppearance(NonCombatChar character, float secondsToWait, Vector3 startPos, Vector3 targetPos)
    {
        character.startMoving(targetPos);
        yield return new WaitForSeconds(secondsToWait);
        character.overrideStopMoving();
        character.transform.position = startPos;
        character.randomizeAppearanceToTemplate();
        yield return null;
        StartCoroutine(loopPathAndRandomizeAppearance(character, secondsToWait, startPos, targetPos));
    }

    private IEnumerator slowdownForPlayerDeath()
    {
        StartCoroutine(deathPostProcessTransition());

        Time.timeScale = 0.5f;

        float time = 0.0f;
        while (time < 0.1f)
        {
            float t = TEMLib.getSmoothLerpT(time, 0.1f);
            Time.timeScale = Mathf.Lerp(1f, 0.5f, t);
            time += Time.unscaledDeltaTime;
            yield return null;
        }

        time = 0.0f;
        while (time < 1.5f)
        {
            float t = TEMLib.getSmoothLerpT(time, 1.5f);
            Time.timeScale = Mathf.Lerp(0.5f, 1f, t);
            time += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private IEnumerator deathPostProcessTransition()
    {
        Vignette vignette;
        mainPostProcess.TryGet<Vignette>(out vignette);
        float startVignetteIntensity = ((float)vignette.intensity);

        ChromaticAberration chromAberration;
        mainPostProcess.TryGet<ChromaticAberration>(out chromAberration);
        float startChromaticIntensity = ((float)chromAberration.intensity);

        float time = 0.0f;
        while (time < 1f)
        {
            float t = TEMLib.getSmoothLerpT(time, 1f);          
            chromAberration.intensity.Override(Mathf.Lerp(startChromaticIntensity, 1f, t));
            time += Time.unscaledDeltaTime;
            yield return null;
        }

        time = 0.0f;
        while (time < 3f)
        {
            float t = TEMLib.getSmoothLerpT(time, 3f);
            vignette.intensity.Override(Mathf.Lerp(startVignetteIntensity, 1f, t));
            time += Time.unscaledDeltaTime;
            yield return null;
        }

        // KICK OFF SCENE TRANSITION!
        string sceneName = SceneManager.GetActiveScene().name;

        // Defer the respawn teleport + save until LevelLoader's fade-to-black has
        // fully covered the screen — otherwise the player sees the dead body warp
        // to the camp position before the scene reloads. The callback fires AFTER
        // the fade and BEFORE LoadSceneAsync begins, so the save still lands before
        // the new scene starts loading.
        LevelLoader.instance.loadLevel(sceneName, () =>
        {
            // Dark-Souls respawn: place the player at last camp / first-entry position
            // BEFORE saving, then revive their live-object state, then save. The save
            // file represents an alive player at the respawn point — the upcoming
            // scene reload reads this directly, no special death-respawn branch in load.
            Vector3? respawnPos = SaveManager.instance.getRespawnPositionForScene(sceneName);
            if (respawnPos.HasValue)
                PlayerController.instance.transform.position = respawnPos.Value;

            // Reverses dead-only state set by Character/CombatChar death pipeline so
            // the save doesn't persist destroyed colliders, isDead=true, etc. See
            // PlayerController.prepareForRespawnSave for the full reset list.
            PlayerController.instance.prepareForRespawnSave();
            SaveManager.respawnRestedWorldStateInCurrentScene();

            // Drop components at the fatal-hit location captured back in
            // PlayerController.triggerDeath. Must run BEFORE the save below so the
            // new drop fields and the zeroed componentCount land in the slot file
            // together. Always overwrites any prior drop, even cross-scene.
            PlayerController.instance.dropComponentsOnDeath(sceneName);

            // Respawning at camp resets the infestation, same as resting at camp.
            // Guarded because infestationEnd isn't idempotent - it stops music and
            // restores weather whether or not a tempest was ever running.
            GameEvents.current.tryEndInfestation();

            SaveManager.instance.saveSceneDataWithoutAutosaveIndicator();
            SaveManager.instance.WaitForActiveSave();
        });
    }

    private IEnumerator freezeFrame()
    {
        hitFrozen = true;
        Time.timeScale = 0.001f;
        yield return new WaitForSecondsRealtime(0.03f);

        if (!hitFrozen)
            yield break;

        Time.timeScale = 1f;
        
        yield return new WaitForSeconds(0.33f); // don't allow hit freezes to spam
        hitFrozen = false;
    }

    // Function to fade out the Tilemap
    private void FadeOut()
    {
        if (currentFadeCoroutine != null)
        {
            StopCoroutine(currentFadeCoroutine);
        }

        currentFadeCoroutine = StartCoroutine(FadeOutCoroutine());
    }

    private IEnumerator FadeOutCoroutine()
    {
        Color color = roofMap1.color;
        float alpha = color.a;

        // Gradually reduce the alpha value of the Tilemap's color over the duration of the fade effect
        for (float t = 0.0f; t < 1.0f; t += Time.deltaTime / fadeDuration)
        {
            color.a = Mathf.Lerp(alpha, 0.0f, t);
            roofMap1.color = color;
            roofMap2.color = color;
            yield return null;
        }

        // Set the alpha value of the Tilemap's color to 0 once the fade effect is complete
        color.a = 0.0f;
        roofMap1.color = color;
        roofMap2.color = color;
    }

    // Function to fade in the Tilemap
    private void FadeIn()
    {
        if (currentFadeCoroutine != null)
        {
            StopCoroutine(currentFadeCoroutine);
        }

        currentFadeCoroutine = StartCoroutine(FadeInCoroutine());
    }

    private IEnumerator FadeInCoroutine()
    {
        Color color = roofMap1.color;
        float alpha = color.a;

        // Gradually increase the alpha value of the Tilemap's color over the duration of the fade effect
        for (float t = 0.0f; t < 1.0f; t += Time.deltaTime / fadeDuration)
        {
            color.a = Mathf.Lerp(alpha, 1.0f, t);
            roofMap1.color = color;
            roofMap2.color = color;
            yield return null;
        }

        // Set the alpha value of the Tilemap's color to 1 once the fade effect is complete
        color.a = 1.0f;
        roofMap1.color = color;
        roofMap2.color = color;
    }
    
    private void loadInEvents()
    {
        if(isTempest)
        {
            GameEvents.current.infestationBegin();
        }
    }

}

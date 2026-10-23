using System.Collections;
using System.Collections.Generic;
using PixelCrushers.DialogueSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Com.LuisPedroFonseca.ProCamera2D;
using System.Linq;

public enum SubtitleFontMode
{
    Default,
    Note,
}

[System.Serializable]
public class SubtitleFontStyle
{
    public SubtitleFontMode mode = SubtitleFontMode.Note;
    public TMP_FontAsset font;
    public Material fontSharedMaterial;
    public float fontSize; // 0 => inherit default
}

public class DialogueIndicatorManager : MonoBehaviour
{
    public Sprite[] factionArray;
    public Image factionImage;
    public ProCamera2DCinematics dialogueCinematics;
    public ProCamera2DTriggerZoom indoorZoomObj;
    public Image headSprite;
    public Image hairSprite;
    public Image chestSprite;
    public Image legsSprite;
    public GameObject characterPortrait;

    [Header("Hologram Portrait")]
    [SerializeField] private Color hologramPortraitTint = new Color(0.55f, 0.8f, 1f, 0.75f);
    [SerializeField] private float hologramPulseMinAlpha = 0.45f;
    [SerializeField] private float hologramPulseMaxAlpha = 0.85f;
    [SerializeField] private float hologramPulseSpeed = 1.2f;
    private Coroutine hologramPulseCoroutine;

    public GameObject continueButton;

    public GameObject spaceBarHelptext;

    [Header("Subtitle Font Styles")]
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private SubtitleFontStyle[] subtitleFontStyles;
    private SubtitleFontStyle defaultStyle;

    private Entity lastSpeaker;

    [System.NonSerialized]
    public static DialogueIndicatorManager instance;

    private void Awake()
    {
        if (instance == null && this != instance)
            instance = this;
        else
        {
            //UnityEngine.Debug.Log("Destroying playerController duplicate");
            Destroy(gameObject);
        }

        captureDefaultSubtitleStyle();

        //continueButton = GetComponentInChildren<StandardUIContinueButtonFastForward>().gameObject;
    }

    private void captureDefaultSubtitleStyle()
    {
        if (subtitleText == null) return;
        defaultStyle = new SubtitleFontStyle
        {
            mode = SubtitleFontMode.Default,
            font = subtitleText.font,
            fontSharedMaterial = subtitleText.fontSharedMaterial,
            fontSize = subtitleText.fontSize,
        };
    }

    public void setSubtitleFontMode(SubtitleFontMode mode)
    {
        if (subtitleText == null)
        {
            Debug.LogWarning("DialogueIndicatorManager: subtitleText reference is not set; cannot switch font mode.");
            return;
        }

        SubtitleFontStyle style = null;
        if (mode == SubtitleFontMode.Default)
        {
            style = defaultStyle;
        }
        else if (subtitleFontStyles != null)
        {
            for (int i = 0; i < subtitleFontStyles.Length; i++)
            {
                if (subtitleFontStyles[i] != null && subtitleFontStyles[i].mode == mode)
                {
                    style = subtitleFontStyles[i];
                    break;
                }
            }
        }

        if (style == null)
        {
            Debug.LogWarning($"DialogueIndicatorManager: no SubtitleFontStyle configured for {mode}; falling back to Default.");
            style = defaultStyle;
            if (style == null) return;
        }

        applySubtitleStyle(style);
    }

    private void applySubtitleStyle(SubtitleFontStyle style)
    {
        subtitleText.font = (style.font != null) ? style.font : defaultStyle.font;

        if (style.fontSharedMaterial != null)
            subtitleText.fontSharedMaterial = style.fontSharedMaterial;
        else if (style.font == null)
            subtitleText.fontSharedMaterial = defaultStyle.fontSharedMaterial;

        subtitleText.fontSize = (style.fontSize > 0f) ? style.fontSize : defaultStyle.fontSize;
    }

    void OnConversationLineEnd(Subtitle subtitle)
    {
        //continueButton.SetActive(true);
    }

    void OnConversationStart(Transform actor)
    {
        //dialogueCinematics.AddCinematicTarget(actor, 1, -1, 1.5f);
        //dialogueCinematics.Play();
        PlayerController.instance.toggleConversation(true);
    }

    void OnConversationLine(Subtitle subtitle)
    {
        //continueButton.SetActive(false);
        hideSpacebarHelptext();

        if (string.IsNullOrEmpty(subtitle.formattedText.text)) return; // Line is empty; don't show indicator.

        // When speaker changes
        if (lastSpeaker == null || subtitle.speakerInfo.transform != lastSpeaker.transform)
        {
            if (lastSpeaker != null && lastSpeaker.speakingIndicator != null)
                lastSpeaker.speakingIndicator.SetActive(false);

            if (subtitle.speakerInfo.transform.gameObject.tag == "Invisible Actor") // not a traditional conversation, but rather a note or recording
            {
                RecordingActor recordingActor = subtitle.speakerInfo.transform.gameObject.GetComponent<RecordingActor>();
                // recordings may carry an Entity holding hologram portrait sprites
                Entity recordingEntity = subtitle.speakerInfo.transform.GetComponent<Entity>();

                if (recordingActor != null)
                {
                    swapFaction(recordingActor.faction);
                    lastSpeaker = null;

                    // Show the note's localized title instead of the generic db actor name ("Note").
                    // The translation table lives on the Dialogue Manager's UILocalizationManager, not a GlobalTextTable component.
                    string conversationTitle = DialogueManager.masterDatabase.GetConversation(subtitle.dialogueEntry.conversationID).Title;
                    PixelCrushers.TextTable textTable = PixelCrushers.UILocalizationManager.instance.textTable;
                    subtitle.speakerInfo.Name = (textTable != null)
                        ? textTable.GetFieldTextForLanguage(conversationTitle, PixelCrushers.GlobalTextTable.currentLanguage)
                        : conversationTitle;
                }
                else if (recordingEntity != null)
                {
                    // Entity-based recordings (e.g. Myra) still need the faction icon, but keep their own actor name
                    swapFaction(recordingEntity.faction);
                    lastSpeaker = null;
                }

                if (recordingEntity != null)
                {
                    recordingEntity.updateSpeakerCanvasPortrait();
                    characterPortrait.SetActive(true);
                    startHologramPulse();
                }
                else
                {
                    stopHologramPulse();
                    characterPortrait.SetActive(false);
                }

                addSpeakerCinematicTarget(PlayerController.instance.transform);
            }
            else // all other speaking objects
            {
                // if it's using the character hierarchy...
                if (subtitle.speakerInfo.transform.GetComponent<Entity>() != null)
                {
                    var currentSpeaker = subtitle.speakerInfo.transform.GetComponent<Entity>();

                    if(currentSpeaker.speakingIndicator != null)
                        currentSpeaker.speakingIndicator.SetActive(true);

                    if (lastSpeaker == null || currentSpeaker.faction != lastSpeaker.faction) // don't remove extraneous null check, it's needed
                    {
                        swapFaction(currentSpeaker.faction);
                    }

                    if (currentSpeaker.transform != PlayerController.instance.transform)
                    {
                        currentSpeaker.faceObject(subtitle.listenerInfo.transform.position - currentSpeaker.transform.position, false); // speaker should point to listener
                                                                                                                                        
                        currentSpeaker.updateSpeakerCanvasPortrait(); // Update close-up portrait
                        characterPortrait.SetActive(true);
                        stopHologramPulse();
                        setPortraitTint(Color.white);
                    }


                    // Camera adjustments
                    if ((lastSpeaker == null) || (currentSpeaker.transform != lastSpeaker.transform) && (currentSpeaker != PlayerController.instance)) // this check bc we set the target on convo start
                    {
                        if (indoorZoomObj != null)
                        {
                            indoorZoomObj.resetAndStopZoom(); // prevent zoom overriding cinematics
                        }
                        else
                        {
                            Debug.LogWarning("No Indoor Zoom Obj found. Was that intentional?");
                        }

                        if(currentSpeaker.isCinematicTarget)
                            addSpeakerCinematicTarget(currentSpeaker.transform);
                        else if(!dialogueCinematics.IsPlaying) // still need cinematic to start even if not zooming in on first speaker
                            addSpeakerCinematicTarget(PlayerController.instance.transform);
                    }
                                   
                    lastSpeaker = currentSpeaker;
                }
                else
                {
                    stopHologramPulse();
                    characterPortrait.SetActive(false);
                }
            }
            
            MouseManager.instance.menusOn();
        }
    }

    private void addSpeakerCinematicTarget(Transform targetTransform)
    {
        if ((dialogueCinematics.CinematicTargets.Count >= 1) && (dialogueCinematics.CinematicTargets[dialogueCinematics.CinematicTargets.Count - 1].TargetTransform != targetTransform))
        {
            dialogueCinematics.AddCinematicTarget(targetTransform, 1f, -1, 1.5f);
            dialogueCinematics.GoToNextTarget();
        }
        else if (!dialogueCinematics.IsPlaying)
        {
            dialogueCinematics.AddCinematicTarget(targetTransform, 1f, -1, 1.5f);
            dialogueCinematics.Play();
        }
    }

    void OnConversationEnd(Transform actor)
    {
        setSubtitleFontMode(SubtitleFontMode.Default);

        stopHologramPulse();
        characterPortrait.SetActive(false);
        dialogueCinematics.Stop();
        dialogueCinematics.CinematicTargets = new List<CinematicTarget>();

        if (lastSpeaker != null)
        {
            if (lastSpeaker.speakingIndicator != null)
                lastSpeaker.speakingIndicator.SetActive(false);

            if (lastSpeaker.transform != PlayerController.instance.transform)
                lastSpeaker.faceObject(Vector2.zero, true);
        }

        if (actor.transform != PlayerController.instance.transform && actor.GetComponent<Character>() != null)
            actor.GetComponent<Character>().faceObject(Vector2.zero, true);

        lastSpeaker = null;

        PlayerController.instance.toggleConversation(false);

        if (Menus.instance.isOn) // don't signal menusOff if the main menu is open
        {
            return;
        }

        MouseManager.instance.menusOff();
    }

    void swapFaction(int faction)
    {
        if (faction == 0) // no faction means no image
            factionImage.enabled = false;
        else
        {
            factionImage.enabled = true;
            factionImage.sprite = factionArray[faction];
        }
    }
    public void updateSpritesheet(Sprite chest, Sprite legs, Sprite hair, Sprite face)
    {
        swapSprite(chestSprite, chest);
        swapSprite(legsSprite, legs);
        swapSprite(hairSprite, hair);
        swapSprite(headSprite, face);
    }

    private void setPortraitTint(Color tint)
    {
        headSprite.color = tint;
        hairSprite.color = tint;
        chestSprite.color = tint;
        legsSprite.color = tint;
    }

    private void startHologramPulse()
    {
        stopHologramPulse();
        hologramPulseCoroutine = StartCoroutine(hologramPulseRoutine());
    }

    private void stopHologramPulse()
    {
        if (hologramPulseCoroutine != null)
        {
            StopCoroutine(hologramPulseCoroutine);
            hologramPulseCoroutine = null;
        }
    }

    private IEnumerator hologramPulseRoutine()
    {
        while (true)
        {
            float t = (Mathf.Sin(Time.time * hologramPulseSpeed) + 1f) * 0.5f;
            Color tint = hologramPortraitTint;
            tint.a = Mathf.Lerp(hologramPulseMinAlpha, hologramPulseMaxAlpha, t);
            setPortraitTint(tint);
            yield return null;
        }
    }

    private void swapSprite(Image image, Sprite sprite)
    {
        if (sprite != null)
        {
            image.gameObject.SetActive(true);
            image.sprite = sprite;

            RectTransform rectTransform = image.GetComponent<RectTransform>();
            Vector2 sizeDelta = rectTransform.sizeDelta;

            sizeDelta.x = image.sprite.rect.width / 100f;
            sizeDelta.y = image.sprite.rect.height / 100f;

            rectTransform.sizeDelta = sizeDelta;           
        }
        else
            image.gameObject.SetActive(false);
    }

    public void displaySpacebarHelptext()
    {
        spaceBarHelptext.SetActive(true);
    }

    public void hideSpacebarHelptext()
    {
        spaceBarHelptext.SetActive(false);
    }

    // -------------------------------------------------------------------------------------------
    // Used by Cinematics attached to Dialogue Manager to dynamically find reference to the Canvas
    public void startCinematic()
    {
        CanvasMaster.instance.HandleCinematicStarted();
    }

    public void endCinematic()
    {
        CanvasMaster.instance.HandleCinematicComplete();
    }
    // -------------------------------------------------------------------------------------------

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PixelCrushers;

public class AlignmentCompassBehavior : MonoBehaviour
{
    public RectTransform face;
    public GameObject parentPanel;
    public float alignmentPointsToUIScale = 4.167f;
    public float unitsPerSecondTraversed = 47f;
    public ParticleSystem particles;
    [Tooltip("Center, OrderSelf, FreedomSelf, FreedomCommunity, OrderCommunity")]
    public Image[] quadrantImages;
    [Tooltip("Order, Self, Freedom, Community")]
    public Image[] iconImages;
    public Sprite questionIconImage;
    [Tooltip("Center, OrderSelf, FreedomSelf, FreedomCommunity, OrderCommunity")]
    public Color[] colors;
    public Image displayedPurposeImage;
    public Image displayedAffinityImage;
    public TextMeshProUGUI purposeText;
    public TextMeshProUGUI affinityText;

    [Header("Identity / Abilities (new UI)")]
    [Tooltip("'You Are' Grouper title text")]
    public TextMeshProUGUI youAreTitleText;
    [Tooltip("Quote Grouper/Quote text")]
    public TextMeshProUGUI quoteText;
    [Tooltip("Ability 1, Ability 2, Ability 3 rows under Abilities Grouper")]
    public AlignmentAbilityRow[] abilityRows;

    [Header("Act Progress")]
    [Tooltip("Act 1..Act 4 bars under Act Progress Grouper, in child order")]
    public Image[] actBars;
    [Tooltip("'ACT N' header text (LocalizeUI-driven)")]
    public TextMeshProUGUI actTitleText;
    public Color actCompleteColor = new Color(1f, 0.5f, 0f, 1f);          // orange
    public Color actInProgressColor = new Color(0f, 0.785954f, 1f, 1f);  // blue (current Act 1 color)
    public Color actUpcomingColor = new Color(0.3f, 0.3f, 0.35f, 1f);    // dim/grey locked

    [System.NonSerialized]
    public static AlignmentCompassBehavior instance;

    private Vector3 originalFacePos;
    private PlayerController.alignmentQuadrant currentHighlightedQuadrant;
    private Vector3[] path;
    private float lastHistoryCount;
    private Image activeXImage;
    private Image activeYImage;
    private PlayerController.alignmentQuadrant lastCheckedQuadrantForText;
    private float pathMagnitude;

    // game world coordinates
    // original location: x = 0, y = 0.5
    // radius: 11.5
    // maximum value: 24

    // gridUnitValue = 100/24 = 4.167. AKA, 1 gameworld coordinate Unit = 4.167 Alignment Points

    void Awake()
    {
        if (instance == null && this != instance)
            instance = this;
        else
        {
            Destroy(gameObject);
        }

        originalFacePos = face.anchoredPosition;

        currentHighlightedQuadrant = PlayerController.alignmentQuadrant.Center;

        parentPanel.SetActive(false);       
    }

    private void OnEnable()
    {
        // Reflect any act-based (Negotiator) unlocks the moment the panel opens.
        // Silent: opening the panel must never fire retroactive unlock popups.
        AlignmentManager.reconcileUnlocks(false);

        tryTextUpdate();
        updateActProgress();

        for (int i = 1; i < quadrantImages.Length; i++) // all but center must fade
            quadrantImages[i].CrossFadeAlpha(0.5f, 0.01f, false);

        foreach (Image image in iconImages)
            image.CrossFadeAlpha(0.5f, 0.01f, false);

        //particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particles.Clear();
        var emission = particles.emission;
        emission.enabled = false;
        face.anchoredPosition = originalFacePos;  
        tryQuadrantChange(PlayerController.alignmentQuadrant.Center); // reset to centrism if needed
        activeXImage = null;
        activeYImage = null;
        startAnim();
    }

    public void startAnim()
    {
        if (PlayerController.instance.alignmentHistory.Count < 2) // minimum of 2 points needed for our lerp strategy
        {
            face.anchoredPosition = new Vector2(originalFacePos.x, originalFacePos.y) + (PlayerController.instance.alignmentArray / alignmentPointsToUIScale);
            return;
        }

        if (lastHistoryCount != PlayerController.instance.alignmentHistory.Count) // only recalculate path if we've made more choices
        {
            Vector3 currentPathWaypoint = originalFacePos;
            path = new Vector3[PlayerController.instance.alignmentHistory.Count+1];

            pathMagnitude = 0f;

            path[0] = currentPathWaypoint; // first point is always the center

            for (int i = 1; i < path.Length; i++)
            {
                currentPathWaypoint.x += (PlayerController.instance.alignmentHistory[i-1].x / alignmentPointsToUIScale);
                currentPathWaypoint.y += (PlayerController.instance.alignmentHistory[i-1].y / alignmentPointsToUIScale);
                path[i] = currentPathWaypoint;

                pathMagnitude += PlayerController.instance.alignmentHistory[i-1].magnitude;
            }

            lastHistoryCount = PlayerController.instance.alignmentHistory.Count;
        }

        StartCoroutine(tweenOnPath(path));
    }

    private IEnumerator tweenOnPath(Vector3[] path)
    {
        yield return new WaitForSeconds(0.2f);

        var emission = particles.emission;
        emission.enabled = true;

        float currentLerpTime = 0.0f;
        float lerpTime;

        if (pathMagnitude < 70f)
            lerpTime = 1.5f;
        else
            lerpTime = pathMagnitude / unitsPerSecondTraversed;

        while(currentLerpTime < lerpTime) // to do - highlight zones
        {
            currentLerpTime += Time.deltaTime;

            if (currentLerpTime > lerpTime)
                currentLerpTime = lerpTime;

            float perc = currentLerpTime / lerpTime;
            perc = perc * perc * perc * (perc * (6f * perc - 15f) + 10f); // smootherstep lerp equation
            face.anchoredPosition = iTween.PointOnPath(path, perc);

            OnFaceMoved();

            yield return null;
        }
    }

    private void OnFaceMoved()
    {
        Vector2 checkVector = new Vector2((face.anchoredPosition.x - originalFacePos.x) * alignmentPointsToUIScale, (face.anchoredPosition.y - originalFacePos.y) * alignmentPointsToUIScale);
        tryQuadrantChange(PlayerController.instance.getAlignmentOfVec(checkVector));
    }

    private void tryQuadrantChange(PlayerController.alignmentQuadrant q)
    {
        if (currentHighlightedQuadrant == q)
            return;

        quadrantImages[getImageIndex(currentHighlightedQuadrant)].CrossFadeAlpha(0.5f, 0.1f, false);

        currentHighlightedQuadrant = q;

        Image[] tempImages = getActiveIconImages(q);

        if (tempImages[0] != activeXImage)
        {
            if (activeXImage != null)
                activeXImage.CrossFadeAlpha(0.5f, 0.1f, false);
            activeXImage = tempImages[0];
            if (activeXImage != null)
                activeXImage.CrossFadeAlpha(1f, 0.1f, false);
        }

        if (tempImages[1] != activeYImage)
        {
            if (activeYImage != null)
                activeYImage.CrossFadeAlpha(0.5f, 0.1f, false);
            activeYImage = tempImages[1];
            if (activeYImage != null)
                activeYImage.CrossFadeAlpha(1f, 0.1f, false);
        }

        quadrantImages[getImageIndex(currentHighlightedQuadrant)].CrossFadeAlpha(1f, 0.1f, false);
    }

    private int getImageIndex(PlayerController.alignmentQuadrant q)
    {
        switch(q)
        {
            case PlayerController.alignmentQuadrant.Center:
                return 0;
            case PlayerController.alignmentQuadrant.OrderSelf:
                return 1;
            case PlayerController.alignmentQuadrant.FreedomSelf:
                return 2;
            case PlayerController.alignmentQuadrant.FreedomCommunity:
                return 3;
            case PlayerController.alignmentQuadrant.OrderCommunity:
                return 4;
            default:
                return 0;
        }
            
    }

    private Image[] getActiveIconImages(PlayerController.alignmentQuadrant q)
    {
        Image[] ret = new Image[2];
        switch (q)
        {
            case PlayerController.alignmentQuadrant.Center:
                break;
            case PlayerController.alignmentQuadrant.OrderSelf:
                ret[0] = iconImages[1];
                ret[1] = iconImages[0];
                break;
            case PlayerController.alignmentQuadrant.FreedomSelf:
                ret[0] = iconImages[1];
                ret[1] = iconImages[2];
                break;
            case PlayerController.alignmentQuadrant.FreedomCommunity:
                ret[0] = iconImages[3];
                ret[1] = iconImages[2];
                break;
            case PlayerController.alignmentQuadrant.OrderCommunity:
                ret[0] = iconImages[3];
                ret[1] = iconImages[0];
                break;
            default:
                break;
        }

        return ret;
    }

    private void tryTextUpdate()
    {
        /*Debug.Log("Current is " + PlayerController.instance.currentAlignment + ". Check is " + lastCheckedQuadrantForText);

        if (PlayerController.instance.currentAlignment == lastCheckedQuadrantForText)
        {
            Debug.Log("Short circuiting, bzrrrt");
            return;
        }*/

        // The two non-center quadrants always show icons; reset visibility each pass
        // because the Act 2+ Center ('The Witness') path is the only thing that hides them.
        displayedPurposeImage.enabled = true;
        displayedAffinityImage.enabled = true;

        switch (PlayerController.instance.currentAlignment)
        {
            case PlayerController.alignmentQuadrant.Center:
                if (SaveManager.instance.act <= 1)
                {
                    // Act 1: still 'finding yourself' — question-mark icons, undecided labels.
                    displayedPurposeImage.sprite = questionIconImage;
                    displayedAffinityImage.sprite = questionIconImage;
                    purposeText.GetComponent<LocalizeUI>().fieldName = "Unknown";
                    affinityText.GetComponent<LocalizeUI>().fieldName = "Undecided";
                }
                else
                {
                    // Act 2+: committed neutrality ('The Witness') — no icons, named labels.
                    displayedPurposeImage.enabled = false;
                    displayedAffinityImage.enabled = false;
                    purposeText.GetComponent<LocalizeUI>().fieldName = "Balance";
                    affinityText.GetComponent<LocalizeUI>().fieldName = "Temperance";
                }
                setTextColor(colors[0]);
                break;
            case PlayerController.alignmentQuadrant.OrderSelf:
                displayedPurposeImage.sprite = iconImages[1].sprite;
                displayedAffinityImage.sprite = iconImages[0].sprite;
                purposeText.GetComponent<LocalizeUI>().fieldName = "Self";
                affinityText.GetComponent<LocalizeUI>().fieldName = "Order";
                setTextColor(colors[1]);
                break;
            case PlayerController.alignmentQuadrant.FreedomSelf:
                displayedPurposeImage.sprite = iconImages[1].sprite;
                displayedAffinityImage.sprite = iconImages[2].sprite;
                purposeText.GetComponent<LocalizeUI>().fieldName = "Self";
                affinityText.GetComponent<LocalizeUI>().fieldName = "Freedom";
                setTextColor(colors[2]);
                break;
            case PlayerController.alignmentQuadrant.FreedomCommunity:
                displayedPurposeImage.sprite = iconImages[3].sprite;
                displayedAffinityImage.sprite = iconImages[2].sprite;
                purposeText.GetComponent<LocalizeUI>().fieldName = "Community";
                affinityText.GetComponent<LocalizeUI>().fieldName = "Freedom";
                setTextColor(colors[3]);
                break;
            case PlayerController.alignmentQuadrant.OrderCommunity:
                displayedPurposeImage.sprite = iconImages[3].sprite;
                displayedAffinityImage.sprite = iconImages[0].sprite;
                purposeText.GetComponent<LocalizeUI>().fieldName = "Community";
                affinityText.GetComponent<LocalizeUI>().fieldName = "Order";
                setTextColor(colors[4]);
                break;
        }

        purposeText.GetComponent<LocalizeUI>().UpdateText();
        affinityText.GetComponent<LocalizeUI>().UpdateText();
        lastCheckedQuadrantForText = PlayerController.instance.currentAlignment;

        updateIdentityAndAbilities();
    }

    // Populates the new UI sections: 'You Are' title, per-quadrant quote, and the 3
    // ability rows (locked/unlocked from AlignmentManager). Center shows the Negotiator track.
    private void updateIdentityAndAbilities()
    {
        PlayerController.alignmentQuadrant q = PlayerController.instance.currentAlignment;
        int act = (SaveManager.instance != null) ? SaveManager.instance.act : 1;

        setLocalizedText(youAreTitleText, AlignmentManager.getTitleKey(q, act));
        setLocalizedText(quoteText, AlignmentManager.getQuoteKey(q));

        if (abilityRows != null)
        {
            AlignmentAbility[] abilities = AlignmentManager.getDisplayAbilities(q);
            for (int i = 0; i < abilityRows.Length; i++)
            {
                if (abilityRows[i] == null)
                    continue;

                AlignmentAbility ability = (i < abilities.Length) ? abilities[i] : null;
                abilityRows[i].bind(ability, ability != null && AlignmentManager.isUnlocked(ability.id));
            }
        }
    }

    // Colors the act bars by progress and updates the 'ACT N' header.
    // Runs on panel open (OnEnable); act is monotonic, so re-coloring every open is deterministic.
    private void updateActProgress()
    {
        int act = (SaveManager.instance != null) ? SaveManager.instance.act : 1;

        if (actTitleText != null)
            setLocalizedText(actTitleText, "ACT " + act);

        if (actBars == null)
            return;

        for (int i = 0; i < actBars.Length; i++)
        {
            if (actBars[i] == null)
                continue;

            int actNumber = i + 1;
            if (actNumber < act)
                actBars[i].color = actCompleteColor;
            else if (actNumber == act)
                actBars[i].color = actInProgressColor;
            else
                actBars[i].color = actUpcomingColor;
        }
    }

    private void setLocalizedText(TextMeshProUGUI text, string key)
    {
        if (text == null)
            return;

        LocalizeUI loc = text.GetComponent<LocalizeUI>();
        if (loc != null)
        {
            loc.fieldName = key;
            loc.UpdateText();
        }
        else
        {
            text.text = key;
        }
    }

    private void setTextColor(Color c)
    {
        purposeText.color = c;
        affinityText.color = c;
    }
}
                            
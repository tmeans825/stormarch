using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PixelCrushers;
using TMPro;

public class SmallAlertsBehavior : MonoBehaviour
{
    public Animator backdropAnimator;
    public Animator grouperAnimator;
    public Animator textAnimator;
    public Image purposeImage;
    public Image affinityImage;
    public LocalizeUI localizeUI;

    public Sprite freedomSprite;
    public Sprite orderSprite;
    public Sprite selfSprite;
    public Sprite communitySprite;

    public Image componentImage;
    public TextMeshProUGUI componentCountText;
    public Image techImage;
    public TextMeshProUGUI techCountText;

    public Image rifleAmmoImage;
    public TextMeshProUGUI rifleAmmoText;
    public Image shotgunAmmoImage;
    public TextMeshProUGUI shotgunAmmoText;

    public Image clothImage;
    public TextMeshProUGUI clothText;
    public Image chemImage;
    public TextMeshProUGUI chemText;
    public Image scrapImage;
    public TextMeshProUGUI scrapText;

    private bool isPlaying;
    private bool pendingAbilityUnlock;


    [System.NonSerialized]
    public static SmallAlertsBehavior instance;

    [System.NonSerialized]
    public int currentState = 0; // 0 - none/done, 1 - intro started, 2 - intro done;idling, 0 - Idle done

    private void Start()
    {
        if (instance == null && this != instance)
            instance = this;
        else
            Destroy(this);
    }

    public void displayAlignmentAlert(Vector2 alignmentScore)
    {
        if(alignmentScore.x > 0)
        {
            purposeImage.gameObject.SetActive(true);
            purposeImage.sprite = selfSprite;
        }
        else if(alignmentScore.x < 0)
        {
            purposeImage.gameObject.SetActive(true);
            purposeImage.sprite = communitySprite;
        }

        if (alignmentScore.y > 0)
        {
            affinityImage.gameObject.SetActive(true);
            affinityImage.sprite = orderSprite;
        }
        else if (alignmentScore.y < 0)
        {
            affinityImage.gameObject.SetActive(true);
            affinityImage.sprite = freedomSprite;
        }

        AudioMaster.instance.PlaySound("Alignment Update");

        displaySmallAlert(grouperAnimator, "Small Alert Grouper Idle");
    }

    // Generic "alignment ability unlocked" toast (see AlignmentManager.reconcileUnlocks notify
    // path). Intentionally text-only and content-free: one message regardless of how many or
    // what kind unlocked. A threshold-crossing dialogue choice fires displayAlignmentAlert in
    // the same frame, so defer behind any in-flight alert rather than overlap or be suppressed.
    public void displayAbilityUnlock()
    {
        if (isPlaying)
        {
            pendingAbilityUnlock = true;
            return;
        }
        showAbilityUnlock();
    }

    private void showAbilityUnlock()
    {
        // Reuses the already-wired objective text line (localizeUI + textAnimator).
        // TODO: add a dedicated "Ability Unlocked" SFX; reuse the alignment sting for now.
        localizeUI.fieldName = "ALIGNMENT ABILITY UNLOCKED";
        localizeUI.UpdateText();
        AudioMaster.instance.PlaySound("Alignment Update");
        displaySmallAlert(textAnimator, "Small Alert Idle");
    }

    public void displayCurrencyAlert(int componentCount, int techCount)
    {
        if (componentCount > 0)
        {
            activateIconAndText(componentCountText, componentImage, componentCount);
            AudioMaster.instance.PlaySound("Component Pickup", true);
        }

        if (techCount > 0)
        {
            activateIconAndText(techCountText, techImage, techCount);
        }

        displaySmallAlert(grouperAnimator, "Small Alert Grouper Idle");
    }

    public void displayAmmoAlert(int ammoIndex, int count)
    {
        switch(ammoIndex)
        {
            case 0:
                activateIconAndText(rifleAmmoText, rifleAmmoImage, count);
                break;
            case 2:
                activateIconAndText(shotgunAmmoText, shotgunAmmoImage, count);
                break;
            default:
                break;
        }

        displaySmallAlert(grouperAnimator, "Small Alert Grouper Idle");
    }

    public void displayCraftMatAlert(int matType, int count)
    {
        switch(matType)
        {
            case 0:
                activateIconAndTextFloat(clothText, clothImage, count/4f);
                break;
            case 1:
                activateIconAndTextFloat(chemText, chemImage, count/4f);
                break;
            case 2:
                activateIconAndTextFloat(scrapText, scrapImage, count/4f);
                break;
            default:
                break;
        }

        displaySmallAlert(grouperAnimator, "Small Alert Grouper Idle");
    }

    private void activateIconAndText(TextMeshProUGUI text, Image image, int count)
    {
        image.gameObject.SetActive(true);
        text.gameObject.SetActive(true);
        text.text = (int.Parse(text.text) + (int)count).ToString();
    }

    private void activateIconAndTextFloat(TextMeshProUGUI text, Image image, float count)
    {
        image.gameObject.SetActive(true);
        text.gameObject.SetActive(true);
        text.text = (float.Parse(text.text) + (float)count).ToString();

        if (text.text[0] == '0') // remove leading zeros
        {
            //Debug.Log("Leading 0 detected. Text contents: " + text.text);
            text.text = text.text.Substring(1);
            //Debug.Log("New text contents: " + text.text);
        }
    }

    public void displayObjectiveAlert(string text)
    {
        if (isPlaying)
           return;

        localizeUI.fieldName = text;
        localizeUI.UpdateText();

        if (text == "OBJECTIVE COMPLETE")
            AudioMaster.instance.PlaySound("Objective Complete");
        else
            AudioMaster.instance.PlaySound("Objective Updated");

        displaySmallAlert(textAnimator, "Small Alert Idle");

    }

    private void displaySmallAlert(Animator contentAnimator, string idleState)
    {      
        if (currentState == 0 || currentState == 3)
        {
            contentAnimator.gameObject.SetActive(true);
            backdropAnimator.gameObject.SetActive(true);

            contentAnimator.SetTrigger("Small Alert");
            backdropAnimator.SetTrigger("Small Alert");
            isPlaying = true;
        }

        else if(currentState == 1)
        {
            return; // just update is fine
        }

        else if(currentState == 2)
        {
            backdropAnimator.Play("Small Alert Backdrop Idle",0,0.0f); // refresh alert
            contentAnimator.Play(idleState,0,0.0f);
        }
    }

    public void donePlaying()
    {
        purposeImage.gameObject.SetActive(false);
        affinityImage.gameObject.SetActive(false);

        disableTextAndImage(componentImage, componentCountText);
        disableTextAndImage(techImage, techCountText);
        disableTextAndImage(rifleAmmoImage, rifleAmmoText);
        disableTextAndImage(shotgunAmmoImage, shotgunAmmoText);
        disableTextAndImage(clothImage, clothText);
        disableTextAndImage(chemImage, chemText);
        disableTextAndImage(scrapImage, scrapText);

        isPlaying = false;

        // An ability unlocked while a prior alert was on screen (e.g. the alignment nudge from
        // the same choice) - now that the surface is free, play the deferred generic toast.
        if (pendingAbilityUnlock)
        {
            pendingAbilityUnlock = false;
            showAbilityUnlock();
        }
    }

    private void disableTextAndImage(Image image, TextMeshProUGUI text)
    {
        image.gameObject.SetActive(false);
        text.text = "0";
        text.gameObject.SetActive(false);
    }
}

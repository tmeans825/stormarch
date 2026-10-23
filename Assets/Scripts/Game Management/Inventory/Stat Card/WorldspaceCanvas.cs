using PixelCrushers;
using PixelCrushers.DialogueSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class WorldspaceCanvas : MonoBehaviour
{
    public static WorldspaceCanvas instance;
    public GameObject worldspaceStatCard;
    public Sprite[] uniqueTraitSprites;
    public Sprite[] itemTypeSprites;
    public Sprite[] raritySprites;
    public Sprite[] armorTypeSprites;
    public Sprite critTraitSprite;
    public Sprite shieldDrainSprite;
    public Image shotCooldownCircle;
    public Image trueAimImage;
    public Sprite trueAimNeutral;
    public Sprite trueAimFailure;
    public Image trueAimSuccess;
    

    public Color positiveColor;
    public Color negativeColor;

    public Color positiveSliderColor;
    public Color negativeSliderColor;

    public Image[] trueAimTicks;
    public ParticleSystem trueAimParticles;

    public GameObject contextualRangePrompt;
    public GameObject contextualRangeImage;
    public TextMeshProUGUI contextualRangeText;

    public GameObject contextualNearPrompt;
    public TextMeshProUGUI contextualNearText;

    public GameObject kiplingEmote;
    public GameObject xEmote;
    public GameObject lockedEmote;
    public GameObject exclamationEmote;

    public TextMeshProUGUI contextualReloadText;
    public GameObject contextualReloadPrompt;

    public Sprite[] charmIconImages;

    private TextTable textTable;

    
    private Vector3 mousePosition;

    private Vector3 offset = new Vector3(0f, 4f, 0f);

    private bool isShowing = false;
    private GameObject currentObj;
    private bool failedTrueAim = false;
    private bool ableToTrueAim = false;

    private Coroutine circleCoroutine;
    private int activeTick = 0;

    private bool maxedOut = false;
    private Vector2 promptOffset = new Vector2(-17f, 26f);

    private bool isRangeHelptextOn = false;
    private GameObject rangeObj;
    private Vector3 rangeOriginalPos;
    private bool isUseHelptextOn = false;
    private GameObject useObj;
    private Vector3 useOriginalPos;

    private Coroutine reloadCoroutine;

    // New flexible stat card
    private Animator statCardAnim;
    private GameObject menuStatCard;
    private GameObject statCard;

    private GameObject[] charmRows = new GameObject[3];
    private GameObject[] charmIcons = new GameObject[3];
    private GameObject[] charmDisplayNames = new GameObject[3];

    private GameObject[] charmNames = new GameObject[3];
    private GameObject[] charmLore = new GameObject[3];

    private GameObject[] debugCharmScore = new GameObject[3]; // for testing only

    private Image statCardRarityGlow;
    private Image[] shieldImages = new Image[3];
    private Transform statCardTier;

    private TextMeshProUGUI statCardTierText;
    private Image statCardItemIcon;
    private TextMeshProUGUI statCardItemName;

    private GameObject ammoMagIndicator;
    private TextMeshProUGUI debugStats;

    private Vector3 originalStatCardScale = Vector3.one;


    private void Awake()
    {
        if (instance == null && this != instance)
            instance = this;
        else
            Destroy(gameObject);

        statCardAnim = worldspaceStatCard.GetComponent<Animator>();
        //DontDestroyOnLoad(gameObject);  
    }

    private void maxMinValueSet(ref float valMin, ref float valMax, float[] referenceMin, float[] referenceMax)
    {
        valMin = referenceMin[0];
        valMax = referenceMax[referenceMax.Length - 1];
    }

    // Start is called before the first frame update
    void Start()
    {
        menuStatCard = InventoryMenu.instance.statCard;
        initializeStatCardVariables(worldspaceStatCard);
        originalStatCardScale = statCard.transform.localScale;
        statCard.SetActive(false);

        textTable = DialogueManager.instance.gameObject.GetComponent<UILocalizationManager>().textTable;
        if (textTable == null)
            Debug.LogError("Couldn't find text table. UI elements will now be fucked up");

        trueAimSuccess.CrossFadeAlpha(0.0f, 0.01f, false);
        activeTick = trueAimTicks.Length;
        clearTicks();
    }

    private void Update()
    {
        if (shotCooldownCircle.fillAmount > 0f)
        {
            mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePosition.z = 0f;

            shotCooldownCircle.gameObject.transform.parent.position = mousePosition;
        }

        if(isRangeHelptextOn)
        {
            if (rangeObj == null)
                isRangeHelptextOn = false;
            else
                checkForObjMovement(rangeObj, ref rangeOriginalPos, contextualRangePrompt);
        }

        if(isUseHelptextOn)
        {
            if (useObj == null)
                isUseHelptextOn = false;
            else
                checkForObjMovement(useObj, ref useOriginalPos, contextualNearPrompt);
        }
    }

    private void initializeStatCardVariables(bool inMenu)
    {
        statCard?.SetActive(false);

        GameObject activeStatCard;

        if(inMenu)
            activeStatCard = menuStatCard;
        else
            activeStatCard = worldspaceStatCard;

        if (statCard == activeStatCard)
            return;
      
        statCard = activeStatCard;

        for(int i = 0; i < 3; i++)
        {
            shieldImages[i] = statCard.transform.GetChild(5).GetChild(i).GetComponent<Image>();
            shieldImages[i].gameObject.SetActive(false);
        }

        resetCharmVariables(inMenu);
        statCardRarityGlow = activeStatCard.transform.GetChild(2).GetComponent<Image>();
        statCardTier = activeStatCard.transform.GetChild(0).GetChild(3);
        statCardTierText = activeStatCard.transform.GetChild(0).GetChild(2).GetComponent<TextMeshProUGUI>();
        statCardItemIcon = activeStatCard.transform.GetChild(0).GetChild(0).GetComponent<Image>();
        statCardItemName = activeStatCard.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>();
        ammoMagIndicator = activeStatCard.transform.GetChild(3).gameObject;
        debugStats = activeStatCard.transform.GetChild(4).GetComponent<TextMeshProUGUI>();
    }

    private void resetCharmVariables(bool inMenu)
    {
        GameObject charms = statCard.transform.GetChild(1).gameObject;

        if (inMenu)
        {
            charmNames = new GameObject[3];
            charmLore = new GameObject[3];
        }
        debugCharmScore = new GameObject[3]; // for testing only

        for (int i = 0; i < charmRows.Length; i++)
        {
            charmRows[i] = charms.transform.GetChild(i).gameObject;
            charmIcons[i] = charmRows[i].transform.GetChild(0).gameObject;
            charmDisplayNames[i] = charmRows[i].transform.GetChild(2).gameObject;

            if (inMenu)
            {
                charmNames[i] = charmRows[i].transform.GetChild(1).gameObject;
                charmLore[i] = charmRows[i].transform.GetChild(3).gameObject;
            }

            debugCharmScore[i] = charmRows[i].transform.GetChild(4).gameObject; // for testing only
        }
    }


    /*private void previousSceneDataLoaded()
    {
        isRangeHelptextOn = false;
        isUseHelptextOn = false;
    }*/


    private void checkForObjMovement(GameObject obj, ref Vector3 originalPos, GameObject contextualPrompt)
    {
        if(obj.transform.position != originalPos)
        {
            originalPos = obj.transform.position;
            moveContextualPrompt(contextualPrompt, obj);          
        }
    }

    public void popUpXEmote(Vector3 location)
    {
        popUpEmote(xEmote, location); 
    }

    public void popUpLockedEmote(Vector3 location)
    {
        popUpEmote(lockedEmote, location);
    }

    public void popUpKiplingEmote(Vector3 location)
    {
        popUpEmote(kiplingEmote, location);
    }

    public void popUpReloadEmote()
    {
        contextualReloadText.text = PlayerController.instance.getReloadKey().ToString();
        contextualReloadPrompt.SetActive(false);
        contextualReloadPrompt.SetActive(true);
        if (reloadCoroutine != null)
            StopCoroutine(reloadCoroutine);
        reloadCoroutine = StartCoroutine(followThePlayer());
    }

    public void popUpExclaimEmote(Vector3 location)
    {
        popUpEmote(exclamationEmote, location);
    }

    private IEnumerator followThePlayer()
    {
        float timeElapsed = 0f;
        while (timeElapsed < 2f)
        {
            moveContextualPrompt(contextualReloadPrompt, PlayerController.instance.gameObject);
            timeElapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void popUpEmote(GameObject emote, Vector3 location)
    {
        emote.SetActive(false);
        emote.SetActive(true);
        emote.transform.position = location;
    }

    public void updateEmoteLocation(Vector3 newLoc)
    {
        kiplingEmote.transform.position = newLoc;
    }


    public void popUpHelpText(GameObject target, bool isKipling)
    { 
        Interactable interactable = target.GetComponent<Interactable>();

        if (isKipling)
        {
            if (!interactable.canKiplingUse || !PlayerController.instance.hasKipling)
                return;

            // pop up kipling key
            if (target == useObj && isUseHelptextOn)
                return; // don't pop up kipling if useObj is 

            contextualRangePrompt.SetActive(true);            
            moveContextualPrompt(contextualRangePrompt, target);
            contextualRangeText.text = getKeyToUse(isKipling);

            isRangeHelptextOn = true;
            rangeOriginalPos = target.transform.position;
            rangeObj = target;
        }
        else
        {
            if (interactable.interactableType == 4) // do nothing for pressure plates
            {
                return;
            }
            else if (interactable.interactableType == 6) // computers are a different button
            {
                if (!PlayerController.instance.hasBaton)
                    return;

                contextualNearText.text = PlayerController.instance.getStunKey().ToString();
            }
            else
            {
                contextualNearText.text = getKeyToUse(isKipling);
            }

            contextualNearPrompt.SetActive(true);

            if(target == rangeObj && isRangeHelptextOn)
               contextualRangePrompt.SetActive(false); // don't show kipling at same time if you can use it 

            moveContextualPrompt(contextualNearPrompt, target);
            
            isUseHelptextOn = true;
            useOriginalPos = target.transform.position;
            useObj = target;
        }
    }

    private void moveContextualPrompt(GameObject obj, GameObject target)
    {
        obj.transform.position = target.transform.position;
        obj.transform.localPosition += (Vector3)promptOffset;
    }

    private string getKeyToUse(bool isKipling)
    {
        if(isKipling)
            return PlayerController.instance.getInteractionKey(isKipling).ToString() + ":"; 

        return PlayerController.instance.getInteractionKey(isKipling).ToString();
    }

    public void endHelpText(bool isKipling)
    {
        if (isKipling)
        {
            isRangeHelptextOn = false;
            contextualRangePrompt.SetActive(false);
            contextualRangePrompt.GetComponentInChildren<CanvasGroup>().alpha = 1f;
        }
        else
        {
            isUseHelptextOn = false;
            contextualNearPrompt.SetActive(false);
            contextualNearText.alpha = 0.392f;
        }
    }

    public void beginCooldown(float cooldown)
    {
        if(circleCoroutine != null)
        {
            if (!ableToTrueAim)
                clearTicks();

            StopCoroutine(circleCoroutine);
            circleCoroutine = null;
        }
        
        circleCoroutine = StartCoroutine(cooldownCircleCycle(cooldown));
    }

    public void signalFailure()
    {
        trueAimImage.sprite = trueAimFailure;
        failedTrueAim = true;
        ableToTrueAim = false;
        clearTicks();
    }

    public bool checkForTrueAim()
    {
        if(ableToTrueAim)
        {
            //Debug.Log("TRUE AIM");
            StartCoroutine(signalSuccess());
            tickUp();
        }

        return ableToTrueAim;
    }

    public int getActiveTick()
    {
        if (maxedOut)
            return 5;

        return activeTick;
    }

    private IEnumerator signalSuccess()
    {
        trueAimImage.fillAmount = 0f;
        trueAimSuccess.CrossFadeAlpha(0.6f, 0.15f, false);
        yield return new WaitForSeconds(0.15f);
        trueAimSuccess.CrossFadeAlpha(0.0f, 0.15f, false);
    }

    private void tickUp()
    {
        if (activeTick < 4)
        {
            trueAimTicks[activeTick].CrossFadeAlpha(0.6f, 0.15f, false);
            activeTick++;
        }
        if (activeTick == 4)
        {
            trueAimParticles.Play();
            maxedOut = true;
        }

        //float pitch = 1f + (0.5f * ((activeTick-1) / 3f));

        //AudioMaster.instance.PlaySoundAtPitch("True Aim Click", pitch);
        AudioMaster.instance.PlaySound("Combo" + activeTick);
    }

    private void clearTicks()
    {
        maxedOut = false;
        while(activeTick > 0)
        {
            activeTick--;
            trueAimTicks[activeTick].CrossFadeAlpha(0.0f, 0.15f, false);
        }
    }

    private IEnumerator cooldownCircleCycle(float cooldown)
    {
        failedTrueAim = false;
        cooldown = cooldown / 0.95f; // initialize buffer for true aim

        float elapsedTime = 0.0f;

        trueAimImage.sprite = trueAimNeutral;
        trueAimImage.fillAmount = 0.0f;
        shotCooldownCircle.CrossFadeAlpha(1.0f, 0.15f, true);
        trueAimImage.CrossFadeAlpha(1.0f, 0.15f, true);

        while (elapsedTime <= cooldown)
        {           
            shotCooldownCircle.fillAmount = elapsedTime / cooldown;
            if(!failedTrueAim)
                trueAimImage.fillAmount = elapsedTime / cooldown;

            if (elapsedTime / cooldown >= 0.95 && !ableToTrueAim && !failedTrueAim) // 5% grace for early true aim
                ableToTrueAim = true; 

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        shotCooldownCircle.fillAmount = 1.0f;
        if(!failedTrueAim)
            trueAimImage.fillAmount = 1.0f;

        
        yield return new WaitForSeconds(0.1f); // grace time to hit true aim after 100%
        ableToTrueAim = false;

        fadeOutReloadGraphic();
    }

    private void fadeOutReloadGraphic()
    {
        shotCooldownCircle.CrossFadeAlpha(0.0f, 0.3f, true);
        trueAimImage.CrossFadeAlpha(0.0f, 0.3f, true);
        failedTrueAim = false;
        clearTicks(); // if still going, clear ticks cuz we done shootin'
    }

    public void signalItemSwap()
    {
        if (currentObj == null || !isShowing)
            return;

        InventoryItem itm = currentObj.GetComponent<Loot>().item;

        if (itm.iType == InventoryItem.itemType.Gun)
        {
            showWeaponCard(currentObj.GetComponent<Loot>().item, currentObj, true);
        }
        else if (itm.iType == InventoryItem.itemType.Armor)
        {
            showArmorCard(currentObj.GetComponent<Loot>().item, currentObj, true);
        }


    }

    public void activateCard(GameObject obj)
    {
        activateCard(obj, false);
    }

    public void activateCard(GameObject obj, bool isInMenus)
    {
        currentObj = obj;
        if (obj.layer == 17 && obj.tag == "Interactable") // if layer = pickup
        {
            InventoryItem item = obj.GetComponent<Loot>().item;
            if (item.iType == InventoryItem.itemType.Gun)
                showWeaponCard(item, obj, isInMenus);
            else if (item.iType == InventoryItem.itemType.Armor)
                showArmorCard(item, obj, isInMenus);
            else
            {
                currentObj = null;
                return;
            }

            transform.position = obj.transform.position + offset;        
        }
        else
            currentObj = null;
    }

    public void deActivateCard()
    {
        isShowing = false;

        if (menuStatCard != null)
            menuStatCard.SetActive(false);

        if (worldspaceStatCard != null)
            worldspaceStatCard.SetActive(false);
    }

    public void showArmorCard(InventoryItem item, GameObject obj, bool inMenu)
    {
        Armor armor = item as Armor;

        enableStatCard(inMenu);
        charmRows[0].SetActive(false); // armor only gets 2 charms
        updateCharmDetails(armor.charms, inMenu, 1);
        populateStatCardFields(armor.charms, armor.tier, armor.armorName, getArmorIcon(armor), armor.rarity, inMenu);

        // Shields
        shieldImages[0].transform.parent.gameObject.SetActive(true);
        updateShieldsIndicator(armor); 

        ammoMagIndicator.SetActive(false);
        debugStats.enabled = false;
    }

    public void showWeaponCard(InventoryItem item, GameObject obj, bool inMenu)
    {      
        Gun gun = item as Gun;

        enableStatCard(inMenu);
        charmRows[0].SetActive(true); // guns get up to 3 charms
        updateCharmDetails(gun.charms, inMenu, 0);
        populateStatCardFields(gun.charms, gun.tier, gun.gunName, gun.itemModel, gun.rarity, inMenu);

        // Magazine
        ammoMagIndicator.SetActive(true);
        for (int i = 1; i < ammoMagIndicator.transform.childCount -1; i++)
        {
            if (i <= gun.magSize)
                ammoMagIndicator.transform.GetChild(i).gameObject.SetActive(true);
            else
                ammoMagIndicator.transform.GetChild(i).gameObject.SetActive(false);
        }

        shieldImages[0].transform.parent.gameObject.SetActive(false); // hide shields for guns

        if (inMenu)
        {
            debugStats.enabled = true;
            debugStats.text = "R" + gun.rarity +
                                 " RN-" + gun.range +
                                 " D-" + gun.damage[0] + "-" + gun.damage[1] +
                                 " C-" + gun.critChance +
                                 " F-" + gun.fireSpeed;
        }
    }  

    private void enableStatCard(bool inMenu)
    {
        initializeStatCardVariables(inMenu);

        statCard.SetActive(true);

        if (!inMenu)
            statCardAnim.Play("StatCard");
   
        isShowing = true;
    }

    private void populateStatCardFields(List<Charm> charms, int tier, string name, Sprite itemModel, int rarity, bool inMenu)
    {
        updateRarityGlow(raritySprites, statCardRarityGlow, rarity);
        updateTiers(statCardTier, tier);
        
        statCardItemName.text = name;
        statCardItemIcon.sprite = itemModel;
    }

    private void updateCharmDetails(List<Charm> charms, bool inMenu, int startIndex)
    {
        for (int i = startIndex; i < charmRows.Length; i++) 
        {
            int charmIndex = i - startIndex;
            if (charmIndex >= charms.Count)
            {
                charmRows[i].SetActive(false);
                continue;
            }

            charmRows[i].SetActive(true);
            charmDisplayNames[i].GetComponent<TextMeshProUGUI>().text = textTable.GetFieldText(charms[charmIndex].DisplayNameKey);

            if (inMenu)
            {
                debugCharmScore[i].GetComponent<TextMeshProUGUI>().text = charms[charmIndex].budgetCost.ToString(); // TO DO - REMOVE!
                charmNames[i].GetComponent<TextMeshProUGUI>().text = textTable.GetFieldText(charms[charmIndex].CharmNameKey);
                charmLore[i].GetComponent<TextMeshProUGUI>().text = textTable.GetFieldText(charms[charmIndex].LoreKey);
            }

            updateCharmDisplayByType(charms[charmIndex], i);
        }
    }

    private void updateCharmDisplayByType(Charm charm, int charmIndex)
    {
        Charm.CharmType type = charm.GetCharmType(charm);
        Image charmImage = charmIcons[charmIndex].GetComponent<Image>();

        charmImage.sprite = getCharmIconImage(type);

        if (type == Charm.CharmType.Curse)
        {
            charmImage.color = Color.red;
            charmDisplayNames[charmIndex].GetComponent<TextMeshProUGUI>().color = Color.red;
        }
        else
        {
            charmImage.color = Color.white;
            charmDisplayNames[charmIndex].GetComponent<TextMeshProUGUI>().color = Color.white;
        }
    }

    private Sprite getCharmIconImage(Charm.CharmType type)
    {
        switch (type) // TODO - update for armors
        {
            case Charm.CharmType.Curse:
                return charmIconImages[0];
            case Charm.CharmType.Projectile:
                return charmIconImages[1];
            case Charm.CharmType.Utility:
                return charmIconImages[2];
            case Charm.CharmType.Behavior:
                return charmIconImages[3];
            case Charm.CharmType.Armor:
                return charmIconImages[4];
            default:
                Debug.LogWarning("Invalid charm type when getting icon image.");
                return uniqueTraitSprites[1]; 
        }
    }  

    private void updateShieldsIndicator(Armor armor)
    {
        for(int i = 0; i < shieldImages.Length; i++)
        {
            if (armor.shields > i || (i == 0 && armor.shields == 0))
            {
                shieldImages[i].gameObject.SetActive(true);
                shieldImages[i].fillAmount = Mathf.Clamp(armor.shields - i, 0f, 1f);
            }
            else
                shieldImages[i].gameObject.SetActive(false);
        }
    }

    private void updateRarityGlow(Sprite[] rarityArray, Image rarityGlow, int rarity)
    {
        Sprite newSprite = rarityArray[rarity];
        if (newSprite != null)
        {
            rarityGlow.enabled = true;
            rarityGlow.sprite = rarityArray[rarity];
        }
        else
            rarityGlow.enabled = false; // common and craftable don't get glows
    }

    private void updateTiers(Transform tiersObj, int tier)
    {
        statCardTierText.text = "MK ";
        if (tier == 1)
            statCardTierText.text += "I";
        else if (tier == 2)
            statCardTierText.text += "II";
        else if (tier == 3)
            statCardTierText.text += "III";

        for (int i = 0; i < 3; i++)
        {
            bool active;
            if (i == tier - 1)
                active = true;
            else
                active = false;

            tiersObj.GetChild(i).gameObject.SetActive(active);
        }
    }

    private Sprite getArmorIcon(Armor armor)
    {
        switch (armor.aType)
        {
            case 0:
                return armorTypeSprites[0];
            case 1:
                return armorTypeSprites[1];
            case 2:
                return armorTypeSprites[2];
            default:
                Debug.LogWarning("Invalid armor type when getting icon image.");
                return itemTypeSprites[3]; // empty icon
        }
    }
}

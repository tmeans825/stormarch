using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using PixelCrushers;
using PixelCrushers.DialogueSystem;

public class HomesteadUIBehavior : MonoBehaviour
{
    public GameObject[] tier1Buildings;
    public GameObject[] tier2Buildings;
    public GameObject[] tier3Buildings;
    private GameObject[,] allBuildings;

    public GameObject[] tier1Plots;
    public GameObject[] tier2Plots;
    public GameObject[] tier3Plots;
    private GameObject[,] allPlots;

    public GameObject[] tierTerrain;

    public GameObject[] tier1ConstructionPlots;
    public GameObject[] tier2ConstructionPlots;
    public GameObject[] tier3ConstructionPlots;
    private GameObject[,] allConstructionPlots;

    public GameObject descriptionBox;

    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public GameObject[] upgradesTitleDecor;
    public GameObject[] costGrouperDecor;
    public TextMeshProUGUI upgradeText;

    public TextMeshProUGUI techCount;
    public TextMeshProUGUI componentCount;

    public TextMeshProUGUI techCost;
    public TextMeshProUGUI componentCost;

    public Sprite tier1icon;
    public Sprite tier2icon;
    public Sprite tier3icon;

    public GameObject buildMenu;

    public GameObject maskParent;
    public GameObject plotParent;
    public GameObject buildingButtonParent;

    public GameObject forwardArrow;
    public GameObject backwardsArrow;

    public Image descriptionBoxIcon;

    public GameObject backButton;

    public GameObject tier1Bench;
    public GameObject tier2Bench;
    public GameObject tier3Bench;

    public bool isLoadingIn = false;

    [Header("Flags")]
    public Color[] colorArray;
    public Sprite[] secondarySprite;
    public GameObject[] flags;

    [System.NonSerialized]
    public int primaryColorInt;
    [System.NonSerialized]
    public int secondaryColorInt;
    [System.NonSerialized]
    public int secondarySpriteInt;

    [System.NonSerialized]
    public static HomesteadUIBehavior instance;

    [System.NonSerialized]
    public int activeTier = 0;

    private GameObject selectedPlotButton;
    private GameObject selectedBuildingButton;

    private TextTable textTable;

    private lastDescriptionBoxConfig lastText;
    private bool currentButtonIsTransient = false;

    private int disabledBuildingIndex = 0;

    private Vector2[,] grouperModificationOffset;
    private Vector2[,] skillTreeModificationOffset;

    private GameObject selectedUpgradeButton;

    // Start is called before the first frame update
    void Start()
    {
        if (instance == null && this != instance)
            instance = this;
        else
        {
            Destroy(gameObject);
        }

        allBuildings = new GameObject[3, Mathf.Max(tier1Buildings.Length, tier2Buildings.Length, tier3Buildings.Length)];
        allPlots = new GameObject[3, Mathf.Max(tier1Plots.Length, tier2Plots.Length, tier3Plots.Length)];
        allConstructionPlots = new GameObject[3, Mathf.Max(tier1ConstructionPlots.Length, tier2ConstructionPlots.Length, tier3ConstructionPlots.Length)];
        grouperModificationOffset = new Vector2[3, allPlots.GetLength(1)];
        grouperModificationOffset[1, 0] = new Vector2(-59f, 190f);
        grouperModificationOffset[1, 2] = new Vector2(-41f, 190f);
        grouperModificationOffset[2, 0] = new Vector2(75f, 0f);
        grouperModificationOffset[2, 1] = new Vector2(35f, 0f);
        grouperModificationOffset[2, 2] = new Vector2(108f, 0f);

        skillTreeModificationOffset = new Vector2[3, allPlots.GetLength(1)];
        skillTreeModificationOffset[1, 1] = new Vector2(0f, 4f);
        skillTreeModificationOffset[2, 0] = new Vector2(1f, 0f);
        skillTreeModificationOffset[2, 2] = new Vector2(0f, 6f);


        // Create building matrix to cross-reference
        setUpDataModels(ref tier1Buildings, ref tier2Buildings, ref tier3Buildings, ref allBuildings);
        setUpDataModels(ref tier1Plots, ref tier2Plots, ref tier3Plots, ref allPlots);
        setUpDataModels(ref tier1ConstructionPlots, ref tier2ConstructionPlots, ref tier3ConstructionPlots, ref allConstructionPlots);

        textTable = HUDUIBehavior.instance.textTable;

        GameEvents.current.onPreviousSceneDataLoaded += previousSceneDataLoaded; // we will set inactive after loading in data       
    }

    private void previousSceneDataLoaded()
    {
        isLoadingIn = true;
        // get building array index for activating on PlayerController's representation of upgrade
        addLocsToBuildings();

        while(activeTier < PlayerController.instance.homesteadTier)
        {
            tierUp(true);
        }

        // Loading in persistent skill tree data
        for (int i = 0; i < allBuildings.GetLength(0); i++)
        {
            for (int j = 0; j < allBuildings.GetLength(1); j++)
            {
                if (PlayerController.instance.skillTree[i, j] == null)
                    continue;

                if (!PlayerController.instance.skillTree[i, j].isPurchased) // if not purchased, skip
                    continue;

                // if purchased, move it into plotLoc position. 
                allBuildings[i, j].GetComponent<BuildingButton>().isPurchased = true;
                allBuildings[i, j].transform.SetParent(plotParent.transform);
                allBuildings[i, j].transform.position = allPlots[PlayerController.instance.skillTree[i, j].iPlotLoc, PlayerController.instance.skillTree[i, j].jPlotLoc].transform.position;
                allPlots[PlayerController.instance.skillTree[i, j].iPlotLoc, PlayerController.instance.skillTree[i, j].jPlotLoc].transform.SetParent(buildingButtonParent.transform);
                allPlots[PlayerController.instance.skillTree[i, j].iPlotLoc, PlayerController.instance.skillTree[i, j].jPlotLoc] = allBuildings[i, j];
                allBuildings[i, j].GetComponent<BuildingButton>().signalPurchase(false);

                // Add equivalent physical building into plot via the architect
                addBuilding(PlayerController.instance.skillTree[i, j].iPlotLoc, PlayerController.instance.skillTree[i, j].jPlotLoc, allBuildings[i, j].GetComponent<BuildingButton>().associatedBuilding, false);

                Transform skillTreeTransform = allBuildings[i, j].transform.GetChild(1);
                // Then, go thru upgrades and 'purchase' them.
                for (int k = 0; k < skillTreeTransform.childCount - 1; k++) // - 1 to exclude connectors
                {
                    if (!PlayerController.instance.skillTree[i, j].upgrades[k])
                    {
                        // TO DO: Check for dependencies. How do we do this current state?
                        continue; // this upgrade is not purchased yet.
                    }

                    skillTreeTransform.GetChild(k).GetComponent<UpgradeButton>().unlockButton();
                    skillTreeTransform.GetChild(k).GetComponent<UpgradeButton>().buyButton(); 
                }

                // Now, calculate remaining dependencies in the skillTree.
                for (int k = 0; k < skillTreeTransform.childCount - 1; k++) // - 1 to exclude connectors
                {
                    UpgradeButton upgradeButton = skillTreeTransform.GetChild(k).GetComponent<UpgradeButton>();

                    if (!upgradeButton.isDisabled) // if it's already enabled, we really don't need to calculate dependencies.
                        continue;

                    bool isPurchasable = true;
                    foreach (UpgradeButton dependency in upgradeButton.dependencies)
                    {
                        if (!dependency.isPurchased)
                        {
                            isPurchasable = false;
                            break;
                        }
                    }

                    if (isPurchasable)
                        upgradeButton.unlockButton();
                }
            }
        }

        isLoadingIn = false;
        backButton.SetActive(false);
        gameObject.SetActive(false);
    }

    public bool tryClose()
    {
        if(gameObject.activeSelf) // menu must be up in order to close
        {
            gameObject.SetActive(false);
            return true;
        }

        return false;
    }

    public void openFlagMenu()
    {
        AudioMaster.instance.PlaySound("Menu Open");
        PlayerController.instance.freezeChar(true);
        WorldspaceCanvas.instance.deActivateCard();
        FlagCustomizationUI.instance.gameObject.SetActive(true);
        PlayerController.instance.isCustomizing = true;

    }

    public void initializeFlags()
    {
        if (PlayerController.instance.flagDesignInt == -1)
            return;

        foreach (GameObject obj in flags)
        {
            obj.SetActive(true);
            obj.transform.GetChild(0).GetComponent<FlagSingleBehavior>().updateTiers();
        }
    }

    /// <summary>
    /// Add physical buildings. Owen will go build them.
    /// </summary>
    /// <param name="xCoordinate">i plot loc</param>
    /// <param name="yCoordinate">j plot loc</param>
    /// <param name="buildingObj"></param>
    private void addBuilding(int xCoordinate, int yCoordinate, GameObject buildingObj)
    {
        addBuilding(xCoordinate, yCoordinate, buildingObj, true);  
    }

    /// <summary>
    /// Add physical buildings.
    /// </summary>
    /// <param name="xCoordinate">i plot loc</param>
    /// <param name="yCoordinate">j plot loc</param>
    /// <param name="buildingObj"></param>
    /// <param name="deployOwen">Optional. Set to false to stop Owen from going to the building; to be used with loading.</param>
    private void addBuilding(int xCoordinate, int yCoordinate, GameObject buildingObj, bool deployOwen)
    {
        HomesteadTerrainHelper.instance.developBuilding(buildingObj, allConstructionPlots, xCoordinate, yCoordinate, deployOwen);
    }

    private void setUpDataModels(ref GameObject[] tier1, ref GameObject[] tier2, ref GameObject[] tier3, ref GameObject[,] mainArray)
    {
        for (int i = 0; i < tier1.Length; i++)
            mainArray[0,i] = tier1[i];
        for (int i = 0; i < tier2.Length; i++)
            mainArray[1,i] = tier2[i];
        for (int i = 0; i < tier3.Length; i++)
            mainArray[2,i] = tier3[i];
    }

    private void addLocsToBuildings()
    {
        for (int k = 0; k < allBuildings.GetLength(0); k++)
        {
            for (int l = 0; l < allBuildings.GetLength(1); l++)
            {
                if (allBuildings[k, l] == null)
                    continue;

                allBuildings[k, l].GetComponent<BuildingButton>().xLoc = k;
                allBuildings[k, l].GetComponent<BuildingButton>().yLoc = l;
            }
        }
    }

    public void expandSkillTree(GameObject button)
    {
        backButton.SetActive(true);
        
        // move skill tree accordingly based on its menu location
        for (int i = 0; i < allPlots.GetLength(0); i++)
        {
            for (int j = 0; j < allPlots.GetLength(1); j++)
            {                
                if (allPlots[i, j] == null)
                {
                    continue;
                }
                else if (allPlots[i, j] == button)
                {
                    if (skillTreeModificationOffset[i, j] != null)
                    {
                        button.transform.position += (Vector3)skillTreeModificationOffset[i, j];
                    }
                }
                else
                    allPlots[i, j].SetActive(false); // disable plots
            }
        }

        backButton.transform.parent.position = button.transform.position + new Vector3(0f, 4f, 0f);
    }

    public void backButtonPressed()
    {
        AudioMaster.instance.PlaySound("Menu Tab");
        descriptionBox.SetActive(false);
        backButton.SetActive(false);
        selectedBuildingButton.GetComponent<BuildingButton>().closeSkillTree();

        if (selectedUpgradeButton != null)
            selectedUpgradeButton.GetComponent<UpgradeButton>().deselect();

        // move skill tree accordingly based on its menu location
        for (int i = 0; i < allPlots.GetLength(0); i++)
        {
            for (int j = 0; j < allPlots.GetLength(1); j++)
            {
                if (allPlots[i, j] == null)
                {
                    continue;
                }

                allPlots[i, j].SetActive(true);
            }
        }

        selectedBuildingButton.GetComponent<BuildingButton>().deselect();
    }

    public void selectPlotButton(GameObject button)
    {
        if (selectedPlotButton != null && button.gameObject != selectedPlotButton)
        {
            selectedPlotButton.GetComponent<GenericButton>().deselect();           
        }

        if(selectedBuildingButton != null)
        {
            if (selectedBuildingButton.GetComponent<BuildingButton>().isPurchased)
                selectedBuildingButton.GetComponent<BuildingButton>().deselect();
        }

        int tier = button.GetComponent<PlotButton>().tier;

        selectedPlotButton = button;

        buildMenu.SetActive(false);
        buildMenu.SetActive(true);

        Vector2 offset;

        offset = Vector2.zero;

        for(int i = 0; i < allPlots.GetLength(0); i++)
        {
            for(int j = 0; j < allPlots.GetLength(1); j++)
            {
                if (allPlots[i, j] == null)
                {
                    continue;
                }
                else if (allPlots[i, j] == button)
                {
                    if (grouperModificationOffset[i, j] != null)
                    {
                        offset = grouperModificationOffset[i, j];
                    }

                    break;
                }
                
            }
        }

       
        buildMenu.transform.localPosition = button.transform.localPosition - new Vector3(0f, 50f) + (Vector3)offset;

        // first, 'pop' everything out of the mask
        while(maskParent.transform.childCount > 0)
        {
            maskParent.transform.GetChild(0).gameObject.SetActive(true);
            maskParent.transform.GetChild(0).SetParent(buildingButtonParent.transform);
        }

        for(int i = tier-1; i >= 0; i--)
        {
            for(int j = 0; j < tier1Buildings.Length; j++)
            {
                if (allBuildings[i, j] == null)
                    continue;

                BuildingButton buildingButton = allBuildings[i, j].GetComponent<BuildingButton>();

                if (buildingButton.isPurchased)
                    continue;

                allBuildings[i, j].transform.SetParent(maskParent.transform);
            }
        }

        // check if maskedParent child count is  < 4. If so, disable right arrow.
        if (maskParent.transform.childCount < 4)
            forwardArrow.SetActive(false);
        else
            forwardArrow.SetActive(true);

        disabledBuildingIndex = 0;

        backwardsArrow.SetActive(false);

        if (selectedBuildingButton != null)
        {
            selectedBuildingButton.GetComponent<GenericButton>().deselect();
        }

        setText("Tier " + button.GetComponent<PlotButton>().tier, true, true, 0, 0, button.transform.GetChild(0).GetComponent<Image>());
        lastText = new lastDescriptionBoxConfig("Tier " + tier, true, true, 0, 0, button.transform.GetChild(0).GetComponent<Image>(), false);
    }

    public void forwardArrowPressed()
    {
        AudioMaster.instance.PlaySound("Menu Tab");
        // IDEA - for forward and backwards buttons, keep track of # 'popped'. Forward = disable leftmost. Backward = re-enable 
        // Save each 'disabled' element to a list. Back button will grab the object at the end of that list and enable it, then pop it.
        int targetIndex = disabledBuildingIndex + 3;
        for (int i = disabledBuildingIndex; i < targetIndex; i++)
        {
            maskParent.transform.GetChild(i).gameObject.SetActive(false);
        }

        disabledBuildingIndex += 3;

        // play anims
        for(int i = disabledBuildingIndex; i < maskParent.transform.childCount; i++)
        {
            maskParent.transform.GetChild(i).gameObject.SetActive(false);
            maskParent.transform.GetChild(i).gameObject.SetActive(true);

            if (i >= disabledBuildingIndex + 2)
                break;
        }

        // Enable backwards arrow
        backwardsArrow.SetActive(true);

        // Check if only 3 enabled items are left, then disable forward arrow
        if ((disabledBuildingIndex + 3) >= maskParent.transform.childCount)
            forwardArrow.SetActive(false);
    }

    public void backArrowPressed()
    {
        AudioMaster.instance.PlaySound("Menu Tab");

        int targetIndex = disabledBuildingIndex - 3;
        for(int i = disabledBuildingIndex-1; i >= targetIndex; i--)
        {            
            maskParent.transform.GetChild(i).gameObject.SetActive(true);

            if (i == 0)
                break;
        }

        disabledBuildingIndex -= 3;

        if (disabledBuildingIndex < 0)
            disabledBuildingIndex = 0;

        forwardArrow.SetActive(true);

        // check if no disabled items are left (disabled item array?), disable back arrow
        if (disabledBuildingIndex == 0)
            backwardsArrow.SetActive(false);

        // when we build something, we need to make sure it's removed from the maskParent entirely
    }

    public void selectBuildingButton(BuildingButton button, bool clicked)
    {
        setText(button.UIKeyName, false, button.isPurchased, button.componentCost, button.techCost, button.transform.GetChild(0).GetComponent<Image>());
       
        if(clicked)
        {
            if (selectedBuildingButton != null && button.gameObject != selectedBuildingButton)
            {
                selectedBuildingButton.GetComponent<GenericButton>().deselect();
            }

            if (button.isPurchased && selectedPlotButton != null)
                selectedPlotButton.GetComponent<PlotButton>().deselect();

            if (button.isPurchased)
                buildMenu.SetActive(false);

            if (selectedUpgradeButton != null)
                selectedUpgradeButton = null;

            selectedBuildingButton = button.gameObject;
            lastText = new lastDescriptionBoxConfig(button.UIKeyName, false, button.isPurchased, button.componentCost, button.techCost, button.transform.GetChild(0).GetComponent<Image>(), false);
            currentButtonIsTransient = false;
        }
        else
        {
            currentButtonIsTransient = true;
        }
    }

    public void selectUpgradeButton(UpgradeButton button, bool clicked)
    {
        bool showCostInfo = false; // these bool values are the opposite of what you think they should be

        if (button.isDisabled || button.isPurchased)
            showCostInfo = true;

        setText(button.UIKeyName, true, showCostInfo, button.componentCost, button.techCost, selectedBuildingButton.transform.GetChild(0).GetComponent<Image>(), true);

        if (clicked)
        {
            if (selectedUpgradeButton != null && button.gameObject != selectedUpgradeButton)
            {
                selectedUpgradeButton.GetComponent<UpgradeButton>().deselect();
            }

            selectedUpgradeButton = button.gameObject;
            lastText = new lastDescriptionBoxConfig(button.UIKeyName, true, showCostInfo, button.componentCost, button.techCost, selectedBuildingButton.transform.GetChild(0).GetComponent<Image>(), true);
            currentButtonIsTransient = false;
        }
        else
            currentButtonIsTransient = true;
    }


    public void exitBuildingButton()
    {
        // need to tell if this button is transient, and if so, revert to last text
        if(currentButtonIsTransient)
        {
            setText(lastText.key, lastText.noUpgrade, lastText.noCost, lastText.compCost, lastText.techCost, lastText.icon, lastText.isUpgradeButton);
            currentButtonIsTransient = false;
        }
    }

    private  void setText(string key, bool noUpgradeText, bool noCostText, int cCost, int tCost, Image icon)
    {
        setText (key, noUpgradeText, noCostText, cCost, tCost, icon, false);
    }

    private void setText(string key, bool noUpgradeText, bool noCostText, int cCost, int tCost, Image icon, bool isUpgrade)
    {
        descriptionBox.SetActive(true);
        descriptionBoxIcon.sprite = icon.sprite;

        if (!isUpgrade)
        {           
            titleText.text = textTable.GetFieldText("Title " + key);
            descriptionText.text = textTable.GetFieldText("Description " + key);
        }
        else // upgrades pull title from selected building button and have a different key structure
        {
            titleText.text = textTable.GetFieldText("Upgrade Title " + selectedBuildingButton.GetComponent<BuildingButton>().UIKeyName);
            descriptionText.text = textTable.GetFieldText(key);
        }

        if (noUpgradeText || isUpgrade)
        {
            upgradeText.gameObject.SetActive(false);
            foreach (GameObject obj in upgradesTitleDecor)
                obj.SetActive(false);           
        }
        else
        {
            upgradeText.gameObject.SetActive(true);
            foreach (GameObject obj in upgradesTitleDecor)
                obj.SetActive(true);

            upgradeText.text = textTable.GetFieldText("Upgrade " + key);
        }

        if(noCostText)
        {
            foreach (GameObject obj in costGrouperDecor)
                obj.SetActive(false);
        }
        else
        {
            bool canAfford = true;

            foreach (GameObject obj in costGrouperDecor)
                obj.SetActive(true);

            if (cCost == 0)
                componentCost.transform.parent.gameObject.SetActive(false);
            else
            {              
                componentCost.transform.parent.gameObject.SetActive(true);
                componentCost.text = cCost.ToString();

                if (cCost > PlayerController.instance.componentCount)
                {
                    componentCost.color = Color.red;
                    canAfford = false;                   
                }
                else
                {
                    componentCost.color = Color.white;                   
                }
            }


            if (tCost == 0)
                techCost.transform.parent.gameObject.SetActive(false);
            else
            {
                techCost.transform.parent.gameObject.SetActive(true);
                techCost.text = tCost.ToString();

                if (tCost > PlayerController.instance.techCount)
                {
                    techCost.color = Color.red;
                    canAfford = false;
                }
                else
                {
                    techCost.color = Color.white;
                }
            }

            if(!canAfford)
            {
                costGrouperDecor[1].GetComponent<Button>().interactable = false;
            }
            else
            {
                costGrouperDecor[1].GetComponent<Button>().interactable = true;
            }
        }
    }

    public void tierUp()
    {
        // To do: play SFX if not loading in!
        tierUp(false);
    }

    public void tierUp(bool isLoading)
    {
        activeTier++;

        switch(activeTier)
        {
            case 1:
                unlockPlots(tier1Plots);
                tier1Bench.SetActive(true);
                break;
            case 2:               
                unlockPlots(tier2Plots);
                tier2Bench.SetActive(true);
                break;
            case 3:               
                unlockPlots(tier3Plots);
                tier3Bench.SetActive(true);
                
                break;
            default:
                break;
        }

        // Coroutine to sync up with particle effects
        HomesteadTerrainHelper.instance.developTerrain(tierTerrain, activeTier, allConstructionPlots);

        if(!isLoading)
            PlayerController.instance.homesteadTier = activeTier;

        initializeFlags();

        DialogueLua.SetVariable("HomesteadTier", activeTier);
    }

    public void clickConstruct()
    {
        PlayerController.instance.updateCurrencies(-1 * int.Parse(techCost.text), -1 * int.Parse(componentCost.text));
        techCount.text = PlayerController.instance.techCount.ToString();
        componentCount.text = PlayerController.instance.componentCount.ToString();
       
        foreach (GameObject obj in costGrouperDecor)
            obj.SetActive(false);

        lastText.noCost = true;

        // disable buildingGrouper, costGrouper, make button Purchased
        if (selectedUpgradeButton != null)
        {
            selectedUpgradeButton.GetComponent<UpgradeButton>().buyButton();
            HomesteadTerrainHelper.instance.triggerMenusBack(1.5f);

            for (int i = 0; i < selectedUpgradeButton.transform.parent.childCount - 1; i++) // - 1 to exclude connectors. Calculate dependencies
            {
                UpgradeButton upgradeButton = selectedUpgradeButton.transform.parent.GetChild(i).GetComponent<UpgradeButton>();

                // Set PlayerController representation of skillTree to match
                if (selectedUpgradeButton == upgradeButton.gameObject)
                {
                    int buildingX = selectedBuildingButton.GetComponent<BuildingButton>().xLoc;
                    int buildingY = selectedBuildingButton.GetComponent<BuildingButton>().yLoc;

                    PlayerController.instance.addPlayerUpgrade(buildingX, buildingY, i);
                }

                if (!upgradeButton.isDisabled) // if it's already enabled, we really don't need to calculate dependencies.
                    continue;

                bool isPurchasable = true;
                foreach (UpgradeButton dependency in upgradeButton.dependencies)
                {
                    if (!dependency.isPurchased)
                    {
                        isPurchasable = false;
                        break;
                    }
                }

                if (isPurchasable)
                    upgradeButton.unlockButton();

                
            }           
        }       
        else
        {
            selectedBuildingButton.GetComponent<BuildingButton>().isPurchased = true;
            int buildingX = selectedBuildingButton.GetComponent<BuildingButton>().xLoc;
            int buildingY = selectedBuildingButton.GetComponent<BuildingButton>().yLoc;

            for (int i = 0; i < allPlots.GetLength(0); i++)
            {
                for (int j = 0; j < allPlots.GetLength(1); j++)
                {
                    if (allPlots[i, j] == null)
                    {
                        continue;
                    }
                    else if (allPlots[i, j] == selectedPlotButton)
                    {
                        // we have the coordinates! Do the swap
                        selectedBuildingButton.transform.SetParent(plotParent.transform);
                        selectedBuildingButton.transform.position = selectedPlotButton.transform.position;

                        // selected building button                                      
                        selectedPlotButton.transform.SetParent(buildingButtonParent.transform);

                        allPlots[i, j] = selectedBuildingButton;
                        PlayerController.instance.addPlayerSkill(buildingX, buildingY, i, j);


                        selectedBuildingButton.GetComponent<BuildingButton>().signalPurchase();
                        expandSkillTree(selectedBuildingButton);

                        // Add physical building to world
                        addBuilding(i, j, selectedBuildingButton.GetComponent<BuildingButton>().associatedBuilding);

                        break;
                    }

                }
              
                if (selectedBuildingButton.transform.parent == plotParent.transform) // if we've moved building into a plot, we can quit 
                    break;
            }

            // do this last
            buildMenu.SetActive(false);          
        }

        HomesteadUIBehavior.instance.gameObject.SetActive(false);
    }

    


    private void unlockPlots(GameObject[] objs)
    {
        foreach(GameObject obj in objs)
        {
            obj.GetComponent<GenericButton>().unlock();
        }
    }

    private void OnEnable()
    {
        if (PlayerController.instance != null)
        {
            techCount.text = PlayerController.instance.techCount.ToString();
            componentCount.text = PlayerController.instance.componentCount.ToString();
            MouseManager.instance.menusOn();
        }
    }

    private void OnDisable()
    {
        if(selectedPlotButton != null)
            selectedPlotButton.GetComponent<GenericButton>().deselect();

        buildMenu.SetActive(false);

        descriptionBox.SetActive(false);

        MouseManager.instance.menusOff();
    }

    public void close()
    {
        PlayerController.instance.closeHomestead();
    }

    // CLASS

    public class lastDescriptionBoxConfig
    {
        public string key;
        public bool noUpgrade;
        public bool noCost;
        public int compCost;
        public int techCost;
        public Image icon;
        public bool isUpgradeButton;

        public lastDescriptionBoxConfig(string k, bool su, bool sc, int cc, int tc, Image i, bool iub)
        {
            key = k;
            noUpgrade = su;
            noCost = sc;
            compCost = cc;
            techCost = tc;
            icon = i;
            isUpgradeButton = iub;
        }
    }
}

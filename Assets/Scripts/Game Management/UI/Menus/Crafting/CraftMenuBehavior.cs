using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using PixelCrushers;

public class CraftMenuBehavior : MonoBehaviour
{
    public RadialLayout radialLayout;
    public CraftButton[] buttons;

    public GameObject contextMenu;
    public GameObject contextMenuClothParent;
    public GameObject contextMenuChemParent;
    public GameObject contextMenuScrapParent;
    public LocalizeUI nameText;
    private Image clothFill;
    private Image chemFill;
    private Image scrapFill;
    private TextMeshProUGUI clothText;
    private TextMeshProUGUI chemText;
    private TextMeshProUGUI scrapText;
    private bool currentSelectionAffordable = false;

    [System.NonSerialized]
    public static CraftMenuBehavior instance;

    private bool[] unlockedCraftingArray = new bool[8];

    [System.NonSerialized]
    public CraftButton currentselection;

    // Start is called before the first frame update
    void Start()
    {
        //UnityEngine.Debug.Log("PlayerController Awake");
        if (instance == null && this != instance)
            instance = this;
        else
        {
            //UnityEngine.Debug.Log("Destroying playerController duplicate");
            Destroy(gameObject);
        }

        for (int i = 0; i < unlockedCraftingArray.Length; i++)
            unlockedCraftingArray[i] = true; // TO DO - only set the standard stuff to true.

        clothFill = contextMenuClothParent.transform.GetChild(1).GetChild(0).GetComponent<Image>();
        clothText = contextMenuClothParent.transform.GetChild(2).GetComponent<TextMeshProUGUI>();
        chemFill = contextMenuChemParent.transform.GetChild(1).GetChild(0).GetComponent<Image>();
        chemText = contextMenuChemParent.transform.GetChild(2).GetComponent<TextMeshProUGUI>();
        scrapFill = contextMenuScrapParent.transform.GetChild(1).GetChild(0).GetComponent<Image>();
        scrapText = contextMenuScrapParent.transform.GetChild(2).GetComponent<TextMeshProUGUI>();

        gameObject.SetActive(false);
    }

    public void refreshContextMenu(InventoryItem item, int costCloth, int costChem, int costScrap)
    {
        contextMenu.SetActive(true);

        nameText.fieldName = LootMaster.instance.getUIName(item);
        nameText.UpdateText();

        // to do - update crafting name
        updateBaseItems();

        currentSelectionAffordable = true; // updatecostItems will set this to false if we can't afford one
        updateCostItems(contextMenuClothParent, costCloth, clothText);
        updateCostItems(contextMenuChemParent, costChem, chemText);
        updateCostItems(contextMenuScrapParent, costScrap, scrapText);

        // Medkit slot full — disable crafting and force red cost visuals so the player sees why
        if (isMedkitRecipe(item) && InventoryManager.instance.isMedkitStackFull())
        {
            currentSelectionAffordable = false;
            if (costCloth > 0) clothText.color = Color.red;
            if (costChem > 0) chemText.color = Color.red;
            if (costScrap > 0) scrapText.color = Color.red;
        }
    }

    private bool isMedkitRecipe(InventoryItem item)
    {
        if (item == null || item.iType != InventoryItem.itemType.Healable)
            return false;
        Healable h = item as Healable;
        return h != null && h.hType == Healable.healableType.Medkit;
    }

    public void clickedButton(InventoryItem item, int costCloth, int costChem, int costScrap)
    {
        // start craftin'
        if(!currentSelectionAffordable)
        {
            return; // to do - sound fx, graphics?
        }

        // to do - kick off crafting progress bar. on complete, execute reduction in goods + refresh context menu
        PlayerProgressBarController.instance.attemptCraft(item, costCloth, costChem, costScrap);
    }

    public void craftingComplete(InventoryItem item, int costCloth, int costChem, int costScrap)
    {
        // Recheck medkit capacity BEFORE consuming materials. Belt-and-braces alongside
        // the affordability check — if the slot filled between click and complete, refund.
        if (isMedkitRecipe(item) && InventoryManager.instance.isMedkitStackFull())
        {
            refreshContextMenu(currentselection.item, currentselection.costCloth, currentselection.costChem, currentselection.costScrap);
            return;
        }

        InventoryManager.instance.reduceCraftingMat(0, costCloth*4);
        InventoryManager.instance.reduceCraftingMat(1, costChem*4);
        InventoryManager.instance.reduceCraftingMat(2, costScrap*4);

        bool routedDirectly = false;

        switch (item.iType) // For items that need stats - generate them
        {
            case InventoryItem.itemType.Healable:
                Healable healable = item as Healable;
                if (healable.hType == Healable.healableType.Bandage)
                    item = LootMaster.instance.createHealable(Healable.healableType.Bandage);
                else
                {
                    item = LootMaster.instance.createHealable(Healable.healableType.Medkit);
                    InventoryManager.instance.equip(InventoryManager.MEDKIT_SLOT, item);
                    routedDirectly = true;
                }
                break;
            case InventoryItem.itemType.Lantern:
                item = LootMaster.instance.createLantern(100f);
                break;
            case InventoryItem.itemType.Throwable:
                Throwable throwable = item as Throwable;
                if (throwable.tType == Throwable.throwableType.Grenade)
                    item = LootMaster.instance.createGrenade();
                else
                    item = LootMaster.instance.createFirebomb();
                break;
            case InventoryItem.itemType.Conductor:
                item = LootMaster.instance.createConductor(3);
                break;
            case InventoryItem.itemType.RifleAmmo:
                item = LootMaster.instance.createAmmo(0, 10);
                break;
            case InventoryItem.itemType.ShotgunAmmo:
                item = LootMaster.instance.createAmmo(2, 5);
                break;
            default:
                break;
        }

        if (!routedDirectly)
            LootMaster.instance.spawnItem(item, PlayerController.instance.transform.position, true, false, false);

        refreshContextMenu(currentselection.item, currentselection.costCloth, currentselection.costChem, currentselection.costScrap);

        AudioMaster.instance.PlaySound("Craft Complete");
    }

    private void updateBaseItems()
    {
        InventoryMenu.instance.updateCraftingMats(0, clothText, clothFill);
        InventoryMenu.instance.updateCraftingMats(1, chemText, chemFill);
        InventoryMenu.instance.updateCraftingMats(2, scrapText, scrapFill);
    }

    private void updateCostItems(GameObject parentObj, int cost, TextMeshProUGUI text)
    {
        if (cost > 0)
        {
            parentObj.GetComponent<CanvasGroup>().alpha = 1f;

            if (cost > int.Parse(text.text))
            {
                text.color = Color.red;
                currentSelectionAffordable = false;
            }
            else
                text.color = Color.green;

            text.text += "/" + cost;
        }
        else
        {
            text.color = Color.white;
            parentObj.GetComponent<CanvasGroup>().alpha = 0.5f;
        }
    }

    private void OnEnable()
    {
        MouseManager.instance.menusOn();
        transform.position = MouseManager.instance.transform.position;
        StartCoroutine(radialMenuLerp());
    }

    private void OnDisable()
    {
        MouseManager.instance.menusOff();
        contextMenu.SetActive(false);
    }

    private IEnumerator radialMenuLerp()
    {
        float duration = 0.5f;
        float time = 0.0f;
        float endPosition = 0f;

        // Calculate angle based on unlocked array + whether or not in safe crafting area
        for(int i = 0; i < unlockedCraftingArray.Length; i++)
        {
            if (!unlockedCraftingArray[i])
                continue;

            if (!EventManager.instance.isCampingNow() && !buttons[i].canCraftAnywhere)
                continue;

            endPosition += 30f; // 30 degrees per button 9 buttons over 270 degrees
            buttons[i].gameObject.SetActive(true);
        }

        while (time < duration)
        {
            float t = TEMLib.getSmoothLerpT(time, duration);
            radialLayout.MinAngle = Mathf.Lerp(0f, endPosition, t);
            time += Time.deltaTime;
            yield return null;
        }
    }
}

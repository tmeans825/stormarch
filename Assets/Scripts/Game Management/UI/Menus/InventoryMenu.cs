using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InventoryMenu : MonoBehaviour
{
    [System.NonSerialized]
    public static InventoryMenu instance;

    public GameObject[] ammos;
    public GameObject[] armorsSlides;
    public GameObject[] playerModel;
    public TextMeshProUGUI quickUse1Text;
    public TextMeshProUGUI quickUse2Text;
    public TextMeshProUGUI componentText;
    public TextMeshProUGUI techText;

    public TextMeshProUGUI clothCountText;
    public Image clothPartialCountFill;
    public TextMeshProUGUI chemCountText;
    public Image chemPartialCountFill;
    public TextMeshProUGUI scrapCountText;
    public Image scrapPartialCountFill;

    public Color orangeColor;

    public GameObject batonSlotObj;

    public GameObject statCard;

    private GameObject[] slots;
    private GameObject[] slides;
    private GameObject[] outlines;
    private GameObject[] beanCounters;
    private GameObject[] draggables;

    private GameObject parentHotbar;

    private TextMeshProUGUI[] ammoText = new TextMeshProUGUI[3];

    private GameObject armorCard;

    public GameObject activeSlot;

    void Awake()
    {
        if (instance == null && this != instance)
            instance = this;
        else
            Destroy(this);

        parentHotbar = transform.GetChild(1).gameObject;

        int slotCount = parentHotbar.transform.childCount - 1; // last child is the separator

        // These indices are passed straight into HotbarUIBehavior.copySprites, which
        // indexes the HUD hotbar's own arrays — the two hotbars must stay the same size.
        if (slotCount != InventoryManager.INVENTORY_SIZE)
            throw new System.InvalidOperationException("Inventory menu hotbar has " + slotCount
                + " slots but INVENTORY_SIZE is " + InventoryManager.INVENTORY_SIZE
                + " — the Canvas prefab and the code are out of sync.");

        slots = new GameObject[slotCount];
        slides = new GameObject[slotCount];
        outlines = new GameObject[slotCount];
        beanCounters = new GameObject[slotCount];
        draggables = new GameObject[slotCount];

        for(int i = 0; i < 2; i++)
            ammoText[i] = ammos[i].GetComponent<TextMeshProUGUI>();

        for (int i = 0; i < slotCount; i++) // instantiate data structures
        {
            slots[i] = parentHotbar.transform.GetChild(i).gameObject;
            draggables[i] = slots[i].transform.GetChild(0).gameObject;
            slides[i] = draggables[i].transform.GetChild(0).gameObject;
            outlines[i] = slides[i].transform.GetChild(0).gameObject;
            beanCounters[i] = draggables[i].transform.GetChild(1).gameObject;
        }

        playerModel[1].GetComponent<Image>().sprite = PlayerController.instance.heads[1].GetComponent<SpriteRenderer>().sprite; // skin

        updateInventory();
    }

    private void OnEnable()
    {
        updateInventory();

        batonSlotObj.SetActive(PlayerController.instance.hasBaton);
    }

    public void close()
    {
        activeSlot = null;

        WorldspaceCanvas.instance.deActivateCard();
    }


    public void updateInventory()
    {
        updateSlideArrays();
        // update each inv slide
        for(int i = 0; i < slides.Length; i++)
        {
            HotbarUIBehavior.instance.copySprites(i, slides[i], outlines[i], draggables[i],beanCounters[i]);
            if (draggables[i].activeSelf)
                slots[i].GetComponent<DroppableSlot>().isSlotted = true;
            else
                slots[i].GetComponent<DroppableSlot>().isSlotted = false;
        }


        // update armor slides
        if (InventoryManager.instance.getEquippedArmor(0).iType != InventoryItem.itemType.Empty) // only show hair slide if wearing helmet
        {
            armorsSlides[0].GetComponent<Image>().enabled = true;
            armorsSlides[0].GetComponent<Image>().sprite = PlayerController.instance.heads[2].transform.GetChild(0).GetComponent<SpriteRenderer>().sprite;
        }

        string sheetName = InventoryManager.instance.getEquippedArmor(1).spritesheetName;
        if (sheetName == null)
            sheetName = "Chest_Base_1"; // fixes edge case, ugly coding
        var chestSprites = Resources.LoadAll<Sprite>("Chests/" + sheetName);
        armorsSlides[1].GetComponent<Image>().sprite = chestSprites[22];

        sheetName = InventoryManager.instance.getEquippedArmor(2).spritesheetName;
        if (sheetName == null)
            sheetName = "Legs_Base_1"; // fixes edge case, ugly coding
        var legsSprites = Resources.LoadAll<Sprite>("Legs/" + sheetName);
        armorsSlides[2].GetComponent<Image>().sprite = legsSprites[22];

        // update player model
        playerModel[0].GetComponent<Image>().sprite = PlayerController.instance.heads[1].transform.GetChild(0).GetComponent<SpriteRenderer>().sprite;
        playerModel[3].GetComponent<Image>().sprite = chestSprites[14];
        playerModel[2].GetComponent<Image>().sprite = legsSprites[14];


        // update ammo count
        ammoText[0].text = InventoryManager.instance.getAmmo(0).ToString();
        ammoText[1].text = InventoryManager.instance.getAmmo(2).ToString();

        // update crafting material count
        updateCraftingMats(0, clothCountText, clothPartialCountFill);
        updateCraftingMats(1, chemCountText, chemPartialCountFill);
        updateCraftingMats(2, scrapCountText, scrapPartialCountFill);

        // update tech and comps
        updateCurrencies();
    }

    public void updateCraftingMats(int matType, TextMeshProUGUI text, Image partialFill)
    {
        int count = InventoryManager.instance.getCraftingMatCount(matType);

        if (InventoryManager.instance.isCraftingMatFull(matType))
            text.color = orangeColor;
        else
            text.color = Color.white;

        int roundedNumber = ((int)(count / 4));

        float leftovers = count - (roundedNumber * 4);
        leftovers = leftovers / 4f;

        text.text = roundedNumber.ToString();
        partialFill.rectTransform.localScale = new Vector3(1f, leftovers, 1f);
    }

    public void updateCurrencies()
    {
        componentText.text = PlayerController.instance.componentCount.ToString();
        techText.text = PlayerController.instance.techCount.ToString();
    }

    public void updateSlideArrays()
    {
        for (int i = 0; i < slots.Length; i++) // instantiate data structures
        {
            draggables[i] = slots[i].transform.GetChild(0).gameObject;
            slides[i] = draggables[i].transform.GetChild(0).gameObject;
            outlines[i] = slides[i].transform.GetChild(0).gameObject;
            beanCounters[i] = draggables[i].transform.GetChild(1).gameObject;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HotbarUIBehavior : MonoBehaviour
{
    public float delayBeforeCollapse = 1.0f;
    public GameObject audioMaster;
    public TextMeshProUGUI quickUse1Text;
    public TextMeshProUGUI quickUse2Text;

    [Header("Medkit Slot Placeholder")]
    public Sprite emptyMedkitIcon;

    [System.NonSerialized]
    public static HotbarUIBehavior instance;

    private GameObject[] slots;
    private GameObject[] slides;
    private GameObject[] outlines;
    private TextMeshProUGUI[] texts;
    private GameObject[] beanCounters;
    private int selectedSlot = 0;
    private Color selectedColor;
    private bool extended = true;
    private float timeAtLastSelection = 0.0f;
    private bool isMonitoring = false;
    private GameObject separator;

    void Awake()
    {
        if (instance == null && this != instance)
            instance = this;
        else
            Destroy(this);

        int slotCount = gameObject.transform.childCount-1; // last child is the separator

        if (slotCount != InventoryManager.INVENTORY_SIZE)
            throw new System.InvalidOperationException("Hotbar has " + slotCount
                + " slots but INVENTORY_SIZE is " + InventoryManager.INVENTORY_SIZE
                + " — the Canvas prefab and the code are out of sync.");

        slots = new GameObject[slotCount];
        slides = new GameObject[slotCount];
        outlines = new GameObject[slotCount];
        texts = new TextMeshProUGUI[slotCount];
        beanCounters = new GameObject[slotCount];
        separator = transform.GetChild(transform.childCount - 1).gameObject;

        for(int i = 0; i < slotCount; i++) // instantiate data structures
        {
            slots[i] = gameObject.transform.GetChild(i).gameObject;
            slides[i] = slots[i].transform.GetChild(0).gameObject;
            outlines[i] = slides[i].transform.GetChild(0).gameObject;
            beanCounters[i] = slots[i].transform.GetChild(2).gameObject;
            texts[i] = slots[i].transform.GetChild(1).gameObject.GetComponent<TextMeshProUGUI>();
        }

        selectedColor = texts[selectedSlot].color;
        selectSlot(selectedSlot);

        // The medkit slot starts as a faded placeholder until something equips it
        unequip(InventoryManager.MEDKIT_SLOT);
    }

    private void Start()
    {
        syncQuickUseTextWithPlayerController();
    }

    public void syncQuickUseTextWithPlayerController()
    {
        if (PlayerController.instance == null)
            return;

        updateQuickUseText(1, PlayerController.instance.getQuickUseKey(1).ToString());
        updateQuickUseText(2, PlayerController.instance.getQuickUseKey(2).ToString());
    }

    public void selectSlot(int index)
    {
        if (!extended)
            extendHotbar();
        unSelectSlot();
        selectedSlot = index;
        if(slides[selectedSlot].activeSelf && !isEmptyMedkitPlaceholder(selectedSlot))
            outlines[selectedSlot].SetActive(true);
        texts[selectedSlot].color = selectedColor;

        timeAtLastSelection = Time.time;


        if (!isMonitoring)
            StartCoroutine(monitorCollapse());

        AudioMaster.instance.PlaySound("UI Hotbar Ping");
        HealthUIBehavior.instance.resetEventHappened();
    }

    public void unequip(int index)
    {
        Image slideImg = slides[index].GetComponent<Image>();
        Image outlineImg = outlines[index].GetComponent<Image>();

        if (index == InventoryManager.MEDKIT_SLOT && emptyMedkitIcon != null)
        {
            // faded medkit placeholder — slot stays visible but at half alpha
            slideImg.sprite = emptyMedkitIcon;
            slideImg.enabled = true;
            outlineImg.enabled = false;
            outlines[index].SetActive(false);
            slideImg.CrossFadeAlpha(0.5f, 0f, true);
            return;
        }

        slideImg.enabled = false;
        outlineImg.enabled = false;
    }

    public void equip(int slideIndex, Sprite itemSprite, Sprite outlineSprite)
    {
        Image slideImg = slides[slideIndex].GetComponent<Image>();
        Image outlineImg = outlines[slideIndex].GetComponent<Image>();

        slideImg.sprite = itemSprite;
        outlineImg.sprite = outlineSprite;
        slideImg.enabled = true;
        outlineImg.enabled = true;

        if (slideIndex == InventoryManager.MEDKIT_SLOT)
        {
            // restore full alpha — clears any leftover fade from the empty placeholder
            slideImg.CrossFadeAlpha(1f, 0f, true);
            outlineImg.CrossFadeAlpha(1f, 0f, true);
        }
    }

    public void updateBeanCounter(int count, int maxCount, int slot) // probably going to want to call this whenever you update the inventory!
    {
        for(int i = 0; i < maxCount; i++)
        {
            if (i != 0)
            {
                bool on;

                if (i < count)
                    on = true;
                else
                    on = false;

                beanCounters[slot].transform.GetChild(i-1).gameObject.SetActive(on);
            }
        }
    }

    public void updateQuickUseText(int quickUseSlot, string newText)
    {
        string finalText = "";
        if (newText.Length > 3)
        {
            foreach (char c in newText)
            {
                if (char.IsUpper(c))
                { 
                    finalText += c;
                }
            }

            if(finalText.Length < 2)
                finalText = newText.Remove(3, newText.Length - 3);
        }
        else
            finalText = newText;

        if (quickUseSlot == 1)
        {
            if (quickUse1Text != null)
                quickUse1Text.text = finalText;
            if (InventoryMenu.instance != null && InventoryMenu.instance.quickUse1Text != null)
                InventoryMenu.instance.quickUse1Text.text = finalText;
        }
        else if (quickUseSlot == 2)
        {
            if (quickUse2Text != null)
                quickUse2Text.text = finalText;
            if (InventoryMenu.instance != null && InventoryMenu.instance.quickUse2Text != null)
                InventoryMenu.instance.quickUse2Text.text = finalText;
        }
        else
            UnityEngine.Debug.LogWarning("Incorrect quick use slot passed to update. Won't update hotbar text.");
    }

    private void unSelectSlot()
    {
        outlines[selectedSlot].SetActive(false);
        texts[selectedSlot].color = Color.white;
    }

    private bool isEmptyMedkitPlaceholder(int index)
    {
        return InventoryManager.instance != null
            && index == InventoryManager.MEDKIT_SLOT
            && InventoryManager.instance.getEquipped(index).iType == InventoryItem.itemType.Empty;
    }
    
    private void collapseHotbar()
    {
        // The quick use pair always stays visible, as does the selected slot. The last
        // weapon slot is a fallback so a collapsed bar is never all quick-use — it only
        // shows when the selection isn't already an earlier weapon slot.
        int lastWeaponSlot = InventoryManager.QUICK_USE_1_SLOT - 1;

        bool oneVisible = false;
        for (int i = 0; i < slots.Length; i++)
        {
            bool keepVisible = false;
            if (i == selectedSlot
                || i >= InventoryManager.QUICK_USE_1_SLOT
                || (i == lastWeaponSlot && oneVisible == false))
            {
                keepVisible = true;
                oneVisible = true;
            }

            slots[i].GetComponent<SlotAnimation>().collapseAnimation(keepVisible);
        }
        separator.GetComponent<SlotAnimation>().collapseAnimation(true);
        extended = false;
    }

    private void extendHotbar()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].GetComponent<SlotAnimation>().extendAnimation();
        }
        separator.GetComponent<SlotAnimation>().extendAnimation();
            extended = true;

        // SlotAnimation.extendAnimation cross-fades every enabled slide/outline back to
        // alpha 1 — that overrides the empty medkit placeholder's intentional 0.5 fade.
        // Re-apply it here so the placeholder stays visibly faded after the hotbar opens.
        reapplyMedkitPlaceholderFadeIfEmpty();
    }

    private void reapplyMedkitPlaceholderFadeIfEmpty()
    {
        if (InventoryManager.instance == null)
            return;
        if (InventoryManager.instance.getEquipped(InventoryManager.MEDKIT_SLOT).iType != InventoryItem.itemType.Empty)
            return;

        unequip(InventoryManager.MEDKIT_SLOT);
    }

    private IEnumerator monitorCollapse()
    {
        bool monitoring = true;
        isMonitoring = true;
        while (monitoring)
        {
            yield return new WaitForSeconds(0.25f);
            if (Time.time > timeAtLastSelection + delayBeforeCollapse)
            {
                collapseHotbar();
                monitoring = false;
            }
        }
        isMonitoring = false;
    }

    public void copySprites(int inventorySlot, GameObject slide, GameObject outline, GameObject draggable, GameObject beanCounter)
    {
        // The medkit slot's faded placeholder is decorative — never copy it as a draggable item
        if (inventorySlot == InventoryManager.MEDKIT_SLOT
            && InventoryManager.instance.getEquipped(inventorySlot).iType == InventoryItem.itemType.Empty)
        {
            draggable.SetActive(true);
            draggable.GetComponent<Draggable>().SetCanDrag(false);
            slide.GetComponent<Image>().sprite = emptyMedkitIcon;
            slide.GetComponent<Image>().enabled = true;
            slide.GetComponent<Image>().CrossFadeAlpha(0.5f, 0f, true);
            outline.SetActive(false);
            outline.GetComponent<Image>().enabled = false;
            for (int i = 0; i < 3; i++)
                beanCounter.transform.GetChild(i).gameObject.SetActive(false);
            return;
        }

        if (slides[inventorySlot].GetComponent<Image>().enabled)
        {
            draggable.SetActive(true);
            draggable.GetComponent<Draggable>().SetCanDrag(inventorySlot != InventoryManager.MEDKIT_SLOT);
            slide.GetComponent<Image>().sprite = slides[inventorySlot].GetComponent<Image>().sprite;
            outline.GetComponent<Image>().sprite = outlines[inventorySlot].GetComponent<Image>().sprite;
            for(int i = 0; i < 3; i++)
            {
                beanCounter.transform.GetChild(i).gameObject.SetActive(beanCounters[inventorySlot].transform.GetChild(i).gameObject.activeSelf);
            }
        }
        else
        {
            draggable.GetComponent<Draggable>().SetCanDrag(true);
            draggable.SetActive(false);
        }
    }
}

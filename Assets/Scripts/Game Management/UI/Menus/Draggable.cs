using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Draggable : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IEndDragHandler, IDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    public bool hasParent = true;
    
    private Canvas canvas;
    private RectTransform rectTransform;
    private GameObject outline;

    private GameObject slot;
    private GameObject hotbar;
    private GameObject fillParent;

    private CanvasGroup canvasGroup;
    private bool canDrag = true;

    public int originalSlot;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        outline = transform.GetChild(0).GetChild(0).gameObject;
        canvas = transform.root.GetComponent<Canvas>();

        slot = transform.parent.gameObject;
        hotbar = slot.transform.parent.gameObject;
        fillParent = hotbar.transform.parent.parent.gameObject;

        originalSlot = slot.GetComponent<DroppableSlot>().slotNumber;

        canvasGroup = GetComponent<CanvasGroup>();
        hasParent = true;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!canDrag)
            return;

        canvasGroup.alpha = .6f;
        canvasGroup.blocksRaycasts = false;
        slot.GetComponent<DroppableSlot>().isSlotted = false;
        transform.SetParent(hotbar.transform);
        hasParent = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!canDrag)
            return;

        //Vector2 newPos = eventData.delta / slot.transform.localScale.x / hotbar.transform.localScale.x / fillParent.transform.localScale.x / canvas.scaleFactor;
        Vector2 newPos = eventData.delta / hotbar.transform.localScale.x / fillParent.transform.localScale.x / canvas.scaleFactor;
        rectTransform.anchoredPosition += newPos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!canDrag)
            return;

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        StartCoroutine(checkForUnequip());
    }

    public void OnPointerDown(PointerEventData eventData)
    {
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!canDrag)
        {
            outline.SetActive(false);
            return;
        }

        outline.SetActive(true);

        if (InventoryMenu.instance.activeSlot != this.gameObject)
        {           
            if (InventoryManager.instance.getEquipped(originalSlot - 1).iType == InventoryItem.itemType.Gun)
            {
                WorldspaceCanvas.instance.showWeaponCard(InventoryManager.instance.getEquipped(originalSlot - 1), null, true);
            }

            AudioMaster.instance.PlaySound("UI Hotbar Ping");

            InventoryMenu.instance.activeSlot = this.gameObject;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        outline.SetActive(false);
    }

    public void updateSlot(GameObject newParent, int slotNo)
    {
        this.transform.SetSiblingIndex(0);
        slot = newParent;
        originalSlot = slotNo;
    }

    public GameObject getSlot()
    {
        return slot;
    }

    public void SetCanDrag(bool canDrag)
    {
        this.canDrag = canDrag;
        if (!canDrag)
        {
            outline.SetActive(false);
            canvasGroup.alpha = 1f;
            hasParent = true;
        }

        canvasGroup.blocksRaycasts = canDrag;
    }

    public void moveSlot(int newSlot)
    {
        transform.SetParent(hotbar.transform.GetChild(newSlot-1).transform);
        rectTransform.anchoredPosition = Vector2.zero;
        updateSlot(transform.parent.gameObject, newSlot);
    }

    private IEnumerator checkForUnequip()
    {
        yield return null;
        if(!hasParent)
        {
            // unequip
            WorldspaceCanvas.instance.deActivateCard();
            InventoryManager.instance.unequip(originalSlot - 1, false, true);
            PlayerController.instance.switchEquipped(InventoryManager.instance.equippedItemIndex, false, false, false);
            moveSlot(originalSlot);
            InventoryMenu.instance.updateInventory();
            PlayerController.instance.audioManager.PlaySound("Equip Unequip");
        }
    }
}

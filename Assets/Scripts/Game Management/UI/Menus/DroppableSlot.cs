using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DroppableSlot : MonoBehaviour, IDropHandler
{
    // Derived from sibling index rather than hand-authored per prefab object: the slot's
    // position in the hotbar IS its inventory index (+1), and slot objects never reparent.
    public int slotNumber { get { return transform.GetSiblingIndex() + 1; } }

    public bool isSlotted = false;
    public void OnDrop(PointerEventData eventData)
    {
        Draggable draggable = eventData.pointerDrag.GetComponent<Draggable>();

        if (eventData.pointerDrag != null)
        {
            // Validate placement BEFORE mutating any state. Setting hasParent = true here
            // would prevent OnEndDrag from doing its own cleanup, so we must do an explicit
            // snap-back ourselves on rejection.
            int destIndex = slotNumber - 1;
            int srcIndex = draggable.originalSlot - 1;
            InventoryItem srcItem = InventoryManager.instance.getEquipped(srcIndex);
            InventoryItem destItem = InventoryManager.instance.getEquipped(destIndex);

            bool placementValid =
                draggable.originalSlot == slotNumber
                || (isSlotted
                    ? InventoryManager.instance.canPlaceInSlot(destIndex, srcItem)
                      && InventoryManager.instance.canPlaceInSlot(srcIndex, destItem)
                    : InventoryManager.instance.canPlaceInSlot(destIndex, srcItem));

            if (!placementValid)
            {
                // snap dragged item back to its original slot — keep inventory data untouched
                GameObject originalSlotObj = draggable.getSlot();
                eventData.pointerDrag.transform.SetParent(originalSlotObj.transform);
                eventData.pointerDrag.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                originalSlotObj.GetComponent<DroppableSlot>().isSlotted = true;
                draggable.hasParent = true;
                return;
            }

            draggable.hasParent = true;
            if (isSlotted && draggable.originalSlot != slotNumber)
            {
                InventoryManager.instance.swapItemSlots(eventData.pointerDrag.GetComponent<Draggable>().originalSlot - 1, slotNumber - 1);
                transform.GetChild(0).GetComponent<Draggable>().moveSlot(eventData.pointerDrag.GetComponent<Draggable>().originalSlot);
            }
            else if(!isSlotted && draggable.originalSlot != slotNumber)
            {
                InventoryManager.instance.equip(slotNumber-1, LootMaster.instance.createNewItemCopy(InventoryManager.instance.getEquipped(draggable.originalSlot-1)));
                InventoryManager.instance.unequip(draggable.originalSlot-1, false);

                Transform oldSlot = transform.GetChild(0);
                oldSlot.gameObject.SetActive(true);
                oldSlot.GetComponent<Draggable>().moveSlot(eventData.pointerDrag.GetComponent<Draggable>().originalSlot);
                oldSlot.gameObject.SetActive(false);
            }

            eventData.pointerDrag.transform.SetParent(this.transform);
            eventData.pointerDrag.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            draggable.updateSlot(this.gameObject, slotNumber);
            isSlotted = true;
            // need to handle calling to inventory manager to make changes

            // InventoryMenu.instance.updateSlideArrays(); // childs have changed, need to update their references
            InventoryMenu.instance.updateInventory();
            PlayerController.instance.switchEquipped(InventoryManager.instance.equippedItemIndex, false, false, false);
        }
    }
}

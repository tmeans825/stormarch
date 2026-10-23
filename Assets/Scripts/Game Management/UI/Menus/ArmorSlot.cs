using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ArmorSlot : MonoBehaviour, IPointerEnterHandler
{
    public int index;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (InventoryMenu.instance.activeSlot != this.gameObject)
        {
            if (GetComponent<Image>().enabled)
                WorldspaceCanvas.instance.showArmorCard(InventoryManager.instance.getEquippedArmor(index), null, true);
            AudioMaster.instance.PlaySound("UI Hotbar Ping");
            InventoryMenu.instance.activeSlot = this.gameObject;
        }
    }
}


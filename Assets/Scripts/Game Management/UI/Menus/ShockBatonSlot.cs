using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShockBatonSlot : MonoBehaviour, IPointerEnterHandler
{    public void OnPointerEnter(PointerEventData eventData)
    {
        if (InventoryMenu.instance.activeSlot != this.gameObject)
        {
            if (GetComponent<Image>().enabled)
                WorldspaceCanvas.instance.showWeaponCard(PlayerController.instance.returnShockBatonGun(), null, true);
            AudioMaster.instance.PlaySound("UI Hotbar Ping");
            InventoryMenu.instance.activeSlot = this.gameObject;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CraftButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public InventoryItem.itemType itemType;
    public Healable.healableType healableType;
    public Throwable.throwableType throwableType;

    public int costCloth = 0;
    public int costChem = 0;
    public int costScrap = 0;

    public bool canCraftAnywhere = false;

    private GameObject slideOutline;

    [System.NonSerialized]
    public InventoryItem item;

    void Awake()
    {
        slideOutline = transform.GetChild(1).gameObject;
    }

    void Start()
    {
        item = new InventoryItem();
        item.iType = itemType;

        if(itemType == InventoryItem.itemType.Healable)
        {
            Healable healable = new Healable();
            healable.hType = healableType;
            item = healable;
        }    
        else if(itemType == InventoryItem.itemType.Throwable)
        {
            Throwable throwable = new Throwable();
            throwable.tType = throwableType;
            item = throwable;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        AudioMaster.instance.PlaySound("UI Hotbar Ping");
        slideOutline.SetActive(true);

        gameObject.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
        CraftMenuBehavior.instance.refreshContextMenu(item, costCloth, costChem, costScrap);

        CraftMenuBehavior.instance.currentselection = this;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        slideOutline.SetActive(false);
        gameObject.transform.localScale = new Vector3(1f, 1f, 1f);
    }

    public virtual void OnPointerClick(PointerEventData eventData)
    {
        // try build
        CraftMenuBehavior.instance.clickedButton(item, costCloth, costChem, costScrap);
    }

    private void OnDisable()
    {
        gameObject.transform.localScale = new Vector3(1f, 1f, 1f);
        slideOutline.SetActive(false);
        gameObject.SetActive(false);
    }
}

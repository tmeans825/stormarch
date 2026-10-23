using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UpgradeButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public bool isDisabled = true;
    public Image[] relatedConnectors;
    public UpgradeButton[] dependencies; 

    public string UIKeyName;
    public bool isPurchased = false;
    public int componentCost;
    public int techCost;

    public GameObject[] upgradeArchitectObjects;

    private Image icon;
    private Color originalColor;

    void Start()
    {
        if (icon == null)
            icon = GetComponent<Image>();

        originalColor = icon.color;

        // will need to load this in
        if (isDisabled)
            lockButton();
        else if (!isPurchased)
            unlockButton();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        AudioMaster.instance.PlaySound("UI Hotbar Ping");

        HomesteadUIBehavior.instance.selectUpgradeButton(this, false);

        if (isPurchased)
            return;

        Color newColor = icon.color;
        newColor.a = 1f;
        icon.color = newColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HomesteadUIBehavior.instance.exitBuildingButton();

        if(transform.localScale.x != 1.2f)
            deselect();
    }

    public void deselect()
    {
        transform.localScale = new Vector3(1f, 1f);

        if (isPurchased)
            return;

        Color newColor = icon.color;
        newColor.a = 0.5f;
        icon.color = newColor;
    }

    public virtual void OnPointerClick(PointerEventData eventData)
    {
        if (isPurchased || isDisabled)
            return;

        AudioMaster.instance.PlaySound("Deep Select");
        transform.localScale = new Vector3(1.2f, 1.2f);
        HomesteadUIBehavior.instance.selectUpgradeButton(this, true);
    }

    private void lockButton()
    {
        Color newColor = Color.black;
        newColor.a = 0.5f;
        icon.color = newColor;

        foreach (Image image in relatedConnectors)
        {
            image.color = newColor;
        }

    }

    public void unlockButton()
    {
        if (icon == null) // this will only happen on load in; we'll have to instantiate some stuff
        {
            icon = GetComponent<Image>();
            originalColor = icon.color;
        }

        Color newColor = originalColor;
        newColor.a = 0.5f;
        icon.color = newColor;

        Color barColor = new Color(107f / 255f, 122f / 255f, 165f / 255f, 1f);

        foreach (Image image in relatedConnectors)
        {
            image.color = barColor;
        }

        isDisabled = false;
    }

    public void buyButton()
    {
        icon.color = Color.white;
        isPurchased = true;

        foreach (GameObject obj in upgradeArchitectObjects)
            obj.SetActive(true);

        deselect();
    }

   
}

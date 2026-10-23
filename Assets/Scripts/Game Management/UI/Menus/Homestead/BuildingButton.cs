using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BuildingButton : GenericButton
{
    public string UIKeyName;
    public GameObject associatedBuilding;
    public bool isPurchased = false;
    public int componentCost;
    public int techCost;

    [System.NonSerialized]
    public int xLoc;
    [System.NonSerialized]
    public int yLoc;

    // will need to save these later
    private Vector3 worldLocation;

    private GameObject skillTree;

    public override void Start()
    {
        base.Start();

        skillTree = transform.GetChild(1).gameObject;
        skillTree.SetActive(false);
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        base.OnPointerEnter(eventData);

        if (!isPurchased)
        {
            // add name to show, 'transient'
            HomesteadUIBehavior.instance.selectBuildingButton(this, false);
        }
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);

        // add name to show, 'permanent'
        HomesteadUIBehavior.instance.selectBuildingButton(this, true);

        if(isPurchased)
        {
            skillTree.SetActive(true);
            HomesteadUIBehavior.instance.expandSkillTree(this.gameObject);
        }
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData);

        HomesteadUIBehavior.instance.exitBuildingButton();
    }

    public void signalPurchase()
    {
        signalPurchase(true);
    }

    public void signalPurchase(bool turnOnSkillTree)
    {
        worldLocation = transform.position;

        if(turnOnSkillTree)
            skillTree.SetActive(true);
    }

    public void closeSkillTree()
    {
        skillTree.SetActive(false);
        transform.position = worldLocation;
    }

}

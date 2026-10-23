using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PlotButton : GenericButton
{
    [Range(1, 3)]
    public int tier;

    // Start is called before the first frame update
    public override void Start()
    {
        base.Start();

        switch(tier)
        {
            case 1:
                icon.sprite = HomesteadUIBehavior.instance.tier1icon;
                break;
            case 2:
                icon.sprite = HomesteadUIBehavior.instance.tier2icon;
                break;
            case 3:
                icon.sprite = HomesteadUIBehavior.instance.tier3icon;
                break;
            default:
                break;
        }

        if(HomesteadUIBehavior.instance.activeTier < tier) // to do: this may have to happen after loading is complete.
        {
            isDisabled = true;
            Color disabledColor = Color.black;
            disabledColor.a = 0.5f;
            icon.color = disabledColor;
            //frame.sprite = disabledFrame;
            frame.enabled = false;
        }
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        HomesteadUIBehavior.instance.selectPlotButton(this.gameObject);
        base.OnPointerClick(eventData);

        // to do: pop up building shit.
    }
}

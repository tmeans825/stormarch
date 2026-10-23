using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GenericButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public Sprite normalFrame;
    public Sprite highlightedFrame;
    public Sprite selectedFrame;
    public Sprite disabledFrame;

    protected Image icon;
    protected Image frame;

    protected bool isSelected = false;
    protected bool isDisabled = false;

    // Start is called before the first frame update
    public virtual void Start()
    {
        frame = GetComponent<Image>();
        icon = transform.GetChild(0).GetComponent<Image>();

        GameEvents.current.onMenuClosed += menuClosed;
    }

    public void unlock()
    {
        isDisabled = false;
        Color enabledColor = Color.white;
        enabledColor.a = 1f;
        icon.color = enabledColor;
        frame.sprite = normalFrame;
        frame.enabled = true;
    }

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        if (isSelected || isDisabled)
            return;

        frame.sprite = highlightedFrame;
        AudioMaster.instance.PlaySound("UI Hotbar Ping");
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        if (isSelected || isDisabled)
            return;

        frame.sprite = normalFrame;       
    }

    public virtual void OnPointerClick(PointerEventData eventData)
    {
        if (isSelected || isDisabled)
            return;

        isSelected = true;
        AudioMaster.instance.PlaySound("Deep Select");
        frame.sprite = selectedFrame;

        
    }

    public void deselect()
    {
        if (!isSelected || isDisabled)
            return;

        isSelected = false;
        frame.sprite = normalFrame;
    }

    protected void menuClosed()
    {
        // make sure highlighted stops being highlighted
        if (isSelected || isDisabled)
            return;

        frame.sprite = normalFrame;
    }

    public void OnDestroy()
    {
        GameEvents.current.onMenuClosed -= menuClosed;
    }
}

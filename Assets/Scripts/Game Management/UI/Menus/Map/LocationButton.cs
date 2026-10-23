using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class LocationButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public string locationName;
    public Sprite normalFrame;
    public Sprite highlightedFrame;
    public Sprite selectedFrame;
    public Sprite disabledFrame;

    private Image currentFrame;
    private MapMenuBehavior mapBehavior;
    private bool isSelected = false;

    private bool currentLocation = false;
    
    void Start()
    {
        GameEvents.current.onMenuClosed += menuClosed;

        if (currentFrame == null)
            currentFrame = GetComponent<Image>();
    }

    void OnDestroy()
    {
        GameEvents.current.onMenuClosed -= menuClosed;
    }

    // Called by MapMenuBehavior each time the map opens. May run before Start on a just-revealed button.
    public void refreshState()
    {
        if (currentFrame == null)
            currentFrame = GetComponent<Image>();

        isSelected = false;
        // Compared by location, not scene, so another sub-location of this scene stays travelable.
        currentLocation = SaveManager.instance.getCurrentLocationName() == locationName;
        currentFrame.sprite = currentLocation ? disabledFrame : normalFrame;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentLocation || isSelected)
            return;

        MapMenuBehavior.instance.buttonMouseover(locationName);
        currentFrame.sprite = highlightedFrame;
        AudioMaster.instance.PlaySound("UI Hotbar Ping");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isSelected && !currentLocation)
        {
            currentFrame.sprite = normalFrame;
            MapMenuBehavior.instance.buttonMouseExit();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isSelected || currentLocation)
            return;

        isSelected = true;
        MapMenuBehavior.instance.newSelection(this);
        AudioMaster.instance.PlaySound("Menu Tab");
        currentFrame.sprite = selectedFrame;
    }

    public void deselect()
    {
        if (!isSelected || currentLocation)
            return;

        isSelected = false;
        currentFrame.sprite = normalFrame;
    }

    private void menuClosed()
    {
        // make sure highlighted stops being highlighted
        if (!isSelected && !currentLocation)
            currentFrame.sprite = normalFrame;
    }
}


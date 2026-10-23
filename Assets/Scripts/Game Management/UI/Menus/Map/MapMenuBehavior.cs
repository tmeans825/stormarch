using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using PixelCrushers;

public class MapMenuBehavior : MonoBehaviour
{
    [System.NonSerialized]
    public static MapMenuBehavior instance;

    public LocalizeUI locationName;
    public GameObject travelButton;

    private LocationButton currentlySelectedButton;
    private string selectedLocationName;
    private LocationButton[] buttons;

    private void Awake()
    {
        buttons = GetComponentsInChildren<LocationButton>(true);
    }

    private void Start()
    {
        if (instance == null && this != instance)
            instance = this;
        else
            Destroy(this);

        startFresh();
    }
   
    public void newSelection(LocationButton newButton)
    {
        if (currentlySelectedButton != null)
        {
            currentlySelectedButton.deselect();
        }

        currentlySelectedButton = newButton;
        selectedLocationName = newButton.locationName;
        locationName.fieldName = selectedLocationName;
        locationName.UpdateText();
        travelButton.SetActive(true);
    }

    public void travel()
    {
        AudioMaster.instance.PlaySound("Deep Select");
        LevelLoader.instance.changeScene(selectedLocationName);
    }

    public void buttonMouseover(string mouseOverText)
    {
        locationName.gameObject.SetActive(true);
        locationName.fieldName = mouseOverText;
        locationName.UpdateText();
    }

    public void buttonMouseExit()
    {
        if (selectedLocationName != null)
        {
            locationName.fieldName = selectedLocationName;
            locationName.UpdateText();
        }
        else
            locationName.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        startFresh();
        MouseManager.instance.menusOff();
    }

    private void OnEnable()
    {
        MouseManager.instance.menusOn();
        refreshButtons();
    }

    // Visibility and current-scene state are re-derived every open, so a location discovered
    // mid-scene (including the one the player stands in) shows correctly the next time the map opens.
    private void refreshButtons()
    {
        LocationDiscoveryManager discovery = LocationDiscoveryManager.instance;
        bool ready = discovery != null && SaveManager.instance != null; // fail closed before managers exist

        foreach (LocationButton button in buttons)
        {
            bool visible = ready
                && discovery.isDiscovered(button.locationName)
                && SaveManager.isTravelableLocation(button.locationName);

            button.gameObject.SetActive(visible);
            if (visible)
                button.refreshState();
        }
    }

    private void startFresh()
    {
        travelButton.SetActive(false);
        if (currentlySelectedButton != null)
        {
            currentlySelectedButton.deselect();
            currentlySelectedButton = null;
        }

        selectedLocationName = null;
        locationName.gameObject.SetActive(false);
    }
}

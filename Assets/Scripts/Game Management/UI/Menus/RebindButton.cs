using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RebindButton : MonoBehaviour
{
    public KeyCode defaultKey;
    private string controlToRebindName;
    private TextMeshProUGUI buttonText;
    private SettingsMenu settingsMenu;
    // Start is called before the first frame update
    void Awake()
    {
        controlToRebindName = transform.parent.name;
        buttonText = gameObject.GetComponentInChildren<TextMeshProUGUI>();
        settingsMenu = transform.parent.parent.parent.gameObject.GetComponent<SettingsMenu>();
    }

    public void RebindPressed()
    {
        settingsMenu.rebindControl(controlToRebindName, buttonText);
    }

    private void OnEnable()
    {   
        KeyCode keyCode;
        if(SettingsMenu.tryGetDisplayKeyBinding(controlToRebindName, out keyCode))
            buttonText.text = keyCode.ToString();
    }

    // TO DO: Initialize button text? based on save settings
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Menus : MonoBehaviour
{
    public GameObject invPanel;
    private GameObject activePanel;
    public TextMeshProUGUI menuPopupText;

    [System.NonSerialized]
    public static Menus instance;
    [System.NonSerialized]
    public bool isRebinding;
    [System.NonSerialized]
    public bool isOn;

    private void Awake()
    {
        if (instance == null && this != instance)
            instance = this;
        else
            Destroy(this);

        if (activePanel == null)
            activePanel = invPanel;        
    }

    public IEnumerator Start()
    {
        if (activePanel.tag == "Inventory Panel")
            GetComponentInChildren<InventoryMenu>().close();

        yield return 0;
        gameObject.SetActive(false);
    }

    public void togglePanel(GameObject panel)
    {
        if (activePanel != panel)
        {
            if(activePanel.tag == "Inventory Panel")
                GetComponentInChildren<InventoryMenu>().close();

            activePanel.SetActive(false);
            activePanel = panel;
            activePanel.SetActive(true);
            AudioMaster.instance.PlaySound("Menu Tab");
        }
    }

    public void closeMenu()
    {
        if (activePanel.tag == "Inventory Panel")
            GetComponentInChildren<InventoryMenu>().close();
        this.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        isOn = true;
        MouseManager.instance.menusOn();
    }

    private void OnDisable()
    {
        isOn = false;
        MouseManager.instance.menusOff();
    }

}

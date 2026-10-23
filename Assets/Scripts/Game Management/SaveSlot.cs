using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using PixelCrushers;

public class SaveSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public string saveFileName;

    private GameObject selectedOutline;
    private TextMeshProUGUI playerName;
    private TextMeshProUGUI location;
    private TextMeshProUGUI act;
    private GameObject playerRepresentation;
    private RectTransform rectTransform;
    private Vector3 originalPos;
    private Vector3 focusedPos = Vector3.zero;
    private SaveSlotMenu saveSlotMenu;
    bool isFocused = false;
    bool hasData = false;

    // Start is called before the first frame update
    void Awake()
    {
        selectedOutline = transform.GetChild(1).gameObject;
        playerName = transform.GetChild(2).GetComponent<TextMeshProUGUI>();
        location = transform.GetChild(3).GetComponent<TextMeshProUGUI>();
        act = transform.GetChild(4).GetComponent<TextMeshProUGUI>();
        playerRepresentation = transform.GetChild(5).gameObject;
        rectTransform = GetComponent<RectTransform>();

        

        saveSlotMenu = GetComponentInParent<SaveSlotMenu>();

        if (!ES3.FileExists(saveFileName + ".es3"))
        {
            initializeEmpty();
        }
        else
        {
            initializeSlot();
        }
    }

    void Start()
    {
        originalPos = rectTransform.position;
    }

    public void reinitialize()
    {
        if (!ES3.FileExists(saveFileName + ".es3"))
        {
            initializeEmpty();
        }
        else
        {
            initializeSlot();
        }

        if (isFocused)
        {
            if (!hasData)
                saveSlotMenu.deleteButton.SetActive(false);
            else
                saveSlotMenu.deleteButton.SetActive(true);
        }
    }

    public void click()
    {
        rectTransform.localPosition = focusedPos;
        rectTransform.localScale = new Vector3(1.25f, 1.25f);
        isFocused = true;
        saveSlotMenu.activeSaveName = saveFileName;
        AudioMaster.instance.PlaySound("Deep Select");

        if (!hasData)
            saveSlotMenu.deleteButton.SetActive(false);
        else
            saveSlotMenu.deleteButton.SetActive(true);
    }

    public void back()
    {
        gameObject.SetActive(true);
        selectedOutline.SetActive(false);
        rectTransform.position = originalPos;
        rectTransform.localScale = new Vector3(1f, 1f);
        isFocused = false;
    }


    private void initializeEmpty()
    {
        hasData = false;
        playerName.text = "EMPTY";
        location.gameObject.SetActive(false);
        act.gameObject.SetActive(false);
        playerRepresentation.SetActive(false);
    }
    private void initializeSlot()
    {
        string fileToLoad = SaveManager.instance.returnSlotName(saveFileName); // get checkpoint vs base slot

        hasData = true;
        location.gameObject.SetActive(true);
        act.gameObject.SetActive(true);
        playerRepresentation.SetActive(true); 
        playerName.text = "JADEN HAYES"; // TO DO: LOAD NAME WHEN ABLE TO PUNCH IN
        location.gameObject.GetComponent<LocalizeUI>().fieldName = ES3.Load<string>("WorldLocation", fileToLoad + ".es3");
        location.gameObject.GetComponent<LocalizeUI>().UpdateText();
        act.gameObject.GetComponent<LocalizeUI>().fieldName = "ACT " + ES3.Load<int>("Act", fileToLoad + ".es3");
        act.gameObject.GetComponent<LocalizeUI>().UpdateText();


        // Player Representation
        playerRepresentation.transform.GetChild(2).GetComponent<Image>().sprite = ES3.Load<Sprite>("ModelSkin", fileToLoad + ".es3");
        playerRepresentation.transform.GetChild(3).GetComponent<Image>().sprite = ES3.Load<Sprite>("ModelHair", fileToLoad + ".es3");

        if (playerRepresentation.transform.GetChild(3).GetComponent<Image>().sprite == null) // handle city hood
            playerRepresentation.transform.GetChild(3).GetComponent<Image>().sprite = saveSlotMenu.defaultHood;

        // Must do chest and legs retarded way b/c at save time you only have overloaded references, not actual sprites
        string legSpritesheetName = ES3.Load<string>("ModelLegs", fileToLoad + ".es3");
        var legSprites = Resources.LoadAll<Sprite>("Legs/" + legSpritesheetName);
        playerRepresentation.transform.GetChild(0).GetComponent<Image>().sprite = legSprites[15];

        string chestSpritesheetName = ES3.Load<string>("ModelChest", fileToLoad + ".es3");
        var chestSprites = Resources.LoadAll<Sprite>("Chests/" + chestSpritesheetName);
        playerRepresentation.transform.GetChild(1).GetComponent<Image>().sprite = chestSprites[15];
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isFocused)
            return;

        AudioMaster.instance.PlaySound("UI Hotbar Ping");
        selectedOutline.SetActive(true);
        rectTransform.localScale = new Vector3(1.05f, 1.05f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isFocused)
            return;

        selectedOutline.SetActive(false);
        rectTransform.localScale = new Vector3(1f, 1f);
    }
}

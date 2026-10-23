using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using PixelCrushers;
using PixelCrushers.DialogueSystem;

public class HUDUIBehavior : MonoBehaviour
{
    public Sprite[] ammoTypeArray;

    public GameObject popupTextObj;
    public GameObject ammoMagParent;

    [System.NonSerialized]
    public static HUDUIBehavior instance;

    private TextMeshProUGUI ammoCount;
    private Image ammoType;

    private int previousAmmoType = 0;
    private int activeBulletInMagIndex = 0;
    private int currentMagCap = 0;
    private Gun currGun;
    private bool isHUDActive = true;

    private GameObject[] magBulletArray;

    private TextMeshProUGUI popupText;

    [System.NonSerialized]
    public TextTable textTable;

    void Awake()
    {
        if (instance == null && this != instance)
            instance = this;
        else
            Destroy(this);

        ammoCount = transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        ammoType = transform.GetChild(1).GetComponent<Image>();

        popupText = popupTextObj.GetComponent<TextMeshProUGUI>();

        magBulletArray = new GameObject[ammoMagParent.transform.childCount - 2];

        for(int i = 1; i < magBulletArray.Length+1; i++) // loop over all but first and last child to grab references
        {
            magBulletArray[i-1] = ammoMagParent.transform.GetChild(i).gameObject; 
        }
    }

    private void Start()
    {
        textTable = DialogueManager.instance.gameObject.GetComponent<UILocalizationManager>().textTable;
        if (textTable == null)
            Debug.LogError("Couldn't find text table. UI elements will now be fucked up");
    }

    public void refreshHUD(int count, int type)
    {
        if (!isHUDActive)
        {
            ammoType.gameObject.SetActive(true);
            ammoMagParent.gameObject.SetActive(true);
            isHUDActive = true;
        }

        if(type == 1) // pistol - do not show
        {
            ammoCount.gameObject.SetActive(false);
        }
        else        
        {
            ammoCount.gameObject.SetActive(true);
        }

        string prefix = "";
        if (count < 100 && count >= 10)
            prefix = "0";
        else if (count < 10)
            prefix = "00";
        ammoCount.text = prefix+count.ToString();

        if(previousAmmoType != type)
        {
            ammoType.sprite = ammoTypeArray[type];
            previousAmmoType = type;
        }

        if (count <= 10)
        {
            //float percent = ((1f - ((float)count / 15f)) / 2f) + 0.5f;
            ammoCount.alpha = 1.0f;
            ammoType.CrossFadeAlpha(1.0f, 0.01f, true);
        }
        else
        {
            ammoCount.alpha = 0.5f;
            ammoType.CrossFadeAlpha(0.5f, 0.01f, true);
        }

        Gun currGunTry = InventoryManager.instance.getEquipped() as Gun;

        ammoMagParent.GetComponent<Animator>().Play("Parent Fadeout", -1, 0f);

        // MAG STUFF
        if (currGun == null || currGun != currGunTry)
        {
            currGun = currGunTry;
            // refresh total mag size
            for (int i = 0; i < magBulletArray.Length; i++)
            {
                if (i < currGun.magSize)
                {
                    magBulletArray[i].SetActive(true);
                    if (i < currGun.bulletsInMag)
                        magBulletArray[i].transform.GetChild(0).GetComponent<Animator>().Play("Ammo Fill");
                    else
                        magBulletArray[i].transform.GetChild(0).GetComponent<Animator>().Play("Ammo Idle");
                }
                else
                    magBulletArray[i].SetActive(false);           
            }

            currentMagCap = currGun.magSize;        
        }
        else
        {
            for (int i = 0; i < currentMagCap; i++)
            {
                if (i < currGun.bulletsInMag)
                    magBulletArray[i].transform.GetChild(0).GetComponent<Animator>().Play("Ammo Fill");
            }
        }

        activeBulletInMagIndex = currGun.bulletsInMag;
    }

    public void useBulletAnim()
    {
        magBulletArray[activeBulletInMagIndex].transform.GetChild(0).GetComponent<Animator>().Play("Ammo Empty");
        activeBulletInMagIndex--;
    }

    public void flashOutOfAmmo()
    {
        ammoMagParent.GetComponent<Animator>().Play("Parent Fadeout", -1, 0f);
        ammoMagParent.transform.GetChild(0).GetChild(0).GetComponent<Animator>().Play("Stopper Flash");
        ammoMagParent.transform.GetChild(ammoMagParent.transform.childCount-1).GetChild(0).GetComponent<Animator>().Play("Stopper Flash");
    }

    public void hideHUB()
    {
        if (isHUDActive)
        {
            ammoCount.gameObject.SetActive(false);
            ammoType.gameObject.SetActive(false);
            ammoMagParent.gameObject.SetActive(false);
            isHUDActive = false;
        }
    }

    public void triggerPopup(string fieldText, bool charName)
    {
        if (!charName)
            popupText.text = fieldText;
        else
            popupText.text = ToTitleCase(textTable.GetFieldText(fieldText));

        popupTextObj.SetActive(true);
    }

    public void closePopup()
    {
        popupTextObj.SetActive(false);
    }

    string ToTitleCase(string stringToConvert)
    {
        string firstChar = stringToConvert[0].ToString();
        return (stringToConvert.Length > 0 ? firstChar.ToUpper() + stringToConvert.Substring(1) : stringToConvert);

    }

}

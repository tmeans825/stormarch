using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterAppearanceReferences : MonoBehaviour
{
    [Header("Default")]
    public string[] generalMaleHair;
    public string[] generalFemaleHair;
    public string[] generalSkin;

    [Header("Normal Citizen")]
    public string[] normalCitizenMaleChest;
    public string[] normalCitizenFemaleChest;
    public string[] normalCitizenLegs;

    [Header("Poor Citizen")]
    public string[] serfMaleChest;
    public string[] serfFemaleChest;
    public string[] serfMaleLegs;
    public string[] serfFemaleLegs;
    public string[] serfSkin;

    [Header("Scholar")]
    public string[] scholarMaleChest;
    public string[] scholarFemaleChest;
    public string[] scholarMaleLegs;
    public string[] scholarFemaleLegs;

    [Header("Wolf")]
    public string[] wolfChests;
    public string[] wolfLegs;
    public string[] wolfHair;

    [Header("Guardian")]
    public string[] guardianChests;
    public string[] guardianLegs;
    public string[] guardianHair;

    [Header("Sentinel")]
    public string[] sentinelChest;

    [System.NonSerialized]
    public static CharacterAppearanceReferences instance;

    public void Awake()
    {
        if (instance == null && this != instance)
            instance = this;
        else
        {
            //UnityEngine.Debug.Log("Destroying playerController duplicate");
            Destroy(gameObject);
        }
    }

    public enum TemplateLook
    {
        None,
        NormalCitizen,
        PoorCitizen,
        Wolf,
        Guardian,
        Sentinel,
        Scholar
    }

    public void changeCharacterAppearance(Character character, TemplateLook template)
    {
        if (template == TemplateLook.None)
            return;

        int randGender = Random.Range(0, 2);
        bool isMale = true;
        if (randGender == 0)
            isMale = false;

        string[] selectedHair;
        if (isMale)
            selectedHair = generalMaleHair;
        else
            selectedHair = generalFemaleHair;
        string[] selectedSkin = generalSkin;
        string[] selectedChests;
        string[] selectedLegs;

        switch(template)
        {
            case TemplateLook.NormalCitizen:
                if (isMale)
                    selectedChests = normalCitizenMaleChest;
                else
                    selectedChests = normalCitizenFemaleChest;
                selectedLegs = normalCitizenLegs;
                break;
            case TemplateLook.PoorCitizen:
                if (isMale)
                {
                    selectedChests = serfMaleChest;
                    selectedLegs = serfMaleLegs;
                }
                else
                {
                    selectedChests = serfFemaleChest;
                    selectedLegs = serfFemaleLegs;
                }
                selectedSkin = serfSkin;
                break;
            case TemplateLook.Wolf:
                selectedHair = wolfHair;
                selectedChests = wolfChests;
                selectedLegs = wolfLegs;
                break;
            case TemplateLook.Guardian:
                selectedHair = guardianHair;
                selectedChests = guardianChests;
                selectedLegs = guardianLegs;
                break;
            case TemplateLook.Sentinel:
                selectedChests = sentinelChest;
                selectedHair = new string[] { "Empty" };
                selectedLegs = new string[] { "Empty" };
                break;
            case TemplateLook.Scholar:
                if (isMale)
                {
                    selectedChests = scholarMaleChest;
                    selectedLegs = scholarMaleLegs;
                }
                else
                {
                    selectedChests = scholarFemaleChest;
                    selectedLegs = scholarFemaleLegs;
                }
                break;
            default:
                selectedChests = normalCitizenMaleChest;
                selectedLegs = normalCitizenLegs;
                break;
        }

        character.chestSpriteSheetName = selectRandomSpriteName(selectedChests);
        character.legsSpriteSheetName = selectRandomSpriteName(selectedLegs);
        // Also stored on the fields: Character.Start re-applies them, which would otherwise undo the pick
        if(selectedHair[0] != "Empty")
        {
            character.hairSpriteSheetName = selectRandomSpriteName(selectedHair);
            character.changeHairSprites(character.hairSpriteSheetName);
        }
        if (selectedSkin[0] != "Empty")
        {
            character.skinSpriteSheetName = selectRandomSpriteName(selectedSkin);
            character.changeSkinColor(character.skinSpriteSheetName);
        }
    }

    private string selectRandomSpriteName(string[] nameList)
    {
        int randomNum = Random.Range(0, nameList.Length);
        return nameList[randomNum];
    }
}

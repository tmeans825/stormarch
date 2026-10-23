using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FlagCustomizationUI : MonoBehaviour
{
    [System.NonSerialized]
    public static FlagCustomizationUI instance;

    public Image primaryFlag;
    public Image secondaryFlag;

    public Image uiPatternRepresentation;
    public Image uiColorOneRepresentation;
    public Image uiColorTwoRepresentation;

    private int designIndex;
    private int primaryColorIndex;
    private int secondaryColorIndex;

    private int designIndexMax;
    private int colorIndexMax;


    void Start()
    {
        if (instance == null && this != instance)
            instance = this;
        else
        {
            Destroy(gameObject);
        }

        designIndexMax = HomesteadUIBehavior.instance.secondarySprite.Length;
        colorIndexMax = HomesteadUIBehavior.instance.colorArray.Length;
      
        GameEvents.current.onPreviousSceneDataLoaded += previousSceneDataLoaded; // we will set inactive after loading in data   
    }

    private void previousSceneDataLoaded()
    {
        // TO DO: load in from playercontroller? if it exists. if it doesn't just quit the fuck out
        if(PlayerController.instance.flagDesignInt != -1)
        {
            designIndex = PlayerController.instance.flagDesignInt;
            primaryColorIndex = PlayerController.instance.flagPrimaryColorInt;
            secondaryColorIndex = PlayerController.instance.flagSecondaryColorInt;
            HomesteadUIBehavior.instance.initializeFlags();
        }
        else
        {
            designIndex = Random.Range(0, designIndexMax);
            primaryColorIndex = Random.Range(0, colorIndexMax);
            secondaryColorIndex = Random.Range(0, colorIndexMax);           
        }

        updateDesign(designIndex);
        updatePrimaryColor(primaryColorIndex);
        updateSecondaryColor(secondaryColorIndex);

        gameObject.SetActive(false);
    }

    void OnEnable()
    {
        StartCoroutine(waitToTurnOnCursor());
    }

    private void OnDisable()
    {
        MouseManager.instance.menusOff();
    }

    private IEnumerator waitToTurnOnCursor()
    {
        yield return new WaitForSeconds(0.1f);
        MouseManager.instance.menusOn();
    }


    public void changeDesign(int direction)
    {
        designIndex += direction;

        if (designIndex >= designIndexMax)
            designIndex = 0;
        else if (designIndex < 0)
            designIndex = designIndexMax - 1;

        updateDesign(designIndex);
    }

    public void changePrimaryColor(int direction)
    {
        primaryColorIndex += direction;

        if (primaryColorIndex >= colorIndexMax)
            primaryColorIndex = 0;
        else if (primaryColorIndex < 0)
            primaryColorIndex = colorIndexMax - 1;

        updatePrimaryColor(primaryColorIndex);
    }

    public void changeSecondaryColor(int direction)
    {
        secondaryColorIndex += direction;

        if (secondaryColorIndex >= colorIndexMax)
            secondaryColorIndex = 0;
        else if (secondaryColorIndex < 0)
            secondaryColorIndex = colorIndexMax - 1;

        updateSecondaryColor(secondaryColorIndex);
    }

    public void completeSelection()
    {
        // save selection
        PlayerController.instance.flagDesignInt = designIndex;
        PlayerController.instance.flagPrimaryColorInt = primaryColorIndex;
        PlayerController.instance.flagSecondaryColorInt = secondaryColorIndex;

        HomesteadUIBehavior.instance.initializeFlags();

        AudioMaster.instance.PlaySound("Menu Close");
        PlayerController.instance.isCustomizing = false;
        PlayerController.instance.freezeChar(false);
        this.gameObject.SetActive(false);
    }

    private void updateDesign(int index)
    {       
        secondaryFlag.sprite = HomesteadUIBehavior.instance.secondarySprite[index];
        uiPatternRepresentation.sprite = secondaryFlag.sprite;
    }

    private void updatePrimaryColor(int index)
    {
        primaryFlag.color = HomesteadUIBehavior.instance.colorArray[index];
        uiColorOneRepresentation.color = primaryFlag.color;
    }

    private void updateSecondaryColor(int index)
    {
        secondaryFlag.color = HomesteadUIBehavior.instance.colorArray[secondaryColorIndex];
        uiColorTwoRepresentation.color = secondaryFlag.color;
    }   
}

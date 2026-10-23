using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlagSingleBehavior : MonoBehaviour
{
    public SpriteRenderer primaryFlag;
    public SpriteRenderer secondaryFlag;

    public GameObject pole2;
    public GameObject pole3;

    private void Start()
    {
        updateFlag();
    }

    void OnEnable()
    {
        updateFlag();
    }

    private void updateFlag()
    {
        if (PlayerController.instance.flagDesignInt == -1)
            return; // prevent out of bounds problems

        secondaryFlag.sprite = HomesteadUIBehavior.instance.secondarySprite[PlayerController.instance.flagDesignInt];
        primaryFlag.color = HomesteadUIBehavior.instance.colorArray[PlayerController.instance.flagPrimaryColorInt];
        secondaryFlag.color = HomesteadUIBehavior.instance.colorArray[PlayerController.instance.flagSecondaryColorInt];        
    }

    public void updateTiers()
    {
        if (PlayerController.instance.homesteadTier == 2 && pole2 != null)
            pole2.SetActive(true);

        if (PlayerController.instance.homesteadTier == 3 && pole3 != null)
            pole3.SetActive(true);
    }
}

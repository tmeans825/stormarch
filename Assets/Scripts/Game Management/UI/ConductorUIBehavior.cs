using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConductorUIBehavior : MonoBehaviour
{
    [System.NonSerialized]
    public static ConductorUIBehavior instance;
   
    public GameObject conductorIndicatorObj;
    public GameObject pipMaster;

    public int maxCharges = 3;

    private GameObject[] pipArray;
    private Image conductorIndicator;
    private Image[] pipImgArray;

    private bool isOff = true;
    private int currentCharges = 0;
    private AudioManager am;

    // Start is called before the first frame update
    void Awake()
    {
        if (instance == null && this != instance)
            instance = this;
        else
            Destroy(this);

        conductorIndicator = conductorIndicatorObj.GetComponent<Image>();
        int childCount = pipMaster.transform.childCount;
        pipArray = new GameObject[childCount];
        pipImgArray = new Image[childCount];

        for(int i = 0; i < childCount; i++)
        {
            GameObject pip = pipMaster.transform.GetChild(i).gameObject;
            pipArray[i] = pip;
            pipImgArray[i] = pip.GetComponent<Image>();
        }

        conductorIndicatorObj.SetActive(false);
        pipMaster.SetActive(false);

        am = gameObject.GetComponent<AudioManager>();
    }

    public void addCharges(int c)
    {
        currentCharges += c;
        if (currentCharges > maxCharges)
            currentCharges = maxCharges;

        if(isOff)
        {
            conductorIndicatorObj.SetActive(true);
            pipMaster.SetActive(true);
        }

        updatePips();
    }

    public void useCharge()
    {
        if(currentCharges == 0)
        {
            Debug.LogWarning("Out of charges, cannot 'use' charge for UI update.");
            return;
        }

        currentCharges--;

        if(currentCharges == 0)
        {
            PlayerController.instance.audioManager.PlaySound("Break");
            conductorIndicatorObj.SetActive(false);
            pipMaster.SetActive(false);
        }
        else
        {
            updatePips();
        }

    }

    private void updatePips()
    {
        for(int i = 0; i < pipArray.Length; i++)
        {
            if(i < currentCharges)
                pipArray[i].SetActive(true);
            else
                pipArray[i].SetActive(false);
        }

        StopAllCoroutines();
        StartCoroutine(activateUIElements()); // make it all light up
    }

    private IEnumerator activateUIElements()
    {
        changeAlphaOfElements(1.0f, 0.01f);
        yield return new WaitForSeconds(1f);
        changeAlphaOfElements(0.3f, 1.5f);
    }

    private void changeAlphaOfElements(float alpha, float time)
    {
        conductorIndicator.CrossFadeAlpha(alpha, time, true);
        foreach (Image pip in pipImgArray)
        {
            if (pip.gameObject.activeSelf)
                pip.CrossFadeAlpha(alpha, time, true);
        }
    }

}

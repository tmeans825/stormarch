using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PixelCrushers.DialogueSystem;

public class BarkManager : MonoBehaviour
{
    public GameObject grenadeManager;
    public GameObject firebombManager;
    public GameObject healManager;

    private DialogueSystemTrigger grenadeBarkTrigger;
    private GameObject lastGrenadeBarkedFor = null;

    private DialogueSystemTrigger firebombBarkTrigger;
    private GameObject lastFireBarkedFor = null;

    private DialogueSystemTrigger healBarkTrigger;
    private GameObject lastClotBarkedFor = null;

    [System.NonSerialized]
    public static BarkManager instance;

    private void Awake()
    {
        grenadeBarkTrigger = grenadeManager.GetComponent<DialogueSystemTrigger>();
        healBarkTrigger = healManager.GetComponent<DialogueSystemTrigger>();
        firebombBarkTrigger = firebombManager.GetComponent<DialogueSystemTrigger>();
    }

    private void Start()
    {
        if (instance == null && this != instance)
            instance = this;
        else
        {
            Destroy(gameObject);
        }
    }

    public void grenadeBark(Transform t, GameObject grenadeObj)
    {
        startBark(grenadeBarkTrigger, grenadeManager, t, grenadeObj, ref lastGrenadeBarkedFor);
    }

    public void fireBark(Transform t, GameObject fireObj)
    {
        startBark(firebombBarkTrigger, firebombManager, t, fireObj, ref lastFireBarkedFor);
    }

    public void healBark(Transform t, GameObject healObj)
    {
        startBark(healBarkTrigger, healManager, t, healObj, ref lastClotBarkedFor);
    }

    private void startBark(DialogueSystemTrigger trigger, GameObject manager, Transform t, GameObject obj, ref GameObject lastObj)
    {
        if (obj == lastObj)
            return;

        manager.SetActive(false);
        trigger.barker = t;
        manager.SetActive(true);
        lastObj = obj;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoweredDoor : PuzzlePiece
{
    public int totalLights;
    public GameObject[] offLights;
    public GameObject[] onLights;
    public Animator doorAnim;
    public GameObject kiplingBlocker;

    private int powerLevel;
    private BoxCollider2D col;
    private AudioManager am;

    [ES3Serializable]
    private bool isCurrentlyOpen = false;

    // Start is called before the first frame update
    void Start()
    {
        GameEvents.current.onPreviousSceneDataLoaded += previousSceneDataLoaded;

        objectType = 2; // 2 - Door
        col = GetComponent<BoxCollider2D>();
        for (int i = 0; i < totalLights; i++)
        {
            offLights[i].SetActive(true);
        }
        am = GetComponent<AudioManager>();

        //if (isCurrentlyOpen)
            //openDoor();
    }

    private void previousSceneDataLoaded()
    {
        if(isCurrentlyOpen)
        {
            if(doorAnim == null)
                doorAnim = gameObject.GetComponentInChildren<Animator>();

            openDoor();
            for (int i = 0; i < totalLights; i++)
            {
                onLights[i].SetActive(true);
            }
        }
    }

    public void togglePowerEvent(bool on) // only for use in editor or events
    {
        togglePower(on);
    }

    public override bool togglePower(bool on)
    {
        if (on)
        {
            powerLevel++;
            if (powerLevel == totalLights)
                openDoor();
        }
        else
        {
            if (powerLevel == totalLights)
                closeDoor();
            powerLevel--;
        }

        for (int i = 0; i < totalLights; i++)
        {
            bool toggle = false;
            if (i < powerLevel)
                toggle = true;

            onLights[i].SetActive(toggle);
        }

        return true;
    }

    private void openDoor()
    {
        col.enabled = false;

        if (doorAnim == null)
            doorAnim = gameObject.GetComponentInChildren<Animator>();

        doorAnim.Play("Door_Open");
        AstarPath.active.UpdateGraphs(col.bounds);
        am.PlaySound("Open");
        isCurrentlyOpen = true;
    }

    private void closeDoor()
    {
        col.enabled = true;

        if (doorAnim == null)
            doorAnim = gameObject.GetComponentInChildren<Animator>();

        doorAnim.Play("Door_Close");
        AstarPath.active.UpdateGraphs(col.bounds);
        am.PlaySound("Close");
        isCurrentlyOpen = false;
    }

    public void addKiplingBlocker(bool add)
    {
        kiplingBlocker.SetActive(add);
        AstarPath.active.UpdateGraphs(kiplingBlocker.GetComponent<BoxCollider2D>().bounds);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoryBeatManager : MonoBehaviour
{
    public int beatIndex = 0;

    [System.NonSerialized]
    public static StoryBeatManager instance;

    public GameObject[] toToggleObjs;
    public int[] toggleBeatFinalIndex;


    private void Start()
    {
        if (instance == null && this != instance)
            instance = this;
        else
        {
            Destroy(gameObject);
        }

        GameEvents.current.onPreviousSceneDataLoaded += previousSceneDataLoaded;
    }

    public void startNewBeat()
    {
        int startIndex = 0;
        if (beatIndex != 0)
            startIndex = toggleBeatFinalIndex[beatIndex-1] + 1;

        for (int i = startIndex; i <= toggleBeatFinalIndex[beatIndex]; i++)
        {
            toggleObj(toToggleObjs[i]);
        }

        beatIndex++;
    }

    private void toggleObj(GameObject obj)
    {
        bool isActive = obj.activeSelf;
        obj.SetActive(!isActive);
    }

    private void previousSceneDataLoaded()
    {
        for (int i = 0; i < beatIndex; i++)
            startNewBeat();
    }    
    
}

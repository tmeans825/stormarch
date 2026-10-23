using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewRheaAct1BeatManager : sceneBeatManager

{
    [ES3NonSerializable]
    public GameObject storyOwen;

    protected override void Start()
    {
        base.Start();
    }

    protected override void initializeData()
    {
        beatMatrix = new int[1];

        // Beat line 0 - 
    }

    protected override void previousSceneDataLoaded()
    {
        base.previousSceneDataLoaded();
        PlayerController.instance.hasBaton = true;

        PlayerController.instance.changeHairSprites("hair_light_tier1_blue");  
        // TODO - soundtrack?
    }

    protected override void loadBeats()
    {
        
    }

    // EVENTS
    

    // Helpers
   
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OwensSceneHandler : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        GameEvents.current.onPreviousSceneDataLoaded += previousSceneDataLoaded;
    }

    // Update is called once per frame
    void previousSceneDataLoaded()
    {
        PlayerController.instance.hasKiplingAccess(true);
        PlayerController.instance.hasBaton = true;
        AudioMaster.instance.PlayMusic("Sanctuary", 0.2f);
    }
}

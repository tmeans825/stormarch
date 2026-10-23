using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuzzleLight : PuzzlePiece
{
    public bool isOnInitially = false;
    private GameObject lightsOn;
    private AudioManager am;
    // Start is called before the first frame update
    void Start()
    {
        objectType = 0; // light = 0, bridge = 1
        am = GetComponent<AudioManager>();
        lightsOn = transform.GetChild(1).gameObject;

        lightsOn.SetActive(isOnInitially);
    }

    public override bool togglePower(bool on)
    {
        lightsOn.SetActive(on);
        if(on)
            am.PlaySound("Switch");

        return true;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TogglePuzzleMaster : MonoBehaviour
{
    public bool[] requiredPattern;

    public ToggleLightPuzzleHandler[] toggleLights;

    public PuzzlePiece itemToPower;

    public ComputerNetwork optionalNetwork;

    public UnityEvent eventToTriggerOnSuccess;

    private bool puzzleSolved = false;

    private bool eventTriggered = false; // limit to calling events once

    private void Start()
    {
        for(int i = 0; i < toggleLights.Length; i++)
        {
            toggleLights[i].setIndicatorColor(requiredPattern[i]);
        }
    }

    public void lightToggled()
    {
        for(int i = 0; i < toggleLights.Length; i++)
        {
            if (toggleLights[i].isOn() != requiredPattern[i])
            {
                if(puzzleSolved)
                {
                    if (optionalNetwork != null)
                        optionalNetwork.reversePermanentOn();
                    itemToPower.togglePower(false);

                    puzzleSolved = false;
                }
                
                return;
            }
        }

        // if we got this far, the pattern is complete!
        if (optionalNetwork != null)
            optionalNetwork.overridePermanentOn();

        itemToPower.togglePower(true);

        puzzleSolved = true;

        if (!eventTriggered)
        {
            eventToTriggerOnSuccess.Invoke();
            eventTriggered = true;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmallAlertTimingHelper : MonoBehaviour
{
    public SmallAlertsBehavior alertBehavior;

    public void introStarted()
    {
        alertBehavior.currentState = 1;
    }

    public void introDone()
    {
        alertBehavior.currentState = 2;
    }

    public void idleDone()
    {
        alertBehavior.currentState = 3;
    }

    public void retreatDone()
    {
        alertBehavior.currentState = 0;
        alertBehavior.donePlaying();
    }
}

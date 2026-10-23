using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Purpose: serve as a wrapper for items that can be connected to generator.
// Has calls the generator uses that child scripts override.
public class PuzzlePiece : MonoBehaviour
{
    protected int objectType; // light = 0, bridge = 1, door = 2, elevator = 3

    public virtual bool togglePower(bool isOn)
    {
        return false;
    }

    public int returnType()
    {
        return objectType;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Debug_FigureOutDisableSource : MonoBehaviour
{
    void OnDisable()
    {
        Debug.LogWarning($"{name} was disabled!\nCall Stack:\n" + Environment.StackTrace, this);
    }
}

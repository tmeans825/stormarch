using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BurrowerAnimHelper : MonoBehaviour
{
    private BurrowerBehavior behavior;

    private void Start()
    {
        behavior = GetComponentInParent<BurrowerBehavior>();
    }

    public void emergeAnimDone()
    {
        behavior.finishEmerging();
    }

    public void retreatAnimDone()
    {
        behavior.finishRetreating();
    }
}

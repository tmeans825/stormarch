using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class spyDeathTrigger : MonoBehaviour
{
    public float delayForStartInvoke = 0f;
    public UnityEvent eventToTriggerAtDeathStart;
    public float delayForEndInvoke = 0f;
    public UnityEvent eventToTriggerAtDeathEnd;


    public void signalDeathStart()
    {
        StartCoroutine(triggerEvent(eventToTriggerAtDeathStart, delayForStartInvoke));
    }

    public void signalDeathEnd()
    {
        StartCoroutine(triggerEvent(eventToTriggerAtDeathEnd, delayForEndInvoke));
    }

    private IEnumerator triggerEvent(UnityEvent eventToTrigger, float delayForInvoke)
    {
        yield return new WaitForSeconds(delayForInvoke);
        eventToTrigger.Invoke();
    }
}

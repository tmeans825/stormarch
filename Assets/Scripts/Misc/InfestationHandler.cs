using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfestationHandler : MonoBehaviour
{
    private int lightZoneCount = 0;
    private int bugZoneCount = 0;
    private float infestationMeterValue = 0;
    private float infestationMeterSpeed = 50f;
    public bool isInfested { get; private set;}
    public event Action<float> onInfestationMeterChange;

    private Coroutine currentMeterCoroutine;


    void Update()
    {
        if(infestationMeterValue == 100f)
            return; // don't update if already at max, player is dead

        if(isInfested && infestationMeterValue < 100f)
        {
            updateInfestationMeter(infestationMeterValue + (infestationMeterSpeed * Time.deltaTime));
        }
        else if(infestationMeterValue > 0)
        {
            updateInfestationMeter(infestationMeterValue - (infestationMeterSpeed/2 * Time.deltaTime));
        }
    }

    public void enterBugZone()
    {
        bugZoneCount++;
        evaluateState();
    }

    public void exitBugZone()
    {
        bugZoneCount--;
        evaluateState();
    }


    public void enterLightZone()
    {
        lightZoneCount++;
        evaluateState();
    }

    public void exitLightZone()
    {  
        lightZoneCount--;
        evaluateState();
    }

    private void evaluateState()
    {
        bool shouldBeInfested = inBugZone() && !inLightZone();

        if (shouldBeInfested == isInfested) return; // no change, bail early
    
        isInfested = shouldBeInfested;
    
        if (isInfested)  OnInfestationStart();  // start coroutine
        else             OnInfestationEnd();    // stop coroutine
    }

    private void OnInfestationStart()
    {
        PlayerController.instance.slowAnimAndSpeed(0.5f);
    }

    private void OnInfestationEnd()
    {
        PlayerController.instance.resumeOriginalAnimAndSpeed();
    }

    private bool inBugZone() => bugZoneCount > 0;

    private bool inLightZone() => lightZoneCount > 0;

    public bool isPointInUnlitInfestation(Vector2 point, Vector2 boxSize)
    {
        Bounds probeBounds = new Bounds(point, boxSize);

        bool inBug = false;
        GameObject[] bugZones = GameObject.FindGameObjectsWithTag("BugZone");
        foreach (GameObject bz in bugZones)
        {
            Collider2D col = bz.GetComponent<Collider2D>();
            if (col != null && col.enabled && col.bounds.Intersects(probeBounds))
            {
                inBug = true;
                break;
            }
        }
        if (!inBug) return false;

        GameObject[] lights = GameObject.FindGameObjectsWithTag("Light");
        foreach (GameObject lt in lights)
        {
            Collider2D col = lt.GetComponent<Collider2D>();
            if (col != null && col.enabled && col.bounds.Intersects(probeBounds))
                return false;
        }
        return true;
    }

    void OnTriggerEnter2D(Collider2D other) 
    {
        if (other.CompareTag("Light")) enterLightZone();
            else if (other.CompareTag("BugZone")) enterBugZone();
    }

    void OnTriggerExit2D(Collider2D other) 
    {
        if (other.CompareTag("Light")) exitLightZone();
            else if (other.CompareTag("BugZone")) exitBugZone();
    }
    
    private void updateInfestationMeter(float amount)
    {
        if(infestationMeterValue == amount) // don't register a change if unnecessary
            return;

        infestationMeterValue = Mathf.Clamp(amount, 0f, 100f);
        onInfestationMeterChange?.Invoke(infestationMeterValue); // clamped, so listeners never see <0 or >100

        if(infestationMeterValue == 100f)
        {
            PlayerController.instance.damageCharacter(PlayerController.instance.maxHealth, PlayerController.instance.transform.position, true, true, false, false);
        }
    } 

}

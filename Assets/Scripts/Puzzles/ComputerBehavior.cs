using System.Collections;
using System.Collections.Generic;
using PixelCrushers.DialogueSystem;
using UnityEngine;

public class ComputerBehavior : MonoBehaviour
{
    public bool isToggleLightPuzzle;
    public PuzzlePiece[] toggleObjects;

    public float batteryLife = 4f;
    public GameObject connectedPuzzleObject;
    private GameObject screens;
    private GameObject powerMeter;
    private AudioManager am;
    private PuzzlePiece puzzlePiece;
    private bool permanentlyOn = false;
    private Vector3 powerMeterStart;
    private float powerMeterFinish;
    private ComputerNetwork computerNetwork;
    private bool thisOn = false;

    private float cachedTime = 0f;
    private Vector3 cachedMeterPosition;

    // Start is called before the first frame update
    IEnumerator Start()
    {
        am = GetComponent<AudioManager>();

        if(!isToggleLightPuzzle)
            puzzlePiece = connectedPuzzleObject.GetComponent<PuzzlePiece>();
        
        screens = transform.GetChild(1).gameObject;
        powerMeter = transform.GetChild(2).gameObject;
        powerMeterStart = powerMeter.transform.localPosition;
        powerMeterFinish = -0.14f;
        computerNetwork = GetComponentInParent<ComputerNetwork>();
        cachedMeterPosition = powerMeterStart;

        yield return 0;
        yield return 0;
        yield return 0;
     
        //Debug.Log("Computer permanently on should be true: " + permanentlyOn);

        if (permanentlyOn)
        {
            permanentlyOn = false;
            powerOn(true);
        }
    }

    public void powerOn()
    {
        powerOn(false);
    }

    public void powerOn(bool ignoreSFX)
    {
        if (permanentlyOn)
            return;

        if (!thisOn)
        {
            thisOn = true;

            if (!isToggleLightPuzzle)
            {
                puzzlePiece.togglePower(true);
                computerNetwork.signalOn();
            }
            else
            {
                for(int i = 0; i < toggleObjects.Length; i++)
                {
                    bool isLastLight = false;
                    if (i == toggleObjects.Length - 1)
                        isLastLight = true;

                    toggleObjects[i].togglePower(isLastLight);
                }
            }

            screens.SetActive(true);
            powerMeter.SetActive(true);
            if(!ignoreSFX)
                am.PlaySound("Power On");                      
        }

        StopAllCoroutines();
        StartCoroutine(drainPower(0.0f, powerMeterStart));

        // signalOn() above can synchronously solve the network and permanentlyPower() this
        // computer before we get here — registering then would leave a stale indicator
        if (!permanentlyOn)
            registerIndicator();
    }

    public void permanentlyPower()
    {
        if (!thisOn) // don't permanently power unless already one
            return;

        screens.SetActive(true);
        powerMeter.SetActive(true);
        permanentlyOn = true;
        StopAllCoroutines();
        unregisterIndicator();
    }

    public void reversePermanentPower()
    {
        if (!permanentlyOn) // nothing to reverse
            return;

        permanentlyOn = false;
        StartCoroutine(drainPower(cachedTime, cachedMeterPosition));
        registerIndicator();
    }

    public void triggerElectricity()
    {
        am.PlaySound("Electricity");
    }

    private void powerOff()
    {
        screens.SetActive(false);
        powerMeter.SetActive(false);

        if (!isToggleLightPuzzle)
        {
            puzzlePiece.togglePower(false);
            computerNetwork.signalOff();
        }
        else
        {
            for (int i = 0; i < toggleObjects.Length; i++)
            {
                bool isLastLight = false;
                if (i == toggleObjects.Length - 1)
                    isLastLight = true;

                toggleObjects[i].togglePower(isLastLight);
            }
        }

        am.PlaySound("Power Off");
        thisOn = false;
        unregisterIndicator();
    }

    void OnDisable()
    {
        unregisterIndicator();
    }

    public float getRemainingPowerFraction()
    {
        if (powerMeter == null)
            return 1f;

        float travel = powerMeterStart.y - powerMeterFinish;
        if (travel <= 0f)
            return 0f;

        return Mathf.Clamp01((powerMeter.transform.localPosition.y - powerMeterFinish) / travel);
    }

    private void registerIndicator()
    {
        if (ComputerIndicatorUIBehavior.instance != null)
            ComputerIndicatorUIBehavior.instance.registerComputer(this);
    }

    private void unregisterIndicator()
    {
        if (ComputerIndicatorUIBehavior.instance != null)
            ComputerIndicatorUIBehavior.instance.unregisterComputer(this);
    }

    private IEnumerator drainPower(float timePreviouslyPassed, Vector3 meterStart)
    {
        float effectiveBatteryLife = batteryLife - timePreviouslyPassed;
        float timePassed = 0f;
        float distanceToTravel = meterStart.y - powerMeterFinish;
        float increment = (0.1f / effectiveBatteryLife) * distanceToTravel;
        powerMeter.transform.localPosition = meterStart;

        while (timePassed < effectiveBatteryLife && !permanentlyOn)
        {
            cachedTime = timePassed;
            cachedMeterPosition = powerMeter.transform.localPosition;
            yield return new WaitForSeconds(0.1f);

            // Hold before advancing so the battery never drains (or powers off) mid-conversation.
            // Barks don't set isConversationActive, so they don't pause the drain.
            if (DialogueManager.isConversationActive)
                yield return new WaitWhile(() => DialogueManager.isConversationActive);

            timePassed += 0.1f;
            powerMeter.transform.localPosition -= new Vector3(0.0f, increment);
        }

        cachedTime = 0f;
        cachedMeterPosition = powerMeter.transform.localPosition;

        if (!permanentlyOn)
            powerOff();
    }
}

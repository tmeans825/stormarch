using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleLightPuzzleHandler : PuzzlePiece
{
    public GameObject offLight;
    public GameObject onLight;

    public GameObject greenIndicator;
    public GameObject redIndicator;

    private bool on = false;

    private TogglePuzzleMaster toggleMaster;

    // Start is called before the first frame update
    void Start()
    {
        if (offLight == null)
            offLight = transform.GetChild(0).gameObject;

        if (onLight == null)
            onLight = transform.GetChild(1).gameObject;

        if (greenIndicator == null)
            greenIndicator = transform.GetChild(2).gameObject;

        if (redIndicator == null)
            redIndicator = transform.GetChild(3).gameObject;

        toggleMaster = GetComponentInParent<TogglePuzzleMaster>();
    }

    public override bool togglePower(bool finalLight)
    {
        on = !on;

        onLight.SetActive(on);
        offLight.SetActive(!on);

        if(finalLight)
            toggleMaster.lightToggled();

        return true;
    }

    public void setIndicatorColor(bool setIndicatorGreen)
    {
        greenIndicator.SetActive(setIndicatorGreen);
        redIndicator.SetActive(!setIndicatorGreen);
    }

    public bool isOn()
    {
        return on;
    }
}

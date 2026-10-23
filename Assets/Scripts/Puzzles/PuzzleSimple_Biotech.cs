using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuzzleSimple_Biotech : PuzzleSimpleLerp
{
    public PuzzleSimpleLerp door1;
    public PuzzleSimpleLerp door2;
    public PuzzleSimpleLerp door3;
    public CircleCollider2D movingGuardrailCollider;
    public BoxCollider2D[] collidersToDisable;

    private bool disableButton = false;

    // Start is called before the first frame update
    public override void Start()
    {
        base.Start();
    }

    public override bool togglePower(bool on)
    {
        if (disableButton)
            return false;

        bool ret = base.togglePower(on);

        if (!ret)
            return ret;

        movingGuardrailCollider.enabled = true;

        if (!isOn)
        {          
            am.PlaySound("Door Open");
            door1.togglePower(false, false);
            door2.togglePower(false, false);
            door3.togglePower(false, false);
        }
        else
        {
            foreach (BoxCollider2D col in collidersToDisable)
                col.enabled = false;
        }

        return ret;
    }

    protected override void signalEnd()
    {
        movingGuardrailCollider.enabled = false;

        if (isOn)
        {
            am.PlaySound("Door Close");
            door1.togglePower(true, false);
            door2.togglePower(true, false);
            door3.togglePower(true, false);
            disableButton = true;
            StartCoroutine(waitToEnableButton());
        }
        else
        {
            foreach (BoxCollider2D col in collidersToDisable)
                col.enabled = true;
        }

        base.signalEnd();
    }

    private IEnumerator waitToEnableButton()
    {
        yield return new WaitForSeconds(0.51f);
        disableButton = false;
    }
}

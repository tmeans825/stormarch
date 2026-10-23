using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Com.LuisPedroFonseca.ProCamera2D;

public class WorkbenchOverride : BaseTrigger
{
    protected override void EnteredTrigger()
    {
        base.EnteredTrigger();

        AudioMaster.instance.PlayMusic("Wind", 0.5f, 0.5f);
    }

    protected override void ExitedTrigger()
    {
        base.ExitedTrigger();

        AudioMaster.instance.PlayMusic("Sanctuary", 0.2f, 0.5f);
    }
}

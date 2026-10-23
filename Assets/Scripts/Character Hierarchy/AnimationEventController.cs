using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** 
    DESCRIPTION: Detect when player shooting animation is complete to unfreeze animation
    REVISION HISTORY:
**/
public class AnimationEventController : MonoBehaviour
{
    private Humanoid pc;
    private void Start()
    {
        pc = GetComponentInParent<Humanoid>();
    }
    public void unfreezeAnimation()
    {
        if (pc != null)
        {
            pc.tryAnimUnlockAfterGun();
        }
    }

    public void prepareToGetUp()
    {
        if (pc != null)
            pc.prepGettingUp();
    }

    public void getUp()
    {
        if (pc != null)
            pc.stopSliding();
    }

    public void finishRecoil()
    {
        if (pc != null)
            pc.doneRecoiling();
    }

    public void bodyBounce()
    {
        if(pc != null)
            pc.characterDeathBounce();
    }

    public void finishDying()
    {
        if (pc != null)
            pc.characterDeath();
        else
            GetComponentInParent<Character>().characterDeath();
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerProgressBarController : MonoBehaviour
{
    public bool inProgress = false;
    public float bandageSpeed = 0.5f;
    public float medkitSpeed = 2f;
    private ProgressBarUIBehavior progressbarUI;
    private PlayerController pc;
    private AudioManager am;
    private string activeSFX;

    [System.NonSerialized]
    public static PlayerProgressBarController instance;

    public bool isReloading = false;

    // Start is called before the first frame update
    void Start()
    {
        if (instance == null && this != instance)
            instance = this;
        else
        {
            //UnityEngine.Debug.Log("Destroying playerController duplicate");
            Destroy(gameObject);
        }

        am = GetComponent<AudioManager>();
        progressbarUI = ProgressBarUIBehavior.instance;
        pc = GetComponent<PlayerController>();
    }
    public void attemptHeal(Healable.healableType ht, int index)
    {
        if (!inProgress)
        {
            float currentCap;
            if (ht == Healable.healableType.Bandage)
            {
                currentCap = bandageSpeed;
                activeSFX = "Bandage";
            }
            else
            {
                currentCap = medkitSpeed;
                activeSFX = "Medkit";
            }

            progressbarUI.startProgress();

            StartCoroutine(startHealBar(currentCap, ht, index));
        }
    }

    public void attemptConductor(int charges, int index)
    {
        if (!inProgress)
        {
            progressbarUI.startProgress();
            StartCoroutine(startConductorBar(charges, index));
        }
    }

    public void attemptCraft(InventoryItem item, int costCloth, int costChem, int costScrap)
    {
        if(!inProgress)
        {
            progressbarUI.startProgress();
            StartCoroutine(startCraftBar(item, costCloth, costChem, costScrap));
        }
    }

    public void attemptReload(float time, int gunType)
    {
        progressbarUI.startProgress();
        StartCoroutine(startReloadBar(time, gunType));
    }

    public void interruptableActionOccured()
    {
        if (inProgress)
            interrupt();
    }

    public void changedEquippedCheckForInterrupt()
    {
        if (isReloading) // changing equipped cancels reload
            interruptableActionOccured();
    }

    public void movingCheckForInterrupt()
    {
        if (!isReloading) // moving won't cancel reload
            interruptableActionOccured();
    }

    public void damageCheckForInterrupt()
    {
        if (!isReloading) // damage won't cancel reload
            interruptableActionOccured();
    }

    private void interrupt()
    {
        if(activeSFX != null)
            am.StopSound(activeSFX);

        StopAllCoroutines();
        inProgress = false;
        isReloading = false;
        progressbarUI.interruptProgress();
    }

    private IEnumerator startHealBar(float length, Healable.healableType ht, int index)
    {
        am.PlaySound(activeSFX);
        float totTime = 0.0f;
        bool done = false;
        inProgress = true;

        while (!done)
        {
            yield return null;
            totTime += Time.deltaTime;
            float percentage = totTime / length;
            progressbarUI.updateProgress(percentage);
            if (percentage >= 1.0f)
                done = true;
            if (pc.isMoving && totTime > 0.3f) // tiny wiggle room to be moving at beginning
                interrupt();
        }

        if (ht == Healable.healableType.Bandage)
            pc.heal(10f, index, false);
        else if (ht == Healable.healableType.Medkit)
            pc.heal(33f, index, true);

        inProgress = false;
    }

    private IEnumerator startConductorBar(int charges, int index)
    {
        am.PlaySound("Duct Tape");
        float totTime = 0.0f;
        bool done = false;
        inProgress = true;

        while (!done)
        {
            yield return null;
            totTime += Time.deltaTime;
            float percentage = totTime / 3f;
            progressbarUI.updateProgress(percentage);
            if (percentage >= 1.0f)
                done = true;
            if (pc.isMoving && totTime > 0.3f) // tiny wiggle room to be moving at beginning
                interrupt();
        }

        pc.equipCharges(charges, index);
        inProgress = false;
        am.PlaySound("Conductor Equipped");
    }

    private IEnumerator startCraftBar(InventoryItem item, int costCloth, int costChem, int costScrap)
    {
        am.PlaySound("Duct Tape");
        float totTime = 0.0f;
        bool done = false;
        inProgress = true;

        while (!done)
        {
            yield return null;
            totTime += Time.deltaTime;
            float percentage = totTime / 3f;
            progressbarUI.updateProgress(percentage);
            if (percentage >= 1.0f)
                done = true;
            if (pc.isMoving && totTime > 0.3f) // tiny wiggle room to be moving at beginning
                interrupt();
        }

        
        inProgress = false;
        CraftMenuBehavior.instance.craftingComplete(item, costCloth, costChem, costScrap);

        // to do - signal complete back to craft menu behavior
    }

    private IEnumerator startReloadBar(float time, int gunType)
    {
        // TODO - slowdown run speed
        PlayerController.instance.lockAnimToGun();

        float totTime = 0.0f;
        bool done = false;
        isReloading = true;
        inProgress = true;
        bool doubleTapped = false;

        while (!done)
        {
            yield return null;
            totTime += Time.deltaTime;
            float percentage = totTime / time;
            progressbarUI.updateProgress(percentage);
            if (percentage >= 1.0f)
                done = true;
            else if(percentage >= 0.5f && !doubleTapped)
            {
                PlayerController.instance.lockAnimToGun(); // loop anim
                doubleTapped = true;
            }
        }


        inProgress = false;
        isReloading = false;

        completedReloadContext ctx = new completedReloadContext();
        ctx.bulletsInChamber = PlayerController.instance.equippedGun.bulletsInMag;
        ctx.bulletsReloaded = PlayerController.instance.equippedGun.magSize - PlayerController.instance.equippedGun.bulletsInMag;
        GameEvents.current.reloadComplete(ctx);
        if (ctx.failReload)
            yield break;

        InventoryManager.instance.reload(gunType);    
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Com.LuisPedroFonseca.ProCamera2D;

public class BurrowerBehavior : Enemy
{
    [Header("Burrower")]
    public float minBurrowTime = 1f;
    public float maxBurrowTime = 3f;
    public float dirtDisturbPause = 1f;
    public float waitToShootAfterEmerge = 0.5f;
    public ParticleSystem dirtRumbleFX;
    public Animator burrowingAnimator;
    public Collider2D hitBox;
    public GameObject dirtLeftovers;
    public float shotgunSpeed = 10f;
    public GameObject deathItems;

    private int originalGunModel;
    private int originalBullets;
    private float originalBulletSpeed;
    private bool originalDeathItemsActive;
    private LayerMask intersectLayers;

    private Vector2 headOffset = new Vector3(0f, 2f, 0f);

    // Start is called before the first frame update
    protected override void Start()
    {
        isHuman = false;
        updatePathRate = 1.0f;
        base.Start();
        gun.SetActive(true);

        eyesightOffset = headOffset;

        originalGunModel = gunModel;
        originalBullets = bullets;
        originalBulletSpeed = bulletSpeed;
        if (deathItems != null)
            originalDeathItemsActive = deathItems.activeSelf;

        //isDocile = true;
        //chestAnimator.gameObject.SetActive(false);
        //hitBox.enabled = false;

        intersectLayers = (1 << LayerMask.NameToLayer("TerrainBlockingLayer")) | (1 << LayerMask.NameToLayer("KiplingCanPass")) | (1 << LayerMask.NameToLayer("Obstacle"));

        Vector2 gunLoc = new Vector2(-0.853f, 1.571f);
        directionLocationsGun = new Vector2[] { gunLoc, gunLoc, gunLoc, gunLoc };
        directionLocationsTip = new Vector2[] { new Vector2(0.0896f, 0.0401f), new Vector2(0.1732f, 0.0034f), new Vector2(0.0905f, 0.0034f), new Vector2(0.0111f, 0.0034f) };

        audioManager.muted = true; // don't make noise
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();
    }

    public void deployFromGround()
    {
        StartCoroutine(emerge());
    }

    // -> Code called once on events
    protected override void changeState(State s) // process one-time state change code here
    {
        base.changeState(s);

        switch (s)
        {
            default:
                break;
            case State.Idle:
                break;
            case State.Engage:
                break;
            case State.Reposition:
                // to do: add a cooldown after repositioning?
                StartCoroutine(beginReposition());
                break;
        }
    }


    protected override void flee(Vector2 target)
    {
        // don't call parent
        return;
    }

    // -> Code called every frame

    protected override void engage()
    {
        enemyFire();
    }

    protected override void reposition()
    {
        return;
    }

    public override void characterDeathBounce()
    {
        // purposely don't do anything. Expects 'lay shadow', etc
        return;
    }

    // -> Unique Helpers
    
    private IEnumerator beginReposition()
    {
        retreat();   

        yield return new WaitForSeconds(Random.Range(minBurrowTime, maxBurrowTime));

        // find emerge target - generate circle, etc
        burrowingAnimator.gameObject.SetActive(false);

        transform.position = findTargetPoint();

        // Upon reposition, lerp towards player until the head can see the player!!!!!       
        bool done = false;
        int loopCount = 0;

        while (!done)
        {
            Vector2 circlePosition = (Vector2)transform.position + headOffset;

            // Check for overlap with obstacle layer
            Collider2D[] obstacleHits = Physics2D.OverlapCircleAll(circlePosition, 1f, sightLayerMask);

            // Handle overlaps
            if (obstacleHits.Length == 0 || loopCount >= 100)
            {
                StartCoroutine(emerge());
                done = true;
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, player.transform.position, 0.01f); // inch up 1% of distance at a time. max 100 loops
                loopCount++;
            }

        }
    }

    protected override void onFirePatternComplete()
    {
        changeState(State.Reposition);
    }

    private Vector3 findTargetPoint()
    {
        bool done = false;

        float angleInRadians = 0f;
        float maxDistance = 12f;
        int counter = 0;

        while (!done)
        {
            counter++;

            if (counter > 300) // this is to prevent infinite loop - shouldn't happen, but still
            {
                return PlayerController.instance.transform.position;
            }

            // Generate a random angle between 0 and 360 degrees
            float randomAngle = Random.Range(0f, 360f);
            // Convert the angle to radians
            angleInRadians = randomAngle * Mathf.Deg2Rad;

            maxDistance = findMaxDistanceStillInSight(angleInRadians); 

            if (maxDistance > 2f) // make sure player isn't next to a rock here
                done = true;
        }

        float distance = Random.Range(2f, maxDistance);

        Vector2 rayDirection = new Vector2(Mathf.Cos(angleInRadians), Mathf.Sin(angleInRadians));

        return (PlayerController.instance.transform.position + (Vector3)(rayDirection.normalized * distance) - new Vector3(0f, 1.6f, 0f));
    }


    private void retreat()
    {
        // remove graphics, collider
        hudController.SetVisible(false);
        isRetracted = true;
        chestAnimator.gameObject.SetActive(false);
        hitBox.enabled = false;


        // play anim
        burrowingAnimator.gameObject.SetActive(true);
        burrowingAnimator.Play("Burrower Retreats");
        audioManager.PlaySound("Burrower Retreats");
        
        // Animator calls finishRetreating when anim is done
    }

    private IEnumerator emerge()
    {
        // move dirt, wait a sec (play SFX)
        audioManager.PlaySound("Dirt Rumble");
        ProCamera2DShake.Instance.ConstantShake("EarthquakeSoft");
        dirtRumbleFX.gameObject.SetActive(true);
        dirtRumbleFX.Play();
        dirtRumbleFX.transform.GetChild(0).gameObject.SetActive(true);

        yield return new WaitForSeconds(dirtDisturbPause);

        // stop SFX
        audioManager.StopSound("Dirt Rumble");
        ProCamera2DShake.Instance.StopConstantShaking();
        dirtRumbleFX.transform.GetChild(0).gameObject.SetActive(false);
        dirtRumbleFX.Stop();

        burrowingAnimator.gameObject.SetActive(true);
        ProCamera2DShake.Instance.Shake("Medium Shake");
        burrowingAnimator.Play("Burrower Emerges");
        audioManager.PlaySound("Dirt Emerge");
        audioManager.PlaySound("Burrower Emerge");

        bulletCount = 5;
        gunModel = 2;
        bulletSpeed = shotgunSpeed;
        overrideGunTipLogic = true;
        fireShotgunBlastFromDirt();
        overrideGunTipLogic = false;
        bulletCount = originalBullets;
        gunModel = originalGunModel;
        bulletSpeed = originalBulletSpeed;
        // Animator calls finishEmerging when anim is done

        nextFireIndex = 0; // reset firing pattern!
    }

    public void finishEmerging()
    {
        burrowingAnimator.gameObject.SetActive(false);
        chestAnimator.gameObject.SetActive(true);
        hitBox.enabled = true;
        hudController.SetVisible(true);
        StartCoroutine(cooldownBeforeUnretract());
    }

    public void finishRetreating()
    {
        Instantiate(dirtLeftovers, transform.position + new Vector3(0.1f, -1.3f, 0f), Quaternion.identity, ObjectPooler.instance.transform);
        bulletCount = 4;
        gunModel = 2;
        bulletSpeed = shotgunSpeed;
        audioManager.PlaySound("Dirt Retreat");
        if (audioManager.muted)
            audioManager.muted = false;
        else
        {
            ProCamera2DShake.Instance.Shake("Small Shake");
            overrideGunTipLogic = true;
            fireShotgunBlastFromDirt();
            overrideGunTipLogic = false;
        }    
    }

    private void fireShotgunBlastFromDirt()
    {
        gun.transform.localPosition = directionLocationsGun[0];
        gunTip.transform.localPosition = new Vector3(0.0905f, -0.2857f);
        shoot(Vector2.zero, gunType);
        gunTip.transform.localPosition = directionLocationsTip[3]; // incorrect - always add gunTip to the LEFT!
    }

    private IEnumerator cooldownBeforeUnretract()
    {
        yield return new WaitForSeconds(0.5f);
        isRetracted = false;

        // check if we should re-position 
        changeState(State.Engage);
    }

    private float findMaxDistanceStillInSight(float angleInRadians)
    {        
        // Calculate the direction of the raycast based on the angle
        Vector2 rayDirection = new Vector2(Mathf.Cos(angleInRadians), Mathf.Sin(angleInRadians));

        // Perform the raycast
        RaycastHit2D hit = Physics2D.Raycast(PlayerController.instance.transform.position, rayDirection, 12f, intersectLayers);

        // Check if the raycast hits something
        if (hit.collider != null)
        {
            return hit.distance;
        }
        else
        {
            return 12f;
        }
    }

    protected override void triggerDeath(int d)
    {
        audioManager.PlaySound("Death_Rattle");
        if (deathItems != null)
        {
            GameObject clone = Instantiate(
                deathItems,
                transform.position,
                transform.rotation,
                ObjectPooler.instance.transform);
            clone.SetActive(true);
        }
        base.triggerDeath(d);
    }

    protected override void restoreAuthoredBaselines()
    {
        base.restoreAuthoredBaselines();

        // The emerge/retreat shotgun swap mutates these public fields, so ES3 brings the shotgun
        // values back on a mid-combat scene reload.
        gunModel = originalGunModel;
        bullets = originalBullets;
        bulletCount = originalBullets;
        bulletSpeed = originalBulletSpeed;
    }

    public override void respawnFromDeath()
    {
        base.respawnFromDeath(); // restoreAuthoredBaselines() covers the gun baselines

        overrideGunTipLogic = false;
        nextFireIndex = 0;
        audioManager.muted = true;

        if (deathItems != null)
            deathItems.SetActive(originalDeathItemsActive);
        if (gun != null)
            gun.SetActive(true);
        if (chestAnimator != null)
            chestAnimator.gameObject.SetActive(true);
        if (hitBox != null)
            hitBox.enabled = true;
        if (burrowingAnimator != null)
            burrowingAnimator.gameObject.SetActive(false);
        if (dirtRumbleFX != null)
        {
            dirtRumbleFX.Stop();
            dirtRumbleFX.gameObject.SetActive(false);
            if (dirtRumbleFX.transform.childCount > 0)
                dirtRumbleFX.transform.GetChild(0).gameObject.SetActive(false);
        }

        audioManager.StopSound("Dirt Rumble");
        if (ProCamera2DShake.Instance != null)
            ProCamera2DShake.Instance.StopConstantShaking();
    }

    public override void aggro()
    {
        if (GameEvents.current.enemiesInCombat == 0)
        {
            // we are starting combat! deep rumble
            audioManager.muted = false;
            audioManager.PlaySound("Deep Growl");
            audioManager.muted = true;
        }

        StartCoroutine(waitToAggroOut());
    }

    private IEnumerator waitToAggroOut()
    {
        yield return new WaitForSeconds(1f);
        base.aggro();
    }
}

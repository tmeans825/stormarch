using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

public class SentinelBehavior : Enemy
{
    [Header("Melee: Personality")]
    [Tooltip("0 = Sentinel, 1 = Wolvolin")]
    [Range(0, 1)]
    public int enemyType = 0;
    [Tooltip("0 = aggressive, 1 = strategic, -1 = random")]
    [Range(-1, 1)]
    public int behaviorType = -1;

    [Header("Melee: Movement")]
    public float lungeDistance = 2f; // distance beyond player
    public float lungeSpeed = 10f;
    

    [Header("Melee: Combat Specific")]
    public float timeBetweenMove = 3.0f;
    public float meleeRange = 4.0f;
    public float chargeTime = 1f;
    public float timeBetweenCharge = 1f;
    public GameObject damageOnContact;

    [Tooltip("Interval will be random between 0.5*input - 1.5*input")]
    public float reFlankMedianInterval = 2.0f;

    [Tooltip("Distance from player at which the enemy holds its flank position.")]
    public float desiredFlankDistance = 6f;
    [Tooltip("Tighter = more side-on. 90 is perfectly perpendicular.")]
    public float flankAngleMin = 70f;
    public float flankAngleMax = 110f;

    [Header("Melee: Misc")]
    public GameObject stunParticles;
    public float deathExplosionRadius;
    public GameObject indicatorArrow; // only used by wolvo

    [Header("Wolvolin: Curl")]
    public GameObject ballAnimator;        // child GameObject, inactive by default
    public float curlDuration = 3f;
    public float curlHealthThreshold = 0.5f;

    private Vector2 flankPosition;
    private GameObject explosionTemplate;
    private Vector2[] gunLocsSentinel = new Vector2[] { new Vector2(0.1094f, 0.0752f), new Vector2(0.1502f, 0.0058f), new Vector2(0.1106f, -0.034f), new Vector2(0.0695f, 0.0058f) };
    private Vector2[] gunLocsWolvolin = new Vector2[] { new Vector2(0.794f, 0.026f), new Vector2(0.794f, 0.026f), new Vector2(-0.817f, -0.402f), new Vector2(-0.817f, -0.402f) };


    // Sentinel Only:
    private bool reFlank = false;
    private bool readyToReFlank = false;
    private float nextReFlank;
    private int flankSide = 0; // +1 = one side, -1 = the other, 0 = unassigned
    private float[] directionRotations = { 0f, 270f, 180f, 90f };
    private Light2D spotlight;
    private Light2D bodyLight;
    private Light2D tipLight;
    private Color originalSpotlightColor;
    private Color originalBodyColor;
    private Vector3[] tipLocs = new Vector3[] { new Vector3(-0.054f, 0.347f), new Vector3(0.047f, 0.551f), new Vector3(0.046f, 0.442f), new Vector3(-0.051f, 0.547f) };
    private ParticleSystem particles;
    private Vector3[] particleLocs = new Vector3[] { new Vector3(0.004f, -0.279f), new Vector3(-0.27f, -0.101f), new Vector3(-0.004f, 0.479f), new Vector3(0.27f, -0.091f) };
    private Quaternion[] quats = new Quaternion[] { Quaternion.Euler(0f, 0f, 180f), Quaternion.Euler(0f, 0f, 90f), Quaternion.identity, Quaternion.Euler(0f, 0f, -90f), };

    private float chargeCooldown = 0f;

    private bool isSpying = false;
    private bool isFrozen = false;

    // Wolvolin curl state
    private bool hasCurled = false;
    private bool isCurled = false;
    private int originalBehaviorType;
    private bool originalReFlank;
    private bool originalGunActive;
    private bool originalIndicatorArrowActive;
    private bool originalBallAnimatorActive;
    private bool originalExplosionActive;
    private bool originalSpotlightEnabled;
    private bool originalBodyLightEnabled;
    private bool originalTipLightEnabled;
    private float originalSpotlightInnerAngle;
    private float originalSpotlightOuterAngle;
    private float originalSpotlightOuterRadius;
    private float originalSpotlightIntensity;

    // Start is called before the first frame update
    protected override void Start()
    {
        updatePathRate = 1.0f;
        if (behaviorType < 0)
            behaviorType = Random.Range(0, 2);

        base.Start();

        gun.SetActive(true);

        switch (enemyType)
        {
            case 0:
                isHuman = false;
                Vector2 gunLoc = new Vector2(-1.1f, -0.15f);
                directionLocationsGun = new Vector2[] { gunLoc, gunLoc, gunLoc, gunLoc };
                directionLocationsTip = gunLocsSentinel;
                explosionTemplate = transform.GetChild(8).gameObject;
                spotlight = gunTip.GetChild(0).GetComponent<Light2D>();
                bodyLight = gunTip.GetChild(1).GetComponent<Light2D>();
                tipLight = transform.GetChild(9).GetComponent<Light2D>();
                particles = transform.GetChild(10).GetComponent<ParticleSystem>();

                // Guard against invalid index into directionRotations:
                if (directionRotations == null || directionRotations.Length == 0)
                {
                    // No rotations defined: use identity rotation and ensure index is zero.
                    directionCurrentlyFacing = 0;
                    gunTip.transform.rotation = Quaternion.identity;
                }
                else
                {
                    // Clamp the index to the valid range [0, length - 1].
                    directionCurrentlyFacing = Mathf.Clamp(directionCurrentlyFacing, 0, directionRotations.Length - 1);
                    gunTip.transform.rotation = Quaternion.Euler(0f, 0f, directionRotations[directionCurrentlyFacing]);
                }

                particles.Stop();
                originalSpotlightColor = spotlight.color;
                originalBodyColor = bodyLight.color;
                originalSpotlightEnabled = spotlight.enabled;
                originalBodyLightEnabled = bodyLight.enabled;
                originalTipLightEnabled = tipLight.enabled;
                originalSpotlightInnerAngle = spotlight.pointLightInnerAngle;
                originalSpotlightOuterAngle = spotlight.pointLightOuterAngle;
                originalSpotlightOuterRadius = spotlight.pointLightOuterRadius;
                originalSpotlightIntensity = spotlight.intensity;
                originalExplosionActive = explosionTemplate.activeSelf;
                setLightsToDirection(directionCurrentlyFacing);
                break;
            case 1:
                isHuman = true;
                indicatorArrow.SetActive(false);
                ax = 0f;
                ay = 0f;
                bx = 0f;
                by = 0f;
                break;
            default:
                break;
        }

        if (behaviorType == 1)
            reFlank = true;

        originalBehaviorType = behaviorType;
        originalReFlank = reFlank;
        originalGunActive = gun.activeSelf;
        if (indicatorArrow != null)
            originalIndicatorArrowActive = indicatorArrow.activeSelf;
        if (ballAnimator != null)
            originalBallAnimatorActive = ballAnimator.activeSelf;
    }


    private IEnumerator spyWhenStuck()
    {
        if (isSpying)
            yield break;

        isSpying = true;

        while (!isFrozen)
        {
            Vector3 snapshotPos = transform.position;

            yield return new WaitForSeconds(5f);

            if (transform.position == snapshotPos)
            {
                // debug your stuff
                Debug.Log("INTERVAL------------------");
                Debug.Log("State: " + state);
                Debug.Log("--------------------------");
                isFrozen = true;
            }
        }
    }

    // Update is called once per frame
    protected override void Update()
    {
        /*if(!enemyMoving)
        {
            Debug.Log("Not moving. I am in state " + state + ".");
        }  */  

        if (!isDead && !isStunned)
        {
            base.Update();
        }
    }

    // -> Code called once on events
    protected override void changeState(State s) // process one-time state change code here
    {
        if (player == null)
            player = PlayerController.instance.gameObject;

        if (subState == "charging") // no state changes while charging
            return;

        base.changeState(s);

        switch (s)
        {
            default:
                break;
            case State.Idle:
                break;
            case State.Engage:
                StartCoroutine(spyWhenStuck());
                if (behaviorType == 1)
                {
                    flankPosition = getFlankingPoint();
                    startMoving(flankPosition);
                }
                else
                    startMoving(player.transform.position);
                break;
            case State.Reposition:
                startMoving(player.transform.position);
                break;
        }
    }

    // -> Code called every frame

    protected override void engage()
    {
        switch (behaviorType)
        {
            default:
            case 0:
                engageAggressive();
                break;
            case 1:
                engageStrategic();
                break;
        }
    }

    private void engageAggressive()
    {
        if (subState == "charging" || isStunned)
            return;

        if ((Vector3.Distance(player.transform.position, this.transform.position) < meleeRange) && chargeCooldown == 0f)
        {
            if (enemyType == 0)
            {
                spotlight.color = Color.red;
                bodyLight.color = Color.red;
                tipLight.color = Color.red;
            }

            if (reFlank && !readyToReFlank)
            {
                readyToReFlank = true; // only reflank once enemy has reached target while Aggressive
                nextReFlank = Time.time + Random.Range(0.5f * reFlankMedianInterval, 1.5f * reFlankMedianInterval);
            }

            StartCoroutine(initiateChargeSequence());
        }
        else
        {
            // Called every frame on purpose: startMoving refreshes the target so the chase tracks a
            // moving player, and clears the arrival latch. It only re-arms the repath invoke when
            // one is not already running, so this does not submit an A* path per frame.
            startMoving(player.transform.position);
        }

        if (reFlank && readyToReFlank)
        {
            if (Time.time > nextReFlank)
            {
                readyToReFlank = false;
                behaviorType = 1;
                changeState(State.Engage);
            }
        }
        else
        {
            pointSpotlightAtPlayer();
        }

    }

    private void engageStrategic()
    {
        if (reachedEndOfPath)
        {
            behaviorType = 0;
            changeState(State.Engage);
            nextReFlank = Time.time + Random.Range(0.5f * reFlankMedianInterval, 1.5f * reFlankMedianInterval);
            return;
        }

        // Commit to the chosen flank. Only abandon it if the player has fled well beyond its useful radius.
        bool flankStale = Vector2.Distance(player.transform.position, flankPosition) > desiredFlankDistance * 2.5f;

        if (flankStale)
        {
            flankPosition = getFlankingPoint();
            startMoving(flankPosition);
            return;
        }

        if (!enemyMoving)
            startMoving(flankPosition);
    }


    protected override void reposition()
    {
        startMoving(PlayerController.instance.transform.position);
    }

    // -> General, one-time use stuff

    private void pointSpotlightAtPlayer()
    {
        if (player.transform != null)
        {
            if(enemyType == 1)
            {
                gun.transform.localPosition = new Vector3(Mathf.Clamp(player.transform.position.x - transform.position.x, -0.839f, 0.839f), gun.transform.localPosition.y);
            }

            //gunTip.transform.LookAt(player.transform.position);
            Vector3 difference = player.transform.position - gunTip.transform.position;
            float rotationZ = Mathf.Atan2(difference.y, difference.x) * Mathf.Rad2Deg;
            gunTip.transform.rotation = Quaternion.Euler(0.0f, 0.0f, rotationZ - 90.0f);
        }
    }

    private bool shouldCancelWolvolinLungeForCurl()
    {
        return enemyType == 1 && isCurled;
    }

    protected override bool shouldCancelBodyColorFade()
    {
        return base.shouldCancelBodyColorFade() || shouldCancelWolvolinLungeForCurl();
    }

    private void cancelWolvolinLungeForCurl()
    {
        if (enemyType != 1)
            return;

        subState = "";
        stopEnemyMoving();

        if (damageOnContact != null)
            damageOnContact.SetActive(false);
        if (indicatorArrow != null)
            indicatorArrow.SetActive(false);

        resetMainRendererColors(Color.white);
    }

    private IEnumerator initiateChargeSequence()
    {
        if (shouldCancelWolvolinLungeForCurl())
            yield break;

        if (enemyType == 1)
        {
            gun.SetActive(true);
            indicatorArrow.SetActive(true);
        }

        audioManager.PlaySound("Charge", true);
        stopEnemyMoving();
        subState = "charging";

        float timeElapsed = 0f;

        int directionForAnimations = getTargetDirection(player.transform.position);

        if (enemyType == 1) // wolvolin crouch anim
        {
            yield return null;
            if (shouldCancelWolvolinLungeForCurl())
            {
                cancelWolvolinLungeForCurl();
                yield break;
            }

            if (directionForAnimations == 2 || directionForAnimations == 3)
                chestAnimator.SetTrigger("PlayerRunLeft");
            else
                chestAnimator.SetTrigger("PlayerRunRight");

            yield return null;
            if (shouldCancelWolvolinLungeForCurl())
            {
                cancelWolvolinLungeForCurl();
                yield break;
            }

            chestAnimator.SetTrigger("PlayerSlide");
        }

        // loop where the beam narrows and sentinel flashes red
        StartCoroutine(fadeBodyColorOverTime(Color.white, Color.red, chargeTime));
        while (timeElapsed < chargeTime)
        {
            if (enemyIsDying)
                yield break;
            if (shouldCancelWolvolinLungeForCurl())
            {
                cancelWolvolinLungeForCurl();
                yield break;
            }

            timeElapsed += Time.deltaTime;
            
            float t = TEMLib.getSmoothLerpT(timeElapsed, chargeTime);
            if (enemyType == 0)
            {
                // close in the beam slowly on player angle              
                spotlight.pointLightInnerAngle = Mathf.Lerp(32f, 6f, t);
                spotlight.pointLightOuterAngle = Mathf.Lerp(56f, 10f, t);
                spotlight.pointLightOuterRadius = Mathf.Lerp(7.18f, Vector3.Distance(transform.position, player.transform.position), t);
                spotlight.intensity = Mathf.Lerp(1f, 2f, t);            
            }

            pointSpotlightAtPlayer();

            yield return null;
        }

        if (shouldCancelWolvolinLungeForCurl())
        {
            cancelWolvolinLungeForCurl();
            yield break;
        }

        // charge the sentinel forward. Stop moving will auto clear out subState so you'll be fine there.
        // Debug idea - may need to handle 'charging' getting cleared out right away. That could happen.
        damageOnContact.SetActive(true);
        dustEmitter.Play();
        // calculate player direction, make speed crazy high, start moving
        // raycast out. If hits obstacle, make the target the closest point before hitting obstacle.
        Vector3 dir = player.transform.position - transform.position;

        float finalLungeDistance = lungeDistance;

        if(enemyType == 0) // sentinel goes all the way. wolvolin goes 3.
            finalLungeDistance = Vector3.Distance(transform.position, player.transform.position) + lungeDistance;

        // ray cast; see if we need to shorten lunge
        int layerMask = (1 << obstacleLayer) | (1 << LayerMask.NameToLayer("KiplingCanPass"));

        RaycastHit2D hit;
        hit = Physics2D.Raycast(transform.position, dir.normalized, finalLungeDistance, layerMask);

        Vector3 endPos = transform.position + dir.normalized * finalLungeDistance;

        if (hit)
            endPos = hit.point + (hit.normal * 0.5f); // buffer from the hit point by the float amount

        StartCoroutine(startLunge(endPos));

        
    }

    private IEnumerator startLunge(Vector3 endPosition)
    {
        if (shouldCancelWolvolinLungeForCurl())
        {
            cancelWolvolinLungeForCurl();
            yield break;
        }

        audioManager.PlaySound("Lunge", true);
        float time = 0.0f;
        Vector3 startPosition = transform.position;

        if (enemyType == 1)
        {
            chestAnimator.SetTrigger(directionTriggersShooting[getTargetDirection(endPosition)]);
        }


        float distance = Vector3.Distance(startPosition, endPosition);

        float lungeDuration = distance / lungeSpeed;

        while (time < lungeDuration && !isStunned)
        {
            if (shouldCancelWolvolinLungeForCurl())
            {
                cancelWolvolinLungeForCurl();
                yield break;
            }

            float t = TEMLib.getSmoothLerpT(time, lungeDuration);
            transform.position = Vector3.Lerp(startPosition, endPosition, t);
            time += Time.deltaTime;
            yield return null;
        }

        chargeCooldown = timeBetweenCharge;

        float currentOuterRadius = 0f;
        if (enemyType == 0 && !isStunned)
        {
            currentOuterRadius = spotlight.pointLightOuterRadius;
            enemyFire();
        }

        StartCoroutine(fadeBodyColorOverTime(Color.red, Color.white, chargeCooldown));
        if (enemyType == 0 && spotlight.color == Color.red)
        {
            spotlight.color = originalSpotlightColor;
            bodyLight.color = originalBodyColor;
            tipLight.color = originalBodyColor;
        }
        while (chargeCooldown > 0f)
        {
            chargeCooldown -= Time.deltaTime;
            if (chargeCooldown < 0f)
                chargeCooldown = 0f;

            float t = TEMLib.getSmoothLerpT(timeBetweenCharge - chargeCooldown, timeBetweenCharge);
            if (enemyType == 0)
            {
                // Open beam back up
                spotlight.pointLightInnerAngle = Mathf.Lerp(6f, 32f, t);
                spotlight.pointLightOuterAngle = Mathf.Lerp(10f, 56f, t);
                spotlight.pointLightOuterRadius = Mathf.Lerp(currentOuterRadius, 7.18f, t);
                spotlight.intensity = Mathf.Lerp(2f, 1f, t);
                
            }

            yield return null;
        }

        damageOnContact.SetActive(false);
        subState = "";
        changeState(State.Engage);

        if (enemyType == 1)
            indicatorArrow.SetActive(false);
    }


    private Vector2 getFlankingPoint()
    {
        int side = pickFlankSide();

        Vector2 playerPos = player.transform.position;
        Vector2 playerToSelf = ((Vector2)transform.position - playerPos);
        if (playerToSelf.sqrMagnitude < 0.0001f)
            playerToSelf = Vector2.right; // degenerate: enemy on top of player

        float angle = Random.Range(flankAngleMin, flankAngleMax) * side;
        Vector2 flankDir = rotateVector(playerToSelf.normalized, Mathf.Deg2Rad * angle);

        return playerPos + flankDir * desiredFlankDistance;
    }

    private int pickFlankSide()
    {
        if (flankSide != 0) return flankSide;

        int leftCount = 0, rightCount = 0;
        Vector2 playerPos = player.transform.position;
        Vector2 playerForward = Vector2.up; // consistent world axis; what matters is sides stay separable

        for (int i = 0; i < aggroedEnemies.Count; i++)
        {
            SentinelBehavior s = aggroedEnemies[i] as SentinelBehavior;
            if (s == null || s == this || s.isDead || s.behaviorType != 1 || s.flankSide == 0)
                continue;

            if (s.flankSide > 0) rightCount++;
            else leftCount++;
        }

        if (leftCount < rightCount)       flankSide = -1;
        else if (rightCount < leftCount)  flankSide = +1;
        else
        {
            Vector2 toSelf = (Vector2)transform.position - playerPos;
            float cross = playerForward.x * toSelf.y - playerForward.y * toSelf.x;
            flankSide = (cross >= 0f) ? +1 : -1;
        }
        return flankSide;
    }

    protected override void triggerDeath(int d)
    {
        audioManager.PlaySound("Death_Rattle");

        if (enemyType == 1)
            indicatorArrow.SetActive(false);

        resetMainRendererColors(Color.white);

        if (!isHuman)
        {
            StopAllCoroutines();
            StartCoroutine(detonationCountdown(d));
            return;
        }
        else
            base.triggerDeath(d);
    }

    protected override void restoreAuthoredBaselines()
    {
        base.restoreAuthoredBaselines();

        // Combat flips this between flanking and direct (see changeState); unlike the Soldier's,
        // nothing in Update walks it back, so a reload would otherwise keep the combat value.
        behaviorType = originalBehaviorType;
    }

    public override void respawnFromDeath()
    {
        base.respawnFromDeath(); // restoreAuthoredBaselines() covers behaviorType

        reFlank = originalReFlank;
        readyToReFlank = false;
        nextReFlank = 0f;
        flankSide = 0;
        chargeCooldown = 0f;
        isSpying = false;
        isFrozen = false;
        hasCurled = false;
        isCurled = false;
        flankPosition = Vector2.zero;

        if (damageOnContact != null)
            damageOnContact.SetActive(false);
        if (gun != null)
            gun.SetActive(originalGunActive);

        if (mainRenderers != null)
        {
            foreach (SpriteRenderer sr in mainRenderers)
            {
                if (sr == null) continue;
                sr.enabled = true;
            }
        }
        resetMainRendererColors(Color.white);

        if (enemyType == 0)
        {
            if (explosionTemplate != null)
                explosionTemplate.SetActive(originalExplosionActive);
            if (spotlight != null)
            {
                spotlight.enabled = originalSpotlightEnabled;
                spotlight.color = originalSpotlightColor;
                spotlight.pointLightInnerAngle = originalSpotlightInnerAngle;
                spotlight.pointLightOuterAngle = originalSpotlightOuterAngle;
                spotlight.pointLightOuterRadius = originalSpotlightOuterRadius;
                spotlight.intensity = originalSpotlightIntensity;
            }
            if (bodyLight != null)
            {
                bodyLight.enabled = originalBodyLightEnabled;
                bodyLight.color = originalBodyColor;
            }
            if (tipLight != null)
            {
                tipLight.enabled = originalTipLightEnabled;
                tipLight.color = originalBodyColor;
            }
            if (particles != null)
                particles.Stop();

            int facing = Mathf.Clamp(directionCurrentlyFacing, 0, directionRotations.Length - 1);
            gunTip.transform.rotation = Quaternion.Euler(0f, 0f, directionRotations[facing]);
            setLightsToDirection(facing);
        }
        else
        {
            if (ballAnimator != null)
                ballAnimator.SetActive(originalBallAnimatorActive);
            if (indicatorArrow != null)
                indicatorArrow.SetActive(originalIndicatorArrowActive);
        }
    }

    private IEnumerator detonationCountdown(int d)
    {
        damageOnContact.SetActive(false);
        spotlight.enabled = false;
        
        particles.Stop();
        tipLight.enabled = false;
        bodyLight.enabled = false;
        GetComponent<BoxCollider2D>().enabled = false;


        if (explosionTemplate != null)
        {
            GameObject clone = Instantiate(
                explosionTemplate,
                explosionTemplate.transform.position,
                explosionTemplate.transform.rotation,
                ObjectPooler.instance.transform);
            clone.GetComponent<SentinelExplode>().radius = deathExplosionRadius;
            clone.SetActive(true);
        }

        stun(1.98f, stunParticles);
        yield return new WaitForSeconds(2f);

        base.triggerDeath(d);
    }

    public override void damageCharacter(float damage, Vector2 d, bool fromBleeding, bool ignoreShields, bool generateBlood, bool crit)
    {
        if (isCurled)
            return;

        bool wasAboveThreshold = (health > maxHealth * curlHealthThreshold);

        base.damageCharacter(damage, d, fromBleeding, ignoreShields, generateBlood, crit);

        if (enemyType == 1 && !hasCurled && !isDead && wasAboveThreshold
            && health <= maxHealth * curlHealthThreshold && hasOtherAggroedWolvolin())
        {
            StartCoroutine(curlIntoBall());
        }
    }

    private bool hasOtherAggroedWolvolin()
    {
        for (int i = 0; i < aggroedEnemies.Count; i++)
        {
            SentinelBehavior s = aggroedEnemies[i] as SentinelBehavior;
            if (s == null || s == this || s.isDead || s.enemyType != 1)
                continue;
            return true;
        }
        return false;
    }

    private IEnumerator curlIntoBall()
    {
        hasCurled = true;
        isCurled = true;

        subState = "";
        stopEnemyMoving();
        damageOnContact.SetActive(false);
        isRetracted = true;

        hudController.SetVisible(false);

        resetMainRendererColors(Color.white);
        if (mainRenderers != null)
        {
            foreach (SpriteRenderer sr in mainRenderers)
            {
                if (sr == null) continue;
                sr.enabled = false;
            }
        }
        gun.SetActive(false);
        indicatorArrow.SetActive(false);

        ballAnimator.SetActive(true);

        yield return new WaitForSeconds(curlDuration);

        ballAnimator.SetActive(false);

        if (mainRenderers != null)
        {
            foreach (SpriteRenderer sr in mainRenderers)
            {
                if (sr == null) continue;
                sr.enabled = true;
            }
        }
        resetMainRendererColors(Color.white);
        gun.SetActive(true);

        hudController.SetVisible(true);
        isRetracted = false;
        isCurled = false;

        changeState(State.Engage);
    }

    protected override void unstick()
    {
        if(state == State.Engage)
            startMoving(player.transform.position);
    }

    protected override void signalDirectionChange(int d)
    {
        base.signalDirectionChange(d);

        if (direction == 4 || enemyType != 0) // only matters for sentinel now
            return;

        if (behaviorType == 1 || state != State.Engage)
            gunTip.transform.rotation = Quaternion.Euler(0f, 0f, directionRotations[d]);

        setLightsToDirection(d);
    }

    private void setLightsToDirection(int d)
    {
        gunTip.transform.localPosition = directionLocationsTip[d];
        gun.transform.localPosition = directionLocationsGun[d];

        tipLight.gameObject.transform.localPosition = tipLocs[d];
        particles.gameObject.transform.localPosition = particleLocs[d];
        particles.transform.rotation = quats[d];
    }

    protected override void move()
    {
        base.move();
        if(!isHuman)
        {
            if(particles.isStopped)
            {
                particles.Play();
            }
        }
    }

    protected override void stopMoving(bool stopAnims)
    {
        base.stopMoving(stopAnims);
        if(!isHuman)
            particles.Stop();
    }
}


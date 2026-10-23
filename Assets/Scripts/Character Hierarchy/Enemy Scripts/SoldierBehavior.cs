using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PixelCrushers.DialogueSystem;

/** 
    DESCRIPTION: State & appearance definition code for soldier-type enemies
    HANDLE FLEEING IN THIS SCRIPT, NOT ENEMY
**/

public class SoldierBehavior : Enemy
{
    [Header("Soldier: Personality")]
    [Tooltip("0 = aggressive, 1 = strategic, -1 = random")]
    [Range(-1, 1)]
    public int behaviorType = -1;
    public bool ultraAggressiveLook = true;
    public float idealDistance = 10f;
    public float tooCloseDistance = 6f;

    [Header("Soldier: Combat Specific")]
    public float[] behaviorSwayArray = new float[3] { 0.2f, 0.1f, 0.1f };  
    public float investigationRange = 20.0f;
    public float timeBetweenMove = 3.0f;
    public float averageThrowTime = 5.0f;
    public float timeBeforeThrow = 5.0f;
    public float throwableDamage = 50f;
    public float throwableRadius = 5f;

    [Header("Soldier: Customization")]
    public string[] armorArray;
    public Sprite[] helmetArray;
    public bool isArrestingAnim = false;

    //[Header("Solider: Lights")]
    //public GameObject[] bodyLights;

    private GameObject currentCover;
    private Vector2 coverPosition;
    private int originalBehaviorType;
    private float timeSinceLastBehaviorCheck = 0.0f;
    private GameObject personalLight;
    private GameObject helmetLight;
    private bool originalPersonalLightActive;
    private bool originalHelmetLightActive;
    private int throwableType = 1;
    private float playerLastHurt = 0.0f;
    private float nextThrow;
    // -1 means "not waiting". Set on arrival at cover, cleared on departure so every later
    // arrival gets a full pause.
    private float repositionRetryAt = -1f;
    

    // Start is called before the first frame update
    protected override void Start()
    {
        if (behaviorType < 0)
            behaviorType = Random.Range(0, 2);
        base.Start();
        originalBehaviorType = behaviorType;
        aStarTargetBuffer = 1f;
        sway = behaviorSwayArray[behaviorType];


        setAppearance();

        personalLight = transform.GetChild(8).gameObject;
        helmetLight = transform.GetChild(9).gameObject;
        originalPersonalLightActive = personalLight.activeSelf;
        originalHelmetLightActive = helmetLight.activeSelf;

        if (faction == 2)
            throwableType = 0;

        GameEvents.current.onPlayerHurt += OnPlayerHurt;
        nextThrow = Random.Range(averageThrowTime * 0.5f, averageThrowTime * 1.5f);
    }

    // Update is called once per frame
    protected override void Update()
    {
        if (!isDead && !isStunned)
        {            
            if (originalBehaviorType != behaviorType && Time.time >= (timeSinceLastBehaviorCheck + 1.0f))
            {
                behaviorType = originalBehaviorType;
            }

            if(behaviorType == 1 && (state==State.Engage || state==State.Reposition)) // check to use throwable: cautious & engaged or repositioniong
            {
                checkToUseThrowable();
            }

            base.Update();
        }
    }

    private void checkToUseThrowable()
    {
        playerLastHurt += Time.deltaTime;
        if (playerLastHurt >= timeBeforeThrow)
        {
            if ((playerLastHurt - timeBeforeThrow) >= nextThrow)
            {
                // make sure the throw won't fucking kill you...
                RaycastHit2D hit;
                hit = Physics2D.Raycast(transform.position, player.transform.position - transform.position, Mathf.Infinity, sightLayerMask);
                bool throwIt = true;

                if (hit)
                {
                    // collide with all in the explosion range of the hit.point. If you're in it, don't throw..
                    Collider2D[] colls = Physics2D.OverlapCircleAll(hit.point, throwableRadius, 1 << LayerMask.NameToLayer("EnemyBlockingLayer"));
                    
                    for (int i = 0; i < colls.Length; i++)
                    {
                        if (this.transform == colls[i].transform)
                        {
                            throwIt = false;
                            break;
                        }
                        
                    }
                }

                if (throwIt)
                {
                    huckaNade(Camera.main.WorldToScreenPoint(player.transform.position) - Camera.main.WorldToScreenPoint(transform.position), throwableType, throwableRadius, throwableDamage);
                }

                nextThrow += Random.Range(averageThrowTime * 0.5f, averageThrowTime * 1.5f);
            }
        }
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
                baseSpeed = enemyOriginalSpeed;
                visitedCovers.Clear();
                if (behaviorType == 0)
                {
                    startMoving(player.transform.position);
                }
                else
                {
                    bool closestToPlayer = false;

                    currentCover = findCover(player.transform.position, investigationRange, closestToPlayer, null, true);
                    if (currentCover == null) // if there's no cover in the area, default to strategic behavior 
                    {
                        revertToAggressive();
                        changeState(State.Engage);
                    }
                    else
                    {
                        coverPosition = coverLocInLosOfPlayer(currentCover); // go to the cover in LOS of player if possible
                        startMoving(coverPosition);
                    }

                }
                break;
            case State.Reposition:
                baseSpeed = enemyOriginalSpeed * repositionSpeedModifier;
                if (behaviorType == 1)
                {
                    reachedEndOfPath = false;
                    currentWaypoint = 0;
                    currentCover = findCover(player.transform.position, investigationRange, true, currentCover, true);
                    if (currentCover == null) // if there's no cover in the area, default to strategic behavior 
                    {
                        revertToAggressive();
                        changeState(State.Reposition);
                    }
                    else
                    {
                        coverPosition = coverLocInLosOfPlayer(currentCover); // go to the cover in LOS of player if possible
                        startMoving(coverPosition);
                    }
                }
                else
                    startMoving(player.transform.position);
                break;
        }
    }

    private void revertToAggressive()
    {
        behaviorType = 0;
        timeSinceLastBehaviorCheck = Time.time;
    }

    // -> Code called every frame

    protected override void engage()
    {
        base.engage();
        switch (behaviorType)
        {
            default:
            case 0:
                engageAggressive();
                break;
            case 1:
                engageCautious();
                break;
        }
    }

    private void engageAggressive()
    {
        enemyFire();

        if (isFleeing()) // don't mess w positioning if fleeing
            return;

        if (Vector2.Distance(transform.position, player.transform.position) > sightRange) // too far, move in
        {
            // Every frame: refreshes the target so the chase tracks a moving player and clears the
            // arrival latch. startMoving only re-arms the repath invoke when one is not running.
            startMoving(player.transform.position);
        }
        else if (enemyMoving && (Vector2.Distance(transform.position, player.transform.position) <= idealDistance)) // just right, stop
        {
            stopEnemyMoving();
        }
        else if (!enemyMoving && (Vector2.Distance(transform.position, player.transform.position) <= tooCloseDistance)) // too close, back up
        {
            Vector2 directionToPlayer = (transform.position - player.transform.position).normalized;
            Vector2 newPosition = (Vector2)transform.position + directionToPlayer * idealDistance;
            flee(newPosition);
        }
    }

    private void engageCautious()
    {
        enemyFire();

        if (isFleeing())
            return;

        if (!reachedEndOfPath && !enemyMoving)
            startMoving(coverPosition);
    }

    protected override void reposition()
    {
        if (behaviorType == 0) // aggressive will charge if player is a little far and out of sight
        {
            startMoving(PlayerController.instance.transform.position);
        }
        else // strategic
        {
            if (!reachedEndOfPath)
            {
                repositionRetryAt = -1f; // still travelling; re-arm the pause for the next arrival
                if (currentCover != null)
                {
                    target = coverPosition;

                    if (!enemyMoving)
                        startMoving(coverPosition);
                }
            }
            else if (repositionRetryAt < 0f) // just arrived - hold before choosing new cover
            {
                repositionRetryAt = Time.time + 1f;
            }
            else if (Time.time >= repositionRetryAt)
            {
                // Was a per-frame StartCoroutine(changeStateAfterSeconds(...)), which kept roughly
                // one live coroutine per frame for as long as the soldier held cover.
                repositionRetryAt = -1f;
                changeState(State.Reposition);
            }
        }
    }

    protected override void unstick()
    {
        if (behaviorType == 0 && Vector2.Distance(transform.position, player.transform.position) <= idealDistance)
            startMoving(player.transform.position);
    }

    // -> General, one-time use stuff
    protected virtual void setAppearance()
    {
        int index = behaviorType;
        if (behaviorType == 0 && !ultraAggressiveLook)
            index = 2; // 3rd sheet is for semi aggressive

        if (isArrestingAnim)
        {
            chestSpriteSheetName = "Chest_Guardian_Arresting";
            chestAnimator.speed = 3.333f;
        }
        else
            chestSpriteSheetName = armorArray[index];
        
        legsSpriteSheetName = "Legs_Guardian";

        for (int i = 0; i < 4; i++) // for each head
        {
            transform.GetChild(1).GetChild(i).GetChild(0).GetComponent<SpriteRenderer>().sprite = helmetArray[i+(4*index)];
        }
    }

    public void resetArrestAnim()
    {
        chestAnimator.Play("Chest_Idle_Left_Anim", 0, 0.0f);
    }

    public override void damageCharacter(float damage, Vector2 d, bool fromBleeding, bool ignoreShields, bool generateBlood, bool crit)
    {
        if (isArrestingAnim)
            chestAnimator.speed = 1;

        base.damageCharacter(damage, d, fromBleeding, ignoreShields, generateBlood, crit);
    }

    public override void faceObject(Vector2 target, bool overrideAndReturnToOriginal)
    {
        if(!isArrestingAnim) // don't change anims if arresting and talking
            base.faceObject(target, overrideAndReturnToOriginal);
    }

    protected override void triggerDeath(int d)
    {
        GameEvents.current.onPlayerHurt -= OnPlayerHurt;
        //StartCoroutine(moveLights(d, 1.5f)); // just turn the lights off
        helmetLight.SetActive(false);
        personalLight.SetActive(false);
        base.triggerDeath(d);
    }

    protected override void restoreAuthoredBaselines()
    {
        base.restoreAuthoredBaselines();

        // revertToAggressive() flips behaviorType mid-fight. Update() walks it back on its own,
        // but sway is derived from it once in Start(), so restore both together here.
        behaviorType = originalBehaviorType;
        sway = behaviorSwayArray[behaviorType];
    }

    public override void respawnFromDeath()
    {
        base.respawnFromDeath(); // restoreAuthoredBaselines() covers behaviorType and sway

        repositionRetryAt = -1f;
        timeSinceLastBehaviorCheck = 0.0f;
        playerLastHurt = 0.0f;
        nextThrow = Random.Range(averageThrowTime * 0.5f, averageThrowTime * 1.5f);
        currentCover = null;
        coverPosition = Vector2.zero;
        setAppearance();

        if (personalLight != null)
            personalLight.SetActive(originalPersonalLightActive);
        if (helmetLight != null)
            helmetLight.SetActive(originalHelmetLightActive);

        GameEvents.current.onPlayerHurt -= OnPlayerHurt;
        GameEvents.current.onPlayerHurt += OnPlayerHurt;
    }

    private IEnumerator moveLights(int d, float speed)
    {
        Vector3 helmetEnd;
        Vector3 bodyEnd;
        if (d == 1)
        {
            helmetEnd = new Vector3(-0.548f, -0.398f, 0.0f);
            bodyEnd = new Vector3(0.082f, -0.344f, 0.0f);
        }
        else
        {
            helmetEnd = new Vector3(0.5f, -0.43f, 0.0f);
            bodyEnd = new Vector3(-0.17f, -0.344f, 0.0f);
        }

        while(helmetLight.transform.localPosition != helmetEnd || personalLight.transform.localPosition != bodyEnd)
        {
            if (helmetLight.transform.localPosition != helmetEnd)
                helmetLight.transform.localPosition = Vector3.MoveTowards(helmetLight.transform.localPosition, helmetEnd, speed * Time.deltaTime);
            if (personalLight.transform.localPosition != bodyEnd)
                personalLight.transform.localPosition = Vector3.MoveTowards(personalLight.transform.localPosition, bodyEnd, speed * Time.deltaTime);
            yield return null;
        }
    }

    private void OnPlayerHurt(damageContext ctx)
    {
        playerLastHurt = 0.0f;
        if(nextThrow > averageThrowTime * 1.5f)
            nextThrow = Random.Range(averageThrowTime * 0.5f, averageThrowTime * 1.5f);
    }

    protected override void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.gameObject.tag == "Fire") 
        {
            BarkManager.instance.fireBark(transform, collider.gameObject);
        }

        if (collider.gameObject.tag == "Flee")
        {
            BarkManager.instance.grenadeBark(transform, collider.gameObject);
        }

        base.OnTriggerEnter2D(collider);
    }

    private void OnDestroy()
    {
        GameEvents.current.onPlayerHurt -= OnPlayerHurt;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using Com.LuisPedroFonseca.ProCamera2D;
using PixelCrushers.DialogueSystem;
using System.Runtime.CompilerServices;
using UnityEngine.Experimental.GlobalIllumination;

/**
    DESCRIPTION: High-level State Machine and event processing code for enemy AI
    PRIMARY FUNCTIONALITY: Input, Reading Inventory
    Fleeing behavior is handled by children scripts. We will mark people as fleeing for fire and grenades here
**/

[RequireComponent(typeof(EnemyHUDController))]
public class Enemy : Humanoid
{
    [Header("Enemy Loot Classification")]
    public dropCategories.classification lootClass;
    public bool dropLoot = true;

    [Header("Enemy: Gun Traits")]
    public float sway = 1.0f;
    public bool useSimpleFirePattern = true;
    public float[] firePattern;
    public float firePatternTelegraphLength = 1.0f;
    public SpriteRenderer[] mainRenderers; // for turning red when telegraphing

    [Tooltip("0 - trad, 1 - lancer, 2 - electro")]
    [Range(0, 2)]
    public int gunType;
    [Tooltip("0 - rifle, 1 - pistol, 2 - shotgun")]
    [Range(0, 2)]
    public int gunModel;
    public float fireSpeed;
    public float damageLow;
    public float damageHigh;
    public float range;
    public float enemyBulletSpeed;
    public int bullets;

    [Header("Enemy: Idle Personality")]
    [Tooltip("0 = still, 1 = pace")]
    [Range(0, 1)]
    public int idleType = 0;
    public Vector3[] paceTargets;
    public float pauseAtTarget;
    public float paceSpeedModifier = 0.5f;
    private int paceIndex = 0;


    [Header("Enemy:Enemy Sight")]
    public float sightRange = 20.0f;
    public float awarenessRange = 15.0f;
    public float nextWaypointDistance = 3f;
    public bool isDocile = false;
    public float repositionSpeedModifier = 3f;

    [Header("Enemy: Unaggro")]
    [Tooltip("Uncheck to disable giving up on the player and returning to spawn (e.g. Burrower/Sentinel).")]
    public bool canUnaggro = true;
    [Tooltip("(A) Player must be at least this far from the enemy's CURRENT position.")]
    public float unaggroSelfDistance = 25f;
    [Tooltip("(B) Player must be at least this far from the enemy's SPAWN position.")]
    public float unaggroSpawnDistance = 50f;
    [Tooltip("All unaggro conditions must hold continuously this many seconds before giving up.")]
    public float unaggroGraceTime = 3.5f;

    [Header("Enemy: Separation")]
    [Tooltip("Uncheck to disable unclumping for this enemy (e.g., Burrower tunneling).")]
    public bool useSeparation = true;
    [Tooltip("Radius within which other aggroed enemies push this one away.")]
    public float separationRadius = 1.2f;
    [Tooltip("How strongly separation veers the pathfinding direction. ~1 = up to 50% veer.")]
    public float separationWeight = 1.2f;

    public static readonly List<Enemy> aggroedEnemies = new List<Enemy>();

    protected EnemyHUDController hudController;

    public bool willUseClotBerries = true;

    // Cached prefab value of willUseClotBerries. decideIfNeedClotberries flips the
    // runtime field to false after one heal attempt, so respawn must restore the
    // designer-set baseline rather than hard-coding back to true.
    private bool originalWillUseClotBerries;
    private bool originalIsDocile;

    private List<Clotberry> nearbyClotberries = new List<Clotberry>();
    private Clotberry activeClotberryInPursuit;
    private float clotberryRange;

    protected Vector2 target;
    protected float aStarTargetBuffer = 0.0f;
    protected bool reachedEndOfPath = false;
    protected int currentWaypoint = 0;
    protected float updatePathRate = 1.0f;
    protected GameObject player;

    protected Vector3 eyesightOffset = Vector3.zero;

    private float nextSightCheck = 0f;
    private bool playerInSightRange = false;
    // Timestamp when all unaggro conditions first became true; -1 means "not currently met".
    private float unaggroConditionsSince = -1f;
    private Path path;
    private Seeker seeker;
    protected bool enemyMoving = false;
    private int stateChangeIndex = 0; // internal index to track total state changes
    protected List<GameObject> visitedCovers;
    protected LayerMask obstacleLayer;
    protected int sightLayerMask;
    private bool engaged = false;

    // Runtime-only lifecycle flags. Never serialized (Unity or ES3): a restored value would not
    // describe this component's actual lifetime, and SaveManager.loadSceneData runs after Start().
    [System.NonSerialized] private bool hasStarted = false;
    [System.NonSerialized] private bool aggroQueuedBeforeStart = false;
    // Temporarily out of play (Burrower underground, Sentinel curled). Distinct from isDocile,
    // which is authored/story state and IS persisted - see isPassive().
    [System.NonSerialized] protected bool isRetracted = false;
    // The GameEvents instance we subscribed to, so unsubscribing survives a singleton swap.
    [System.NonSerialized] private GameEvents subscribedEvents;
    // DEPRECATED: direction smoothing handled in Character.getNewDirection + commit debounce.
    protected int smoothAnimWaypoints = 3;

    protected bool enemyIsDying = false;

    const string TINY_SHAKE = "Tiny Shake";

    protected float enemyOriginalSpeed;

    protected string subState = "";

    protected int nextFireIndex = 0;

    private DialogueSystemTrigger dst;

    private float stuckCheckInterval = 0.25f;
    private float timeSinceLastStuckCheck = 0.0f;
    private float timeStuck = 0.0f;
    private Vector3 lastPos;

    private float shieldsLastOn = 0f;

    private static float actHealthModifier = 0.2f; 
    private static float actShieldModifier = 0.1f;

    private GraphUpdateScene penaltyModifier;

    private Animator hitAnimator;


    //private static bool frameIsFrozen = false;
    //private float originalTimeScale;

    public enum State
    {
        Idle,
        Engage,
        Reposition
    }
    // Pure runtime state - Start() always assigns Idle, so a restored value has no authored
    // meaning. ES3 saves public fields but not the private ones that give this meaning (engaged,
    // subState), and it writes them after Start(), so persisting this brings back a combat state
    // with engaged == false: aggro() then rejects it forever (state != Idle) and checkForUnaggro()
    // rejects it forever (!engaged), leaving an enemy that fights but can never rejoin combat.
    [ES3NonSerializable]
    public State state;

    protected override void Awake()
    {
        base.Awake();

        enemyOriginalSpeed = baseSpeed;
        originalWillUseClotBerries = willUseClotBerries;
        originalIsDocile = isDocile;

        dst = GetComponent<DialogueSystemTrigger>();
    }

    // Start is called before the first frame update
    protected override void Start()
    {
        player = PlayerController.instance.gameObject;
        seeker = GetComponent<Seeker>();
        visitedCovers = new List<GameObject> { };
        obstacleLayer = LayerMask.NameToLayer("Obstacle");

        penaltyModifier = GetComponentInChildren<GraphUpdateScene>(true);

        // Read the volume for its authored amount and radius, but never let it write to the graph
        // itself. Its GameObject starts inactive and aggro switches it on, at which point
        // applyOnStart would fire a second, unremovable +penaltyDelta over the same area on top of
        // the one applyGraphPenalty tracks. Disabling the component stops OnEnable/Start/OnPostScan.
        if (penaltyModifier != null) penaltyModifier.enabled = false;

        int act = SaveManager.instance.act;

        health *= ((act * actHealthModifier) + 1);
        shields *= ((act * actShieldModifier) + 1); 

        if (idleType == 1) // 1 is pace, 0 is idle
        {
            transform.position = paceTargets[0];
            slowAnimAndSpeed(paceSpeedModifier);
        }

        hudController = GetComponent<EnemyHUDController>();
        hudController.Initialize(health, maxHealth, maxShields);

        int blockingLayer = LayerMask.NameToLayer("BlockingLayer");

        sightLayerMask = (1 << obstacleLayer) | (1 << blockingLayer);
        //originalTimeScale = Time.timeScale;

        base.Start();

        // will eventually change this to create a gun based on the act/difficulty, as well as char type...
        Gun equippedGun = LootMaster.instance.createGun(gunModel, fireSpeed, enemyBulletSpeed, damageLow, damageHigh, range, critChance, bullets, 1, 1, 4, null);

        equippedGunSFX = gunSFX[equippedGun.gType][equippedGun.mType];
        bulletSpeed = equippedGun.bulletSpeed;
        bulletLifetime = equippedGun.range;
        nextFire = Time.time + equippedGun.fireSpeed;
        bulletCount = equippedGun.bulletCount;
        damage = equippedGun.damage;
        critChance = equippedGun.critChance;

        // gun animator override here
        switch (equippedGun.gType)
        {
            case 0:
                switch (equippedGun.mType)
                {
                    case 0:
                        gunSpriteSheetName = "Traditional_Rifle";
                        break;
                    case 1:
                        gunSpriteSheetName = "Traditional_Pistol";
                        break;
                    case 2:
                        gunSpriteSheetName = "Traditional_Shotgun";
                        break;
                    default:
                        break;
                }
                break;
            case 1:
                switch (equippedGun.mType)
                {
                    case 0:
                        gunSpriteSheetName = "Lancer_Rifle";
                        break;
                    case 1:
                        gunSpriteSheetName = "Lancer_Pistol";
                        break;
                    default:
                        break;
                }
                break;
            case 2:
                electroAOE = equippedGun.aoeSize;
                switch (equippedGun.mType)
                {
                    case 0:
                        gunSpriteSheetName = "Electrospike_Rifle";
                        break;
                    case 2:
                        gunSpriteSheetName = "Electrospike_Shotgun";
                        break;
                    default:
                        break;
                }
                break;
            default:
                break;
        }

        changeState(State.Idle);

        setupNearbyClotberries();

        if (!TryGetComponent(out hitAnimator))
            Debug.LogWarning($"{name}: missing Animator for hit anims", this);

        // Fallback in case this component enabled before GameEvents.Awake ran.
        trySubscribeToSceneLoad();

        // A story trigger that activates an enemy and aggros it in the same UnityEvent reaches
        // aggro() before Start() runs, and the changeState(State.Idle) above would silently clobber
        // it. aggro() queues itself in that case; replay it now that initialization is complete.
        // Never reset hasStarted - Unity calls Start() once per component, so clearing it on
        // disable/respawn/pool reuse would queue every future aggro forever.
        hasStarted = true;
        bool replayAggro = aggroQueuedBeforeStart;
        aggroQueuedBeforeStart = false;
        if (replayAggro)
            aggro();
    }

    // Update is called once per frame
    protected override void Update()
    {
        if (isDead || isPassive() || isStunned || subState == "charging")
            return;

        checkForStateChanges();

        runPathfindingCode();

        if (isReturning())
            checkForReturnStuck();

        switch (state) // Call behavior code that should be processed every single frame
        {
            case State.Engage:
                engage();
                checkForStuck();
                break;
            case State.Reposition:
                reposition();
                checkForStuck();
                break;
            default:
                break;
        }

        if (isRecharging)
            hudController.OnRecharging(health, shields);

        base.Update();
    }

    private void checkForStateChanges()
    {
        // First - disengage completely if player is gone
        if (PlayerController.instance == null || PlayerController.instance.checkForDead())
        {
            if (state != State.Idle)
            {
                playerInSightRange = false;
                catchAnimationIssues();
                stopEnemyMoving();
                changeState(State.Idle);
            }
            return;
        }
        // Second - Make sure player is in range - if fighting ,decides between engage vs reposition. If idle, check if we can see player
        else if (Time.time > nextSightCheck) // spaced by time for performance. Fleeing overrides all
        {
            // Independent of sight range: an engaged enemy may give up if the player has fled far and broken LOS.
            checkForUnaggro();

            // see if 'could potentially see player'
            if (Vector3.Distance(this.transform.position, PlayerController.instance.transform.position) < sightRange)
            {
                playerInSightRange = true;
            }
            else if (playerInSightRange) // if player was in sight range, but is now too far away
            {
                playerInSightRange = false;

                if (state == State.Engage) // Fighting & player is out of gun range - reposition to go kill
                {
                    changeState(State.Reposition);
                }
            }

            if (playerInSightRange)
            {
                if (PlayerController.instance == null)
                    return;

                bool tryToSeePlayer = true;

                //If idle - we only care if player's angle is within the 'cone of vision', otherwise we ain't doin shit
                if (state == State.Idle && Vector3.Distance(this.transform.position, PlayerController.instance.transform.position) > awarenessRange) // awareness range = auto aggro
                {
                    // compare to direction enemy is facing. Return if not in vision cone, as enemy will not leave idle state
                    if (directionCurrentlyFacing != TEMLib.getDirection(transform.position, PlayerController.instance.transform.position))
                        tryToSeePlayer = false; // don't look for player if they aren't in cone of vision
                }

                if (tryToSeePlayer) // Player is in sight range - tiry to see them
                {
                    if (hasLineOfSightToPlayer()) // enemy can see player
                    {
                        seePlayer();
                    }
                    else if (!PlayerController.instance.isSliding) // don't confuse sliding for being 'not in sight line'
                    {
                        if (state == State.Engage) // Fighting & player is behind stuff - reposition to go kill
                        {
                            changeState(State.Reposition);
                        }
                    }
                }
            }

            nextSightCheck = Time.time + 0.5f;
        }
    }

    // Give up on the player and return to spawn once all three conditions hold continuously
    // for unaggroGraceTime: (A) player far from us, (B) player far from our spawn, (C) LOS blocked.
    private void checkForUnaggro()
    {
        // Scope to actual combat states; a "returning"/"fleeing"/"charging" substate blocks it too.
        if (!canUnaggro || !engaged || isPassive() || subState != ""
            || (state != State.Engage && state != State.Reposition))
        {
            unaggroConditionsSince = -1f;
            return;
        }

        Vector3 playerPos = PlayerController.instance.transform.position;
        bool farFromSelf = Vector3.Distance(transform.position, playerPos) > unaggroSelfDistance;    // (A)
        bool farFromSpawn = Vector3.Distance(originalSpawnPosition, playerPos) > unaggroSpawnDistance; // (B)
        bool losBlocked = !hasLineOfSightToPlayer();                                                  // (C)

        if (farFromSelf && farFromSpawn && losBlocked)
        {
            if (unaggroConditionsSince < 0f)
                unaggroConditionsSince = Time.time; // first moment all conditions held
            else if (Time.time - unaggroConditionsSince >= unaggroGraceTime)
                unaggro();
        }
        else
        {
            unaggroConditionsSince = -1f; // any condition lapsed -> reset the grace window
        }
    }

    private bool hasLineOfSightToPlayer()
    {
        if (PlayerController.instance == null)
            return false;

        Vector3 origin = transform.position + eyesightOffset;
        RaycastHit2D hit = Physics2D.Raycast(origin, PlayerController.instance.transform.position - origin, Mathf.Infinity, sightLayerMask);
        return hit && hit.collider.CompareTag("Player");
    }

    private void runPathfindingCode()
    {
        if (!reachedEndOfPath && enemyMoving)
        {
            if (path == null)
                return;

            if (currentWaypoint >= path.vectorPath.Count || Vector2.Distance((Vector2)this.transform.position, target) <= aStarTargetBuffer)
            {
                stopEnemyMoving();
                return;
            }
            else
            {
                reachedEndOfPath = false;
            }

            movementDirection = ((Vector2)path.vectorPath[currentWaypoint] - rb.position);
            movementSpeed = Mathf.Clamp(movementDirection.magnitude, 0.0f, 1.0f);
            movementDirection.Normalize();

            if (useSeparation)
            {
                Vector2 sep = computeSeparation();
                if (sep.sqrMagnitude > 0f)
                    movementDirection = (movementDirection + sep * separationWeight).normalized;
            }

            float distance = Vector2.Distance(rb.position, path.vectorPath[currentWaypoint]);

            if (distance < nextWaypointDistance)
            {
                currentWaypoint++;
            }

            checkForMove(); // check for move will move if movementDirection is modified
        }
    }

    protected Vector2 computeSeparation()
    {
        Vector2 accum = Vector2.zero;
        Vector2 myPos = rb.position;
        float radiusSq = separationRadius * separationRadius;

        for (int i = 0; i < aggroedEnemies.Count; i++)
        {
            Enemy other = aggroedEnemies[i];
            if (other == null || other == this || other.isDead)
                continue;

            Vector2 delta = myPos - (Vector2)other.transform.position;
            float dSq = delta.sqrMagnitude;
            if (dSq < 0.0001f || dSq > radiusSq)
                continue;

            float d = Mathf.Sqrt(dSq);
            accum += delta / d * (1f - d / separationRadius);
        }

        return accum;
    }

    private void checkForStuck()
    {
        // unstick if stuck for too long
        if (timeSinceLastStuckCheck >= stuckCheckInterval)
        {
            Vector3 checkVect = transform.position - lastPos;
            bool checkBool = false;
            if (checkVect.magnitude < 0.2f)
                checkBool = true;

            if (!checkBool)
            {
                timeStuck = 0.0f;
                timeSinceLastStuckCheck = 0.0f;
                lastPos = transform.position;
            }
            else if (checkBool && (timeStuck >= stuckCheckInterval))
            {
                unstick();

                timeStuck = 0.0f;
                timeSinceLastStuckCheck = 0.0f;
            }
            else
            {
                timeStuck += Time.deltaTime;
            }
        }
        else
            timeSinceLastStuckCheck += Time.deltaTime;
    }

    protected virtual void unstick()
    {

    }

    private void checkForReturnStuck()
    {
        if (!enemyMoving || reachedEndOfPath)
        {
            timeStuck = 0.0f;
            timeSinceLastStuckCheck = 0.0f;
            lastPos = transform.position;
            return;
        }

        timeSinceLastStuckCheck += Time.deltaTime;
        if (timeSinceLastStuckCheck < stuckCheckInterval)
            return;

        timeSinceLastStuckCheck = 0.0f;

        if (Vector3.Distance(transform.position, lastPos) >= 0.2f)
        {
            timeStuck = 0.0f;
            lastPos = transform.position;
            return;
        }

        timeStuck += stuckCheckInterval;
        if (timeStuck < stuckCheckInterval * 4f)
            return;

        // Returning is not a combat state, so do not call virtual unstick(); child overrides chase the player.
        timeStuck = 0.0f;
        lastPos = transform.position;
        currentWaypoint = 0;
        path = null;
        reachedEndOfPath = false;
        target = originalSpawnPosition;

        if (!enemyMoving)
            startMoving(originalSpawnPosition);
        else
            updatePath();
    }

    protected virtual void stopEnemyMoving()
    {
        // Capture before any branch clears subState; drives the "arrived home" finalize below.
        bool wasReturning = (subState == "returning");

        if (isFleeing())
        {
            subState = "";

            if(activeClotberryInPursuit != null && health > 0)
            {
                if (!activeClotberryInPursuit.used && Vector3.Distance(transform.position, activeClotberryInPursuit.transform.position) < 4f)
                {              
                    activeClotberryInPursuit.useClotberry();
                    heal(5f, -1);
                }

                activeClotberryInPursuit = null;
            }
        }
        currentWaypoint = 0;
        reachedEndOfPath = true;
        enemyMoving = false;
        movementDirection = Vector2.zero;
        checkForMove();
        CancelInvoke();

        if (wasReturning)
        {
            // Arrived home. Common cleanup above already ran; idle() may restart pacing via startMoving,
            // so finalize here and return so we don't also schedule a second pauseBeforeNextPace below.
            subState = "";
            if (idleType == 1) // aggro() restored full speed for combat; pacers need their slow pace speed back
                slowAnimAndSpeed(paceSpeedModifier);
            
            faceStartingDirection();
            idle();
            return;
        }

        if(state == State.Idle && idleType == 1)
        {
            StartCoroutine(pauseBeforeNextPace());
        }
    }

    private IEnumerator pauseBeforeNextPace()
    {
        yield return new WaitForSeconds(pauseAtTarget);
        if (state == State.Idle && idleType == 1) // only if haven't aggro'd or something 
            idle(); // next target
    }

    protected virtual void flee(Vector2 target)
    {
        subState = "fleeing";
        startMoving(target);
    }

    protected bool isFleeing()
    {
        return (subState.Equals("fleeing"));
    }

    protected bool isReturning()
    {
        return (subState.Equals("returning"));
    }

    protected virtual void changeState(State s) // allow for one-time calls
    {
        if (!isDead)
        {
            stateChangeIndex++;
            timeStuck = 0.0f;
            state = s;
            reachedEndOfPath = false;

            if (state == State.Idle)
                idle();
        }
    }

    protected void startMoving(Vector2 t)
    {
        // Callers routinely assign `target` themselves and pass it straight back, which defeated the
        // old `target != t` guard and stranded the enemy with reachedEndOfPath == true while
        // enemyMoving == true - runPathfindingCode then no-ops forever with nothing left to clear it.
        bool restarting = !enemyMoving;

        target = t;
        reachedEndOfPath = false;

        if (restarting || !IsInvoking("updatePath"))
        {
            if (restarting)
            {
                // stopEnemyMoving() rewinds currentWaypoint but keeps the old path, so without this
                // a restart walks the previous route from its start until the new path arrives.
                // Only on restart - clearing it on the every-frame retarget calls would stall movement.
                path = null;
                currentWaypoint = 0;
            }

            enemyMoving = true;
            CancelInvoke("updatePath"); // never stack duplicate repath invokes
            InvokeRepeating("updatePath", 0f, updatePathRate);
        }
    }

    private void updatePath()
    {
        if (seeker.IsDone())
            seeker.StartPath(rb.position, target, onPathComplete);
    }

    private void onPathComplete(Path p)
    {
        if (!p.error)
        {
            path = p;
            currentWaypoint = 0;
        }
    }

    public virtual void aggro()
    {
        if (!hasStarted)
        {
            // Activated and aggroed in the same frame. Start() has not run yet and its
            // changeState(State.Idle) would clobber whatever we set here, so queue instead.
            aggroQueuedBeforeStart = true;
            return;
        }

        // Deliberately does NOT guard on `engaged`. Paths that force state back to Idle without
        // clearing it (player death, makeDocile) would otherwise latch the enemy off permanently.
        // The combat refcount below is what stays guarded.
        if (state != State.Idle || isPassive())
            return;

        if (subState == "returning") // was walking home; abandon the return and re-enter combat
            subState = "";
        unaggroConditionsSince = -1f;

        changeState(State.Reposition);

        // Membership is independent of the refcount transition: OnDisable drops us from the list,
        // so a re-enabled enemy must be able to rejoin even when `engaged` is already true.
        // Separation, flank allocation and hasOtherAggroedWolvolin() all read this list.
        if (!aggroedEnemies.Contains(this))
            aggroedEnemies.Add(this);

        if(penaltyModifier != null)
        {
            penaltyModifier.gameObject.SetActive(true);
            StartUpdatingGraph(3f);
        }

        if (!engaged)
        {
            engaged = true;

            if(GameEvents.current.enemiesInCombat == 0 && dst != null)
            {
                // we are starting combat! Bark about it
                dst.enabled = true;
            }

            GameEvents.current.aggro(faction);
        }

        if (idleType == 1)
            resumeOriginalAnimAndSpeed();

        StartCoroutine(callForHelp());
    }

    private IEnumerator callForHelp()
    {
        yield return new WaitForSeconds(0.35f);

        //Debug.Log("Alerting");
        Collider2D[] colls = Physics2D.OverlapCircleAll(transform.position, sightRange, 1 << LayerMask.NameToLayer("EnemyBlockingLayer"));
        for (int i = 0; i < colls.Length; i++)
        {
            Enemy e = colls[i].gameObject.GetComponent<Enemy>();
            if (e != null && e.state == State.Idle && !e.isPassive())
                e.aggro();
        }
    }

    public void seePlayer()
    {
        if(state == State.Idle)
        {
            aggro();
        }
        else if (state != State.Engage)
        {
            changeState(State.Engage);
        }
    }

    // Player has clearly disengaged: leave combat, decrement the global combat count, and walk home to idle.
    // Mirrors the engaged-guarded cleanup in respawnFromDeath so the refcount stays balanced.
    protected virtual void unaggro()
    {
        if (!engaged)
            return;

        engaged = false;
        GameEvents.current.unaggro(faction); // decrements count; fires onUnaggro (music/reticle/save) at 0
        aggroedEnemies.Remove(this);
        unaggroConditionsSince = -1f;

        // tear down the combat pathfinding penalty, same as idle() does
        if (penaltyModifier != null)
        {
            penaltyModifier.gameObject.SetActive(false);
            StopUpdatingGraph();
        }

        // Leave Engage/Reposition and walk home. Set state manually rather than changeState(State.Idle):
        // that would call idle()->paceIdle() and immediately clobber the home target for pacing enemies.
        // Leaving state == Idle keeps re-aggro (seePlayer/damageCharacter) working while "returning" drives the walk.
        stateChangeIndex++; // cancel any pending changeStateAfterSeconds coroutines
        timeStuck = 0.0f;
        timeSinceLastStuckCheck = 0.0f;
        lastPos = transform.position;
        state = State.Idle;
        reachedEndOfPath = false;
        subState = "returning";
        startMoving(originalSpawnPosition);
    }

    protected virtual void idle()
    {
        if(penaltyModifier != null)
        {
            penaltyModifier.gameObject.SetActive(false);
            StopUpdatingGraph();
        }
        
        switch (idleType)
        {
            case 0:
                break;
            case 1:
                paceIdle();
                break;
            default:
                break;
        }
    }

    private void paceIdle()
    {
        paceIndex++;
        if (paceIndex >= paceTargets.Length)
            paceIndex = 0;

        startMoving(paceTargets[paceIndex]);
    }
    protected virtual void engage()
    {
        return;
    }

    protected virtual void reposition()
    {       
        return;
    }

    public override void characterDeath()
    {
        StopAllCoroutines();
        // StopAllCoroutines leaves Invokes running. A stale subclass transition during the death
        // animation can re-arm updatePath, which would then keep calling seeker.StartPath on a
        // corpse whose seeker is disabled below. respawnFromDeath already does this.
        CancelInvoke();
        // StopAllCoroutines killed the updater but not the graph write it left behind. Clear the
        // handle and take the penalty back out, or a dead enemy keeps deflecting paths forever.
        StopUpdatingGraph();
        if(engaged)
        {
            engaged = false;
            GameEvents.current.unaggro(faction);
        }
        aggroedEnemies.Remove(this);

        GameEvents.current.enemyKilled();
        seeker.enabled = false;
        hudController.SetVisible(false);
        base.characterDeath();
    }

    public override void respawnFromDeath()
    {
        base.respawnFromDeath();

        CancelInvoke();

        if (engaged)
        {
            engaged = false;
            GameEvents.current.unaggro(faction);
        }
        aggroedEnemies.Remove(this);

        if (seeker != null)
            seeker.enabled = true;

        path = null;
        target = transform.position;
        currentWaypoint = 0;
        reachedEndOfPath = true;
        enemyMoving = false;
        movementDirection = Vector2.zero;
        movementSpeed = 0f;
        timeStuck = 0.0f;
        timeSinceLastStuckCheck = 0.0f;
        lastPos = transform.position;
        playerInSightRange = false;
        nextSightCheck = 0f;
        unaggroConditionsSince = -1f;
        shieldsLastOn = 0f;

        enemyIsDying = false;
        subState = "";
        nextFireIndex = 0;
        nextFire = 0f;
        activeClotberryInPursuit = null;
        restoreAuthoredBaselines(); // docility, clotberries and speed - must precede setup below
        setupNearbyClotberries();

        if (mainRenderers != null)
        {
            foreach (SpriteRenderer mainRenderer in mainRenderers)
            {
                if (mainRenderer == null) continue;
                mainRenderer.enabled = true;
            }
        }
        resetMainRendererColors(Color.white);

        paceIndex = 0;
        state = State.Idle;
        changeState(State.Idle);

        if (hudController != null)
        {
            hudController.ResetValues(health, shields);
            hudController.SetVisible(false);
        }
    }

    /// <summary>
    /// Re-applies the designer-authored baselines that combat mutates at runtime. Called both from
    /// respawnFromDeath and after ES3 restores scene data.
    /// ES3 persists public fields but not the private caches holding their authored values, and it
    /// writes them after Start(), so a scene left mid-combat comes back with combat values and
    /// nothing to reset them - death was previously the only thing that did.
    /// Assigns fixed baselines and never toggles, so it is safe to run on every load.
    /// </summary>
    protected virtual void restoreAuthoredBaselines()
    {
        // A runtime `true` can only have come from the authored baseline: the transient burrow and
        // curl states live on isRetracted now, and makeDocile() has no C# or UnityEvent caller
        // (only undoDocile is wired, in Act1_Wilds). This also repairs legacy saves that captured
        // the old transient isDocile. If makeDocile ever gains a caller, split the flag again.
        isDocile = originalIsDocile && isDocile;
        isRetracted = false;

        willUseClotBerries = originalWillUseClotBerries;

        resumeOriginalAnimAndSpeed();
        if (idleType == 1)
            slowAnimAndSpeed(paceSpeedModifier);
    }

    void OnEnable()
    {
        trySubscribeToSceneLoad();
    }

    private void trySubscribeToSceneLoad()
    {
        GameEvents ge = GameEvents.current;
        if (ge == null || subscribedEvents != null)
            return;

        subscribedEvents = ge;
        ge.onPreviousSceneDataLoaded += previousSceneDataLoaded;
    }

    private void previousSceneDataLoaded()
    {
        // isDead is [ES3Serializable], so a corpse restores as a corpse. Leave it staged for death;
        // respawnFromDeath re-applies these baselines if it is ever revived.
        if (isDead)
            return;

        restoreAuthoredBaselines();
    }

    void OnDisable()
    {
        if (subscribedEvents != null)
        {
            subscribedEvents.onPreviousSceneDataLoaded -= previousSceneDataLoaded;
            subscribedEvents = null;
        }

        aggroedEnemies.Remove(this);

        // Deactivation leaves the graph write behind the same way death does.
        StopUpdatingGraph();

        // Deactivation is not death. Balance the combat refcount here or enemiesInCombat leaks and
        // combat music / save suppression never clear. scene.isLoaded is false during scene
        // teardown, where the singletons GameEvents.unaggro touches may already be destroyed.
        if (engaged)
        {
            engaged = false;
            if (GameEvents.current != null && gameObject.scene.isLoaded)
                GameEvents.current.unaggro(faction);
        }

        // Leave the machine in a re-aggroable Idle. Clearing `engaged` while `state` stayed at
        // Engage/Reposition would let a re-enabled enemy run combat AI that aggro() can no longer
        // register (its `state != Idle` guard rejects it), so it would fight without ever rejoining
        // aggroedEnemies or the combat count. Assign directly rather than via changeState - that
        // calls idle(), which can start pacing movement on an object that is being disabled.
        CancelInvoke("updatePath");
        path = null;
        currentWaypoint = 0;
        enemyMoving = false;
        reachedEndOfPath = true;
        movementDirection = Vector2.zero;
        subState = "";
        // Deactivation tears down the retracted presentation (animator, collider); don't let the
        // logical flag outlive it or the enemy returns permanently passive.
        isRetracted = false;
        state = State.Idle;
        unaggroConditionsSince = -1f;
        timeStuck = 0.0f;
        timeSinceLastStuckCheck = 0.0f;
        lastPos = transform.position;

        if (rb != null)
            rb.velocity = Vector2.zero;
    }

    protected override void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.gameObject.tag == "Fire" || collider.gameObject.tag == "Flee") // move out of fire
        {
            flee(CalculateExitPoint(collider));
        }

        base.OnTriggerEnter2D(collider);
    }


    protected override void OnTriggerExit2D(Collider2D collider)
    {
        if (collider.gameObject.tag == "Fire" || collider.gameObject.tag == "Flee") // move out of fire
        {
            if (isFleeing())
                stopEnemyMoving();
        }

        base.OnTriggerExit2D(collider);
    }

    protected override void OnTriggerStay2D(Collider2D collision)
    {
        if ((collision.gameObject.tag == "Fire" || collision.gameObject.tag == "Flee") && !isFleeing())
        {
            flee(CalculateExitPoint(collision));
        }

        base.OnTriggerStay2D(collision);
    }

    private Vector2 CalculateExitPoint(Collider2D collider)
    {
        // Get the center and radius of the circle collider
        Vector2 colliderCenter = collider.bounds.center;
        float colliderRadius = collider.bounds.extents.x; // Assuming a circle collider

        // Calculate the direction from the collider center to the current position
        Vector2 direction = (Vector2)transform.position - colliderCenter;
        direction.Normalize(); // Ensure the direction is a unit vector

        // Calculate the exit point on the perimeter of the collider with an offset
        float distanceMultiplier = 20f;
        Vector2 exitPoint = colliderCenter + (colliderRadius * distanceMultiplier) * direction;

        return exitPoint;
    }

    protected override void triggerDeath(int d)
    {
        if (enemyIsDying)
            return;

        stopEnemyMoving();
        resetMainRendererColors(Color.white);
        if(dropLoot)
        {
            DropManager.instance.generateDrop(lootClass, transform.position);
        }
        base.triggerDeath(d);

        enemyIsDying = true;
    }

    // inheritable helper functions -----------------------

    protected void resetMainRendererColors(Color color)
    {
        if (mainRenderers == null)
            return;

        foreach (SpriteRenderer mainRenderer in mainRenderers)
        {
            if (mainRenderer == null)
                continue;

            mainRenderer.color = color;
        }
    }

    protected virtual bool shouldCancelBodyColorFade()
    {
        return enemyIsDying;
    }

    /// <summary>
    /// Put the enemy in a new state after a specified number of seconds.
    /// If an outside trigger changes the enemy's state during the transition time, this function will cancel.
    /// </summary>
    /// <param name="t">Length to delay in seconds</param>
    /// <param name="s">State to transition to</param>
    /// <returns></returns>
    protected IEnumerator changeStateAfterSeconds(float t, State s) // puts player in idle for a specific amount of time before transitioning to new state
    {       
        int currentIndex = stateChangeIndex;
        yield return new WaitForSeconds(t);
        if(currentIndex == stateChangeIndex) // transition has been interrupted by a different state change
            changeState(s);
    }

    protected void enemyFire()
    {
        if (!canFireGun())
            return;

        if (useSimpleFirePattern && firePattern.Length != 0) // Firing patterns
        {
            StartCoroutine(patternFire());
        }
        else
        {
            straightFire();
        }
    }

    private void straightFire()
    {
        nextFire = Time.time + fireSpeed;
        fireWeapon();
    }

    private IEnumerator patternFire()
    {
        nextFire = Mathf.Infinity; // lock out firing until pattern is done
        nextFireIndex = 0;
        float timeElapsed = 0f;

        // FIRST - Telegraph
        shoot(player.transform.position, gunType, 0.0f, true); // This locks them in the aiming animation

        StartCoroutine(fadeBodyColorOverTime(Color.white, Color.red, firePatternTelegraphLength));
        while (timeElapsed < firePatternTelegraphLength)
        {
            if (enemyIsDying)
                yield break;

            if (isPassive()) // aborted — e.g. Burrower retreated underground mid-telegraph
            {
                nextFire = 0f;
                yield break;
            }

            timeElapsed += Time.deltaTime;

            yield return null;
        }

        StartCoroutine(fadeBodyColorOverTime(Color.red, Color.white, 0.2f));

        // THEN - Cycle Fire Pattern
        while (nextFireIndex < firePattern.Length)
        {
            while (isStunned)
                yield return null;

            if (enemyIsDying)
                yield break;

            if (isPassive()) // aborted — e.g. Burrower retreated underground mid-pattern
            {
                nextFire = 0f;
                yield break;
            }

            fireWeapon();

            yield return new WaitForSeconds(firePattern[nextFireIndex]);

            nextFireIndex++;
        }

        onFirePatternComplete();
        nextFire = 0f; // allow the beginning of the pattern again

    }

    protected IEnumerator fadeBodyColorOverTime(Color originalColor, Color newColor, float timeToTake)
    {
        float timeElapsed = 0f;

        while (timeElapsed < timeToTake)
        {
            if (shouldCancelBodyColorFade())
                yield break;

            timeElapsed += Time.deltaTime;

            float t = TEMLib.getSmoothLerpT(timeElapsed, timeToTake);
            Color calculatedColor = Color.Lerp(originalColor, newColor, t);

            resetMainRendererColors(calculatedColor);

            yield return null;
        }
    }

    private bool canFireGun()
    {
        if (isSliding || shieldsDeployed || Time.time < nextFire || Time.time < slideCooldown)
            return false;

        return true;
    }

    

    private void fireWeapon()
    {
        Vector3 playerPos = player.transform.position;
        float posSway = Random.Range(-1 * sway, sway);
        playerPos += (new Vector3(posSway, posSway) * swayController.instance.swayModifier);


        shoot(playerPos, gunType);
        playGunSFX();
    }

    // child classes can do something when they're done shooting a round
    protected virtual void onFirePatternComplete()
    {
        return;
    }

    public override void damageCharacter(float damage, Vector2 d, bool fromBleeding, bool ignoreShields, bool generateBlood, bool crit)
    {
        base.damageCharacter(damage, d, fromBleeding, ignoreShields, generateBlood, crit);

        if (!fromBleeding)
        {
            ProCamera2DShake.Instance.Shake(TINY_SHAKE);
            EventManager.instance.hitFreeze();

            if(hitAnimator != null)
            {
                hitAnimator.SetTrigger("Hit");
            }
        }

        hudController.OnDamaged(health, shields);

        if (state == State.Idle && health > 0 && !isPassive())
            aggro();

        decideIfNeedClotberries();
    }

    /// <summary>
    /// True when the enemy is out of the fight for any reason - authored/story docility or a
    /// temporary retraction. Every "should this enemy act?" check reads this rather than either
    /// flag alone, so the two cannot drift apart.
    /// </summary>
    public bool isPassive()
    {
        return isDocile || isRetracted;
    }

    public void undoDocile()
    {
        isDocile = false;   
    }

    public void makeDocile()
    {
        isDocile = true;
        if (state != State.Idle)
        {
            stopEnemyMoving();
            changeState(State.Idle);
        }
    }

    private void decideIfNeedClotberries()
    {
        if (!willUseClotBerries || subState == "fleeing" || health <= 0 || isPassive())
            return;

        float closestCB = float.MaxValue;
        int chosenCBIndex = -1;

        for (int i = 0; i < nearbyClotberries.Count; i++)
        {
            if (nearbyClotberries[i].used)
                continue;

            // does it qualify via the algorithm?


            float distance = Vector3.Distance(transform.position, nearbyClotberries[i].transform.position);

            if (((distance / clotberryRange) + (health / maxHealth)) >= 1)
                return;

            if (distance < closestCB)
            {
                chosenCBIndex = i;
                closestCB = distance;
            }
        }

        if (chosenCBIndex > -1)
        {
            flee(nearbyClotberries[chosenCBIndex].transform.position);
            activeClotberryInPursuit = nearbyClotberries[chosenCBIndex];
            BarkManager.instance.healBark(transform, nearbyClotberries[chosenCBIndex].gameObject);
            willUseClotBerries = false;
        }
    }

    private void setupNearbyClotberries()
    {
        nearbyClotberries.Clear();
        activeClotberryInPursuit = null;

        if (!willUseClotBerries)
            return;

        clotberryRange = sightRange;
        Collider2D[] colls = Physics2D.OverlapCircleAll(transform.position, clotberryRange, 1 << LayerMask.NameToLayer("Obstacle"));
        for (int i = 0; i < colls.Length; i++)
        {
            Clotberry cb = colls[i].gameObject.GetComponentInParent<Clotberry>();
            if (cb != null && !nearbyClotberries.Contains(cb))
                nearbyClotberries.Add(cb);
        }
    }

    private IEnumerator temporarilyTurnShieldsOn()
    {
        deployShields(target);

        while(shieldsLastOn < 0.5f && shieldsDeployed)
        {
            shieldsLastOn += Time.deltaTime;
            yield return null;
        }

        undeployShields();
    }

    /// <summary>
    /// Find a piece of cover based on a given search center and radius.
    /// </summary>
    /// <param name="searchCenter"></param>
    /// <param name="searchRadius"></param>
    /// <param name="returnClosestToPlayer">Set to 'true' to return the cover within the radius closest to the caller (new cover will be prioritized over visited cover). Otherwise, a random cover will be selected from within the radius.</param>
    /// <param name="coverToIgnore">If not set to null, will not return the cover passed in this parameter.</param>
    /// <param name="excludeCloseCover">If set to true, cover will not be returned that is very close to the target.</param>
    /// <returns>GameObject of cover within the search area, or null if no cover is found.</returns>
    protected GameObject findCover(Vector2 searchCenter, float searchRadius, bool returnClosestToPlayer, GameObject coverToIgnore, bool excludeCloseCover)
    {
        GameObject selectedCover = null;
        GameObject secondChoice = null;
        Collider2D[] colls = Physics2D.OverlapCircleAll(searchCenter, searchRadius, 1 << obstacleLayer);
        if (colls.Length != 0)
        {
            if (returnClosestToPlayer)
            {
                float currentClosest = float.MaxValue;
                float secondClosest = float.MaxValue;
                for (int i = 0; i < colls.Length; i++)
                {
                    GameObject currentCover = colls[i].gameObject;

                    if (excludeCloseCover && Vector2.Distance(searchCenter, currentCover.transform.position) < 8f)
                        break;

                    if (coverToIgnore != currentCover)
                    {
                        float thisDistance = Vector2.Distance(transform.position, currentCover.transform.position);
                        if (thisDistance < currentClosest)
                        {
                            if (!visitedCovers.Contains(currentCover)) // have we visited it before?
                            {
                                selectedCover = currentCover;
                                currentClosest = thisDistance;
                            }
                            else if(thisDistance < secondClosest) // if we have, and it's closer than the current backup, store this as a backup
                            {
                                secondChoice = currentCover;
                                secondClosest = thisDistance;
                            }
                        }
                    }
                }
                if (selectedCover == null) // there is no cover nearby that hasn't already been visited
                {
                    selectedCover = secondChoice;
                    visitedCovers.Clear();
                }
            }
            else
            {
                if (colls.Length != 1)
                {
                    int index = Random.Range(0, colls.Length);
                    selectedCover = colls[index].gameObject;
                    if(selectedCover == coverToIgnore)
                    {
                        if((index + 1) < colls.Length)
                            selectedCover = colls[index + 1].gameObject;
                        else
                            selectedCover = colls[index - 1].gameObject;
                    }
                }
            }
        }
        if (selectedCover != null)
            visitedCovers.Add(selectedCover);
        return selectedCover;
    }

    /// <summary>
    /// Returns a point on the selected cover that is in LOS of the player. If there isn't one, returns the Vector2 transform of the cover.
    /// </summary>
    /// <param name="cover"></param>
    /// <returns></returns>
    protected Vector2 coverLocInLosOfPlayer(GameObject cover)
    {
        if (cover == null)
            return transform.position;
        RaycastHit2D hit;
        hit = Physics2D.Raycast(PlayerController.instance.transform.position, cover.transform.position - player.transform.position, Mathf.Infinity, 1 << obstacleLayer);
        if (hit)
        {
            return hit.point;
        }
        else
        {
            return (Vector2)cover.transform.position;
        }
    }

    private Coroutine penaltyModifierUpdater;

    // What this enemy currently has written into the graph, so the exact same amount can be taken
    // back out of the exact same area. Removing by recalculating the cells instead would reset them
    // and destroy whatever else penalised those nodes - notably the water mask.
    private bool hasAppliedPenalty;
    private Bounds appliedPenaltyBounds;
    private int appliedPenaltyAmount;

    /// <summary>
    /// Starts periodically updating the graph with the penaltyModifier's position.
    /// </summary>
    /// <param name="updateInterval">Time interval in seconds between updates.</param>
    public void StartUpdatingGraph(float updateInterval)
    {
        if (penaltyModifierUpdater != null)
        {
            StopCoroutine(penaltyModifierUpdater);
        }
        penaltyModifierUpdater = StartCoroutine(UpdateGraphPeriodically(updateInterval));
    }

    /// <summary>
    /// Stops the periodic graph updates and takes this enemy's penalty back out of the graph.
    /// </summary>
    public void StopUpdatingGraph()
    {
        if (penaltyModifierUpdater != null)
        {
            StopCoroutine(penaltyModifierUpdater);
            penaltyModifierUpdater = null;
        }
        removeGraphPenalty();
    }

    /// <summary>
    /// Writes this enemy's avoidance penalty into the graph, replacing whatever it wrote before.
    /// The penaltyModifier component is still the authored source of the amount and the radius;
    /// only the graph write happens here, so it can be taken back out symmetrically.
    /// </summary>
    private void applyGraphPenalty()
    {
        if (AstarPath.active == null || penaltyModifier == null) return;

        CircleCollider2D area = penaltyModifier.GetComponent<CircleCollider2D>();
        if (area == null) return;

        removeGraphPenalty();

        appliedPenaltyBounds = area.bounds;
        appliedPenaltyAmount = penaltyModifier.penaltyDelta;
        hasAppliedPenalty = true;

        // Penalty only - the authored volume sets modifyTag to 0, and letting this default to
        // updatePhysics would recalculate the cells and clear every other penalty on them.
        GraphUpdateObject guo = new GraphUpdateObject(appliedPenaltyBounds);
        guo.addPenalty = appliedPenaltyAmount;
        guo.updatePhysics = false;
        guo.updateErosion = false;

        AstarPath.active.UpdateGraphs(guo);
    }

    /// <summary>
    /// Takes this enemy's penalty back out of the graph, leaving anything else on those nodes
    /// alone. Safe to call when nothing is applied.
    /// </summary>
    private void removeGraphPenalty()
    {
        if (!hasAppliedPenalty) return;
        hasAppliedPenalty = false;

        if (AstarPath.active == null) return;

        AstarPath.active.UpdateGraphs(new PenaltyRemoval(appliedPenaltyBounds, (uint)appliedPenaltyAmount));
    }

    /// <summary>
    /// Subtracts a fixed penalty from every node in its bounds, clamped at zero.
    /// Being a plain inverse of the additive apply makes it order-independent, which matters
    /// because graph updates drain through one shared work item and can batch out of step with
    /// anything queued separately.
    /// A negative addPenalty cannot do this job: GraphUpdateObject.Apply casts the sum back to
    /// uint, so subtracting from an already-cleared node wraps to ~4.29 billion and walls it off.
    /// </summary>
    private class PenaltyRemoval : GraphUpdateObject
    {
        private readonly uint amount;

        public PenaltyRemoval(Bounds bounds, uint amount) : base(bounds)
        {
            this.amount = amount;
            updatePhysics = false;
            updateErosion = false;
        }

        public override void Apply(GraphNode node)
        {
            node.Penalty = node.Penalty >= amount ? node.Penalty - amount : 0;
        }
    }

    /// <summary>
    /// Coroutine to periodically update the graph with the penaltyModifier's position.
    /// </summary>
    /// <param name="updateInterval">Time interval in seconds between updates.</param>
    private IEnumerator UpdateGraphPeriodically(float updateInterval, float minDistanceMoved = 5f)
    {
        applyGraphPenalty();
        Vector3 lastPosition = transform.position;

        while (true)
        {
            // Check if the character has moved a significant distance
            if (Vector3.Distance(lastPosition, transform.position) >= minDistanceMoved)
            {
                applyGraphPenalty();
                lastPosition = transform.position;
            }

            yield return new WaitForSeconds(updateInterval);
        }
    }


    // temporary test scripts ---------------------------------------------------------------------

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, sightRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, awarenessRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 5.36f);
    }
}

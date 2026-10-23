using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

public class NonCombatChar : Character
{
    // NOTE: This class can handle simple movement, including APIs that force it to move, with code hooks for children scripts to hook into

    [Header("Non-Combat Char: Appearance Template")]
    public CharacterAppearanceReferences.TemplateLook templateLook;

    [Header("Non-Combat Char: Pathfinding")]
    public float nextWaypointDistance = 3f;
    public float maxDistanceFromPlayer = 20f;
    public float idealStopDistanceFromPlayer = 3f;

    [Header("Non-Combat Char: Follow Rescue")]
    [Tooltip("While following, teleport to the player if farther than this for rescueDelaySeconds")]
    public float rescueDistanceFromPlayer = 35f;
    public float rescueDelaySeconds = 3f;
    public float rescueRetryCooldown = 1f;

    private const float RESCUE_RING_RADIUS = 12f;
    private const float RESCUE_SNAP_TOLERANCE = 1.5f;
    private const float RESCUE_VIEWPORT_MARGIN = 0.1f;
    private const int RESCUE_RING_SAMPLES = 8;
    private const int MAX_NODE_SEARCH_RETRIES = 2;

    protected Vector2 startingSpawn;

    protected Vector2 target;
    protected float aStarTargetBuffer = 0.0f;
    protected bool reachedEndOfPath = true; // *TEM 5/3 - this change might be risky.
    protected int currentWaypoint = 1;
    protected float updatePathRate = 1.0f;
    // DEPRECATED: direction smoothing handled in Character.getNewDirection + commit debounce.
    protected int smoothAnimWaypoints = 0;

    private Path path;
    private Seeker seeker;

    
    private bool deployedButWaitForCharacter = false;
    private Vector2 cachedMovementDirection;
    private bool isWaitingForChar = false;

    private bool isFollowingPlayer = false;
    private GameObject followTarget;

    private bool isCalculatingNewPath = false;
    private int pathRequestId = 0; // bumped to invalidate in-flight path callbacks
    private int nodeSearchRetries = 0;

    private float farFromPlayerTimer = 0f;
    private float rescueCooldownTimer = 0f;


    protected override void Start()
    {
        seeker = GetComponent<Seeker>();
        startingSpawn = transform.position;

        // Before base.Start(): Character.Start loads the chest/legs sheets by name, and a blank name
        // makes Resources.LoadAll pull every sheet in the folder (duplicate "Chest_0" key).
        randomizeAppearanceToTemplate();

        base.Start();
    }

    protected virtual void OnDisable()
    {
        resetRescueTimers();
    }

    protected override void Update()
    {
        checkRescue();
        runPathfindingCode();
        base.Update();
    }

    // Backup for followers that get stuck or left behind: teleport to the player after being too far for a while
    private void checkRescue()
    {
        if (!isFollowingPlayer || isDead || !canRescue())
        {
            resetRescueTimers();
            return;
        }

        float sqrDistance = ((Vector2)transform.position - (Vector2)PlayerController.instance.transform.position).sqrMagnitude;
        if (sqrDistance <= rescueDistanceFromPlayer * rescueDistanceFromPlayer)
        {
            resetRescueTimers();
            return;
        }

        if (rescueCooldownTimer > 0f)
        {
            rescueCooldownTimer -= Time.deltaTime;
            return;
        }

        farFromPlayerTimer += Time.deltaTime;
        if (farFromPlayerTimer < rescueDelaySeconds)
            return;

        if (tryFindRescuePoint(out Vector2 rescuePoint))
            rescueTeleport(rescuePoint);
        else
            rescueCooldownTimer = rescueRetryCooldown; // no valid off-screen spot yet; try again shortly
    }

    private bool canRescue()
    {
        PlayerController player = PlayerController.instance;
        return player != null && !player.isDead && !player.frozen
            && (CanvasMaster.instance == null || !CanvasMaster.instance.isCutscenePlaying);
    }

    private void resetRescueTimers()
    {
        farFromPlayerTimer = 0f;
        rescueCooldownTimer = 0f;
    }

    // Finds a walkable, same-area node near the player that is off-camera so the NPC doesn't visibly pop in
    private bool tryFindRescuePoint(out Vector2 point)
    {
        point = Vector2.zero;
        Camera cam = Camera.main;
        if (AstarPath.active == null || cam == null)
            return false;

        Vector2 playerPos = PlayerController.instance.transform.position;
        NNInfo playerNode = AstarPath.active.GetNearest(playerPos, NNConstraint.Default);
        if (playerNode.node == null)
            return false;

        float bestSqr = float.MaxValue;
        bool found = false;
        float startAngle = Random.value * Mathf.PI * 2f;
        for (int i = 0; i < RESCUE_RING_SAMPLES; i++)
        {
            float angle = startAngle + (i * Mathf.PI * 2f / RESCUE_RING_SAMPLES);
            Vector2 sample = playerPos + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Mathf.Max(idealStopDistanceFromPlayer, RESCUE_RING_RADIUS);

            NNInfo info = AstarPath.active.GetNearest(sample, NNConstraint.Default);
            if (info.node == null || !info.node.Walkable || info.node.Area != playerNode.node.Area)
                continue;

            Vector2 snapped = info.position;
            if (Vector2.Distance(snapped, sample) > RESCUE_SNAP_TOLERANCE)
                continue;

            Vector3 viewport = cam.WorldToViewportPoint(snapped);
            bool onScreen = viewport.x > -RESCUE_VIEWPORT_MARGIN && viewport.x < 1f + RESCUE_VIEWPORT_MARGIN
                         && viewport.y > -RESCUE_VIEWPORT_MARGIN && viewport.y < 1f + RESCUE_VIEWPORT_MARGIN;
            if (onScreen)
                continue;

            float sqr = (snapped - playerPos).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                point = snapped;
                found = true;
            }
        }
        return found;
    }

    private void rescueTeleport(Vector2 destination)
    {
        // invalidate any in-flight path so a stale callback can't overwrite the fresh one
        pathRequestId++;
        seeker.CancelCurrentPathRequest();
        CancelInvoke(nameof(updatePath));

        path = null;
        currentWaypoint = 1;
        isCalculatingNewPath = false;
        nodeSearchRetries = 0;
        isWaitingForChar = false;
        deployedButWaitForCharacter = false;
        movementDirection = Vector2.zero;
        movementSpeed = 0f;

        rb.position = destination;
        transform.position = destination;
        rb.velocity = Vector2.zero;
        Physics2D.SyncTransforms();

        resetRescueTimers();
        reachedEndOfPath = false;
        beginPathing(followTarget.transform.position); // not startMoving(): subclasses (Owen) hook it with unrelated behavior
    }

    private void runPathfindingCode()
    {
        if (!reachedEndOfPath)
        {
            if (path == null)
                return;

            if (currentWaypoint >= path.vectorPath.Count || Vector2.Distance((Vector2)this.transform.position, target) <= aStarTargetBuffer) // made it to target
            {
                if (isFollowingPlayer && isPlayerFartherThanIdeal()) // made it, but player too far - wait for a path update, which happens every 1 second
                {                   
                    return;
                }
                else // made it to target
                {
                    deployedButWaitForCharacter = false;
                    isWaitingForChar = false;
                    stopMoving();
                    return;
                }
            }
            else if (deployedButWaitForCharacter && isPlayerFartherThanMax()) // don't go too far if waiting for char
            {
                pauseMovement();
                return;
            }
            else
            {
                reachedEndOfPath = false;
            }

            movementDirection = ((Vector2)path.vectorPath[currentWaypoint] - rb.position);
            movementSpeed = Mathf.Clamp(movementDirection.magnitude, 0.0f, 1.0f);
            movementDirection.Normalize();


            float distance = Vector2.Distance(rb.position, path.vectorPath[currentWaypoint]);

            checkForMove(); // check for move will move if movementDirection is modified

            if (distance < nextWaypointDistance)
            {
                currentWaypoint++;
            }
        }
        else if (isWaitingForChar && (Vector3.Distance(this.transform.position, PlayerController.instance.transform.position) <= (maxDistanceFromPlayer / 2)))
        {
            restartMovement();
            reachedEndOfPath = false;
        }
        else if (isFollowingPlayer && isPlayerFartherThanMax()) // reached end of path; need to restart
        {
            startMoving(followTarget.transform.position);
        }
    }

    // PUBLIC HOOK: Use this to force the character to move to location t
    public virtual void startMoving(Vector2 t)
    {
        if (target != t || !reachedEndOfPath)
            beginPathing(t);
    }

    private void beginPathing(Vector2 t)
    {
        target = t;
        reachedEndOfPath = false;
        CancelInvoke(nameof(updatePath)); // avoid stacking repeating invokes
        InvokeRepeating(nameof(updatePath), 0f, updatePathRate);
    }

    // PUBLIC HOOK: Use this to force moving, but waiting for char
    public void startMovingWaitingForChar(Vector2 t)
    {
        deployedButWaitForCharacter = true;
        cachedMovementDirection = t;
        startMoving(t);
    }

    // PUBLIC HOOK: Use this to force following a target
    public void startFollowingPlayer()
    {
        isFollowingPlayer = true;
        resetRescueTimers();
        followTarget = PlayerController.instance.gameObject;
        startMoving(followTarget.transform.position);
    }

    // PUBLIC HOOK: Use this to stop following a character
    public void stopFollowingPlayer()
    {
        isFollowingPlayer = false;
        resetRescueTimers();
    }

    public void overrideStopMoving()
    {
        target = Vector2.zero;
        stopMoving();
    }

    public void randomizeAppearanceToTemplate()
    {
        if (templateLook != CharacterAppearanceReferences.TemplateLook.None) 
            CharacterAppearanceReferences.instance.changeCharacterAppearance(this, templateLook);
    }

    private void pauseMovement()
    {
        //Debug.Log("too far from char");
        isWaitingForChar = true;
        stopMoving();
    }

    private void restartMovement()
    {
        //Debug.Log("restarting movement");
        isWaitingForChar = false;
        startMoving(cachedMovementDirection);
    }

    protected virtual void stopMoving()
    {
        if (!isWaitingForChar)
        {
            currentWaypoint = 1;
            Invoke("arriveAndFaceTarget", 0.1f); // has to be delayed so the idle triggers can go through
        }

        reachedEndOfPath = true;
        movementDirection = Vector2.zero;
        checkForMove();
        CancelInvoke();

        //base.stopMoving(true);     
    }

    private void arriveAndFaceTarget()
    {
        if(reachedEndOfPath) // make sure new path hasn't started
            faceObject(target, false);
    }

    private void updatePath()
    {
        if (seeker.IsDone() && !isCalculatingNewPath)
        {
            if(isFollowingPlayer)
            {
                target = followTarget.transform.position;
            }

            int requestId = pathRequestId;
            isCalculatingNewPath = true;
            seeker.StartPath(rb.position, target, p => onPathComplete(p, requestId));
        }
    }

    private void onPathComplete(Path p, int requestId)
    {
        if (requestId != pathRequestId) // cancelled or superseded (e.g. by a rescue teleport)
            return;

        isCalculatingNewPath = false; // always clear, or one path error would stop repathing forever

        if (!p.error)
        {
            path = p;
            currentWaypoint = 1;
            nodeSearchRetries = 0;
        }
        else if (p.errorLog.Contains("Couldn't find a node close") && nodeSearchRetries < MAX_NODE_SEARCH_RETRIES)
        {
            nodeSearchRetries++;
            stopMoving();
            isCalculatingNewPath = true;
            seeker.StartPath(rb.position, target, p2 => onPathComplete(p2, requestId));
        }
        // other errors: the repeating updatePath retries on the next tick
    }

    private bool isPlayerFartherThanMax()
    {
        return isPlayerFartherThanDistance(maxDistanceFromPlayer);
    }  
    
    private bool isPlayerFartherThanIdeal()
    {
        return isPlayerFartherThanDistance(idealStopDistanceFromPlayer);
    }

    private bool isPlayerFartherThanDistance(float distance)
    {
        return (Vector3.Distance(this.transform.position, PlayerController.instance.transform.position) > distance);
    }
}

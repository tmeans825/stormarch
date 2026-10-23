using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using UnityEngine.Rendering;

public class KiplingBehavior : Character
{    
    private const float TARGET_REACH_DISTANCE = 2f;
    // plates must be physically reached to depress; everything else stops short
    private const float PLATE_REACH_DISTANCE = 0.25f;

    public float nextWaypointDistance = 3f;
    public float swimSlowModifier = 0.5f;
    public float interactAnimTime = 0.2f;
    public bool isWaitingOnPlate = false;

    protected Vector2 target;
    protected float aStarTargetBuffer = 0.0f;
    protected bool reachedEndOfPath = true;
    protected int currentWaypoint = 0;
    protected float updatePathRate = 1.0f;

    private Path path;
    private Seeker seeker;
    private int pathRequestVersion = 0;
    private int movementDirectiveVersion = 0;

    private bool isReturning = false;
    private KiplingController kc;

    private ParticleSystem splash;
    public bool isSwimming = false;

    private Interactable deployedObj;
    private bool hasDeployedReference = false;

    private GameObject spriteObj;
    private GameObject interactKip;
    private Animator interactAnim;

    private bool locked = false;

    private int triggerCount = 0;

    private GameObject player;

    private float stuckCheckInterval = 0.5f;
    private float timeSinceLastStuckCheck = 0.0f;
    private float timeStuck = 0.0f;
    private Vector3 lastPos;
    private BoxCollider2D boxCol;
    private bool isCarrying = false;

    private SortingGroup sortingGroup;

    private bool activelyStopping = false;

    private bool shortCircuit = false;

    private NNConstraint constraint = NNConstraint.None;

    // DEPRECATED: direction smoothing handled in Character.getNewDirection + commit debounce.
    protected int smoothAnimWaypoints = 3;



    [System.NonSerialized]
    public static KiplingBehavior instance;
    [System.NonSerialized]
    public bool exitingVent = false;


    protected override void Awake()
    {

        if (instance == null && this != instance)
            instance = this;
        else
            Destroy(this);

        seeker = GetComponent<Seeker>();
        base.Awake();

        player = PlayerController.instance.gameObject;

        kc = player.GetComponent<KiplingController>();

        splash = transform.GetChild(8).GetComponent<ParticleSystem>();

        spriteObj = transform.GetChild(0).gameObject;
        interactKip = transform.GetChild(9).gameObject;
        interactAnim = interactKip.GetComponent<Animator>();

        boxCol = GetComponent<BoxCollider2D>();

        sortingGroup = GetComponent<SortingGroup>();

        // Constrain the search to walkable nodes only
        constraint.constrainWalkability = true;
        constraint.walkable = true;
        // Constrain the search to only nodes with tag 3 or tag 5
        // The 'tags' field is a bitmask
        constraint.graphMask = (1 << 1); // only Kipling graph

        lastPos = new Vector3(float.MaxValue, float.MaxValue);

        gameObject.SetActive(false);
    }

    protected override void Update()
    {
        runPathfindingCode();

        base.Update();
    }

    private void runPathfindingCode()
    {
        if (!reachedEndOfPath)
        {
            if (path == null || path.vectorPath.Count == 0)
                return;

            if (hasReachedMoveTarget())
            {
                if (!activelyStopping)
                    StartCoroutine(stopMovingCoroutine());
                return;
            }
            else
            {
                reachedEndOfPath = false;
            }

            bool needToCheck = false;
            if (currentWaypoint >= path.vectorPath.Count)
                currentWaypoint = path.vectorPath.Count - 1;

            Vector2 newDirection = ((Vector2)path.vectorPath[currentWaypoint] - rb.position);
            if (movementDirection != newDirection.normalized)
            {
                movementDirection = newDirection;
                movementSpeed = Mathf.Clamp(movementDirection.magnitude, 0.0f, 1.0f);
                movementDirection.Normalize();
                needToCheck = true;
            }

            float distance = Vector2.Distance(rb.position, path.vectorPath[currentWaypoint]);

            if (distance < nextWaypointDistance && currentWaypoint < path.vectorPath.Count - 1)
            {
                currentWaypoint++;
            }

            if (needToCheck)
            {
                checkForMove(); // check for move will move if movementDirection is modified
            }

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
                    Debug.Log("unsticking");
                    StartCoroutine(unstick());

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
    }

    private bool hasReachedMoveTarget()
    {
        // pressure plates need contact to depress, so approach tightly;
        // other interactables stop short so kipling doesn't walk inside them
        float baseReach = TARGET_REACH_DISTANCE;
        if (!isReturning && hasDeployedReference && deployedObj != null && deployedObj.tag.Equals("PressurePlate"))
            baseReach = PLATE_REACH_DISTANCE;

        float reachDistance = Mathf.Max(aStarTargetBuffer, baseReach);
        if (Vector2.Distance((Vector2)transform.position, target) <= reachDistance)
            return true;

        if (path != null && path.vectorPath.Count > 0 && currentWaypoint >= path.vectorPath.Count - 1)
        {
            Vector2 finalWaypoint = path.vectorPath[path.vectorPath.Count - 1];
            return Vector2.Distance(rb.position, finalWaypoint) <= reachDistance;
        }

        return false;
    }

    public bool returnIsReturning()
    {
        return isReturning;
    }

    public bool IsActivelyTargeting(Interactable interactable)
    {
        return interactable != null && hasDeployedReference && deployedObj == interactable && !isReturning && (!reachedEndOfPath || activelyStopping || locked);
    }

    public bool isAccessibleToKipling(GameObject obj)
    {
        return isAccessibleToKipling(obj, transform.position);
    }

    public bool isAccessibleToKipling(GameObject obj, Vector2 startPosition)
    {
        GraphNode node1 = AstarPath.active.GetNearest(obj.transform.position, constraint).node;
        GraphNode node2 = AstarPath.active.GetNearest(startPosition, constraint).node;

        /*List<GraphNode> nodes = new List<GraphNode>();
        nodes.Add(node1);
        nodes.Add(node2);*/

        //return PathUtilities.IsPathPossible(nodes, 1);
        if (node1 == null || node2 == null)
            return false;

        return PathUtilities.IsPathPossible(node1, node2);
    }

    private IEnumerator unstick()
    {
        boxCol.enabled = false;
        yield return new WaitForSeconds(1f);
        boxCol.enabled = true;
    }

    public void startMoving(GameObject obj, bool newDirective)
    {
        timeSinceLastStuckCheck = 0.0f;
        timeStuck = 0.0f;

        if (locked)
            return;

        Vector2 t = obj.transform.position;

        if (obj.tag.Equals("PressurePlate"))
            t = new Vector3(t.x, t.y + 0.8f); // run to top of pressure plate

        if (newDirective)
        {         
            isReturning = false;
            if (isCarrying)
            {
                deployedObj.signalReturn(true);
                isCarrying = false;
            }
            //Debug.Log("Not returning");
        }

        bool targetChanged = target != t || target == Vector2.zero;
        if (targetChanged)
        {
            //Debug.Log("Target doesn't equal t");
            movementDirectiveVersion++;
            activelyStopping = false;
            CancelInvoke("updatePath");
            pathRequestVersion++;
            path = null;
            currentWaypoint = 0;

            if(hasDeployedReference)
            {            
                deployedObj.toggleDeployed(false);
                hasDeployedReference = false;
            }
            reachedEndOfPath = false;
            target = t;
            if (!isReturning)
            {              
                deployedObj = obj.GetComponent<Interactable>();
                deployedObj.toggleDeployed(true);
                hasDeployedReference = true;
                //Debug.Log("Added new object to reference");
            }
           
        }

        if (!reachedEndOfPath)
        {
            if (!IsInvoking("updatePath"))
                InvokeRepeating("updatePath", 0f, updatePathRate);
            //Debug.Log("Path in process");
        }

        //Debug.Log("Started moving to " + target);
    }

    // Called from KiplingController.recallForRespawn during the death-respawn flow.
    // Drops any in-flight pathing/deployed-object reference so the saved state is
    // a clean "kip is back with player" baseline. Caller deactivates the GameObject
    // after this returns.
    public void resetForRespawn()
    {
        CancelInvoke();
        StopAllCoroutines();
        pathRequestVersion++;
        movementDirectiveVersion++;
        isReturning = false;
        activelyStopping = false;
        reachedEndOfPath = true;
        currentWaypoint = 0;
        path = null;
        target = Vector2.zero;
        movementDirection = Vector2.zero;
        if (hasDeployedReference && deployedObj != null)
            deployedObj.toggleDeployed(false);
        hasDeployedReference = false;
        deployedObj = null;
        if (isCarrying)
            isCarrying = false;
    }

    public void playerDied()
    {
        isReturning = true;
        /*spriteObj.SetActive(false);
        interactKip.transform.localScale = new Vector3(-10f, 10f, 10f);
        interactKip.SetActive(true);
        interactAnim.Play("Kipling_Interact_Right");*/
        startMoving(player, false);
    }

    private IEnumerator stopMovingCoroutine()
    {
        int stopVersion = movementDirectiveVersion;
        Interactable stopTarget = deployedObj;

        if (!isReturning) // this is the worst block of code I've written in my life.
        {
            if (stopTarget == null)
                yield break;

            bool stopTargetIsPressurePlate = stopTarget.tag.Equals("PressurePlate");

            activelyStopping = true;
            CancelInvoke("updatePath");
            pathRequestVersion++;

            if (stopTargetIsPressurePlate)
            {
                // coast onto the plate so OnTriggerEnter2D can depress it
                float interactDelay = 0.35f;
                PressurePlate plate = stopTarget.GetComponent<PressurePlate>();
                float coastElapsed = 0f;

                while (coastElapsed < interactDelay && plate != null && !plate.isDepressed())
                {
                    coastElapsed += Time.deltaTime;
                    yield return null;
                }
            }
            else
            {
                // stop immediately on arrival; non-plate targets don't coast/wait
                movementDirection = Vector2.zero;
                checkForMove();
            }

            if (stopVersion != movementDirectiveVersion || stopTarget != deployedObj)
                yield break;

            if (stopTargetIsPressurePlate) // edge case of not depressing it
            {
                if (!stopTarget.GetComponent<PressurePlate>().isDepressed())
                    lerpToPos(target);
            }

            int modifier = 1;
            if (stopTarget.transform.position.x < transform.position.x)
                modifier = -1;

            //
            //if (direction == 0 || direction == 3)
            //    modifier = -1;
            if (stopTargetIsPressurePlate)
            {
                movementDirection = Vector2.zero;
                checkForMove();
            }
            reachedEndOfPath = true;
            locked = true;
            spriteObj.SetActive(false);
            interactKip.transform.localScale = new Vector3(modifier * 10f, 10f, 10f);
            interactKip.SetActive(true);
            interactAnim.Play("Kipling_Interact_Right");

            if (!isAccessibleToKipling(player)) // he always chills out if the player can't be reached
                isWaitingOnPlate = true;

            yield return new WaitForSeconds(interactAnimTime / 2f);
            isCarrying = stopTarget.interact();
            while (isWaitingOnPlate == true)
            {
                if (Vector2.Distance(transform.position, player.transform.position) < 2f || Vector2.Distance(transform.position, player.transform.position) > 60f)
                    isWaitingOnPlate = false;

                yield return null;
            }

            if (shortCircuit)
            {
                shortCircuit = false;
                yield break;             
            }

            yield return new WaitForSeconds(interactAnimTime / 2f);
            
            locked = false;
            spriteObj.SetActive(true);
            interactKip.SetActive(false);
            activelyStopping = false;
        }

        stopMoving();
    }

    public void transferFromOne()
    {
        locked = false;
        spriteObj.SetActive(true);
        interactKip.SetActive(false);
        activelyStopping = false;
        shortCircuit = true; // kill active coroutine
    }


    private void stopMoving()
    {
        if (!isReturning)
        {
            isReturning = true;            
            startMoving(player, false);
        }
        else
        {
            if(!reachedEndOfPath)
                StartCoroutine(reachedEnd());
        }
    }

    public void hitInfestation()
    {
        if (!isReturning)
            stopMoving();
    }

    private IEnumerator reachedEnd()
    {
        locked = true;
        reachedEndOfPath = true;
        yield return new WaitForSeconds(0.3f);
        locked = false;
        if (Vector2.Distance(transform.position, player.transform.position) > 4f)
        {
            startMoving(player, false);
        }
        else
        {
            if (deployedObj != null)
            {
                deployedObj.signalReturn(false);
                isCarrying = false;
            }



            isReturning = false;
            movementDirection = Vector2.zero;
            target = Vector2.zero;
            checkForMove();
            CancelInvoke();

            // check for dead
            if(PlayerController.instance.isDead)
            {
                interactKip.GetComponent<SpriteRenderer>().sortingLayerName = "Overhead";
                int modifier = 1;
                if (direction == 0 || direction == 3)
                    modifier = -1;
                spriteObj.SetActive(false);
                interactKip.transform.localScale = new Vector3(modifier * 10f, 10f, 10f);
                interactKip.SetActive(true);
                interactAnim.Play("Kipling_Interact_Right");
            }
            else
            {
                kc.returnKipling();
                this.gameObject.SetActive(false);
            }        
        }
    }

    private void updatePath()
    {
        if (isReturning)
            target = player.transform.position;
        if (seeker.IsDone())
        {
            int requestVersion = ++pathRequestVersion;
            seeker.StartPath(rb.position, target, p => onPathComplete(p, requestVersion));
        }
    }

    public void refreshPath()
    {
        //seeker.StartPath(rb.position, target, onPathComplete);
        currentWaypoint++;
    }

    private void onPathComplete(Path p, int requestVersion)
    {
        if (requestVersion != pathRequestVersion)
            return;

        if (!p.error)
        {
            path = p;
            currentWaypoint = 0;
        }
    }

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        base.OnTriggerEnter2D(other);
        if (triggerCount == 0)
        {
            if (other.gameObject.layer == 4)
            {
                triggerCount++;
                chestSpriteSheetName = "Kipling_Swimming";
                baseSpeed = originalSpeed * swimSlowModifier;
                audioManager.PlaySound("Splash_Enter");
                splash.Play();
                stopWalkSFX();
                disallowFootsteps = true;

                isSwimming = true;
            }
            else if (other.gameObject.layer == 20)
            {
                if (exitingVent) // ignore if we are outtie OR we aren't even fucking trying to go through the vent
                    return;

                bool fromBelow = true;
                int modifier = -1; // modifier is for checking if we need to go through the vent at all - depends on directionality of kipling

                if (transform.position.y < other.gameObject.transform.position.y)
                {
                    fromBelow = false;
                    modifier = 1;
                }

                float check1 = path.vectorPath[currentWaypoint].y * modifier;
                float check2 = other.gameObject.transform.position.y * modifier;

                if (check1 <= check2) // only try to enter vent if next waypoint is through vent
                    return;

                //Debug.Log("entered vent");
                triggerCount++;
                sortingGroup.sortingLayerName = "Default";
                sortingGroup.sortingOrder = -1;

                other.gameObject.transform.parent.gameObject.GetComponent<Vent>().startAnim(fromBelow, this.gameObject);
            }
        }
    }

    protected override void OnTriggerExit2D(Collider2D other)
    {
        base.OnTriggerExit2D(other);
        if (triggerCount == 1)
        {
            if (other.gameObject.layer == 4)
            {
                triggerCount--;
                chestSpriteSheetName = "Kipling";
                baseSpeed = originalSpeed;
                audioManager.PlaySound("Splash_Exit");
                splash.Play();

                disallowFootsteps = false;
                isSwimming = false;
            }
            else if (other.gameObject.layer == 20)
            {
                if (!exitingVent)
                    return;

                //Debug.Log("exited vent");
                triggerCount--;
                sortingGroup.sortingLayerName = "Dynamic Depth";
                sortingGroup.sortingOrder = 0;
                exitingVent = false;
            }
        }
    }

    // Not currently used:
    public void lerpToPos(Vector3 endValue)
    {
        StartCoroutine(Lerp(endValue));
    }

    IEnumerator Lerp(Vector3 endValue)
    {
        float lerpDuration = 0.15f;
        Vector3 startValue = transform.position;
        float timeElapsed = 0;
        while (timeElapsed < lerpDuration)
        {
            transform.position = Vector3.Lerp(startValue, endValue, timeElapsed / lerpDuration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = endValue;
    }
}


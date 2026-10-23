using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

public class Critter : CombatChar
{
    [Header("Critter")]
    [Tooltip("0 = Anglerhare, 1 = Bird")]
    [Range(0, 1)]
    public int critterType = 0;
    public float checkRadius = 15f;
    public float nextWaypointDistance = 3f;
    public bool critterMoving = false;
    public float hareHomeRadius = 5f;
    [Tooltip("Anglerhare flee destinations stay within this radius of the spawn position")]
    public float fleeRadius = 10f;
    [Tooltip("Speed and animation multiplier while an anglerhare idly wanders")]
    public float wanderSpeedModifier = 0.5f;
    public float minimumIdleCheck = 3f;
    public float maximumIdleCheck = 10f;
    public bool doNotFlee = false;

    protected bool reachedEndOfPath = false;
    protected int currentWaypoint = 0;

    private const int FLEE_CANDIDATE_COUNT = 12;
    private const float FLEE_MIN_TRAVEL = 3f;
    private const float FLEE_PATH_CLEARANCE = 2f;
    private const float FINAL_WAYPOINT_DISTANCE = 0.5f;
    private const float MAX_MOVE_SECONDS = 6f;
    private const float PATH_RETRY_DELAY = 0.5f;

    private int ebl;
    private int bl;
    private int kl;
    private int pb;
    private int eb;
    private int mask;
    private Path path;
    private Seeker seeker;
    private Vector3 runAwayDirection;
    private GameObject anglerhareLight;
    private Vector3 lightLeftPos = new Vector3(-0.605f, -0.107f);
    private Vector3 lightRightPos = new Vector3(0.528f, -0.107f);
    private bool critterFleeing = false;
    private float timeSinceLastMove = 0.0f;
    private bool originalAnglerhareLightBehaviorEnabled;
    private Vector3 originalAnglerhareLightPos;
    private readonly List<Vector2> threatPositions = new List<Vector2>();
    private float moveStartTime = 0.0f;
    private float nextFleeAllowedTime = 0.0f;

    protected override void Awake()
    {
        if(critterType == 0)
            anglerhareLight = transform.GetChild(8).gameObject;
        base.Awake();
    }
    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
        ebl = 1 << LayerMask.NameToLayer("EnemyBlockingLayer");
        bl = 1 << LayerMask.NameToLayer("BlockingLayer");
        kl = 1 << LayerMask.NameToLayer("Kipling");
        pb = 1 << LayerMask.NameToLayer("PlayerBullet");
        mask = ebl | bl | kl | pb | eb;

        seeker = GetComponent<Seeker>();

        if (critterType == 0)
        {
            originalAnglerhareLightPos = anglerhareLight.transform.localPosition;
            originalAnglerhareLightBehaviorEnabled = anglerhareLight.GetComponent<LightBehavior>().enabled;
        }
    }

    // Update is called once per frame
    protected override void Update()
    {
        // A dead anglerhare must not wander (setWalking would slow its death anim). Birds keep running so a
        // corpse still schedules its despawn.
        if (critterType == 0 && isDead)
        {
            base.Update();
            return;
        }

        if (!critterFleeing)
            checkForPeople();

        pathfindingCode();

        base.Update();
    }

    private void pathfindingCode()
    {
        if (critterMoving)
        {
            if (critterType == 0) // anglerhare movement
            {
                // Checked before the null-path return so a hare wedged on geometry, a lost path callback, or a save
                // loaded mid-move (critterMoving is saved, path isn't) all recover instead of freezing.
                if (Time.time - moveStartTime > MAX_MOVE_SECONDS)
                {
                    stopMoving();
                    return;
                }

                if (path == null)
                    return;

                if (currentWaypoint >= path.vectorPath.Count)
                {
                    stopMoving();
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

                // Tight tolerance on the last waypoint so the hare reaches its destination instead of stopping 3u short
                float waypointDistance = currentWaypoint == path.vectorPath.Count - 1 ? FINAL_WAYPOINT_DISTANCE : nextWaypointDistance;
                if (distance < waypointDistance)
                {
                    currentWaypoint++;
                }
            }

            else // roosterfinch movement
            {
                movementDirection = (runAwayDirection);
                movementSpeed = Mathf.Clamp(movementDirection.magnitude, 0.0f, 1.0f);
                movementDirection.Normalize();
            }

            checkForMove(); // check for move will move if movementDirection is modified
        }
    }

    private void checkForPeople()
    {
        Collider2D[] colls = Physics2D.OverlapCircleAll(transform.position, checkRadius, mask);
        collectThreats(colls);

        if (threatPositions.Count != 0 && !doNotFlee)
        {
            if (critterType == 0)
            {
                if (Time.time >= nextFleeAllowedTime)
                    beginFlee();
            }
            else
            {
                critterFleeing = true;
                baseSpeed = originalSpeed;
                startMoving(nearestThreat(transform.position));
            }
        }
        else if(critterType == 0 && !critterMoving) // if the critter is a rabbit and no one is around, progress towards idle moving
        {
            timeSinceLastMove += Time.deltaTime;
            checkForIdleMove();
        }

    }

    // Only characters and player bullets are threats. Crates, chests and camps share BlockingLayer but aren't.
    private void collectThreats(Collider2D[] colls)
    {
        threatPositions.Clear();
        for (int i = 0; i < colls.Length; i++)
        {
            bool isBullet = ((1 << colls[i].gameObject.layer) & pb) != 0;
            if (isBullet || colls[i].GetComponentInParent<Character>() != null)
                threatPositions.Add(colls[i].transform.position);
        }
    }

    // Anglerhares flee to a point inside their home circle; birds fly straight away from threatPosition.
    // Also called by InfestationBehavior on every particle hit.
    public void startMoving(Vector3 threatPosition)
    {
        if (critterType == 0)
        {
            if (doNotFlee || critterFleeing) // don't re-roll a flee that's already underway
                return;

            threatPositions.Clear();
            threatPositions.Add(threatPosition);
            beginFlee();
            return;
        }

        runAwayDirection = (transform.position - threatPosition).normalized;
        updatePath(transform.position + runAwayDirection * checkRadius);
        shadowCaster.enabled = false;
    }

    private void beginFlee()
    {
        critterFleeing = true;
        setWalking(false);
        updatePath(pickFleePoint());
    }

    // Samples destinations inside the flee circle and takes the one farthest from any threat, preferring
    // ones that don't mean running through a threat. Straight-line heuristic: the A* route can still bend.
    private Vector2 pickFleePoint()
    {
        Vector2 home = originalSpawnPosition;
        Vector2 pos = rb.position;
        Vector2 bestSafe = Vector2.zero;
        Vector2 bestAny = Vector2.zero;
        float bestSafeScore = -1f;
        float bestAnyScore = -1f;

        for (int i = 0; i < FLEE_CANDIDATE_COUNT; i++)
        {
            Vector2 candidate = home + Random.insideUnitCircle * fleeRadius;
            if (AstarPath.active != null)
            {
                NNInfo nearestNode = AstarPath.active.GetNearest(candidate, NNConstraint.Default);
                if (nearestNode.node == null)
                    continue;
                candidate = nearestNode.position;
            }

            if (Vector2.Distance(candidate, home) > fleeRadius || Vector2.Distance(candidate, pos) < FLEE_MIN_TRAVEL)
                continue;

            float score = Vector2.Distance(candidate, nearestThreat(candidate));
            if (score > bestAnyScore)
            {
                bestAnyScore = score;
                bestAny = candidate;
            }
            if (score > bestSafeScore && !crossesThreat(pos, candidate))
            {
                bestSafeScore = score;
                bestSafe = candidate;
            }
        }

        if (bestSafeScore >= 0f)
            return bestSafe;
        if (bestAnyScore >= 0f)
            return bestAny;

        // Nothing usable was sampled. Head for the edge opposite the nearest threat, and throttle the next flee
        // in case the hare is already standing there.
        nextFleeAllowedTime = Time.time + PATH_RETRY_DELAY;
        Vector2 away = home - nearestThreat(home);
        if (away == Vector2.zero)
        {
            float angle = Random.Range(0.0f, 2 * Mathf.PI);
            away = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }
        return home + away.normalized * fleeRadius;
    }

    private Vector2 nearestThreat(Vector2 point)
    {
        Vector2 nearest = threatPositions[0];
        float nearestDistance = Vector2.Distance(point, nearest);
        for (int i = 1; i < threatPositions.Count; i++)
        {
            float distance = Vector2.Distance(point, threatPositions[i]);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = threatPositions[i];
            }
        }
        return nearest;
    }

    private bool crossesThreat(Vector2 from, Vector2 to)
    {
        for (int i = 0; i < threatPositions.Count; i++)
        {
            Vector2 threat = threatPositions[i];
            if (Vector2.Distance(from, threat) < FLEE_PATH_CLEARANCE)
            {
                // Already on top of the hare, so any destination on the far side counts as clear
                if (Vector2.Dot(to - from, threat - from) >= 0f)
                    return true;
            }
            else if (distanceToSegment(threat, from, to) < FLEE_PATH_CLEARANCE)
            {
                return true;
            }
        }
        return false;
    }

    private static float distanceToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = ab.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot(point - a, ab) / ab.sqrMagnitude) : 0f;
        return Vector2.Distance(point, a + ab * t);
    }

    private void checkForIdleMove()
    {
        if(timeSinceLastMove > Random.Range(minimumIdleCheck, maximumIdleCheck))
        {
            float angle = Random.Range(0.0f, 2 * Mathf.PI); // Get a random angle between 0 and 2π
            Vector2 randomDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)); // Convert the angle to a vector direction
            Vector2 randomPoint = originalSpawnPosition + hareHomeRadius * (Vector3)randomDirection; // Multiply the direction by the radius and add to the starting vector to get the final point
            updatePath(randomPoint);
            setWalking(true);
        }
    }

    // Reset first: slowAnimAndSpeed multiplies, so applying it on top of an earlier slow would compound.
    private void setWalking(bool walking)
    {
        resumeOriginalAnimAndSpeed();
        if (walking)
            slowAnimAndSpeed(wanderSpeedModifier);
    }

    private void stopMoving()
    {
        reachedEndOfPath = true;
        critterMoving = false;
        critterFleeing = false;
        movementDirection = Vector2.zero;
        timeSinceLastMove = 0.0f;
        checkForMove();
    }

    private void updatePath(Vector3 target)
    {
        critterMoving = true;
        if (critterType == 0)
        {
            // Drop the consumed path so pathfindingCode waits for this request instead of reading the old one as
            // "arrived". StartPath cancels any pending request and canceled requests never call back.
            path = null;
            currentWaypoint = 0;
            reachedEndOfPath = false;
            moveStartTime = Time.time;
            seeker.StartPath(rb.position, target, onPathComplete);
        }
        else
            Destroy(this.gameObject, 5f);
    }

    private void onPathComplete(Path p)
    {
        if (!p.error)
        {
            path = p;
            currentWaypoint = 0;
        }
        else if (critterMoving)
        {
            // Back off before retrying a flee so an off-graph hare doesn't re-request every other frame.
            // A failed wander already waits on the idle timer.
            if (critterFleeing)
                nextFleeAllowedTime = Time.time + PATH_RETRY_DELAY;
            stopMoving();
        }
    }

    protected override void triggerDeath(int d)
    {
        audioManager.PlaySound("Death_Rattle");
        if (critterType == 0)
        {
            setWalking(false); // killed mid-wander shouldn't play the death anim at walk speed

            if(d == 3)
                anglerhareLight.transform.localPosition = lightLeftPos;
            else
                anglerhareLight.transform.localPosition = lightRightPos;

            anglerhareLight.GetComponent<LightBehavior>().enabled = true;
        }
        base.triggerDeath(d);
    }

    public override void respawnFromDeath()
    {
        // Restore the default before base re-applies the idle facing (setAnim -> signalDirectionChange),
        // so a left/right respawn facing wins and up/down keeps the prefab default.
        if (critterType == 0 && anglerhareLight != null)
            anglerhareLight.transform.localPosition = originalAnglerhareLightPos;

        base.respawnFromDeath();

        path = null;
        currentWaypoint = 0;
        reachedEndOfPath = true;
        critterMoving = false;
        critterFleeing = false;
        movementDirection = Vector2.zero;
        movementSpeed = 0f;
        runAwayDirection = Vector3.zero;
        timeSinceLastMove = 0.0f;
        threatPositions.Clear();
        moveStartTime = 0.0f;
        nextFleeAllowedTime = 0.0f;
        setWalking(false);

        if (seeker != null)
            seeker.enabled = true;

        if (critterType == 0 && anglerhareLight != null)
        {
            LightBehavior lightBehavior = anglerhareLight.GetComponent<LightBehavior>();
            if (lightBehavior != null)
                lightBehavior.enabled = originalAnglerhareLightBehaviorEnabled;
        }
    }

    protected override void checkDirection()
    {

        if (movementDirection.x <= 0 && direction != 3)
            direction = 3; // left

        else if (movementDirection.x > 0 && direction != 1)
            direction = 1;

    }

    // Light follows the committed facing, not checkDirection's candidate — Character.move() debounces
    // the candidate before committing, so moving the light there made it flip ahead of the sprite.
    protected override void signalDirectionChange(int d)
    {
        base.signalDirectionChange(d);
        if (critterType != 0)
            return;

        if (d == 3)
            anglerhareLight.transform.localPosition = lightLeftPos;
        else if (d == 1)
            anglerhareLight.transform.localPosition = lightRightPos;
    }

    // temporary test scripts ---------------------------------------------------------------------

    private void OnDrawGizmos()
    {
        if (critterType == 0)
        {
            // Home circles are anchored to the spawn position once play starts
            Vector3 home = Application.isPlaying ? originalSpawnPosition : transform.position;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(home, fleeRadius);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(home, hareHomeRadius);
        }

        Gizmos.DrawWireSphere(transform.position, checkRadius);
    }
}

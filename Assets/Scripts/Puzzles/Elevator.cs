using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Purpose: a car that rides between two stops, carrying every Character standing on it.
// Pressed through PuzzleButtonBehavior -> togglePower (in-car button, needs the player aboard).
// On load the car parks at the top if the player is inside topZone, otherwise at the bottom.
// Riders are never parented: each gets the car's velocity as Character.carrierVelocity every
// physics step, composed with their own locomotion, so the player can still walk on the car and
// the persistent player is never reparented.
public class Elevator : PuzzlePiece
{
    private const int BOTTOM_STOP = 0;
    private const int TOP_STOP = 1;
    private const int MAX_OVERLAPS = 32;
    private const float GRAPH_BOUNDS_PADDING = 1f;
    private const float MIN_SHAKE_FRACTION = 0.35f; // keep a faint rattle while easing in/out

    [ES3NonSerializable] public Transform bottomStop;
    [ES3NonSerializable] public Transform topStop;
    [ES3NonSerializable] public BoxCollider2D platformTrigger; // who counts as aboard
    // Top is on unless parked at the top (exit); bottom is on unless parked at the bottom (entry).
    // Both are on while travelling (not during the lurch) so riders are boxed in.
    [ES3NonSerializable] public Collider2D topBlocker;
    [ES3NonSerializable] public Collider2D bottomBlocker;
    [ES3NonSerializable] public Transform carVisual; // only the visual jitters, never colliders/riders
    [ES3NonSerializable] public SubLocationZone topZone; // car parks at the top when the player loads inside this zone, else bottom

    [ES3NonSerializable] public float maxSpeed = 40f;
    [ES3NonSerializable] public float acceleration = 12f;
    [ES3NonSerializable] public float minSpeed = 1.5f;
    [ES3NonSerializable] public float lurchTime = 0.5f;
    [ES3NonSerializable] public float lurchDip = 0.1f;
    [ES3NonSerializable] public float maxShake = 0.06f;
    [ES3NonSerializable] public float shakeDelay = 0.05f;

    private int currentStop = BOTTOM_STOP; // derived from the player's position on load, never saved

    private enum Phase { Idle, Lurch, Travel }

    private class Rider
    {
        public Character character;
        public Rigidbody2D body;
        public RigidbodyInterpolation2D originalInterpolation;
    }

    // Child bodies (e.g. the in-car Button keeps its own Rigidbody2D so mouse/trigger messages
    // reach it) are moved with the car in the same step, so they never trail it and shove riders.
    private class AttachedBody
    {
        public Rigidbody2D body;
        public Vector2 offset;
    }

    private Phase phase = Phase.Idle;
    private Rigidbody2D rb;
    private AudioManager am;
    private SpriteRenderer carSprite;
    private Vector3 carVisualBasePos;
    private List<Rider> riders = new List<Rider>();
    private List<AttachedBody> attachedBodies = new List<AttachedBody>();
    private Collider2D[] overlapResults = new Collider2D[MAX_OVERLAPS];

    private int targetStop;
    private Vector2 departurePos;
    private Vector2 travelDir;
    private float travelDistance;
    private float traveled;
    private float lurchElapsed;
    private float speedFraction;
    private float lastShake;
    private Bounds departureBounds;
    private bool subscribed = false;

    void Awake()
    {
        objectType = 3; // light = 0, bridge = 1, door = 2, elevator = 3
        rb = GetComponent<Rigidbody2D>();
        am = GetComponent<AudioManager>();
        carSprite = carVisual.GetComponent<SpriteRenderer>();
        carVisualBasePos = carVisual.localPosition;

        foreach (Rigidbody2D body in GetComponentsInChildren<Rigidbody2D>(true))
        {
            if (body != rb)
                attachedBodies.Add(new AttachedBody { body = body, offset = (Vector2)body.transform.position - (Vector2)transform.position });
        }
    }

    void OnEnable()
    {
        subscribe();
    }

    // Retried here: OnEnable can run before GameEvents.current is assigned.
    void Start()
    {
        subscribe();
        snapToStop(currentStop);
    }

    void OnDisable()
    {
        // Scene unload / title return mid-ride: settle quietly so the persistent player is never
        // left frozen, interpolating, or mid-shaft.
        if (phase != Phase.Idle)
            finishRide(false);

        if (subscribed && GameEvents.current != null)
        {
            GameEvents.current.onSceneTransitionComplete -= parkForPlayer;
            GameEvents.current.onSaveStarted -= saveStarted;
        }
        subscribed = false;
    }

    private void subscribe()
    {
        if (subscribed || GameEvents.current == null)
            return;

        GameEvents.current.onSceneTransitionComplete += parkForPlayer;
        GameEvents.current.onSaveStarted += saveStarted;
        subscribed = true;
    }

    // Fires after scene data (and the player's position) is loaded: meet the player at their landing.
    private void parkForPlayer()
    {
        if (phase != Phase.Idle)
            return;

        bool playerUpTop = topZone != null && PlayerController.instance != null
            && topZone.containsPoint(PlayerController.instance.transform.position);

        currentStop = playerUpTop ? TOP_STOP : BOTTOM_STOP;
        snapToStop(currentStop);
    }

    // Fires before SaveManager captures anything, so a save never records a mid-shaft position.
    private void saveStarted()
    {
        // Nobody is captured during the lurch, so grab them now or the car would settle without them.
        if (phase == Phase.Lurch)
            collectRiders();

        if (phase != Phase.Idle)
            finishRide(true);
    }

    // In-car button: ride to the other stop. Only with the player aboard, so the car never
    // leaves without them.
    public override bool togglePower(bool isOn)
    {
        if (!canDepart() || !isPlayerAboard())
            return false;

        depart(currentStop == BOTTOM_STOP ? TOP_STOP : BOTTOM_STOP);
        return true;
    }

    private bool canDepart()
    {
        if (phase != Phase.Idle)
            return false;

        if (GameEvents.current != null && GameEvents.current.enemiesInCombat > 0)
            return false;

        PlayerController player = PlayerController.instance;
        if (player == null || player.checkForDead() || player.isSliding)
            return false;

        // Recalling a deployed Kipling mid-carry would strand the carried Interactable.
        if (KiplingBehavior.instance != null && KiplingBehavior.instance.gameObject.activeInHierarchy)
            return false;

        return true;
    }

    private bool isPlayerAboard()
    {
        Rigidbody2D playerBody = PlayerController.instance.GetComponent<Rigidbody2D>();
        return playerBody != null && platformTrigger.OverlapPoint(playerBody.position);
    }

    private void depart(int stop)
    {
        targetStop = stop;
        departurePos = rb.position;
        Vector2 toTarget = getStopPosition(stop) - departurePos;
        travelDistance = toTarget.magnitude;
        travelDir = travelDistance > Mathf.Epsilon ? toTarget / travelDistance : Vector2.up;
        traveled = 0f;
        lurchElapsed = 0f;
        speedFraction = 1f;
        departureBounds = getCarBounds();

        // Riders are captured and blockers closed only once the lurch ends, so followers can catch up.
        am.PlaySound("Move");
        EventManager.instance.shakeScreenSmall();

        phase = Phase.Lurch;
    }

    // Lurch is over: capture whoever is aboard now and box them in. If the player stepped off
    // during the lurch the car stays parked rather than leaving without them.
    private void beginTravel()
    {
        if (!isPlayerAboard())
        {
            cancelDeparture();
            return;
        }

        collectRiders();
        applyBlockers(true, true);
        phase = Phase.Travel;
    }

    private void cancelDeparture()
    {
        placeCar(getStopPosition(currentStop));
        am.StopSound("Move");
        phase = Phase.Idle;
    }

    private void collectRiders()
    {
        riders.Clear();

        ContactFilter2D filter = new ContactFilter2D();
        filter.NoFilter();
        int count = platformTrigger.OverlapCollider(filter, overlapResults);

        for (int i = 0; i < count; i++)
        {
            Character character = overlapResults[i].GetComponentInParent<Character>();
            if (character == null || character.checkForDead() || !character.gameObject.activeInHierarchy)
                continue;

            Rigidbody2D body = character.GetComponent<Rigidbody2D>();
            if (body == null || isRider(body) || !platformTrigger.OverlapPoint(body.position))
                continue;

            riders.Add(new Rider
            {
                character = character,
                body = body,
                originalInterpolation = body.interpolation
            });

            body.interpolation = RigidbodyInterpolation2D.Interpolate; // smooth at cruise speed
        }
    }

    private bool isRider(Rigidbody2D body)
    {
        foreach (Rider rider in riders)
            if (rider.body == body)
                return true;

        return false;
    }

    void FixedUpdate()
    {
        if (phase == Phase.Idle)
            return;

        // Dead or deactivated riders are left behind.
        for (int i = riders.Count - 1; i >= 0; i--)
        {
            Rider rider = riders[i];
            if (rider.body == null || !rider.body.gameObject.activeInHierarchy || rider.character.checkForDead())
            {
                releaseRider(rider);
                riders.RemoveAt(i);
            }
        }

        if (phase == Phase.Lurch)
        {
            lurchElapsed += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(lurchElapsed / lurchTime);

            // Shudder and sag against the direction of travel before pulling away.
            moveCarTo(departurePos - (travelDir * (Mathf.Sin(t * Mathf.PI) * lurchDip)));

            if (lurchElapsed >= lurchTime)
                beginTravel();

            return;
        }

        // Trapezoid profile by distance: ramp up from the start, brake into the stop, cruise between.
        float remaining = travelDistance - traveled;
        float speed = Mathf.Min(maxSpeed, Mathf.Sqrt(2f * acceleration * traveled), Mathf.Sqrt(2f * acceleration * remaining));
        speed = Mathf.Max(minSpeed, speed);
        speedFraction = speed / maxSpeed;

        traveled = Mathf.Min(travelDistance, traveled + (speed * Time.fixedDeltaTime));

        if (traveled >= travelDistance)
            finishRide(true);
        else
            moveCarTo(departurePos + (travelDir * traveled));
    }

    void Update()
    {
        if (phase == Phase.Idle)
            return;

        if (Time.time > (lastShake + shakeDelay))
        {
            float amplitude = maxShake * Mathf.Max(MIN_SHAKE_FRACTION, speedFraction);
            carVisual.localPosition = carVisualBasePos + new Vector3(Random.Range(-amplitude, amplitude), 0f, 0f);
            lastShake = Time.time;
        }
    }

    // Riders get the car's velocity as a carrier velocity that Character composes with its own
    // locomotion, so they keep walking and still collide normally against the moving blockers.
    private void moveCarTo(Vector2 pos)
    {
        Vector2 carVelocity = (pos - rb.position) / Time.fixedDeltaTime;
        rb.MovePosition(pos);

        foreach (AttachedBody attached in attachedBodies)
            attached.body.MovePosition(pos + attached.offset);

        foreach (Rider rider in riders)
            rider.character.carrierVelocity = carVelocity;
    }

    private void releaseRider(Rider rider)
    {
        if (rider.character != null)
            rider.character.carrierVelocity = Vector2.zero;

        if (rider.body != null)
            rider.body.interpolation = rider.originalInterpolation;
    }

    // Single cleanup path for arrival, saves and teardown. withEffects is false on disable,
    // when audio/camera/A* may already be gone.
    private void finishRide(bool withEffects)
    {
        Vector2 target = getStopPosition(targetStop);
        Vector2 carPosBeforeSettle = rb.position;

        placeCar(target);

        // The car jumps to the stop here (save/teardown), so carry riders the remaining distance.
        Vector2 remaining = target - carPosBeforeSettle;
        foreach (Rider rider in riders)
        {
            if (rider.body != null)
            {
                Vector2 riderPos = rider.body.position + remaining;
                rider.body.position = riderPos;
                rider.body.transform.position = riderPos;
            }
            releaseRider(rider);
        }
        riders.Clear();

        currentStop = targetStop;
        phase = Phase.Idle;
        applyBlockers(currentStop == BOTTOM_STOP, currentStop == TOP_STOP);

        if (!withEffects)
            return;

        am.StopSound("Move");
        am.PlaySound("Stop");
        EventManager.instance.shakeScreenSmall();

        updateGraphs(departureBounds);
        updateGraphs(getCarBounds());
    }

    private void snapToStop(int stop)
    {
        placeCar(getStopPosition(stop));

        applyBlockers(stop == BOTTOM_STOP, stop == TOP_STOP);
        updateGraphs(getCarBounds());
    }

    // Set positions directly (not MovePosition) so a save or query that runs right now reads them.
    private void placeCar(Vector2 pos)
    {
        rb.position = pos;
        transform.position = pos;
        carVisual.localPosition = carVisualBasePos;

        foreach (AttachedBody attached in attachedBodies)
            attached.body.position = pos + attached.offset;
    }

    private void applyBlockers(bool topOn, bool bottomOn)
    {
        topBlocker.enabled = topOn;
        bottomBlocker.enabled = bottomOn;
    }

    private Vector2 getStopPosition(int stop)
    {
        return (stop == TOP_STOP ? topStop : bottomStop).position;
    }

    // Renderer bounds track the transform immediately, unlike collider bounds, and cover both blockers.
    private Bounds getCarBounds()
    {
        Bounds bounds = carSprite.bounds;
        bounds.Expand(GRAPH_BOUNDS_PADDING);
        return bounds;
    }

    private void updateGraphs(Bounds bounds)
    {
        if (AstarPath.active != null)
            AstarPath.active.UpdateGraphs(bounds);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (platformTrigger != null && !platformTrigger.isTrigger)
            Debug.LogWarning($"[Elevator:{name}] platformTrigger must be a trigger.", this);

        if ((topBlocker != null && topBlocker.isTrigger) || (bottomBlocker != null && bottomBlocker.isTrigger))
            Debug.LogWarning($"[Elevator:{name}] blockers must be solid (non-trigger) colliders.", this);

        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (body != null && body.bodyType != RigidbodyType2D.Kinematic)
            Debug.LogWarning($"[Elevator:{name}] root Rigidbody2D must be Kinematic.", this);

        // A child with a static/dynamic body (e.g. the Button prefab's default static Rigidbody2D)
        // is teleported by its transform, so its colliders trail the car by a physics step and
        // shove riders. Kinematic children are driven in lockstep by moveCarTo.
        foreach (Rigidbody2D childBody in GetComponentsInChildren<Rigidbody2D>(true))
            if (childBody != body && childBody.bodyType != RigidbodyType2D.Kinematic)
                Debug.LogWarning($"[Elevator:{name}] child '{childBody.name}' Rigidbody2D must be Kinematic so the car can drive it.", childBody);

        if (topZone == null)
            Debug.LogWarning($"[Elevator:{name}] topZone is unset - the car will always park at the bottom on load.", this);
    }
#endif
}

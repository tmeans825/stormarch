using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bridge : PuzzlePiece
{
    private const int COLLIDER_ENDS_CHILD_INDEX = 0;
    private const int LIGHT_CHILD_INDEX = 1;
    private const int TOP_WALKWAY_COLLIDERS_CHILD_INDEX = 2;
    private const int WALKWAY_CHILD_INDEX = 3;
    private const int BOTTOM_CHILD_INDEX = 4;
    private const int MASK_CHILD_INDEX = 5;

    private const float END_Y_OFFSET = -0.397f;
    // Negative rail margins pull the walkway rail colliders inward from the shore anchors,
    // leaving a buffer at either end so the player can round the corner onto the deck.
    private const float RAIL_TOP_MARGIN = -0.176f;
    private const float RAIL_BOTTOM_MARGIN = -0.278f;
    private const float TOP_CAP_OFFSET = -0.409f;
    private const float BOTTOM_CAP_OFFSET = 0.333f;
    // Root-local slack when fitting the reveal mask / water hazard to the near-anchor..endY gap,
    // the retract rest position, and the light. Hand-tuned in-editor like the RAIL_*/CAP offsets.
    private const float START_Y_OFFSET = 0f;
    private const float MASK_TOP_MARGIN = 0f;
    private const float MASK_BOTTOM_MARGIN = 0f;
    private const float WATER_TOP_MARGIN = 0f;
    private const float WATER_BOTTOM_MARGIN = 0f;
    private const float LIGHT_Y_OFFSET = 0f;

    public int powerRequired = 1;
    public float movementSpeed = 2f;
    public float startY;
    public float endY;
    public float maxShake = 0.2f;
    public float shakeDelay = 0.1f;
    //public Collider2D bounds;

    private GameObject onLight;
    private GameObject walkway;
    private GameObject colliderEnds;
    private AudioManager topAudio;
    private AudioManager bottomAudio;
    private Collider2D waterTrigger;
    private float startX;
    private bool isMoving = false;
    private ParticleSystem ps;
    private Animator gearAnimator;
    private BoxCollider2D topCollider;
    private SpriteMask bridgeMask;
    private Transform maskTransform;
    private BoxCollider2D waterBox;
    private List<GameObject> onBridge;

    [ES3Serializable]
    bool extended = false;

    private int powerReceived = 0;

    public bool isVertical = true;

    // Editor-only opt-in: logs why the Move/Stop SFX may be silent (muted flag, missing
    // Sound entry, listener distance). Left off by default so it never spams the console.
    public bool debugAudio = false;

    // Start is called before the first frame update
    void Start()
    {
        onBridge = new List<GameObject>();

        objectType = 1; // light = 0, bridge = 1
        onLight = transform.GetChild(LIGHT_CHILD_INDEX).gameObject;
        walkway = transform.GetChild(WALKWAY_CHILD_INDEX).gameObject;
        colliderEnds = transform.GetChild(COLLIDER_ENDS_CHILD_INDEX).gameObject;
        waterTrigger = GetComponent<Collider2D>();
        waterBox = waterTrigger as BoxCollider2D;
        topCollider = walkway.GetComponent<BoxCollider2D>();

        Transform maskChild = transform.GetChild(MASK_CHILD_INDEX);
        maskTransform = maskChild;
        bridgeMask = maskChild.GetComponent<SpriteMask>();

        topAudio = transform.GetChild(TOP_WALKWAY_COLLIDERS_CHILD_INDEX).GetComponent<AudioManager>();
        bottomAudio = transform.GetChild(BOTTOM_CHILD_INDEX).GetComponent<AudioManager>();

        ps = transform.GetChild(BOTTOM_CHILD_INDEX).transform.GetChild(0).GetComponent<ParticleSystem>();
        gearAnimator = transform.GetChild(TOP_WALKWAY_COLLIDERS_CHILD_INDEX).transform.GetChild(0).GetComponent<Animator>();
        
        startX = walkway.transform.localPosition.x;

        autoFitColliders();

        if(extended)
        {
            togglePower(true);
        }

        //if (transform.rotation.z == 90 || transform.rotation.z == -90)
            //isVertical = false;

    }

    private void autoFitColliders()
    {
        // Runs for both orientations. All fitting is root-local Y math + world-AABB graph bounds,
        // which is correct for the sideways bridge because it is the vertical prefab with only its
        // *root* rotated -90 under a uniform (1,1,1) root scale (no skew) -- NOT general rotation.
        bool hasGraphBounds = tryGetAutoFitGraphBounds(out Bounds graphBounds);

        Transform topWalkwayColliders = transform.GetChild(TOP_WALKWAY_COLLIDERS_CHILD_INDEX);
        Transform bottom = transform.GetChild(BOTTOM_CHILD_INDEX);

        float topAnchorY = topWalkwayColliders.localPosition.y;
        float bottomAnchorY = bottom.localPosition.y;

        endY = bottomAnchorY + END_Y_OFFSET;

        // Deck travel is driven by the two shore anchors: it retracts to the near anchor and
        // deploys to endY (far anchor). Moving either anchor resizes the span. Snap the retracted
        // deck to startY so it hides at the near shore (no mid-gap sliver).
        startY = topAnchorY + START_Y_OFFSET;
        if (!extended)
            walkway.transform.localPosition = new Vector3(startX, startY, 0f);

        fitSideRailColliders(topWalkwayColliders, topAnchorY, bottomAnchorY);
        fitEndCapColliders(topAnchorY, bottomAnchorY);
        fitMask(topAnchorY);
        fitWaterTrigger(topAnchorY);
        fitLight(topAnchorY);

        if (tryGetAutoFitGraphBounds(out Bounds fittedBounds))
        {
            if (hasGraphBounds)
                graphBounds.Encapsulate(fittedBounds);
            else
                graphBounds = fittedBounds;

            updateGraphs(graphBounds);
        }
    }

    private void fitSideRailColliders(Transform topWalkwayColliders, float topAnchorY, float bottomAnchorY)
    {
        BoxCollider2D[] railColliders = topWalkwayColliders.GetComponents<BoxCollider2D>();
        float railTopRootY = topAnchorY + RAIL_TOP_MARGIN;
        float railBottomRootY = bottomAnchorY - RAIL_BOTTOM_MARGIN;
        float railCenterRootY = (railTopRootY + railBottomRootY) * 0.5f;
        float railLengthRootY = Mathf.Abs(railTopRootY - railBottomRootY);

        foreach (BoxCollider2D railCollider in railColliders)
        {
            float scaleY = railCollider.transform.localScale.y;
            if (Mathf.Abs(scaleY) <= Mathf.Epsilon)
                continue;

            Vector2 size = railCollider.size;
            size.y = railLengthRootY / Mathf.Abs(scaleY);
            railCollider.size = size;

            Vector2 offset = railCollider.offset;
            offset.y = rootYToColliderOffsetY(railCollider, railCenterRootY);
            railCollider.offset = offset;
        }
    }

    private void fitEndCapColliders(float topAnchorY, float bottomAnchorY)
    {
        BoxCollider2D[] endCapColliders = colliderEnds.GetComponents<BoxCollider2D>();
        if (endCapColliders.Length < 2)
            return;

        BoxCollider2D topCap = endCapColliders[0];
        BoxCollider2D bottomCap = endCapColliders[0];

        foreach (BoxCollider2D endCapCollider in endCapColliders)
        {
            float rootY = colliderOffsetYToRootY(endCapCollider, endCapCollider.offset.y);
            if (rootY > colliderOffsetYToRootY(topCap, topCap.offset.y))
                topCap = endCapCollider;

            if (rootY < colliderOffsetYToRootY(bottomCap, bottomCap.offset.y))
                bottomCap = endCapCollider;
        }

        Vector2 topOffset = topCap.offset;
        topOffset.y = rootYToColliderOffsetY(topCap, topAnchorY + TOP_CAP_OFFSET);
        topCap.offset = topOffset;

        Vector2 bottomOffset = bottomCap.offset;
        bottomOffset.y = rootYToColliderOffsetY(bottomCap, bottomAnchorY + BOTTOM_CAP_OFFSET);
        bottomCap.offset = bottomOffset;
    }

    // Sizes/positions the reveal SpriteMask (child 5) to the deck gap [endY .. near anchor] so the
    // deployed deck is fully revealed and the retracted deck (parked above the near anchor) is
    // hidden. Only local Y is touched. The same GameObject carries the "Metal"/on-bridge trigger
    // (Character.OnTriggerEnter2D -> addToBridge + metal footsteps); we deliberately let that
    // collider scale with the mask so the on-bridge zone grows with the deck.
    private void fitMask(float topAnchorY)
    {
        if (maskTransform == null || bridgeMask == null || bridgeMask.sprite == null)
            return;

        float spanTop = topAnchorY + MASK_TOP_MARGIN;
        float spanBottom = endY - MASK_BOTTOM_MARGIN;

        float maskSpriteHeight = bridgeMask.sprite.bounds.size.y;
        if (maskSpriteHeight <= Mathf.Epsilon)
            return;

        float newScaleY = Mathf.Abs(spanTop - spanBottom) / maskSpriteHeight;
        Vector3 scale = maskTransform.localScale;
        scale.y = newScaleY;
        maskTransform.localScale = scale;

        Vector3 pos = maskTransform.localPosition;
        pos.y = ((spanTop + spanBottom) * 0.5f) - (bridgeMask.sprite.bounds.center.y * newScaleY);
        maskTransform.localPosition = pos;
    }

    // Fits the root water/gap trigger (enabled while retracted) to the same gap span so a resized
    // bridge's open-water hazard covers the actual gap. This collider lives on the root, so its
    // offset is already in root-local units (no child localPosition to subtract).
    private void fitWaterTrigger(float topAnchorY)
    {
        if (waterBox == null)
            return;

        float scaleY = waterBox.transform.localScale.y;
        if (Mathf.Abs(scaleY) <= Mathf.Epsilon)
            return;

        float spanTop = topAnchorY + WATER_TOP_MARGIN;
        float spanBottom = endY - WATER_BOTTOM_MARGIN;

        Vector2 size = waterBox.size;
        size.y = Mathf.Abs(spanTop - spanBottom) / Mathf.Abs(scaleY);
        waterBox.size = size;

        Vector2 offset = waterBox.offset;
        offset.y = ((spanTop + spanBottom) * 0.5f) / scaleY;
        waterBox.offset = offset;
    }

    // Keeps the deploy light (root child, so it does not follow the near anchor on its own) at the
    // near shore as the bridge is resized.
    private void fitLight(float topAnchorY)
    {
        if (onLight == null)
            return;

        Vector3 pos = onLight.transform.localPosition;
        pos.y = topAnchorY + LIGHT_Y_OFFSET;
        onLight.transform.localPosition = pos;
    }

    private float rootYToColliderOffsetY(BoxCollider2D collider, float rootY)
    {
        float scaleY = collider.transform.localScale.y;
        if (Mathf.Abs(scaleY) <= Mathf.Epsilon)
            return collider.offset.y;

        return (rootY - collider.transform.localPosition.y) / scaleY;
    }

    private float colliderOffsetYToRootY(BoxCollider2D collider, float offsetY)
    {
        return collider.transform.localPosition.y + (offsetY * collider.transform.localScale.y);
    }

    private bool tryGetAutoFitGraphBounds(out Bounds graphBounds)
    {
        graphBounds = new Bounds(transform.position, Vector3.zero);
        bool hasBounds = false;

        encapsulateColliderBounds(transform.GetChild(TOP_WALKWAY_COLLIDERS_CHILD_INDEX).GetComponents<BoxCollider2D>(), ref graphBounds, ref hasBounds);
        encapsulateColliderBounds(colliderEnds.GetComponents<BoxCollider2D>(), ref graphBounds, ref hasBounds);
        encapsulateColliderBounds(topCollider, ref graphBounds, ref hasBounds);

        return hasBounds;
    }

    private void encapsulateColliderBounds(Collider2D[] colliders, ref Bounds graphBounds, ref bool hasBounds)
    {
        foreach (Collider2D collider in colliders)
        {
            encapsulateColliderBounds(collider, ref graphBounds, ref hasBounds);
        }
    }

    private void encapsulateColliderBounds(Collider2D collider, ref Bounds graphBounds, ref bool hasBounds)
    {
        if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy)
            return;

        if (hasBounds)
            graphBounds.Encapsulate(collider.bounds);
        else
            graphBounds = collider.bounds;

        hasBounds = true;
    }

    private void updateGraphs(Bounds bounds)
    {
        if (AstarPath.active != null)
            AstarPath.active.UpdateGraphs(bounds);
    }

    // A* rescan region for a deploy/retract: the deck collider swept across its full travel
    // (startY..endY) plus both end caps and the water/gap trigger, since all of those change
    // blocking state together. Must be called while those colliders are still enabled --
    // encapsulateColliderBounds skips disabled ones and a disabled collider has empty bounds.
    private Bounds getGraphUpdateBounds()
    {
        Bounds bounds = topCollider.bounds;

        // Sweep the deck bounds to both travel endpoints so the region it vacated is rescanned
        // too. The walkway animates on root-local Y, so convert the travel delta to world space
        // through the (possibly rotated) root.
        float currentY = walkway.transform.localPosition.y;
        Bounds sweptBounds = bounds;
        sweptBounds.center += transform.TransformVector(new Vector3(0f, startY - currentY, 0f));
        bounds.Encapsulate(sweptBounds);
        sweptBounds = topCollider.bounds;
        sweptBounds.center += transform.TransformVector(new Vector3(0f, endY - currentY, 0f));
        bounds.Encapsulate(sweptBounds);

        bool hasBounds = true;
        encapsulateColliderBounds(colliderEnds.GetComponents<BoxCollider2D>(), ref bounds, ref hasBounds);
        encapsulateColliderBounds(waterTrigger, ref bounds, ref hasBounds);

        return bounds;
    }

    // Opt-in (debugAudio) probe for the "gears animate but no SFX" case. PlaySound can fail
    // silently for several reasons, so report each separately: muted flag (no warning logged),
    // whether the Move/Stop Sound entries actually exist (getSound logs its own warning if not),
    // and the 3D distance from the AudioListener (Move/Stop are spatialBlend 1, maxDistance 25).
    private void logAudioDiagnostics(string phase)
    {
        if (!debugAudio)
            return;

        AudioListener listener = FindObjectOfType<AudioListener>();
        float topDist = listener != null ? Vector3.Distance(listener.transform.position, topAudio.transform.position) : -1f;
        float bottomDist = listener != null ? Vector3.Distance(listener.transform.position, bottomAudio.transform.position) : -1f;

        Debug.Log($"[Bridge:{name}] audio@{phase} | topMuted={topAudio.muted} MoveFound={topAudio.getSound("Move") != null} topDist={topDist:F1} | " +
                  $"bottomMuted={bottomAudio.muted} StopFound={bottomAudio.getSound("Stop") != null} bottomDist={bottomDist:F1} (Move/Stop maxDistance=25)");
    }

    public void forcePowerOn()
    {
        if(!extended)
            togglePower(true);
    }

    public override bool togglePower(bool on)
    {
        if (on)
            powerReceived++;
        else
            powerReceived--;

        if(on && powerReceived >= powerRequired)
        {
            if (!isMoving)
            {
                isMoving = true;
                logAudioDiagnostics("Move (extend)");
                topAudio.PlaySound("Move");
                gearAnimator.Play("Gears_Animation");
            }

            StopAllCoroutines();
            StartCoroutine(extend());
        }
        else if(!on && powerReceived <= powerRequired)
        {
            if (!isMoving)
            {
                isMoving = true;
                logAudioDiagnostics("Move (retract)");
                topAudio.PlaySound("Move");
                gearAnimator.Play("Gears_Animation");
            }

            StopAllCoroutines();
            StartCoroutine(retract());
        }

        return true;
    }

    private IEnumerator extend()
    {
        onLight.SetActive(true);
        bool done = false;

        float lastShake = 0.0f;
        float randX = startX;

        while (!done)
        {
            if(Time.time > (lastShake+shakeDelay))
            {
                randX = startX + Random.Range(maxShake * -1, maxShake);
                lastShake = Time.time;
            }

            if (walkway.transform.localPosition.y > endY)
            {              
                walkway.transform.localPosition = new Vector3(randX, walkway.transform.localPosition.y - (movementSpeed * Time.deltaTime), 0f);
                moveRiders(-movementSpeed * Time.deltaTime);
            }
            else
            {
                walkway.transform.localPosition = new Vector3(startX, endY);
                done = true;
            }
            yield return 0;
        }

        Bounds graphBounds = getGraphUpdateBounds();

        colliderEnds.SetActive(false);
        waterTrigger.enabled = false;
        logAudioDiagnostics("Stop");
        topAudio.StopSound("Move");
        bottomAudio.PlaySound("Stop");
        isMoving = false;
        gearAnimator.Play("Gears_Idle");
        ps.Play();

        topCollider.enabled = false;

        updateGraphs(graphBounds);
        extended = true;
    }

    private IEnumerator retract()
    {
        extended = false;
        topCollider.enabled = true;
        onLight.SetActive(false);
        colliderEnds.SetActive(true);
        waterTrigger.enabled = true;
        bool done = false;

        float lastShake = 0.0f;
        float randX = startX;

        while (!done)
        {
            if (Time.time > (lastShake + shakeDelay))
            {
                randX = startX + Random.Range(maxShake * -1, maxShake);
                lastShake = Time.time;
            }

            if (walkway.transform.localPosition.y < startY)
            {
                walkway.transform.localPosition = new Vector3(randX, walkway.transform.localPosition.y + (movementSpeed*Time.deltaTime), 0f);
                moveRiders(movementSpeed * Time.deltaTime);
            }
            else
            {
                walkway.transform.localPosition = new Vector3(startX, startY);
                done = true;
            }
            yield return 0;
        }

        isMoving = false;
        gearAnimator.Play("Gears_Idle");
        topAudio.StopSound("Move");

        updateGraphs(getGraphUpdateBounds());
    }

    // Carries riders (players/objects on the deck) with the moving walkway. The walkway itself
    // always animates on local Y (child of the possibly-rotated root); riders are world-space, so
    // they follow local Y for vertical bridges and local X for sideways ones. Retract must mirror
    // extend's axis (opposite sign) or sideways riders get carried the wrong direction.
    private void moveRiders(float delta)
    {
        foreach (GameObject obj in onBridge)
        {
            Vector3 p = obj.transform.localPosition;
            if (isVertical)
                p.y += delta;
            else
                p.x += delta;
            obj.transform.localPosition = p;
        }
    }

    public void addToBridge(GameObject obj)
    {
        onBridge.Add(obj);
    }

    public void removeFromBridge(GameObject obj)
    {
        onBridge.Remove(obj);
    }
}

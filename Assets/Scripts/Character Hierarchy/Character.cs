using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

/** 
    DESCRIPTION: MasterClass for Character-type Objects.
    PRIMARY FUNCTIONALITY: Movement/Associated Movement Animation Triggers
**/

public class Character : Entity
{
    [System.NonSerialized]
    public AudioManager audioManager;

    [Header("Character: Movement Speeds")]
    public float baseSpeed = 1.0f;
    public float slowdownModifier;
    public float shieldsSlowdownModifier = 0.5f;
    public float slideSpeed;
    public float slideSlowdownModifier;
    public float slideCooldownIncrement;

    [Header("Character: Spritesheets")]
    public string chestSpriteSheetName;
    public string legsSpriteSheetName;
    public string skinSpriteSheetName;
    public string hairSpriteSheetName;

    [Header("Character: Customization")]
    [ES3NonSerializable]
    public GameObject[] heads;
    [Tooltip("-1 = Random, 0 = Up, 1 = Right, 2 = Down, 3 = Left")]
    public int startingDirection = -1;

    [System.NonSerialized]
    [ES3Serializable]
    public bool isDead = false;
    [System.NonSerialized]
    public bool isMoving = false;

    protected int direction = 4;
    protected bool animLocked;
    protected int lockedDirection;
    protected bool transitionOut = false;
    protected Rigidbody2D rb;
    protected Vector2 movementDirection;
    // Velocity imparted by a moving platform (Elevator), composed with the character's own locomotion.
    [System.NonSerialized] public Vector2 carrierVelocity;
    private Vector2 locomotionVelocity;
    private bool carrierApplied;
    protected float movementSpeed; 
    protected float originalSpeed;
    protected float originalChestSpeed;
    protected float originalLegSpeed;
    protected float originalHeadSpeed;
    protected float slowedLegSpeed;
    protected string[] directionTriggersRunning;
    protected Animator legsAnimator;
    protected Animator headAnimator;
    [System.NonSerialized]
    public bool isSliding = false;
    protected float slideCooldown = 0.0f;
    // Watchdog: a slide normally ends on the getUp animation event. If that final-frame event
    // is dropped, isSliding never clears — force-abort after this long as a code-side fallback.
    protected float slideStartTime = 0f;
    protected bool slidePrepCompleted = true;
    protected const float MAX_SLIDE_DURATION = 2.0f;
    protected int deathDirectionModifier = 1;    
    protected bool isHuman = true;
    protected bool disallowFootsteps = false;
    protected string[] chestIdleStates;
    protected string[] runStates;
    protected int lastWalkType = 1;

    protected Animator chestAnimator;
    private string loadedChestSpriteSheetName;
    private Dictionary<string, Sprite> chestSpriteSheet;
    private SpriteRenderer chestSpriteRenderer;
    private string loadedLegsSpriteSheetName;
    private Dictionary<string, Sprite> legsSpriteSheet;
    private SpriteRenderer legsSpriteRenderer;
    protected Vector2 lastMovementDirection;
    protected int lastDirection = 4;

    // Direction-smoothing tuning. Other axis must be 1.25× current to flip; pending direction must
    // hold for DIRECTION_COMMIT_DEBOUNCE before anims swap. Smoothing is applied only to the vector
    // feeding direction selection — rb.velocity uses the raw movementDirection so feel is unchanged.
    protected const float DIRECTION_HYSTERESIS = 1.25f;
    protected const float DIRECTION_COMMIT_DEBOUNCE = 0.09f;
    protected const float DIRECTION_SMOOTHING = 0.35f;

    private int pendingDirection = 4;
    private float pendingDirectionTime = 0f;
    private Vector2 smoothedMovementDirection;
    private string lastChestTrigger = null;
    private string lastLegsTrigger = null;
    private int activeHead = 3;
    // 0 = asphalt, 1 = gravel, 2 = metal
    private string[] footstepSFX = { "Footsteps_Asphalt", "Footsteps_Gravel", "Footsteps_Metal" };
    private int selectedWalkType = 1;
    private bool hasTriggeredFinalAnim = false;
    private int lastY;
    private bool isIndoors = false;
    // Tracks every Indoors trigger we're currently inside. Counting (not a single bool) keeps
    // overlapping indoor zones correct: leaving one while still inside another stays "indoors".
    private HashSet<Collider2D> indoorTriggers = new HashSet<Collider2D>();
    private bool isOnMetal = false;

    private SpriteRenderer[] renderers;

    private Animator parentAnimator;

    [System.NonSerialized]
    public bool shieldsDeployed = false;

    protected int directionCurrentlyFacing = 4;

    protected virtual void Awake()
    {
        audioManager = GetComponent<AudioManager>();
        parentAnimator = GetComponent<Animator>();

        lastY = (int)transform.position.y;
        //updateSortingLayers(lastY); // set initial sorting layers     // DEPRECATED  
    }
    // Start is called before the first frame update
    protected override void Start()
    {
        if (isDead)
        {
            this.gameObject.SetActive(false);
            return;
        }

        rb = GetComponent<Rigidbody2D>();
        
        chestAnimator = transform.GetChild(0).GetComponent<Animator>();
        headAnimator = transform.GetChild(1).GetComponent<Animator>();
        legsAnimator = transform.GetChild(2).GetComponent<Animator>();

        this.chestSpriteRenderer = transform.GetChild(0).GetComponent<SpriteRenderer>();
        this.legsSpriteRenderer = transform.GetChild(2).GetComponent<SpriteRenderer>();

        this.LoadChestSpriteSheet();
        this.LoadLegsSpriteSheet();

        if(hairSpriteSheetName != "")
            changeHairSprites(hairSpriteSheetName);

        if(skinSpriteSheetName != "")
            changeSkinColor(skinSpriteSheetName);

        lastMovementDirection = Vector2.zero;

        animLocked = false;
        lockedDirection = -1;

        directionTriggersRunning = new string[] { "PlayerRunUp", "PlayerRunRight", "PlayerRunDown", "PlayerRunLeft" };
        runStates = new string[] { "Chest_Run_Up", "Chest_Run_Right", "Chest_Run_Down", "Chest_Run_Left" };
        //directionTriggersStop = new string[] { "PlayerStopUp", "PlayerStopRight", "PlayerStopDown", "PlayerStopLeft" };
        chestIdleStates = new string[] { "Chest_Idle_Up", "Chest_Idle_Right", "Chest_Idle_Down", "Chest_Idle_Left_Anim" };

        originalSpeed = baseSpeed;
        originalLegSpeed = legsAnimator.speed;
        originalChestSpeed = chestAnimator.speed;
        originalHeadSpeed = headAnimator.speed;
        slowedLegSpeed = legsAnimator.speed * slowdownModifier;

        if (startingDirection == -1)
            startingDirection = UnityEngine.Random.Range(0, 4);

        if(startingDirection != 3) // faces left by default, no need to set anims one frame later so start() method can be completed for children inheritors
            faceStartingDirection();

        refreshRenderersList();

        directionCurrentlyFacing = startingDirection;

        base.Start();
    }

    protected virtual void Update()
    {
        
    }

    private void LateUpdate()
    {
        checkForGameBreakingBugs();
        overrideAllSprites();
    }

    void FixedUpdate()
    {
        // Re-assert every physics step: locomotion code overwrites velocity in Update.
        if (carrierVelocity != Vector2.zero && !isSliding && !isDead)
        {
            rb.velocity = locomotionVelocity + carrierVelocity;
            carrierApplied = true;
        }
        else if (carrierApplied)
        {
            carrierApplied = false;
            if (!isSliding && !isDead)
                rb.velocity = locomotionVelocity;
        }

        if (isSliding || (isDead && rb.velocity.magnitude > 0f))
        {
            rb.velocity *= slideSlowdownModifier;
            if (isSliding) onSlideTick();
        }
    }

    protected virtual void onSlideTick() { }

    protected void faceStartingDirection()
    {
        StartCoroutine(setInitialIdle(startingDirection, chestIdleStates));
    }


    public override void updateSpeakerCanvasPortrait()
    {
        loadDialogueSprites();
        base.updateSpeakerCanvasPortrait();
    }

    private void loadDialogueSprites()
    {
        // load dialogue sprites
        if (dialogueHead == null)
        {
            dialogueHead = heads[3].GetComponent<SpriteRenderer>().sprite;
            dialogueHair = heads[3].transform.GetChild(0).GetComponent<SpriteRenderer>().sprite;

            if (chestSpriteSheet.ContainsKey("Chest_6")) // error catch
            {
                dialogueChest = chestSpriteSheet["Chest_6"];
            }
            else
                Debug.Log("No chest found for dialogue manager");

            if (legsSpriteSheet.ContainsKey("Legs_6")) // error catch
            {
                dialogueLegs = legsSpriteSheet["Legs_6"];
            }
            else
                Debug.Log("No leg found for dialogue manager");
        }
    }

    // Renderers a subclass declares as belonging to another system are excluded from
    // whole-body color effects (e.g. the deployed Kipling parented under the player).
    protected virtual bool ownsRenderer(SpriteRenderer renderer)
    {
        return true;
    }

    private void refreshRenderersList()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true).Where(ownsRenderer).ToArray();
    }

    protected void desaturateEntireBody()
    {
        foreach(SpriteRenderer renderer in renderers)
        {
            Color originalColor = renderer.color;
            StartCoroutine(desaturateCoroutine(renderer, originalColor));
        }
    }

    protected void saturateEntireBody()
    {
        foreach(SpriteRenderer renderer in renderers)
        {
            // Stop any in-flight desaturation lerp; force renderer back to full saturation.
            StopCoroutine("desaturateCoroutine");
            renderer.color = Color.white;
        }
    }

    private IEnumerator desaturateCoroutine(SpriteRenderer renderer, Color originalColor)
    {
        float desaturationAmount = 0f;
        while (desaturationAmount < 0.6f) 
        {
            desaturationAmount += Time.deltaTime * 0.5f; 
            Color newColor = Color.Lerp(originalColor, Color.gray, desaturationAmount);
            renderer.color = newColor;
            yield return null;
        }
    }

    private void goTransparent()
    {
        // Alpha-only: RGB may be owned by another system (e.g. KiplingTempestVisuals' tint)
        foreach (SpriteRenderer renderer in renderers)
        {
            Color newColor = renderer.color;
            newColor.a = 0.5f;
            renderer.color = newColor;
        }
    }

    private void doneTransparent()
    {
        foreach (SpriteRenderer renderer in renderers)
        {
            StartCoroutine(fadeInRoutine(renderer));
        }
    }

    private IEnumerator fadeInRoutine(SpriteRenderer renderer)
    {
        // Alpha-only, re-reading color each frame: RGB may be mid-lerp under another
        // system (e.g. KiplingTempestVisuals' tint fade) and must pass through untouched.
        float time = 0.0f;
        while (time < 0.15f)
        {
            float t = TEMLib.getSmoothLerpT(time, 0.15f);

            Color newColor = renderer.color;
            newColor.a = Mathf.Lerp(0.5f, 1f, t);
            renderer.color = newColor;

            time += Time.deltaTime;
            yield return null;
        }

        Color finalColor = renderer.color;
        finalColor.a = 1f;
        renderer.color = finalColor;
    }

    private IEnumerator setInitialIdle(int startingDirection, string[] chestIdleStates)
    {
        yield return 0;
        setAnim(startingDirection, chestIdleStates);
    }

    private void checkForGameBreakingBugs() // We've fixed these bugs; this shouldn't fire anymore
    {
        // Time-based fallback: catches a stuck slide even when the chest never reaches an idle
        // state (e.g. parked at the end of the recover clip after a dropped getUp event).
        if (isSliding && Time.time - slideStartTime > MAX_SLIDE_DURATION)
        {
            Debug.Log("Slide watchdog fired — aborting stuck slide.");
            abortSlide();
            return;
        }

        if (isSliding || isDead)
        {
            foreach (string st in chestIdleStates) // for infinite 'skate' or 'dead man walking' game breaking bug
            {
                if (chestAnimator.GetCurrentAnimatorStateInfo(0).IsName(st))
                {
                    Debug.Log("BIG PROBLEM. Error detected");
                    if (isSliding)
                    {
                        abortSlide();
                        Debug.Log("infinite slide.");
                    }
                    else
                    {
                        characterDeath();
                        Debug.Log("infinite death.");
                        if (this.transform != PlayerController.instance.transform)
                            gameObject.SetActive(false);
                        chestSpriteRenderer.enabled = false;
                        legsSpriteRenderer.enabled = false;
                    }
                    break;
                }
            }
        }
    }

    public override void faceObject(Vector2 target, bool overrideAndReturnToOriginal)
    {
        int d; 

        if (overrideAndReturnToOriginal)
            d = startingDirection;
        else
            d = getTargetDirection(target);

        setAnim(d, chestIdleStates);
    }

    public void slowAnimAndSpeed(float speedPercentage)
    {
        baseSpeed *= speedPercentage;
        if(legsAnimator != null)
            legsAnimator.speed *= speedPercentage;
        if(chestAnimator != null)
            chestAnimator.speed *= speedPercentage;
        if(headAnimator != null)
            headAnimator.speed *= speedPercentage;
    }

    public void resumeOriginalAnimAndSpeed()
    {
        baseSpeed = originalSpeed;
        if (legsAnimator != null)
            legsAnimator.speed = originalLegSpeed;
        if (chestAnimator != null)
            chestAnimator.speed = originalChestSpeed;
        if (headAnimator != null)
            headAnimator.speed = originalHeadSpeed;
    }

    protected int getTargetDirection(Vector2 target)
    {
        return TEMLib.getDirection(target);
    }

    protected virtual void overrideAllSprites()
    {
        updateSpritesheet(ref loadedChestSpriteSheetName, chestSpriteSheetName, LoadChestSpriteSheet, chestSpriteRenderer, chestSpriteSheet);
        updateSpritesheet(ref loadedLegsSpriteSheetName, legsSpriteSheetName, LoadLegsSpriteSheet, legsSpriteRenderer, legsSpriteSheet);
    }

    protected void updateSpritesheet(ref string loadedSpriteSheetName, string currentSpriteSheetName, Action loadSpriteSheetAction, SpriteRenderer spriteRenderer, Dictionary<string, Sprite> spriteSheet)
    {
        if (loadedSpriteSheetName != currentSpriteSheetName)
        {
            loadSpriteSheetAction();
            loadedSpriteSheetName = currentSpriteSheetName; // Update cache
        }

        if (spriteSheet.TryGetValue(spriteRenderer.sprite.name, out var newSprite))
        {
            spriteRenderer.sprite = newSprite;
        }
        else
        {
            Debug.LogWarning($"Sprite key '{spriteRenderer.sprite.name}' not found.");
            // Optionally set a default sprite
            // spriteRenderer.sprite = defaultSprite;
        }
    }

    // Idempotent full slide cleanup: restores collider/transparency (prepGettingUp) and clears
    // slide state/anim (stopSliding) in order. Safe to call from any recovery path or repeatedly.
    public virtual void abortSlide()
    {
        if (!isSliding) return;
        rb.velocity = Vector2.zero;
        prepGettingUp();
        stopSliding();
    }

    protected void checkForMove()
    {
        if (!isSliding && !isDead) // can't move while sliding or dead
        {
            if (movementDirection != Vector2.zero)
            { // only move if movement direction is registered
                move();
            }
            else  // process idle anim transition since no movement is registered
            {
                if (direction == 4) // only if character has movement direction
                    return;
                
                locomotionVelocity = Vector2.zero;
                rb.velocity = carrierVelocity; // stop the character to prevent 'sliding'

                if (animLocked) // character is brandishing a weapon, do not allow animation to process normally
                {
                    //legsAnimator.SetTrigger(directionTriggersStop[lockedDirection]); // stop just the legs
                    legsAnimator.SetTrigger("PlayerStop"); // stop just the legs

                    if (shieldsDeployed)
                    {
                        chestAnimator.SetTrigger("PlayerStop");
                    }

                    stopMoving(false);
                }
                else
                {
                    //triggerAnim(direction, directionTriggersStop); // stop everything
                    stopMoving(true);
                    //headAnimator.SetTrigger("StopRunning"); // head stops regardless of if shooting or not
                }

                direction = 4; // no movement direction
                lastDirection = 4;
                lastMovementDirection = Vector2.zero; // reset previous direction
                isMoving = false;

                pendingDirection = 4;
                pendingDirectionTime = 0f;
                smoothedMovementDirection = Vector2.zero;
                lastChestTrigger = null;
                lastLegsTrigger = null;

                audioManager.StopSound(footstepSFX[selectedWalkType]);

            }
        }
    }

    protected virtual void move()
    {
        if (isDead)
            return;

        float modifier = 1.0f;
        if (animLocked) // if movement input is registereed AND the animation is locked because of firearm
        {
            modifier = slowdownModifier;
            if (direction != lockedDirection)
            {
                legsAnimator.SetTrigger(directionTriggersRunning[lockedDirection]); // replace with leg walking animation

                if (shieldsDeployed)
                    chestAnimator.SetTrigger(directionTriggersRunning[lockedDirection]);

                direction = lockedDirection;
            }
        }
        else if (movementDirection != lastMovementDirection || transitionOut) // check for direction change
        {
            // Latch immediate-apply conditions before checkDirection() clears transitionOut.
            bool forceImmediate = !useDirectionSmoothing || transitionOut || !isMoving || animLocked;

            if (forceImmediate)
                smoothedMovementDirection = movementDirection; // snap to avoid stale blend on first-frame / exit-slide
            else
                smoothedMovementDirection = Vector2.Lerp(smoothedMovementDirection, movementDirection, DIRECTION_SMOOTHING);

            checkDirection();

            if (lastDirection != direction)
            {
                if (forceImmediate)
                {
                    commitDirection();
                }
                else if (direction == pendingDirection)
                {
                    if (Time.time - pendingDirectionTime >= DIRECTION_COMMIT_DEBOUNCE)
                        commitDirection();
                    else
                        direction = lastDirection; // still waiting — don't leak uncommitted candidate
                }
                else
                {
                    // New candidate — start the timer and hold the previously-committed facing.
                    pendingDirection = direction;
                    pendingDirectionTime = Time.time;
                    direction = lastDirection;
                }
            }
            else
            {
                // Candidate returned to current facing before debounce elapsed — drop any pending flip.
                pendingDirection = lastDirection;
            }

            lastMovementDirection = movementDirection;
        }

        locomotionVelocity = movementDirection * movementSpeed * baseSpeed * modifier;
        rb.velocity = locomotionVelocity + carrierVelocity;

        if (!isMoving)
        {
            if (!disallowFootsteps)
                audioManager.PlaySound(footstepSFX[selectedWalkType]);
            isMoving = true;
            signalStartMove();
        }

    }

    private void commitDirection()
    {
        triggerAnimNoInterrupt(direction, directionTriggersRunning, runStates);
        headAnimator.SetTrigger("Running");
        signalDirectionChange(direction);
        lastDirection = direction;
        pendingDirection = direction;
        pendingDirectionTime = Time.time;
    }

    protected virtual bool isAllowedToMoveDirection()
    {
        return true;    // DEPRECATED: smoothing now handled by hysteresis + debounce in Character.move
    }

    // Override to false in subclasses that want instant direction changes (e.g., PlayerController).
    protected virtual bool useDirectionSmoothing => true;

    protected virtual void signalStartMove()
    {
        // inheritable function to show when we start moving
        return;
    }

    // When overwritten in a child class, allows that child class to take action when the animation direction changes
    protected virtual void signalDirectionChange(int d)
    {
        directionCurrentlyFacing = d;
        return;
    }

    protected virtual void checkDirection()
    {
        if (transitionOut) // gun is put away while character is moving in the same direction - reset anims
        {
            lastMovementDirection = Vector2.zero;
            lastDirection = 4;
            direction = 4;
            pendingDirection = 4;
            pendingDirectionTime = 0f;
            lastChestTrigger = null;
            lastLegsTrigger = null;
            transitionOut = false;
        }

        // Use the smoothed vector for direction selection so waypoint-swap / separation-force spikes
        // don't reach the cardinal classifier. rb.velocity still uses raw movementDirection.
        direction = getNewDirection(smoothedMovementDirection, lastMovementDirection, direction);
    }

    // Picks the cardinal animation direction from the dominant axis of newVector. Biased to keep
    // the currently-facing axis (originalDirection) unless the other axis exceeds it by
    // DIRECTION_HYSTERESIS — this prevents per-frame flipping on near-diagonal motion. oldVector
    // is unused after the rewrite but kept for API stability (subclasses may still call this).
    protected int getNewDirection(Vector2 newVector, Vector2 oldVector, int originalDirection)
    {
        float absX = Mathf.Abs(newVector.x);
        float absY = Mathf.Abs(newVector.y);

        bool useVertical;
        if (!useDirectionSmoothing || originalDirection == 4) // no bias — pure dominant axis
            useVertical = absY > absX;
        else if (originalDirection == 1 || originalDirection == 3) // currently horizontal
            useVertical = absY > absX * DIRECTION_HYSTERESIS;
        else // currently vertical (0 or 2)
            useVertical = !(absX > absY * DIRECTION_HYSTERESIS);

        if (useVertical)
            return newVector.y >= 0 ? 0 : 2;
        else
            return newVector.x > 0 ? 1 : 3;
    }

    protected void triggerAnimNoInterrupt(int d, string[] triggerArray, string[] animsToNotInterrupt)
    {
        if (!areAnimConditionsValid(d, triggerArray))
            return;

        string trig = triggerArray[d];

        // IsName() can report the old state during a Mecanim transition, which lets a duplicate
        // SetTrigger through and restarts the clip. The last-sent-trigger cache catches that.
        if (lastChestTrigger != trig && !chestAnimator.GetCurrentAnimatorStateInfo(0).IsName(animsToNotInterrupt[d]))
        {
            setChestAndHeadTrigger(d, trig);
            lastChestTrigger = trig;
        }
        if (lastLegsTrigger != trig && !legsAnimator.GetCurrentAnimatorStateInfo(0).IsName(animsToNotInterrupt[d]))
        {
            setLegsTrigger(trig);
            lastLegsTrigger = trig;
        }
    }

    protected void triggerAnim(int d, string[] triggerArray)
    {
        if (!areAnimConditionsValid(d, triggerArray))
            return;

        setChestAndHeadTrigger(d, triggerArray[d]);
        setLegsTrigger(triggerArray[d]);
    }

    private void setChestAndHeadTrigger(int d, string triggerString)
    {
        if (!shieldsDeployed)
        {
            chestAnimator.SetTrigger(triggerString);
            swapHeads(d);
        }
    }

    private void setLegsTrigger(string triggerString)
    {
        if (!animLocked || isDead)
        {
            legsAnimator.SetTrigger(triggerString);
        }
    }

    private bool areAnimConditionsValid(int d, string[] triggerArray)
    {
        if (isDead || d == 4)
            return false;

        if (d >= triggerArray.Length)
        {
            Debug.LogError("Bad direction parameter passed to triggerAnim");
            return false;
        }

        return true;
    }

    protected void setAnim(int d, string[] stateArray)
    {
        if (d >= stateArray.Length)
            return;
        chestAnimator.Play(stateArray[d],0);
        if (!animLocked || isDead)
            legsAnimator.Play(stateArray[d],0);

        swapHeads(d);
        signalDirectionChange(d);
    }

    protected virtual bool trySlide()
    {
        // No sliding on a moving platform: slide velocity ignores the carrier and would lag the car.
        if(isMoving && !isSliding && (Time.time > slideCooldown) && carrierVelocity == Vector2.zero)
        {
            // Sync direction to live movementDirection — PlayerController processes the slide key
            // inside processInputs() before move() runs, so without this the head/chest slide
            // triggers see last frame's direction while rb.velocity uses the fresh vector.
            // Also force chest/legs directly to the matching run state: PlayerSlide transitions
            // chest/legs from their *current* run state, so the head's new-direction slide would
            // otherwise mismatch the body's old-direction slide.
            if (movementDirection != Vector2.zero)
            {
                int freshDir = getNewDirection(movementDirection, lastMovementDirection, direction);
                if (freshDir != direction)
                {
                    direction = freshDir;
                    lastDirection = freshDir;
                    // Legs use an override controller over Chest_Controller — state names are
                    // "Chest_Run_*" on both, only clips differ. runStates is correct for both.
                    chestAnimator.Play(runStates[freshDir]);
                    legsAnimator.Play(runStates[freshDir]);
                    swapHeads(freshDir);
                }
                // move() doesn't run during the slide; keep the smoothed vector fresh so
                // stopSliding's checkDirection sees live input, not an entry-time stale value.
                smoothedMovementDirection = movementDirection;
            }

            string headSlideTrigger;
            isSliding = true;
            slideStartTime = Time.time;
            slidePrepCompleted = false;
            chestAnimator.SetBool("IsSliding", true);
            rb.velocity = movementDirection * slideSpeed * baseSpeed;

            catchAnimationIssues(); 

            chestAnimator.SetTrigger("PlayerSlide");
            legsAnimator.SetTrigger("PlayerSlide");

            if (direction == 0 || direction == 3)
                headSlideTrigger = "SlideLeft";
            else
                headSlideTrigger = "SlideRight";

            headAnimator.SetTrigger(headSlideTrigger);

            audioManager.StopSound(footstepSFX[selectedWalkType]);
            audioManager.PlaySound("Slide");

            parentAnimator.enabled = true;
            parentAnimator.Play("SlideAnim");
            goTransparent();

            return true;
        }

        return false;
    }

    // this function is used by inherited members to override and stop current walk sound

    protected void stopWalkSFX()
    {
        audioManager.StopSound(footstepSFX[selectedWalkType]);
    }

    public virtual void prepGettingUp()
    {
        doneTransparent();
        slidePrepCompleted = true;
    }

    public virtual void stopSliding()
    {
        if (!slidePrepCompleted)
            prepGettingUp();

        clearTriggers(new string[] { "PlayerSlide" });

        slideCooldown = Time.time + slideCooldownIncrement;
        int tempDirection;
        isSliding = false;
        chestAnimator.SetBool("IsSliding", false);
        transitionOut = true;
        tempDirection = direction;
        if (movementDirection != Vector2.zero)
            smoothedMovementDirection = movementDirection;
        checkDirection();
        if (direction != 4)
        {
            triggerAnimNoInterrupt(direction, directionTriggersRunning, runStates);
            headAnimator.SetTrigger("Running");
        }
        else
        {
            direction = tempDirection;
            triggerAnimNoInterrupt(tempDirection, directionTriggersRunning, runStates);
            headAnimator.SetTrigger("Running");
        }

        audioManager.PlaySound(footstepSFX[selectedWalkType]);      
    }

    protected void swapHeads(int headNo)
    {
        if (headNo != activeHead)
        {
            heads[activeHead].SetActive(false);
            activeHead = headNo;
            heads[activeHead].SetActive(true);
        }
    }

    protected virtual void stopMoving(bool stopAnims)
    {
        pendingDirection = 4;
        pendingDirectionTime = 0f;
        smoothedMovementDirection = Vector2.zero;
        lastChestTrigger = null;
        lastLegsTrigger = null;

        if (stopAnims)
        {
            legsAnimator.SetTrigger("PlayerStop"); // stop just the legs
            chestAnimator.SetTrigger("PlayerStop");
            headAnimator.SetTrigger("StopRunning"); // head stops regardless of if shooting or not
        }
    }

    private void clearTriggers(string[] triggerArray)
    {
        //Debug.Log("clearing triggers");
        foreach(string st in triggerArray)
        {
            //Debug.Log("Clearing " + st);
            chestAnimator.ResetTrigger(st);
            legsAnimator.ResetTrigger(st);
        }
    }

    public virtual void characterDeath()
    {
        swapHeads(2);
        if(rb.velocity.magnitude > 0f)
            rb.velocity = Vector2.zero;
        audioManager.StopSound(footstepSFX[selectedWalkType]);
        //Destroy(rb);
        foreach (BoxCollider2D bc in GetComponentsInChildren<BoxCollider2D>(false))
        {
            bc.enabled = false;
        }
        // Disable rather than Destroy so respawn flows can re-enable in place.
        var mainCollider = this.GetComponent<BoxCollider2D>();
        if (mainCollider != null) mainCollider.enabled = false;

        if(GetComponent<spyDeathTrigger>() != null)
        {
            GetComponent<spyDeathTrigger>().signalDeathEnd();
        }
    }

    private void LoadChestSpriteSheet()
    {
        var sprites = Resources.LoadAll<Sprite>("Chests/" + this.chestSpriteSheetName);
        this.chestSpriteSheet = sprites.ToDictionary(x => x.name, x => x);

        this.loadedChestSpriteSheetName = this.chestSpriteSheetName;
    }

    private void LoadLegsSpriteSheet()
    {
        var sprites = Resources.LoadAll<Sprite>("Legs/" + this.legsSpriteSheetName);
        this.legsSpriteSheet = sprites.ToDictionary(x => x.name, x => x);

        this.loadedLegsSpriteSheetName = this.legsSpriteSheetName;
    }

    public bool checkForDead() // public accessor function
    {
        return isDead;
    }

    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Metal")
        {
            if(!isIndoors)
                changeWalkSound(2);

            isOnMetal = true;

            Bridge b = collision.gameObject.transform.GetComponentInParent<Bridge>();
            if (b != null)
                b.addToBridge(this.gameObject);
        }

        if(collision.gameObject.tag == "PressurePlate")
        {
            PressurePlate pp = collision.gameObject.transform.GetComponentInParent<PressurePlate>();
            pp.activate(gameObject);
        }

        if(collision.gameObject.tag == "Indoors")
        {
            // Only fire on the true outdoor→indoor transition (first trigger entered).
            if (indoorTriggers.Add(collision) && indoorTriggers.Count == 1)
            {
                isIndoors = true;
                changeWalkSound(0);
                onEnteredIndoors();
            }
        }

        if(collision.gameObject.tag == "SlowTime")
        {
            baseSpeed = 0.5f;
        }
    }

    protected virtual void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "SlowTime")
        {
            baseSpeed = originalSpeed;
        }

        if (collision.gameObject.tag == "Metal")
        {
            if (!isIndoors)
            {
                changeWalkSound(1);
            }
            else
            {
                changeWalkSound(0);
            }

            isOnMetal = false;

            Bridge b = collision.gameObject.transform.GetComponentInParent<Bridge>();
            if (b != null)
                b.removeFromBridge(this.gameObject);
        }

        if (collision.gameObject.tag == "PressurePlate")
        {
            PressurePlate pp = collision.gameObject.transform.GetComponentInParent<PressurePlate>();
            pp.deActivate();
        }

        if (collision.gameObject.tag == "Indoors")
        {
            // Guard against spurious exits (e.g. collider toggles during a slide): OverlapPoint
            // tests the trigger collider against our position, so it's correct regardless of our
            // own collider state. Still inside this trigger → ignore the exit.
            if (collision.OverlapPoint(transform.position))
                return;

            // Only fire on the true indoor→outdoor transition (last trigger left).
            if (indoorTriggers.Remove(collision) && indoorTriggers.Count == 0)
            {
                isIndoors = false;

                if (isOnMetal)
                    changeWalkSound(2);
                else
                    changeWalkSound(1);

                onExitedIndoors();
            }
        }
    }

    // Indoor transition hooks — overridden by PlayerController to drive zoom/overhead/rain.
    protected virtual void onEnteredIndoors() { }
    protected virtual void onExitedIndoors() { }

    public void changeWalkSound(int index)
    {
        audioManager.StopSound(footstepSFX[selectedWalkType]);
        selectedWalkType = index;
        if(isMoving)
            audioManager.PlaySound(footstepSFX[selectedWalkType]);
    }

    public void changeHairSprites(string spritesheetName)
    {
        var sprites = Resources.LoadAll<Sprite>("Hair/" + spritesheetName);
        for (int i = 0; i < 4; i++) // for each head
        {
            int index = i; // order of heads is fucked up
            if (i == 0)
                index = 3;
            else if (i == 3)
                index = 0;

            try
            {
                transform.GetChild(1).GetChild(i).GetChild(0).GetComponent<SpriteRenderer>().sprite = sprites[index];
            }
            catch(Exception)
            {
                Debug.LogError("Index out of range for hair sprites. Hair is " + spritesheetName + ", index is " + index);
                return;
            }
        }
    }

    public void changeSkinColor(string spritesheetName)
    {
        var sprites = Resources.LoadAll<Sprite>("Skin/" + spritesheetName);
        for (int i = 0; i < 4; i++) // for each head
        {
            int index = i; // order of heads is fucked up
            if (i == 0)
                index = 3;
            else if (i == 3)
                index = 0;

            transform.GetChild(1).GetChild(i).GetComponent<SpriteRenderer>().sprite = sprites[index];
        }
    }

    protected void catchAnimationIssues()
    {
        ResetAllTriggers(chestAnimator);
        ResetAllTriggers(legsAnimator);
        ResetAllTriggers(headAnimator);
    }

    private void ResetAllTriggers(Animator animator)
    {
        foreach (var param in animator.parameters)
        {
            if (param.type == AnimatorControllerParameterType.Trigger)
            {
                animator.ResetTrigger(param.name);
            }
        }
    }


}

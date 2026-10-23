using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using Com.LuisPedroFonseca.ProCamera2D;
using PixelCrushers.DialogueSystem;
using UnityEngine.Experimental.Rendering.Universal;

/** 
    DESCRIPTION: Child Class for Controlling the Player
**/

public class PlayerController : Humanoid
{
    [Header("Player: GameObject References")]
    [System.NonSerialized]
    public Image bloodVignette;
    public GameObject shell;
    public GameObject lancerShell;
    public GameObject electroShell;
    public GameObject batonShell;
    public GameObject modifiedBatonShell;
    public GameObject overloadPrefab;
    public GameObject turretPrefab;
    public GameObject firebugPrefab;

    [Header("Player: Combat Specific")]
    public float noiseRadius = 5.0f;
    public float swayModifier = 0f;
    public float upgradedBatonRange = 0.3f;
    public float upgradedBatonSpeed = 30f;

    public float globalInfestationCooldown = 0.0f;

    public Color[] muzzleFlashColors = new Color[4];

    public float[] reloadTimeArray = new float []{ 1f, 0.5f, 1.5f };

    [System.NonSerialized]
    [ES3NonSerializable]
    public static PlayerController instance;
    [System.NonSerialized]
    public bool frozen = false;
    [System.NonSerialized]
    public bool isInCombat = false;
    [System.NonSerialized]
    public bool canSave = true;

    [System.NonSerialized]
    [ES3Serializable]
    public Vector2 alignmentArray = new Vector2(0f, 0f);
    [System.NonSerialized]
    [ES3Serializable]
    public List<Vector2> alignmentHistory = new List<Vector2>();

    // Alignment abilities unlocked so far (ability ids from AlignmentData). Monotonic.
    // NOTE: also registered manually in ES3UserType_PlayerController (properties + read +
    // write); a bare [ES3Serializable] is not enough for this manual-save type.
    [System.NonSerialized]
    [ES3Serializable]
    public List<string> unlockedAlignmentAbilities = new List<string>();

    [System.NonSerialized]
    [ES3Serializable]
    public int componentCount = 0;
    [System.NonSerialized]
    [ES3Serializable]
    public int techCount = 0;
    [System.NonSerialized]
    [ES3Serializable]
    public int homesteadTier = 0;

    [System.NonSerialized]
    [ES3Serializable]
    public string deathDropScene = "";
    [System.NonSerialized]
    [ES3Serializable]
    public Vector3 deathDropPos;
    [System.NonSerialized]
    [ES3Serializable]
    public int deathDropComponentCount = 0;

    private Vector3 fatalHitPosition;

    [System.NonSerialized]
    [ES3Serializable]
    public int flagDesignInt = -1;
    [System.NonSerialized]
    [ES3Serializable]
    public int flagPrimaryColorInt = -1;
    [System.NonSerialized]
    [ES3Serializable]
    public int flagSecondaryColorInt = -1;

    [System.NonSerialized]
    public bool isCustomizing = false;

    [System.NonSerialized]
    public bool inSafeZone = true; // TO DO  - default to false once safe zone created

    [System.NonSerialized]
    [ES3Serializable]
    public bool hasBaton = false;

    [System.NonSerialized]
    [ES3Serializable]
    public bool hasKipling = false;

    public bool weaponsAllowed = true;

    private bool isInvincible = false;

    public enum alignmentQuadrant
    {
        Center,
        OrderSelf,
        FreedomSelf,
        FreedomCommunity,
        OrderCommunity
    }

    private bool vigOn = false;
    private bool oscillateVig = false;
    private float nextFade = 0.0f;
    private float hitLength = 0.5f;
    private bool hit = false;
    private float nextNoiseCheck = 0.0f;
    private InventoryItem equippedItem;
    [System.NonSerialized]
    public Gun equippedGun;
    private bool gunEquipped = true;
    [ES3Serializable]
    private Gun shockBatonGun = new Gun();

    private InfestationHandler infestationHandler;

    private PlayerProgressBarController playerProgressBarController;
    private int lastEquippedIndex = 0;
    private KiplingController kipController;
    
    private float addCritDmg;    
    private bool mouseIsUpLancerReady = true;
    private GameObject lancerChargingParticles;
    private int lockCount = 0;

    private float nextBatonFire = 0.0f;
    private bool displayingShock = false;
    private bool isBatonUpgraded = false;

    private KeyCode menuKeyCode = KeyCode.Tab;
    private KeyCode quickUse1 = KeyCode.X;
    private KeyCode quickUse2 = KeyCode.Z;
    private KeyCode slideKey = KeyCode.LeftShift;
    private KeyCode interact = KeyCode.E;
    private KeyCode deployKipling = KeyCode.F;
    private KeyCode crafting = KeyCode.LeftControl;
    private KeyCode shockBaton = KeyCode.Mouse1;
    private KeyCode shockBatonNoConductor = KeyCode.Q;
    private KeyCode reloadKeyCode = KeyCode.R;
    private KeyCode shieldsKeyCode = KeyCode.Space;
    private KeyCode overloadKeyCode = KeyCode.C;
    private KeyCode turretKeyCode = KeyCode.T;
    private KeyCode firebugKeyCode = KeyCode.G;
    private KeyCode recallFirebugKeyCode = KeyCode.H;

    private string playerName = "Jaden";

    private bool isMapOpen = false;
    private HoverBikeBehavior activeHoverBike; // bike that opened the map; deactivated on map close
    private bool isHomesteadOpen = false;
    private bool isInConversation = false;

    [ES3Serializable]
    [System.NonSerialized]
    public playerSkill[,] skillTree;

    [ES3NonSerializable]
    [System.NonSerialized]
    public alignmentQuadrant currentAlignment;

    [ES3NonSerializable]
    [System.NonSerialized]
    public const string SMALL_SHAKE = "Small Shake";
    const string TINY_SHAKE = "Tiny Shake";

    private static readonly string[] gunNamePrefix = { "Trad ", "Lancer ", "Electro " };
    private static readonly string[] gunModelPrefix = { "Rifle ", "Pistol ", "Shotgun " };

    private Transform shieldWallTransform;
    private BoxCollider2D shieldWallCollider;

    private ParticleSystem shieldSparkPS;

    private bool godMode = false;

    private GameObject instantiatedOverload;
    private GameObject instantiatedTurret;
    private List<GameObject> instantiatedFirebugs = new List<GameObject>();
    private int maxFirebugs = 0;

    // Cached prefab-baseline of slideSlowdownModifier. CombatChar.triggerDeath
    // overwrites the live field with the death-deceleration value; we restore
    // from this cache during respawn-prep so the death value never makes it into
    // a save file.
    [ES3NonSerializable]
    [System.NonSerialized]
    private float aliveSlideSlowdownModifier;

    protected override bool useDirectionSmoothing => false;


    protected override void Awake()
    {
        //UnityEngine.Debug.Log("PlayerController Awake");
        if (instance == null && this != instance)
            instance = this;
        else
        {
            //UnityEngine.Debug.Log("Destroying playerController duplicate");
            Destroy(gameObject);
        }

        playerProgressBarController = GetComponent<PlayerProgressBarController>();

        base.Awake();
        equippedGunSFX = gunSFX[0][0];
        kipController = GetComponent<KiplingController>();

        infestationHandler = GetComponent<InfestationHandler>();

        aliveSlideSlowdownModifier = slideSlowdownModifier;
        //DontDestroyOnLoad(this);
    }

    // The deployed Kipling is parented under the player until its first deploy detaches
    // it, so Character's renderer sweep would otherwise capture it and drag it through
    // whole-body effects (slide transparency). Its renderers belong to KiplingBehavior /
    // KiplingTempestVisuals, never the player.
    protected override bool ownsRenderer(SpriteRenderer renderer)
    {
        if (kipController != null && kipController.mobileKipling != null
            && renderer.transform.IsChildOf(kipController.mobileKipling.transform))
            return false;

        return true;
    }
    protected override void Start()
    {
        bloodVignette = CanvasMaster.instance.bloodVignette;
        InventoryManager.instance.changeEquipped(0, false, false);
        bloodVignette.canvasRenderer.SetAlpha(0.0f);
        base.Start();

        shieldWallTransform = deployedAnimationObj.transform.GetChild(0);
        shieldWallCollider = shieldWallTransform.GetComponentInChildren<BoxCollider2D>();

        shieldSparkPS = shieldWallTransform.parent.parent.GetChild(1).GetComponent<ParticleSystem>();

        switchEquipped(0, false, false, false);

        GameEvents.current.onAggro += OnAggro;
        GameEvents.current.onUnaggro += OnUnaggro;
        GameEvents.current.onPreviousSceneDataLoaded += previousSceneDataLoaded;
        GameEvents.current.onSceneTransitionComplete += sceneTransitionComplete;

        lancerChargingParticles = gunTip.GetChild(1).gameObject;
        shockBatonGun.createGun(3, 1, shockBatonRate, 20f, shockBatonDamage[0], shockBatonDamage[1], shockBatonBulletLife, 0, 1, 2, 1, 1, null);
        GameEvents.current.onReturnToTitle += returningToTitle;
    }

    private void previousSceneDataLoaded()
    {
        if(conductorCharges > 0)
            ConductorUIBehavior.instance.addCharges(conductorCharges);

        if (bloodVignette == null)
            bloodVignette = CanvasMaster.instance.bloodVignette;

        if(heads[0] == null)
        {
            // we need heads. I don't fucking know why.
            Transform myHead = transform.GetChild(1).transform;
            heads[0] = myHead.GetChild(0).gameObject;
            heads[1] = myHead.GetChild(1).gameObject;
            heads[2] = myHead.GetChild(2).gameObject;
            heads[3] = myHead.GetChild(3).gameObject;
        }

        if (skillTree == null)
        {
            initializeSkillTree();
        }

        if (GameDevTestOverrides.instance != null)
            GameDevTestOverrides.instance.TryApplyForcedSkillTree(skillTree);

        // TODO - update things based on skilltree, like health and what not?

        loadedHealth();
        InventoryManager.instance.recalculateMaxShields();
        HealthUIBehavior.instance.initalizeLoadedUI();
        updatePlayerAlignment();
        // Backfill act-based / migrated unlocks after load. Silent: no retroactive popups.
        AlignmentManager.reconcileUnlocks(false);

        kipController.toggleKipOnShoulders(hasKipling);
        FirebugUIBehavior.instance.setFirebugCount(maxFirebugs);
    }

    public void hasKiplingAccess(bool hasK)
    {
        hasKipling = hasK;

        kipController.toggleKipOnShoulders(hasKipling);
    }

    private void sceneTransitionComplete()
    {
        isMapOpen = false;
        activeHoverBike = null;
        freezeChar(false);
        MapMenuBehavior.instance.gameObject.SetActive(false);
    }

    private void returningToTitle()
    {
        GameEvents.current.onReturnToTitle -= returningToTitle;
        kipController.recallForTitle(); // recall a deployed Kipling before the player moves, so it isn't orphaned
        transform.parent = ObjectPooler.instance.transform;
    }

    protected override void Update()
    {
        if (!isDead && !isStunned)
        {
            if (!frozen)
            {
                processInputs(); // modifies movement direction and shooting controls
                checkForMove(); // check for move will move if movementDirection is modified
                
            }
            else
            {
                if (!Menus.instance.isRebinding && !isInConversation && !isCustomizing)
                {
                    if ((Input.GetKeyDown(menuKeyCode)) && !CanvasMaster.instance.isCutscenePlaying)
                    {
                        bool failed = false;

                        if (!isMapOpen && !isHomesteadOpen)
                        {
                            Menus.instance.closeMenu();
                        }
                        else if (isMapOpen)
                        {
                            if (activeHoverBike != null) activeHoverBike.deActivate();
                            activeHoverBike = null;
                            MapMenuBehavior.instance.gameObject.SetActive(false);
                            isMapOpen = false;
                        }
                        else if (isHomesteadOpen)
                        {
                            if (HomesteadUIBehavior.instance.tryClose())
                                isHomesteadOpen = false;
                            else
                                failed = true;
                        }

                        if (!failed)
                        {
                            AudioMaster.instance.PlaySound("Menu Close");
                            freezeChar(false);
                            GameEvents.current.menuClosed();
                        }
                    }
                    
                    if (Input.GetKeyUp(crafting))
                    {
                        HealthUIBehavior.instance.resetEventHappened();
                        freezeChar(false);
                        CraftMenuBehavior.instance.gameObject.SetActive(false);
                    }
                }
                else if(isInConversation)
                {
                    if (Input.GetKeyDown(KeyCode.Space))
                    {
                        EventManager.instance.skipTypewriterEffect();
                    }
                    else if(Input.anyKeyDown)
                    {
                        EventManager.instance.otherButtonPressedDuringDialogue();
                    }
                }
            }

            base.Update();
            if (hit && vigOn && Time.time > nextFade)
            {
                oscillateVig = isBleeding;
                hit = false;
            }

            if (oscillateVig)
            {
                bloodVignette.canvasRenderer.SetAlpha((Mathf.PingPong(Time.time, 4) + 3) / 10);
            }
            else if (vigOn && !hit)
            {
                vigOn = false;
                bloodVignette.CrossFadeAlpha(0, 0.5f, false);
            }

            if(shieldsDeployed) // make shield wall move to mouse
            {
                // Get the position of the mouse in world space
                Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

                // Calculate the direction from the GameObject to the mouse
                Vector3 direction = mousePosition - transform.position;

                // Calculate the angle in radians and convert it to degrees
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 180f;

                // Set the rotation of the GameObject to the calculated angle
                shieldWallTransform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
            }

        }
    }

    public KeyCode getInteractionKey(bool isKipling)
    {
        if (isKipling)
            return deployKipling;
        else
            return interact;
    }

    public KeyCode getStunKey()
    {
        return shockBatonNoConductor;
    }

    public KeyCode getReloadKey()
    {
        return reloadKeyCode;
    }

    public KeyCode getQuickUseKey(int quickUseSlot)
    {
        if (quickUseSlot == 1)
            return quickUse1;
        if (quickUseSlot == 2)
            return quickUse2;

        UnityEngine.Debug.LogWarning("Incorrect quick use slot requested.");
        return KeyCode.None;
    }

    // Current movement-input heading (normalized); Vector2.zero when the player isn't moving.
    public Vector2 getMovementDirection()
    {
        return movementDirection;
    }

    public override void damageCharacter(float damage, Vector2 d, bool fromBleeding, bool ignoreShields, bool generateBlood, bool crit)
    {
        if (godMode)
            return;

        if (!fromBleeding)
        {
            if (isInvincible)
                return;
            else
            {
                StartCoroutine(startInvincibilityFrames());
            }
        }

        bool shakeTime = true;
        if (drainingShieldsForAbility)
            shakeTime = false;
        
        base.damageCharacter(damage, d, fromBleeding, ignoreShields, generateBlood, crit); // NOTE: THIS MODIFIES drainingShieldsForAbility - rely on shakeTime from here.

        if (damagePassedShields)
        {           
            if (!fromBleeding)
            {
                playerProgressBarController.damageCheckForInterrupt();
                if(shakeTime)
                    ProCamera2DShake.Instance.Shake(SMALL_SHAKE);
                vigOn = true;
                oscillateVig = false;
                bloodVignette.canvasRenderer.SetAlpha(1);
                bloodVignette.CrossFadeAlpha(0.5f, 1f, false);
                nextFade = Time.time + hitLength;
                hit = true;
            }
            else if(shakeTime)
            {
                ProCamera2DShake.Instance.Shake(TINY_SHAKE);
            }

            if (health <= 0)
            {
                vigOn = false;
                oscillateVig = false;
                bloodVignette.CrossFadeAlpha(0, 0.5f, false);
            }

            if (!isBleeding)
            {
                oscillateVig = false;
            }

            damagePassedShields = false;
        }
        else
        {
            if(shakeTime)
            { 
                ProCamera2DShake.Instance.Shake(TINY_SHAKE);
                shieldSparkPS.transform.position = d;
                shieldSparkPS.Play();
            }
        }

        if(shakeTime)
            EventManager.instance.hitFreeze();

    }

    private IEnumerator startInvincibilityFrames()
    {
        isInvincible = true;
        yield return new WaitForSeconds(0.1f);
        isInvincible = false;
    }

    protected override void signalBleed(bool isBleeding)
    {
        HealthUIBehavior.instance.toggleBleed(isBleeding);
    }

    public void freezeChar(bool freeze)
    {
        if (freeze)
        {
            lockCount++;
            stopMoving();
        }
        else
        {
            lockCount--;
            if (lockCount <= 0)
            {
                lockCount = 0;
            }
            else
                return; // don't continue to unfreeze unless all frozen activities are resolved
        }

        frozen = freeze;

        ProCamera2D.Instance.GetComponent<ProCamera2DPointerInfluence>().enabled = !freeze; 
    }

    public void toggleConversation(bool toggle)
    {
        isInConversation = toggle;
        freezeChar(toggle);
    }

    private void emptyShieldsAnimAndSound()
    {
        shieldAnimator.SetTrigger("break");
        audioManager.PlaySound("ShieldSpark");
        audioManager.PlaySound("ShieldError");
    }

    private void processInputs()
    {
        if (!isDead)
        {
            movementDirection = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            movementSpeed = Mathf.Clamp(movementDirection.magnitude, 0.0f, 1.0f);
            movementDirection.Normalize();

            /*if(Input.GetKeyDown(shieldsKeyCode)) -- OVERHAULING SHIELDS
            {          
                if (!deployShields(getRelativeMousePos(transform.position)))
                    emptyShieldsAnimAndSound();
                else
                    playerProgressBarController.interruptableActionOccured();

            }
            else if(Input.GetKeyUp(shieldsKeyCode))
            {
                undeployShields();
            }  */  

            if (Input.GetMouseButton(0) && equippedItem.iType == InventoryItem.itemType.Gun)
            {
                firePlayerGun();
            }
            else if(Input.GetMouseButtonDown(0))
            {
                useEquipped();
            }

            if (Input.GetKeyDown(overloadKeyCode))
                attemptOverload();

            if (Input.GetKeyDown(slideKey))
                slide();

            if (Input.GetMouseButtonUp(0) && !mouseIsUpLancerReady)
                mouseIsUpLancerReady = true;

            if (Input.GetKeyDown(shockBaton))
                firePlayerBaton(false);

            if (Input.GetKeyDown(shockBatonNoConductor))
                firePlayerBaton(true);

            // Weapon slots take the number row; the quick use pair keeps its own bindings.
            // Alpha1..Alpha9 and Keypad1..Keypad9 are contiguous, hence the cap at 9.
            bool slotKeyPressed = false;
            for (int i = 0; i < InventoryManager.QUICK_USE_1_SLOT && i < 9; i++)
            {
                KeyCode alphaKey = (KeyCode)((int)KeyCode.Alpha1 + i);
                KeyCode keypadKey = (KeyCode)((int)KeyCode.Keypad1 + i);

                if (Input.GetKeyDown(alphaKey) || Input.GetKeyDown(keypadKey))
                {
                    switchEquipped(i, false, false, false);
                    slotKeyPressed = true;
                    break;
                }
            }

            if (!slotKeyPressed)
            {
                if (Input.GetKeyDown(quickUse1))
                    switchEquipped(InventoryManager.QUICK_USE_1_SLOT, false, true, false);
                else if (Input.GetKeyDown(quickUse2))
                    switchEquipped(InventoryManager.MEDKIT_SLOT, false, true, false);
                else if (Input.mouseScrollDelta.y > 0)
                    switchEquipped(-1, true, false, false);
                else if (Input.mouseScrollDelta.y < 0)
                    switchEquipped(1, true, false, false);
            }
                       
            if (Input.GetKeyDown(deployKipling))
                kipController.sendKiplingOut(false); 

            if(Input.GetKeyDown(interact))
            {
                interactWithTarget();
            }

            if(Input.GetKeyDown(menuKeyCode))
            {
                HealthUIBehavior.instance.resetEventHappened();
                AudioMaster.instance.PlaySound("Menu Open");
                freezeChar(true);
                WorldspaceCanvas.instance.deActivateCard();
                Menus.instance.gameObject.SetActive(true);                
            }

            if(Input.GetKeyDown(crafting) && !shieldsDeployed)
            {
                HealthUIBehavior.instance.resetEventHappened();

                AudioMaster.instance.PlaySound("Open Bag", true);
                freezeChar(true);
                WorldspaceCanvas.instance.deActivateCard();
                CraftMenuBehavior.instance.gameObject.SetActive(true);
            }

            if(Input.GetKeyDown(reloadKeyCode))
            {
                tryReload();
            }

            if(Input.GetKeyDown(turretKeyCode))
            {
                attemptTurretPlacement();
            }
            else if(Input.GetKeyUp(turretKeyCode))
            {
                placeTurret();
            }

            if(Input.GetKeyDown(firebugKeyCode))
            {
                attemptFirebugPlacement();
            }
            else if(Input.GetKeyUp(firebugKeyCode))
            {
                placeFirebug();
            }

            if(Input.GetKeyDown(recallFirebugKeyCode))
            {
                recallFirebug();
            }
        }

        // put UI access code here, outside of game over, maybe. If not, can move into update
    }

    public void tryReload()
    {
        if (equippedGun == null)
            return;

        if((equippedGun.bulletsInMag < equippedGun.magSize) && (InventoryManager.instance.getAmmo(equippedGun.mType) > 0) && !isSliding && !shieldsDeployed) // room in mag + bullets
        {
            audioManager.PlaySound(gunNamePrefix[equippedGun.gType] + "Start Load");

            float reloadTime = reloadTimeArray[equippedGun.gType];

            reloadContext ctx = new reloadContext();
            ctx.reloadTime = reloadTime;
            ctx.gun = equippedGun;
            ctx.failReload = false;
            GameEvents.current.reloadStart(ctx);
            reloadTime = ctx.reloadTime;

            if(ctx.failReload)
                return;

            PlayerProgressBarController.instance.attemptReload(reloadTime, equippedGun.mType);
        }
    }

    public void playFinalReloadSound()
    {
        audioManager.PlaySound(gunNamePrefix[equippedGun.gType] + gunModelPrefix[equippedGun.mType] + "Finish Load");
    }

    public void updateCurrencies(int changeInTechCount, int changeInCompCount)
    {
        techCount += changeInTechCount;
        if (techCount < 0)
            techCount = 0;

        componentCount += changeInCompCount;
        if (componentCount < 0)
            componentCount = 0;

        if (changeInTechCount > 0 || changeInCompCount > 0)
            SmallAlertsBehavior.instance.displayCurrencyAlert(changeInCompCount, changeInTechCount);

        InventoryMenu.instance.updateCurrencies();
    }

    public void openMap(HoverBikeBehavior sourceBike)
    {
        // two bikes activated before the first one's map opens: power down the older one so only one stays running
        if (activeHoverBike != null && activeHoverBike != sourceBike) activeHoverBike.deActivate();
        activeHoverBike = sourceBike;
        freezeChar(true);
        WorldspaceCanvas.instance.deActivateCard();
        MapMenuBehavior.instance.gameObject.SetActive(true);
        isMapOpen = true;
    }

    private void interactWithTarget()
    {
        if (!ObjectSelector.current.isTouchableSelected)
            return;

        playerProgressBarController.interruptableActionOccured();
        WorldspaceCanvas.instance.signalFailure();

        WorldspaceCanvas.instance.endHelpText(false); // turn off contextual popup since player knows what's goin on

        switch(ObjectSelector.current.selectedTouchable.gameObject.GetComponent<Interactable>().interactableType)
        {
            case 0:
                ObjectSelector.current.selectedTouchable.GetComponent<GeneratorBehavior>().toggle();
                break;
            case 1:                
                Loot loot = ObjectSelector.current.selectedTouchable.GetComponent<Loot>();

                if(ObjectSelector.current.selectedTouchable == ObjectSelector.current.selectedWithPointer)
                    HUDUIBehavior.instance.closePopup();

                if (loot.item.iType == InventoryItem.itemType.Armor)
                {
                    bool quickEquip = true;
                    quickEquip = InventoryManager.instance.equipArmor(loot.item as Armor);
                    if (quickEquip)
                        audioManager.PlaySound("Quick Equip", true);
                    else
                        audioManager.PlaySound("Equip Unequip");
                }
                else if(loot.item.iType == InventoryItem.itemType.RifleAmmo)
                {
                    InventoryManager.instance.addBullet(0, loot.item.count);
                    audioManager.PlaySound("Ammo Pickup");
                }
                else if (loot.item.iType == InventoryItem.itemType.ShotgunAmmo)
                {
                    InventoryManager.instance.addBullet(2, loot.item.count);
                    audioManager.PlaySound("Electro Pickup");
                }
                else if (loot.item.iType == InventoryItem.itemType.Cloth)
                {   
                    if(InventoryManager.instance.isCraftingMatFull(0))
                    {
                        genericErrorFeedback(loot.gameObject.transform.position);
                        break;
                    }
                    InventoryManager.instance.addCraftingMat(0, loot.item.count);
                    audioManager.PlaySound("Quick Equip", true);
                }
                else if (loot.item.iType == InventoryItem.itemType.Chemical)
                {
                    if (InventoryManager.instance.isCraftingMatFull(1))
                    {
                        genericErrorFeedback(loot.gameObject.transform.position);
                        break;
                    }
                    InventoryManager.instance.addCraftingMat(1, loot.item.count);
                    audioManager.PlaySound("Quick Equip", true);
                }
                else if (loot.item.iType == InventoryItem.itemType.Scrap)
                {
                    if (InventoryManager.instance.isCraftingMatFull(2))
                    {
                        genericErrorFeedback(loot.gameObject.transform.position);
                        break;
                    }
                    InventoryManager.instance.addCraftingMat(2, loot.item.count);
                    audioManager.PlaySound("Quick Equip", true);
                }
                else if(loot.item.iType == InventoryItem.itemType.Tech)
                {
                    updateCurrencies(loot.item.count, 0);
                    audioManager.PlaySound("Quick Equip", true);
                    AudioMaster.instance.PlaySound("Tech Pickup");
                }
                else
                {
                    if (loot.item.iType == InventoryItem.itemType.Gun)
                    {
                        Gun tempGun = loot.item as Gun;
                      
                        if(tempGun.gType == 3) // baton
                        {
                            audioManager.PlaySound("Quick Equip", true);
                            hasBaton = true;
                            loot.pickup();
                            return;
                        }

                        audioManager.PlaySound(gunNamePrefix[tempGun.gType] + gunModelPrefix[tempGun.mType] + "Select");
                    }
                    else
                        audioManager.PlaySound("Quick Equip", true);

                    int slot = InventoryManager.instance.getNewEquipSlot(loot.item);
                    InventoryManager.instance.equip(slot, loot.item);
                    switchEquipped(InventoryManager.instance.equippedItemIndex, false, false, false);           
                }
                
                loot.pickup();                                
                break;
            case 2:
                if (isInCombat || isSliding) break;
                if (isBleeding) { genericErrorFeedback(); break; }
                ObjectSelector.current.selectedTouchable.GetComponent<DialogueSystemTrigger>().OnUse();
                break;
            case 3:
                if (isInCombat || isSliding) break;
                if (isBleeding) { genericErrorFeedback(); break; }
                ObjectSelector.current.selectedTouchable.gameObject.GetComponent<RecorderBehavior>().use();
                break;
            case 5:
                ObjectSelector.current.selectedTouchable.GetComponent<Vent>().use();
                break;
            case 7:
                if (!isInCombat && !isSliding)
                {
                    ObjectSelector.current.selectedTouchable.GetComponent<HoverBikeBehavior>().activate();
                    ObjectSelector.current.UnselectTouchable();
                }
                break;
            case 8:
                if (!isInCombat && !isSliding)
                {
                    freezeChar(true);
                    isHomesteadOpen = true;
                    AudioMaster.instance.PlaySound("Menu Open");
                    WorldspaceCanvas.instance.deActivateCard();
                    HomesteadUIBehavior.instance.gameObject.SetActive(true);
                }
                break;
            case 9:
                ObjectSelector.current.selectedTouchable.GetComponent<PuzzleButtonBehavior>().press();
                break;
            case 10:
                if (isInCombat || isSliding) break;
                if (isBleeding) { genericErrorFeedback(); break; }
                ObjectSelector.current.selectedTouchable.GetComponent<DialogueSystemTrigger>().OnUse();
                break;
            case 11:
                ObjectSelector.current.selectedTouchable.GetComponent<LootBox>().open();
                break;
            case 12:
                if (isBleeding || health < healthThresholds[activeHealthModule])
                {
                    ObjectSelector.current.selectedTouchable.GetComponentInParent<Clotberry>().useClotberry();
                    heal(5f, -1, false);
                }
                else
                {
                    genericErrorFeedback(ObjectSelector.current.selectedTouchable.transform.position + new Vector3(0f, 2f, 0f));
                    return;
                }
                break;
            default:
                break;
        }
    }

    private void slide()
    {
        if (infestationHandler.isInfested)
            return;
        
        trySlide(); // deployed Kipling is scene-rooted, so sliding no longer drags it

        playerProgressBarController.interruptableActionOccured();
        WorldspaceCanvas.instance.signalFailure();
        lancerCharging = false;                    
        
    }

    public override void stopSliding()
    {
        base.stopSliding();
    }

    protected override void onSlideTick()
    {
        Vector2 probeCenter = (Vector2)transform.position + standingCollider.offset;
        Vector2 probeSize = Vector2.Scale(standingCollider.size, transform.lossyScale);
        if (infestationHandler.isPointInUnlitInfestation(probeCenter, probeSize))
            abortSlide();
    }

    private void firePlayerGun()
    {
        if (shieldsDeployed || !weaponsAllowed || infestationHandler.isInfested)
        {
            genericErrorFeedback();
            return;
        }

        if(displayingShock) // override shock display and swap guns back
        {
            changeActiveGunStats();
            displayingShock = false;
        }
      
        if (equippedGun.bulletsInMag > 0)
        {
            playerProgressBarController.interruptableActionOccured();

            if (equippedGun.gType == 1)
            {
                if (!lancerCharging && mouseIsUpLancerReady && !isSliding && (Time.time > slideCooldown))
                    StartCoroutine(startCharging());

                MouseManager.instance.swapReticle(true, true);
            }
            else
            {
                if ((Time.time > nextFire) && !isSliding && (Time.time > slideCooldown) && gunEquipped)
                {
                    shootAttemptContext ctx = new shootAttemptContext();
                    ctx.gun = equippedGun;
                    GameEvents.current.attemptingShot(ctx);

                    float fireSpeedToPass = equippedGun.fireSpeed + ctx.additionalFireTime;

                    WorldspaceCanvas.instance.beginCooldown(fireSpeedToPass);
                    nextFire = Time.time + fireSpeedToPass;

                    MouseManager.instance.swapReticle(true, true);
                    HealthUIBehavior.instance.resetEventHappened();

                    if (ctx.preventShot)
                        return;

                    createBullets();                  
                }
                else if(Time.time <= nextFire)
                {
                    WorldspaceCanvas.instance.signalFailure();
                }
                    
            }
        }
        else
        {
            if ((Time.time > nextFire) && !isSliding && (Time.time > slideCooldown) && gunEquipped)
            {
                audioManager.PlaySound("Empty Magazine");
                HUDUIBehavior.instance.flashOutOfAmmo();

                if(InventoryManager.instance.getAmmo(equippedGun.mType) > 0)
                    WorldspaceCanvas.instance.popUpReloadEmote();

                nextFire = Time.time + equippedGun.fireSpeed;
            }
        }
    }

    private void firePlayerBaton(bool ignoreConductor)
    {
        if (!hasBaton)
            return;

        if ((Time.time > nextBatonFire) && !isSliding && (Time.time > slideCooldown))
        {
            HealthUIBehavior.instance.resetEventHappened();
            MouseManager.instance.swapReticle(true, true);
            if ((shields >= (maxShields * 0.33f)) || ((conductorCharges > 0) && !ignoreConductor)) // hard coded 0.33 instead of batonShieldCost
            {
                playerProgressBarController.interruptableActionOccured();
                WorldspaceCanvas.instance.signalFailure();

                equippedItem = shockBatonGun;
                changeActiveGunStats();

                nextBatonFire = Time.time + shockBatonGun.fireSpeed;
                WorldspaceCanvas.instance.beginCooldown(shockBatonGun.fireSpeed);
                createBullets(shockBatonGun, true, ignoreConductor, false);

                if (conductorCharges == 0 || ignoreConductor)
                {
                    drainingShieldsForAbility = true;
                    damageCharacter((maxShields / 3f), Vector2.zero, false, false, false, false); // hard coded instead of batonShieldCost
                }
                else
                {
                    audioManager.PlaySound("Conductor Shot");
                    conductorCharges--;
                    ConductorUIBehavior.instance.useCharge();
                }

                equippedItem = InventoryManager.instance.getEquipped();
                if (equippedItem.iType == InventoryItem.itemType.Gun)
                    StartCoroutine(changeFromBaton());
            }
            else
            {
                audioManager.PlaySound("Empty Magazine");
                emptyShieldsAnimAndSound();
            }
        }
    }

    private IEnumerator changeFromBaton() // needed to maintain correct spritesheet through the animation
    {
        displayingShock = true;   
        yield return new WaitForSeconds(1f);
        if(displayingShock) // still displaying shock? change.
        {
            changeActiveGunStats();
            displayingShock = true;
        }       
    }

    private IEnumerator startCharging()
    {
        float timeElapsed = 0.0f;
        lancerCharging = true;
        mouseIsUpLancerReady = false;
        lancerChargingParticles.SetActive(true);
        audioManager.PlaySound("Lancer_Charge_SFX");
        while (timeElapsed < equippedGun.fireSpeed)
        {
            shoot(Camera.main.ScreenToWorldPoint(Input.mousePosition), equippedGun.gType, addCritDmg, true);
            yield return null;

            if (!Input.GetMouseButton(0))
            {
                lancerCharging = false;
                stopLancerCharging();
            }

            if (!lancerCharging)
                break;
           
            timeElapsed += Time.deltaTime;
        }

        if (lancerCharging)
        {
            lancerCharging = false;
            createBullets();            
        }

        lancerChargingParticles.SetActive(false);
        audioManager.StopSound("Lancer_Charge_SFX");
    }

    public void createBullets()
    {
        bool check = WorldspaceCanvas.instance.checkForTrueAim();
        createBullets(equippedGun, false, false, check);
    }

    private void createBullets(Gun gunToShoot, bool isElectroBaton, bool ignoreConductor, bool isTrueAim)
    {
        // process mouse direction to dictate shooting direction
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (!isTrueAim) // only apply sway if we aren't trueAim
        {
            float sway = swayController.instance.swayByGunType[gunToShoot.mType] + swayModifier;
            float posSway = Random.Range(-1 * sway, sway);
            mouseWorldPos += (new Vector2(posSway, posSway) * swayController.instance.swayModifier);
        }

        int gunTypeToPass = gunToShoot.gType;

        /*if (equippedGun.bulletsInMag <= 0 && gunTypeToPass == 0) // no bullets but for trad, produce shitty bullet
            gunTypeToPass = -1;*/

        shoot(mouseWorldPos, gunTypeToPass, addCritDmg, false, ignoreConductor, isTrueAim); // subtract / add gunTip pos?`  
        swayController.instance.recoil(gunToShoot.fireSpeed);
        ProCamera2DShake.Instance.Shake(SMALL_SHAKE);

        // kickback
        // Move the player in the opposite direction from the mouse cursor.
        Vector3 startPos = transform.position;
        Vector3 direction = Camera.main.ScreenToWorldPoint(Input.mousePosition) - startPos;
        Vector3 targetPos = startPos - (direction.normalized * gunToShoot.kickBackAmount);
        StartCoroutine(CharmBehaviorManager.instance.TeleportLerp(gameObject, targetPos, 0.1f));

        if (gunTypeToPass == -1)
            audioManager.PlaySound("Empty_Fire", true);

        playGunSFX();

        Transform parent = ObjectPooler.instance.transform;

        GameObject thisShell = shell;
        if (gunTypeToPass == 1)
            thisShell = lancerShell;
        else if (gunTypeToPass == 2)
        {
            thisShell = electroShell;
            //parent = transform; // BUG - causes particles to spawn behind player...
        }
        else if (gunTypeToPass == 3)
        {
            if (conductorCharges == 0 || ignoreConductor)
                thisShell = batonShell;
            else
                thisShell = modifiedBatonShell;
        }

        if(gunTypeToPass != -1)
            Instantiate(thisShell, gun.transform.position, Quaternion.identity, parent);

        if (!isElectroBaton)
        {
            if (Time.time > nextNoiseCheck)
                noiseCheck();

            InventoryManager.instance.useBullet(gunToShoot.mType, bulletCount);
        }       
    }

    public void lockAnimToGun()
    {
        lockAnim(getRelativeMousePos(gunTip.position));

        showGun(lockedDirection, directionTriggersGun);
        gunAnimator.Play(directionIdleGun[lockedDirection]);


        if (isMoving && headAnimator.GetCurrentAnimatorStateInfo(0).IsName("Head_Bob"))
            headAnimator.SetTrigger("StopRunning");

        triggerAnim(lockedDirection, directionTriggersShooting);

        isRecoiling = true;
    }

    private Vector2 getRelativeMousePos(Vector3 originPoint)
    {
        Vector2 originPosition = Camera.main.WorldToScreenPoint(originPoint);
        Vector2 relativeMousePosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y) - originPosition;
        return relativeMousePosition;
    }

    private void noiseCheck()
    {
        Collider2D[] colls = Physics2D.OverlapCircleAll(transform.position, noiseRadius, 1 << LayerMask.NameToLayer("EnemyBlockingLayer"));
        for(int i = 0; i < colls.Length; i++)
        {
            Enemy e = colls[i].gameObject.GetComponent<Enemy>();
            if(e != null)
                e.aggro();
        }
        nextNoiseCheck = Time.time + 2.0f;
    }

    protected override void triggerDeath(int d)
    {
        // Capture before base.triggerDeath sets slideSlowdownModifier=0.985f and the
        // corpse drifts. dropComponentsOnDeath reads this ~4s later (after fade), so
        // recording transform.position there would land on the drift point instead.
        fatalHitPosition = transform.position;
        GameEvents.current.playerDeath();
        EventManager.instance.playerDeath(); // slow down time
        GetComponent<ProCamera2DCinematics>().Play();
        unsubscribe();
        HealthUIBehavior.instance.healthUpdate(); // force health update before the script gets deleted.
        kipController.sendKiplingOut(true);
        base.triggerDeath(d);
    }

    public void dropComponentsOnDeath(string sceneName)
    {
        deathDropScene = sceneName;
        deathDropPos = fatalHitPosition;
        deathDropComponentCount = componentCount;
        updateCurrencies(0, -componentCount);
    }


    private void OnDestroy()
    {
        unsubscribe();
    }

    private void unsubscribe()
    {
        GameEvents.current.onAggro -= OnAggro;
        GameEvents.current.onUnaggro -= OnUnaggro;
        GameEvents.current.onPreviousSceneDataLoaded -= previousSceneDataLoaded;
        GameEvents.current.onSceneTransitionComplete -= sceneTransitionComplete;
    }

    public override void characterDeath()
    {
        //kipController.sendKiplingOut(true);
        base.characterDeath();
    }

    // Reverses the live-object state set by triggerDeath / Character.characterDeath
    // so the player can be safely saved and reloaded as alive. Called from
    // EventManager.deathPostProcessTransition right before the death-save.
    // Does NOT touch inventory, ammo, or equipment — only revives the actor.
    public void prepareForRespawnSave()
    {
        isDead = false;
        isBleeding = false;
        signalBleed(isBleeding);
        invulnerable = false;
        damagePassedShields = false;

        health = maxHealth;
        loadedHealth();

        // CombatChar.triggerDeath overwrites slideSlowdownModifier with the death
        // deceleration value (0.985f). Restore the prefab baseline so that value
        // doesn't get serialized into the slot.
        slideSlowdownModifier = aliveSlideSlowdownModifier;

        var mainCollider = this.GetComponent<BoxCollider2D>();
        if (mainCollider != null) mainCollider.enabled = true;
        foreach (BoxCollider2D bc in GetComponentsInChildren<BoxCollider2D>(true))
            bc.enabled = true;

        if (shadowCaster != null) shadowCaster.enabled = true;

        if (chestAnimator != null) chestAnimator.SetBool("IsDead", false);
        if (headAnimator != null)
        {
            headAnimator.ResetTrigger("DeathRight");
            headAnimator.ResetTrigger("DeathLeft");
        }
        deathDirectionModifier = 1;

        saturateEntireBody();

        // Cancel the death-side Kipling deploy + pathing so the save captures
        // Kipling back on the player's shoulders rather than mid-mourning state.
        if (kipController != null)
            kipController.recallForRespawn();

        // triggerDeath calls unsubscribe(); re-attach the same handlers Start()
        // wired up. The -= guards against double-subscribe if this ever runs twice.
        GameEvents.current.onAggro -= OnAggro;
        GameEvents.current.onAggro += OnAggro;
        GameEvents.current.onUnaggro -= OnUnaggro;
        GameEvents.current.onUnaggro += OnUnaggro;
        GameEvents.current.onPreviousSceneDataLoaded -= previousSceneDataLoaded;
        GameEvents.current.onPreviousSceneDataLoaded += previousSceneDataLoaded;
        GameEvents.current.onSceneTransitionComplete -= sceneTransitionComplete;
        GameEvents.current.onSceneTransitionComplete += sceneTransitionComplete;

        if (HealthUIBehavior.instance != null)
            HealthUIBehavior.instance.healthUpdate();
    }

    public void switchEquipped(int index, bool isRelative, bool actionKey, bool recurse) 
    {
        if(PlayerProgressBarController.instance != null)
            PlayerProgressBarController.instance.changedEquippedCheckForInterrupt();

        bool noChange = false;
        InventoryItem prevItem = InventoryManager.instance.getEquipped();

        bool usable = false;
        if (recurse)
        {
            lastEquippedIndex = InventoryManager.instance.changeEquipped(index, isRelative, true);
        }
        else if (!actionKey)
        {
            lastEquippedIndex = InventoryManager.instance.changeEquipped(index, isRelative, actionKey);
        }
        else
        {
            InventoryManager.instance.changeEquipped(index, isRelative, actionKey);
        }

        equippedItem = InventoryManager.instance.getEquipped();

        if (equippedItem == prevItem)
            noChange = true;

        if (gunEquipped)
            GameEvents.current.gunUnequip(equippedGun);

        if (equippedItem.iType == InventoryItem.itemType.Gun)
        {
            changeActiveGunStats();
            if(!noChange && !recurse)
                audioManager.PlaySound(gunNamePrefix[equippedGun.gType] + gunModelPrefix[equippedGun.mType] + "Select");

            GameEvents.current.gunEquip(equippedItem as Gun);
        }
        else 
            gunEquipped = false;

        if(equippedItem.iType == InventoryItem.itemType.Lantern || equippedItem.iType == InventoryItem.itemType.Throwable || equippedItem.iType == InventoryItem.itemType.Healable)
        {
            usable = true;               
        }

        if (actionKey && (usable || equippedItem.iType == InventoryItem.itemType.Empty))
        {
            if (equippedItem.iType != InventoryItem.itemType.Empty)
                useEquipped();
            switchEquipped(lastEquippedIndex, false, false, true);
        }
        else
        {
            if(!usable && actionKey)
                InventoryManager.instance.changeEquipped(index, false, false);
            WorldspaceCanvas.instance.signalItemSwap(); // to compare new item to item on ground
        }
    }

    public void changeActiveGunStats()
    {
        equippedGun = equippedItem as Gun;

        equippedGunSFX = gunSFX[equippedGun.gType][equippedGun.mType];
        bulletSpeed = equippedGun.bulletSpeed;
        bulletLifetime = equippedGun.range;
        //nextFire = Time.time + equippedGun.fireSpeed;
        bulletCount = equippedGun.bulletCount;
        damage = equippedGun.damage;
        critChance = equippedGun.critChance;
        addCritDmg = equippedGun.critDmg;

        muzzleFlashLight.GetComponent<Light2D>().color = muzzleFlashColors[equippedGun.gType];

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
                    case 2:
                        gunSpriteSheetName = "Lancer_Shotgun";
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
                    case 1:
                        gunSpriteSheetName = "Electrospike_Pistol";
                        break;
                    case 2:
                        gunSpriteSheetName = "Electrospike_Shotgun";
                        break;
                    default:
                        break;
                }
                break;
            case 3:
                gunSpriteSheetName = "Electro_Shock_Baton"; // TO DO: Change to shock baton
                break;
            default:
                break;
        }

        gunEquipped = true;
    }

    private void useEquipped()
    {
        if(infestationHandler.isInfested) // can't use items while infested
        {
            genericErrorFeedback();
            return;
        }

        HealthUIBehavior.instance.resetEventHappened();
        //UnityEngine.Debug.Log(equippedItem.iType);
        playerProgressBarController.interruptableActionOccured();
        WorldspaceCanvas.instance.signalFailure();
        //UnityEngine.Debug.Log("Equipped item type: " + equippedItem.iType);
        if(equippedItem.iType == InventoryItem.itemType.Throwable && weaponsAllowed)
        {
            InventoryManager.instance.useStackable(InventoryManager.instance.equippedItemIndex);
            Throwable throwable = equippedItem as Throwable;

            if(throwable.tType == Throwable.throwableType.Firebomb)
                huckaNade(getRelativeMousePos(transform.position), 1, 7f, 7f); // Hard coded firebomb dmg values
            else
                huckaNade(getRelativeMousePos(transform.position), 0, 5f, 200f); // Hard coded grenade dmg values


        }
        else if(equippedItem.iType == InventoryItem.itemType.Healable)
        {         
            Healable healable = equippedItem as Healable;

            if (healable.hType == Healable.healableType.Bandage)
            {
                if (!isBleeding && health >= healthThresholds[activeHealthModule])
                {
                    genericErrorFeedback();
                    return;
                }
            }
            else if(healable.hType == Healable.healableType.Medkit)
            {
                if (health >= maxHealth)
                {
                    genericErrorFeedback();
                    return;
                }
            }

            playerProgressBarController.attemptHeal(healable.hType, InventoryManager.instance.equippedItemIndex);
        }
        else if(equippedItem.iType == InventoryItem.itemType.Conductor)
        {
            Conductor con = equippedItem as Conductor;
            playerProgressBarController.attemptConductor(con.charges, InventoryManager.instance.equippedItemIndex);
        }
    }

    public void equipCharges(int charges, int index)
    {
        InventoryManager.instance.useStackable(index);
        conductorCharges = charges;
        ConductorUIBehavior.instance.addCharges(charges);
    }

    private void attemptOverload()
    {
        if (!canUseAbility(1f) || !hasOverloadSkill())
            return;

        initiateOverload(Mathf.Floor(shields), transform.position);
        useShieldsForAbility(shields);

    }

    public void initiateOverload(float power, Vector3 location)
    {
        processAbilityInstance(ref instantiatedOverload, ref overloadPrefab, location);
        instantiatedOverload.GetComponent<OverloadBehavior>().triggerOverload(power);
    }

    private void attemptTurretPlacement()
    {
        if (!hasTurretSkill())
            return;

        // If turret is already deployed, recall it instead
        if (instantiatedTurret != null && instantiatedTurret.GetComponent<TurretBehavior>().isDeployed)
        {
            instantiatedTurret.GetComponent<TurretBehavior>().recall();
            return;
        }

        processAbilityInstance(ref instantiatedTurret, ref turretPrefab, transform.position);

        TurretBehavior turretBehavior = instantiatedTurret.GetComponent<TurretBehavior>();

        if (!turretBehavior.canPlaceTurret() || !canUseAbility(1f))
            return;

        turretBehavior.activatePlacerMode();
    }

    // True while the turret is deployed (or deploying). Causes maxShields to be
    // reduced by 1 until the turret is recalled. Read by InventoryManager.recalculateMaxShields().
    [System.NonSerialized]
    [ES3NonSerializable]
    public bool turretShieldCostActive = false;

    public void signalTurretComplete()
    {
        turretShieldCostActive = false;
        InventoryManager.instance.recalculateMaxShields();
    }

    private void placeTurret()
    {
        if (instantiatedTurret == null || !instantiatedTurret.GetComponent<TurretBehavior>().activateTurretMode())
            return;

        useShieldsForAbility(1f);

        turretShieldCostActive = true;
        InventoryManager.instance.recalculateMaxShields();
    }

    private void attemptFirebugPlacement()
    {
        // Clean up any destroyed firebugs
        instantiatedFirebugs.RemoveAll(f => f == null || !f.GetComponent<FirebugBehavior>().isActive);

        if (instantiatedFirebugs.Count >= maxFirebugs)
            return;

        int slot = findFreeFirebugSlot();
        if (slot < 0)
            return;

        GameObject firebug = Instantiate(firebugPrefab, transform.position, Quaternion.identity);
        instantiatedFirebugs.Add(firebug);

        FirebugBehavior firebugBehavior = firebug.GetComponent<FirebugBehavior>();
        firebugBehavior.firebugIndex = slot;

        if (!firebugBehavior.canPlace())
        {
            instantiatedFirebugs.Remove(firebug);
            Destroy(firebug);
            return;
        }

        firebugBehavior.activatePlacerMode();
    }

    private int findFreeFirebugSlot()
    {
        bool[] used = new bool[maxFirebugs];
        foreach (GameObject fb in instantiatedFirebugs)
        {
            if (fb == null) continue;
            int idx = fb.GetComponent<FirebugBehavior>().firebugIndex;
            if (idx >= 0 && idx < maxFirebugs)
                used[idx] = true;
        }
        for (int i = 0; i < maxFirebugs; i++)
            if (!used[i]) return i;
        return -1;
    }

    private void placeFirebug()
    {
        if (instantiatedFirebugs.Count == 0)
            return;

        GameObject latest = instantiatedFirebugs[instantiatedFirebugs.Count - 1];

        if (latest == null)
            return;

        latest.GetComponent<FirebugBehavior>().activateDeployMode();
    }

    private void recallFirebug()
    {
        for (int i = 0; i < instantiatedFirebugs.Count; i++)
        {
            if (instantiatedFirebugs[i] != null && instantiatedFirebugs[i].GetComponent<FirebugBehavior>().isDeployed)
            {
                instantiatedFirebugs[i].GetComponent<FirebugBehavior>().recall();
                return;
            }
        }
    }

    public void addMaxFirebugs()
    {
        maxFirebugs++;
        FirebugUIBehavior.instance.setFirebugCount(maxFirebugs);
    }

    public int getMaxFirebugs()
    {
        return maxFirebugs;
    }

    private bool canUseAbility(float minimumCost)
    {
        if(shields < minimumCost)
        {
            audioManager.PlaySound("Empty Magazine");
            emptyShieldsAnimAndSound();
            return false;
        }

        return true;
    }

    private void processAbilityInstance(ref GameObject instance, ref GameObject prefab, Vector3 location)
    {
        if (instance == null)
            instance = GameObject.Instantiate(prefab, location, Quaternion.identity);
        else
            instance.transform.position = location;
    }

    private void useShieldsForAbility(float cost)
    {
        drainingShieldsForAbility = true;
        damageCharacter(cost, transform.position, false, false, false, false);
    }

    public override void heal(float healAmt, int index)
    {
        heal(healAmt, index, true);
    }

    // pass -1 if not using item, otherwise index of item to use up
    public override void heal(float healAmt, int index, bool canHealOrb)
    {
        oscillateVig = false;
        if (index >= 0)
        {
            InventoryManager.instance.useStackable(index);
        }
        base.heal(healAmt, index, canHealOrb);

        GameEvents.current.playerHeal();
    }


    protected override void signalDirectionChange(int d)
    {
        base.signalDirectionChange(d);
        kipController.changeDirection(d);
    }

    protected override void signalStartMove()
    {
        playerProgressBarController.movingCheckForInterrupt();
    }

    // used to stop player by dialogue manager
    public void stopMoving()
    {
        movementDirection = Vector2.zero;
        checkForMove();
    }

    public void OnAggro()
    {
        isInCombat = true;
        HealthUIBehavior.instance.cancelEventHappened();
    }

    public void OnUnaggro()
    {
        isInCombat = false;
    }

    public void updateKeyBindings(string name, KeyCode code)
    {
        if (name == "Quick Use 1") // update HUD text!
        {
            HotbarUIBehavior.instance.updateQuickUseText(1, code.ToString());
            quickUse1 = code;
        }
        else if (name == "Quick Use 2")
        {
            quickUse2 = code;
            HotbarUIBehavior.instance.updateQuickUseText(2, code.ToString());
        }
        else if (name == "Slide")
            slideKey = code;
        else if (name == "Interact")
            interact = code;
        else if (name == "Deploy Kipling")
            deployKipling = code;
        else if (name == "Menu")
            menuKeyCode = code;
        else if (name == "Crafting")
            crafting = code;
        else if (name == "Shock Baton")
            shockBaton = code;
        else if (name == "No Conductor")
            shockBatonNoConductor = code;
        else if (name == "Reload")
            reloadKeyCode = code;
        else if (name == "Shield Wall")
            shieldsKeyCode = code;
        else if (name == "Overload")
            overloadKeyCode = code;
        else if (name == "Turret")
            turretKeyCode = code;
        else if (name == "Deploy Firebug")
            firebugKeyCode = code;
        else if (name == "Recall Firebug")
            recallFirebugKeyCode = code;
        else
            UnityEngine.Debug.LogError("Failed to re-key... Name passed was wrong.");

    }

    private void upgradeBaton()
    {
        shockBatonGun.range = upgradedBatonRange;
        shockBatonGun.bulletSpeed = upgradedBatonSpeed;
        shockBatonGun.tier = 3;
        shockBatonGun.rarity = 4;
        isBatonUpgraded = true;
    }

    // Dev-only hooks driven by GameDevTestOverrides. Thin public wrappers so the private
    // implementation/state above stays encapsulated.
    public void devUpgradeBaton()
    {
        upgradeBaton();
    }

    // remembers the pre-cheat walk speed so the 3x can be cleanly reverted
    private float devStoredWalkSpeed = -1f;
    private BoxCollider2D devTerrainCollider;

    // Dev-only: while `on`, disable the Terrain Collider (noclip) and triple walk speed.
    public void devSetFreeMovement(bool on)
    {
        if (devStoredWalkSpeed < 0f)
            devStoredWalkSpeed = originalSpeed; // captured in Character.Start()

        float target = on ? devStoredWalkSpeed * 3f : devStoredWalkSpeed;
        baseSpeed = target;
        originalSpeed = target; // keep SlowTime-exit / resumeOriginalAnimAndSpeed resets at the cheat value

        if (devTerrainCollider == null)
        {
            Transform terrain = transform.Find("Terrain Collider");
            if (terrain != null)
                devTerrainCollider = terrain.GetComponent<BoxCollider2D>();
        }
        if (devTerrainCollider != null)
            devTerrainCollider.enabled = !on;
    }

    public void devToggleGodMode()
    {
        if (godMode)
        {
            UnityEngine.Debug.Log("GOD MODE OFF");
            godMode = false;
            return;
        }

        // GOD MODE!
        if (equippedGun != null)
        {
            UnityEngine.Debug.Log("GOD MODE ON");
            damage[0] = 1000;
            damage[1] = 1000;
            godMode = true;
        }
    }

    public Gun returnShockBatonGun()
    {
        return shockBatonGun;
    }

    public bool checkForBatonUpgraded()
    {
        return isBatonUpgraded;
    }

    // Positive purpose = solo
    // Positive affinity = order
    public void updateAlignment(int purpose, int affinity)
    {
        Vector2 alignmentUpdate = new Vector2(purpose, affinity);

        alignmentHistory.Add(alignmentUpdate);

        alignmentArray += alignmentUpdate;

        if (alignmentArray.x > 100)
            alignmentArray.x = 100;
        else if (alignmentArray.x < -100)
            alignmentArray.x = -100;

        if (alignmentArray.y > 100)
            alignmentArray.y = 100;
        else if (alignmentArray.y < -100)
            alignmentArray.y = -100;

        updatePlayerAlignment();

        SmallAlertsBehavior.instance.displayAlignmentAlert(new Vector2(purpose, affinity));

        // Dialogue-driven change: evaluate ability unlocks and fire the unlock popup.
        AlignmentManager.reconcileUnlocks(true);
    }

    private void updatePlayerAlignment()
    {
        // for now, radius is 48
        currentAlignment = getAlignmentOfVec(alignmentArray);

        // update our lua variable so dialogue system can easily check for alignment
        DialogueLua.SetVariable("Alignment", getAlignmentString(currentAlignment));
    }

    public alignmentQuadrant getAlignmentOfVec(Vector2 arr)
    {
        if (arr.magnitude >= 48)
        {
            if (arr.x >= 0)
            {
                if (arr.y > 0)
                    return alignmentQuadrant.OrderSelf;
                else
                    return alignmentQuadrant.FreedomSelf;
            }
            else
            {
                if (arr.y > 0)
                    return alignmentQuadrant.OrderCommunity;
                else
                    return alignmentQuadrant.FreedomCommunity;
            }
        }

        return alignmentQuadrant.Center;
    }

    private string getAlignmentString(alignmentQuadrant q)
    {
        switch(q)
        {
            case alignmentQuadrant.Center:
                return "C";
            case alignmentQuadrant.OrderSelf:
                return "SO";
            case alignmentQuadrant.FreedomSelf:
                return "SF";
            case alignmentQuadrant.FreedomCommunity:
                return "CF";
            case alignmentQuadrant.OrderCommunity:
                return "CO";
            default:
                return "C";               
        }
    }

    public bool inCombatOrBleeding()
    {
        if (isInCombat || isBleeding)
            return true;

        return false;
    }
    public void closeHomestead()
    {
        isHomesteadOpen = false;
        HomesteadUIBehavior.instance.gameObject.SetActive(false);
        AudioMaster.instance.PlaySound("Menu Close");
        freezeChar(false);
    }

    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        /*if (shieldWallCollider.IsTouching(collision))
        {
            // special logic - determine if the player is actually in the trigger
            Bounds playerBounds = standingCollider.bounds;
            Bounds triggerBounds = collision.bounds;

            if (!triggerBounds.Intersects(playerBounds))
                return; // false intersection
        }*/

        // Indoor detection is centralized in Character (slide-safe + multi-trigger correct);
        // it calls back via onEnteredIndoors/onExitedIndoors below.
        if (collision.gameObject.CompareTag("SPS"))
        {
            collision.gameObject.GetComponentInParent<SPSBehavior2>().detectPlayer();
        }

        base.OnTriggerEnter2D(collision);
    }

    protected override void OnTriggerExit2D(Collider2D collision)
    {
        // determine if the player is still in the trigger to make sure other colliders turning off (like shields) do not triggering this
        /*Bounds playerBounds = standingCollider.bounds;
        Bounds triggerBounds = collision.bounds;

        if (triggerBounds.Intersects(playerBounds))
            return; // false exit*/

        if (!isSliding)
        {
            bool isPointInside = collision.OverlapPoint(transform.position);
            if (isPointInside) return; // false exit - prevent shutting off shields from triggering this
        }

        if (collision.gameObject.CompareTag("SPS") && !isSliding)
        {
            collision.gameObject.GetComponentInParent<SPSBehavior2>().undetectPlayer();
        }

        // Indoors handled by Character.OnTriggerExit2D (its own slide-safe OverlapPoint guard)
        // which calls onExitedIndoors below.
        base.OnTriggerExit2D(collision);
    }

    protected override void onEnteredIndoors()
    {
        EventManager.instance.enteredIndoors();
    }

    protected override void onExitedIndoors()
    {
        EventManager.instance.exitedIndoors();
    }

    private void genericErrorFeedback(Vector3 xLocation = default)
    {
        if (xLocation == default) xLocation = transform.position + new Vector3(0f, 2f, 0f);
        AudioMaster.instance.PlaySound("Kipling Error");
        WorldspaceCanvas.instance.popUpXEmote(xLocation);
    }

    private void initializeSkillTree()
    {
        skillTree = new playerSkill[3,4];

        skillTree[0, 0] = new playerSkill(false, 0, 0, new List<bool> { false, false, false, false }); // Armory
        skillTree[0, 1] = new playerSkill(false, 0, 0, new List<bool> { false, false, false, false }); // Explosives Range
        skillTree[0, 2] = new playerSkill(false, 0, 0, new List<bool> { false, false, false, false }); // Tinkering Bench
        skillTree[0, 3] = new playerSkill(false, 0, 0, new List<bool> { false, false, false, false }); // Campsite

        skillTree[1, 0] = new playerSkill(false, 0, 0, new List<bool> { false, false, false, false, false }); // Computer Lab
        skillTree[1, 1] = new playerSkill(false, 0, 0, new List<bool> { false, false, false, false, false }); // Firing Range
        skillTree[1, 2] = new playerSkill(false, 0, 0, new List<bool> { false, false, false, false, false }); // Medical Tent
        skillTree[1, 3] = new playerSkill(false, 0, 0, new List<bool> { false, false, false, false, false }); // Ammo Forge

        skillTree[2, 0] = new playerSkill(false, 0, 0, new List<bool> { false, false, false }); // Auto-Cultivator
        skillTree[2, 1] = new playerSkill(false, 0, 0, new List<bool> { false, false, false }); // Experimental Facility
        skillTree[2, 2] = new playerSkill(false, 0, 0, new List<bool> { false, false, false }); // Gun Research Lab
        skillTree[2, 3] = new playerSkill(false, 0, 0, new List<bool> { false, false, false }); // Biotech Workshop
    }

    public void addPlayerSkill(int xKey, int yKey, int iPlot, int jPlot)
    {
        skillTree[xKey, yKey].isPurchased = true;
        skillTree[xKey, yKey].iPlotLoc = iPlot;
        skillTree[xKey, yKey].jPlotLoc = jPlot;
    }

    public void addPlayerUpgrade(int xKey, int yKey, int skillIndex)
    {
        skillTree[xKey, yKey].upgrades[skillIndex] = true;
    }

    public bool hasOverloadSkill()
    {
        return hasPlayerSkill(0, 0);
    }

    public bool hasTurretSkill()
    {
        return hasPlayerSkill(1, 0);
    }

    private bool hasPlayerSkill(int xKey, int yKey)
    {
        return skillTree[xKey, yKey].isPurchased;
    }

    // Class definitions

    public class playerSkill
    {
        public bool isPurchased;
        public int iPlotLoc;
        public int jPlotLoc;
        public List<bool> upgrades;

        public playerSkill(bool hasSkill, int iPlotLocation, int jPlotLocation, List<bool> upgaradeList)
        {
            isPurchased = hasSkill;
            iPlotLoc = iPlotLocation;
            jPlotLoc = jPlotLocation;
            upgrades = upgaradeList;
        }
    }



    // Editor-only visualization of the noise radius.
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, noiseRadius);
    }

}

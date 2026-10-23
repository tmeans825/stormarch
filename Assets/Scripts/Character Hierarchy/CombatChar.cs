using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Experimental.Rendering.Universal;
using TMPro;

/** 
    DESCRIPTION: Parent Class for Humanoid/Animal Combat Characters
    PRIMARY FUNCTIONALITY: Shields, Health, Dying, and Melee
**/

public class CombatChar : Character
{
    [Header("Combat: Health & Damage")]
    public float health = 300.0f;
    public float maxHealth = 300.0f;
    public float bleedAmount = 3.0f;
    public float bleedCooldown = 3.0f;
    public GameObject bloodPing;
    public GameObject blood;
    public GameObject healthPopup;
    public GameObject critPopup;
    public GameObject medPopup;

    [Header("Combat: Items")]
    public GameObject grenade;
    public GameObject firebomb;
    public float huckSpeedMultiplier = 1.0f;
    public float maxThrowMag = 10.0f;

    [Header("Character Type")]
    public bool isPlayerChar = false;

    [Header("Respawn")]
    [Tooltip("If true, this character stays dead permanently. Camp/death respawn skips it. For bosses and story-scripted enemies.")]
    public bool noRespawn = false;

    [System.NonSerialized]
    public bool damagePassedShields = false;
    [System.NonSerialized]
    public bool isBleeding = false;

    // Original spawn anchor - captured first time this object is alive in a scene
    // and persisted across saves. respawnFromDeath() warps the character back here.
    [System.NonSerialized]
    [ES3Serializable]
    public Vector3 originalSpawnPosition;
    [System.NonSerialized]
    [ES3Serializable]
    public int originalDirection = -1;
    [System.NonSerialized]
    [ES3Serializable]
    public bool spawnAnchorRecorded = false;

    // Cached prefab baseline of slideSlowdownModifier so respawnFromDeath can restore
    // it after CombatChar.triggerDeath overwrites with the death-bounce value (0.985f).
    // Captured in Awake before any death code path can run.
    private float aliveSlideSlowdownModifier;
    private Vector3 startupSpawnPosition;
    private int startupDirection = -1;
    private bool startupAnchorCaptured = false;

    protected ParticleSystem dustEmitter;
    protected ShadowCaster2D shadowCaster;
    protected bool shieldsJustBroke = false;
    protected bool isStunned;

    private string[] directionTriggersDeath;
    protected int activeHealthModule;
    protected float[] healthThresholds;
    private float nextBleed = 0.0f;
    private GameObject chest;
    private GameObject head;
    private GameObject legs;
    private Vector3 originalScale;
    private bool isFlickering = false;
    private float fireTimer = 0.0f;
    private float fireDamageInterval = 0.5f;
    private float timeStunned = 0.0f;
    protected bool invulnerable = false;

    protected override void Awake()
    {
        base.Awake();
        // Capture the prefab/inspector value before any code path (including CombatChar.triggerDeath)
        // can mutate it. respawnFromDeath restores from this cache.
        aliveSlideSlowdownModifier = slideSlowdownModifier;
    }

    // Start is called before the first frame update
    protected override void Start()
    {
        chest = transform.GetChild(0).gameObject;
        head = transform.GetChild(1).gameObject;
        legs = transform.GetChild(2).gameObject;
        originalScale = chest.transform.localScale;

        float healthPerOrb = maxHealth / 3.0f;
        healthThresholds = new float[] { healthPerOrb, healthPerOrb * 2.0f, healthPerOrb * 3.0f, healthPerOrb * 4.0f, healthPerOrb * 5.0f };
        //directionTriggersDeath = new string[] { "", "PlayerDeathRight", "", "PlayerDeathLeft" };
        directionTriggersDeath = new string[] { "", "Chest_Death_Right", "", "Chest_Death_Left" };
        dustEmitter = transform.GetChild(5).GetComponent<ParticleSystem>();

        if (health > maxHealth)
            health = maxHealth;
        int mod = -1;
        if ((health % healthPerOrb) > 0)
            mod = 0;    
        activeHealthModule = (int)(health / healthPerOrb) + mod;

        base.Start();

        shadowCaster = GetComponent<ShadowCaster2D>();

        captureStartupAnchor();
        captureSpawnAnchorIfMissing();
    }

    // Captures the canonical spawn position+facing exactly once per save slot.
    // Called from Start after base initialization so subclass pre-positioning
    // (e.g. Enemy's idleType==1 paceTargets[0] warp) has already happened.
    // Also called defensively after ES3 load because Start runs before
    // SaveManager.loadSceneData fires ES3 deserialization, so a tolerant capture
    // is needed to handle the case where ES3 didn't restore an anchor (fresh
    // save / new field added). The spawnAnchorRecorded flag prevents drift on
    // every scene load. Once locked, ES3 is the source of truth.
    protected void captureSpawnAnchorIfMissing()
    {
        if (spawnAnchorRecorded)
            return;
        if (startupAnchorCaptured)
        {
            originalSpawnPosition = startupSpawnPosition;
            originalDirection = startupDirection;
            spawnAnchorRecorded = true;
            return;
        }
        originalSpawnPosition = transform.position;
        originalDirection = startingDirection;
        spawnAnchorRecorded = true;
    }

    private void captureStartupAnchor()
    {
        startupSpawnPosition = transform.position;
        startupDirection = startingDirection;
        startupAnchorCaptured = true;
    }

    protected void loadedHealth()
    {
        float healthPerOrb = maxHealth / 3.0f;
        if (health > maxHealth)
            health = maxHealth;
        int mod = -1;
        if ((health % healthPerOrb) > 0)
            mod = 0;
        activeHealthModule = (int)(health / healthPerOrb) + mod;
    }

    protected override void Update()
    {
        if (!isDead)
        {
            if (isBleeding && Time.time > nextBleed)
                damageCharacter(bleedAmount, transform.position, true, false, true, false);
        }

        base.Update();
    }

    public virtual void stun(float length, GameObject stunParticles, bool overloadAfter = false)
    {
        if (isStunned)
            timeStunned = 0.0f; // refresh stun
        else
        {
            rb.velocity = Vector2.zero;
            stopMoving(true);
            StartCoroutine(stunForSeconds(length, overloadAfter));
            Instantiate(stunParticles, transform.position, Quaternion.identity, transform);
        }
    }

    private IEnumerator stunForSeconds(float length, bool overloadAfter = false)
    {
        timeStunned = 0.0f;
        //audioManager.PlaySound("Stunned"); moved to audio source on shock spanwer
        isStunned = true;  
        Vector3 originalPos = transform.position;
        while(timeStunned < length)
        {
            Vector3 modifiedVec = new Vector3(Random.Range(-0.04f, 0.04f), Random.Range(-0.04f, 0.04f));
            transform.position = originalPos + modifiedVec;
            yield return null;
            timeStunned += Time.deltaTime;
        }      
        isStunned = false;
        audioManager.StopSound("Stunned");

        if (overloadAfter)
            PlayerController.instance.initiateOverload(1, transform.position);
    }

    protected override bool trySlide()
    {
        if (base.trySlide())
        {
            dustEmitter.Play();
            return true;
        }

        return false;
    }

    public override void stopSliding()
    {
        base.stopSliding();
    }

    public virtual void damageCharacter(float damage, Vector2 d, bool fromBleeding, bool ignoreShields, bool generateBlood, bool crit)
    {
        if (invulnerable || damage == 0 || isDead)
            return;

        StartCoroutine(invulnerableFrame());

        
        damagePassedShields = true;

        damageContext dmgContext = new damageContext();
        dmgContext.damageAmount = damage;
        if (isPlayerChar)
            GameEvents.current.playerHurt(dmgContext);
        damage = dmgContext.damageAmount;

        int bloodCount = (int)(damage / 10f);
        if (bloodCount == 0)
            bloodCount++;
        health -= damage;

        if (generateBlood)
        {
            for (int i = 0; i < bloodCount; i++)
            {
                Vector2 origination = d;
                if (!isHuman) // for sentinels, sparks come from middle
                    origination = transform.position;
                Instantiate(blood, origination, Quaternion.identity, ObjectPooler.instance.transform);
            }
        }

        int direction;
        if (d.x > transform.position.x)
            direction = 1;
        else
            direction = 3;

        if (!fromBleeding)
        {
            audioManager.PlaySound("Health_Impact", true);
            Instantiate(bloodPing, d, Quaternion.identity, this.transform);
            isBleeding = true;
            signalBleed(isBleeding);
        }
        else
        {
            audioManager.PlaySound("Blood_Impact", true);
        }

        if (health <= 0)
        {
            if (fromBleeding)
            {
                isBleeding = false;
            }

            triggerDeath(direction);
        }
        else if (activeHealthModule > 0 && !isDead)
        {
            if (health <= healthThresholds[activeHealthModule - 1])
            {
                if (fromBleeding)
                {
                    health = healthThresholds[activeHealthModule - 1];
                    isBleeding = false;
                    signalBleed(isBleeding);
                }
                activeHealthModule--;
            }
        }

        nextBleed = Time.time + bleedCooldown;

        if (!isFlickering)
            StartCoroutine(flashFeedback());

        if (!shieldsJustBroke)
        {
            GameObject popup = healthPopup;
            if (crit)
                popup = critPopup;

            createDmgPopup(popup, d, damage);
        }
    }

    private IEnumerator invulnerableFrame()
    {
        invulnerable = true;
        yield return null;
        invulnerable = false;
    }

    protected virtual void signalBleed(bool isBleeding) // used to signal to inheritable members, for override only
    {
        return;
    }

    protected void createDmgPopup(GameObject prefab, Vector2 location, float dmg)
    {
        GameObject popup = Instantiate(prefab, location, Quaternion.identity);
        TextMeshPro text = popup.GetComponent<TextMeshPro>();
        text.text = ((int)dmg).ToString();
    }

    protected virtual void triggerDeath(int d)
    {
        desaturateEntireBody();
        isDead = true;
        chestAnimator.SetBool("IsDead", true);
        this.GetComponent<Collider2D>().enabled = false;
        slideSlowdownModifier = 0.985f;
        signalDirectionChange(d);

        catchAnimationIssues();

        //triggerAnim(d, directionTriggersDeath); // animation event code will process remainder of death
        setAnim(d, directionTriggersDeath);

        if (d == 1)
            headAnimator.SetTrigger("DeathRight");
        else
        {
            headAnimator.SetTrigger("DeathLeft");
            deathDirectionModifier = -1;
        }

        shadowCaster.enabled = false;
        audioManager.PlaySound("DeathThud");

        if (GetComponent<spyDeathTrigger>() != null)
            GetComponent<spyDeathTrigger>().signalDeathStart();
    }

    // Reverses the dead-state mutations applied by triggerDeath / Character.characterDeath
    // so the character can be re-presented alive in-place (no scene reload). Mirrors the
    // relevant subset of PlayerController.prepareForRespawnSave but for non-player CombatChars.
    // Subclasses chain to base then reset their own type-specific death state.
    public virtual void respawnFromDeath()
    {
        StopAllCoroutines();

        isDead = false;
        isBleeding = false;
        signalBleed(isBleeding);
        damagePassedShields = false;
        invulnerable = false;
        isStunned = false;

        health = maxHealth;
        loadedHealth();

        // Reverse triggerDeath's death-bounce override (0.985f). Without restoring
        // the prefab baseline, the character's slide deceleration stays broken for
        // every subsequent slide / FixedUpdate ragdoll fall.
        slideSlowdownModifier = aliveSlideSlowdownModifier;

        // Re-enable colliders disabled by Character.characterDeath + CombatChar.triggerDeath.
        var mainCollider = GetComponent<BoxCollider2D>();
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

        catchAnimationIssues();
        saturateEntireBody();

        // Warp back to the spawn anchor and re-apply the original facing's idle pose.
        // chestIdleStates indexes by direction (0-3); guard against startingDirection
        // being -1 (unrandomized) by falling back to the post-Start direction.
        if (spawnAnchorRecorded)
            transform.position = originalSpawnPosition;
        int facing = (originalDirection >= 0 && originalDirection < 4) ? originalDirection : startingDirection;
        if (facing >= 0 && facing < 4)
        {
            setAnim(facing, chestIdleStates);
            directionCurrentlyFacing = facing;
        }
    }

    // Resets respawnable CombatChars in the loaded scene back to their spawn anchor:
    //   - dead respawnable       -> activate (if inactive) + respawnFromDeath()
    //   - living + active         -> respawnFromDeath() (teleport back, clear aggro/path)
    //   - living + inactive       -> skip; story-hidden objects must stay hidden
    //   - isPlayerChar/noRespawn  -> skip
    public static void respawnAllRespawnableInCurrentScene()
    {
        captureMissingSpawnAnchorsInCurrentScene();
        var all = GameObject.FindObjectsOfType<CombatChar>(true);
        for (int i = 0; i < all.Length; i++)
        {
            CombatChar c = all[i];
            if (c == null) continue;
            if (c.isPlayerChar) continue;
            if (c.noRespawn) continue;

            bool deadToRevive = c.isDead;
            bool aliveAndActive = !c.isDead && c.gameObject.activeSelf;
            if (!deadToRevive && !aliveAndActive) continue;

            if (deadToRevive && !c.gameObject.activeSelf)
                c.gameObject.SetActive(true);
            c.respawnFromDeath();
        }
    }

    public static void captureMissingSpawnAnchorsInCurrentScene()
    {
        var all = GameObject.FindObjectsOfType<CombatChar>(true);
        for (int i = 0; i < all.Length; i++)
        {
            CombatChar c = all[i];
            if (c == null) continue;
            if (c.isPlayerChar) continue;
            c.captureSpawnAnchorIfMissing();
        }
    }

    private IEnumerator flashFeedback()
    {
        isFlickering = true;
        reScaleBody(Vector3.zero);
        yield return new WaitForSeconds(0.08f);
        reScaleBody(originalScale);
        isFlickering = false;
    }

    protected virtual void reScaleBody(Vector3 scale)
    {
        chest.transform.localScale = scale;
        legs.transform.localScale = scale;
        head.transform.localScale = scale;
    }

    public void huckaNade(Vector2 target, int itemIndex, float radius, float dmg)
    {
        GameObject huckedNade;
        GameObject prefab;
        if (itemIndex == 0)
        {
            prefab = grenade;
        }
        else
        {
            prefab = firebomb;
        }

        huckedNade = Instantiate(prefab, transform.position, transform.rotation);
        huckedNade.GetComponent<GrenadeExplode>().initializeGrenade(radius, dmg, GetComponent<BoxCollider2D>());

        Vector2 force = Vector2.ClampMagnitude(target * huckSpeedMultiplier, maxThrowMag);

        huckedNade.GetComponent<Rigidbody2D>().AddForce(force);

        audioManager.PlaySound("Throw");
    }

    public virtual void heal(float healAmt, int index)
    {
        heal(healAmt, index, true);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="healAmt"></param>
    /// <param name="index"></param>
    /// <param name="canIncreaseModule">Allow increasing health orb</param>
    public virtual void heal(float healAmt, int index, bool canIncreaseModule)
    {
        if (isBleeding)
        {
            isBleeding = false;
            signalBleed(isBleeding);
        }

        health += healAmt;

        if(!canIncreaseModule && health > healthThresholds[activeHealthModule])
        {
            healAmt -= health - healthThresholds[activeHealthModule];
            health = healthThresholds[activeHealthModule];
        }

        if (health > maxHealth)
        {
            healAmt -= health - maxHealth;
            health = maxHealth;
        }

        string popText = "+";
        if (healAmt > 0)
            popText = popText + ((int)healAmt).ToString();

        GameObject popup = Instantiate(medPopup, transform.position, Quaternion.identity);
        TextMeshPro text = popup.GetComponent<TextMeshPro>();
        text.text = popText;

        if (health > healthThresholds[activeHealthModule])
        {
            //health = healthThresholds[activeHealthModule + 1];
            activeHealthModule++;
        }
    }

    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Fire")
        {
            fireTimer = fireDamageInterval;
        }
        base.OnTriggerEnter2D(collision);
    }

    protected virtual void OnTriggerStay2D(Collider2D collision)
    {       
        if (collision.gameObject.tag == "Fire")
        {
            if (fireTimer >= fireDamageInterval)
            {
                fireTimer = 0f;
                damageContext ctx = new damageContext();
                ctx.damageAmount = collision.transform.GetComponentInParent<FirebombExplode>().damage;
                GameEvents.current.fireDamageTaken(ctx);
                damageCharacter(ctx.damageAmount, transform.position, true, false, false, false);
            }
            fireTimer += Time.deltaTime;
        }
    }

    public float getNextBleed()
    {
        return nextBleed;
    }
}

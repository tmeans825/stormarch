using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Data;

/** 
    DESCRIPTION: Parent Class for Player and Enemy Characters
    PRIMARY FUNCTIONALITY: Guns, Shooting, and Items
**/

public class Humanoid : CombatChar
{
    [Header("Humanoid: Gun Traits")]
    public float shotgunSpread;
    public int shotgunBulletCount;
    /*public float[] gunCooldownTimes;
    public float[] gunBulletSpeeds;
    public float[] gunBulletLifetime;*/
    public float[] damage;
    public float critChance = 0.1f;
    public float electroAOE = 10f;
    public float[] shockBatonDamage;
    public float shockBatonBulletLife;
    public float shockBatonStunLength;
    public float shockBatonRate = 1.0f;
    public float batonShieldCost = 25.0f;
    public float conductorMultiplier = 1.5f;
    protected int conductorCharges = 0;


    [Header("Humanoid: Health & Damage")]
    public float shields = 3f;
    public float maxShields = 3f;
    public float shieldRechargeCooldown = 1.0f;
    public float shieldRechargeSpeed = 0.01f;
    public GameObject shieldPopup;
    public float shieldDrainAmount = 0.1f;
    public float shieldDrainRate = 0.1f;

    [Header("Humanoid: GameObject References")]
    public GameObject shieldPing;
    public GameObject bullet;
    public GameObject lancerBullet;
    public GameObject electroBullet;
    public GameObject shockBullet;
    public GameObject modifiedShockBullet;
    public GameObject emptyTradBullet;
    

    [Header("Humanoid: Spritesheets")]
    public string gunSpriteSheetName;

    [System.NonSerialized]
    [ES3NonSerializable]
    public float nextFire = 0f;

    //protected int equippedFirearm;
    protected int bulletCount = 1;
    protected float bulletLifetime;
    protected float bulletSpeed;
    protected GameObject gun;
    protected string[][] gunSFX = new string[4][];
    protected readonly string[] tradSFX = { "Trad_Rifle_SFX", "Trad_Pistol_SFX", "Trad_Shotgun_SFX" };
    protected readonly string[] lancerSFX = { "Lancer_Rifle_SFX", "Lancer_Pistol_SFX", "Lancer_Rifle_SFX" };
    protected readonly string[] electroSFX = { "Electro_Rifle_SFX", "Electro_Rifle_SFX", "Electro_Shotgun_SFX" };
    protected readonly string[] shockSFX = { "", "Electro Shock", "" }; 
    protected string equippedGunSFX;
    protected Vector2[] directionLocationsTip;
    protected Vector2[] directionLocationsGun;
    protected GameObject standShadow;
    protected GameObject layShadow;
    protected float ax = -0.5f;
    protected float ay = -0.3f;
    protected float bx = 0.15f;
    protected float by = 0.2f;
    protected BoxCollider2D standingCollider;
    protected PolygonCollider2D slidingCollider;
    protected Transform gunTip;
    protected bool lancerCharging = false;
    protected LightBehavior muzzleFlashLight;
    protected bool drainingShieldsForAbility = false;

    protected int lastLockedDirection;
    protected string[] directionTriggersGun;
    protected string[] directionTriggersShooting;
    protected string[] directionIdleGun;
    
    private string loadedGunSpriteSheetName;
    private Dictionary<string, Sprite> gunSpriteSheet;
    private SpriteRenderer gunSpriteRenderer;
    protected Animator gunAnimator;   
    private int lastShotDirection;    
    private SpriteRenderer gunRenderer;
    protected bool isRecoiling = false;   
    private float nextRecharge = 0.0f;
    protected bool isRecharging = false;
    protected Animator shieldAnimator;
    protected GameObject deployedAnimationObj;
    private string[] chestRunningStates;

    private float nextDrain = 0f;

    private laserTargetBehavior laserBehavior;

    protected bool overrideGunTipLogic = false;

    

    protected override void Awake()
    {
        chestRunningStates = new string[] { "Chest_Run_Up", "Chest_Run_Right", "Chest_Run_Down", "Chest_Run_Left" };
        base.Awake();
        gunSFX[0] = tradSFX;
        gunSFX[1] = lancerSFX;
        gunSFX[2] = electroSFX;
        gunSFX[3] = shockSFX;
    }
    // Start is called before the first frame update
    protected override void Start()
    {
        standShadow = transform.GetChild(4).gameObject;

        base.Start();

        gun = transform.GetChild(3).gameObject;

        gunAnimator = gun.GetComponent<Animator>();

        shieldAnimator = transform.GetChild(7).GetComponent<Animator>();
        deployedAnimationObj = transform.GetChild(7).GetChild(0).gameObject;

        this.gunSpriteRenderer = transform.GetChild(3).GetComponent<SpriteRenderer>();

        this.LoadGunSpriteSheet();

        //equippedFirearm = 0;
        nextFire = 0.0f;

        directionTriggersShooting = new string[] { "PlayerShootUp", "PlayerShootRight", "PlayerShootDown", "PlayerShootLeft" };

        directionTriggersGun = new string[] { "GunShootUp", "GunShootRight", "GunShootDown", "GunShootLeft" };

        directionIdleGun = new string[] { "Gun_Idle_Up", "Gun_Idle_Right", "Gun_Idle_Down", "Gun_Idle_Left" };

        lastLockedDirection = 4;
        lastShotDirection = 3;
        

        directionLocationsGun = new Vector2[] { new Vector2(0f, -0.25f), new Vector2(0.9f, -0.15f), new Vector2(0.15f, -0.6f), new Vector2(-0.9f, -0.15f) };
        directionLocationsTip = new Vector2[] { new Vector2(0f, 0.06f), new Vector2(0.05f, 0.03f), new Vector2(-0.005f, -0.06f), new Vector2(-0.05f, 0.03f) };

        gunRenderer = gun.GetComponent<SpriteRenderer>();

        gunTip = gun.transform.GetChild(0).GetComponent<Transform>();
        
        layShadow = transform.GetChild(6).gameObject;

        standingCollider = this.GetComponent<BoxCollider2D>();
        slidingCollider = this.GetComponent<PolygonCollider2D>();

        if (isHuman)
        {
            gun.SetActive(false);
            muzzleFlashLight = gunTip.GetChild(0).transform.GetComponent<LightBehavior>();
        }

        laserBehavior = gunTip.GetComponent<laserTargetBehavior>();

        updateGunPosition(lastShotDirection);
    }

    protected override void Update()
    {
        if (!isDead)
        {
            if (shieldsDeployed && Time.time > nextDrain && shields > 0)
            {
                drainingShieldsForAbility = true;
                damageCharacter(shieldDrainAmount, Vector2.zero, false, false, false, false);

                nextDrain = Time.time + shieldDrainRate;
            }
            else
            {
                if (shields < maxShields && !isRecharging && Time.time >= nextRecharge)
                {
                    if (isPlayerChar)
                        GameEvents.current.rechargeStart();

                    isRecharging = true;
                    shieldAnimator.SetTrigger("recharge");
                    audioManager.PlaySound("Shield_Recharge");
                }
                if (isRecharging)
                {
                    rechargeShields(shieldRechargeSpeed * Time.deltaTime);
                }
            }

            base.Update();
            humanoidAnimationHandling();
        }
    }

    public void rechargeShields(float amount)
    {
        shields += amount;
        if (shields >= maxShields)
        {
            shields = maxShields;
            isRecharging = false;
        }
    }

    private void humanoidAnimationHandling()
    {
        /* Try to handle via lateUpdate instead
        if (this.loadedGunSpriteSheetName != this.gunSpriteSheetName)
        {
            this.LoadGunSpriteSheet();
        }

        this.gunSpriteRenderer.sprite = this.gunSpriteSheet[this.gunSpriteRenderer.sprite.name];*/

        if (!isMoving)
        {
            foreach (string st in chestRunningStates) // for infinite running bug
            {
                if (legsAnimator.GetCurrentAnimatorStateInfo(0).IsName(st))
                {
                    legsAnimator.SetTrigger("PlayerStop");
                    break;
                }
            }
        }
    }

    public override void stun(float length, GameObject particles, bool overloadAfter = false)
    {
        //if (shields <= 0) // only stun if we break shields
        base.stun(length, particles, overloadAfter);

        if (shields > 0)
        {
            drainingShieldsForAbility = true;
            damageCharacter(shields, transform.position, false, false, false, false);
        }
    }

    public override void damageCharacter(float damage, Vector2 d, bool fromBleeding, bool ignoreShields, bool generateBlood, bool crit)
    {
        shieldsJustBroke = false;

        if (!fromBleeding && !ignoreShields)
        {
            tryHitShields(ref damage, d);
        }

        base.damageCharacter(damage, d, fromBleeding, ignoreShields, generateBlood, crit);        

        if(!fromBleeding)
            nextRecharge = Time.time + shieldRechargeCooldown; // delay recharge each hit
    }

    private void tryHitShields(ref float damage, Vector2 d)
    {
        isRecharging = false;

        if(!drainingShieldsForAbility)
            shields = Mathf.Floor(shields); // all recharging ceases

        if (shields >= 1f || drainingShieldsForAbility)
        {
            if (!drainingShieldsForAbility) // taking a hit - subtract 1
            {              
                audioManager.PlaySound("Shield_Impact", true);
                shields--;
            }
            else
            {
                shields -= damage;
            }

            //GameObject popup = shieldPopup;
            //if (crit)
            // popup = critPopup;

            if (areShieldsBroken())
            {
                shieldsJustBroke = true;
                audioManager.PlaySound("Shield_Break");
                shieldAnimator.SetTrigger("break");
                shields = 0;
                GameEvents.current.shieldsBroken();
                undeployShields(); // if shield wall is up, turn it off
            }

            // Handle FX if appropriate
            if (!drainingShieldsForAbility)
                Instantiate(shieldPing, d, Quaternion.identity, this.transform);
            else
                drainingShieldsForAbility = false;

            damage = 0; // if shields hit, do not pass on damage
        }
    }

    private bool areShieldsBroken()
    {
        if (drainingShieldsForAbility)
            return shields <= 0;

        return shields < 1; 
    }

    public void shoot(Vector2 aimTarget, int ammoType)
    {
        shoot(aimTarget, ammoType, 0.0f);
    }

    protected void shoot(Vector2 aimTarget, int ammoType, float addCrit)
    {
        shoot(aimTarget, ammoType, addCrit, false);
    }

    protected void shoot(Vector2 aimTarget, int ammoType, float addCrit, bool isCharging)
    {
        shoot(aimTarget, ammoType, addCrit, isCharging, false);
    }

    protected void shoot(Vector2 aimTarget, int ammoType, float addCrit, bool isCharging, bool ignoreConductor)
    {
        shoot(aimTarget, ammoType, addCrit, isCharging, ignoreConductor, false);
    }

    protected void shoot(Vector2 aimTarget, int ammoType, float addCrit, bool isCharging, bool ignoreConductor, bool isTrueAim)
    {
        if (!isDead && !shieldsDeployed)
        {
            lockAnim(TEMLib.getRelativePos(transform.position, aimTarget).normalized);

            if (!isCharging)
            {
                stopLancerCharging();

                fireGun(lockedDirection, directionTriggersGun);

                Vector2 aimDirection = TEMLib.getRelativePos(gunTip.position, aimTarget); // make sure it comes out of gun tip

                if (bulletCount == 1)
                    fireBullet(gunTip.position, aimDirection.normalized, ammoType, addCrit, ignoreConductor, isTrueAim);
                else
                {
                    if (isPlayerChar)
                    {
                        StartCoroutine(playerShotgunBlast(bulletCount, aimDirection, ammoType, addCrit, ignoreConductor, isTrueAim)); // shotgun bullets are more chaotic for player
                    }
                    else
                    {
                        for (int i = 0; i < bulletCount; i++)
                        {
                            fireBullet(gunTip.position, rotateVector(aimDirection, Mathf.Deg2Rad * (shotgunSpread * (-0.5f) + (i * (shotgunSpread / (bulletCount))))).normalized, ammoType, addCrit, ignoreConductor, isTrueAim);
                        }
                    }
                }
            }
            else
            {
                showGun(lockedDirection, directionTriggersGun);
                gunAnimator.Play(directionIdleGun[lockedDirection]);

                // to do - show a laser ray from gun to target, hitting colliders
                if (laserBehavior != null)
                {
                    laserBehavior.enabled = true;
                    laserBehavior.direction = TEMLib.getRelativePos(gunTip.position, aimTarget);
                }
            }

            if (isMoving && headAnimator.GetCurrentAnimatorStateInfo(0).IsName("Head_Bob"))
                headAnimator.SetTrigger("StopRunning");

            triggerAnim(lockedDirection, directionTriggersShooting);
            isRecoiling = true;
        }
    }

    protected void stopLancerCharging()
    {
        if (laserBehavior != null)
        {
            laserBehavior.enabled = false;
        }
    }

    private IEnumerator playerShotgunBlast(int bulletCount, Vector2 aimTarget, int ammoType, float addCrit, bool ignoreConductor, bool isTrueAim)
    {
        int count = Mathf.Max(1, bulletCount);
        float dt = 0.025f / count;

        for (int i = 0; i < count; i++)
        {
            fireBullet(gunTip.position, rotateVector(aimTarget, Mathf.Deg2Rad * (shotgunSpread * Random.Range(-1f, 1f))).normalized, ammoType, addCrit, ignoreConductor, isTrueAim);
            yield return new WaitForSeconds(dt); 
        }
    }

    // true if shields deployed, false if shields out
    protected bool deployShields(Vector2 d)
    {
        if (!isDead && shields > 0)
        {
            shieldsDeployed = true; // Also further slowdown? // need to: move head
            deployedAnimationObj.SetActive(true);
            if (isHuman)
                gun.SetActive(false);
            lockAnim(d);
            audioManager.PlaySound("ShieldStatic");
            audioManager.PlaySound("ShieldsOn");
            return true;
        }

        return false;
    }


    protected void undeployShields()
    {
        if (!shieldsDeployed) // nothing to undeploy
            return;

        animUnlock();
        shieldsDeployed = false; // also undo further slowdown?
        deployedAnimationObj.SetActive(false);

        if(shields > 0)
            audioManager.PlaySound("ShieldsOff");

        audioManager.StopSound("ShieldStatic");
    }

    public void lockAnim(Vector2 d)
    {
        lockedDirection = getTargetDirection(d); // get mouse relative position

        if (lastLockedDirection != lockedDirection) // if not moving & direction has CHANGED
        {
            signalDirectionChange(lockedDirection);
            if (direction == 4)
            {
                legsAnimator.SetTrigger(directionTriggersRunning[lockedDirection]);
                legsAnimator.SetTrigger("PlayerStop");

                if(shieldsDeployed)
                {
                    chestAnimator.SetTrigger(directionTriggersRunning[lockedDirection]);
                    chestAnimator.SetTrigger("PlayerStop");
                }

                lastLockedDirection = lockedDirection;
            }
        }

        animLocked = true; // character is now 'locked' in the direction they are firing in
        legsAnimator.speed = slowedLegSpeed;

        if (shieldsDeployed)
        {
            if (isMoving && headAnimator.GetCurrentAnimatorStateInfo(0).IsName("Head_Bob"))
                headAnimator.SetTrigger("StopRunning");

            swapHeads(lockedDirection);
            slowAnimAndSpeed(0.5f);
        }
    }

    public void tryAnimUnlockAfterGun()
    {
        if (!shieldsDeployed)
            animUnlock();
        else
        {
            if (isHuman)
                gun.SetActive(false);
        }
    }    

    public void animUnlock()
    {
        if (isHuman)
            gun.SetActive(false);
        animLocked = false;
        if (isMoving)
        {
            transitionOut = true;
        }
        legsAnimator.speed = originalLegSpeed;
        if (shieldsDeployed)
        {
            resumeOriginalAnimAndSpeed();
        }
        lastLockedDirection = 4;
    }


    protected override bool trySlide()
    {
        if ((!isRecoiling || PlayerProgressBarController.instance.isReloading) && !isDead && !shieldsDeployed) // currently, this only lets player char slide
        {
            if (animLocked)
            {
                animUnlock();
            }

            if(base.trySlide())
            {
                standShadow.SetActive(false);
                layShadow.SetActive(true);
                standingCollider.enabled = false;
                //slidingCollider.enabled = true;
                GameEvents.current.slideStart();
                return true;
            }

            return false;
        }
        else
            return false;
    }

    public virtual void characterDeathBounce()
    {
        if (!isHuman) // sentinels need different shadow shift
        {
            ax = -0.1f;
            bx = 0.25f;
            by = 0.15f;
        }

        this.transform.Translate(new Vector3(ax * deathDirectionModifier, ay, 0f));
        layShadow.transform.Translate(new Vector3(bx * deathDirectionModifier, by, 0f));

        standShadow.SetActive(false);
        layShadow.SetActive(true);
        //Destroy(slidingCollider);
    }

    public override void prepGettingUp()
    {
        standingCollider.enabled = true;
        base.prepGettingUp();
    }

    public override void stopSliding()
    {
        base.stopSliding();
        layShadow.SetActive(false);
        standShadow.SetActive(true);

        //slidingCollider.enabled = false;
    }

    protected static Vector2 rotateVector(Vector2 v, float delta)
    {
        return new Vector2(
            v.x * Mathf.Cos(delta) - v.y * Mathf.Sin(delta),
            v.x * Mathf.Sin(delta) + v.y * Mathf.Cos(delta)
        );
    }

    private void fireGun(int d, string[] triggerArray)
    {
        showGun(d, triggerArray);

        gunAnimator.SetTrigger(triggerArray[d]); // FIRE! 

        if (muzzleFlashLight != null && isHuman)
            muzzleFlashLight.trigger();
    }

    protected void showGun(int d, string[] triggerArray)
    {
        if (!gun.activeSelf) // turn that gun on baby
            gun.SetActive(true);
        if (lastShotDirection != d) // move gun as necessary if it's flipping directions
        {
            if (d == 0)
                gunRenderer.sortingOrder = -2;
            else if (lastShotDirection == 0)
                gunRenderer.sortingOrder = 4;

            updateGunPosition(d);
            lastShotDirection = d;
        }        
    }

    private void updateGunPosition(int d)
    {
        if (!overrideGunTipLogic)
        {
            gun.transform.localPosition = directionLocationsGun[d];
            gunTip.transform.localPosition = directionLocationsTip[d];
        }
    }

    private void fireBullet(Vector3 pos, Vector2 direction, int ammoType, float addCrit)
    {
        fireBullet(pos, direction, ammoType, addCrit, false, false);
    }

    private void fireBullet(Vector3 pos, Vector2 direction, int ammoType, float addCrit, bool ignoreConductor, bool isTrueAim)
    {
        GameObject thisBullet;
        bool usingConductor = false;
        switch(ammoType)
        {
            case 0:
                thisBullet = bullet;
                break;
            case 1:
                thisBullet = lancerBullet;
                break;
            case 2:
                thisBullet = electroBullet;
                break;
            case 3:
                if (conductorCharges == 0 || ignoreConductor)
                    thisBullet = shockBullet;
                else
                {
                    thisBullet = modifiedShockBullet;
                    usingConductor = true;
                }
                break;
            case -1:
                thisBullet = emptyTradBullet;
                break;
            default:
                thisBullet = bullet;
                break;
        }

        bool crit = false;
        float modifier = 1.0f;
        if ((Random.Range(0.0f, 1.0f) <= critChance) || usingConductor)
        {
            modifier = 1.5f;
            crit = true;
        }

        float dmg = Random.Range(damage[0], damage[1]);

        Quaternion rot = Quaternion.identity;
        if (ammoType == 1)
        {
            switch (getTargetDirection(direction))
            {
                case 0:
                    rot = Quaternion.Euler(0f, 0f, 270f);
                    break;
                case 1:
                    rot = Quaternion.Euler(0f, 0f, 180f);
                    break;
                case 2:
                    rot = Quaternion.Euler(0f, 0f, 90f);
                    break;
                default:
                    break;
            }
        }

        GameObject currentBullet = (GameObject)Instantiate(thisBullet, pos, rot, ObjectPooler.instance.transform);
        ProjectileScript projectile = currentBullet.GetComponent<ProjectileScript>();
        projectile.bulletSpeed = bulletSpeed; // hacky workaround needed for destructible delay

        if(ammoType == 2)
        {
            projectile.initialize(bulletLifetime, dmg * modifier, crit, addCrit, electroAOE);
        }
        else if(ammoType == 3)
        {
            float tempStunModMultiplier = 1f;
            if (usingConductor)
                tempStunModMultiplier = conductorMultiplier;

            float tempStunLength = shockBatonStunLength * tempStunModMultiplier;
            projectile.initialize(bulletLifetime, dmg * modifier, tempStunLength, crit);
        }
        else if(ammoType == -1) // nerf
        {
            projectile.initialize(bulletLifetime / 2, (dmg * modifier) / 2, false, 0);
            currentBullet.GetComponent<Rigidbody2D>().velocity = new Vector2(direction.x * (bulletSpeed / 2), direction.y * (bulletSpeed / 2));

            if (isPlayerChar)
                GameEvents.current.bulletCreate(projectile);

            return;
        }
        else
        {
            projectile.initialize(bulletLifetime, dmg * modifier, crit, addCrit);
        }

        if (isTrueAim)
            projectile.conferTrueAim();
       
        currentBullet.GetComponent<Rigidbody2D>().velocity = new Vector2(direction.x * bulletSpeed, direction.y * bulletSpeed);

        if(isPlayerChar)
            GameEvents.current.bulletCreate(projectile);
    }

    protected override void overrideAllSprites()
    {
        base.overrideAllSprites();
        updateSpritesheet(ref loadedGunSpriteSheetName, gunSpriteSheetName, LoadGunSpriteSheet, gunSpriteRenderer, gunSpriteSheet);
    }

    private void LoadGunSpriteSheet()
    {
        var sprites = Resources.LoadAll<Sprite>("Guns/" + this.gunSpriteSheetName);
        this.gunSpriteSheet = sprites.ToDictionary(x => x.name, x => x);

        this.loadedGunSpriteSheetName = this.gunSpriteSheetName;
    }

    

    public void doneRecoiling()
    {
        isRecoiling = false;
    }

    protected override void triggerDeath(int d)
    {
        if (animLocked)
            animUnlock();
        base.triggerDeath(d);
    }

    public override void respawnFromDeath()
    {
        base.respawnFromDeath();

        shields = maxShields;
        isRecharging = false;
        shieldsJustBroke = false;

        // Death may have left the shield wall mid-deploy. Clean both halves so the
        // revived character isn't standing inside its own deployed shield visual.
        if (shieldsDeployed)
            undeployShields();

        if (animLocked)
            animUnlock();
    }

    protected void playGunSFX()
    {
        audioManager.PlaySound(equippedGunSFX, true);
    }
}

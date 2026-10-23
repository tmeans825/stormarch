using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Com.LuisPedroFonseca.ProCamera2D;
using Com.LuisPedroFonseca.ProCamera2D.TopDownShooter;
using UnityEngine.Experimental.Rendering.Universal;

/** 
    DESCRIPTION: Control projectile interactions
    MAJOR CHANGE HISTORY:
**/
public class ProjectileScript : MonoBehaviour
{
    public GameObject shieldDamage;
    public GameObject healthDamage;
    public GameObject criticalDamage;
    public GameObject bulletCollision;
    public GameObject bulletImpact;
    public GameObject lancerBurn;
    public GameObject electricity;
    public GameObject batonStunParticles;
    public bool collided = false;
    public float damage = 20.0f;
    public bool ignoreShields = false;
    public bool crit = false;
    public int bulletType = 0; // 1 - lancer, 2 - electro, 3 - shockbaton!
    public float critDmgAdd = 0.0f;
    public GameObject trueAimParticles;
    public GameObject debris;
    public GameObject bulletHole;
    public GameObject collisionDust;

    [System.NonSerialized]
    public float bulletSpeed = 40f;

    public bool inert = false;

    private bool isFriendly = false;
    private AudioManager audioManager;
    private bool isHidden = false;
    private Collider2D thisCol;
    private Explode explode;
    private float stunTime = 0.0f;
    private Coroutine bulletCoroutine;
    private bool pierceEnvironment = false;

    private Rigidbody2D rb;

    public bool onlyOnePierce = false;
    public bool extraWolvoDmg = false;
    public int wallBounces = 0;
    public float knockBackStrength = 0.5f;
    public float knockBackDuration = 0.1f;
    public bool homing = false;

    // Homing configuration
    public float homingRadius = 5f;         // search radius for targets (around the in-flight projectile)
    public float homingTurnSpeed = 5f;      // steering responsiveness (higher = snappier; soft-assist by default)
    private Enemy homingTarget = null;

    // Active-homing runtime state
    private float acquisitionTimer = 0f;    // throttles target searches while targetless
    private int homingPassFrames = 0;       // consecutive FixedUpdates spent moving away from the target

    // Homing tuning
    private const float HOMING_ACQUIRE_INTERVAL = 0.1f; // seconds between target searches (only while targetless)
    private const float HOMING_MAX_DROP_FACTOR = 1.5f;  // drop the target once it's farther than radius * this
    private const int HOMING_PASS_TOLERANCE = 5;        // FixedUpdates moving away before we treat it as passed

    // Shared, cached query state - avoids per-projectile GC and repeated NameToLayer lookups.
    // EnemyBlockingLayer holds many child colliders per enemy plus non-Enemy objects (critters,
    // hoverbikes), so the buffer is generous to avoid NonAlloc truncating before a valid enemy is seen.
    private static readonly Collider2D[] homingOverlapBuffer = new Collider2D[128];
    private static readonly HashSet<Enemy> homingSeenEnemies = new HashSet<Enemy>();
    private static int homingEnemyMask = 0;
    private static int homingSightMask = 0;
    private static bool homingMasksInit = false;

    public GameObject fireBomb;
    public bool incendiaryAmmo = false;

    private bool isSlowingTime = false;
    public GameObject slowTimeObj;

    public bool causesOverload = false;

    

    [System.NonSerialized]
    public static List<ProjectileScript> all = new List<ProjectileScript>();
    void OnEnable() => all.Add(this);
    void OnDisable() => all.Remove(this);

    void Awake()
    {
        audioManager = GetComponent<AudioManager>();
        thisCol = GetComponent<Collider2D>();
        if (this.gameObject.layer == 14)
            isFriendly = true;
        if (bulletType == 2)
            explode = GetComponent<Explode>();
        rb = GetComponent<Rigidbody2D>();
        rb.position += Vector2.zero; // This is enough to force registration
    }

    void FixedUpdate()
    {
        if (!homing)
            return;

        if (isHidden || rb == null)
        {
            resetHomingState();
            return;
        }

        // Sticky targeting: only search for a new target when we don't already hold a valid one.
        if (!isValidTarget(homingTarget))
        {
            homingTarget = null;
            acquisitionTimer += Time.fixedDeltaTime;
            if (acquisitionTimer >= HOMING_ACQUIRE_INTERVAL)
            {
                acquisitionTimer = 0f;
                acquireTarget();
            }
        }

        if (homingTarget == null)
            return;

        Vector2 vel = rb.velocity;
        float speed = vel.magnitude;
        // If something else zeroed our velocity (e.g. the "desync" charm pause), don't steer and
        // don't fabricate speed - just wait until real motion resumes.
        if (speed < 0.01f)
            return;

        Vector2 toTarget = (Vector2)homingTarget.transform.position - rb.position;
        float dist = toTarget.magnitude;

        // Drop a target that has drifted out of range; reacquisition can pick another next interval.
        if (dist > homingRadius * HOMING_MAX_DROP_FACTOR)
        {
            clearTarget();
            return;
        }

        if (dist > 0.0001f)
        {
            Vector2 toTargetDir = toTarget / dist;

            // Soft steer: bend velocity toward the target, then preserve the original speed magnitude.
            Vector2 desiredVel = toTargetDir * speed;
            Vector2 blended = Vector2.Lerp(vel, desiredVel, Mathf.Clamp01(homingTurnSpeed * Time.fixedDeltaTime));

            if (blended.sqrMagnitude > 0.0001f)
            {
                rb.velocity = blended.normalized * speed;

                float angle = Mathf.Atan2(rb.velocity.y, rb.velocity.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }

            // "Passed" detection: heading away from the target for several consecutive steps means
            // we've flown past it - drop and allow reacquisition.
            if (Vector2.Dot(rb.velocity.normalized, toTargetDir) < 0f)
                homingPassFrames++;
            else
                homingPassFrames = 0;

            if (homingPassFrames >= HOMING_PASS_TOLERANCE)
                clearTarget();
        }
    }

    // True only for a living, active enemy (excludes player, critters, NPCs, and dead/inactive enemies).
    private bool isValidTarget(Enemy e)
    {
        return e != null && !e.isDead && e.gameObject.activeSelf;
    }

    // Clears the current target but keeps homing active so another enemy can be acquired mid-flight.
    private void clearTarget()
    {
        homingTarget = null;
        homingPassFrames = 0;
    }

    // Terminal reset: stop homing entirely. Used when the projectile is hidden/expired so its
    // leftover impact light can never read as a live homing bullet.
    private void resetHomingState()
    {
        clearTarget();
        homing = false;
        acquisitionTimer = 0f;
    }

    private static void ensureHomingMasks()
    {
        if (homingMasksInit)
            return;

        homingEnemyMask = 1 << LayerMask.NameToLayer("EnemyBlockingLayer");
        homingSightMask = (1 << LayerMask.NameToLayer("Obstacle")) | (1 << LayerMask.NameToLayer("BlockingLayer"));
        homingMasksInit = true;
    }

    private void hideProjectile(bool destroyRigidbody, bool destroyProjectileAfterDelay)
    {
        resetHomingState();
        all.Remove(this);
        isHidden = true;

        if (destroyRigidbody)
        {
            Rigidbody2D currentRb = rb;
            if (currentRb == null)
                currentRb = GetComponent<Rigidbody2D>();

            if (currentRb != null)
                Destroy(currentRb);

            rb = null;
        }

        if (destroyProjectileAfterDelay)
            Destroy(gameObject, 1.0f);
    }

    // electroshock init
    public void initialize(float range, float dmg, float stunLength, bool c)
    {

        if (bulletType == 3)
            stunTime = stunLength;
        initialize(range, dmg, c);
    }

    public void initialize(float range, float dmg, bool c, float addCritDmg, float radius)
    {      
        float electroDmg = dmg;
        if (c)
            electroDmg += addCritDmg;

        explode.damage = electroDmg;
        explode.radius = radius;
        explode.crit = c;

        if (bulletType == 2)
            stunTime = 1f;

        bulletCoroutine = StartCoroutine(bulletLifetime(range, true));
    }

    public void changeExplodeRadius(float radius)
    {
        explode.radius = radius;
    }

    public void initialize(float range, float dmg, bool c, float addCritDmg)
    {
        critDmgAdd += addCritDmg;       
        initialize(range, dmg, c);
    }

    public void initialize(float range, float dmg, bool c)
    {
        bulletCoroutine = StartCoroutine(bulletLifetime(range));
        damage = dmg;
        crit = c;

        if (crit)
            damage += critDmgAdd;

        if(bulletType == 1)
            StartCoroutine(delayedCollision());
    }

    public void conferTrueAim()
    {
        trueAimParticles.SetActive(true);

        // add to damage. Right now it's:
        // 1 - +0%
        // 2 - +2%
        // 3 - +5%
        // 4 - +9%
        // 5 - +15%

        int trueAimLevel = WorldspaceCanvas.instance.getActiveTick();
        float addition = 0f;

        switch(trueAimLevel)
        {
            case 1:
                addition = 0f * damage;
                break;
            case 2:
                addition = 0.02f * damage;
                break;
            case 3:
                addition = 0.05f * damage;
                break;
            case 4:
                addition = 0.09f * damage;
                break;
            case 5:
                addition = 0.15f * damage;
                break;

        }

        damage += addition;
    }

    private IEnumerator delayedCollision()
    {
        yield return null;
        yield return null;
        thisCol.enabled = true;
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.gameObject.tag == "SlowTime" && !isSlowingTime)
        {
            rb.velocity *= 0.5f; // slow down bullet
        }

        if (col.isTrigger)
            return;

        if (col.CompareTag("Shield Wall"))
        {
            Vector2 collisionPoint = col.ClosestPoint(transform.position);
            reflectEnemyBullets(collisionPoint);
            //PlayerController.instance.damageCharacter(damage, new Vector2(transform.position.x, transform.position.y), false, ignoreShields, true, crit);
        }
        else
            collide(col);
    }

    protected virtual void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "SlowTime" && !isSlowingTime)
        {
            rb.velocity *= 2f; 
        }
    }

    public void reflectEnemyBullets(Vector2 collisionPoint)
    {
        if(!isFriendly)
            reflectBullet(collisionPoint);
    }

    public void reflectBullet(Vector2 collisionPoint)
    {     
        Vector2 collisionNormal = (transform.position - (Vector3)collisionPoint).normalized;

        if (collisionNormal != Vector2.zero)
        {
            Vector2 reflectedVelocity = Vector2.Reflect(rb.velocity, collisionNormal);
            rb.velocity = reflectedVelocity;
        }
        else
        {
            rb.velocity = rb.velocity * -1f; // fall back to just reversing course
        }

        isFriendly = true;
        gameObject.layer = 14;
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        collide(col.collider); // I'm pretty sure this never gets called when a bullet is a trigger, but keep it for non-trigger bullets just in case.
    }

    private void collide(Collider2D col)
    {
        bool passThrough = false;
        if (col.gameObject.tag == "Player" || col.gameObject.tag == "Enemy")
        {
            CombatChar colChar = col.gameObject.GetComponent<CombatChar>();

            // Check for Wolvolin behavior on the collided object and compute applied damage
            // without mutating the original `damage` field.
            float appliedDamage = damage;
            var wolvoBehavior = col.gameObject.GetComponent<SentinelBehavior>();
            if (wolvoBehavior != null)
            {
                // Increase damage when WolvolinBehavior is present.
                // Adjust the multiplier as desired; currently set to +50%.
                if (wolvoBehavior.enemyType == 1 && extraWolvoDmg)
                {
                    appliedDamage = damage * 1.5f;
                }
            }

            if (bulletType != 2 && bulletType != 3)
            {

                colChar.damageCharacter(appliedDamage, new Vector2(transform.position.x, transform.position.y), false, ignoreShields, true, crit);
                
            }
            else if (bulletType == 3)
            {
                colChar.damageCharacter(appliedDamage, new Vector2(transform.position.x, transform.position.y), true, ignoreShields, true, crit);
                // Try to process a stun if shields are out?
                colChar.stun(stunTime, batonStunParticles, causesOverload);
            }
            else if (bulletType == 2)
            {
                colChar.stun(stunTime, batonStunParticles);
            }

            if (bulletType == 1 || onlyOnePierce) // lancer goes thru players and enemies
            {
                passThrough = true;
                onlyOnePierce = false;

                if (lancerBurn != null)
                    Instantiate(lancerBurn, transform.position, Quaternion.identity, col.transform);
            }


            Vector2 knockbackDir = (col.transform.position - transform.position).normalized;
            Vector2 knockbackTargetPos = (Vector2)col.transform.position + knockbackDir * knockBackStrength;
            Rigidbody2D hitRb = col.gameObject.GetComponent<Rigidbody2D>();
            if (hitRb == null || hitRb.bodyType != RigidbodyType2D.Static)
                StartCoroutine(CharmBehaviorManager.instance.TeleportLerp(col.gameObject, knockbackTargetPos, knockBackDuration));

        }

        if (col.gameObject.tag == "Breakable" && bulletType == 1)
        {
            passThrough = true;
            if(lancerBurn != null)
                Instantiate(lancerBurn, transform.position, Quaternion.identity, col.transform);
        }

        if (col.gameObject.tag == "Computer" && bulletType == 3)
        {
            col.gameObject.GetComponent<ComputerBehavior>().powerOn();
            col.gameObject.GetComponent<ComputerBehavior>().triggerElectricity();
            Instantiate(batonStunParticles, col.transform.position, Quaternion.identity, col.transform);
        }

        if (col.gameObject.tag == "Bullet" && !col.gameObject.GetComponent<ProjectileScript>().collided)
        {           
            audioManager.PlaySound("Bullet_Collision");
            Instantiate(bulletCollision, transform.position, Quaternion.identity);
            if (bulletType == 1) // lancer goes through other bullets
                passThrough = true;
            else
                collided = true;
        }

        if (col.gameObject.tag == "Environment" || col.gameObject.tag == "Interactable" || col.gameObject.tag == "Environment - Overhead")
        {
            // actually , bounce 
            if (wallBounces > 0)
            {
                passThrough = true;
                wallBounces--;
                Vector2 collisionPoint = col.ClosestPoint(transform.position);
                reflectBullet(collisionPoint);
                damage *= 1.25f;
                if (audioManager != null)
                    audioManager.PlaySound("Terrain_Impact", true);
            }
            else
            {
                pierceEnvironment = true;
            }
        }

        if(col.gameObject.layer == 12) // kiplingCanPass 
        {
            Physics2D.IgnoreCollision(col, thisCol);               
            return;
        }

        if(col.gameObject.layer == 0 && bulletType == 1)
        {
            passThrough = true;
        }

        if(col.gameObject.layer == 18 && isFriendly) // only player bullets blow up throwables
        {
            col.gameObject.GetComponent<Explode>().overrideCountdown();
            if (bulletType == 1 || onlyOnePierce) // lancer goes through explosives
                passThrough = true;
        }

        if(col.gameObject.tag == "OilThistle")
        {
            col.GetComponent<OilthistleBehavior>().blowUp();
        }

        if (!passThrough)
        {
            if(this.gameObject.activeSelf)
                StartCoroutine(tryPierce(pierceEnvironment, col));

            if(incendiaryAmmo)
            {
                Instantiate(fireBomb, transform.position, Quaternion.identity);
            }
        }
    }

    private IEnumerator tryPierce(bool pierce, Collider2D col)
    {
        if (pierce) // check direction before spawning in debris and making a bullethole
        {
            Vector2 velocity = GetComponent<Rigidbody2D>().velocity;
            float absX = Mathf.Abs(velocity.x);
            float absY = Mathf.Abs(velocity.y);

            float alterXVal = 0f;
            float alterYVal = 0f;
            float dustAngle = 0f; // starts up

            if (absX > absY)
            {
                pierce = false; // don't clip if coming from the side

                if (velocity.x > 0)
                {
                    // Bullet is primarily moving right
                    dustAngle = 90f;
                    alterXVal = -1f;
                    alterYVal = 1f;
                }
                else
                {
                    // Bullet is primarily moving left
                    dustAngle = 270f;
                    alterXVal = 1f;
                    alterYVal = 1f;
                }
            }
            else
            {
                if (velocity.y > 0)
                {
                    dustAngle = 180f;
                    // Bullet is primarily moving up
                }
                else
                {
                    pierce = false; // don't clip if coming from above
                    alterYVal = 2f;
                }
            }           

            if (pierce && col.gameObject.tag != "Environment - Overhead") // if still piercing, make sure not overhead and then wait
            {
                if (bulletType != 1)
                {
                    //Debug.Log("piercing");
                    // Model timing is 0.02f for a bullet speed of 40.
                    float waitTime = 0.02f * (40f / bulletSpeed);
                    yield return new WaitForSecondsRealtime(Random.Range(0.85f * waitTime, 0.9f * waitTime)); // slight variance in wait                   
                }
                
                Instantiate(bulletHole, transform.position, Quaternion.Euler(0, 0, Random.Range(0, 4) * 90), ObjectPooler.instance.transform); // only bullet hole if coming from below
            }

            
            // always instantiate debris if we're hitting an obstacle/environment
            GameObject debrisObj = Instantiate(debris, transform.position, Quaternion.identity, ObjectPooler.instance.transform);
            if (alterXVal != 0f || alterYVal != 0f)
            {
                debrisObj.GetComponent<bulletShellBehavior>().alterCourse(alterXVal, alterYVal);
            }

            // shoot out dust
            Instantiate(collisionDust, transform.position, Quaternion.Euler(0f, 0f, dustAngle), ObjectPooler.instance.transform);
        }

        Instantiate(bulletImpact, transform.position, Quaternion.identity);

        gameObject.GetComponent<BoxCollider2D>().enabled = false;
      

        if (gameObject.transform.childCount > 0)
        {
            SpriteRenderer renderer = gameObject.transform.GetChild(0).transform.GetComponent<SpriteRenderer>();
            if (renderer != null)
                renderer.enabled = false;

            GameObject lightChild = gameObject.transform.GetChild(2).gameObject;
            if (lightChild != null)
            {
                LightBehavior lb = lightChild.transform.GetComponent<LightBehavior>();
                lb.enabled = true;
                StopCoroutine(bulletCoroutine);
                //bulletLifetime(1.0f);
            }
        }

        if (bulletType == 2)
        {
            GetComponent<Explode>().overrideCountdown();
            transform.GetChild(3).gameObject.SetActive(false);
            Destroy(this.gameObject, 1.0f);
        }

        if (bulletType == 3)
        {
            Instantiate(electricity, transform.position, Quaternion.identity);
        }

        if(audioManager != null)
            audioManager.PlaySound("Terrain_Impact", true);
        
        if (bulletType == 1 && lancerBurn != null)
            Instantiate(lancerBurn, transform.position, Quaternion.identity, col.transform);

        hideProjectile(true, true);
    }

    /*private void OnCollisionStay2D(Collision2D col)
    {
        Physics2D.IgnoreCollision(col.collider, thisCol);
    }*/

    private IEnumerator bulletLifetime(float range)
    {
        Vector3 startPos = transform.position;

        if (!inert)
        {
            while (Vector3.Distance(startPos, transform.position) < range)
            {
                yield return null;
            }
        }

        if (!isHidden)
        {
            GetComponent<Rigidbody2D>().velocity = Vector2.zero;
            //Destroy(this.gameObject);
            gameObject.GetComponent<BoxCollider2D>().enabled = false;

            if (gameObject.transform.childCount >= 1)
            {
                GameObject lightChild = gameObject.transform.GetChild(2).gameObject;

                if (lightChild != null)
                {
                    lightChild.SetActive(false);
                }

                SpriteRenderer renderer = gameObject.transform.GetChild(0).transform.GetComponent<SpriteRenderer>();
                if (renderer != null)
                    renderer.enabled = false;
            }
        }
        //else
        hideProjectile(false, false);
        Destroy(this.gameObject, 1.0f);
    }

    private IEnumerator bulletLifetime(float range, bool isElectro)
    {
        Vector3 startPos = transform.position;

        while (Vector3.Distance(startPos, transform.position) < range)
        {
            yield return null;
        }

        GetComponent<Explode>().overrideCountdown();
        transform.GetChild(3).gameObject.SetActive(false);
        transform.GetChild(2).gameObject.SetActive(false);
        gameObject.GetComponent<BoxCollider2D>().enabled = false;
        hideProjectile(true, false);
        transform.GetChild(0).transform.GetComponent<SpriteRenderer>().enabled = false;
        Destroy(this.gameObject, 1.0f);
    }

    // ---------- Charm Behaviors ----------
    public void shootBackward()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (rb == null)
            return;

        Vector2 currentVel = rb.velocity;
        if (currentVel != Vector2.zero)
        {
            rb.velocity = -currentVel;
        }
    }

    public void applyRandomAngle(float minAngle, float maxAngle)
    {
        // choose random delta between minAngle and +maxAngle
        float deltaDeg = Random.Range(minAngle, maxAngle);

        applyAngle(deltaDeg);
    }

    public void applyAngle(float deltaDeg)
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (rb == null)
            return;

        Vector2 currentVel = rb.velocity;
        // If velocity is essentially zero, nothing to re-angle
        if (currentVel.sqrMagnitude <= 0.0001f)
            return;

        float speed = currentVel.magnitude;
        if (speed < 0.01f)
            speed = bulletSpeed;

        // current direction in degrees
        float currentAngleDeg = Mathf.Atan2(currentVel.y, currentVel.x) * Mathf.Rad2Deg;

        float newAngleDeg = currentAngleDeg + deltaDeg;
        float newAngleRad = newAngleDeg * Mathf.Deg2Rad;

        Vector2 newDir = new Vector2(Mathf.Cos(newAngleRad), Mathf.Sin(newAngleRad)).normalized;
        rb.velocity = newDir * speed;

        // rotate to face velocity direction (optional but consistent with other code)
        transform.rotation = Quaternion.Euler(0f, 0f, newAngleDeg);
    }

    public void desyncShot()
    {
        StartCoroutine(updateRbVelocity());
    }

    private IEnumerator updateRbVelocity()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (rb == null)
            yield break;

        Vector2 currentVel = rb.velocity;
        rb.velocity = Vector2.zero;

        yield return new WaitForSeconds(0.3f);

        if(rb != null)
            rb.velocity = currentVel;
    }

    public void startZigZag()
    {
        StartCoroutine(zigZagOverTime(0.1f));
    }

    private IEnumerator zigZagOverTime(float timeBetweenZag)
    {
        while (true)
        {
            yield return new WaitForSeconds(timeBetweenZag);
            applyRandomAngle(0f, 45f);
            yield return new WaitForSeconds(timeBetweenZag);
            applyRandomAngle(-45f, 0f);
        }
    }

    public void angleBullet(float angle)
    {

    }

    public void startHoming()
    {
        homing = true;
        clearTarget();
        acquisitionTimer = HOMING_ACQUIRE_INTERVAL; // allow an acquisition attempt on the next FixedUpdate
    }

    // Picks the best living enemy within homingRadius that we have line-of-sight to, weighting
    // enemies ahead of the projectile over ones behind it. Runs only while targetless (sticky).
    private void acquireTarget()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        ensureHomingMasks();
        homingSeenEnemies.Clear();

        Vector2 myPos = rb != null ? rb.position : (Vector2)transform.position;
        Vector2 vel = rb != null ? rb.velocity : Vector2.zero;
        Vector2 forward = vel.sqrMagnitude > 0.0001f ? vel.normalized : Vector2.zero;

        int count = Physics2D.OverlapCircleNonAlloc(myPos, homingRadius, homingOverlapBuffer, homingEnemyMask);

        Enemy best = null;
        float bestScore = float.MinValue;

        for (int i = 0; i < count; i++)
        {
            Collider2D col = homingOverlapBuffer[i];
            if (col == null)
                continue;

            // The EnemyBlockingLayer collider may sit on a child of the enemy root.
            Enemy e = col.GetComponentInParent<Enemy>();
            if (!isValidTarget(e))
                continue;

            // Skip enemies we've already scored this pass - each has many child colliders, and the
            // score depends only on the enemy root position, so one evaluation (and one LOS ray) is enough.
            if (!homingSeenEnemies.Add(e))
                continue;

            Vector2 toTarget = (Vector2)e.transform.position - myPos;
            float dist = toTarget.magnitude;
            if (dist < 0.0001f || dist > homingRadius)
                continue;

            // Line-of-sight: skip enemies hidden behind walls/obstacles.
            Vector2 toTargetDir = toTarget / dist;
            if (Physics2D.Raycast(myPos, toTargetDir, dist, homingSightMask).collider != null)
                continue;

            // Prefer enemies ahead of us (forward alignment in [-1, 1]) and nearby (proximity in (0, 1]).
            float forwardAlign = forward == Vector2.zero ? 0f : Vector2.Dot(forward, toTargetDir);
            float proximity = 1f - (dist / homingRadius);
            float score = forwardAlign + proximity;

            if (score > bestScore)
            {
                bestScore = score;
                best = e;
            }
        }

        if (best != null)
        {
            homingTarget = best;
            homingPassFrames = 0;
        }
    }

    public void slowTime()
    {
        if (slowTimeObj != null)
        {
            GameObject instantiatedSlowTime = Instantiate(slowTimeObj, transform.position, Quaternion.identity);
            instantiatedSlowTime.GetComponent<SlowTimeBehavior>().setFollowTarget(transform);
            isSlowingTime = true;
        }
    }
    public void changeScaleByMultiple(float scaleMult)
    {
        LineRenderer lineRenderer = GetComponentInChildren<LineRenderer>();
        Light2D bulletCircleLight = GetComponentInChildren<Light2D>();

        transform.localScale *= scaleMult;

        if(lineRenderer != null)
        {
            lineRenderer.startWidth *= scaleMult;
        }

        if(bulletCircleLight != null)
        {
            bulletCircleLight.pointLightInnerRadius *= scaleMult;
            bulletCircleLight.pointLightOuterRadius *= scaleMult;
        }

    }

    private int getCorrectChildIndex(int defaultIndex)
    {
        int childIndex = defaultIndex;
        if (bulletType == 2)
        {
            childIndex = defaultIndex + 1;
        }
        if (bulletType == 1)
        {
            childIndex = defaultIndex - 1;
        }
        return childIndex;
    }
}

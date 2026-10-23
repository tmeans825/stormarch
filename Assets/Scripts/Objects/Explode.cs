using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Com.LuisPedroFonseca.ProCamera2D;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Rendering;

public class Explode : MonoBehaviour
{
    [Header("Explosion Stats")]
    [Tooltip("Delay in seconds")]
    public float delay = 3f;
    public float damage = 50f;
    public float radius = 5f;
    public bool ignorePlayer = false;

    [Tooltip("Explosion particle system - should have DestroyAfterSeconds attached.")]
    public GameObject vfx;

    [Tooltip("0 = None - use Misc_Explosion, 1 = Small, 2 = Medium, 3 = Large, 4 = Huge")]
    [Range(0, 3)]
    public int explosionSize = 1;

    public bool crit = false;
    public bool distanceBasedDmg = true;
    public bool electrospikeVFXScaling = false;

    public Light2D radiusLight;

    public bool requireManualExplode = false;

    public float stunTime = 0f; // if > 0, will stun and clear enemy shields
    public GameObject batonStunParticles; 

    protected Vector3 offset = Vector3.zero;
    protected bool handleDamage = true;
    protected GameObject explosion;

    private string screenShake;
    private string sfx;

    public float countdown;
    private bool hasExploded = false;

    const string MISC_SFX = "Misc_Explosion";

    const string SMALL_SHAKE = "Small Shake";
    const string SMALL_SFX = "Small_Explosion";

    const string MEDIUM_SHAKE = "Medium Shake";
    const string MEDIUM_SFX = "Medium_Explosion";

    const string LARGE_SHAKE = "Large Shake";
    const string LARGE_SFX = "Large_Explosion";


    // Start is called before the first frame update
    void Awake()
    {
        switch(explosionSize) // also can set audio here
        {
            case 0:
                sfx = MISC_SFX;
                break;
            case 1:
                screenShake = SMALL_SHAKE;
                sfx = SMALL_SFX;
                break;
            case 2:
                screenShake = MEDIUM_SHAKE;
                sfx = MEDIUM_SFX;
                break;
            case 3:
                screenShake = LARGE_SHAKE;
                sfx = LARGE_SFX;
                break;
            default:
                screenShake = SMALL_SHAKE;
                sfx = SMALL_SFX;
                break;
        }

        countdown = delay;

        if(radiusLight != null)
            radiusLight.pointLightOuterRadius = radius;
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        if (requireManualExplode)
            return;

        countdown -= Time.deltaTime;
        if(countdown <= 0 && !hasExploded)
        {
            blowUp(transform, offset);
            hasExploded = true;
        }
    }

    public virtual void initialize(float r, float d)
    {
        radius = r;
        damage = d;

        if(radiusLight != null)
            radiusLight.pointLightOuterRadius = radius;
    }

    public virtual void overrideCountdown()
    {
        countdown = 0;
    }

    public void manualExplosion()
    {
        blowUp(transform, offset);
        hasExploded = true;
    }

    protected virtual void blowUp(Transform parent, Vector3 offset)
    {
        explosion = Instantiate(vfx, transform.position + offset, transform.rotation, parent);

        if (explosion.GetComponent<FireBombExplosionVFX>() != null)
            explosion.GetComponent<FireBombExplosionVFX>().setValues(radius);

        if (electrospikeVFXScaling)
        {
            ParticleSystem.ShapeModule ps = explosion.transform.GetChild(1).GetComponent<ParticleSystem>().shape;
            ps.radius *= radius;

            float lightningRadius = radius;
            if(radius > 5f)
                lightningRadius = 5f; // cap the radius for the main lightning effect
            explosion.transform.GetChild(2).gameObject.transform.localScale *= lightningRadius; // main lightning, cap of 5


            explosion.transform.GetChild(3).GetComponent<Light2D>().pointLightOuterRadius *= radius;

            // Overload ability specific here
            if (explosion.transform.childCount > 5)
            {
                Transform child5 = explosion.transform.GetChild(5);

                if (child5 != null)
                {
                    ParticleSystem smallPs = child5.GetComponent<ParticleSystem>();
                    if (smallPs != null)
                    {
                        ParticleSystem.ShapeModule smallLightningPS = smallPs.shape;
                        smallLightningPS.radius *= radius; // small lightning, no cap
                        smallLightningPS.radiusThickness *= radius; // small lightning, no cap
                    }
                }
            }
        }

        explosion.GetComponent<AudioManager>().PlaySound(sfx);

        if (explosionSize != 0)
        {
            ProCamera2DShake.Instance.Shake(screenShake);
        }


        int ebl = 1 << LayerMask.NameToLayer("EnemyBlockingLayer");
        int bl = 1 << LayerMask.NameToLayer("BlockingLayer");
        int cl = 1 << LayerMask.NameToLayer("Critter");
        int ol = 1 << LayerMask.NameToLayer("Obstacle");
        int mask;
        if(ignorePlayer)
            mask = ebl | cl | ol;
        else
            mask = ebl | bl | cl | ol;

        //Debug.Log("Radius is " + radius);
        Collider2D[] colls = Physics2D.OverlapCircleAll(transform.position, radius, mask);
        //Debug.Log("Total collisions are " + colls.Length);

        float blastForce = 2.5f;

        if (explosionSize < 2)
            blastForce = 0.5f;

        for (int i = 0; i < colls.Length; i++)
        {
            if (colls[i].gameObject != this.gameObject)
            {
                CombatChar c = colls[i].gameObject.GetComponent<CombatChar>();

                if (c != null && handleDamage)
                {
                    if(stunTime > 0f)
                    {
                        c.stun(stunTime, batonStunParticles);
                    }


                    float dmg;
                    if (distanceBasedDmg)
                    {
                        float distancePercentage = Vector2.Distance(transform.position, c.transform.position) / radius;
                        dmg = damage - ((distancePercentage / 2) * damage);
                    }
                    else
                        dmg = damage;

                    if(dmg > 0)
                        c.damageCharacter(dmg, colls[i].gameObject.transform.position, false, true, true, crit);
                }
                else
                {
                    WoodenCrateBehavior wcb = colls[i].gameObject.GetComponent<WoodenCrateBehavior>();

                    if (wcb != null)
                    {
                        wcb.destroyCrate(blastForce, transform.position);
                    }
                    else
                    {
                        DestructibleWallBehavior dwb = colls[i].gameObject.GetComponent<DestructibleWallBehavior>();

                        if (dwb != null)
                        {
                            dwb.destroyWall(blastForce, transform.position);
                        }
                        else
                        {
                            Clotberry cb = colls[i].gameObject.GetComponentInParent<Clotberry>();

                            if (cb != null)
                                cb.useClotberry();
                        }
                    }
                }
            }
        }
    }
  

    // temporary test scripts ---------------------------------------------------------------------

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }


}

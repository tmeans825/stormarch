using System.Collections;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine;

public class InfestationBehavior : MonoBehaviour
{
    public GameObject player;
    public float damage;
    public float cooldown;
    public float slowdownModifier;
    public float rampUpRate = 1.0f;
    public Light2D globalLight;
    public float darkenedIntensity = 0.1f;
    public List<ParticleCollisionEvent> collisionEvents;
    private LightBehavior globalLightBehavior;
    private PlayerController pc;
    private bool canDamage = true;
    private ParticleSystem ps;
    private float originalLightIntensity;
    

    private void Start()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player");
        pc = player.GetComponent<PlayerController>();
        ps = GetComponent<ParticleSystem>();
        originalLightIntensity = globalLight.intensity;
        globalLightBehavior = globalLight.GetComponent<LightBehavior>();
        collisionEvents = new List<ParticleCollisionEvent>();
    }

    private void OnParticleCollision(GameObject other)
    {
        if (other.tag == "Player" && canDamage)
        {
            int numCollisionEvents = ps.GetCollisionEvents(other, collisionEvents);

            for (int i = 0; i < numCollisionEvents; i++) // this is to handle not damaging player if they're in lights
            {
                Collider2D[] colls = Physics2D.OverlapCircleAll(collisionEvents[i].intersection, 0.5f, 1 << LayerMask.NameToLayer("Ignore Raycast"));
                if(colls.Length == 0)
                {
                    //Debug.Log("hit player");
                    StartCoroutine(damagePlayer());
                    break;
                }
            }
        }
        if(other.tag == "Critter")
        {
            other.GetComponent<BoxCollider2D>().enabled = false;
            other.GetComponent<Critter>().startMoving(other.transform.position - new Vector3(-1f, 0f, 0f));
        }
        if(other.tag == "Kipling")
        {
            other.GetComponent<KiplingBehavior>().hitInfestation();
        }
    }

    private IEnumerator damagePlayer()
    {
        canDamage = false;
        //pc.infest(slowdownModifier);
        yield return new WaitForSeconds(cooldown);
        canDamage = true;
    }

    public bool toggleParticles()
    {
        if (ps.isEmitting)
        {
            StopAllCoroutines();
            ps.Stop();
            StartCoroutine(globalLightBehavior.fadeInAndOut(darkenedIntensity, originalLightIntensity, true, 2.0f));
            return false;
        }
        else
        {
            StartCoroutine(beginInfesting());
            return true;
        }
    }

    private IEnumerator beginInfesting()
    {
        StartCoroutine(globalLightBehavior.fadeInAndOut(darkenedIntensity, originalLightIntensity, false, 2.0f));
        var em = ps.emission;
        em.rateOverTime = 10f;
        ps.Play();
        yield return new WaitForSeconds(rampUpRate);
        em.rateOverTime = 30f;
        yield return new WaitForSeconds(rampUpRate);
        em.rateOverTime = 70f;
        yield return new WaitForSeconds(rampUpRate);
        em.rateOverTime = 150f;
        yield return new WaitForSeconds(rampUpRate);
        em.rateOverTime = 200f;
    }
}

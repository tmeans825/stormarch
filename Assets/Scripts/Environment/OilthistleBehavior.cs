using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OilthistleBehavior : MonoBehaviour
{
    public float breakVelocityThreshold = 10f;
    public GameObject stemToBreak;
    public Rigidbody2D plantRB;

    public float newMass = 70000f;
    public float newLinDrag = 30;
    public float newAngDrag = 30;

    public float slideForce = 1000f;
    public float walkForce = 500f;

    public ParticleSystem pushDustPS;
    public Animator anim;

    public GameObject firebombPrefab;
    public float radius = 20f;
    public float damage = 10f;

    public AudioManager am;

    private bool isDetached = false;

    // Persists across scene reload so respawn knows which plants to restore. Default false
    // protects story-disabled untouched plants (and un-Awake'd inactive ones) from being
    // touched by the static iteration.
    [System.NonSerialized]
    [ES3Serializable]
    private bool wasConsumed = false;

    // Captured at Awake. detach() and blowUp() mutate physics, transform, parent, and
    // collider settings, none of which are saved fields. blowUp() also reparents pushDustPS
    // to world root and rescales it, so we capture the particle's original parent + local
    // transform + active state to restore on respawn.
    private float origMass, origLinDrag, origAngDrag, origGravityScale;
    private Vector3 origPosition, origLocalScale;
    private Quaternion origRotation;
    private Transform origParent;
    private Vector2 origCollOffset, origCollSize;
    private Transform origPushDustParent;
    private Vector3 origPushDustLocalPosition, origPushDustLocalScale;
    private Quaternion origPushDustLocalRotation;
    private bool origPushDustActive;

    private void Awake()
    {
        if (plantRB != null)
        {
            origMass = plantRB.mass;
            origLinDrag = plantRB.drag;
            origAngDrag = plantRB.angularDrag;
            origGravityScale = plantRB.gravityScale;
        }

        origPosition = transform.position;
        origRotation = transform.rotation;
        origLocalScale = transform.localScale;
        origParent = transform.parent;

        var box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            origCollOffset = box.offset;
            origCollSize = box.size;
        }

        if (pushDustPS != null)
        {
            Transform pdt = pushDustPS.transform;
            origPushDustParent = pdt.parent;
            origPushDustLocalPosition = pdt.localPosition;
            origPushDustLocalRotation = pdt.localRotation;
            origPushDustLocalScale = pdt.localScale;
            origPushDustActive = pushDustPS.gameObject.activeSelf;
        }
    }

    private void Start()
    {
        GameEvents.current.onPreviousSceneDataLoaded += previousSceneDataLoaded;
    }

    private void previousSceneDataLoaded()
    {
        if (plantRB.gameObject.activeSelf && !stemToBreak.activeSelf)
        {
            detach();
            plantRB.transform.rotation = Quaternion.Euler(0, 0, 90);
            plantRB.transform.position -= new Vector3(0f, 1.1f, 0f);
            anim.SetTrigger("boinga");
        }
    }

    private void Update()
    {
        if (plantRB == null || !isDetached) // only play this fun stuff if detached
            return;

        if ((plantRB.velocity.magnitude >= 1f) && (!pushDustPS.isPlaying))
        {
            //pushDustPS.Play();
            am.PlaySound("Slide");
        }
        else if ((plantRB.velocity.magnitude < 1f) && (pushDustPS.isPlaying))
        {
            //pushDustPS.Stop();
            am.StopSound("Slide");
        }
    }

    public void blowUp()
    {
        wasConsumed = true;
        pushDustPS.transform.parent = null;
        pushDustPS.transform.localScale = new Vector3(1f, 1f, 1f);
        GameObject huckedNade = Instantiate(firebombPrefab, transform.position, transform.rotation);
        huckedNade.GetComponent<GrenadeExplode>().initializeGrenade(radius, damage, GetComponent<BoxCollider2D>());
        huckedNade.GetComponent<GrenadeExplode>().overrideCountdown();
        gameObject.SetActive(false);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDetached)
        {
            float tempForce = walkForce;

            if (collision.collider.transform.parent != null)
            {
                if (collision.collider.transform.parent.gameObject == PlayerController.instance.gameObject)
                {
                    am.PlaySound("Splat", true);
                    anim.SetTrigger("boinga");

                    if (PlayerController.instance.isSliding)
                    {
                        tempForce = slideForce;
                    }
                }
            }

            kickIt(collision.collider, tempForce);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isDetached)
            return;

        // DETACH CHECK

        if (collision.attachedRigidbody == null || !(collision.attachedRigidbody.velocity.magnitude >= breakVelocityThreshold)) // only break if something hits it fast enough, or it's a bullet
        {
            // play push around sfx?
            return;
        }

        detach();
        anim.SetTrigger("boinga");
        kickIt(collision, slideForce);
    }

    private void detach()
    {
        wasConsumed = true;
        pushDustPS.gameObject.SetActive(true);
        isDetached = true;
        plantRB.gravityScale = 0f;
        plantRB.mass = newMass;
        plantRB.drag = newLinDrag;
        plantRB.angularDrag = newAngDrag;
        stemToBreak.SetActive(false);
        transform.parent = null;
        GetComponent<BoxCollider2D>().offset = new Vector2(0f, 0f);
        GetComponent<BoxCollider2D>().size = new Vector2(0.13f, 0.15f);
        GetComponent<CapsuleCollider2D>().enabled = false;
        transform.localScale = new Vector3(10f, 10f, 10f);
    }

    private void kickIt(Collider2D collision, float force)
    {
        Vector2 direction = (transform.position - collision.transform.position).normalized;
        Vector2 knockback = direction * force;

        plantRB.AddForce(knockback, ForceMode2D.Impulse);
    }

    private void OnDestroy()
    {
        GameEvents.current.onPreviousSceneDataLoaded -= previousSceneDataLoaded;
    }

    // Reverses both detach() and blowUp(). Called by respawnAllConsumedInCurrentScene
    // during the death/camp respawn flow. Handles the blown-up case (gameObject inactive,
    // pushDustPS reparented to world root + rescaled) and the merely-detached case.
    public void respawnToIntact()
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);

        // Plant transform.
        transform.parent = origParent;
        transform.position = origPosition;
        transform.rotation = origRotation;
        transform.localScale = origLocalScale;

        // Plant physics.
        if (plantRB != null)
        {
            plantRB.mass = origMass;
            plantRB.drag = origLinDrag;
            plantRB.angularDrag = origAngDrag;
            plantRB.gravityScale = origGravityScale;
            plantRB.velocity = Vector2.zero;
            plantRB.angularVelocity = 0f;
        }

        // Colliders.
        var box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            box.offset = origCollOffset;
            box.size = origCollSize;
        }
        var capsule = GetComponent<CapsuleCollider2D>();
        if (capsule != null) capsule.enabled = true;

        // pushDustPS — blowUp reparents this to world root and rescales it. Without this
        // restore step the particle GameObject orphans into scene state.
        if (pushDustPS != null)
        {
            Transform pdt = pushDustPS.transform;
            pdt.parent = origPushDustParent;
            pdt.localPosition = origPushDustLocalPosition;
            pdt.localRotation = origPushDustLocalRotation;
            pdt.localScale = origPushDustLocalScale;
            pushDustPS.gameObject.SetActive(origPushDustActive);
            if (pushDustPS.isPlaying) pushDustPS.Stop();
        }

        stemToBreak.SetActive(true);
        isDetached = false;
        wasConsumed = false;

        if (am != null) am.StopSound("Slide");
    }

    public static void respawnAllConsumedInCurrentScene()
    {
        var all = GameObject.FindObjectsOfType<OilthistleBehavior>(true);
        for (int i = 0; i < all.Length; i++)
        {
            OilthistleBehavior o = all[i];
            if (o == null) continue;
            if (!o.wasConsumed) continue;
            o.respawnToIntact();
        }
    }
}

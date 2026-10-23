using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Com.LuisPedroFonseca.ProCamera2D;

public class WoodenCrateBehavior : Pushable
{
    public SpriteRenderer mainSprite;
    public GameObject debrisGrouper;

    public float breakVelocityThreshold = 5f;

    public float breakForce = 5f;

    public float breakRadius = 5f;

    public string[] hitFX = new string[2] { "Crate Hit 1", "Crate Hit 2" };
    public string[] impactFX = new string[3] { "Impact 1", "Impact 2", "Impact 3" };

    const string TINY_SHAKE = "Tiny Shake";

    [ES3Serializable]
    [System.NonSerialized]
    private bool isBroken = false;

    // Captured at Awake so resetBrokenState can put debris back the way the prefab authored
    // them. ES3 may serialize child transforms / Rigidbody2D state depending on autosave
    // setup, so we can't rely on scene reload alone to undo the explosion-driven mutations.
    [System.NonSerialized]
    private Vector3[] origDebrisLocalPositions;
    [System.NonSerialized]
    private Quaternion[] origDebrisLocalRotations;
    [System.NonSerialized]
    private Vector3[] origDebrisLocalScales;
    [System.NonSerialized]
    private Transform[] debrisTransforms;
    [System.NonSerialized]
    private Rigidbody2D[] debrisRBs;

    protected override void Awake()
    {
        base.Awake();

        if (debrisGrouper != null)
        {
            // Includes inactive children — debrisGrouper is inactive in the prefab default.
            var children = debrisGrouper.GetComponentsInChildren<Transform>(true);
            // Skip index 0 (debrisGrouper's own transform).
            int n = children.Length > 0 ? children.Length - 1 : 0;
            debrisTransforms = new Transform[n];
            origDebrisLocalPositions = new Vector3[n];
            origDebrisLocalRotations = new Quaternion[n];
            origDebrisLocalScales = new Vector3[n];
            debrisRBs = new Rigidbody2D[n];
            for (int i = 0, w = 0; i < children.Length; i++)
            {
                if (children[i] == debrisGrouper.transform) continue;
                debrisTransforms[w] = children[i];
                origDebrisLocalPositions[w] = children[i].localPosition;
                origDebrisLocalRotations[w] = children[i].localRotation;
                origDebrisLocalScales[w] = children[i].localScale;
                debrisRBs[w] = children[i].GetComponent<Rigidbody2D>();
                w++;
            }
        }
    }

    protected override void previousSceneDataLoaded()
    {
        if (isBroken)
        {
            gameObject.SetActive(false);
            return;
        }

        base.previousSceneDataLoaded();
    }

    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        base.OnTriggerEnter2D(collision);

        if (isBroken || (collision.attachedRigidbody == null))
            return;

        if (!(collision.attachedRigidbody.velocity.magnitude >= breakVelocityThreshold) && (collision.tag != "Bullet")) // only break if something hits it fast enough, or it's a bullet
            return;

        float isCharMultiplier = 1f;
        if (collision.tag == "Player")
        {
            isCharMultiplier = 2f;
        }

        destroyCrate(isCharMultiplier, collision.transform.position);

        if (collision.tag == "Bullet")
        {
            //if (collision.GetComponent<ProjectileScript>().bulletType != 1)
                //collision.gameObject.SetActive(false);
        }
    }

    public void destroyCrate(float impactMultiplier, Vector3 impactPos)
    {
        ProCamera2DShake.Instance.Shake(TINY_SHAKE);
        EventManager.instance.hitFreeze();
        isBroken = true;

        mainSprite.enabled = false;

        foreach (BoxCollider2D bc in GetComponents<BoxCollider2D>())
            bc.enabled = false;

        debrisGrouper.SetActive(true);

        foreach (BoxCollider2D coll in debrisGrouper.GetComponentsInChildren<BoxCollider2D>())
        {
            Rigidbody2D rb = coll.GetComponent<Rigidbody2D>();

            if (rb != null)
            {
                float randForce = Random.Range(0.5f, 1.5f) * (breakForce * impactMultiplier);
                TEMLib.AddExplosionForce(rb, randForce, impactPos, breakRadius);
            }
        }

        int randHit = Random.Range(0, hitFX.Length);
        int randImpact = Random.Range(0, impactFX.Length);

        am.PlaySound(hitFX[randHit], true);
        am.PlaySound(impactFX[randImpact], true);

        DropManager.instance.generateDrop(dropCategories.classification.Crate, transform.position);

        StartCoroutine(slideTimer());
    }

    private IEnumerator slideTimer() // disable physics after a while
    {
        yield return new WaitForSeconds(3f);

        foreach (BoxCollider2D coll in debrisGrouper.GetComponentsInChildren<BoxCollider2D>())
            coll.enabled = false;
    }

    // Always called by Pushable.respawnAllPushedInCurrentScene() via virtual dispatch.
    // MUST call base unconditionally so intact-but-pushed crates still get position-reset.
    public override void respawnToOriginalLocation()
    {
        StopAllCoroutines();

        if (isBroken)
            resetBrokenState();

        base.respawnToOriginalLocation();
    }

    // Reverses destroyCrate()'s mutations. Includes the GameObject reactivation that's
    // critical for crates that were broken in a prior session: previousSceneDataLoaded
    // SetActive(false)'d them on load, so without this the post-respawn save would
    // persist an "intact-but-still-inactive" crate that stays missing forever.
    private void resetBrokenState()
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);

        isBroken = false;
        mainSprite.enabled = true;

        foreach (BoxCollider2D bc in GetComponents<BoxCollider2D>())
            bc.enabled = true;

        // Restore each debris piece to its prefab-authored local pose and zero its
        // velocity, then hide the group again. ES3 autosave may capture child kinematic
        // state, so we can't trust scene reload to undo this on its own.
        if (debrisTransforms != null)
        {
            for (int i = 0; i < debrisTransforms.Length; i++)
            {
                Transform t = debrisTransforms[i];
                if (t == null) continue;
                t.localPosition = origDebrisLocalPositions[i];
                t.localRotation = origDebrisLocalRotations[i];
                t.localScale = origDebrisLocalScales[i];
                Rigidbody2D rb = debrisRBs[i];
                if (rb != null)
                {
                    rb.velocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                }
            }
        }

        // Re-enable any debris colliders that slideTimer disabled, so a future break
        // of this crate produces real collisions again.
        if (debrisGrouper != null)
        {
            foreach (BoxCollider2D bc in debrisGrouper.GetComponentsInChildren<BoxCollider2D>(true))
                bc.enabled = true;

            debrisGrouper.SetActive(false);
        }
    }

    // Static iteration for the death/camp respawn flow. Includes inactive instances —
    // post-reload broken crates are SetActive(false) by previousSceneDataLoaded.
    // The isBroken filter protects story-disabled non-broken inactive crates from being
    // touched. The override of respawnToOriginalLocation chains into resetBrokenState
    // (which reactivates the GameObject) when isBroken, then unconditionally restores
    // position via the base call.
    public static void respawnAllBrokenInCurrentScene()
    {
        var all = GameObject.FindObjectsOfType<WoodenCrateBehavior>(true);
        for (int i = 0; i < all.Length; i++)
        {
            WoodenCrateBehavior c = all[i];
            if (c == null) continue;
            if (!c.isBroken) continue;
            if (!c.spawnAnchorRecorded) continue;
            c.respawnToOriginalLocation();
        }
    }
}

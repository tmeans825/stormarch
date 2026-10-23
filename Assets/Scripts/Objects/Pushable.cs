using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

public class Pushable : MonoBehaviour
{
    public ParticleSystem pushDustPS;

    [ES3Serializable]
    protected Vector3 finalLoc = new Vector3();

    protected Rigidbody2D thisRB;
    protected AudioManager am;

    private BoxCollider2D coll;

    // Captured at Awake for respawn-to-origin. Non-serialized: this is the prefab/scene-editor
    // default, re-derived each session, not save data. Inactive-at-scene-start objects don't
    // run Awake until activated, so spawnAnchorRecorded stays false and respawn skips them
    // (avoids teleporting story-disabled pushables to (0,0,0)).
    [System.NonSerialized]
    protected Vector3 originalSpawnPosition;
    [System.NonSerialized]
    protected bool spawnAnchorRecorded = false;

    protected virtual void Awake()
    {
        coll = GetComponent<BoxCollider2D>();
        thisRB = GetComponent<Rigidbody2D>();
        am = GetComponent<AudioManager>();

        originalSpawnPosition = transform.position;
        spawnAnchorRecorded = true;
    }

    private void Start()
    {
        GameEvents.current.onPreviousSceneDataLoaded += previousSceneDataLoaded;
        finalLoc = transform.position;
    }

    private void Update()
    {
        if (thisRB == null)
            return;

        if ((thisRB.velocity.magnitude >= 1f) && (!pushDustPS.isPlaying))
        {
            pushDustPS.Play();
            am.PlaySound("Push Loop");          
        }
        else if ((thisRB.velocity.magnitude < 1f) && (pushDustPS.isPlaying))
        {
            pushDustPS.Stop();
            am.StopSound("Push Loop");
            finalLoc = transform.position;
            AstarPath.active.UpdateGraphs(coll.bounds);
        }
        else if ((thisRB.velocity.magnitude >= 1f) && (pushDustPS.isPlaying)) // if it's actively being moved
        {
            AstarPath.active.UpdateGraphs(coll.bounds);
        }
    }

    protected virtual void previousSceneDataLoaded()
    {      
        if (finalLoc != transform.position)
            transform.position = finalLoc;
    }

    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "PressurePlate")
        {
            PressurePlate pp = collision.gameObject.transform.GetComponentInParent<PressurePlate>();
            pp.activate(gameObject);
        }
    }

    protected virtual void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "PressurePlate")
        {
            PressurePlate pp = collision.gameObject.transform.GetComponentInParent<PressurePlate>();
            pp.deActivate();
        }
    }

    private void OnDestroy()
    {
        GameEvents.current.onPreviousSceneDataLoaded -= previousSceneDataLoaded;
    }

    // Snap this pushable back to where it was authored in the scene. Resets finalLoc too
    // so the next save persists the original position and previousSceneDataLoaded won't
    // re-apply a stale finalLoc on the upcoming scene reload.
    public virtual void respawnToOriginalLocation()
    {
        if (!spawnAnchorRecorded) return;

        transform.position = originalSpawnPosition;
        finalLoc = originalSpawnPosition;

        if (thisRB != null)
        {
            thisRB.velocity = Vector2.zero;
            thisRB.angularVelocity = 0f;
        }
    }

    // Static iteration for the death/camp respawn flow. Only touches ACTIVE pushables,
    // protecting story-disabled inactive ones (their Awake may not have run, and we don't
    // want to teleport them to (0,0,0)). Subclasses with their own inactive-broken state
    // (e.g. WoodenCrateBehavior) handle reactivation in their own statics.
    public static void respawnAllPushedInCurrentScene()
    {
        var all = GameObject.FindObjectsOfType<Pushable>(false);
        for (int i = 0; i < all.Length; i++)
        {
            Pushable p = all[i];
            if (p == null) continue;
            if (!p.spawnAnchorRecorded) continue;
            p.respawnToOriginalLocation();
        }
    }
}

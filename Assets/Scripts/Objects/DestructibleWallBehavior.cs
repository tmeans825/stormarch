using System.Collections;
using UnityEngine;
using Com.LuisPedroFonseca.ProCamera2D;
using Pathfinding;

public class DestructibleWallBehavior : MonoBehaviour
{
    [Header("Visuals")]
    public SpriteRenderer mainSprite;
    public GameObject debrisGrouper;

    [Header("Debris Physics")]
    public float breakForce = 8f;
    public float breakRadius = 5f;

    [Header("SFX")]
    public string[] hitFX = new string[] { "Crate Hit 1", "Crate Hit 2" };
    public string[] impactFX = new string[] { "Impact 1", "Impact 2", "Impact 3" };

    const string SMALL_SHAKE = "Small Shake";

    [ES3Serializable]
    [System.NonSerialized]
    private bool isBroken = false;

    private BoxCollider2D[] colliders;
    private AudioManager am;

    private void Awake()
    {
        colliders = GetComponents<BoxCollider2D>();
        am = GetComponent<AudioManager>();
    }

    private void Start()
    {
        GameEvents.current.onPreviousSceneDataLoaded += previousSceneDataLoaded;
    }

    private void OnDestroy()
    {
        GameEvents.current.onPreviousSceneDataLoaded -= previousSceneDataLoaded;
    }

    private void previousSceneDataLoaded()
    {
        if (isBroken)
        {
            mainSprite.enabled = false;

            foreach (BoxCollider2D bc in colliders)
            {
                AstarPath.active.UpdateGraphs(bc.bounds);
                bc.enabled = false;
            }

            debrisGrouper.SetActive(false);
        }
    }

    public void destroyWall(float impactMultiplier, Vector3 impactPos)
    {
        if (isBroken)
            return;

        ProCamera2DShake.Instance.Shake(SMALL_SHAKE);
        EventManager.instance.hitFreeze();
        isBroken = true;

        mainSprite.enabled = false;

        // cache bounds before disabling colliders so A* gets the correct area
        Bounds[] cachedBounds = new Bounds[colliders.Length];
        for (int i = 0; i < colliders.Length; i++)
            cachedBounds[i] = colliders[i].bounds;

        foreach (BoxCollider2D bc in colliders)
            bc.enabled = false;

        for (int i = 0; i < cachedBounds.Length; i++)
            AstarPath.active.UpdateGraphs(cachedBounds[i]);

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

        StartCoroutine(debrisTimer());
    }

    private IEnumerator debrisTimer()
    {
        yield return new WaitForSeconds(3f);

        foreach (BoxCollider2D coll in debrisGrouper.GetComponentsInChildren<BoxCollider2D>())
            coll.enabled = false;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * A storm drain is open or shut purely as a function of the live tempest state and this
 * drain's own authored baseline - never as a blind toggle. The authored blocker state is
 * the calm-weather baseline; a tempest inverts it, so drains authored shut open up and
 * drains authored open seal over. Driving it as state (rather than flipping on each event)
 * means a stray or replayed infestation event can't leave the drain backwards.
 */

public class StormDrainBehavior : MonoBehaviour
{
    public GameObject blockerObj;
    public Animator animator;
    public GameObject lightGrouper;

    const float TELEGRAPH_SECONDS = 5f;
    const string DRAIN_SOUND = "Drain";
    const string OPEN_STATE = "Drain_Open";
    const string CLOSE_STATE = "Drain_Close";

    private AudioManager am;

    // Captured in Awake so it's the authored value - the first state application can't be
    // mistaken for the baseline.
    private bool baselineBlocked;
    private bool targetBlocked;     // the state the drain is in, or is telegraphing toward
    private bool appliedBlocked;    // the pose the drain is physically in right now
    private Bounds blockerBounds;
    private Coroutine transitionRoutine;

    // Cache the dispatcher we subscribed to so OnDisable unbinds the same instance even if
    // GameEvents.current is replaced/nulled during teardown (mirrors GameEventListener).
    private GameEvents subscribedEvents;

    void Awake()
    {
        am = GetComponent<AudioManager>();
        baselineBlocked = blockerObj.activeSelf;
        targetBlocked = baselineBlocked;
        appliedBlocked = baselineBlocked;
        cacheBlockerBounds();
    }

    void OnEnable()
    {
        trySubscribe();
    }

    void Start()
    {
        // Fallback: if we enabled before GameEvents.Awake ran, retry once now.
        if (subscribedEvents == null)
            trySubscribe();

        // Stage the authored pose. A shut drain already rests in the controller's default
        // Drain_Idle state, so only an authored-open drain needs posing. isTempest is
        // restored from the save after every Start runs, so the baseline is all we can know
        // here - syncToTempestState has the last word once the transition completes.
        if (!baselineBlocked)
            applyBlocked(baselineBlocked, true);
    }

    void OnDisable()
    {
        if (subscribedEvents == null)
            return;
        subscribedEvents.beginInfestation -= onTempestBegin;
        subscribedEvents.endInfestation -= onTempestEnd;
        subscribedEvents.onSceneTransitionComplete -= syncToTempestState;
        subscribedEvents = null;
    }

    private void trySubscribe()
    {
        if (subscribedEvents != null) return; // already bound
        GameEvents ge = GameEvents.current;
        if (ge == null) return;
        subscribedEvents = ge;
        ge.beginInfestation += onTempestBegin;
        ge.endInfestation += onTempestEnd;
        ge.onSceneTransitionComplete += syncToTempestState;
    }

    // beginInfestation/endInfestation fire before EventManager flips isTempest, so these
    // handlers must take the state from the event itself, not read it back.
    private void onTempestBegin()
    {
        requestBlocked(blockedFor(true));
    }

    private void onTempestEnd()
    {
        requestBlocked(blockedFor(false));
    }

    // Forced sync from live tempest state. Runs after loadInEvents has replayed
    // beginInfestation for a tempest carried across a scene load, so it cancels that
    // telegraph and restores the drain silently instead of announcing it again.
    private void syncToTempestState()
    {
        if (EventManager.instance == null)
            return;

        bool blocked = blockedFor(EventManager.instance.isTempestActive());
        if (blocked == targetBlocked && transitionRoutine == null)
            return;

        applyBlocked(blocked, true);
    }

    // The authored state is the calm baseline; a tempest inverts this drain.
    private bool blockedFor(bool tempestActive)
    {
        return tempestActive ? !baselineBlocked : baselineBlocked;
    }

    // Live transition: warning lights, then the drain moves. Repeat/stray fires for a state
    // we're already in (or already heading to) are ignored; a reversal that cancels a
    // telegraph before the drain moved is dropped silently inside applyBlocked.
    private void requestBlocked(bool blocked)
    {
        if (blocked == targetBlocked)
            return;

        applyBlocked(blocked, false);
    }

    private void applyBlocked(bool blocked, bool instant)
    {
        targetBlocked = blocked;

        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        if (!instant)
        {
            // A reversal that lands back on the pose the drain is already in - the tempest
            // ended inside the telegraph, before the blocker ever moved. Drop it silently:
            // running a second telegraph would burn another TELEGRAPH_SECONDS on the warning
            // lights and then replay the slide and the drain sound for a drain that never
            // left this pose.
            if (blocked == appliedBlocked)
            {
                lightGrouper.SetActive(false);
                return;
            }

            transitionRoutine = StartCoroutine(telegraphThenSet(blocked));
            return;
        }

        lightGrouper.SetActive(false);
        setBlocked(blocked, true);
    }

    private IEnumerator telegraphThenSet(bool blocked)
    {
        lightGrouper.SetActive(true);
        yield return new WaitForSeconds(TELEGRAPH_SECONDS);
        setBlocked(blocked, false);
        lightGrouper.SetActive(false);
        transitionRoutine = null;
    }

    private void setBlocked(bool blocked, bool instant)
    {
        bool colliderMoved = blockerObj.activeSelf != blocked;
        blockerObj.SetActive(blocked);
        appliedBlocked = blocked;

        // Re-scan the blocked region in BOTH directions - shutting the drain without this
        // leaves the pathfinding graph walkable straight through it. Only on an actual
        // toggle: the baked graph already matches the authored state, so the settle-on-load
        // pass has nothing to correct.
        if (colliderMoved && AstarPath.active != null)
            AstarPath.active.UpdateGraphs(blockerBounds);

        // normalizedTime 1 on a restore so the drain shows its finished pose instead of
        // replaying the slide on every scene load.
        animator.Play(blocked ? CLOSE_STATE : OPEN_STATE, 0, instant ? 1f : 0f);

        if (!instant && am != null)
            am.PlaySound(DRAIN_SOUND);
    }

    // Collider2D.bounds is unreliable while its GameObject is inactive, and the blocker is
    // switched off whenever the drain is open - derive the region from the box geometry
    // instead. Drains don't move, so once is enough.
    private void cacheBlockerBounds()
    {
        BoxCollider2D box = blockerObj.GetComponent<BoxCollider2D>();
        if (box == null)
        {
            blockerBounds = new Bounds(blockerObj.transform.position, Vector3.one);
            return;
        }

        Vector3 scale = blockerObj.transform.lossyScale;
        Vector3 center = blockerObj.transform.TransformPoint(box.offset);
        Vector3 size = new Vector3(Mathf.Abs(box.size.x * scale.x), Mathf.Abs(box.size.y * scale.y), 1f);
        blockerBounds = new Bounds(center, size);
    }
}

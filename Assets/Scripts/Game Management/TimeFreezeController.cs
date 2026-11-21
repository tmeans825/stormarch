using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Scripted "time stops, player gets one aimed shot" moment. Drops Time.timeScale to near zero, lets the
/// player aim (aim input and the reticle run on real frames) and fire, then restores the time scale the
/// moment the player's bullet is created. Ends on its own after a real-time timeout so the player can
/// never be stuck frozen.
///
/// Restores the PRIOR timeScale/fixedDeltaTime rather than assuming 1, since hit-freeze, the death
/// slowdown and charm slow-mo also write the global time scale.
/// </summary>
public class TimeFreezeController : MonoBehaviour
{
    [System.NonSerialized]
    public static TimeFreezeController instance;

    [Tooltip("Time.timeScale while frozen. Not 0, so animation/audio/particles still creep and the bullet can be created.")]
    public float frozenTimeScale = 0.05f;

    [System.NonSerialized]
    public bool isFrozen = false;

    // Fires once when a freeze ends. True if the player fired, false on timeout/death/cancel.
    public event Action<bool> onFreezeEnded;

    private float savedTimeScale = 1f;
    private float savedFixedDeltaTime = 0.02f;
    private Coroutine timeoutRoutine;

    void Awake()
    {
        if (instance == null && this != instance)
            instance = this;
        else
            Destroy(gameObject);
    }

    private bool subscribed = false;

    void OnEnable()
    {
        subscribe();
    }

    // GameEvents.current may not exist yet during scene load (Awake order isn't guaranteed), so retry in Start.
    void Start()
    {
        subscribe();
    }

    private void subscribe()
    {
        if (subscribed || GameEvents.current == null)
            return;

        GameEvents.current.onBulletCreate += handleBulletCreated;
        GameEvents.current.onPlayerDeath += handlePlayerDeath;
        GameEvents.current.onReturnToTitle += handleReturnToTitle;
        subscribed = true;
    }

    void OnDisable()
    {
        // Never leave the game frozen if this object is disabled or the scene unloads mid-freeze.
        // Restore time first so a teardown-order problem with GameEvents can't skip it.
        endFreeze(false);

        if (subscribed && GameEvents.current != null)
        {
            GameEvents.current.onBulletCreate -= handleBulletCreated;
            GameEvents.current.onPlayerDeath -= handlePlayerDeath;
            GameEvents.current.onReturnToTitle -= handleReturnToTitle;
        }
        subscribed = false;
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    public void freeze(float realtimeTimeout)
    {
        if (isFrozen)
            return;

        isFrozen = true;
        savedTimeScale = Time.timeScale;
        savedFixedDeltaTime = Time.fixedDeltaTime;

        Time.timeScale = frozenTimeScale;
        Time.fixedDeltaTime = savedFixedDeltaTime * (frozenTimeScale / Mathf.Max(savedTimeScale, 0.0001f));

        // Fire cooldown is measured in scaled Time.time, so a shot taken just before the freeze would
        // otherwise be blocked for the entire freeze.
        if (PlayerController.instance != null)
            PlayerController.instance.nextFire = 0f;

        timeoutRoutine = StartCoroutine(timeoutAfter(realtimeTimeout));
    }

    // Public so a beat can cancel the freeze (e.g. the player died or the scene is ending).
    public void endFreeze(bool playerFired)
    {
        if (!isFrozen)
            return; // idempotent: bullet, timeout, death and disable can all race to end it

        isFrozen = false;

        if (timeoutRoutine != null)
        {
            StopCoroutine(timeoutRoutine);
            timeoutRoutine = null;
        }

        Time.timeScale = savedTimeScale;
        Time.fixedDeltaTime = savedFixedDeltaTime;

        onFreezeEnded?.Invoke(playerFired);
    }

    private IEnumerator timeoutAfter(float realtimeSeconds)
    {
        yield return new WaitForSecondsRealtime(realtimeSeconds);
        timeoutRoutine = null; // finished by itself, nothing to stop
        endFreeze(false);
    }

    private void handleBulletCreated(ProjectileScript bullet)
    {
        endFreeze(true);
    }

    private void handlePlayerDeath()
    {
        endFreeze(false);
    }

    private void handleReturnToTitle()
    {
        endFreeze(false);
    }
}

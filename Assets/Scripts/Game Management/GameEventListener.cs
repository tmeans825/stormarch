using UnityEngine;
using UnityEngine.Events;

/*
 * Reusable inspector-driven listener for parameterless GameEvents.
 * Pick an EventKind, wire a UnityEvent response - no per-use-site code.
 */

public class GameEventListener : MonoBehaviour
{
    public enum EventKind
    {
        BeginInfestation,
        EndInfestation,
        BeginCamping,
        OnPlayerHeal,
        OnAggro,
        OnUnaggro,
        OnReturnToTitle,
        OnNoteRecordingPickup,
        OnPreviousSceneDataLoaded,
        OnSceneTransitionComplete,
        OnSaveStarted,
        OnAutosaveIndicatorRequested,
        OnMenuClosed,
        OnEnemyKilled,
        OnRechargeStart,
        OnShieldsBroken,
        OnSlideStart,
    }

    [Tooltip("Which GameEvents event to listen for. Note: EndInfestation is not idempotent - " +
             "it only fires meaningfully when a tempest is active. Prefer wiring to BeginInfestation " +
             "and tracking state on the response side, or trigger via tryEndInfestation upstream.")]
    [SerializeField] private EventKind eventKind = EventKind.BeginInfestation;

    [SerializeField] private UnityEvent response = new UnityEvent();

    // Cache the dispatcher we subscribed to so OnDisable unbinds the same instance,
    // even if GameEvents.current has been replaced or nulled during scene teardown.
    private GameEvents subscribedEvents;

    void OnEnable()
    {
        trySubscribe();
    }

    void Start()
    {
        // Fallback: if we enabled before GameEvents.Awake, retry once.
        if (subscribedEvents == null)
            trySubscribe();
    }

    void OnDisable()
    {
        if (subscribedEvents == null)
            return;
        bind(subscribedEvents, false);
        subscribedEvents = null;
    }

    private void trySubscribe()
    {
        GameEvents ge = GameEvents.current;
        if (ge == null)
            return;
        subscribedEvents = ge;
        bind(ge, true);
    }

    private void handleEvent()
    {
        response?.Invoke();
    }

    private void bind(GameEvents ge, bool subscribe)
    {
        switch (eventKind)
        {
            case EventKind.BeginInfestation:
                if (subscribe) ge.beginInfestation += handleEvent; else ge.beginInfestation -= handleEvent; break;
            case EventKind.EndInfestation:
                if (subscribe) ge.endInfestation += handleEvent; else ge.endInfestation -= handleEvent; break;
            case EventKind.BeginCamping:
                if (subscribe) ge.beginCamping += handleEvent; else ge.beginCamping -= handleEvent; break;
            case EventKind.OnPlayerHeal:
                if (subscribe) ge.onPlayerHeal += handleEvent; else ge.onPlayerHeal -= handleEvent; break;
            case EventKind.OnAggro:
                if (subscribe) ge.onAggro += handleEvent; else ge.onAggro -= handleEvent; break;
            case EventKind.OnUnaggro:
                if (subscribe) ge.onUnaggro += handleEvent; else ge.onUnaggro -= handleEvent; break;
            case EventKind.OnReturnToTitle:
                if (subscribe) ge.onReturnToTitle += handleEvent; else ge.onReturnToTitle -= handleEvent; break;
            case EventKind.OnNoteRecordingPickup:
                if (subscribe) ge.onNoteRecordingPickup += handleEvent; else ge.onNoteRecordingPickup -= handleEvent; break;
            case EventKind.OnPreviousSceneDataLoaded:
                if (subscribe) ge.onPreviousSceneDataLoaded += handleEvent; else ge.onPreviousSceneDataLoaded -= handleEvent; break;
            case EventKind.OnSceneTransitionComplete:
                if (subscribe) ge.onSceneTransitionComplete += handleEvent; else ge.onSceneTransitionComplete -= handleEvent; break;
            case EventKind.OnSaveStarted:
                if (subscribe) ge.onSaveStarted += handleEvent; else ge.onSaveStarted -= handleEvent; break;
            case EventKind.OnAutosaveIndicatorRequested:
                if (subscribe) ge.onAutosaveIndicatorRequested += handleEvent; else ge.onAutosaveIndicatorRequested -= handleEvent; break;
            case EventKind.OnMenuClosed:
                if (subscribe) ge.onMenuClosed += handleEvent; else ge.onMenuClosed -= handleEvent; break;
            case EventKind.OnEnemyKilled:
                if (subscribe) ge.onEnemyKilled += handleEvent; else ge.onEnemyKilled -= handleEvent; break;
            case EventKind.OnRechargeStart:
                if (subscribe) ge.onRechargeStart += handleEvent; else ge.onRechargeStart -= handleEvent; break;
            case EventKind.OnShieldsBroken:
                if (subscribe) ge.onShieldsBroken += handleEvent; else ge.onShieldsBroken -= handleEvent; break;
            case EventKind.OnSlideStart:
                if (subscribe) ge.onSlideStart += handleEvent; else ge.onSlideStart -= handleEvent; break;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Swaps every Kipling representation (shoulder + deployed) to an unlit material
 * with a warm tint while a tempest is active, so Kipling stays visible in the
 * storm darkness. Lives on the always-active Player root because the deployed
 * Kipling deactivates itself in Awake; renderers are gathered with
 * includeInactive so a mid-tempest deploy appears already tinted.
 *
 * Stand-in sprites that replace Kipling but don't live under the player - the
 * vent run-through animation, which sits on the Vent prefab - hand themselves
 * over with registerStandInRenderer for as long as they are on screen.
 */

public class KiplingTempestVisuals : MonoBehaviour
{
    // Per-renderer bookkeeping, kept in a list rather than parallel arrays so
    // stand-ins can join and leave at any point, including partway through a fade.
    private class TrackedRenderer
    {
        public SpriteRenderer renderer;
        public Material originalMaterial;
        public Color originalColor;
        public Color fadeStartColor;
    }

    [SerializeField] private Material unlitMaterial;
    [SerializeField] private Color tempestTint = new Color(1f, 0.78f, 0.55f, 1f);
    // Mirrors LightWeatherManager.setGlobalLight's default fadeDuration (5f) so the
    // tint eases in/out in step with the storm darkening — retune both together.
    [SerializeField] private float fadeDuration = 5f;
    [SerializeField] private GameObject shoulderKipling;
    [SerializeField] private GameObject mobileKipling;

    private List<TrackedRenderer> trackedRenderers = new List<TrackedRenderer>();
    private bool visualsApplied = false;
    private Coroutine tintFadeRoutine;

    // Shape of the in-flight fade, so a stand-in that registers midway can be put on
    // the same curve without reading it back off another renderer.
    private float fadeElapsed = 0f;
    private bool fadeTowardTempest = false;

    // Cache the dispatcher we subscribed to so OnDisable unbinds the same instance,
    // even if GameEvents.current has been replaced or nulled during scene teardown.
    private GameEvents subscribedEvents;

    void Awake()
    {
        foreach (SpriteRenderer renderer in shoulderKipling.GetComponentsInChildren<SpriteRenderer>(true))
            trackRenderer(renderer);

        foreach (SpriteRenderer renderer in mobileKipling.GetComponentsInChildren<SpriteRenderer>(true))
            trackRenderer(renderer);
    }

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
        subscribedEvents.beginInfestation -= onTempestBegin;
        subscribedEvents.endInfestation -= onTempestEnd;
        subscribedEvents.onSceneTransitionComplete -= syncToTempestState;
        subscribedEvents = null;
    }

    private void trySubscribe()
    {
        GameEvents ge = GameEvents.current;
        if (ge == null)
            return;
        subscribedEvents = ge;
        ge.beginInfestation += onTempestBegin;
        ge.endInfestation += onTempestEnd;
        ge.onSceneTransitionComplete += syncToTempestState;

        syncToTempestState(); // catch a tempest that began before we subscribed
    }

    private TrackedRenderer trackRenderer(SpriteRenderer renderer)
    {
        TrackedRenderer tracked = new TrackedRenderer();
        tracked.renderer = renderer;
        tracked.originalMaterial = renderer.sharedMaterial;
        tracked.originalColor = renderer.color;
        tracked.fadeStartColor = renderer.color;

        trackedRenderers.Add(tracked);
        return tracked;
    }

    private TrackedRenderer findTracked(SpriteRenderer renderer)
    {
        for (int i = 0; i < trackedRenderers.Count; i++)
        {
            if (trackedRenderers[i].renderer == renderer)
                return trackedRenderers[i];
        }

        return null;
    }

    // The vent run-through hides Kipling and plays a stand-in sprite that lives on the
    // Vent prefab, so Awake can't reach it. Vents register it while it is on screen so
    // it gets the same treatment as the Kipling it stands in for.
    public void registerStandInRenderer(SpriteRenderer standIn)
    {
        if (standIn == null || findTracked(standIn) != null)
            return;

        TrackedRenderer tracked = trackRenderer(standIn);

        // fadeTint holds the unlit material for the whole of a fade-out, so a fade in
        // either direction still counts as the tempest look - not just visualsApplied.
        if (!visualsApplied && tintFadeRoutine == null)
            return;

        standIn.sharedMaterial = unlitMaterial;

        Color live = tempestTint;
        if (tintFadeRoutine != null)
        {
            // Rebuild the curve this renderer would have been on had it been present
            // when the fade started, so it lands in step with the rest of them.
            tracked.fadeStartColor = fadeTowardTempest ? tracked.originalColor : tempestTint;
            Color fadeTarget = fadeTowardTempest ? tempestTint : tracked.originalColor;
            live = Color.Lerp(tracked.fadeStartColor, fadeTarget, fadeProgress());
        }

        live.a = standIn.color.a;
        standIn.color = live;
    }

    public void unregisterStandInRenderer(SpriteRenderer standIn)
    {
        TrackedRenderer tracked = findTracked(standIn);
        if (tracked == null)
            return;

        trackedRenderers.Remove(tracked);

        if (tracked.renderer == null)
            return;

        // Hand it back lit and untinted: a later re-register captures whatever it finds,
        // so leaving the tint on would poison that renderer's originalColor.
        tracked.renderer.sharedMaterial = tracked.originalMaterial;
        Color restored = tracked.originalColor;
        restored.a = tracked.renderer.color.a;
        tracked.renderer.color = restored;
    }

    // beginInfestation/endInfestation fire before EventManager flips isTempest,
    // so these handlers must take the state from the event itself, not read it back.
    private void onTempestBegin()
    {
        applyTempestVisuals(true);
    }

    private void onTempestEnd()
    {
        applyTempestVisuals(false);
    }

    // Forced sync from live tempest state; clears stale visuals carried across
    // a scene load on the persisted player.
    private void syncToTempestState()
    {
        if (EventManager.instance == null)
            return;
        applyTempestVisuals(EventManager.instance.isTempestActive(), true);
    }

    private float fadeProgress()
    {
        if (fadeDuration <= 0f)
            return 1f;

        return fadeElapsed / fadeDuration;
    }

    private void applyTempestVisuals(bool active, bool force = false)
    {
        // endInfestation is not idempotent - never toggle blindly, only set state
        if (active == visualsApplied && !force)
            return;
        visualsApplied = active;

        if (tintFadeRoutine != null)
        {
            StopCoroutine(tintFadeRoutine);
            tintFadeRoutine = null;
        }

        if (force)
        {
            // Instant snap for cross-scene sync: material and color must be set
            // together, or interrupting a fade-out here would strand unlitMaterial.
            for (int i = 0; i < trackedRenderers.Count; i++)
            {
                TrackedRenderer tracked = trackedRenderers[i];
                if (tracked.renderer == null)
                    continue;

                tracked.renderer.sharedMaterial = active ? unlitMaterial : tracked.originalMaterial;
                tracked.renderer.color = active ? tempestTint : tracked.originalColor;
            }
            return;
        }

        tintFadeRoutine = StartCoroutine(fadeTint(active));
    }

    // Eases the tint over fadeDuration, linear to match LightWeatherManager's global
    // light fade (both start in the same frame from GameEvents.infestationBegin/End).
    // Starts from each renderer's live RGB so a mid-fade reversal continues from
    // where it left off instead of popping. Alpha is owned by the slide transparency
    // in Character, so every write preserves the renderer's current alpha.
    private IEnumerator fadeTint(bool active)
    {
        fadeTowardTempest = active;
        fadeElapsed = 0f;

        if (active)
        {
            // Swap to unlit immediately: unlit white under still-full light looks the
            // same as lit white, and the tint then fades in with the darkening.
            for (int i = 0; i < trackedRenderers.Count; i++)
            {
                if (trackedRenderers[i].renderer != null)
                    trackedRenderers[i].renderer.sharedMaterial = unlitMaterial;
            }
        }

        for (int i = 0; i < trackedRenderers.Count; i++)
        {
            if (trackedRenderers[i].renderer != null)
                trackedRenderers[i].fadeStartColor = trackedRenderers[i].renderer.color;
        }

        while (fadeElapsed < fadeDuration)
        {
            // Indexed and re-read every frame: stand-ins register and unregister
            // between resumptions, so the list can grow or shrink under us.
            for (int i = 0; i < trackedRenderers.Count; i++)
            {
                TrackedRenderer tracked = trackedRenderers[i];
                if (tracked.renderer == null)
                    continue;

                Color newColor = Color.Lerp(tracked.fadeStartColor, active ? tempestTint : tracked.originalColor, fadeProgress());
                newColor.a = tracked.renderer.color.a;
                tracked.renderer.color = newColor;
            }

            fadeElapsed += Time.deltaTime;
            yield return null;
        }

        for (int i = 0; i < trackedRenderers.Count; i++)
        {
            TrackedRenderer tracked = trackedRenderers[i];
            if (tracked.renderer == null)
                continue;

            Color finalColor = active ? tempestTint : tracked.originalColor;
            finalColor.a = tracked.renderer.color.a;
            tracked.renderer.color = finalColor;

            // Lit material only comes back once the tint has fully faded out.
            if (!active)
                tracked.renderer.sharedMaterial = tracked.originalMaterial;
        }

        tintFadeRoutine = null;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfestationBehavior_New : MonoBehaviour
{
    public ParticleSystem infestationParticles;
    public BoxCollider2D infestationTriggerCollider;

    const string SCRATCH_SOUND = "Scratch";
    AudioManager infestationAudio;   // same GameObject; carries the looping "Scratch" SFX
    Sound scratchSound;              // cached once in Start() to avoid per-event string lookups

    float borderThickness = 1.5f;
    float triggerInset = 1f;

    const float REFERENCE_AREA = 250f;
    const float REFERENCE_RATE = 25f;

    ParticleSystem[] killTargetSystems;  // main system + sub-emitters (e.g. the burrow puff)
    Renderer[] killTargetRenderers;      // per-system live bounds, for cheap light prefiltering
    readonly List<Collider2D> overlappingLights = new List<Collider2D>();
    ParticleSystem.Particle[] particleBuffer;
    bool isActive;

    // Cache the dispatcher we subscribed to so OnDisable unbinds the same instance even if
    // GameEvents.current is replaced/nulled during teardown (mirrors GameEventListener).
    GameEvents subscribedEvents;

    void Start()
    {
        // include-inactive so the burrow-puff sub-emitter is captured even while deactivated
        killTargetSystems = infestationParticles.GetComponentsInChildren<ParticleSystem>(true);
        killTargetRenderers = new Renderer[killTargetSystems.Length];

        for (int i = 0; i < killTargetSystems.Length; i++)
        {
            killTargetRenderers[i] = killTargetSystems[i].GetComponent<Renderer>();

            // children are re-SetActive on every activation; the burrow puff has playOnAwake, so it
            // would emit a puff on enable. Emission is driven explicitly (main via Play(), puff via
            // sub-emitter), so kill playOnAwake to avoid an unculled activation-frame burst.
            ParticleSystem.MainModule main = killTargetSystems[i].main;
            main.playOnAwake = false;
        }

        infestationAudio = GetComponent<AudioManager>();
        if (infestationAudio != null)
            scratchSound = infestationAudio.getSound(SCRATCH_SOUND);

        sizeColliderToParticles();
        alignBorderColliders();
        scaleEmissionToArea();
        sizeAudioToShape();

        deactivateInfestation();

        // Fallback: if we enabled before GameEvents.Awake ran, retry once now.
        if (subscribedEvents == null)
            trySubscribe();
    }

    void OnEnable()
    {
        trySubscribe();
    }

    void OnDisable()
    {
        if (subscribedEvents == null) return;
        subscribedEvents.beginInfestation -= activateInfestation;
        subscribedEvents.endInfestation -= deactivateInfestation;
        subscribedEvents = null;
    }

    void trySubscribe()
    {
        if (subscribedEvents != null) return; // already bound
        GameEvents ge = GameEvents.current;
        if (ge == null) return;
        subscribedEvents = ge;
        ge.beginInfestation += activateInfestation;
        ge.endInfestation += deactivateInfestation;
    }

    void activateInfestation()
    {
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(true);
        }

        isActive = true;
        infestationParticles.Play();

        // guard against repeated beginInfestation events restarting the loop from the top
        if (scratchSound != null && scratchSound.source != null && !scratchSound.source.isPlaying)
            infestationAudio.PlaySound(SCRATCH_SOUND);
    }

    void deactivateInfestation()
    {
        isActive = false;
        infestationParticles.Stop();

        if (scratchSound != null)
            infestationAudio.StopSound(SCRATCH_SOUND);

        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
        }
    }

    // The ParticleSystem Trigger module doesn't reliably cull against 2D colliders (Unity 2020.1), so
    // we cull manually: any live particle whose world position is inside a lit light collider dies.
    // OverlapPoint respects the exact collider shape (circle/cone/freeform), and reading the registry
    // each frame (filtered by isActiveAndEnabled) means lights toggled on/off at runtime — e.g.
    // Puzzle_Floodlight — are handled automatically.
    void LateUpdate()
    {
        if (!isActive || killTargetSystems == null) return;

        // a light may have been SetActive(toggled) this frame; sync so collider bounds/shapes are current
        Physics2D.SyncTransforms();

        for (int i = 0; i < killTargetSystems.Length; i++)
            cullParticlesInLight(killTargetSystems[i], killTargetRenderers[i]);
    }

    void cullParticlesInLight(ParticleSystem ps, Renderer rend)
    {
        if (ps == null) return;

        // prefilter to lights overlapping this system's LIVE particle bounds — can't miss edge
        // particles (unlike the inset trigger collider) and skips distant lights cheaply
        overlappingLights.Clear();
        Bounds particleBounds = rend != null ? rend.bounds : default;
        IReadOnlyList<Collider2D> lights = Light2DAutoCollider.ActiveLightColliders;
        for (int i = 0; i < lights.Count; i++)
        {
            Collider2D col = lights[i];
            if (col == null || !col.isActiveAndEnabled) continue;
            if (rend != null && !col.bounds.Intersects(particleBounds)) continue;
            overlappingLights.Add(col);
        }
        if (overlappingLights.Count == 0) return;

        int max = ps.main.maxParticles;
        if (particleBuffer == null || particleBuffer.Length < max)
            particleBuffer = new ParticleSystem.Particle[max];

        int count = ps.GetParticles(particleBuffer);
        if (count == 0) return;

        // particle positions are reported in the system's simulation space; resolve to world
        ParticleSystemSimulationSpace space = ps.main.simulationSpace;
        bool needsTransform = space != ParticleSystemSimulationSpace.World;
        Matrix4x4 toWorld = ps.transform.localToWorldMatrix;
        if (space == ParticleSystemSimulationSpace.Custom)
        {
            Transform custom = ps.main.customSimulationSpace;
            if (custom != null) toWorld = custom.localToWorldMatrix; // else fall back to local→world above
        }

        bool changed = false;
        for (int i = 0; i < count; i++)
        {
            Vector3 wp = needsTransform ? toWorld.MultiplyPoint3x4(particleBuffer[i].position) : particleBuffer[i].position;
            Vector2 p = new Vector2(wp.x, wp.y);

            for (int c = 0; c < overlappingLights.Count; c++)
            {
                Collider2D col = overlappingLights[c];
                Bounds b = col.bounds;
                if (p.x < b.min.x || p.x > b.max.x || p.y < b.min.y || p.y > b.max.y) continue; // cheap AABB reject
                if (!col.OverlapPoint(p)) continue;

                ParticleSystem.Particle particle = particleBuffer[i];
                particle.remainingLifetime = 0f;
                particleBuffer[i] = particle;
                changed = true;
                break;
            }
        }

        if (changed) ps.SetParticles(particleBuffer, count);
    }

    void sizeColliderToParticles()
    {
        var shape = infestationParticles.shape;

        infestationTriggerCollider.size = new Vector2(shape.scale.y - triggerInset * 2f, shape.scale.x - triggerInset * 2f);
        infestationTriggerCollider.offset = shape.position;
    }

    // Size the "Scratch" AudioSource falloff to the infestation footprint: full volume out to the
    // farthest corner of the shape, then fading over the next 20 units. The AudioSource sits at the
    // GameObject origin, so include shape.position so the offset shape is still fully covered.
    void sizeAudioToShape()
    {
        if (scratchSound == null) return;

        var shape = infestationParticles.shape;
        Vector3 scale = shape.scale;
        Vector2 offset = (Vector2)shape.position;
        float halfW = scale.y * 0.5f;
        float halfH = scale.x * 0.5f;
        float minDist = new Vector2(Mathf.Abs(offset.x) + halfW, Mathf.Abs(offset.y) + halfH).magnitude;
        float maxDist = minDist + 20f;

        scratchSound.minDistance = minDist;
        scratchSound.maxDistance = maxDist;
        if (scratchSound.source != null) // source set in AudioManager.Awake, which ran before Start
        {
            scratchSound.source.minDistance = minDist;
            scratchSound.source.maxDistance = maxDist;
        }
    }

    void scaleEmissionToArea()
    {
        var shape = infestationParticles.shape;
        float area = shape.scale.x * shape.scale.y;
        float density = REFERENCE_RATE / REFERENCE_AREA;

        var emission = infestationParticles.emission;
        emission.rateOverTime = density * area;
    }

    void alignBorderColliders()
    {
        BoxCollider2D[] allColliders = GetComponentsInChildren<BoxCollider2D>();
        List<BoxCollider2D> borders = new List<BoxCollider2D>();
        foreach (BoxCollider2D col in allColliders)
        {
            if (col != infestationTriggerCollider)
                borders.Add(col);
        }
        if (borders.Count < 4) return;

        var shape = infestationParticles.shape;
        Vector2 size = new Vector2(shape.scale.y, shape.scale.x);
        Vector2 offset = (Vector2)shape.position;
        float halfW = size.x / 2f;
        float halfH = size.y / 2f;
        float halfT = borderThickness / 2f;

        // top, right, bottom, left — placed flush against the outer edge, no overlap
        Vector2[] positions = new Vector2[]
        {
            offset + new Vector2(0, halfH + halfT),
            offset + new Vector2(halfW + halfT, 0),
            offset - new Vector2(0, halfH + halfT),
            offset - new Vector2(halfW + halfT, 0)
        };

        Vector2[] sizes = new Vector2[]
        {
            new Vector2(size.x, borderThickness),
            new Vector2(borderThickness, size.y),
            new Vector2(size.x, borderThickness),
            new Vector2(borderThickness, size.y)
        };

        for (int i = 0; i < 4; i++)
        {
            borders[i].transform.localPosition = new Vector3(positions[i].x, positions[i].y, 0);
            borders[i].size = sizes[i];
            borders[i].offset = Vector2.zero;
        }
    }
}

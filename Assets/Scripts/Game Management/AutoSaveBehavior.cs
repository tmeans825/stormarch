using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

public class AutoSaveBehavior : MonoBehaviour
{
    public GameObject saveTriggerObj;
    public GameObject lightObj;
    public GameObject childBurst;
    public bool isCamp = false;

    [ES3Serializable]
    [System.NonSerialized]
    public bool usedUp = false;

    private BoxCollider2D triggerCollider;
    private Light2D light2D;
    private ParticleSystem thisParticleSystem;

    private void Awake()
    {
        if (isCamp) // prevent automatically getting a checkpoint if logging out there
            StartCoroutine(temporarilyDisableSavePoint());
    }
    // Start is called before the first frame update
    void Start()
    {
        thisParticleSystem = GetComponent<ParticleSystem>();
        triggerCollider = saveTriggerObj.GetComponent<BoxCollider2D>();
        light2D = lightObj.GetComponent<Light2D>();

        // Apply collider bounds to the light and particle system so they "snap" to the same corners
        ApplyBoundsToVisuals();

        GameEvents.current.onAggro += preventSaving;
        GameEvents.current.onPlayerHurt += preventSaving;

        GameEvents.current.onUnaggro += tryEnableSaving;
        GameEvents.current.onPlayerHeal += tryEnableSaving;

        GameEvents.current.onPreviousSceneDataLoaded += previousSceneDataLoaded;
    }

    // Apply the BoxCollider2D size/offset to ParticleSystem shape and Light2D transform/radius
    private void ApplyBoundsToVisuals()
    {
        if (triggerCollider == null)
            return;

        // Collider size is in local space (to this GameObject)
        Vector2 size = triggerCollider.size;
        Vector2 offset = triggerCollider.offset;

        // Apply to ParticleSystem shape module if present
        if (thisParticleSystem != null)
        {
            var shape = thisParticleSystem.shape;

            // shape.scale is in local space of the particle system
            shape.scale = new Vector3(size.x, size.y, 1f);
            shape.position = new Vector3(offset.x, offset.y, 0f);
        }

        // Apply to light transform if present
        if (light2D != null)
        {
            // Position the light's local transform to the collider offset
            light2D.transform.localPosition = new Vector3(offset.x, offset.y, light2D.transform.localPosition.z);

            // Scale the light GameObject so its shape roughly matches the collider bounds.
            // This works well when the light uses a shape or sprite; for point lights we also set radius as fallback.
            light2D.transform.localScale = new Vector3(size.x, size.y, 1f);

            // Fallback: if Light2D exposes point radius properties, try to set outer radius to cover collider.
            // Not all Light2D variants rely on radius, but many do for point lights.
            // Use reflection to safely set common radius properties if they exist.
            /*
            float outerRadius = Mathf.Max(size.x, size.y) * 0.5f;
            var type = typeof(Light2D);
            var outerProp = type.GetProperty("pointLightOuterRadius");
            var innerProp = type.GetProperty("pointLightInnerRadius");
            if (outerProp != null && outerProp.CanWrite)
            {
                outerProp.SetValue(light2D, outerRadius, null);
            }
            // Optionally reduce inner radius a bit
            if (innerProp != null && innerProp.CanWrite)
            {
                innerProp.SetValue(light2D, Mathf.Max(0f, outerRadius * 0.5f), null);
            }*/
        }
    }

    private void previousSceneDataLoaded()
    {
        if (usedUp)
            gameObject.SetActive(false);

        else if(!isCamp) // reusable handled in awake
        {
            saveTriggerObj.SetActive(true);
            lightObj.SetActive(true);
        }
    }

    private void preventSaving(damageContext ctx)
    {
        // Only disable the checkpoint if the player is actually in combat —
        // aggro'd enemies nearby or currently bleeding. Environmental/self-inflicted
        // damage (e.g. walking into your own firebomb) should not remove checkpoints.
        if (PlayerController.instance.isBleeding || GameEvents.current.enemiesInCombat > 0)
            preventSaving();
    }

    private void preventSaving()
    {
        saveTriggerObj.SetActive(false);
        lightObj.SetActive(false);
    }

    private void tryEnableSaving()
    {
        if (saveTriggerObj.activeSelf)
            return;

        if (!PlayerController.instance.isBleeding && GameEvents.current.enemiesInCombat == 0)
        {
            saveTriggerObj.SetActive(true);
            lightObj.SetActive(true);
        }
    }

    public void saveCheckpoint()
    {
        if (!isCamp)
        {
            lightObj.SetActive(false);
            usedUp = true;
            StartCoroutine(turnOffParticlesAfterSec());

            GameEvents.current.onAggro -= preventSaving;
            GameEvents.current.onPlayerHurt -= preventSaving;

            GameEvents.current.onUnaggro -= tryEnableSaving;
            GameEvents.current.onPlayerHeal -= tryEnableSaving;
        }
        else
        {
            StartCoroutine(temporarilyDisableSavePoint());
        }

        childBurst.SetActive(true);

        childBurst.transform.position = PlayerController.instance.transform.position;
        AudioMaster.instance.PlaySound("Autosave");

        CanvasMaster.instance.showCheckpointText();

        if (isCamp)
        {
            SaveManager.instance.campSave();
        }
        else
        {
            SaveManager.instance.saveSceneData();
        }
    }

    private IEnumerator turnOffParticlesAfterSec()
    {
        yield return new WaitForSeconds(0.1f);
        thisParticleSystem.Stop();
    }

    private IEnumerator temporarilyDisableSavePoint()
    {
        preventSaving();

        yield return new WaitForSeconds(5f);

        while(Vector2.Distance(PlayerController.instance.transform.position, transform.position) < 3.0f)
        {
            yield return new WaitForSeconds(5f);
        }

        childBurst.SetActive(false);
        tryEnableSaving();
    }

    public void permanentlyDisableCheckpoint()
    {
        usedUp = true;
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        GameEvents.current.onAggro -= preventSaving;
        GameEvents.current.onPlayerHurt -= preventSaving;

        GameEvents.current.onUnaggro -= tryEnableSaving;
        GameEvents.current.onPlayerHeal -= tryEnableSaving;

        GameEvents.current.onPreviousSceneDataLoaded -= previousSceneDataLoaded;
    }
}

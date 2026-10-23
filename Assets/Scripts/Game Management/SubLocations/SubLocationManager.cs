using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Switches location text and ambient audio between regions of a single scene.
/// Outside every zone the scene's defaults apply (scene location name, AudioMaster's serialized
/// ambient, defaultAmbientBedName). Region is always derived from player position — never stored —
/// so loads, respawns and teleports can't leave it out of sync.
/// </summary>
public class SubLocationManager : MonoBehaviour
{
    [ES3NonSerializable]
    public SubLocationZone[] zones; // first match wins
    public string defaultAmbientBedName;
    [ES3NonSerializable]
    public Transform defaultArrivalPoint; // where fast travel to the scene's own location lands; must be outside every zone
    public float settleTime = 0.5f; // time in a new region before switching; prevents border flicker

    [System.NonSerialized]
    public static SubLocationManager instance;

    private string defaultLocationName;
    private string defaultAmbientSongName;

    private SubLocationZone currentZone;
    private SubLocationZone pendingZone;
    private float pendingSince;
    private bool resolved = false;
    private GameEvents subscribedEvents;

    void Awake()
    {
        if (instance == null && this != instance)
            instance = this;
        else
            Destroy(gameObject);
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

        captureDefaults();
    }

    void OnDisable()
    {
        if (subscribedEvents == null)
            return;

        subscribedEvents.onSceneTransitionComplete -= resolveImmediately;
        subscribedEvents = null;
    }

    private void trySubscribe()
    {
        if (subscribedEvents != null) return; // already bound
        GameEvents ge = GameEvents.current;
        if (ge == null) return;
        subscribedEvents = ge;
        ge.onSceneTransitionComplete += resolveImmediately;
    }

    void Update()
    {
        if (!resolved)
            return;

        SubLocationZone zone = findZone(PlayerController.instance.transform.position);
        if (zone == currentZone)
        {
            pendingZone = null;
            return;
        }

        if (zone != pendingZone)
        {
            pendingZone = zone;
            pendingSince = Time.time;
            return;
        }

        if (Time.time - pendingSince >= settleTime)
        {
            pendingZone = null;
            applyZone(zone, true);
        }
    }

    /// <summary>
    /// Snaps to the region the player is standing in without showing location text.
    /// </summary>
    public void resolveImmediately()
    {
        captureDefaults();
        pendingZone = null;
        applyZone(findZone(PlayerController.instance.transform.position), false);
        resolved = true;
    }

    /// <summary>
    /// Side-effect-free lookup of the location name at a point.
    /// </summary>
    public string getLocationNameAt(Vector2 point)
    {
        captureDefaults();
        SubLocationZone zone = findZone(point);
        return zone != null ? zone.locationName : defaultLocationName;
    }

    /// <summary>
    /// Fast-travel landing spot for a location in this scene, or null if none is set.
    /// Disabled zones are skipped, matching findZone — landing in one would report the scene default.
    /// </summary>
    public Transform getArrivalPoint(string locationName)
    {
        captureDefaults();
        if (locationName == defaultLocationName)
            return defaultArrivalPoint;

        foreach (SubLocationZone zone in zones)
        {
            if (zone != null && zone.isActiveAndEnabled && zone.locationName == locationName)
                return zone.arrivalPoint;
        }
        return null;
    }

#if UNITY_EDITOR
    // Catches arrivals that would land in the wrong region and immediately flip location text + ambience.
    void OnValidate()
    {
        if (zones == null)
            return;

        if (defaultArrivalPoint == null)
            Debug.LogWarning("SubLocationManager: defaultArrivalPoint isn't set.", this);

        foreach (SubLocationZone zone in zones)
        {
            if (zone == null || zone.GetComponent<Collider2D>() == null)
                continue;

            if (!SaveManager.isKnownLocation(zone.locationName))
                Debug.LogWarning("SubLocationManager: zone '" + zone.locationName + "' isn't a location in SaveManager's location tables.", zone);

            if (zone.arrivalPoint == null)
                Debug.LogWarning("SubLocationManager: zone '" + zone.locationName + "' has no arrivalPoint.", zone);
            else if (!zone.containsPoint(zone.arrivalPoint.position))
                Debug.LogWarning("SubLocationManager: zone '" + zone.locationName + "' arrivalPoint is outside its zone.", zone);

            if (defaultArrivalPoint != null && zone.containsPoint(defaultArrivalPoint.position))
                Debug.LogWarning("SubLocationManager: defaultArrivalPoint is inside zone '" + zone.locationName + "'.", this);
        }
    }
#endif

    // Helper Functions -----------------------------------------
    private void captureDefaults()
    {
        if (defaultLocationName != null)
            return;

        // Read before any zone has overwritten AudioMaster's ambient.
        defaultLocationName = SaveManager.instance.getLocationNameFromFilename(SceneManager.GetActiveScene().name);
        defaultAmbientSongName = AudioMaster.instance.sceneAmbientSongName;
    }

    private SubLocationZone findZone(Vector2 point)
    {
        foreach (SubLocationZone zone in zones)
        {
            if (zone != null && zone.isActiveAndEnabled && zone.containsPoint(point))
                return zone;
        }
        return null;
    }

    private void applyZone(SubLocationZone zone, bool announce)
    {
        currentZone = zone;

        AudioMaster.instance.setSceneAmbient(zone != null ? zone.ambientSongName : defaultAmbientSongName);
        AudioMaster.instance.setAmbientBed(zone != null ? zone.ambientBedName : defaultAmbientBedName);

        if (announce)
            LocationTextBehavior.instance.showLocation(zone != null ? zone.locationName : defaultLocationName);
    }
}

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks which fast-travel map locations the player has discovered. Keys are the same text table
/// location names used by LocationButton (e.g. "OLDTOWN"). Lives on the per-scene @GameMaster;
/// SaveManager writes the set to the slot file and merges it back in on load.
/// </summary>
public class LocationDiscoveryManager : MonoBehaviour
{
    public const string STARTING_LOCATION = "WRECK OF THE ZEPHYR"; // always discovered — the game starts here

    [System.NonSerialized]
    public static LocationDiscoveryManager instance;

    [System.NonSerialized]
    private HashSet<string> discovered = new HashSet<string>();

    void Awake()
    {
        if (instance == null && this != instance)
            instance = this;
        else
            Destroy(this);
    }

    public bool isDiscovered(string locationName)
    {
        if (locationName == STARTING_LOCATION || discovered.Contains(locationName))
            return true;

        return GameDevTestOverrides.instance != null && GameDevTestOverrides.instance.shouldForceAllLocationsDiscovered();
    }

    // Safe to call repeatedly (beat replays, re-entered triggers). Unmapped locations are still
    // recorded; their map button just stays hidden until the location has a scene to travel to.
    public void discoverLocation(string locationName)
    {
        if (string.IsNullOrEmpty(locationName) || discovered.Contains(locationName))
            return;

#if UNITY_EDITOR
        if (!SaveManager.isKnownLocation(locationName))
            Debug.LogWarning("discoverLocation: '" + locationName + "' isn't a location in SaveManager's location table.");
#endif
        discovered.Add(locationName);
    }

    public List<string> getDiscoveredLocations()
    {
        return new List<string>(discovered);
    }

    // Union, not replace: a discovery made this scene before loadSceneData runs must survive the load.
    public void mergeLoadedLocations(IEnumerable<string> locationNames)
    {
        if (locationNames == null)
            return;

        foreach (string locationName in locationNames)
        {
            if (!string.IsNullOrEmpty(locationName))
                discovered.Add(locationName);
        }
    }
}

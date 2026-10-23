using UnityEngine;

/// <summary>
/// A region inside a scene with its own location name and ambient audio (e.g. New Rhea inside Old Town).
/// Membership is tested against the player's position by SubLocationManager — this collider never needs
/// trigger callbacks, so keep it an untagged trigger on Ignore Raycast.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SubLocationZone : MonoBehaviour
{
    public string locationName;      // text table key, e.g. "NEW RHEA"
    public string ambientSongName;   // AudioMaster song, e.g. "Shadow Caster"
    public string ambientBedName;    // looping SFX under the music; "" = none
    [ES3NonSerializable]
    public Transform arrivalPoint;   // where fast travel to locationName lands; must be inside this zone

    [System.NonSerialized]
    public Collider2D zoneCollider;

    void Awake()
    {
        zoneCollider = GetComponent<Collider2D>();
    }

    public bool containsPoint(Vector2 point)
    {
        if (zoneCollider == null)
            zoneCollider = GetComponent<Collider2D>();

        return zoneCollider.OverlapPoint(point);
    }
}

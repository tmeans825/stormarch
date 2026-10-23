using UnityEngine;

// Marker for the single Deployed Camp in a scene. Camp Summon Rocks move this object to themselves
// when the player summons. The camp persists its own active-state and Transform via its ES3AutoSave
// component (saveActive + Transform in componentsToSave), so it self-restores on load regardless of
// when rocks are (re)enabled.
public class DeployedCamp : MonoBehaviour
{
    // A child placed at a walkable spot inside the camp (e.g. by the bedroll). Used as the
    // death-respawn point so the player never respawns inside the camp's scenery/collider bounds.
    // Not ES3-saved: DeployedCamp isn't listed in the ES3AutoSave componentsToSave (only Transform is),
    // and the attribute keeps it excluded even if someone later adds this component to that list.
    [ES3NonSerializable]
    public Transform respawnPoint;

    public Vector3 respawnPosition => respawnPoint != null ? respawnPoint.position : transform.position;

    // The camp may be inactive before the first summon, so include inactive objects in the search.
    public static DeployedCamp resolve()
    {
        return FindObjectOfType<DeployedCamp>(true);
    }
}

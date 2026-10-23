using UnityEngine;
using PixelCrushers.DialogueSystem;

// A rock the player interacts with to summon/move the single Deployed Camp to this spot.
// The rock the camp currently sits beside is the "active rock": its DialogueSystemTrigger is
// disabled so it can't re-summon onto itself. Only isActiveRock persists (via this prefab's
// ES3AutoSave); the camp persists its own position/active-state separately.
public class CampSummonRock : MonoBehaviour
{
    // The only persisted field. ES3 saves it because the prefab's ES3AutoSave lists CampSummonRock
    // in componentsToSave (see Crate.prefab / Pushable for the same pattern).
    [ES3Serializable]
    private bool isActiveRock = false;

    // Where the camp ORIGIN sits when summoned here. Scene reference — never saved.
    [ES3NonSerializable]
    public Transform campAnchor;

    [System.NonSerialized]
    private DialogueSystemTrigger dst;

    void Awake()
    {
        dst = GetComponent<DialogueSystemTrigger>();
    }

    void OnEnable()
    {
        GameEvents.current.onPreviousSceneDataLoaded += previousSceneDataLoaded;
        // Apply here (not just on the event) so a rock enabled late by a story beat still reflects
        // its restored isActiveRock.
        applyState();
    }

    void OnDisable()
    {
        GameEvents.current.onPreviousSceneDataLoaded -= previousSceneDataLoaded;
    }

    private void previousSceneDataLoaded()
    {
        applyState();
    }

    private void applyState()
    {
        if (dst != null)
            dst.enabled = !isActiveRock; // active rock can't be re-summoned to
    }

    private Vector3 anchorPos => campAnchor != null ? campAnchor.position : transform.position;

    // Called from the summonCamp() Lua function.
    public void summonCampHere()
    {
        // 1. Resolve the camp FIRST; abort before mutating anything if it's missing/misconfigured,
        //    so we never disable a trigger or autosave a broken "active rock with no camp" state.
        DeployedCamp camp = DeployedCamp.resolve();
        if (camp == null)
        {
            Debug.LogError("summonCampHere: no DeployedCamp in scene; aborting summon.", this);
            return;
        }

        // 2. Demote any other active rock (include inactive so a story-disabled old rock can't keep
        //    isActiveRock=true and re-enable itself inert later).
        foreach (CampSummonRock r in FindObjectsOfType<CampSummonRock>(true))
        {
            if (r != this && r.isActiveRock)
            {
                r.isActiveRock = false;
                r.applyState();
            }
        }

        // 3. Move (and reveal) the camp; its respawnPoint child moves with it.
        if (!camp.gameObject.activeSelf)
            camp.gameObject.SetActive(true);
        camp.transform.position = anchorPos;

        // 4. Become the active rock.
        isActiveRock = true;
        applyState(); // disables our own trigger

        // 5. Move the death-respawn point to the camp's respawn anchor. Mirror camp-rest ordering:
        //    drain any in-flight save FIRST so the direct ES3 write can't race it, then schedule the
        //    deferred full autosave at conversation end (project convention, cf. updateAlignment /
        //    recordOutcome -> flagImportant).
        SaveManager.instance.WaitForActiveSave();
        SaveManager.instance.recordCampPositionForScene(camp.respawnPosition);
        if (DialogueAutosaver.instance != null)
            DialogueAutosaver.instance.flagImportant();
    }
}

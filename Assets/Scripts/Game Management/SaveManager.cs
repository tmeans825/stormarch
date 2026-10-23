using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using PixelCrushers;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SaveManager : MonoBehaviour
{
    public string activeSlotName;

    [System.NonSerialized]
    public static SaveManager instance;
    [System.NonSerialized]
    public bool hasLoaded = false;

    [System.NonSerialized]
    [ES3Serializable]
    public int act = 1;

    private Task currentSaveTask;
    private Exception pendingSaveException;

    // Single authority for which map locations exist and can be travelled to.
    private static readonly Dictionary<string, string> SCENENAME_TO_LOCATIONNAME_DICT = new Dictionary<string, string>
    {
        { "Zephyr", "WRECK OF THE ZEPHYR" },
        { "Owens", "OWEN'S FARM" },
        { "DreamWorldKipling", "???" },
        { "OldTown", "OLDTOWN" },
        { "Wilds", "THE WILDS" }
    };

    // Map locations that share another location's scene. Each also needs a SubLocationZone with an
    // arrivalPoint in that scene; one-scene locations only need a row in the table above.
    private static readonly Dictionary<string, string> SUBLOCATION_TO_SCENENAME_DICT = new Dictionary<string, string>
    {
        { "NEW RHEA", "OldTown" }
    };

    // Set by LevelLoader.changeScene, consumed once by the next scene load.
    [System.NonSerialized]
    private string pendingArrivalLocation;

    void Awake()
    {
        if (instance == null && this != instance)
            instance = this;
        else
            Destroy(gameObject);

        DontDestroyOnLoad(this.gameObject);
    }

    public void saveSceneData()
    {
        saveSceneData(SceneManager.GetActiveScene().name, true);
    }

    public void saveSceneData(string sceneName)
    {
        saveSceneData(sceneName, true);
    }

    public void saveSceneDataWithoutAutosaveIndicator()
    {
        saveSceneData(SceneManager.GetActiveScene().name, false);
    }

    private void saveSceneData(string sceneName, bool requestAutosaveIndicator)
    {
        if (string.IsNullOrEmpty(activeSlotName))
        {
            Debug.LogWarning("No slot name specified, won't save.");
            return;
        }

        saveAllData(sceneName, requestAutosaveIndicator);
    }

    private void saveAllData(string sceneName, bool requestAutosaveIndicator)
    {
        // Make sure any prior background flush is done before we mutate ES3 state
        // or kick off a new save; otherwise overlapping writes can corrupt the file.
        WaitForActiveSave();

        string slotNameToUse = activeSlotName;
        string sceneSpecificFile = slotNameToUse + "_" + sceneName + ".es3";
        string slotFilePath = slotNameToUse + ".es3";

        GameEvents.current.saveStarted();
        if (requestAutosaveIndicator)
            GameEvents.current.autosaveIndicatorRequested();

        // Scene-level [ES3Serializable] components are still flushed synchronously
        // by ES3AutoSaveMgr (one File.WriteAllBytes). Backgrounding it would require
        // editing the plugin; the per-scene write is small relative to the slot file.
        ES3AutoSaveMgr.Current.settings.path = sceneSpecificFile;
        ES3AutoSaveMgr.Current.Save();

        // Stage the rest into RAM-cached ES3File instances (main thread, fast).
        // Sync() will be called from a background thread below to do the disk I/O.
        var sceneFile = new ES3File(sceneSpecificFile);
        sceneFile.Save("PlayerPosition", PlayerController.instance.transform.position);

        string dialogueData = SaveSystem.Serialize(SaveSystem.RecordSavedGameData());
        string dreamModifier = "";
        if (sceneName.Contains("DreamWorld")) // Dream world has scene-specific inventories
            dreamModifier = sceneName;

        var slotFile = new ES3File(slotFilePath);
        slotFile.Save("DialogueData", dialogueData);
        slotFile.Save("Player", PlayerController.instance);
        slotFile.Save("NotableOutcomes", NotableOutcomesTracker.instance);
        slotFile.Save("InventoryManager" + dreamModifier, InventoryManager.instance);
        slotFile.Save("RecordingsMenu", RecordingsMenu.instance);
        slotFile.Save("Scene", sceneName);
        slotFile.Save("WorldLocation", sceneName == SceneManager.GetActiveScene().name
            ? getCurrentLocationName()
            : getLocationNameFromFilename(sceneName));
        slotFile.Save("Act", act);
        if (LocationDiscoveryManager.instance != null)
            slotFile.Save("DiscoveredLocations", LocationDiscoveryManager.instance.getDiscoveredLocations());
        slotFile.Save("ModelHair", InventoryMenu.instance.playerModel[0].GetComponent<Image>().sprite);
        slotFile.Save("ModelSkin", InventoryMenu.instance.playerModel[1].GetComponent<Image>().sprite);
        slotFile.Save("ModelLegs", PlayerController.instance.legsSpriteSheetName);
        slotFile.Save("ModelChest", PlayerController.instance.chestSpriteSheetName);
        slotFile.Save("Time", System.DateTime.Now);

        // Flush both files to disk on a background thread.
        currentSaveTask = Task.Run(() =>
        {
            try
            {
                slotFile.Sync();
                sceneFile.Sync();
            }
            catch (Exception e)
            {
                pendingSaveException = e;
            }
        });
    }

    public void campSave()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        Vector3 campPos = PlayerController.instance.transform.position;

        LevelLoader.instance.loadLevel(sceneName, () =>
        {
            // Snap the player back to the rest-trigger position before saveSceneData
            // writes PlayerPosition. Input isn't frozen during the fade, so without
            // this the saved position can drift away from the camp anchor we record below.
            PlayerController.instance.transform.position = campPos;

            respawnRestedWorldStateInCurrentScene();
            GameEvents.current.tryEndInfestation();
            saveSceneData(sceneName, false);
            WaitForActiveSave();
            // Camp position is written AFTER the scene save flushes, to avoid the
            // synchronous-vs-background flush race. See CLAUDE.md "Camp save ordering".
            recordCampPositionForScene(campPos);
        });
    }

    public static void respawnRestedWorldStateInCurrentScene()
    {
        CombatChar.respawnAllRespawnableInCurrentScene();
        WoodenCrateBehavior.respawnAllBrokenInCurrentScene();
        Pushable.respawnAllPushedInCurrentScene();
        Clotberry.respawnAllUsedInCurrentScene();
        OilthistleBehavior.respawnAllConsumedInCurrentScene();
    }


    /// <summary>
    /// Blocks until any in-flight background save has finished writing to disk.
    /// Cheap if nothing is in flight. Call before scene transitions, app quit/suspend,
    /// or any code path that needs the just-saved bytes on disk.
    /// Returns true if the most recent save reached disk cleanly, false if it threw.
    /// </summary>
    public bool WaitForActiveSave()
    {
        bool clean = true;
        var t = currentSaveTask;
        if (t != null && !t.IsCompleted)
        {
            try { t.Wait(); }
            catch (AggregateException ae)
            {
                Debug.LogException(ae.InnerException ?? ae);
                clean = false;
            }
        }

        if (pendingSaveException != null)
        {
            Debug.LogException(pendingSaveException);
            pendingSaveException = null;
            clean = false;
        }
        return clean;
    }

    // Reads the timestamp written by saveAllData ("Time" key in the slot file).
    // False when there's no active slot, no slot file, or the key is missing
    // (e.g. a save written before the timestamp was being recorded).
    public bool tryGetLastSaveTime(out System.DateTime savedAt)
    {
        savedAt = default;
        if (string.IsNullOrEmpty(activeSlotName)) return false;
        string slotFilePath = activeSlotName + ".es3";
        if (!ES3.FileExists(slotFilePath)) return false;
        if (!ES3.KeyExists("Time", slotFilePath)) return false;
        savedAt = ES3.Load<System.DateTime>("Time", slotFilePath);
        return true;
    }

    // OnApplicationQuit fires on PC/Mac quit but is unreliable on consoles & mobile
    // (which suspend rather than quit). Pause/Focus hooks cover those lifecycles.
    private void OnApplicationQuit()
    {
        WaitForActiveSave();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            WaitForActiveSave();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            WaitForActiveSave();
    }

    public string getLocationNameFromFilename(string filename)
    {
        string[] words = filename.Split('_');
        string val;

        SCENENAME_TO_LOCATIONNAME_DICT.TryGetValue(words[1], out val);
        if (val == null)
            Debug.LogWarning("Corresponding location name not found for " + filename + " file name.");

        return val;
    }

    // Location name for where the player stands now - a sub-location (e.g. New Rhea inside
    // Old Town) if the scene has a SubLocationManager, otherwise the scene's own name.
    public string getCurrentLocationName()
    {
        if (SubLocationManager.instance != null)
            return SubLocationManager.instance.getLocationNameAt(PlayerController.instance.transform.position);

        return getLocationNameFromFilename(SceneManager.GetActiveScene().name);
    }

    public string getCurrentActFilenameFromLocation(string location)
    {
        foreach(KeyValuePair<string, string> kvp in SCENENAME_TO_LOCATIONNAME_DICT)
        {
            if (kvp.Value == location)
                return "Act" + act + "_" + kvp.Key;
        }

        string sceneKey;
        if (SUBLOCATION_TO_SCENENAME_DICT.TryGetValue(location, out sceneKey))
            return "Act" + act + "_" + sceneKey;

        Debug.LogWarning("Corresponding filename name not found for " + location + " location.");
        return null;
    }

    public static bool isKnownLocation(string location)
    {
        return SCENENAME_TO_LOCATIONNAME_DICT.ContainsValue(location)
            || SUBLOCATION_TO_SCENENAME_DICT.ContainsKey(location);
    }

    // Travel resolves through the same table, so today "known" and "travelable" are the same set.
    public static bool isTravelableLocation(string location)
    {
        return isKnownLocation(location);
    }

    private void loadDiscoveredLocations(string slotName)
    {
        if (LocationDiscoveryManager.instance == null)
            return;

        string slotFilePath = slotName + ".es3";
        if (ES3.KeyExists("DiscoveredLocations", slotFilePath))
        {
            LocationDiscoveryManager.instance.mergeLoadedLocations(ES3.Load<List<string>>("DiscoveredLocations", slotFilePath));
            return;
        }

        // Saves from before discovery existed: best-effort seed from scenes that have scene data.
        // Sub-location-only visits (e.g. New Rhea inside Old Town) can't be recovered this way.
        foreach (KeyValuePair<string, string> kvp in SCENENAME_TO_LOCATIONNAME_DICT)
        {
            if (ES3.FileExists(slotName + "_Act" + act + "_" + kvp.Key + ".es3"))
                LocationDiscoveryManager.instance.discoverLocation(kvp.Value);
        }
    }

    public string returnSlotName(string baseSlot)
    {
        if (string.IsNullOrEmpty(baseSlot)) return "";
        return ES3.FileExists(baseSlot + ".es3") ? baseSlot : "";
    }

    public void recordCampPositionForScene(Vector3 pos)
    {
        if (string.IsNullOrEmpty(activeSlotName)) return;
        string file = activeSlotName + "_" + SceneManager.GetActiveScene().name + ".es3";
        ES3.Save("LastCampPositionInScene", pos, file);
        ES3.Save("HasCampPositionInScene", true, file);
    }

    public void recordFirstEntryPositionForSceneIfMissing(Vector3 pos)
    {
        if (string.IsNullOrEmpty(activeSlotName)) return;
        string file = activeSlotName + "_" + SceneManager.GetActiveScene().name + ".es3";
        // Skip only if the key both exists AND is explicitly true. A missing key,
        // or an existing key with a non-true value, means we still need to record —
        // otherwise a half-written first-entry leaves death respawn with no fallback.
        if (ES3.FileExists(file)
            && ES3.KeyExists("HasFirstEntryPosition", file)
            && ES3.Load<bool>("HasFirstEntryPosition", file))
            return;
        ES3.Save("SceneFirstEntryPosition", pos, file);
        ES3.Save("HasFirstEntryPosition", true, file);
    }

    public Vector3? getRespawnPositionForScene(string sceneName)
    {
        if (string.IsNullOrEmpty(activeSlotName)) return null;
        string file = activeSlotName + "_" + sceneName + ".es3";
        if (!ES3.FileExists(file)) return null;
        if (ES3.KeyExists("HasCampPositionInScene", file)
            && ES3.Load<bool>("HasCampPositionInScene", file))
            return ES3.Load<Vector3>("LastCampPositionInScene", file);
        if (ES3.KeyExists("HasFirstEntryPosition", file)
            && ES3.Load<bool>("HasFirstEntryPosition", file))
            return ES3.Load<Vector3>("SceneFirstEntryPosition", file);
        return null;
    }

    public void loadSceneData(string sceneName)
    {
        // Drain any in-flight background save before reading; otherwise reload
        // paths that bypass LevelLoader.changeScene (e.g. reloadLastSave,
        // death-reload) can race a still-flushing save write.
        WaitForActiveSave();

        string slotNameToLoad = returnSlotName(activeSlotName);

        if (string.IsNullOrEmpty(slotNameToLoad))
        {
            Debug.Log("No slot name specified, loading scene fresh.");

            if (!sceneName.Contains("Act1_Zephyr"))
                PlayerController.instance.hasKipling = true;

            loadComplete();
            return;
        }

        if (!ES3.FileExists(slotNameToLoad + ".es3"))
        {
            // This should only happen on editor plays and first game load of all time
            Debug.Log("No save file exists yet for this slot, nothing to load.");

            if (!sceneName.Contains("Act1_Zephyr"))
                PlayerController.instance.hasKipling = true;

            //PlayerController.instance.transform.position = Vector3.zero;
            loadComplete();
            return;
        }

        ES3.LoadInto<PlayerController>("Player", slotNameToLoad + ".es3", PlayerController.instance);
        ES3.LoadInto<NotableOutcomesTracker>("NotableOutcomes", slotNameToLoad + ".es3", NotableOutcomesTracker.instance);

        // Player inventory + special handling for dream world
        string dreamModifier = "";
        if (sceneName.Contains("DreamWorld")) // Dream world has scene-specific inventories
            dreamModifier = sceneName;
        else
        {
            ES3.LoadInto<InventoryManager>("InventoryManager" + dreamModifier, slotNameToLoad + ".es3", InventoryManager.instance); // treat inventory as player-level, not a dream world
            InventoryManager.instance.updateEquippedItemsAfterLoad();
        }

        ES3.LoadInto<RecordingsMenu>("RecordingsMenu", slotNameToLoad + ".es3", RecordingsMenu.instance);
        loadDiscoveredLocations(slotNameToLoad);
        hasLoaded = true;

        string sceneFile = slotNameToLoad + "_" + sceneName + ".es3";

        if (ES3.FileExists(sceneFile)) // only load scene data if we've saved scene data
        {
            ES3AutoSaveMgr.Current.settings.path = sceneFile;
            ES3AutoSaveMgr.Current.Load();
            CombatChar.captureMissingSpawnAnchorsInCurrentScene();
            PlayerController.instance.transform.position = ES3.Load<Vector3>("PlayerPosition", slotNameToLoad + "_" + sceneName + ".es3");

            if(dreamModifier == sceneName)
            {
                ES3.LoadInto<InventoryManager>("InventoryManager" + dreamModifier, slotNameToLoad + ".es3", InventoryManager.instance); // treat inventory as scene-level, this is a dream world
                InventoryManager.instance.updateEquippedItemsAfterLoad();
            }
        }
        else
        {
            Debug.Log("No save file exists for this scene, loading scene items fresh.");
            CombatChar.captureMissingSpawnAnchorsInCurrentScene();
        }

        ES3.Save("ActiveScene", sceneName, slotNameToLoad + ".es3"); // make sure the active scene reference is to this one

        string dialogueData = ES3.Load<string>("DialogueData", slotNameToLoad + ".es3");
        if (dialogueData != null)
            SaveSystem.ApplySavedGameData(SaveSystem.Deserialize<SavedGameData>(dialogueData));

        applyPendingArrival();
        GameEvents.current.previousSceneDataLoaded();
    }

    private void loadComplete()
    {
        CombatChar.captureMissingSpawnAnchorsInCurrentScene();
        InventoryManager.instance.updateEquippedItemsAfterLoad();
        applyPendingArrival();
        GameEvents.current.previousSceneDataLoaded();
    }

    public void setPendingArrival(string locationName)
    {
        pendingArrivalLocation = locationName;
    }

    public void clearPendingArrival()
    {
        pendingArrivalLocation = null;
    }

    // Overrides the loaded PlayerPosition with the travelled-to location's arrival point. Runs before
    // previousSceneDataLoaded, since beat managers place followers relative to the player there.
    // Scenes without sub-locations have no arrival points, so the player keeps their saved position.
    private void applyPendingArrival()
    {
        string locationName = pendingArrivalLocation;
        pendingArrivalLocation = null;

        if (string.IsNullOrEmpty(locationName) || SubLocationManager.instance == null)
            return;

        Transform arrival = SubLocationManager.instance.getArrivalPoint(locationName);
        if (arrival == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("applyPendingArrival: no arrival point set for '" + locationName + "' in this scene's SubLocationManager.");
#endif
            return;
        }

        Transform player = PlayerController.instance.transform;
        player.position = new Vector3(arrival.position.x, arrival.position.y, player.position.z);
    }

}

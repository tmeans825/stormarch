using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using ES3Internal;

/**
    DESCRIPTION: Keeps each scene's Easy Save 3 reference list (ES3ReferenceMgr.idRef) down to one
    entry per live object. Two leaks made scenes 0.5 GB:

    1. Orphaned shadow meshes. URP 8.2.0's ShadowCaster2D.OnEnable builds a new Mesh whenever its
       saved m_InstanceId differs from GetInstanceID() - every time a scene is opened. ES3's refresh
       adds it to idRef (CanBeSaved deliberately lets hidden Meshes through) and never removes the
       replaced one, so Unity keeps writing it into the scene and it reloads as live next open.
       Live = every ShadowCaster2D's m_Mesh (inactive included) plus EditorUtility.CollectDependencies
       of the scene roots, minus the Easy Save 3 Manager (its idRef would keep everything "live").

    2. Duplicate IDs. ES3ReferenceMgrBase.Add(obj) only reuses an ID when the lazy reverse cache
       refId already maps the object; otherwise it mints a random one and never drops the old key.
       Whenever refId misses objects idRef still holds, the next refresh registers every object
       again - scenes reached hundreds of IDs per object. Per object we keep one ID: its global
       reference ID if it is an asset and that ID is among its keys, else the ID refId mapped it to
       before cleaning, else the first seen. Once each object has one key, rebuilding refId is exact,
       so it is also invalidated after Undo/Redo, scene open and domain reload.

    ES3's Optimize can't be used - Remove(obj) scans all of idRef per object, which hangs on
    millions of entries. Instead a compact dictionary is built in one pass and swapped in. No Undo:
    snapshotting millions of entries stalls the editor, and git is the backup.

    Plugin code is untouched; only the scene's own manager is changed. The game has no save files to
    preserve, so which ID an object keeps doesn't matter beyond the current editor session.

    USAGE:
      Stormfall/ES3 Reference List/Preview Open Scenes - dry run, logs counts
      Stormfall/ES3 Reference List/Clean Open Scenes   - cleans and checks; then Ctrl+S
      Stormfall/ES3 Reference List/Clean All Scenes in Project - opens, cleans and saves each scene
                                                                  (also the batch-mode entry point)

    Also runs on every scene save, silent unless it removed something.
**/

[InitializeOnLoad]
public static class ES3ReferenceListCleaner
{
    const string MANAGER_NAME = "Easy Save 3 Manager";
    const string LOG_PREFIX = "[ES3ReferenceListCleaner]";

    class CleanResult
    {
        public ES3IdRefDictionary idRef;
        public int before;
        public int orphanMeshes;
        public int liveMeshes;
        public int duplicates;
        public int removed => orphanMeshes + duplicates;
    }

    static ES3ReferenceListCleaner()
    {
        EditorSceneManager.sceneSaving += onSceneSaving;
        EditorSceneManager.sceneOpened += (scene, mode) => invalidateRefId(scene);
        Undo.undoRedoPerformed += invalidateAllRefIds;
        // Domain reload drops ES3's static refresh state too; scenes may not be ready inside the ctor.
        EditorApplication.delayCall += invalidateAllRefIds;
    }

    // -------------------------------------------------------------------------
    // Menu
    // -------------------------------------------------------------------------

    [MenuItem("Stormfall/ES3 Reference List/Preview Open Scenes")]
    public static void previewOpenScenes()
    {
        forEachOpenScene((scene, mgr) =>
        {
            CleanResult result = buildCleanIdRef(mgr);
            Debug.Log($"{LOG_PREFIX} {scene.name}: would remove {result.orphanMeshes} orphan meshes and " +
                      $"{result.duplicates} duplicate IDs, keeping {result.liveMeshes} live meshes " +
                      $"(idRef {result.before} -> {result.idRef.Count}). No changes made.");
        });
    }

    [MenuItem("Stormfall/ES3 Reference List/Clean Open Scenes")]
    public static void cleanOpenScenes()
    {
        forEachOpenScene((scene, mgr) =>
        {
            CleanResult result = buildCleanIdRef(mgr);
            applyCleanIdRef(mgr, result);
            EditorSceneManager.MarkSceneDirty(scene);

            Debug.Log($"{LOG_PREFIX} {scene.name}: removed {result.orphanMeshes} orphan meshes and " +
                      $"{result.duplicates} duplicate IDs, kept {result.liveMeshes} live meshes " +
                      $"(idRef {result.before} -> {mgr.idRef.Count}). Save the scene to apply.");
            checkInvariants(scene, mgr);
        });
    }

    // Opens, cleans and saves every scene under Assets/Scenes. Also the batch-mode entry point:
    //   Unity.exe -batchmode -quit -projectPath <path> -executeMethod ES3ReferenceListCleaner.cleanAllScenes
    [MenuItem("Stormfall/ES3 Reference List/Clean All Scenes in Project")]
    public static void cleanAllScenes()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning($"{LOG_PREFIX} Exit play mode first.");
            return;
        }
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        bool allPassed = true;
        foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

            var mgr = ES3ReferenceMgrBase.GetManagerFromScene(scene) as ES3ReferenceMgr;
            if (mgr == null) continue;

            CleanResult result = buildCleanIdRef(mgr);
            applyCleanIdRef(mgr, result);
            Debug.Log($"{LOG_PREFIX} {scene.name}: removed {result.orphanMeshes} orphan meshes and " +
                      $"{result.duplicates} duplicate IDs, kept {result.liveMeshes} live meshes " +
                      $"(idRef {result.before} -> {mgr.idRef.Count}).");

            if (!checkInvariants(scene, mgr))
            {
                allPassed = false;
                continue; // leave this scene unsaved
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        if (Application.isBatchMode) EditorApplication.Exit(allPassed ? 0 : 1);
    }

    // -------------------------------------------------------------------------
    // Editor hooks
    // -------------------------------------------------------------------------

    static void onSceneSaving(Scene scene, string path)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        var mgr = ES3ReferenceMgrBase.GetManagerFromScene(scene) as ES3ReferenceMgr;
        if (mgr == null) return;

        CleanResult result = buildCleanIdRef(mgr);
        if (result.removed == 0) return;

        applyCleanIdRef(mgr, result);
        Debug.Log($"{LOG_PREFIX} {scene.name}: removed {result.orphanMeshes} orphan meshes and " +
                  $"{result.duplicates} duplicate IDs on save.");
    }

    static void invalidateAllRefIds()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
            invalidateRefId(SceneManager.GetSceneAt(i));
    }

    // refId is a lazy reverse cache of idRef; nulling it makes ES3 rebuild it on next use.
    static void invalidateRefId(Scene scene)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || !scene.isLoaded) return;

        var mgr = ES3ReferenceMgrBase.GetManagerFromScene(scene) as ES3ReferenceMgr;
        if (mgr != null) mgr.refId = null;
    }

    // -------------------------------------------------------------------------
    // Core
    // -------------------------------------------------------------------------

    // Read-only: builds the replacement idRef without touching the manager.
    static CleanResult buildCleanIdRef(ES3ReferenceMgr mgr)
    {
        var result = new CleanResult { before = mgr.idRef.Count };

        // Read before anything nulls refId - a rebuild would pick an arbitrary duplicate.
        ES3RefIdDictionary currentIds = mgr.refId;
        ES3RefIdDictionary globalIds = ES3GlobalReferences.Instance != null ? ES3GlobalReferences.Instance.refId : null;
        HashSet<Object> liveMeshes = collectLiveMeshes(mgr);

        // Pass 1: pick one keeper per object (lower rank wins).
        var keeper = new Dictionary<Object, long>();
        var keeperRank = new Dictionary<Object, int>();
        var orphanMeshes = new HashSet<Object>();
        int candidates = 0;

        foreach (KeyValuePair<long, Object> pair in mgr.idRef)
        {
            Object obj = pair.Value;
            if (obj == null) continue;

            if (obj is Mesh mesh && !EditorUtility.IsPersistent(mesh))
            {
                if (!liveMeshes.Contains(mesh))
                {
                    orphanMeshes.Add(mesh);
                    result.orphanMeshes++;
                    continue;
                }
            }

            candidates++;
            int rank = idRank(obj, pair.Key, currentIds, globalIds);
            if (!keeperRank.TryGetValue(obj, out int bestRank) || rank < bestRank)
            {
                keeper[obj] = pair.Key;
                keeperRank[obj] = rank;
            }
        }

        // Pass 2: rebuild in the original order so the scene diff stays readable.
        // Null values are kept as-is - ES3's RemoveNullOrInvalidValues owns those.
        result.idRef = new ES3IdRefDictionary();
        foreach (KeyValuePair<long, Object> pair in mgr.idRef)
        {
            if (pair.Value == null)
                result.idRef[pair.Key] = null;
            else if (keeper.TryGetValue(pair.Value, out long id) && id == pair.Key)
                result.idRef[pair.Key] = pair.Value;
        }

        result.duplicates = candidates - keeper.Count;
        result.liveMeshes = keeper.Keys.Count(o => o is Mesh mesh && !EditorUtility.IsPersistent(mesh));
        return result;
    }

    static int idRank(Object obj, long id, ES3RefIdDictionary currentIds, ES3RefIdDictionary globalIds)
    {
        if (globalIds != null && globalIds.TryGetValue(obj, out long globalId) && globalId == id) return 0;
        if (currentIds.TryGetValue(obj, out long currentId) && currentId == id) return 1;
        return 2;
    }

    static HashSet<Object> collectLiveMeshes(ES3ReferenceMgr mgr)
    {
        GameObject[] roots = mgr.gameObject.scene.GetRootGameObjects()
            .Where(go => go != mgr.gameObject && go.name != MANAGER_NAME)
            .ToArray();

        var live = new HashSet<Object>(EditorUtility.CollectDependencies(roots));
        foreach (GameObject root in roots)
        {
            foreach (ShadowCaster2D caster in root.GetComponentsInChildren<ShadowCaster2D>(true))
            {
                Object mesh = new SerializedObject(caster).FindProperty("m_Mesh").objectReferenceValue;
                if (mesh != null) live.Add(mesh);
            }
        }
        return live;
    }

    // Touches only this manager - ES3's Remove(obj) would hit every registered manager.
    static void applyCleanIdRef(ES3ReferenceMgr mgr, CleanResult result)
    {
        mgr.idRef = result.idRef;
        // Same invalidation ES3 uses after ChangeId.
        mgr.refId = null;
        EditorUtility.SetDirty(mgr);
    }

    static bool checkInvariants(Scene scene, ES3ReferenceMgr mgr)
    {
        int nonNull = mgr.idRef.Values.Count(o => o != null);
        int distinct = mgr.refId.Count;
        if (distinct != nonNull)
        {
            Debug.LogError($"{LOG_PREFIX} {scene.name}: {nonNull} non-null entries but {distinct} distinct objects - " +
                           "some object still has more than one ID.", mgr);
            return false;
        }

        ES3RefIdDictionary globalIds = ES3GlobalReferences.Instance != null ? ES3GlobalReferences.Instance.refId : null;
        if (globalIds == null) return true;

        int offGlobal = mgr.idRef.Count(pair => pair.Value != null
            && globalIds.TryGetValue(pair.Value, out long globalId) && globalId != pair.Key);
        if (offGlobal > 0)
            Debug.Log($"{LOG_PREFIX} {scene.name}: {offGlobal} assets keep a scene ID that differs from their " +
                      "global ID (their global ID was never in this scene's list). Harmless within the scene.");
        return true;
    }

    static void forEachOpenScene(System.Action<Scene, ES3ReferenceMgr> action)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning($"{LOG_PREFIX} Exit play mode first.");
            return;
        }

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;

            var mgr = ES3ReferenceMgrBase.GetManagerFromScene(scene) as ES3ReferenceMgr;
            if (mgr == null)
            {
                Debug.Log($"{LOG_PREFIX} {scene.name}: no Easy Save 3 Manager, nothing to do.");
                continue;
            }
            action(scene, mgr);
        }
    }
}

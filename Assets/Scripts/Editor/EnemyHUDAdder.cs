using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/**
    DESCRIPTION: One-shot migration tool for EnemyHUDController extraction.

    USAGE:
      1. Run "Stormfall/EnemyHUD Migration/Step 1 - Migrate Enemy Prefabs"
         This adds EnemyHUDController to every enemy prefab in the project.
      2. Open each scene and run "Step 2 - Migrate Scene Enemies" for any
         enemies placed directly in scenes (not covered by prefabs).
      3. In the Inspector, assign healthBarContainers + shieldBarGrouper on
         each EnemyHUDController — these refs need to be set manually since
         they were moved from Enemy. They are the same child GameObjects that
         were previously referenced; nothing moved in the hierarchy.
      4. Run "Validate Scene" to confirm no enemies are missing the component.

    Safe to re-run: already-migrated objects are skipped.
**/

public static class EnemyHUDAdder
{
    // -------------------------------------------------------------------------
    // Step 1 — Prefabs
    // -------------------------------------------------------------------------

    [MenuItem("Stormfall/EnemyHUD Migration/Step 1 - Migrate Enemy Prefabs")]
    public static void MigratePrefabs()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        int added   = 0;
        int skipped = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            Enemy enemy = prefab.GetComponent<Enemy>();
            if (enemy == null) continue; // not an enemy prefab

            if (prefab.GetComponent<EnemyHUDController>() != null)
            {
                skipped++;
                continue; // already migrated
            }

            using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
            {
                scope.prefabContentsRoot.AddComponent<EnemyHUDController>();
                // NOTE: healthBarContainers + shieldBarGrouper must be assigned
                //       manually in the Inspector for each prefab after this runs.
            }

            added++;
            Debug.Log($"[EnemyHUDAdder] Added EnemyHUDController to prefab: {path}");
        }

        Debug.Log($"[EnemyHUDAdder] Prefab migration complete. Added: {added}, Already done: {skipped}.");
    }

    // -------------------------------------------------------------------------
    // Step 2 — Scene objects (run once per scene)
    // -------------------------------------------------------------------------

    [MenuItem("Stormfall/EnemyHUD Migration/Step 2 - Migrate Scene Enemies")]
    public static void MigrateSceneObjects()
    {
        Enemy[] enemies = Object.FindObjectsOfType<Enemy>(true); // true = include inactive
        int added   = 0;
        int skipped = 0;

        foreach (Enemy enemy in enemies)
        {
            if (enemy.GetComponent<EnemyHUDController>() != null)
            {
                skipped++;
                continue;
            }

            // Use Undo so the change is undoable in the editor
            Undo.AddComponent<EnemyHUDController>(enemy.gameObject);
            // NOTE: healthBarContainers + shieldBarGrouper must be assigned
            //       manually in the Inspector after this runs.

            EditorUtility.SetDirty(enemy.gameObject);
            added++;
            Debug.Log($"[EnemyHUDAdder] Added EnemyHUDController to scene object: {enemy.gameObject.name}");
        }

        if (added > 0)
            EditorSceneManager.MarkAllScenesDirty();

        Debug.Log($"[EnemyHUDAdder] Scene migration complete. Added: {added}, Already done: {skipped}.");
    }

    // -------------------------------------------------------------------------
    // Validation — run after code changes to confirm nothing is missing
    // -------------------------------------------------------------------------

    [MenuItem("Stormfall/EnemyHUD Migration/Validate Scene (post-code-change)")]
    public static void ValidateScene()
    {
        Enemy[] enemies = Object.FindObjectsOfType<Enemy>(true);
        int missing = 0;

        foreach (Enemy enemy in enemies)
        {
            if (enemy.GetComponent<EnemyHUDController>() == null)
            {
                Debug.LogError($"[EnemyHUDAdder] MISSING EnemyHUDController on: {enemy.gameObject.name}", enemy.gameObject);
                missing++;
            }
        }

        if (missing == 0)
            Debug.Log("[EnemyHUDAdder] Validation passed — all scene enemies have EnemyHUDController.");
        else
            Debug.LogError($"[EnemyHUDAdder] Validation FAILED — {missing} enemy/enemies missing EnemyHUDController.");
    }
}

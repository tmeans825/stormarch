using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;

public class SyncCharmsFromResources : EditorWindow
{
    // Default, per your project layout
    private const string DefaultJsonPath =
        "Assets/Resources/charms.json";

    private const string DefaultTranslationAssetPath =
        "Assets/Plugins/Pixel Crushers/Dialogue System/Resources/UI Translation Table.asset";

    // Window fields (optional overrides)
    private UnityEngine.Object translationAsset;
    private TextAsset charmsJson;

    private bool useStableKeys = true;     // Charm/<id>/<field>
    private string namespaceRoot = "Charm";
    private bool migrateLegacyTextKeys = true;

    private bool syncDisplayName = true;
    private bool syncCharmName = true;
    private bool syncLore = true;
    private bool syncNamePrefix = true;
    private bool syncNameCore = true;
    private bool syncNameSuffix = true;

    [MenuItem("Tools/Localization/Sync Charms (Resources One-Click)")]
    public static void OneClick()
    {
        // Load the Translation Table asset directly by AssetDatabase path
        var table = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(DefaultTranslationAssetPath);
        if (table == null)
        {
            EditorUtility.DisplayDialog("Error",
                $"Could not load Translation Table at:\n{DefaultTranslationAssetPath}", "OK");
            return;
        }

        // Load the JSON as TextAsset from Resources by AssetDatabase path
        // (Since it's under Assets/Resources, we can also do Resources.Load<TextAsset>("charms"))
        var jsonAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(DefaultJsonPath);
        if (jsonAsset == null)
        {
            // fallback to Resources.Load in case of meta/file issues
            jsonAsset = Resources.Load<TextAsset>("charms");
        }
        if (jsonAsset == null)
        {
            EditorUtility.DisplayDialog("Error",
                $"Could not load charms.json at:\n{DefaultJsonPath}\n(or via Resources.Load(\"charms\"))", "OK");
            return;
        }

        try
        {
            var result = Sync(table, jsonAsset,
                useStableKeys: true,
                namespaceRoot: "Charm",
                migrateLegacyTextKeys: true,
                fields: new[] {
                    ("displayName",  true),
                    ("charmName",    true),
                    ("lore",         true),
                    ("namePrefix",   true),
                    ("nameCore",     true),
                    ("nameSuffix",   true),
                });

            EditorUtility.DisplayDialog("Sync Complete",
                $"Added: {result.added}\nUpdated: {result.updated}\nMigrated: {result.migrated}\nNext Field ID: {result.nextFieldId}",
                "OK");
        }
        catch (Exception ex)
        {
            Debug.LogError("[Sync Charms] " + ex);
            EditorUtility.DisplayDialog("Error", ex.Message, "OK");
        }
    }

    [MenuItem("Tools/Localization/Sync Charms (Resources… Window)")]
    public static void ShowWindow()
    {
        var w = GetWindow<SyncCharmsFromResources>("Sync Charms");
        w.minSize = new Vector2(520, 380);

        // Pre-fill defaults if possible
        w.translationAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(DefaultTranslationAssetPath);
        w.charmsJson = AssetDatabase.LoadAssetAtPath<TextAsset>(DefaultJsonPath);
        if (w.charmsJson == null) w.charmsJson = Resources.Load<TextAsset>("charms");
        w.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Default Locations (detected)", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("UI Table:", DefaultTranslationAssetPath, EditorStyles.miniLabel);
        EditorGUILayout.LabelField("Charms JSON:", DefaultJsonPath, EditorStyles.miniLabel);
        EditorGUILayout.Space(6);

        EditorGUILayout.LabelField("Override (optional)", EditorStyles.boldLabel);
        translationAsset = EditorGUILayout.ObjectField("Translation Table", translationAsset, typeof(UnityEngine.Object), false);
        charmsJson = (TextAsset)EditorGUILayout.ObjectField("charms.json", charmsJson, typeof(TextAsset), false);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);
        useStableKeys = EditorGUILayout.ToggleLeft("Use Stable Keys (Charm/<id>/<field>)", useStableKeys);
        if (useStableKeys)
        {
            EditorGUI.indentLevel++;
            namespaceRoot = EditorGUILayout.TextField("Namespace Root", namespaceRoot);
            EditorGUI.indentLevel--;
        }
        migrateLegacyTextKeys = EditorGUILayout.ToggleLeft("Migrate legacy text-only keys (preserve translations)", migrateLegacyTextKeys);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Fields to Sync", EditorStyles.boldLabel);
        syncDisplayName = EditorGUILayout.ToggleLeft("displayName → Default", syncDisplayName);
        syncCharmName = EditorGUILayout.ToggleLeft("charmName → Default", syncCharmName);
        syncLore = EditorGUILayout.ToggleLeft("lore → Default", syncLore);
        syncNamePrefix = EditorGUILayout.ToggleLeft("namePrefix → Default", syncNamePrefix);
        syncNameCore = EditorGUILayout.ToggleLeft("nameCore → Default", syncNameCore);
        syncNameSuffix = EditorGUILayout.ToggleLeft("nameSuffix → Default", syncNameSuffix);

        EditorGUILayout.Space(10);
        if (GUILayout.Button("Sync Now"))
        {
            // Fallback to defaults if fields are empty
            var table = translationAsset ??
                        AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(DefaultTranslationAssetPath);
            var json = charmsJson ??
                        AssetDatabase.LoadAssetAtPath<TextAsset>(DefaultJsonPath) ??
                        Resources.Load<TextAsset>("charms");

            if (table == null)
            {
                EditorUtility.DisplayDialog("Error",
                    "Could not resolve Translation Table (override empty and default path not found).", "OK");
                return;
            }
            if (json == null)
            {
                EditorUtility.DisplayDialog("Error",
                    "Could not resolve charms.json (override empty and default path not found).", "OK");
                return;
            }

            try
            {
                var result = Sync(table, json,
                    useStableKeys: useStableKeys,
                    namespaceRoot: namespaceRoot,
                    migrateLegacyTextKeys: migrateLegacyTextKeys,
                    fields: new[] {
                        ("displayName",  syncDisplayName),
                        ("charmName",    syncCharmName),
                        ("lore",         syncLore),
                        ("namePrefix",   syncNamePrefix),
                        ("nameCore",     syncNameCore),
                        ("nameSuffix",   syncNameSuffix),
                    });

                EditorUtility.DisplayDialog("Sync Complete",
                    $"Added: {result.added}\nUpdated: {result.updated}\nMigrated: {result.migrated}\nNext Field ID: {result.nextFieldId}",
                    "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError("[Sync Charms] " + ex);
                EditorUtility.DisplayDialog("Error", ex.Message, "OK");
            }
        }
    }

    // ---------- Core sync ----------

    [Serializable]
    private class Charm
    {
        public string id;
        public string displayName;
        public string charmName;
        public string lore;
        public string type;
        public int budgetCost;
        public string namePrefix;
        public string nameCore;
        public string nameSuffix;
    }

    [Serializable]
    private class CharmRoot { public List<Charm> charms; }

    private static (int added, int updated, int migrated, int nextFieldId) Sync(
        UnityEngine.Object translationAsset,
        TextAsset charmsJson,
        bool useStableKeys,
        string namespaceRoot,
        bool migrateLegacyTextKeys,
        (string field, bool enabled)[] fields)
    {
        var root = JsonUtility.FromJson<CharmRoot>(charmsJson.text);
        if (root == null || root.charms == null || root.charms.Count == 0)
            throw new InvalidOperationException("No charms found in JSON (Assets/Resources/charms.json).");

        var so = new SerializedObject(translationAsset);
        var spLangKeys = so.FindProperty("m_languageKeys");   // int[]
        var spFieldKeys = so.FindProperty("m_fieldKeys");      // int[]
        var spFieldValues = so.FindProperty("m_fieldValues");    // array of entries
        var spNextFieldId = so.FindProperty("m_nextFieldID");    // int

        if (spLangKeys == null || spFieldKeys == null || spFieldValues == null || spNextFieldId == null)
            throw new InvalidOperationException(
                "Expected serialized fields not found:\n" +
                "m_languageKeys, m_fieldKeys, m_fieldValues, m_nextFieldID");

        int languageCount = spLangKeys.arraySize;
        if (languageCount <= 0) throw new InvalidOperationException("No languages in m_languageKeys.");

        // Build lookup for existing entries
        var byFieldName = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < spFieldValues.arraySize; i++)
        {
            var entry = spFieldValues.GetArrayElementAtIndex(i);
            var nameProp = entry.FindPropertyRelative("m_fieldName");
            string fname = nameProp?.stringValue ?? "";
            if (!string.IsNullOrEmpty(fname) && !byFieldName.ContainsKey(fname))
                byFieldName.Add(fname, i);
        }

        // Helper to ensure keys & values sized to languageCount
        void EnsureKeyAndValues(SerializedProperty entry)
        {
            var keys = entry.FindPropertyRelative("m_keys");
            if (keys.arraySize != languageCount)
            {
                keys.arraySize = languageCount;
                for (int i = 0; i < languageCount; i++)
                    keys.GetArrayElementAtIndex(i).intValue = i;
            }

            var values = entry.FindPropertyRelative("m_values");
            if (values.arraySize != languageCount)
            {
                int old = values.arraySize;
                values.arraySize = languageCount;
                for (int i = old; i < languageCount; i++)
                    values.GetArrayElementAtIndex(i).stringValue = string.Empty;
            }
        }

        void SetDefault(SerializedProperty entry, string text)
        {
            var values = entry.FindPropertyRelative("m_values");
            if (values != null && values.arraySize > 0)
                values.GetArrayElementAtIndex(0).stringValue = text ?? string.Empty;
        }

        int nextFieldId = spNextFieldId.intValue;
        int added = 0, updated = 0, migrated = 0;

        so.Update();

        foreach (var charm in root.charms)
        {
            foreach (var (field, enabled) in fields)
            {
                if (!enabled) continue;

                string value = GetField(charm, field)?.Trim();
                if (string.IsNullOrEmpty(value)) continue;

                string stableKey = useStableKeys
                    ? $"{namespaceRoot}/{charm.id}/{field}"
                    : value;

                // Update existing by stable key
                if (byFieldName.TryGetValue(stableKey, out int idx))
                {
                    var entry = spFieldValues.GetArrayElementAtIndex(idx);
                    EnsureKeyAndValues(entry);
                    SetDefault(entry, value);
                    updated++;
                    continue;
                }

                // Migrate legacy text-only key to stable key (preserve translations)
                if (useStableKeys && migrateLegacyTextKeys && byFieldName.TryGetValue(value, out idx))
                {
                    var entry = spFieldValues.GetArrayElementAtIndex(idx);
                    EnsureKeyAndValues(entry);
                    entry.FindPropertyRelative("m_fieldName").stringValue = stableKey;
                    SetDefault(entry, value);

                    byFieldName.Remove(value);
                    byFieldName[stableKey] = idx;
                    migrated++;
                    continue;
                }

                // Create new entry
                int newIndex = spFieldValues.arraySize;
                spFieldValues.InsertArrayElementAtIndex(newIndex);
                var newEntry = spFieldValues.GetArrayElementAtIndex(newIndex);
                newEntry.FindPropertyRelative("m_fieldName").stringValue = stableKey;

                var keys = newEntry.FindPropertyRelative("m_keys");
                keys.arraySize = languageCount;
                for (int i = 0; i < languageCount; i++)
                    keys.GetArrayElementAtIndex(i).intValue = i;

                var vals = newEntry.FindPropertyRelative("m_values");
                vals.arraySize = languageCount;
                vals.GetArrayElementAtIndex(0).stringValue = value;
                for (int i = 1; i < languageCount; i++)
                    vals.GetArrayElementAtIndex(i).stringValue = string.Empty;

                // Append new field key
                int fkIdx = spFieldKeys.arraySize;
                spFieldKeys.InsertArrayElementAtIndex(fkIdx);
                spFieldKeys.GetArrayElementAtIndex(fkIdx).intValue = nextFieldId++;

                byFieldName[stableKey] = newIndex;
                added++;
            }
        }

        spNextFieldId.intValue = nextFieldId;

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(translationAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[Sync Charms] Added:{added} Updated:{updated} Migrated:{migrated} NextFieldID:{nextFieldId} Langs:{languageCount}");
        return (added, updated, migrated, nextFieldId);
    }

    private static string GetField(object charmObj, string field)
    {
        var t = charmObj.GetType();
        var f = t.GetField(field);
        if (f != null) return f.GetValue(charmObj) as string;

        var p = t.GetProperty(field);
        if (p != null) return p.GetValue(charmObj) as string;

        return null;
    }
}

using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text;
using System;
using System.Linq;
using System.Collections.Generic;

public static class TranslatorCsvSync
{
    // Adjust these to your defaults (you can still pick different files at runtime):
    const string DefaultTablePath =
        "Assets/Plugins/Pixel Crushers/Dialogue System/Resources/UI Translation Table.asset";

    // ===== MENU =====

    [MenuItem("Tools/Localization/Export Translations (CSV)")]
    public static void ExportCsv()
    {
        var table = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(DefaultTablePath);
        if (table == null)
        {
            EditorUtility.DisplayDialog("Error", $"Could not load:\n{DefaultTablePath}", "OK");
            return;
        }

        // Ask where to save
        string outPath = EditorUtility.SaveFilePanel("Export Translations CSV", Application.dataPath, "translations", "csv");
        if (string.IsNullOrEmpty(outPath)) return;

        // Ask if we filter to Charm/*
        bool onlyCharm = EditorUtility.DisplayDialog("Filter", "Export only keys starting with Charm/ ?", "Yes", "No");

        try
        {
            int exported = ExportTableToCsv(table, outPath, onlyCharm ? "Charm/" : null);
            EditorUtility.DisplayDialog("Export Complete", $"Exported {exported} rows to:\n{outPath}", "OK");
            EditorUtility.RevealInFinder(outPath);
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
            EditorUtility.DisplayDialog("Export Failed", ex.Message, "OK");
        }
    }

    [MenuItem("Tools/Localization/Import Translations (CSV)")]
    public static void ImportCsv()
    {
        var table = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(DefaultTablePath);
        if (table == null)
        {
            EditorUtility.DisplayDialog("Error", $"Could not load:\n{DefaultTablePath}", "OK");
            return;
        }

        string inPath = EditorUtility.OpenFilePanel("Import Translations CSV", Application.dataPath, "csv");
        if (string.IsNullOrEmpty(inPath)) return;

        try
        {
            var (updated, added) = ImportCsvToTable(table, inPath);
            EditorUtility.DisplayDialog("Import Complete", $"Updated {updated} cells.\nAdded {added} new keys (if any).", "OK");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
            EditorUtility.DisplayDialog("Import Failed", ex.Message, "OK");
        }
    }

    // ===== EXPORT =====

    private static int ExportTableToCsv(UnityEngine.Object table, string outPath, string prefixFilter)
    {
        var so = new SerializedObject(table);
        var spLangKeys = so.FindProperty("m_languageKeys");   // array of language names (often "Default","en","es")
        var spFieldValues = so.FindProperty("m_fieldValues");    // entries
        if (spLangKeys == null || spFieldValues == null)
            throw new InvalidOperationException("Expected m_languageKeys / m_fieldValues not found on the asset.");

        // Gather language names (fallback to Lang0.. if missing)
        int L = spLangKeys.arraySize;
        var langNames = new List<string>();
        for (int i = 0; i < L; i++)
        {
            var el = spLangKeys.GetArrayElementAtIndex(i);
            langNames.Add(!string.IsNullOrEmpty(el.stringValue) ? el.stringValue : $"Lang{i}");
        }

        var sb = new StringBuilder(1024);
        // Header
        sb.Append(Esc("key"));
        foreach (var ln in langNames) { sb.Append(',').Append(Esc(ln)); }
        sb.Append('\n');

        int rows = 0;
        for (int i = 0; i < spFieldValues.arraySize; i++)
        {
            var entry = spFieldValues.GetArrayElementAtIndex(i);
            var nameProp = entry.FindPropertyRelative("m_fieldName");
            var values = entry.FindPropertyRelative("m_values");
            string key = nameProp?.stringValue ?? "";

            if (!string.IsNullOrEmpty(prefixFilter) && !key.StartsWith(prefixFilter, StringComparison.Ordinal))
                continue;

            // key
            sb.Append(Esc(key));

            // languages (pad if needed)
            int count = values != null ? values.arraySize : 0;
            for (int li = 0; li < L; li++)
            {
                string v = (li < count) ? values.GetArrayElementAtIndex(li).stringValue : "";
                sb.Append(',').Append(Esc(v));
            }
            sb.Append('\n');
            rows++;
        }

        // Write CSV (UTF-8 with BOM to play nicer with Excel)
        File.WriteAllText(outPath, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        return rows;
    }

    // ===== IMPORT =====

    private static (int updatedCells, int addedKeys) ImportCsvToTable(UnityEngine.Object table, string inPath)
    {
        var so = new SerializedObject(table);
        var spLangKeys = so.FindProperty("m_languageKeys");
        var spFieldKeys = so.FindProperty("m_fieldKeys");
        var spFieldValues = so.FindProperty("m_fieldValues");
        var spNextFieldId = so.FindProperty("m_nextFieldID");

        if (spLangKeys == null || spFieldKeys == null || spFieldValues == null || spNextFieldId == null)
            throw new InvalidOperationException("Expected m_languageKeys, m_fieldKeys, m_fieldValues, m_nextFieldID missing.");

        int L = spLangKeys.arraySize; // language count
        if (L <= 0) throw new InvalidOperationException("No languages configured in m_languageKeys.");

        // Build index of existing keys
        var index = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < spFieldValues.arraySize; i++)
        {
            var e = spFieldValues.GetArrayElementAtIndex(i);
            var nameProp = e.FindPropertyRelative("m_fieldName");
            string k = nameProp?.stringValue ?? "";
            if (!string.IsNullOrEmpty(k) && !index.ContainsKey(k)) index.Add(k, i);
        }

        var lines = File.ReadAllLines(inPath);
        if (lines.Length == 0) return (0, 0);

        // Parse header: expect first column "key" then L language columns
        var header = SplitCsvLine(lines[0]);
        if (header.Count < 1 + L)
            throw new InvalidOperationException($"CSV must have at least {1 + L} columns (key + {L} languages).");

        // Find "key" column (case-insensitive)
        int keyCol = header.FindIndex(h => string.Equals(h, "key", StringComparison.OrdinalIgnoreCase));
        if (keyCol != 0)
            throw new InvalidOperationException("First column must be 'key'.");

        int updatedCells = 0;
        int addedKeys = 0;
        int nextId = spNextFieldId.intValue;

        so.Update();

        for (int li = 1; li < lines.Length; li++)
        {
            if (string.IsNullOrWhiteSpace(lines[li])) continue;
            var cells = SplitCsvLine(lines[li]);

            if (cells.Count < 1) continue;
            string key = cells[0]?.Trim() ?? "";
            if (string.IsNullOrEmpty(key)) continue;

            // Ensure we have L language columns; pad if shorter
            while (cells.Count < 1 + L) cells.Add("");

            // Locate or create entry
            int rowIndex;
            if (!index.TryGetValue(key, out rowIndex))
            {
                // Create new entry
                rowIndex = spFieldValues.arraySize;
                spFieldValues.InsertArrayElementAtIndex(rowIndex);
                var newEntry = spFieldValues.GetArrayElementAtIndex(rowIndex);
                newEntry.FindPropertyRelative("m_fieldName").stringValue = key;

                // m_keys: 0..L-1
                var keysProp = newEntry.FindPropertyRelative("m_keys");
                keysProp.arraySize = L;
                for (int kk = 0; kk < L; kk++) keysProp.GetArrayElementAtIndex(kk).intValue = kk;

                // m_values: initialize empty
                var valsProp = newEntry.FindPropertyRelative("m_values");
                valsProp.arraySize = L;
                for (int vi = 0; vi < L; vi++) valsProp.GetArrayElementAtIndex(vi).stringValue = string.Empty;

                // m_fieldKeys append
                int fkIdx = spFieldKeys.arraySize;
                spFieldKeys.InsertArrayElementAtIndex(fkIdx);
                spFieldKeys.GetArrayElementAtIndex(fkIdx).intValue = nextId++;

                index[key] = rowIndex;
                addedKeys++;
            }

            // Update values: blank = leave as-is, [[CLEAR]] = set to empty
            var entry = spFieldValues.GetArrayElementAtIndex(rowIndex);
            var values = entry.FindPropertyRelative("m_values");
            values.arraySize = Math.Max(values.arraySize, L);

            for (int lang = 0; lang < L; lang++)
            {
                string cell = cells[1 + lang]; // after key
                if (string.IsNullOrEmpty(cell)) continue;             // leave as-is
                if (cell == "[[CLEAR]]") cell = string.Empty;         // explicit clear

                var sp = values.GetArrayElementAtIndex(lang);
                if (sp.stringValue != cell)
                {
                    sp.stringValue = cell;
                    updatedCells++;
                }
            }
        }

        spNextFieldId.intValue = nextId;

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(table);
        return (updatedCells, addedKeys);
    }

    // ===== CSV helpers =====

    private static string Esc(string s)
    {
        if (s == null) return "\"\"";
        bool mustQuote =
            s.Contains(",") || s.Contains("\"") || s.Contains("\n") || s.Contains("\r");
        if (!mustQuote) return s;
        return "\"" + s.Replace("\"", "\"\"") + "\"";
    }

    private static List<string> SplitCsvLine(string line)
    {
        var result = new List<string>();
        if (line == null) return result;
        var sb = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    // Double quote inside quotes → escaped quote
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"'); i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else sb.Append(c);
            }
            else
            {
                if (c == ',')
                {
                    result.Add(sb.ToString());
                    sb.Length = 0;
                }
                else if (c == '"')
                {
                    inQuotes = true;
                }
                else sb.Append(c);
            }
        }
        result.Add(sb.ToString());
        return result;
    }
}


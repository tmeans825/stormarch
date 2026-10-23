// TreeBrushTool.cs
// Place this file inside any folder named "Editor" in your Assets directory.
// e.g. Assets/Editor/TreeBrushTool.cs

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class TreeBrushTool : EditorWindow
{
    // --- Palette ---
    private List<GameObject> prefabs = new List<GameObject>();
    private Vector2 paletteScroll;

    // --- Brush Settings ---
    private float brushRadius    = 2f;
    private float minSpacing     = 1f;
    private float minScale       = 0.9f;
    private float maxScale       = 1.1f;
    private bool  randomRotation = true;
    private bool  randomFlipX    = true;

    // --- Parent object (keeps hierarchy tidy) ---
    private GameObject brushParent;
    private string     parentName = "Trees";

    // --- Runtime state ---
    private bool      isPainting  = false;
    private Vector3   lastPlaced  = Vector3.positiveInfinity;
    private int       controlID;

    // -------------------------------------------------------------------------
    [MenuItem("Tools/Tree Brush Tool")]
    public static void OpenWindow()
    {
        GetWindow<TreeBrushTool>("Tree Brush");
    }

    // -------------------------------------------------------------------------
    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        LoadSettings();
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        SaveSettings();
        isPainting = false;
    }

    // -------------------------------------------------------------------------
    //  Persistence
    // -------------------------------------------------------------------------
    private const string PREF_PREFABS         = "TreeBrush_Prefabs";
    private const string PREF_BRUSH_RADIUS    = "TreeBrush_BrushRadius";
    private const string PREF_MIN_SPACING     = "TreeBrush_MinSpacing";
    private const string PREF_MIN_SCALE       = "TreeBrush_MinScale";
    private const string PREF_MAX_SCALE       = "TreeBrush_MaxScale";
    private const string PREF_RAND_ROTATION   = "TreeBrush_RandRotation";
    private const string PREF_RAND_FLIPX      = "TreeBrush_RandFlipX";
    private const string PREF_PARENT_NAME     = "TreeBrush_ParentName";

    private void SaveSettings()
    {
        // Prefabs — store as comma-separated GUIDs
        var guids = new List<string>();
        foreach (var p in prefabs)
        {
            if (p == null) continue;
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(p));
            if (!string.IsNullOrEmpty(guid))
                guids.Add(guid);
        }
        EditorPrefs.SetString(PREF_PREFABS,       string.Join(",", guids));

        // Brush settings
        EditorPrefs.SetFloat (PREF_BRUSH_RADIUS,  brushRadius);
        EditorPrefs.SetFloat (PREF_MIN_SPACING,   minSpacing);
        EditorPrefs.SetFloat (PREF_MIN_SCALE,     minScale);
        EditorPrefs.SetFloat (PREF_MAX_SCALE,     maxScale);
        EditorPrefs.SetInt   (PREF_RAND_ROTATION, randomRotation ? 1 : 0);
        EditorPrefs.SetInt   (PREF_RAND_FLIPX,    randomFlipX    ? 1 : 0);
        EditorPrefs.SetString(PREF_PARENT_NAME,   parentName);
    }

    private void LoadSettings()
    {
        // Prefabs
        string saved = EditorPrefs.GetString(PREF_PREFABS, "");
        prefabs.Clear();
        if (!string.IsNullOrEmpty(saved))
        {
            foreach (var guid in saved.Split(','))
            {
                if (string.IsNullOrEmpty(guid)) continue;
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go != null)
                    prefabs.Add(go);
            }
        }

        // Brush settings
        brushRadius    = EditorPrefs.GetFloat (PREF_BRUSH_RADIUS,  brushRadius);
        minSpacing     = EditorPrefs.GetFloat (PREF_MIN_SPACING,   minSpacing);
        minScale       = EditorPrefs.GetFloat (PREF_MIN_SCALE,     minScale);
        maxScale       = EditorPrefs.GetFloat (PREF_MAX_SCALE,     maxScale);
        randomRotation = EditorPrefs.GetInt   (PREF_RAND_ROTATION, randomRotation ? 1 : 0) == 1;
        randomFlipX    = EditorPrefs.GetInt   (PREF_RAND_FLIPX,    randomFlipX    ? 1 : 0) == 1;
        parentName     = EditorPrefs.GetString(PREF_PARENT_NAME,   parentName);
    }

    // -------------------------------------------------------------------------
    //  Editor Window UI
    // -------------------------------------------------------------------------
    private void OnGUI()
    {
        GUILayout.Label("🌲 Tree Brush Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // --- Prefab Palette ---
        GUILayout.Label("Prefab Palette", EditorStyles.boldLabel);

        paletteScroll = EditorGUILayout.BeginScrollView(paletteScroll, GUILayout.Height(120));
        for (int i = 0; i < prefabs.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            var prev = prefabs[i];
            prefabs[i] = (GameObject)EditorGUILayout.ObjectField(prefabs[i], typeof(GameObject), false);
            if (prefabs[i] != prev) SaveSettings();
            if (GUILayout.Button("✕", GUILayout.Width(24)))
            {
                prefabs.RemoveAt(i);
                SaveSettings();
                break;
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("+ Add Prefab Slot"))
        {
            prefabs.Add(null);
            SaveSettings();
        }

        EditorGUILayout.Space();

        EditorGUI.BeginChangeCheck();

        // --- Brush Settings ---
        GUILayout.Label("Brush Settings", EditorStyles.boldLabel);
        brushRadius    = EditorGUILayout.Slider("Brush Radius",   brushRadius,  0.5f, 20f);
        minSpacing     = EditorGUILayout.Slider("Min Spacing",    minSpacing,   0.1f, 10f);

        EditorGUILayout.Space();
        GUILayout.Label("Randomization", EditorStyles.boldLabel);
        minScale       = EditorGUILayout.Slider("Scale Min",      minScale,     0.1f, 3f);
        maxScale       = EditorGUILayout.Slider("Scale Max",      maxScale,     0.1f, 3f);
        randomRotation = EditorGUILayout.Toggle("Random Rotation", randomRotation);
        randomFlipX    = EditorGUILayout.Toggle("Random Flip X",   randomFlipX);

        EditorGUILayout.Space();

        // --- Parent Object ---
        GUILayout.Label("Hierarchy", EditorStyles.boldLabel);
        parentName = EditorGUILayout.TextField("Parent Name", parentName);

        if (EditorGUI.EndChangeCheck())
            SaveSettings();

        if (GUILayout.Button("Set / Create Parent Object"))
            EnsureParent();

        EditorGUILayout.Space();

        // --- Status ---
        GUI.color = isPainting ? Color.green : Color.white;
        GUILayout.Label(isPainting ? "● PAINTING  (hold LMB in Scene)" : "○ Idle  (hold LMB in Scene to paint)");
        GUI.color = Color.white;

        EditorGUILayout.HelpBox(
            "Hold LEFT MOUSE in the Scene view to paint.\n" +
            "Hold SHIFT to erase (removes prefabs in radius).",
            MessageType.Info);
    }

    // -------------------------------------------------------------------------
    //  Scene View Hook
    // -------------------------------------------------------------------------
    private void OnSceneGUI(SceneView sceneView)
    {
        if (prefabs.Count == 0) return;

        controlID = GUIUtility.GetControlID(FocusType.Passive);
        Event e   = Event.current;

        DrawBrushPreview(sceneView);

        // Erase mode: Shift held
        bool erasing = e.shift;

        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            GUIUtility.hotControl = controlID;
            isPainting  = true;
            lastPlaced  = Vector3.positiveInfinity;
            e.Use();
        }

        if (e.type == EventType.MouseUp && e.button == 0)
        {
            GUIUtility.hotControl = 0;
            isPainting  = false;
            lastPlaced  = Vector3.positiveInfinity;
            e.Use();
        }

        if (isPainting && (e.type == EventType.MouseDrag || e.type == EventType.MouseDown))
        {
            Vector3 worldPos = GetMouseWorldPos(e.mousePosition, sceneView);

            if (erasing)
            {
                EraseInRadius(worldPos);
            }
            else
            {
                // Only place if we've moved at least minSpacing since last placement
                if (Vector3.Distance(worldPos, lastPlaced) >= minSpacing)
                {
                    PlaceTree(worldPos);
                    lastPlaced = worldPos;
                }
            }

            e.Use();
        }

        // Keep the scene view refreshing while painting so the cursor circle updates
        if (isPainting)
            sceneView.Repaint();
    }

    // -------------------------------------------------------------------------
    //  Placement
    // -------------------------------------------------------------------------
    private void PlaceTree(Vector3 center)
    {
        // Filter out null slots
        List<GameObject> valid = prefabs.FindAll(p => p != null);
        if (valid.Count == 0) return;

        EnsureParent();

        // Pick a random position inside the brush radius
        Vector2 offset  = Random.insideUnitCircle * brushRadius;
        Vector3 spawnPos = new Vector3(center.x + offset.x, center.y + offset.y, center.z);

        // Pick a random prefab
        GameObject prefab = valid[Random.Range(0, valid.Count)];

        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.position   = spawnPos;
        go.transform.parent     = brushParent != null ? brushParent.transform : null;

        // Random scale
        float scale = Random.Range(minScale, maxScale);
        go.transform.localScale = new Vector3(scale, scale, scale);

        // Random rotation (Z axis for 2D)
        if (randomRotation)
            go.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));

        // Random flip on X
        if (randomFlipX && Random.value > 0.5f)
        {
            Vector3 s = go.transform.localScale;
            go.transform.localScale = new Vector3(-s.x, s.y, s.z);
        }

        Undo.RegisterCreatedObjectUndo(go, "Paint Tree");
    }

    // -------------------------------------------------------------------------
    //  Erasing
    // -------------------------------------------------------------------------
    private void EraseInRadius(Vector3 center)
    {
        if (brushParent == null) return;

        float r2 = brushRadius * brushRadius;
        var children = new List<Transform>();

        foreach (Transform child in brushParent.transform)
            children.Add(child);

        foreach (Transform child in children)
        {
            if ((child.position - center).sqrMagnitude <= r2)
            {
                Undo.DestroyObjectImmediate(child.gameObject);
            }
        }
    }

    // -------------------------------------------------------------------------
    //  Helpers
    // -------------------------------------------------------------------------
    private void EnsureParent()
    {
        if (brushParent != null && brushParent.name == parentName) return;

        brushParent = GameObject.Find(parentName);
        if (brushParent == null)
        {
            brushParent = new GameObject(parentName);
            Undo.RegisterCreatedObjectUndo(brushParent, "Create Tree Parent");
        }
    }

    private Vector3 GetMouseWorldPos(Vector2 mousePos, SceneView sv)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(mousePos);
        // For a 2D scene the plane is Z=0
        Plane plane = new Plane(Vector3.back, Vector3.zero);
        if (plane.Raycast(ray, out float dist))
            return ray.GetPoint(dist);
        return ray.GetPoint(10f);
    }

    private void DrawBrushPreview(SceneView sv)
    {
        Vector3 mouseWorld = GetMouseWorldPos(Event.current.mousePosition, sv);
        bool erasing       = Event.current.shift;

        Handles.color = erasing
            ? new Color(1f, 0.3f, 0.3f, 0.4f)
            : new Color(0.3f, 1f, 0.4f, 0.4f);

        Handles.DrawSolidDisc(mouseWorld, Vector3.back, brushRadius);

        Handles.color = erasing
            ? new Color(1f, 0.3f, 0.3f, 1f)
            : new Color(0.3f, 1f, 0.4f, 1f);

        Handles.DrawWireDisc(mouseWorld, Vector3.back, brushRadius);
    }
}

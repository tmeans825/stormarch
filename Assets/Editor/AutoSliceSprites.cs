// Assets/Editor/AutoSliceFromTemplate.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class AutoSliceFromTemplate
{
    // Folders (recursive)
    private const string ChestsFolder = "Assets/Art/Spritesheets/Resources/Chests";
    private const string LegsFolder = "Assets/Art/Spritesheets/Resources/Legs";
    private const string HairFolder = "Assets/Art/Spritesheets/Resources/Hair";
    private const string SkinFolder = "Assets/Art/Spritesheets/Resources/Skin";

    // OPTIONAL: pin exact templates (recommended) for CHESTS/LEGS only
    private const string ChestsTemplatePath = ChestsFolder + "/Chest_Base_1.png"; // e.g. "Assets/.../Chests/[template]_chest.png"
    private const string LegsTemplatePath = LegsFolder + "/legs_absconder_male.png"; // e.g. "Assets/.../Legs/[template]_legs.png"
    private const string SkinTemplatePath = SkinFolder + "/Head_1.png";

    // Hair fixed slicing config (exactly 4 sprites of 36x52)
    private const int HairTileW = 36;
    private const int HairTileH = 52;
    private const int HairCount = 4;
    private const int HairGroupIndex = 1; // Names: Hair_<group>_<frame> => Hair_1_0..Hair_1_3
    private const int HairFrameStart = 0;

    private const int DefaultPPU = 100;

    private static readonly Vector2 ChestPivot = new Vector2(0f, 0f);
    private static readonly Vector2 LegsPivot = new Vector2(0.5f, 0.5f);
    private static readonly Vector2 HairPivot = new Vector2(0.5f, 0.5f);
    private static readonly Vector2 SkinPivot = new Vector2(0f, 0f);

    // ===== MENU =====

    [MenuItem("Tools/Sprite Slicer/Auto Slice All")]
    public static void AutoSliceAll() => RunForFolders(
        new[]
        {
            new FolderCfg(ChestsFolder, "Chest", ChestsTemplatePath, ChestPivot, FolderMode.TemplateCopy),
            new FolderCfg(LegsFolder,   "Legs",  LegsTemplatePath,   LegsPivot,  FolderMode.TemplateCopy),
            new FolderCfg(HairFolder,   "Hair",  "",                 HairPivot,  FolderMode.FixedHair4),
            new FolderCfg(SkinFolder,   "Skin",  SkinTemplatePath,   SkinPivot,  FolderMode.TemplateCopy)
        },
        apply: true
    );

    [MenuItem("Tools/Sprite Slicer/Auto Slice/Chests")]
    public static void AutoSliceChests() => RunForFolders(
        new[] { new FolderCfg(ChestsFolder, "Chest", ChestsTemplatePath, ChestPivot, FolderMode.TemplateCopy) },
        apply: true
    );

    [MenuItem("Tools/Sprite Slicer/Auto Slice/Legs")]
    public static void AutoSliceLegs() => RunForFolders(
        new[] { new FolderCfg(LegsFolder, "Legs", LegsTemplatePath, LegsPivot, FolderMode.TemplateCopy) },
        apply: true
    );

    [MenuItem("Tools/Sprite Slicer/Auto Slice/Hair")]
    public static void AutoSliceHair() => RunForFolders(
        new[] { new FolderCfg(HairFolder, "Hair", "", HairPivot, FolderMode.FixedHair4) },
        apply: true
    );

    [MenuItem("Tools/Sprite Slicer/Auto Slice/Skin")]
    public static void AutoSliceSkin() => RunForFolders(
        new[] { new FolderCfg(SkinFolder, "Skin", SkinTemplatePath, SkinPivot, FolderMode.TemplateCopy) },
        apply: true
    );

    [MenuItem("Tools/Sprite Slicer/Dry Run (log templates & hair fits)")]
    public static void DryRun() => RunForFolders(
        new[]
        {
            new FolderCfg(ChestsFolder, "Chest", ChestsTemplatePath, ChestPivot, FolderMode.TemplateCopy),
            new FolderCfg(LegsFolder,   "Legs",  LegsTemplatePath,   LegsPivot,  FolderMode.TemplateCopy),
            new FolderCfg(HairFolder,   "Hair",  "",                 HairPivot,  FolderMode.FixedHair4),
            new FolderCfg(SkinFolder,   "Skin",  SkinTemplatePath,   SkinPivot,  FolderMode.TemplateCopy)
        },
        apply: false
    );

    // ===== CORE =====

    private static void RunForFolders(IEnumerable<FolderCfg> folders, bool apply)
    {
        int scanned = 0, sliced = 0;
        var notes = new List<string>();

        try
        {
            if (apply) AssetDatabase.StartAssetEditing();

            foreach (var f in folders)
            {
                TemplateInfo template = null;

                if (f.Mode == FolderMode.TemplateCopy)
                {
                    template = ResolveTemplate(f, notes);
                    if (template == null)
                    {
                        notes.Add($"No suitable template for '{f.Prefix}' in '{f.Folder}'. " +
                                  $"Pin a template path or tag a file name with [template].");
                        continue;
                    }
                    var tTex = AssetDatabase.LoadAssetAtPath<Texture2D>(template.Path);
                    if (!tTex) { notes.Add($"Template texture missing at '{template.Path}'."); continue; }
                    Debug.Log($"[Slice] {f.Prefix}: using template {template.Path} (sprites: {template.Importer.spritesheet.Length})");
                }

                var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { f.Folder });
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    if (!tex) continue;
                    scanned++;

                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer == null) { notes.Add($"Not a TextureImporter: {path}"); continue; }

                    // Skip already-sliced textures
                    if (importer.spriteImportMode == SpriteImportMode.Multiple &&
                        importer.spritesheet != null &&
                        importer.spritesheet.Length > 0)
                    {
                        continue;
                    }

                    if (f.Mode == FolderMode.TemplateCopy)
                    {
                        var count = ApplyTemplateSlices(template, tex, importer, apply);
                        if (count > 0) sliced += count;
                    }
                    else if (f.Mode == FolderMode.FixedHair4)
                    {
                        var count = ApplyFixedHairSlices(tex, importer, apply, notes);
                        if (count > 0) sliced += count;
                    }
                }
            }
        }
        finally
        {
            if (apply) { AssetDatabase.StopAssetEditing(); AssetDatabase.Refresh(); }
        }

        var mode = apply ? "APPLIED" : "DRY RUN";
        Debug.Log($"[AutoSlice] {mode} — scanned: {scanned}, new slices: {sliced}. " +
                  (notes.Count > 0 ? $"Notes: {notes.Count} (see Console)" : "No notes."));
        foreach (var n in notes) Debug.LogWarning("[AutoSlice] " + n);
    }

    // ===== Template copy (Chests/Legs) =====

    private static int ApplyTemplateSlices(TemplateInfo template, Texture2D target, TextureImporter importer, bool apply)
    {
        var existing = importer.spritesheet;
        if (importer.spriteImportMode == SpriteImportMode.Multiple &&
            existing != null &&
            existing.Length > 0)
        {
            // Already sliced, skip entirely (no "Would slice" logs)
            return 0;
        }

        var tImporter = template.Importer;
        var tRects = tImporter.spritesheet;

        var tTex = AssetDatabase.LoadAssetAtPath<Texture2D>(template.Path);
        int tW = tTex.width, tH = tTex.height;
        int w = target.width, h = target.height;

        if (tW <= 0 || tH <= 0 || w <= 0 || h <= 0) return 0;

        float sx = (float)w / tW, sy = (float)h / tH;

        var copied = new List<SpriteMetaData>(tRects.Length);
        for (int i = 0; i < tRects.Length; i++)
        {
            var tr = tRects[i];
            var r = tr.rect;
            var scaled = new Rect(r.x * sx, r.y * sy, r.width * sx, r.height * sy);

            // clamp inside (avoid rounding issues)
            scaled.x = Mathf.Clamp(scaled.x, 0, Mathf.Max(0, w - 1));
            scaled.y = Mathf.Clamp(scaled.y, 0, Mathf.Max(0, h - 1));
            scaled.width = Mathf.Min(scaled.width, w - scaled.x);
            scaled.height = Mathf.Min(scaled.height, h - scaled.y);

            copied.Add(new SpriteMetaData
            {
                name = tr.name,
                rect = scaled,
                alignment = tr.alignment,
                pivot = tr.pivot,
                border = tr.border
            });
        }

        if (!apply) { Debug.Log($"[DryRun] Would slice (template) {AssetDatabase.GetAssetPath(target)} with {copied.Count} rects."); return 0; }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = DefaultPPU;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.spritesheet = copied.ToArray();
        importer.SaveAndReimport();
        Debug.Log($"[Slice] {AssetDatabase.GetAssetPath(target)}: applied {copied.Count} template slices.");
        return copied.Count;
    }

    // ===== Fixed Hair: exactly 4 sprites of 36x52 =====

    private static int ApplyFixedHairSlices(Texture2D tex, TextureImporter importer, bool apply, List<string> notes)
    {
        var existing = importer.spritesheet;
        if (importer.spriteImportMode == SpriteImportMode.Multiple &&
            existing != null &&
            existing.Length > 0)
        {
            // Already sliced, skip entirely (no "Would slice" logs)
            return 0;
        }

        // Base “tile” (strip) size; used only for strip cases
        const int StripTileW = 36;
        const int StripTileH = 52;
        const int Count = 4;

        int w = tex.width, h = tex.height;

        // Case A: 2x2 grid in a 36x52 sheet (each tile 18x26)
        if (w == 36 && h == 52)
        {
            int tileW = 18, tileH = 26;
            var metas = new List<SpriteMetaData>(Count);

            // Order: left->right, top->bottom (Unity is bottom-left origin, so flip Y)
            for (int row = 0; row < 2; row++)
            {
                for (int col = 0; col < 2; col++)
                {
                    int i = row * 2 + col; // 0..3
                    int x = col * tileW;
                    // y from top-left to bottom-left
                    int yTop = row * tileH;
                    int y = h - (yTop + tileH);

                    metas.Add(new SpriteMetaData
                    {
                        name = $"Hair_{HairGroupIndex}_{HairFrameStart + i}",
                        rect = new Rect(x, y, tileW, tileH),
                        alignment = (int)SpriteAlignment.Custom,
                        pivot = HairPivot,
                        border = Vector4.zero
                    });
                }
            }

            if (!apply)
            {
                Debug.Log($"[DryRun][Hair] Would slice {AssetDatabase.GetAssetPath(tex)} as 2x2 grid of 18x26.");
                return 0;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = DefaultPPU;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.spritesheet = metas.ToArray();
            importer.SaveAndReimport();
            Debug.Log($"[Hair] Sliced {AssetDatabase.GetAssetPath(tex)} into 4 frames (2x2 of 18x26).");
            return metas.Count;
        }

        // Case B: 4x1 strip (36x52 tiles)
        if (w == StripTileW * Count && h == StripTileH)
        {
            var metas = new List<SpriteMetaData>(Count);
            for (int i = 0; i < Count; i++)
            {
                int x = i * StripTileW;
                int y = 0; // already bottom aligned

                metas.Add(new SpriteMetaData
                {
                    name = $"Hair_{HairGroupIndex}_{HairFrameStart + i}",
                    rect = new Rect(x, y, StripTileW, StripTileH),
                    alignment = (int)SpriteAlignment.Custom,
                    pivot = HairPivot,
                    border = Vector4.zero
                });
            }

            if (!apply)
            {
                Debug.Log($"[DryRun][Hair] Would slice {AssetDatabase.GetAssetPath(tex)} as 4x1 strip of 36x52.");
                return 0;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = DefaultPPU;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.spritesheet = metas.ToArray();
            importer.SaveAndReimport();
            Debug.Log($"[Hair] Sliced {AssetDatabase.GetAssetPath(tex)} into 4 frames (4x1 of 36x52).");
            return metas.Count;
        }

        // Case C: 1x4 strip (36x52 tiles)
        if (w == StripTileW && h == StripTileH * Count)
        {
            var metas = new List<SpriteMetaData>(Count);
            for (int i = 0; i < Count; i++)
            {
                int x = 0;
                // flip Y from top to bottom
                int y = h - (i + 1) * StripTileH;

                metas.Add(new SpriteMetaData
                {
                    name = $"Hair_{HairGroupIndex}_{HairFrameStart + i}",
                    rect = new Rect(x, y, StripTileW, StripTileH),
                    alignment = (int)SpriteAlignment.Custom,
                    pivot = HairPivot,
                    border = Vector4.zero
                });
            }

            if (!apply)
            {
                Debug.Log($"[DryRun][Hair] Would slice {AssetDatabase.GetAssetPath(tex)} as 1x4 strip of 36x52.");
                return 0;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = DefaultPPU;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.spritesheet = metas.ToArray();
            importer.SaveAndReimport();
            Debug.Log($"[Hair] Sliced {AssetDatabase.GetAssetPath(tex)} into 4 frames (1x4 of 36x52).");
            return metas.Count;
        }

        // Not a recognized hair layout — skip safely
        notes.Add($"Hair not sliced (unrecognized size {w}x{h}). Expected 36x52 (2x2 of 18x26), 144x52 (4x1), or 36x208 (1x4): {AssetDatabase.GetAssetPath(tex)}");
        return 0;
    }


    // ===== Template selection (Chests/Legs) =====

    private class TemplateInfo { public string Path; public TextureImporter Importer; }
    private enum FolderMode { TemplateCopy, FixedHair4 }

    private readonly struct FolderCfg
    {
        public readonly string Folder;
        public readonly string Prefix;
        public readonly string ExplicitTemplatePath;
        public readonly Vector2 ExpectedPivot;
        public readonly FolderMode Mode;

        public FolderCfg(string folder, string prefix, string explicitTemplatePath, Vector2 expectedPivot, FolderMode mode)
        {
            Folder = folder.Replace('\\', '/');
            Prefix = prefix;
            ExplicitTemplatePath = string.IsNullOrWhiteSpace(explicitTemplatePath) ? "" : explicitTemplatePath.Replace('\\', '/');
            ExpectedPivot = expectedPivot;
            Mode = mode;
        }
    }

    private static TemplateInfo ResolveTemplate(FolderCfg f, List<string> notes)
    {
        // 1) explicit
        if (!string.IsNullOrEmpty(f.ExplicitTemplatePath))
        {
            var imp = AssetImporter.GetAtPath(f.ExplicitTemplatePath) as TextureImporter;
            if (IsSliced(imp)) return new TemplateInfo { Path = f.ExplicitTemplatePath, Importer = imp };
            notes.Add($"Explicit template for {f.Prefix} is not valid/sliced: '{f.ExplicitTemplatePath}'");
        }

        // 2) filename tag
        var tagged = FindTaggedTemplateByName(f.Folder);
        if (tagged != null) return tagged;

        // 3) heuristic
        var scored = FindBestTemplateHeuristic(f.Folder, f.Prefix, f.ExpectedPivot);
        if (scored != null) return scored;

        return null;
    }

    private static TemplateInfo FindTaggedTemplateByName(string folder)
    {
        var tags = new[] { "[template]", "_template", "template" };
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var file = Path.GetFileNameWithoutExtension(path);
            if (!tags.Any(t => file.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0)) continue;

            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (IsSliced(imp)) return new TemplateInfo { Path = path, Importer = imp };
        }
        return null;
    }

    private static TemplateInfo FindBestTemplateHeuristic(string folder, string prefix, Vector2 expectedPivot)
    {
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
        TemplateInfo best = null;
        float bestScore = float.NegativeInfinity;

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (!IsSliced(imp)) continue;

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (!tex) continue;

            var sheet = imp.spritesheet;
            int n = sheet.Length;
            if (n == 0) continue;

            float score = 0;
            score += n * 10f;
            int nameMatches = sheet.Count(s => s.name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            score += (float)nameMatches / Mathf.Max(1, n) * 5f;
            int pivotMatches = sheet.Count(s => (s.pivot - expectedPivot).sqrMagnitude < 1e-6f);
            score += (float)pivotMatches / Mathf.Max(1, n) * 3f;

            var key = sheet.Select(s => new Vector2(Mathf.Round(s.rect.width), Mathf.Round(s.rect.height)))
                           .GroupBy(v => v)
                           .OrderByDescending(g => g.Count())
                           .First().Key;
            int sizeMatches = sheet.Count(s => Mathf.Round(s.rect.width) == key.x && Mathf.Round(s.rect.height) == key.y);
            score += (float)sizeMatches / Mathf.Max(1, n) * 4f;

            if (score > bestScore)
            {
                bestScore = score;
                best = new TemplateInfo { Path = path, Importer = imp };
            }
        }
        return best;
    }

    private static bool IsSliced(TextureImporter imp) =>
        imp != null &&
        imp.spriteImportMode == SpriteImportMode.Multiple &&
        imp.spritesheet != null &&
        imp.spritesheet.Length > 0;
}

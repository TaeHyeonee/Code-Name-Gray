// FrontierMaterialHelper.cs
// Place in: Assets/Editor/ (or any Editor folder)
//
// Purpose:
// Batch-remap materials on prefabs in a selected prefab folder by swapping a token segment in the material name,
// e.g. AnyPrefix_EngineerYellow_Clean_URP  ->  AnyPrefix_MedicRed_Clean_URP

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace ML.Frontier.Editor
{
    public class FrontierMaterialHelper : EditorWindow
    {
        private enum RenderPipeline
        {
            URP,
            HDRP
        }

        [MenuItem("Tools/ML/Material Helper (Token Remap)")]
        public static void ShowWindow()
        {
            var window = GetWindow<FrontierMaterialHelper>("Material Helper (Token Remap)");
            window.minSize = new Vector2(520, 620);
            window.InitializeDefaultsIfNeeded();
        }

        // ---------- UI state ----------

        [SerializeField] private RenderPipeline renderPipeline = RenderPipeline.URP; // purely informational unless you use it for presets
        [SerializeField] private DefaultAsset prefabFolder;

        // Generic: allow N folders (not “core + asset”)
        [SerializeField] private List<DefaultAsset> allowedMaterialFolders = new List<DefaultAsset>();

        [SerializeField] private string oldToken = "EngineerYellow";
        [SerializeField] private string newToken = "MedicRed";

        [SerializeField] private bool dryRun = true;

        // Advanced behavior toggles
        [SerializeField] private bool caseSensitiveTokenMatch = true;   // token segment match
        [SerializeField] private bool lookupCaseInsensitive = true;     // material lookup keying
        [SerializeField] private bool logMissingTargets = false;        // per-miss log (can be noisy)

        // ---------- last run summary ----------
        private int lastPrefabsProcessed;
        private int lastPrefabsWouldSave;   // Dry-run: how many prefabs would be saved
        private int lastPrefabsSaved;       // Real run: how many prefabs were saved
        private int lastRenderersTouched;
        private int lastMaterialsWouldChange;
        private int lastMaterialsChanged;
        private int lastMissingTargetMaterials;
        private int lastSkippedDueToNoToken; // materials inspected that didn’t contain token segment

        // Material cache (built once per run)
        private Dictionary<string, Material> materialLookup;

        // Collapsed UI state
        [SerializeField] private bool showAdvanced = false;

        // ---------- GUI ----------

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Material Helper (Token Remap)", EditorStyles.boldLabel);
            DrawUsageHelp();

            EditorGUILayout.Space(8);
            DrawSettingsSection();

            EditorGUILayout.Space(8);
            DrawPrefabFolderSection();

            EditorGUILayout.Space(8);
            DrawAllowedFoldersSection();

            EditorGUILayout.Space(10);
            DrawVariantSection();

            EditorGUILayout.Space(10);
            DrawAdvancedSection();

            EditorGUILayout.Space(12);

            using (new EditorGUI.DisabledScope(!CanRun()))
            {
                if (GUILayout.Button(dryRun ? "Run (Dry Run)" : "Run (Apply Changes)", GUILayout.Height(36)))
                {
                    Run();
                }
            }

            DrawLastRunSummary();
        }

        private void DrawUsageHelp()
        {
            EditorGUILayout.HelpBox(
                "What this does:\n" +
                "• Scans all prefabs inside the selected Prefab Folder.\n" +
                "• For each renderer material, replaces the token segment \"_OldToken_\" → \"_NewToken_\" in the material name.\n" +
                "• Assigns the matching target material, but only if it exists inside the Allowed Material Folders.\n\n" +
                "Typical usage:\n" +
                "1) Select Prefab Folder.\n" +
                "2) Add one or more Allowed Material Folders (the only places the tool will search).\n" +
                "3) Set Old token + New token.\n" +
                "4) Dry Run first, then Apply.",
                MessageType.Info
            );
        }

        private void DrawSettingsSection()
        {
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);

            // Keep pipeline selector as a simple label/convenience for your own mental model;
            // not tied to any defaults or enforced behavior.
            renderPipeline = (RenderPipeline)EditorGUILayout.EnumPopup(
                new GUIContent("Pipeline Label", "Optional label for your current context. This tool does not enforce pipeline naming."),
                renderPipeline
            );

            dryRun = EditorGUILayout.ToggleLeft(
                new GUIContent("Dry Run (no assets saved)", "When enabled, the tool reports what it WOULD change without saving prefabs."),
                dryRun
            );
        }

        private void DrawPrefabFolderSection()
        {
            EditorGUILayout.LabelField("Target Prefabs", EditorStyles.boldLabel);

            prefabFolder = (DefaultAsset)EditorGUILayout.ObjectField(
                "Prefab Folder",
                prefabFolder,
                typeof(DefaultAsset),
                false
            );
        }

        private void DrawAllowedFoldersSection()
        {
            EditorGUILayout.LabelField("Allowed Material Folders", EditorStyles.boldLabel);

            if (allowedMaterialFolders == null)
                allowedMaterialFolders = new List<DefaultAsset>();

            // Ensure at least one slot exists for UX
            if (allowedMaterialFolders.Count == 0)
                allowedMaterialFolders.Add(null);

            int removeIndex = -1;

            for (int i = 0; i < allowedMaterialFolders.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                allowedMaterialFolders[i] = (DefaultAsset)EditorGUILayout.ObjectField(
                    $"Folder {i + 1}",
                    allowedMaterialFolders[i],
                    typeof(DefaultAsset),
                    false
                );

                if (GUILayout.Button("-", GUILayout.Width(26)))
                    removeIndex = i;

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+ Add Folder", GUILayout.Width(120)))
                allowedMaterialFolders.Add(null);
            EditorGUILayout.EndHorizontal();

            if (removeIndex >= 0)
            {
                allowedMaterialFolders.RemoveAt(removeIndex);
                if (allowedMaterialFolders.Count == 0)
                    allowedMaterialFolders.Add(null);
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.HelpBox(
                "Only materials inside these folders are eligible targets. This prevents accidental matches from other packages.",
                MessageType.None
            );
        }

        private void DrawVariantSection()
        {
            EditorGUILayout.LabelField("Variant Remap", EditorStyles.boldLabel);

            oldToken = EditorGUILayout.TextField("Old token", oldToken);
            newToken = EditorGUILayout.TextField("New token", newToken);

            EditorGUILayout.HelpBox(
                (caseSensitiveTokenMatch ? "Case-sensitive token segment match.\n" : "Case-insensitive token segment match.\n") +
                "Matches materials that contain \"_" + oldToken + "_\" and replaces that segment with \"_" + newToken + "_\".\n" +
                "Example:\n" +
                "  AnyPrefix_" + oldToken + "_Clean_URP\n" +
                "→ AnyPrefix_" + newToken + "_Clean_URP",
                MessageType.None
            );
        }

        private void DrawAdvancedSection()
        {
            showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Advanced", true);
            if (!showAdvanced) return;

            EditorGUI.indentLevel++;

            caseSensitiveTokenMatch = EditorGUILayout.Toggle(
                new GUIContent("Case-sensitive token match", "If off, token segment match is case-insensitive."),
                caseSensitiveTokenMatch
            );

            lookupCaseInsensitive = EditorGUILayout.Toggle(
                new GUIContent("Case-insensitive lookup", "If on, material lookup keys are case-insensitive (recommended)."),
                lookupCaseInsensitive
            );

            logMissingTargets = EditorGUILayout.Toggle(
                new GUIContent("Log missing targets (noisy)", "Logs each missing target material name during run."),
                logMissingTargets
            );

            EditorGUI.indentLevel--;
        }

        private void DrawLastRunSummary()
        {
            if (lastPrefabsProcessed <= 0) return;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Last Run Summary", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Prefabs processed:            {lastPrefabsProcessed}");
            EditorGUILayout.LabelField($"Prefabs would save:           {lastPrefabsWouldSave}");
            EditorGUILayout.LabelField($"Prefabs saved:                {lastPrefabsSaved}");
            EditorGUILayout.LabelField($"Renderers touched:            {lastRenderersTouched}");
            EditorGUILayout.LabelField($"Materials would change:       {lastMaterialsWouldChange}");
            EditorGUILayout.LabelField($"Materials changed:            {lastMaterialsChanged}");
            EditorGUILayout.LabelField($"Missing target materials:     {lastMissingTargetMaterials}");
            EditorGUILayout.LabelField($"Skipped (no token segment):   {lastSkippedDueToNoToken}");
        }

        // ---------- Run ----------

        private bool CanRun()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return false;

            if (prefabFolder == null)
                return false;

            if (string.IsNullOrWhiteSpace(oldToken) || string.IsNullOrWhiteSpace(newToken))
                return false;

            if (string.Equals(oldToken.Trim(), newToken.Trim(),
                    caseSensitiveTokenMatch ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase))
                return false;

            string pf = AssetDatabase.GetAssetPath(prefabFolder);
            if (string.IsNullOrEmpty(pf) || !AssetDatabase.IsValidFolder(pf))
                return false;

            if (allowedMaterialFolders == null || allowedMaterialFolders.Count == 0)
                return false;

            bool hasAtLeastOneValidFolder = false;
            foreach (var f in allowedMaterialFolders)
            {
                if (f == null) continue;
                string p = AssetDatabase.GetAssetPath(f);
                if (!string.IsNullOrEmpty(p) && AssetDatabase.IsValidFolder(p))
                {
                    hasAtLeastOneValidFolder = true;
                    break;
                }
            }

            return hasAtLeastOneValidFolder;
        }

        private void Run()
        {
            string prefabFolderPath = AssetDatabase.GetAssetPath(prefabFolder);

            // Build lookup strictly from allowed folders.
            BuildMaterialLookupFromAllowedFolders();

            lastPrefabsProcessed = 0;
            lastPrefabsWouldSave = 0;
            lastPrefabsSaved = 0;
            lastRenderersTouched = 0;
            lastMaterialsWouldChange = 0;
            lastMaterialsChanged = 0;
            lastMissingTargetMaterials = 0;
            lastSkippedDueToNoToken = 0;

            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { prefabFolderPath });
            if (prefabGuids == null || prefabGuids.Length == 0)
            {
                EditorUtility.DisplayDialog("Material Helper (Token Remap)", "No prefabs found in the selected folder.", "OK");
                return;
            }

            int total = prefabGuids.Length;

            try
            {
                for (int idx = 0; idx < total; idx++)
                {
                    string guid = prefabGuids[idx];
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (string.IsNullOrEmpty(path)) continue;

                    lastPrefabsProcessed++;

                    EditorUtility.DisplayProgressBar(
                        "Material Helper (Token Remap)",
                        $"{(dryRun ? "Dry Run" : "Applying")} {idx + 1}/{total}\n{path}",
                        (idx + 1) / (float)total
                    );

                    GameObject prefabRoot;
                    try
                    {
                        prefabRoot = PrefabUtility.LoadPrefabContents(path);
                    }
                    catch
                    {
                        continue;
                    }

                    if (prefabRoot == null) continue;

                    bool assetWouldChange = false;
                    bool assetChanged = false;

                    var renderers = prefabRoot.GetComponentsInChildren<Renderer>(true);
                    foreach (var rend in renderers)
                    {
                        if (rend == null) continue;

                        var shared = rend.sharedMaterials;
                        bool rendererWouldChange = false;
                        bool rendererChanged = false;

                        for (int i = 0; i < shared.Length; i++)
                        {
                            var currentMat = shared[i];
                            if (currentMat == null) continue;

                            if (!ContainsTokenSegment(currentMat.name, oldToken, caseSensitiveTokenMatch))
                            {
                                lastSkippedDueToNoToken++;
                                continue;
                            }

                            string targetName = ReplaceTokenSegment(currentMat.name, oldToken, newToken, caseSensitiveTokenMatch);
                            if (string.IsNullOrEmpty(targetName))
                                continue;

                            if (!materialLookup.TryGetValue(targetName, out var targetMat) || targetMat == null)
                            {
                                lastMissingTargetMaterials++;
                                if (logMissingTargets)
                                    Debug.LogWarning($"[MaterialHelper] Missing target: '{targetName}' (from '{currentMat.name}')");
                                continue;
                            }

                            if (targetMat != currentMat)
                            {
                                rendererWouldChange = true;
                                assetWouldChange = true;
                                lastMaterialsWouldChange++;

                                if (!dryRun)
                                {
                                    shared[i] = targetMat;
                                    rendererChanged = true;
                                    assetChanged = true;
                                    lastMaterialsChanged++;
                                }
                            }
                        }

                        if (rendererWouldChange)
                            lastRenderersTouched++;

                        if (!dryRun && rendererChanged)
                        {
                            rend.sharedMaterials = shared;
                            EditorUtility.SetDirty(rend);
                        }
                    }

                    if (assetWouldChange)
                        lastPrefabsWouldSave++;

                    if (!dryRun && assetChanged)
                    {
                        PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
                        lastPrefabsSaved++;
                    }

                    PrefabUtility.UnloadPrefabContents(prefabRoot);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                if (!dryRun)
                    AssetDatabase.SaveAssets();
            }

            Debug.Log(
                $"[MaterialHelper] {(dryRun ? "Dry Run" : "Applied")} complete. " +
                $"Prefabs processed: {lastPrefabsProcessed}, would-save: {lastPrefabsWouldSave}, saved: {lastPrefabsSaved}, " +
                $"renderers touched: {lastRenderersTouched}, would-change: {lastMaterialsWouldChange}, changed: {lastMaterialsChanged}, " +
                $"missing targets: {lastMissingTargetMaterials}, lookup entries: {(materialLookup != null ? materialLookup.Count : 0)}"
            );

            EditorUtility.DisplayDialog(
                "Material Helper (Token Remap)",
                (dryRun ? "Dry Run complete.\n\n" : "Done.\n\n") +
                $"Prefabs processed:          {lastPrefabsProcessed}\n" +
                $"Prefabs would save:         {lastPrefabsWouldSave}\n" +
                $"Prefabs saved:              {lastPrefabsSaved}\n" +
                $"Renderers touched:          {lastRenderersTouched}\n" +
                $"Materials would change:     {lastMaterialsWouldChange}\n" +
                $"Materials changed:          {lastMaterialsChanged}\n" +
                $"Missing target materials:   {lastMissingTargetMaterials}\n" +
                $"Skipped (no token segment): {lastSkippedDueToNoToken}",
                "OK"
            );
        }

        // ---------- Material lookup (STRICT) ----------

        private void BuildMaterialLookupFromAllowedFolders()
        {
            var comparer = lookupCaseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
            materialLookup = new Dictionary<string, Material>(comparer);

            int scannedFolders = 0;
            int scannedMaterials = 0;
            int duplicates = 0;

            foreach (var folderAsset in allowedMaterialFolders)
            {
                if (folderAsset == null) continue;

                string folderPath = AssetDatabase.GetAssetPath(folderAsset);
                if (string.IsNullOrEmpty(folderPath) || !AssetDatabase.IsValidFolder(folderPath))
                    continue;

                scannedFolders++;
                string[] guids = AssetDatabase.FindAssets("t:Material", new[] { folderPath });

                foreach (var g in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(g);
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (mat == null) continue;

                    scannedMaterials++;

                    // Keep first occurrence to avoid “random” overrides.
                    if (!materialLookup.ContainsKey(mat.name))
                        materialLookup.Add(mat.name, mat);
                    else
                        duplicates++;
                }
            }

            Debug.Log(
                $"[MaterialHelper] Material lookup built from {scannedFolders} folder(s). " +
                $"Materials scanned: {scannedMaterials}, entries: {materialLookup.Count}, duplicates ignored: {duplicates}. " +
                $"Lookup case-insensitive: {lookupCaseInsensitive}"
            );
        }

        // ---------- Token helpers (segment-based) ----------

        private static bool ContainsTokenSegment(string materialName, string token, bool caseSensitive)
        {
            if (string.IsNullOrEmpty(materialName) || string.IsNullOrEmpty(token))
                return false;

            string segment = "_" + token + "_";
            return materialName.IndexOf(segment, caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string ReplaceTokenSegment(string materialName, string oldToken, string newToken, bool caseSensitive)
        {
            if (string.IsNullOrEmpty(materialName) || string.IsNullOrEmpty(oldToken) || string.IsNullOrEmpty(newToken))
                return null;

            // Replace only occurrences of "_OldToken_" → "_NewToken_"
            // If case-insensitive mode is enabled, we still replace the matched segment with the exact newToken casing.
            string escaped = Regex.Escape(oldToken);
            string pattern = "_" + escaped + "_";
            string replacement = "_" + newToken + "_";

            var options = caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;

            if (!Regex.IsMatch(materialName, pattern, options))
                return null;

            return Regex.Replace(materialName, pattern, replacement, options);
        }

        // ---------- Defaults ----------

        private void InitializeDefaultsIfNeeded()
        {
            // Generic behavior: do nothing. Keep whatever the user serialized.
            // Optionally ensure list is not null.
            if (allowedMaterialFolders == null)
                allowedMaterialFolders = new List<DefaultAsset>();
            if (allowedMaterialFolders.Count == 0)
                allowedMaterialFolders.Add(null);
        }
    }
}

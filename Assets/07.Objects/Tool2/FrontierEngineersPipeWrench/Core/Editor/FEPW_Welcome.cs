using UnityEditor;
using UnityEngine;
using System.IO;

namespace ML.Frontier.Editor
{
    public class FrontierEngineersPipeWrenchWelcomeWindow : EditorWindow
    {
        private const string HasShownKey = "FrontierEngineersPipeWrench_WelcomeShown";

        private static readonly string[] ReadmeFolders = new[]
        {
            "Assets/ML/FrontierEngineersPipeWrench/Core/Documentation",
            "Assets/ML/Frontier_EngineersPipeWrench_URP/Documentation",
            "Assets/ML/Frontier_EngineersPipeWrench_HDRP/Documentation",
            "Assets/ML/FrontierEngineersPipeWrench/Documentation",
            "Assets/ML/Frontier_EngineersPipeWrench/Documentation",
        };

        private const string ReadmePrefix = "Frontier_EngineersPipeWrench_Unity_Readme_";

        private const string PublisherPageURL = "https://assetstore.unity.com/publishers/96895";
        private const string ToolboxURL = "https://assetstore.unity.com/packages/3d/props/tools/frontier-engineer-s-toolbox-351206";

        // Increased size
        private static readonly Vector2 WindowSize = new Vector2(560, 350);

        private GUIStyle authorStyle;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private bool stylesReady;

        [InitializeOnLoadMethod]
        private static void ShowOnLoad()
        {
            if (!EditorPrefs.GetBool(HasShownKey, false) &&
                !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += () =>
                {
                    var window = GetWindow<FrontierEngineersPipeWrenchWelcomeWindow>(
                        true,
                        "Frontier – Engineer’s Pipe Wrench");

                    CenterWindow(window, WindowSize);
                    window.ShowPopup();
                    EditorPrefs.SetBool(HasShownKey, true);
                };
            }
        }

        private void OnEnable()
        {
            stylesReady = false;
        }

        private void EnsureStyles()
        {
            if (stylesReady) return;

            if (EditorStyles.boldLabel == null || EditorStyles.label == null || EditorStyles.wordWrappedLabel == null)
                return;

            titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };

            bodyStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
            {
                fontSize = 12,
                alignment = TextAnchor.UpperLeft,
                wordWrap = true,
                margin = new RectOffset(16, 16, 0, 0)
            };

            authorStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Italic,
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = Color.gray }
            };

            stylesReady = true;
        }

        private void OnGUI()
        {
            EnsureStyles();

            if (!stylesReady)
            {
                GUILayout.Label("Loading...", EditorStyles.label);
                Repaint();
                return;
            }

            GUILayout.BeginVertical("box");
            GUILayout.Space(14);

            GUILayout.Label("Frontier – Engineer’s Pipe Wrench", titleStyle);
            GUILayout.Space(12);

            GUILayout.Label(
                "Thank you for using Frontier – Engineer’s Pipe Wrench.\n\n" +
                "This pack provides a production-ready engineering tool prop designed for repair, " +
                "maintenance, and environmental storytelling. It demonstrates Frontier material quality, " +
                "LOD structure, and packaging standards for URP and HDRP.\n\n" +
                "To get started:\n" +
                "• Open the Readme for setup details and pipeline notes\n" +
                "• Open the included sample scene for a quick visual preview\n" +
                "• Drag the Pipe Wrench prefab into your scene\n" +
                "• Adjust material parameters in the Inspector if needed\n\n" +
                "For a complete engineering tool set in the Frontier Series, see Frontier Engineer’s Toolbox.",
                bodyStyle);

            GUILayout.Space(8);
            GUILayout.EndVertical();

            Rect lastRect = GUILayoutUtility.GetLastRect();
            Rect authorRect = new Rect(
                lastRect.x + 12,
                lastRect.yMax + 8,
                position.width - 24,
                18);

            EditorGUI.LabelField(authorRect, "// Martin Ljungblad (ML)", authorStyle);

            GUILayout.Space(36);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Open Readme", GUILayout.Width(130), GUILayout.Height(34)))
                OpenReadme();

            GUILayout.Space(12);

            if (GUILayout.Button("Toolbox", GUILayout.Width(120), GUILayout.Height(34)))
                Application.OpenURL(ToolboxURL);

            GUILayout.Space(12);

            if (GUILayout.Button("Publisher Page", GUILayout.Width(150), GUILayout.Height(34)))
                Application.OpenURL(PublisherPageURL);

            GUILayout.Space(12);

            if (GUILayout.Button("Close", GUILayout.Width(110), GUILayout.Height(34)))
                Close();

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(16);
        }

        private void OpenReadme()
        {
            string latestReadme = null;

            foreach (var folder in ReadmeFolders)
            {
                latestReadme = FindLatestReadmeInFolder(folder);
                if (!string.IsNullOrEmpty(latestReadme))
                    break;
            }

            if (string.IsNullOrEmpty(latestReadme))
            {
                Debug.LogWarning(
                    "[Frontier – Engineer’s Pipe Wrench] No README PDF found.\n" +
                    "Searched:\n- " + string.Join("\n- ", ReadmeFolders) + "\n" +
                    "Matching: " + ReadmePrefix + "*.pdf"
                );
                return;
            }

            EditorUtility.OpenWithDefaultApp(Path.GetFullPath(latestReadme));
        }

        private static string FindLatestReadmeInFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
                return null;

            string[] files = Directory.GetFiles(folder, ReadmePrefix + "*.pdf");
            if (files == null || files.Length == 0)
                return null;

            System.Array.Sort(files);
            return files[files.Length - 1];
        }

        [MenuItem("Tools/ML/Frontier – Engineer’s Pipe Wrench/Show Welcome Message")]
        public static void ShowWelcomeManually()
        {
            var window = GetWindow<FrontierEngineersPipeWrenchWelcomeWindow>(
                true,
                "Frontier – Engineer’s Pipe Wrench");

            CenterWindow(window, WindowSize);
            window.Show();
        }

        private static void CenterWindow(EditorWindow window, Vector2 size)
        {
            var mainWin = EditorGUIUtility.GetMainWindowPosition();
            window.position = new Rect(
                mainWin.x + (mainWin.width - size.x) * 0.5f,
                mainWin.y + (mainWin.height - size.y) * 0.5f,
                size.x,
                size.y);
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace com.github.xuuxiaolan.crassetbundlebuilder
{
    [InitializeOnLoad]
    public class CRBundleWindow : EditorWindow
    {
        static CRBundleWindow()
        {
            OpenOnStartup();
        }

        internal static Dictionary<string, BundleBuildSettings> bundles = new Dictionary<string, BundleBuildSettings>();

        internal static bool logChangedFiles = false;

        private SortOption _assetSortOption => CRBundleWindowSettings.Instance.assetSortOption;

        private Vector2 scrollPosition;

        private void OnEnable()
        {
            Debug.Log("OnEnable called");
            LoadSettings();
            CRBundleWindowSettings.Instance.Save();
            Refresh();
        }

        [MenuItem("Code Rebirth/Bundle Builder")]
        public static void Open()
        {
            GetWindow<CRBundleWindow>("CR Bundle Builder");

            // Ensure settings are loaded if manually opened
            LoadSettings();
            CRBundleWindowSettings.Instance.Save();
            Refresh();
        }

        private static void OpenOnStartup()
        {
            // Ensures that the window opens when Unity starts
            EditorApplication.delayCall += () =>
            {
                Open(); // Opens the window when Unity finishes loading
            };
        }

        private static void LoadSettings()
        {
            Debug.Log("LoadSettings called");
            var settings = CRBundleWindowSettings.Instance;

            if (string.IsNullOrEmpty(settings.buildOutputPath))
            {
                Debug.Log("Searching for 'AssetBundles' directory.");

                // Search for 'AssetBundles' directory within the 'Assets' folder
                string[] assetBundlesDirs = Directory.GetDirectories(Application.dataPath, "AssetBundles", SearchOption.AllDirectories);

                if (assetBundlesDirs.Length > 0)
                {
                    settings.buildOutputPath = assetBundlesDirs[0];
                    SaveBuildOutputPath(settings.buildOutputPath);
                    Debug.Log($"Found 'AssetBundles' directory at: {settings.buildOutputPath}");
                }
                else
                {
                    Debug.LogWarning("Could not find 'AssetBundles' directory. Setting default build output path to 'Assets/AssetBundles/StandaloneWindows'.");
                    // Construct the absolute path to 'Assets/AssetBundles/StandaloneWindows'
                    string defaultAbsolutePath = Path.Combine(Application.dataPath, "AssetBundles", "StandaloneWindows");

                    // Ensure the directory exists
                    if (!Directory.Exists(defaultAbsolutePath))
                    {
                        Directory.CreateDirectory(defaultAbsolutePath);
                    }

                    settings.buildOutputPath = defaultAbsolutePath;
                    SaveBuildOutputPath(settings.buildOutputPath);
                }
            }

            settings.buildOnlyChanged = EditorPrefs.GetBool("build_changed", settings.buildOnlyChanged);
            settings.assetSortOption = (SortOption)EditorPrefs.GetInt("asset_sort_option", (int)settings.assetSortOption);
            settings.processDependenciesRecursively = EditorPrefs.GetBool("process_dependencies_recursively", settings.processDependenciesRecursively);
        }

        private static void SaveBuildOutputPath(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                CRBundleWindowSettings.Instance.buildOutputPath = path;
                EditorPrefs.SetString("build_output", path);
                Debug.Log($"Saved build_output: {path}");
                CRBundleWindowSettings.Instance.Save();
            }
        }

        private static void Refresh()
        {
            Debug.Log("Refreshing bundler.");

            AssetDatabase.SaveAssets();

            bundles.Clear();
            foreach (string bundle in AssetDatabase.GetAllAssetBundleNames())
            {
                BundleBuildSettings settings = new BundleBuildSettings(bundle);
                settings.Build = EditorPrefs.GetBool($"{bundle}_build", false);
                settings.Blacklisted = EditorPrefs.GetBool($"{bundle}_blacklisted", false);
                settings.ChangedSinceLastBuild = EditorPrefs.GetBool($"{bundle}_changed", true);
                settings.LastBuildSize = EditorPrefs.GetInt($"{bundle}_size", 0);
                bundles[bundle] = settings;
            }
        }

        private void OnGUI()
        {
            Color FolderColor = new Color(0.8f, 0.8f, 1f, 1f);
            Color BundleDataColor = new Color(0.8f, 1f, 0.8f, 1f);
            Color AssetColor = new Color(1f, 0.8f, 0.8f, 1f);

            GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 14
            };

            GUIStyle boldLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold
            };

            GUIStyle assetFoldoutStyle = new GUIStyle(EditorStyles.foldout)
            {
                fontStyle = FontStyle.Bold
            };

            if (GUILayout.Button("Refresh"))
            {
                Refresh();
            }

            var settings = CRBundleWindowSettings.Instance;

            settings.assetSortOption = (SortOption)EditorGUILayout.EnumPopup("Sort Assets By:", settings.assetSortOption);
            EditorPrefs.SetInt("asset_sort_option", (int)settings.assetSortOption);

            settings.processDependenciesRecursively = EditorGUILayout.Toggle("Process Dependencies Recursively", settings.processDependenciesRecursively);
            EditorPrefs.SetBool("process_dependencies_recursively", settings.processDependenciesRecursively);

            settings.Save();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            foreach (BundleBuildSettings bundle in bundles.Values)
            {
                string prefix = bundle.ChangedSinceLastBuild ? "(*) " : "";

                // Reset indent level
                int originalIndentLevel = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;

                // Begin a horizontal group for the bundle header
                EditorGUILayout.BeginHorizontal();

                // Define constants for widths and spacing
                float foldoutWidth = 15f;
                float iconWidth = 20f;
                float toggleWidth = 70f;
                float spacing = 2f;

                // Get the rect for the entire line
                Rect lastRect = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight);

                // Calculate the total width available for label and toggles
                float totalWidth = lastRect.width;

                // Calculate positions for each element

                // Foldout Rect
                Rect foldoutRect = new Rect(lastRect.x, lastRect.y, foldoutWidth, lastRect.height);

                // Icon Rect
                Rect iconRect = new Rect(foldoutRect.xMax + spacing, lastRect.y, iconWidth, lastRect.height);

                // Available width after foldout and icon
                float remainingWidth = totalWidth - (foldoutWidth + iconWidth + spacing * 2);

                // Let's define the total width for the toggles
                float togglesTotalWidth = toggleWidth * 2 + spacing; // two toggles and spacing between them

                // Label Rect - occupy the remaining space minus the toggles
                Rect labelRect = new Rect(
                    iconRect.xMax + spacing,
                    lastRect.y,
                    remainingWidth - togglesTotalWidth - spacing, // subtract toggles width and additional spacing
                    lastRect.height);

                // Build Toggle Rect - position it at the right end
                Rect buildToggleRect = new Rect(
                    lastRect.xMax - togglesTotalWidth + spacing, // position from the right
                    lastRect.y,
                    toggleWidth,
                    lastRect.height);

                // Blacklist Toggle Rect - position to the right of Build Toggle
                Rect blacklistToggleRect = new Rect(
                    buildToggleRect.xMax + spacing,
                    lastRect.y,
                    toggleWidth,
                    lastRect.height);

                // Draw the foldout
                bundle.FoldOutOpen = EditorGUI.Foldout(foldoutRect, bundle.FoldOutOpen, "", true);

                // Draw the icon
                Texture2D bundleIcon = EditorGUIUtility.IconContent("d_Folder Icon").image as Texture2D;
                GUI.DrawTexture(iconRect, bundleIcon);

                // Draw the label
                EditorGUI.LabelField(labelRect, $"{prefix}{bundle.DisplayName}", new GUIStyle(headerStyle) { normal = { textColor = FolderColor } });

                // Draw the Build toggle
                bool build = bundle.Build;
                bool newBuild = EditorGUI.ToggleLeft(buildToggleRect, "Build", build);
                if (newBuild != build)
                {
                    bundle.Build = newBuild;
                    EditorPrefs.SetBool($"{bundle.BundleName}_build", newBuild);

                    if (newBuild)
                    {
                        // Untoggle Blacklist
                        bundle.Blacklisted = false;
                        EditorPrefs.SetBool($"{bundle.BundleName}_blacklisted", false);
                    }
                }

                // Draw the Blacklist toggle
                bool blacklisted = bundle.Blacklisted;
                bool newBlacklisted = EditorGUI.ToggleLeft(blacklistToggleRect, "Blacklist", blacklisted);
                if (newBlacklisted != blacklisted)
                {
                    bundle.Blacklisted = newBlacklisted;
                    EditorPrefs.SetBool($"{bundle.BundleName}_blacklisted", newBlacklisted);

                    if (newBlacklisted)
                    {
                        // Untoggle Build
                        bundle.Build = false;
                        EditorPrefs.SetBool($"{bundle.BundleName}_build", false);
                    }
                }

                EditorGUILayout.EndHorizontal();

                // Handle foldout click outside toggles
                if (Event.current.type == EventType.MouseDown && lastRect.Contains(Event.current.mousePosition))
                {
                    // Check if the click is not in the toggles
                    if (!buildToggleRect.Contains(Event.current.mousePosition) && !blacklistToggleRect.Contains(Event.current.mousePosition))
                    {
                        bundle.FoldOutOpen = !bundle.FoldOutOpen;
                        Event.current.Use();
                    }
                }

                if (!bundle.FoldOutOpen)
                {
                    EditorGUI.indentLevel = originalIndentLevel; // Restore indent level
                    continue;
                }

                // Display bundle details
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("Total Size", Utils.GetReadableFileSize(bundle.TotalSize), new GUIStyle(boldLabelStyle) { normal = { textColor = BundleDataColor } });
                EditorGUILayout.LabelField("Previous Total Size", Utils.GetReadableFileSize(bundle.LastBuildSize), new GUIStyle(boldLabelStyle) { normal = { textColor = BundleDataColor } });
                EditorGUILayout.LabelField("Built Bundle Size", Utils.GetReadableFileSize(bundle.BuiltBundleSize), new GUIStyle(boldLabelStyle) { normal = { textColor = BundleDataColor } });

                bundle.AssetsFoldOut = EditorGUILayout.Foldout(bundle.AssetsFoldOut, "Assets in Bundle", true, assetFoldoutStyle);

                if (bundle.AssetsFoldOut)
                {
                    SortAssets(bundle.Assets);

                    foreach (var asset in bundle.Assets)
                    {
                        EditorGUILayout.BeginHorizontal();
                        Texture2D icon = AssetDatabase.GetCachedIcon(asset.Path) as Texture2D;
                        if (icon != null)
                        {
                            GUILayout.Label(icon, GUILayout.Width(24), GUILayout.Height(24), GUILayout.ExpandWidth(false));
                        }
                        EditorGUILayout.LabelField(Path.GetFileName(asset.Path), new GUIStyle(EditorStyles.label) { normal = { textColor = AssetColor } }, GUILayout.ExpandWidth(true));
                        Rect assetRect = GUILayoutUtility.GetLastRect();
                        EditorGUILayout.LabelField(Utils.GetReadableFileSize(asset.Size), new GUIStyle(EditorStyles.label) { normal = { textColor = AssetColor } }, GUILayout.Width(150), GUILayout.ExpandWidth(false));
                        EditorGUILayout.EndHorizontal();

                        EditorGUIUtility.AddCursorRect(assetRect, MouseCursor.Link);

                        if (Event.current.type == EventType.MouseDown && assetRect.Contains(Event.current.mousePosition) && Event.current.clickCount == 2)
                        {
                            var assetObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(asset.Path);
                            EditorGUIUtility.PingObject(assetObject);
                            Event.current.Use();
                        }
                    }
                }
                EditorGUI.indentLevel = originalIndentLevel; // Restore indent level
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Build Settings", headerStyle);

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Select All for Build"))
            {
                foreach (BundleBuildSettings bundle in bundles.Values)
                {
                    if (!bundle.Blacklisted)
                    {
                        bundle.Build = true;
                        EditorPrefs.SetBool($"{bundle.BundleName}_build", true);

                        // Ensure Blacklist is untoggled
                        bundle.Blacklisted = false;
                        EditorPrefs.SetBool($"{bundle.BundleName}_blacklisted", false);
                    }
                }
            }
            if (GUILayout.Button("Unselect All for Build"))
            {
                foreach (BundleBuildSettings bundle in bundles.Values)
                {
                    bundle.Build = false;
                    EditorPrefs.SetBool($"{bundle.BundleName}_build", false);
                }
            }

            GUILayout.EndHorizontal();

            EditorGUIUtility.labelWidth = 300;
            settings.buildOnlyChanged = EditorGUILayout.Toggle("Build Only Changed (Experimental): ", settings.buildOnlyChanged, GUILayout.ExpandWidth(true));
            EditorPrefs.SetBool("build_changed", settings.buildOnlyChanged);

            EditorGUIUtility.labelWidth = 150;

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            string newBuildOutputPath = EditorGUILayout.TextField("Build Output Directory:", settings.buildOutputPath, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Select", EditorStyles.miniButton))
            {
                newBuildOutputPath = EditorUtility.OpenFolderPanel("Select Build Output Directory", settings.buildOutputPath, "");
                if (!string.IsNullOrEmpty(newBuildOutputPath))
                {
                    SaveBuildOutputPath(newBuildOutputPath);
                }
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Build"))
            {
                EditorApplication.update += BuildBundlesOnMainThread;
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            logChangedFiles = EditorGUILayout.Toggle("Log Changed Files", logChangedFiles);
            EditorPrefs.SetBool("log_changed_files", logChangedFiles);

            if (GUILayout.Button("Clear Console"))
            {
                if (EditorUtility.DisplayDialog("Clear Console", "Are you sure you want to clear the console?", "Yes", "No"))
                {
                    ClearConsole();
                }
            }
        }

        private void SortAssets(List<AssetDetails> assets)
        {
            switch (_assetSortOption)
            {
                case SortOption.Size:
                    assets.Sort((a, b) =>
                    {
                        int sizeComparison = b.Size.CompareTo(a.Size);
                        if (sizeComparison == 0)
                        {
                            // Use path as secondary sort criterion to ensure a stable sort
                            return string.Compare(a.Path, b.Path, StringComparison.OrdinalIgnoreCase);
                        }
                        return sizeComparison;
                    });
                    break;
                case SortOption.Alphabetical:
                    assets.Sort((a, b) => string.Compare(a.Path, b.Path, StringComparison.OrdinalIgnoreCase));
                    break;
                case SortOption.ReverseAlphabetical:
                    assets.Sort((a, b) => string.Compare(b.Path, a.Path, StringComparison.OrdinalIgnoreCase));
                    break;
                case SortOption.ReverseSize:
                    assets.Sort((a, b) =>
                    {
                        int sizeComparison = a.Size.CompareTo(b.Size);
                        if (sizeComparison == 0)
                        {
                            // Use path as secondary sort criterion to ensure a stable sort
                            return string.Compare(a.Path, b.Path, StringComparison.OrdinalIgnoreCase);
                        }
                        return sizeComparison;
                    });
                    break;
            }
        }

        private void BuildBundlesOnMainThread()
        {
            EditorApplication.update -= BuildBundlesOnMainThread;

            try
            {
                Debug.Log("Starting the build process.");

                foreach (var bundle in bundles.Values)
                {
                    if (bundle.Blacklisted)
                    {
                        Debug.Log($"Skipping blacklisted bundle: {bundle.BundleName}");
                        continue;
                    }

                    if (CRBundleWindowSettings.Instance.buildOnlyChanged && !bundle.ChangedSinceLastBuild)
                    {
                        Debug.Log($"Skipping unchanged bundle: {bundle.BundleName}");
                        continue;
                    }

                    if (!bundle.Build && !CRBundleWindowSettings.Instance.buildOnlyChanged)
                    {
                        Debug.Log($"Skipping unselected bundle: {bundle.BundleName}");
                        continue;
                    }

                    // Prepare the AssetBundleBuild for this specific bundle
                    var build = new AssetBundleBuild
                    {
                        assetBundleName = bundle.BundleName,
                        assetNames = bundle.Assets.Select(a => a.Path).ToArray()
                    };

                    Debug.Log($"Building AssetBundle: {bundle.BundleName}");
                    BuildPipeline.BuildAssetBundles(
                        CRBundleWindowSettings.Instance.buildOutputPath,
                        new AssetBundleBuild[] { build },
                        BuildAssetBundleOptions.None,
                        EditorUserBuildSettings.activeBuildTarget
                    );

                    // Mark the bundle as built
                    bundle.ChangedSinceLastBuild = false;
                    bundle.LastBuildSize = bundle.TotalSize;
                    EditorPrefs.SetBool($"{bundle.BundleName}_changed", false);
                    EditorPrefs.SetInt($"{bundle.BundleName}_size", (int)bundle.TotalSize);

                    Debug.Log($"Built bundle: {bundle.BundleName}");
                }

                Debug.Log("Performing cleanup.");
                Refresh();

                // Clean up empty bundle files and their associated .manifest and .meta files
                foreach (string bundleName in bundles.Keys)
                {
                    string bundlePath = Path.Combine(CRBundleWindowSettings.Instance.buildOutputPath, bundleName);
                    if (File.Exists(bundlePath) && new FileInfo(bundlePath).Length == 0)
                    {
                        File.Delete(bundlePath);

                        // Delete associated .manifest and .meta files
                        string manifestPath = bundlePath + ".manifest";
                        string bundleMetaPath = bundlePath + ".meta";
                        string manifestMetaPath = manifestPath + ".meta";

                        DeleteIfExists(manifestPath);
                        DeleteIfExists(bundleMetaPath);
                        DeleteIfExists(manifestMetaPath);
                    }
                }

                // Remove manifest files if not needed
                foreach (string file in Directory.GetFiles(CRBundleWindowSettings.Instance.buildOutputPath, "*.manifest", SearchOption.TopDirectoryOnly))
                {
                    DeleteIfExists(file);
                    DeleteIfExists(file + ".meta");
                }

                Debug.Log("Build completed and cleanup done.");
            }
            catch (Exception e)
            {
                Debug.LogError($"Build process encountered an error: {e.Message}\n{e.StackTrace}");
            }
        }

        private void DeleteIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private void ClearConsole()
        {
            var logEntries = System.Type.GetType("UnityEditor.LogEntries,UnityEditor.dll");
            var clearMethod = logEntries.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            clearMethod.Invoke(null, null);
        }
    }
}
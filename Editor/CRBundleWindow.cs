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
            settings.scaleFactor = EditorPrefs.GetFloat("ui_scale_factor", settings.scaleFactor);
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
            var settings = CRBundleWindowSettings.Instance;

            // Scale factor from settings
            float scaleFactor = settings.scaleFactor;

            // Adjust the styles and layout sizes using the updated scaleFactor
            Color FolderColor = new Color(0.8f, 0.8f, 1f, 1f);
            Color BundleDataColor = new Color(0.0f, 0.9f, 0.0f, 1f); // Slightly lighter green
            Color AssetColor = new Color(1f, 0.8f, 0.8f, 1f);
            Color DarkGreyColor = new Color(0.5f, 0.5f, 0.5f, 1f); // Slightly lighter dark grey

            GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = Mathf.RoundToInt(14 * scaleFactor)
            };

            GUIStyle boldLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = Mathf.RoundToInt(12 * scaleFactor)
            };

            GUIStyle assetFoldoutStyle = new GUIStyle(EditorStyles.foldout)
            {
                fontStyle = FontStyle.Bold,
                fontSize = Mathf.RoundToInt(12 * scaleFactor)
            };

            if (GUILayout.Button("Refresh", GUILayout.Height(20 * scaleFactor)))
            {
                Refresh();
            }

            // Add UI to adjust scaleFactor
            settings.scaleFactor = EditorGUILayout.Slider("UI Scale Factor", settings.scaleFactor, 0.5f, 2.0f);
            EditorPrefs.SetFloat("ui_scale_factor", settings.scaleFactor);

            settings.assetSortOption = (SortOption)EditorGUILayout.EnumPopup("Sort Assets By:", settings.assetSortOption, GUILayout.Height(20 * scaleFactor));
            EditorPrefs.SetInt("asset_sort_option", (int)settings.assetSortOption);

            settings.processDependenciesRecursively = EditorGUILayout.Toggle("Process Dependencies Recursively", settings.processDependenciesRecursively, GUILayout.Height(20 * scaleFactor));
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
                float foldoutWidth = 15f * scaleFactor;
                float iconWidth = 20f * scaleFactor;
                float spacing = 5f * scaleFactor;
                float lineHeight = (EditorGUIUtility.singleLineHeight + 5f) * scaleFactor;

                // Get the rect for the entire line
                Rect lastRect = GUILayoutUtility.GetRect(0, lineHeight);

                // Calculate positions for each element

                // Foldout Rect
                Rect foldoutRect = new Rect(lastRect.x + 5f, lastRect.y + 2f * scaleFactor, foldoutWidth, lastRect.height);

                // Icon Rect
                Rect iconRect = new Rect(foldoutRect.xMax + spacing, lastRect.y + 2f * scaleFactor, iconWidth, lastRect.height);

                // We will calculate the widths of the toggles dynamically
                GUIStyle toggleStyle = EditorStyles.toggle;

                // Calculate toggle widths
                float buildToggleWidth = toggleStyle.CalcSize(new GUIContent("Build")).x + toggleStyle.padding.horizontal;
                float blacklistToggleWidth = toggleStyle.CalcSize(new GUIContent("Blacklist")).x + toggleStyle.padding.horizontal;

                // Total toggle width including spacing
                float togglesTotalWidth = buildToggleWidth + blacklistToggleWidth + spacing * 2;

                // Available width for label
                float remainingWidth = lastRect.width - (foldoutWidth + iconWidth + togglesTotalWidth + spacing * 4);

                // Label Rect
                Rect labelRect = new Rect(
                    iconRect.xMax + spacing,
                    lastRect.y + 2f * scaleFactor,
                    remainingWidth,
                    lastRect.height);

                // Build Toggle Rect - position it at the right end
                Rect buildToggleRect = new Rect(
                    lastRect.xMax - togglesTotalWidth + spacing, // position from the right
                    lastRect.y + 2f * scaleFactor,
                    buildToggleWidth,
                    lastRect.height);

                // Blacklist Toggle Rect - position to the right of Build Toggle
                Rect blacklistToggleRect = new Rect(
                    buildToggleRect.x + buildToggleWidth + spacing,
                    lastRect.y + 2f * scaleFactor,
                    blacklistToggleWidth,
                    lastRect.height);

                // Draw the foldout
                bundle.FoldOutOpen = EditorGUI.Foldout(foldoutRect, bundle.FoldOutOpen, "", true);

                // Draw the icon
                Texture2D bundleIcon = EditorGUIUtility.IconContent("d_Folder Icon").image as Texture2D;
                GUI.DrawTexture(iconRect, bundleIcon, ScaleMode.ScaleToFit);

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

                // Create styles for labels and values
                GUIStyle sizeLabelStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = Mathf.RoundToInt(12 * scaleFactor),
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = DarkGreyColor } // Slightly lighter dark grey
                };

                GUIStyle sizeValueStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = Mathf.RoundToInt(12 * scaleFactor),
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = BundleDataColor } // Slightly lighter green
                };

                // Use BeginHorizontal to apply the styles
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Total Size", sizeLabelStyle, GUILayout.Width(150 * scaleFactor));
                EditorGUILayout.LabelField(Utils.GetReadableFileSize(bundle.TotalSize), sizeValueStyle);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Previous Total Size", sizeLabelStyle, GUILayout.Width(150 * scaleFactor));
                EditorGUILayout.LabelField(Utils.GetReadableFileSize(bundle.LastBuildSize), sizeValueStyle);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Built Bundle Size", sizeLabelStyle, GUILayout.Width(150 * scaleFactor));
                EditorGUILayout.LabelField(Utils.GetReadableFileSize(bundle.BuiltBundleSize), sizeValueStyle);
                EditorGUILayout.EndHorizontal();

                // Adjusted the foldout to use EditorGUI.Foldout to scale font size
                Rect assetsFoldoutRect = GUILayoutUtility.GetRect(new GUIContent("Assets in Bundle"), assetFoldoutStyle);
                bundle.AssetsFoldOut = EditorGUI.Foldout(assetsFoldoutRect, bundle.AssetsFoldOut, "Assets in Bundle", true, assetFoldoutStyle);

                if (bundle.AssetsFoldOut)
                {
                    EditorGUI.indentLevel++; // Increase indent for assets

                    SortAssets(bundle.Assets);

                    // Temporarily reset indent level to prevent unintended indentation
                    int assetIndentLevel = EditorGUI.indentLevel;
                    EditorGUI.indentLevel = 0;

                    foreach (var asset in bundle.Assets)
                    {
                        EditorGUILayout.BeginHorizontal();

                        // Apply indentation manually
                        GUILayout.Space(assetIndentLevel * 15f * scaleFactor);

                        // Get the icon
                        Texture2D icon = AssetPreview.GetAssetPreview(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(asset.Path));
                        if (icon == null)
                        {
                            icon = AssetPreview.GetMiniThumbnail(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(asset.Path));
                        }

                        if (icon != null)
                        {
                            GUILayout.Label(icon, GUILayout.Width(24f * scaleFactor), GUILayout.Height(24f * scaleFactor), GUILayout.ExpandWidth(false));
                        }
                        else
                        {
                            // If no icon, add space to align names
                            GUILayout.Space(24f * scaleFactor);
                        }

                        // Asset name
                        EditorGUILayout.LabelField(Path.GetFileName(asset.Path), new GUIStyle(EditorStyles.label)
                        {
                            fontSize = Mathf.RoundToInt(12 * scaleFactor),
                            normal = { textColor = AssetColor }
                        }, GUILayout.ExpandWidth(true)); // Remove any width constraints

                        // Asset size
                        EditorGUILayout.LabelField(Utils.GetReadableFileSize(asset.Size), new GUIStyle(EditorStyles.label)
                        {
                            fontSize = Mathf.RoundToInt(12 * scaleFactor),
                            normal = { textColor = AssetColor }
                        }, GUILayout.Width(80f * scaleFactor), GUILayout.ExpandWidth(false));

                        EditorGUILayout.EndHorizontal();

                        Rect assetRect = GUILayoutUtility.GetLastRect();
                        EditorGUIUtility.AddCursorRect(assetRect, MouseCursor.Link);

                        if (Event.current.type == EventType.MouseDown && assetRect.Contains(Event.current.mousePosition) && Event.current.clickCount == 2)
                        {
                            var assetObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(asset.Path);
                            EditorGUIUtility.PingObject(assetObject);
                            Event.current.Use();
                        }
                    }

                    // Restore indent level
                    EditorGUI.indentLevel = assetIndentLevel;

                    EditorGUI.indentLevel--; // Decrease indent after assets
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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace com.github.xuuxiaolan.crassetbundlebuilder
{
    [InitializeOnLoad]
    public class CRBundleWindow : EditorWindow
    {
        // Static constructor is called when Unity loads, including on startup
        static CRBundleWindow()
        {
            OpenOnStartup();
        }

        internal class BundleBuildSettings
        {
            public string BundleName { get; private set; }
            public string DisplayName { get; private set; }

            public bool FoldOutOpen { get; set; } = false;
            public bool Build { get; set; } = false;
            public bool Blacklisted { get; set; } = false;
            public bool ChangedSinceLastBuild { get; set; } = true;

            public long TotalSize { get; private set; } = 0;
            public long LastBuildSize { get; set; } = 0;
            public long BuiltBundleSize { get; private set; } = 0;

            public bool AssetsFoldOut { get; set; } = false;

            public List<AssetDetails> Assets { get; private set; } = new List<AssetDetails>();

            internal BundleBuildSettings(string bundleName)
            {
                BundleName = bundleName;
                DisplayName = ConvertToDisplayName(bundleName);

                HashSet<string> processedAssets = new HashSet<string>();

                foreach (string asset in AssetDatabase.GetAssetPathsFromAssetBundle(bundleName))
                {
                    ProcessAsset(asset, processedAssets);
                }

                FileInfo bundleFileInfo = new FileInfo(Path.Combine(CRBundleWindowSettings.Instance.buildOutputPath, bundleName));
                if (bundleFileInfo.Exists)
                    BuiltBundleSize = bundleFileInfo.Length;
            }

        private void ProcessAsset(string assetPath, HashSet<string> processedAssets)
        {
            if (processedAssets.Contains(assetPath))
                return;

            // Exclude script files
            if (assetPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            FileInfo fileInfo = new FileInfo(assetPath);
            if (!fileInfo.Exists) return;

            long fileSize = fileInfo.Length;
            TotalSize += fileSize;
            Assets.Add(new AssetDetails { Path = assetPath, Size = fileSize });
            processedAssets.Add(assetPath);

            if (!CRBundleWindowSettings.Instance.processDependenciesRecursively)
                return;

            UnityEngine.Object assetObject = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (assetObject != null)
            {
                try
                {
                    foreach (string dependency in AssetDatabase.GetDependencies(assetPath))
                    {
                        if (!processedAssets.Contains(dependency) && dependency != assetPath)
                        {
                            ProcessAsset(dependency, processedAssets);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error processing dependencies for {assetPath}: {e.Message}");
                }
            }
        }

            internal class AssetDetails
            {
                public string Path { get; set; }
                public long Size { get; set; }
            }
        }

        [FilePath("Project/CRBundleWindowSettings.asset", FilePathAttribute.Location.PreferencesFolder)]
        public class CRBundleWindowSettings : ScriptableSingleton<CRBundleWindowSettings>
        {
            public static CRBundleWindowSettings Instance => instance;

            public string buildOutputPath;
            public bool buildOnlyChanged;
            public bool processDependenciesRecursively = false; // Default is false
            public SortOption assetSortOption = SortOption.Size; // Default sorting by size

            public void Save()
            {
                Save(true);
            }
        }

        public enum SortOption
        {
            Alphabetical,
            Size,
            ReverseAlphabetical,
            ReverseSize
        }

        static string ConvertToDisplayName(string bundleName)
        {
            return Regex.Replace(bundleName, "(\\B[A-Z])", " $1")
                        .Replace("assets", " Assets")
                        .Split(' ')
                        .Select(word => char.ToUpper(word[0]) + word.Substring(1))
                        .Aggregate((a, b) => a + " " + b);
        }

        static string GetReadableFileSize(long bytes)
        {
            if (bytes <= 0) return "N/A";

            string[] suffixes = { "B", "KB", "MB", "GB" };
            int i;
            double doubleBytes = bytes;

            for (i = 0; i < suffixes.Length && bytes >= 1024; i++, bytes /= 1024)
            {
                doubleBytes = bytes / 1024.0;
            }

            return string.Format("{0:0.##} {1}", doubleBytes, suffixes[i]);
        }

        internal static Dictionary<string, BundleBuildSettings> bundles = new();

        internal static bool logChangedFiles = false;

        private SortOption _assetSortOption => CRBundleWindowSettings.Instance.assetSortOption;

        Vector2 scrollPosition;

        void OnEnable()
        {
            Debug.Log("OnEnable called");
            LoadSettings();
            CRBundleWindowSettings.Instance.Save();
            Refresh();
        }

        [MenuItem("Code Rebirth/Bundle Builder")]
        static void Open()
        {
            var window = GetWindow<CRBundleWindow>("CR Bundle Builder");
            
            // Use the class name to call static methods
            CRBundleWindow.LoadSettings();  // Ensure settings are loaded if manually opened
            CRBundleWindowSettings.Instance.Save();
            CRBundleWindow.Refresh();
        }

        static void OpenOnStartup()
        {
            // Ensures that the window opens when Unity starts
            EditorApplication.delayCall += () =>
            {
                Open(); // Opens the window when Unity finishes loading
            };
        }

        static void LoadSettings()
        {
            Debug.Log("LoadSettings called");
            var settings = CRBundleWindowSettings.Instance;

            settings.buildOutputPath = EditorPrefs.GetString("build_output", settings.buildOutputPath);

            if (string.IsNullOrEmpty(settings.buildOutputPath))
            {
                Debug.Log("Having to use default build output path.");
                settings.buildOutputPath = "F:/Ammar Stuff/ModdingProject/CodeRebirthStuff/CodeRebirth/CodeRebirth/CodeRebirthUnityProject/Assets/LethalCompany/Mods/plugins/CodeRebirth/AssetBundles";
                SaveBuildOutputPath(settings.buildOutputPath);
            }

            settings.buildOnlyChanged = EditorPrefs.GetBool("build_changed", settings.buildOnlyChanged);
            settings.assetSortOption = (SortOption)EditorPrefs.GetInt("asset_sort_option", (int)settings.assetSortOption);
            settings.processDependenciesRecursively = EditorPrefs.GetBool("process_dependencies_recursively", settings.processDependenciesRecursively);
        }

        static void SaveBuildOutputPath(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                CRBundleWindowSettings.Instance.buildOutputPath = path;
                EditorPrefs.SetString("build_output", path);
                Debug.Log($"Saved build_output: {path}");
                CRBundleWindowSettings.Instance.Save();
            }
        }

        static void Refresh()
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

        void OnGUI()
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

                EditorGUI.indentLevel = 0;
                Rect foldoutRect = GUILayoutUtility.GetRect(20f, 25f, GUILayout.ExpandWidth(true)); // Increased height for better spacing
                bundle.FoldOutOpen = EditorGUI.Foldout(foldoutRect, bundle.FoldOutOpen, "", true, headerStyle);

                // Adding a custom icon for asset bundles
                Texture2D bundleIcon = EditorGUIUtility.IconContent("d_Folder Icon").image as Texture2D;
                Rect iconRect = new Rect(foldoutRect.x + 15, foldoutRect.y + 3, 20, 20); // Lower the icon positioning
                GUI.DrawTexture(iconRect, bundleIcon);

                // Drawing the label next to the icon
                Rect labelRect = new Rect(iconRect.xMax + 5, foldoutRect.y, foldoutRect.width - iconRect.width, foldoutRect.height);
                EditorGUI.LabelField(labelRect, $"{prefix}{bundle.DisplayName}", new GUIStyle(headerStyle) { normal = { textColor = FolderColor } });

                if (Event.current.type == EventType.MouseDown && foldoutRect.Contains(Event.current.mousePosition))
                {
                    bundle.FoldOutOpen = !bundle.FoldOutOpen;
                    Event.current.Use();
                }

                if (!bundle.FoldOutOpen)
                {
                    continue;
                }

                EditorGUILayout.LabelField("Total Size", GetReadableFileSize(bundle.TotalSize), new GUIStyle(boldLabelStyle) { normal = { textColor = BundleDataColor } });
                EditorGUILayout.LabelField("Previous Total Size", GetReadableFileSize(bundle.LastBuildSize), new GUIStyle(boldLabelStyle) { normal = { textColor = BundleDataColor } });
                EditorGUILayout.LabelField("Built Bundle Size", GetReadableFileSize(bundle.BuiltBundleSize), new GUIStyle(boldLabelStyle) { normal = { textColor = BundleDataColor } });

                bool build = bundle.Build;
                build = EditorGUILayout.Toggle("Build", build);
                if (build != bundle.Build)
                {
                    bundle.Build = build;
                    EditorPrefs.SetBool($"{bundle.BundleName}_build", build);
                }

                bool blacklisted = bundle.Blacklisted;
                blacklisted = EditorGUILayout.Toggle("Blacklist", blacklisted);
                if (blacklisted != bundle.Blacklisted)
                {
                    bundle.Blacklisted = blacklisted;
                    EditorPrefs.SetBool($"{bundle.BundleName}_blacklisted", blacklisted);
                }

                EditorGUI.indentLevel++;
                Rect assetFoldoutRect = GUILayoutUtility.GetRect(20f, 25f, GUILayout.ExpandWidth(true));
                bundle.AssetsFoldOut = EditorGUI.Foldout(assetFoldoutRect, bundle.AssetsFoldOut, "Assets in Bundle", true, assetFoldoutStyle);

                if (Event.current.type == EventType.MouseDown && assetFoldoutRect.Contains(Event.current.mousePosition))
                {
                    bundle.AssetsFoldOut = !bundle.AssetsFoldOut;
                    Event.current.Use();
                }

                if (bundle.AssetsFoldOut)
                {
                    SortAssets(bundle.Assets);

                    foreach (var asset in bundle.Assets)
                    {
                        EditorGUILayout.BeginHorizontal();
                        Texture2D icon = AssetDatabase.GetCachedIcon(asset.Path) as Texture2D;
                        if (icon != null)
                        {
                            GUILayout.Label(icon, GUILayout.Width(24), GUILayout.Height(24), GUILayout.ExpandWidth(false)); // Adjust icon size and positioning
                        }
                        EditorGUILayout.LabelField(Path.GetFileName(asset.Path), new GUIStyle(EditorStyles.label) { normal = { textColor = AssetColor } }, GUILayout.ExpandWidth(true));
                        Rect lastRect = GUILayoutUtility.GetLastRect();
                        EditorGUILayout.LabelField(GetReadableFileSize(asset.Size), new GUIStyle(EditorStyles.label) { normal = { textColor = AssetColor } }, GUILayout.Width(150), GUILayout.ExpandWidth(false));
                        EditorGUILayout.EndHorizontal();

                        EditorGUIUtility.AddCursorRect(lastRect, MouseCursor.Link); // Change cursor to link cursor

                        if (Event.current.type == EventType.MouseDown && lastRect.Contains(Event.current.mousePosition) && Event.current.clickCount == 2)
                        {
                            // Ping the asset when double clicked
                            var assetObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(asset.Path);
                            EditorGUIUtility.PingObject(assetObject);
                            Event.current.Use();
                        }
                    }
                }
                EditorGUI.indentLevel--;
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

        void SortAssets(List<BundleBuildSettings.AssetDetails> assets)
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

        void BuildBundlesOnMainThread()
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
                        BuildTarget.StandaloneWindows
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

                // Optionally, handle manifest files differently if they are needed elsewhere
                if (!NeedToKeepManifests())
                {
                    foreach (string file in Directory.GetFiles(CRBundleWindowSettings.Instance.buildOutputPath, "*.manifest", SearchOption.TopDirectoryOnly))
                    {
                        DeleteIfExists(file);
                        DeleteIfExists(file + ".meta");
                    }
                }

                Debug.Log("Build completed and cleanup done.");
            }
            catch (Exception e)
            {
                Debug.LogError($"Build process encountered an error: {e.Message}\n{e.StackTrace}");
            }
        }

        void DeleteIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        void ClearConsole()
        {
            var logEntries = System.Type.GetType("UnityEditor.LogEntries,UnityEditor.dll");
            var clearMethod = logEntries.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            clearMethod.Invoke(null, null);
        }

        bool NeedToKeepManifests()
        {
            // Implement logic to determine if manifests need to be kept
            return true; // Placeholder: modify based on your requirements
        }
    }
}
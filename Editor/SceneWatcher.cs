using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

namespace com.github.xuuxiaolan.crassetbundlebuilder
{
    [InitializeOnLoad]
    public class SceneChangeWatcher
    {
        static SceneChangeWatcher()
        {
            EditorSceneManager.sceneSaved += OnSceneSaved;
        }

        private static void OnSceneSaved(UnityEngine.SceneManagement.Scene scene)
        {
            string scenePath = scene.path;
            // Call your helper to mark the bundle as changed.
            MarkBundleAsChanged(scenePath);
        }

        private static void MarkBundleAsChanged(string assetPath)
        {
            Debug.Log($"Marking {assetPath} as changed.");
            string bundleName = AssetDatabase.GetImplicitAssetBundleName(assetPath);
            string variantName = AssetDatabase.GetImplicitAssetBundleVariantName(assetPath);
            Debug.Log($"{bundleName}");
            if (!string.IsNullOrEmpty(bundleName))
            {
                if (!string.IsNullOrEmpty(variantName))
                {
                    bundleName += "." + variantName;
                }
                if (CRBundleWindow.bundles.TryGetValue(bundleName, out BundleBuildSettings bundleSettings))
                {
                    bundleSettings.ChangedSinceLastBuild = true;
                    Debug.Log($"Scene change detected. Marked bundle {bundleName} as changed.");
                    return;
                }
            }
        }
    }
}
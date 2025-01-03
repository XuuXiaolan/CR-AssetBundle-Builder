using System;
using UnityEditor;
using UnityEngine;

namespace com.github.xuuxiaolan.crassetbundlebuilder
{
    public class MarkChangedBundles : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            foreach (string assetPath in importedAssets)
            {
                MarkBundleAsChanged(assetPath);
            }

            foreach (string assetPath in deletedAssets)
            {
                MarkBundleAsChanged(assetPath);
            }

            for (int i = 0; i < movedAssets.Length; i++)
            {
                MarkBundleAsChanged(movedAssets[i]);
                MarkBundleAsChanged(movedFromAssetPaths[i]);
            }
        }

        private static void MarkBundleAsChanged(string assetPath)
        {
            if (!AssetExists(assetPath))
            {
                return;
            }

            // If it's a real path, see if there's an implicit AssetBundle name assigned.
            string bundleName = AssetDatabase.GetImplicitAssetBundleName(assetPath);
            if (!string.IsNullOrEmpty(bundleName))
            {
                if (CRBundleWindow.bundles.TryGetValue(bundleName, out BundleBuildSettings bundleSettings))
                {
                    bundleSettings.ChangedSinceLastBuild = true;
                }
            }
        }

        private static bool AssetExists(string assetPath)
        {
            // 1) Check if it’s a valid folder:
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return true;
            }

            // 2) Check if it’s a valid asset:
            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            return obj != null;
        }
    }
}

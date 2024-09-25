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
            string bundleName = AssetDatabase.GetImplicitAssetBundleName(assetPath);
            if (!string.IsNullOrEmpty(bundleName))
            {
                if (CRBundleWindow.bundles.TryGetValue(bundleName, out BundleBuildSettings bundleSettings))
                {
                    bundleSettings.ChangedSinceLastBuild = true;
                }
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace com.github.xuuxiaolan.crassetbundlebuilder
{
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
            DisplayName = Utils.ConvertToDisplayName(bundleName);

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
}
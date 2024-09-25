using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public BundleBuildSettings(string bundleName)
        {
            BundleName = bundleName;
            DisplayName = Utils.ConvertToDisplayName(bundleName);

            LoadBundleData();
        }

        private void LoadBundleData()
        {
            HashSet<string> processedAssets = new HashSet<string>();
            string[] assetPaths = AssetDatabase.GetAssetPathsFromAssetBundle(BundleName);

            foreach (string assetPath in assetPaths)
            {
                ProcessAsset(assetPath, processedAssets);
            }

            string bundlePath = Path.Combine(CRBundleWindowSettings.Instance.buildOutputPath, BundleName);
            if (File.Exists(bundlePath))
            {
                BuiltBundleSize = new FileInfo(bundlePath).Length;
            }
        }

        private void ProcessAsset(string assetPath, HashSet<string> processedAssets)
        {
            if (processedAssets.Contains(assetPath) || assetPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                return;

            long fileSize = GetFileSize(assetPath);
            if (fileSize >= 0)
            {
                TotalSize += fileSize;
                Assets.Add(new AssetDetails { Path = assetPath, Size = fileSize });
                processedAssets.Add(assetPath);
            }

            if (!CRBundleWindowSettings.Instance.processDependenciesRecursively)
                return;

            try
            {
                string[] dependencies = AssetDatabase.GetDependencies(assetPath, true);
                foreach (string dependency in dependencies)
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

        private long GetFileSize(string assetPath)
        {
            string fullPath = Path.Combine(Application.dataPath.Replace("Assets", ""), assetPath);
            if (File.Exists(fullPath))
            {
                return new FileInfo(fullPath).Length;
            }
            return -1;
        }

        public bool ShouldBuild()
        {
            if (Blacklisted)
                return false;

            if (CRBundleWindowSettings.Instance.buildOnlyChanged && !ChangedSinceLastBuild)
                return false;

            return Build;
        }
    }

    internal class AssetDetails
    {
        public string Path { get; set; }
        public long Size { get; set; }
    }
}
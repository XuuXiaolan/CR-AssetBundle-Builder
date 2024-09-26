using UnityEditor;
using UnityEngine;

namespace com.github.xuuxiaolan.crassetbundlebuilder
{
    [FilePath("ProjectSettings/CRBundleWindowSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class CRBundleWindowSettings : ScriptableSingleton<CRBundleWindowSettings>
    {
        public static CRBundleWindowSettings Instance => instance;

        public string buildOutputPath = "Assets/AssetBundles";
        public bool buildOnlyChanged = false;
        public bool processDependenciesRecursively = false;
        public SortOption assetSortOption = SortOption.Size;

        public float scaleFactor = 1.1f;

        // New color fields for customization
        [SerializeField]
        public Color folderColor = new Color(0.8f, 0.8f, 1f, 1f);

        [SerializeField]
        public Color bundleLabelColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        [SerializeField]
        public Color bundleValueColor = new Color(0.0f, 1.0f, 0.0f, 1f);

        [SerializeField]
        public Color assetNameColor = new Color(1f, 0.8f, 0.8f, 1f);

        [SerializeField]
        public Color assetSizeColor = new Color(1f, 0.8f, 0.8f, 1f);

        public void Save()
        {
            Save(true);
        }

        public void Load()
        {
            // Implement any necessary loading logic here if needed
        }
    }
}
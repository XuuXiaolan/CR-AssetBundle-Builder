using UnityEditor;

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

        public float scaleFactor = 1.1f; // Added scaleFactor

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
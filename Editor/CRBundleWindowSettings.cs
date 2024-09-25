using UnityEditor;

namespace com.github.xuuxiaolan.crassetbundlebuilder
{
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
}
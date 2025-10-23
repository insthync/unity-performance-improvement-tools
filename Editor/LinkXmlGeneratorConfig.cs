using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Insthync.PerformanceImprovementTools
{
    public class LinkXmlGeneratorConfig : ScriptableObject
    {
        private const string ResourceFolderPath = "Assets/Editor/Resources";
        private const string AssetName = "LinkXmlGeneratorConfig.asset";

        public List<string> alwaysPreserveAssemblies = new List<string>()
        {
            "Newtonsoft.Json",
            "Unity.Addressables",
            "Unity.ResourceManager",
            "System",
            "System.Configuration",
            "System.Net",
            "System.Net.Security",
            "System.Security",
            "System.Security.Cryptography.X509Certificates",
        };

        public List<string> assemblyNameExcludePatterns = new List<string>()
        {
            "Editor",
        };

        public static LinkXmlGeneratorConfig GetConfig()
        {
            string assetPath = Path.Combine(ResourceFolderPath, AssetName);
            string resourcesRelativePath = Path.ChangeExtension(AssetName, null);

            // Try to load it from Resources
            var existing = Resources.Load<LinkXmlGeneratorConfig>(resourcesRelativePath);
            if (existing != null)
                return existing;

            // Make sure the directory exists
            if (!Directory.Exists(ResourceFolderPath))
                Directory.CreateDirectory(ResourceFolderPath);

            // Create a new instance and save it
            var instance = CreateInstance<LinkXmlGeneratorConfig>();
            AssetDatabase.CreateAsset(instance, assetPath);
            AssetDatabase.SaveAssets();

            Debug.Log($"Created missing {AssetName} in {ResourceFolderPath}");
            return instance;
        }

        [InitializeOnLoadMethod]
        private static void EnsureAssetExists()
        {
            GetConfig();
        }

        public bool IsExcludeAssemblyName(string name)
        {
            foreach (var pattern in assemblyNameExcludePatterns)
            {
                if (name.ToLower().Contains(pattern.ToLower()))
                    return true;
            }
            return false;
        }
    }
}
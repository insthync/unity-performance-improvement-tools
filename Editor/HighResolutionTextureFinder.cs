using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Insthync.PerformanceImprovementTools
{
    public class HighResolutionTextureFinder : EditorWindow
    {
        private List<int> _resolutionThresholds = new List<int> { 1024, 512 };
        private List<int> _resizeValues = new List<int> { 512, 256 };
        private List<Texture2D> _highResTextures = new List<Texture2D>();
        private List<bool> _textureSelections = new List<bool>();

        private int _currentPage = 0;
        private int _itemsPerPage = 10;
        private string _selectedFolderPath = "";
        private string _backupFolderPath = "";

        [MenuItem("Tools/High Resolution Texture Finder and Resizer")]
        public static void ShowWindow()
        {
            GetWindow<HighResolutionTextureFinder>("Texture Finder and Resizer");
        }

        private void OnGUI()
        {
            GUILayout.Label("High Resolution Texture Finder and Resizer", EditorStyles.boldLabel);

            GUILayout.Label("Resolution Thresholds and Resize Values", EditorStyles.boldLabel);
            for (int i = 0; i < _resolutionThresholds.Count; i++)
            {
                GUILayout.BeginHorizontal();
                _resolutionThresholds[i] = EditorGUILayout.IntField($"Threshold {i + 1} >=", _resolutionThresholds[i]);
                _resizeValues[i] = EditorGUILayout.IntField($"Resize Value {i + 1}", _resizeValues[i]);
                if (GUILayout.Button("Remove", GUILayout.Width(70)))
                {
                    _resolutionThresholds.RemoveAt(i);
                    _resizeValues.RemoveAt(i);
                    i--;
                }
                GUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Add Threshold/Resize Pair"))
            {
                _resolutionThresholds.Add(0);
                _resizeValues.Add(0);
            }

            if (GUILayout.Button("Select Folder"))
            {
                _selectedFolderPath = EditorUtility.OpenFolderPanel("Select Folder", "Assets", "");
                if (!string.IsNullOrEmpty(_selectedFolderPath) && _selectedFolderPath.StartsWith(Application.dataPath))
                {
                    _selectedFolderPath = "Assets" + _selectedFolderPath.Substring(Application.dataPath.Length);
                }
                else
                {
                    _selectedFolderPath = "";
                }
            }

            GUILayout.Label($"Selected Folder: {_selectedFolderPath}", EditorStyles.wordWrappedLabel);

            if (GUILayout.Button("Select Backup Folder"))
            {
                _backupFolderPath = EditorUtility.OpenFolderPanel("Select Backup Folder", "", "");
            }

            GUILayout.Label($"Backup Folder: {_backupFolderPath}", EditorStyles.wordWrappedLabel);

            if (!string.IsNullOrEmpty(_selectedFolderPath) && GUILayout.Button("Find High Resolution Textures"))
            {
                FindHighResolutionTextures();
            }

            if (_highResTextures.Count > 0)
            {
                GUILayout.Label("High Resolution Textures in Selected Folder:", EditorStyles.boldLabel);

                // Display select/deselect all buttons
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Select All", GUILayout.Width(100)))
                {
                    SelectAllTextures(true);
                }
                if (GUILayout.Button("Deselect All", GUILayout.Width(100)))
                {
                    SelectAllTextures(false);
                }
                GUILayout.EndHorizontal();

                // Display pagination controls
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Previous", GUILayout.Width(100)) && _currentPage > 0)
                {
                    _currentPage--;
                }

                GUILayout.FlexibleSpace();
                GUILayout.Label($"Page {_currentPage + 1} of {Mathf.CeilToInt((float)_highResTextures.Count / _itemsPerPage)}", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Next", GUILayout.Width(100)) && (_currentPage + 1) * _itemsPerPage < _highResTextures.Count)
                {
                    _currentPage++;
                }
                GUILayout.EndHorizontal();

                // Display textures for the current page
                int startIndex = _currentPage * _itemsPerPage;
                int endIndex = Mathf.Min(startIndex + _itemsPerPage, _highResTextures.Count);
                for (int i = startIndex; i < endIndex; i++)
                {
                    GUILayout.BeginHorizontal();
                    _textureSelections[i] = EditorGUILayout.Toggle(_textureSelections[i], GUILayout.Width(20));
                    GUILayout.Label($"{_highResTextures[i].name} ({_highResTextures[i].width}x{_highResTextures[i].height})");
                    GUILayout.EndHorizontal();
                }

                GUILayout.Space(10);

                if (GUILayout.Button("Resize Selected Textures"))
                {
                    ResizeTextures();
                }
            }
        }

        private void FindHighResolutionTextures()
        {
            _highResTextures.Clear();
            _textureSelections.Clear();

            if (string.IsNullOrEmpty(_selectedFolderPath))
            {
                Debug.LogError("No folder selected!");
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { _selectedFolderPath });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture != null && ShouldResize(texture.width, texture.height))
                {
                    _highResTextures.Add(texture);
                    _textureSelections.Add(false);
                }
            }
            Debug.Log($"Found {_highResTextures.Count} high resolution textures in the selected folder.");

            // Reset pagination
            _currentPage = 0;
        }

        private bool ShouldResize(int width, int height)
        {
            for (int i = 0; i < _resolutionThresholds.Count; i++)
            {
                if (width >= _resolutionThresholds[i] || height >= _resolutionThresholds[i])
                {
                    return true;
                }
            }
            return false;
        }

        private void SelectAllTextures(bool select)
        {
            for (int i = 0; i < _textureSelections.Count; i++)
            {
                _textureSelections[i] = select;
            }
        }

        private void ResizeTextures()
        {
            for (int i = 0; i < _highResTextures.Count; i++)
            {
                if (_textureSelections[i])
                {
                    Texture2D texture = _highResTextures[i];
                    string assetPath = AssetDatabase.GetAssetPath(texture);

                    // Backup the original file if backup path is specified
                    if (!string.IsNullOrEmpty(_backupFolderPath))
                    {
                        BackupOriginalTexture(assetPath);
                    }

                    // Find the appropriate resize value based on the texture's dimensions
                    int resizeValue = GetResizeValue(texture.width, texture.height);

                    // Resize the texture
                    Texture2D resizedTexture = ResizeTexture(texture, resizeValue);

                    byte[] bytes = null;
                    string extension = Path.GetExtension(assetPath).ToLower();
                    if (extension == ".png")
                    {
                        bytes = resizedTexture.EncodeToPNG();
                    }
                    else if (extension == ".jpg")
                    {
                        bytes = resizedTexture.EncodeToJPG();
                    }
                    else if (extension == ".tga")
                    {
                        bytes = ImageConversion.EncodeToTGA(resizedTexture);
                    }
                    else if (extension == ".psd" || extension == ".exr" || extension == ".hdr")
                    {
                        bytes = resizedTexture.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat);
                    }

                    if (bytes != null)
                    {
                        File.WriteAllBytes(assetPath, bytes);
                        Debug.Log($"Resized texture saved to: {assetPath}");
                    }
                    else
                    {
                        Debug.LogError($"Unsupported texture format for file: {assetPath}");
                    }

                    // Update import settings for mobile
                    UpdateImportSettings(assetPath);

                    // Refresh the AssetDatabase to reflect changes
                    AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private int GetResizeValue(int width, int height)
        {
            for (int i = 0; i < _resolutionThresholds.Count; i++)
            {
                if (width >= _resolutionThresholds[i] || height >= _resolutionThresholds[i])
                {
                    return _resizeValues[i];
                }
            }
            return Mathf.Max(width, height); // Default to original size if no threshold is met
        }

        private void BackupOriginalTexture(string assetPath)
        {
            string relativePath = assetPath.Replace("Assets", "").TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            string backupPath = Path.Combine(_backupFolderPath, relativePath);

            string directory = Path.GetDirectoryName(backupPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.Copy(assetPath, backupPath, true);
            Debug.Log($"Backed up texture to: {backupPath}");
        }

        private Texture2D ResizeTexture(Texture2D originalTexture, int maxDimension)
        {
            int newWidth, newHeight;
            if (originalTexture.width > originalTexture.height)
            {
                newWidth = maxDimension;
                newHeight = Mathf.RoundToInt((float)originalTexture.height / originalTexture.width * maxDimension);
            }
            else
            {
                newHeight = maxDimension;
                newWidth = Mathf.RoundToInt((float)originalTexture.width / originalTexture.height * maxDimension);
            }

            RenderTexture rt = new RenderTexture(newWidth, newHeight, 24);
            RenderTexture.active = rt;
            Graphics.Blit(originalTexture, rt);

            Texture2D newTexture = new Texture2D(newWidth, newHeight);
            newTexture.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
            newTexture.Apply();

            RenderTexture.active = null;
            rt.Release();

            return newTexture;
        }

        private void UpdateImportSettings(string assetPath)
        {
            TextureImporter textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (textureImporter != null)
            {
                // Set settings for Android
                TextureImporterPlatformSettings androidSettings = textureImporter.GetPlatformTextureSettings("Android");
                androidSettings.overridden = true;
                androidSettings.format = TextureImporterFormat.ASTC_4x4;
                textureImporter.SetPlatformTextureSettings(androidSettings);

                // Set settings for iOS
                TextureImporterPlatformSettings iosSettings = textureImporter.GetPlatformTextureSettings("iPhone");
                iosSettings.overridden = true;
                iosSettings.format = TextureImporterFormat.ASTC_4x4;
                textureImporter.SetPlatformTextureSettings(iosSettings);

                // Apply the changes
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            }
        }
    }
}

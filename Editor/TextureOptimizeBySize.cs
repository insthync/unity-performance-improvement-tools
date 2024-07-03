using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace Insthync.PerformanceImprovementTools
{
    public class TextureOptimizeBySize : EditorWindow
    {
        private enum SizeOption
        {
            MultiplyBy4,
            PowerOf2
        }

        private enum ResizeMethod
        {
            IncreaseCanvas,
            StretchTexture
        }

        private List<Texture2D> _texturesToResize = new List<Texture2D>();
        private Vector2 _scrollPosition;
        private int _itemsPerPage = 10;
        private int _currentPage = 0;
        private bool[] _selectedTextures;
        private SizeOption[] _sizeOptions;
        private ResizeMethod[] _resizeMethods;
        private string _selectedFolderPath = "";
        private string _backupFolderPath = "";

        [MenuItem("Tools/Performance Tools/Texture Optimize By Size")]
        public static void ShowWindow()
        {
            GetWindow<TextureOptimizeBySize>("Texture Optimize By Size");
        }

        private void OnGUI()
        {
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

            if (!string.IsNullOrEmpty(_selectedFolderPath) && GUILayout.Button("Find Textures"))
            {
                FindTexturesInFolder(_selectedFolderPath);
            }

            if (_texturesToResize.Count > 0)
            {
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
                GUILayout.Label($"Page {_currentPage + 1} of {Mathf.CeilToInt((float)_texturesToResize.Count / _itemsPerPage)}", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Next", GUILayout.Width(100)) && (_currentPage + 1) * _itemsPerPage < _texturesToResize.Count)
                {
                    _currentPage++;
                }
                GUILayout.EndHorizontal();

                _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(400));

                int startIndex = _currentPage * _itemsPerPage;
                int endIndex = Mathf.Min(startIndex + _itemsPerPage, _texturesToResize.Count);
                for (int i = startIndex; i < endIndex; i++)
                {
                    GUILayout.BeginHorizontal();
                    _selectedTextures[i] = EditorGUILayout.Toggle(_selectedTextures[i], GUILayout.Width(20));
                    GUILayout.Label(AssetDatabase.GetAssetPath(_texturesToResize[i]));
                    _sizeOptions[i] = (SizeOption)EditorGUILayout.EnumPopup(_sizeOptions[i]);
                    _resizeMethods[i] = (ResizeMethod)EditorGUILayout.EnumPopup(_resizeMethods[i]);
                    GUILayout.EndHorizontal();
                }

                GUILayout.EndScrollView();

                GUILayout.Space(10);

                if (GUILayout.Button("Resize Selected Textures"))
                {
                    ResizeSelectedTextures();
                }
            }
        }

        private void FindTexturesInFolder(string folderPath)
        {
            _texturesToResize.Clear();
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture != null && (!IsMultipleOfFour(texture.width) || !IsMultipleOfFour(texture.height)))
                {
                    _texturesToResize.Add(texture);
                }
            }

            _selectedTextures = new bool[_texturesToResize.Count];
            _sizeOptions = new SizeOption[_texturesToResize.Count];
            _resizeMethods = new ResizeMethod[_texturesToResize.Count];
            _currentPage = 0;
            Debug.Log($"Found {_texturesToResize.Count} textures to resize in folder '{folderPath}'.");
        }

        private bool IsMultipleOfFour(int value)
        {
            return value % 4 == 0;
        }

        private void ResizeSelectedTextures()
        {
            foreach (var index in GetSelectedIndices())
            {
                if (!string.IsNullOrEmpty(_backupFolderPath))
                {
                    BackupTexture(_texturesToResize[index]);
                }

                int newWidth = _texturesToResize[index].width;
                int newHeight = _texturesToResize[index].height;

                switch (_sizeOptions[index])
                {
                    case SizeOption.MultiplyBy4:
                        newWidth = MakeMultipleOfFour(newWidth);
                        newHeight = MakeMultipleOfFour(newHeight);
                        break;
                    case SizeOption.PowerOf2:
                        newWidth = Mathf.NextPowerOfTwo(newWidth);
                        newHeight = Mathf.NextPowerOfTwo(newHeight);
                        break;
                }

                switch (_resizeMethods[index])
                {
                    case ResizeMethod.IncreaseCanvas:
                        ResizeTextureIncreaseCanvas(_texturesToResize[index], newWidth, newHeight);
                        break;
                    case ResizeMethod.StretchTexture:
                        ResizeTextureStretch(_texturesToResize[index], newWidth, newHeight);
                        break;
                }

                UpdateImportSettings(AssetDatabase.GetAssetPath(_texturesToResize[index]));
            }

            AssetDatabase.Refresh();
            Debug.Log("Resizing completed.");
        }

        private void BackupTexture(Texture2D texture)
        {
            string path = AssetDatabase.GetAssetPath(texture);
            string relativePath = path.Replace("Assets", "").TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            string backupPath = Path.Combine(_backupFolderPath, relativePath);

            string directory = Path.GetDirectoryName(backupPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.Copy(path, backupPath, true);
            Debug.Log($"Backed up texture to: {backupPath}");
        }

        private void ResizeTextureIncreaseCanvas(Texture2D texture, int newWidth, int newHeight)
        {
            RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight, 0, RenderTextureFormat.ARGB32);
            RenderTexture.active = rt;

            // Clear the render texture with transparent color
            GL.Clear(true, true, Color.clear);

            Vector2 scale = Vector2.one;
            Vector2 blitOffsets = Vector2.zero;
            if (!Mathf.Approximately(newWidth, newHeight))
            {
                if (newWidth > newHeight)
                {
                    // Width > Height
                    float hScale = (float)newHeight / (float)texture.height;
                    float hOffsets = (1f - hScale) * 0.5f;
                    scale = new Vector2(1f, hScale);
                    blitOffsets = new Vector2(0, hOffsets);
                }
                else
                {
                    // Width < Height
                    float wScale = (float)newWidth / (float)texture.width;
                    float wOffsets = (1f - wScale) * 0.5f;
                    scale = new Vector2(wScale, 1f);
                    blitOffsets = new Vector2(wOffsets, 0);
                }
            }
            Debug.Log($"Resizing texture (increase canvas): {texture.name} to {newWidth} x {newHeight}, from {texture.width} x {texture.height}");

            // Blit the original texture to the render texture at the calculated offsets
            Graphics.Blit(texture, rt, scale, blitOffsets);

            Texture2D resizedTexture = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false);
            resizedTexture.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
            resizedTexture.Apply();

            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);

            SaveResizedTexture(texture, resizedTexture);
        }

        private void ResizeTextureStretch(Texture2D texture, int newWidth, int newHeight)
        {
            RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight, 0, RenderTextureFormat.ARGB32);
            RenderTexture.active = rt;

            Debug.Log($"Resizing texture (stretch): {texture.name} to {newWidth} x {newHeight}, from {texture.width} x {texture.height}");

            // Blit the original texture to the render texture with resizing
            Graphics.Blit(texture, rt);

            Texture2D resizedTexture = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false);
            resizedTexture.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
            resizedTexture.Apply();

            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);

            SaveResizedTexture(texture, resizedTexture);
        }

        private void SaveResizedTexture(Texture2D originalTexture, Texture2D resizedTexture)
        {
            string path = AssetDatabase.GetAssetPath(originalTexture);
            byte[] bytes = null;
            string extension = Path.GetExtension(path).ToLower();
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
                File.WriteAllBytes(path, bytes);
                Debug.Log($"Resized texture saved to: {path}");
            }
            else
            {
                Debug.LogError($"Unsupported texture format for file: {path}");
            }
            Debug.Log($"Resized texture '{path}' to: {resizedTexture.width}x{resizedTexture.height}");
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

        private int MakeMultipleOfFour(int value)
        {
            return Mathf.CeilToInt(value / 4.0f) * 4;
        }

        private void SelectAllTextures(bool select)
        {
            for (int i = 0; i < _selectedTextures.Length; i++)
            {
                _selectedTextures[i] = select;
            }
        }

        private IEnumerable<int> GetSelectedIndices()
        {
            for (int i = 0; i < _selectedTextures.Length; i++)
            {
                if (_selectedTextures[i])
                {
                    yield return i;
                }
            }
        }
    }
}

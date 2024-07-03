using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine.U2D;
using System.Collections.Generic;

namespace Insthync.PerformanceImprovementTools
{
    public class SpriteAtlasByPrefabPacker : EditorWindow
    {
        private List<Sprite> _foundSprites = new List<Sprite>();
        private string _savePath = "Assets";
        private GameObject _selectedPrefab;

        [MenuItem("Tools/Performance Tools/Sprite Atlas By Prefab Packer")]
        public static void ShowWindow()
        {
            GetWindow<SpriteAtlasByPrefabPacker>("Sprite Atlas By Prefab Packer");
        }

        private void OnGUI()
        {
            GUILayout.Label("Select Prefab", EditorStyles.boldLabel);
            _selectedPrefab = (GameObject)EditorGUILayout.ObjectField("Prefab", _selectedPrefab, typeof(GameObject), false);

            if (_selectedPrefab != null && GUILayout.Button("Find UI Sprites"))
            {
                FindUISprites();
            }

            if (_foundSprites.Count > 0)
            {
                GUILayout.Label("Found Sprites:");
                foreach (var sprite in _foundSprites)
                {
                    GUILayout.Label(sprite.name);
                }

                if (GUILayout.Button("Save Sprite Atlas"))
                {
                    SaveSpriteAtlas();
                }
            }
        }

        private void FindUISprites()
        {
            _foundSprites.Clear();
            if (_selectedPrefab != null)
            {
                Image[] images = _selectedPrefab.GetComponentsInChildren<Image>(true);
                foreach (var image in images)
                {
                    if (image.sprite != null && !_foundSprites.Contains(image.sprite))
                    {
                        _foundSprites.Add(image.sprite);
                    }
                }

                Debug.Log($"Found {_foundSprites.Count} unique sprites in the selected prefab.");
            }
        }

        private void SaveSpriteAtlas()
        {
            string path = EditorUtility.SaveFilePanel("Save Sprite Atlas", _savePath, "SpriteAtlas", "spriteatlas");
            if (!string.IsNullOrEmpty(path))
            {
                _savePath = FileUtil.GetProjectRelativePath(path);
            }
            else
            {
                Debug.LogError("Save path is invalid.");
                return;
            }

            // Load or create a SpriteAtlas
            SpriteAtlas spriteAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(_savePath);
            if (spriteAtlas == null)
            {
                spriteAtlas = new SpriteAtlas();
                AssetDatabase.CreateAsset(spriteAtlas, _savePath);
            }

            // Add found sprites to the atlas
            spriteAtlas.Add(_foundSprites.ToArray());
            EditorUtility.SetDirty(spriteAtlas);
            AssetDatabase.SaveAssets();

            Debug.Log("Sprites added to the sprite atlas.");
        }
    }
}

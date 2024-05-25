using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace Insthync.PerformanceImprovementTools
{
    public class TriCountEditor : EditorWindow
    {
        public enum SortMode
        {
            ByName,
            ByTriCount,
        }
        public enum SortOrder
        {
            Asc,
            Desc,
        }
        private List<Component> _meshComponents = new List<Component>();
        private List<KeyValuePair<int, Mesh>> _meshes = new List<KeyValuePair<int, Mesh>>();
        private Vector2 _scrollPos;
        private SortMode _sortMode = SortMode.ByName;
        private SortOrder _sortOrder = SortOrder.Asc;

        [MenuItem("Tools/In-Scenes Tri Counter")]
        public static void ShowWindow()
        {
            GetWindow<TriCountEditor>("In-Scenes Tri Counter");
        }

        private void OnGUI()
        {
            if (GUILayout.Button("Count Meshes Tri"))
            {
                FindMeshes();
            }

            if (_meshes.Count > 0)
            {
                GUILayout.BeginHorizontal();

                if (GUILayout.Button($"Name ({_sortOrder})", EditorStyles.boldLabel, GUILayout.Width(300)))
                {
                    if (_sortMode == SortMode.ByName)
                        _sortOrder = _sortOrder == SortOrder.Desc ? SortOrder.Asc : SortOrder.Desc;
                    else
                        _sortOrder = SortOrder.Asc;
                    _sortMode = SortMode.ByName;
                    SortMeshes();
                }

                if (GUILayout.Button($"Tri Count ({_sortOrder})", EditorStyles.boldLabel, GUILayout.Width(125)))
                {
                    if (_sortMode == SortMode.ByTriCount)
                        _sortOrder = _sortOrder == SortOrder.Desc ? SortOrder.Asc : SortOrder.Desc;
                    else
                        _sortOrder = SortOrder.Asc;
                    _sortMode = SortMode.ByTriCount;
                    SortMeshes();
                }

                GUILayout.Label("Prefab", EditorStyles.boldLabel, GUILayout.Width(200));

                GUILayout.EndHorizontal();

                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
                foreach (KeyValuePair<int, Mesh> mesh in _meshes)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(mesh.Value.name, GUILayout.Width(300));
                    GUILayout.Label(mesh.Value.triangles.Length.ToString("N0"), GUILayout.Width(125));
                    GameObject tempPrefab = null;
                    GameObject tempInstanceRoot = PrefabUtility.GetNearestPrefabInstanceRoot(_meshComponents[mesh.Key]);
                    if (tempInstanceRoot != null)
                    {
                        tempPrefab = PrefabUtility.GetCorrespondingObjectFromSource(tempInstanceRoot);
                    }
                    GUI.enabled = false;
                    EditorGUILayout.ObjectField(tempPrefab, typeof(GameObject), false, GUILayout.Width(200));
                    GUI.enabled = true;
                    GUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void FindMeshes()
        {
            _meshComponents.Clear();
            _meshes.Clear();
            for (int i = 0; i < SceneManager.sceneCount; ++i)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                {
                    continue;
                }
                GameObject[] rootGameObjects = scene.GetRootGameObjects();
                for (int j = 0; j < rootGameObjects.Length; ++j)
                {
                    MeshFilter[] meshFilters = rootGameObjects[j].GetComponentsInChildren<MeshFilter>();
                    foreach (MeshFilter meshFilter in meshFilters)
                    {
                        _meshes.Add(new KeyValuePair<int, Mesh>(_meshComponents.Count, meshFilter.sharedMesh));
                        _meshComponents.Add(meshFilter);
                    }
                    SkinnedMeshRenderer[] skinnedMeshes = rootGameObjects[j].GetComponentsInChildren<SkinnedMeshRenderer>();
                    foreach (SkinnedMeshRenderer skinnedMesh in skinnedMeshes)
                    {
                        _meshes.Add(new KeyValuePair<int, Mesh>(_meshComponents.Count, skinnedMesh.sharedMesh));
                        _meshComponents.Add(skinnedMesh);
                    }
                }
            }

            SortMeshes();
        }

        private void SortMeshes()
        {
            switch (_sortMode)
            {
                case SortMode.ByTriCount:
                    _meshes.Sort((a, b) => (a.Value.triangles.Length).CompareTo(b.Value.triangles.Length));
                    break;
                default:
                    _meshes.Sort((a, b) => a.Value.name.CompareTo(b.Value.name));
                    break;
            }
            if (_sortOrder == SortOrder.Desc)
                _meshes.Reverse();
        }
    }
}
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
        private List<Mesh> _meshes = new List<Mesh>();
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

                if (GUILayout.Button($"Tri Count ({_sortOrder})", EditorStyles.boldLabel, GUILayout.Width(300)))
                {
                    if (_sortMode == SortMode.ByTriCount)
                        _sortOrder = _sortOrder == SortOrder.Desc ? SortOrder.Asc : SortOrder.Desc;
                    else
                        _sortOrder = SortOrder.Asc;
                    _sortMode = SortMode.ByTriCount;
                    SortMeshes();
                }

                GUILayout.Label("Prefab", EditorStyles.boldLabel, GUILayout.Width(300));

                GUILayout.EndHorizontal();

                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
                foreach (Mesh mesh in _meshes)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(mesh.name, GUILayout.Width(300));
                    GUILayout.Label(mesh.triangles.Length.ToString("N0"), GUILayout.Width(300));
                    GameObject tempPrefab = null;
                    GameObject tempInstanceRoot = PrefabUtility.GetNearestPrefabInstanceRoot(mesh);
                    if (tempInstanceRoot != null)
                    {
                        tempPrefab = PrefabUtility.GetCorrespondingObjectFromSource(tempInstanceRoot);
                    }
                    GUI.enabled = false;
                    EditorGUILayout.ObjectField(tempPrefab, typeof(GameObject), false, GUILayout.Width(300));
                    GUI.enabled = true;
                    GUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void FindMeshes()
        {
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
                        _meshes.Add(meshFilter.sharedMesh);
                    }
                    SkinnedMeshRenderer[] skinnedMeshes = rootGameObjects[j].GetComponentsInChildren<SkinnedMeshRenderer>();
                    foreach (SkinnedMeshRenderer skinnedMesh in skinnedMeshes)
                    {
                        _meshes.Add(skinnedMesh.sharedMesh);
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
                    _meshes.Sort((a, b) => (a.triangles.Length).CompareTo(b.triangles.Length));
                    break;
                default:
                    _meshes.Sort((a, b) => a.name.CompareTo(b.name));
                    break;
            }
            if (_sortOrder == SortOrder.Desc)
                _meshes.Reverse();
        }
    }
}
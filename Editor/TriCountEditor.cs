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
            ByObject,
            ByTriCount,
            ByUsage,
        }
        public enum SortOrder
        {
            Asc,
            Desc,
        }
        public class MeshInfo
        {
            public Component component;
            public GameObject prefab;
            public Mesh mesh;
        }
        private Dictionary<GameObject, int> _usages = new Dictionary<GameObject, int>();
        private List<MeshInfo> _meshes = new List<MeshInfo>();
        private Vector2 _scrollPos;
        private SortMode _sortMode = SortMode.ByObject;
        private SortOrder _sortOrder = SortOrder.Asc;


        public int GetUsage(MeshInfo mesh)
        {
            if (mesh == null || mesh.prefab == null || !_usages.TryGetValue(mesh.prefab, out int usage))
                usage = 1;
            return usage;
        }

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

                if (GUILayout.Button($"Object ({_sortOrder})", EditorStyles.boldLabel, GUILayout.Width(200)))
                {
                    if (_sortMode == SortMode.ByObject)
                        _sortOrder = _sortOrder == SortOrder.Desc ? SortOrder.Asc : SortOrder.Desc;
                    else
                        _sortOrder = SortOrder.Asc;
                    _sortMode = SortMode.ByObject;
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

                if (GUILayout.Button($"Usage ({_sortOrder})", EditorStyles.boldLabel, GUILayout.Width(125)))
                {
                    if (_sortMode == SortMode.ByUsage)
                        _sortOrder = _sortOrder == SortOrder.Desc ? SortOrder.Asc : SortOrder.Desc;
                    else
                        _sortOrder = SortOrder.Asc;
                    _sortMode = SortMode.ByUsage;
                    SortMeshes();
                }

                GUILayout.Label("Prefab", EditorStyles.boldLabel, GUILayout.Width(200));

                GUILayout.EndHorizontal();

                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
                foreach (MeshInfo mesh in _meshes)
                {
                    GUILayout.BeginHorizontal();
                    // Object column
                    GUI.enabled = false;
                    EditorGUILayout.ObjectField(mesh.component, typeof(GameObject), false, GUILayout.Width(200));
                    GUI.enabled = true;
                    // Tri count column
                    GUILayout.Label(mesh.mesh.triangles.Length.ToString("N0"), GUILayout.Width(125));
                    // Usage column
                    GUILayout.Label(GetUsage(mesh).ToString("N0"), GUILayout.Width(125));
                    // Prefab column
                    GUI.enabled = false;
                    EditorGUILayout.ObjectField(mesh.prefab, typeof(GameObject), false, GUILayout.Width(200));
                    GUI.enabled = true;
                    GUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void FindMeshes()
        {
            _meshes.Clear();
            _usages.Clear();
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
                        GameObject tempPrefab = null;
                        GameObject tempInstanceRoot = PrefabUtility.GetNearestPrefabInstanceRoot(meshFilter);
                        if (tempInstanceRoot != null)
                        {
                            tempPrefab = PrefabUtility.GetCorrespondingObjectFromSource(tempInstanceRoot);
                        }
                        MeshInfo mesh = new MeshInfo()
                        {
                            component = meshFilter,
                            prefab = tempPrefab,
                            mesh = meshFilter.sharedMesh,
                        };
                        _meshes.Add(mesh);
                        if (tempPrefab != null)
                        {
                            if (!_usages.TryGetValue(tempPrefab, out int usage))
                                usage = 1;
                            usage++;
                            _usages[tempPrefab] = usage;
                        }
                    }
                    SkinnedMeshRenderer[] skinnedMeshes = rootGameObjects[j].GetComponentsInChildren<SkinnedMeshRenderer>();
                    foreach (SkinnedMeshRenderer skinnedMesh in skinnedMeshes)
                    {
                        GameObject tempPrefab = null;
                        GameObject tempInstanceRoot = PrefabUtility.GetNearestPrefabInstanceRoot(skinnedMesh);
                        if (tempInstanceRoot != null)
                        {
                            tempPrefab = PrefabUtility.GetCorrespondingObjectFromSource(tempInstanceRoot);
                        }
                        MeshInfo mesh = new MeshInfo()
                        {
                            component = skinnedMesh,
                            prefab = tempPrefab,
                            mesh = skinnedMesh.sharedMesh,
                        };
                        _meshes.Add(mesh);
                        if (tempPrefab != null)
                        {
                            if (!_usages.TryGetValue(tempPrefab, out int usage))
                                usage = 1;
                            usage++;
                            _usages[tempPrefab] = usage;
                        }
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
                    _meshes.Sort((a, b) => a.mesh.triangles.Length.CompareTo(b.mesh.triangles.Length));
                    break;
                case SortMode.ByUsage:
                    _meshes.Sort((a, b) => GetUsage(a).CompareTo(GetUsage(b)));
                    break;
                default:
                    _meshes.Sort((a, b) => a.mesh.name.CompareTo(b.mesh.name));
                    break;
            }
            if (_sortOrder == SortOrder.Desc)
                _meshes.Reverse();
        }
    }
}
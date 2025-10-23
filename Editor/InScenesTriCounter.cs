using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace Insthync.PerformanceImprovementTools
{
    public class InScenesTriCounter : EditorWindow
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
        private Dictionary<GameObject, int> _prefabUsages = new Dictionary<GameObject, int>();
        private List<MeshInfo> _meshes = new List<MeshInfo>();
        private Vector2 _scrollPos;
        private SortMode _sortMode = SortMode.ByObject;
        private SortOrder _sortOrder = SortOrder.Asc;
        private int _currentPage = 0;
        private int _itemsPerPage = 100;

        public int GetUsage(MeshInfo mesh)
        {
            if (mesh == null || mesh.prefab == null || !_prefabUsages.TryGetValue(mesh.prefab, out int usage))
                usage = 1;
            return usage;
        }

        [MenuItem("Tools/Performance Tools/In-Scenes Tri Counter")]
        public static void ShowWindow()
        {
            GetWindow<InScenesTriCounter>("In-Scenes Tri Counter");
        }

        private void OnGUI()
        {
            if (GUILayout.Button("Count Meshes Tri"))
            {
                FindMeshes();
            }

            if (_meshes.Count > 0)
            {
                // Pagination controls
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Previous", GUILayout.Width(100)) && _currentPage > 0)
                {
                    _currentPage--;
                }
                GUILayout.FlexibleSpace();
                GUILayout.Label($"Page {_currentPage + 1} of {Mathf.CeilToInt((float)_meshes.Count / _itemsPerPage)}");
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Next", GUILayout.Width(100)) && (_currentPage + 1) * _itemsPerPage < _meshes.Count)
                {
                    _currentPage++;
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();

                if (GUILayout.Button("Object" + (_sortMode == SortMode.ByObject ? $"({_sortOrder})" : string.Empty), EditorStyles.boldLabel, GUILayout.Width(200)))
                {
                    if (_sortMode == SortMode.ByObject)
                        _sortOrder = _sortOrder == SortOrder.Desc ? SortOrder.Asc : SortOrder.Desc;
                    else
                        _sortOrder = SortOrder.Asc;
                    _sortMode = SortMode.ByObject;
                    SortMeshes();
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Tri Count " + (_sortMode == SortMode.ByTriCount ? $"({_sortOrder})" : string.Empty), EditorStyles.boldLabel, GUILayout.Width(125)))
                {
                    if (_sortMode == SortMode.ByTriCount)
                        _sortOrder = _sortOrder == SortOrder.Desc ? SortOrder.Asc : SortOrder.Desc;
                    else
                        _sortOrder = SortOrder.Asc;
                    _sortMode = SortMode.ByTriCount;
                    SortMeshes();
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Prefab Usage " + (_sortMode == SortMode.ByUsage ? $"({_sortOrder})" : string.Empty), EditorStyles.boldLabel, GUILayout.Width(125)))
                {
                    if (_sortMode == SortMode.ByUsage)
                        _sortOrder = _sortOrder == SortOrder.Desc ? SortOrder.Asc : SortOrder.Desc;
                    else
                        _sortOrder = SortOrder.Asc;
                    _sortMode = SortMode.ByUsage;
                    SortMeshes();
                }
                GUILayout.FlexibleSpace();
                GUILayout.Label("Prefab ", EditorStyles.boldLabel, GUILayout.Width(200));

                GUILayout.EndHorizontal();

                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
                // Display current page of meshes
                int startIndex = _currentPage * _itemsPerPage;
                int endIndex = Mathf.Min(startIndex + _itemsPerPage, _meshes.Count);
                for (int i = startIndex; i < endIndex; i++)
                {
                    MeshInfo mesh = _meshes[i];
                    GUILayout.BeginHorizontal();
                    // Object column
                    GUI.enabled = false;
                    EditorGUILayout.ObjectField(mesh.component, typeof(GameObject), false, GUILayout.Width(200));
                    GUI.enabled = true;
                    GUILayout.FlexibleSpace();
                    // Tri count column
                    if (mesh.mesh != null)
                        GUILayout.Label(mesh.mesh.triangles.Length.ToString("N0"), GUILayout.Width(125));
                    else
                        GUILayout.Label("NULL", GUILayout.Width(125));
                    GUILayout.FlexibleSpace();
                    // Usage column
                    GUILayout.Label(GetUsage(mesh).ToString("N0"), GUILayout.Width(125));
                    GUILayout.FlexibleSpace();
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
            _prefabUsages.Clear();
            List<GameObject> countedInstances = new List<GameObject>();
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
                        StoreMesh(countedInstances, meshFilter, (comp) => comp.sharedMesh);
                    }
                    SkinnedMeshRenderer[] skinnedMeshes = rootGameObjects[j].GetComponentsInChildren<SkinnedMeshRenderer>();
                    foreach (SkinnedMeshRenderer skinnedMesh in skinnedMeshes)
                    {
                        StoreMesh(countedInstances, skinnedMesh, (comp) => comp.sharedMesh);
                    }
                }
            }

            SortMeshes();
        }

        private void StoreMesh<T>(List<GameObject> countedInstances, T comp, System.Func<T, Mesh> getMesh) where T : Component
        {
            GameObject tempPrefab = null;
            GameObject tempInstanceRoot = PrefabUtility.GetNearestPrefabInstanceRoot(comp);
            if (tempInstanceRoot != null)
                tempPrefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(tempInstanceRoot);
            MeshInfo mesh = new MeshInfo()
            {
                component = comp,
                prefab = tempPrefab,
                mesh = getMesh(comp),
            };
            _meshes.Add(mesh);
            if (!countedInstances.Contains(tempInstanceRoot) && tempPrefab != null)
            {
                countedInstances.Add(tempInstanceRoot);
                if (!_prefabUsages.TryGetValue(tempPrefab, out int usage))
                    usage = 0;
                usage++;
                _prefabUsages[tempPrefab] = usage;
            }
        }

        private void SortMeshes()
        {
            switch (_sortMode)
            {
                case SortMode.ByTriCount:
                    _meshes.Sort((a, b) =>
                    {
                        if (a.mesh == null && b.mesh == null)
                            return 0;
                        else if (a.mesh == null)
                            return -1;
                        else if (b.mesh == null)
                            return 1;
                        return a.mesh.triangles.Length.CompareTo(b.mesh.triangles.Length);
                    });
                    break;
                case SortMode.ByUsage:
                    _meshes.Sort((a, b) => GetUsage(a).CompareTo(GetUsage(b)));
                    break;
                default:
                    _meshes.Sort((a, b) =>
                    {
                        if (a.component == null && b.component == null)
                            return 0;
                        else if (a.component == null)
                            return -1;
                        else if (b.component == null)
                            return 1;
                        return a.component.name.CompareTo(b.component.name);
                    });
                    break;
            }
            if (_sortOrder == SortOrder.Desc)
                _meshes.Reverse();
        }
    }
}
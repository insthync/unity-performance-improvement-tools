using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace Insthync.PerformanceImprovementTools
{
    public class TriCountEditor : EditorWindow
    {
        private List<Mesh> meshes = new List<Mesh>();
        private Vector2 scrollPos;
        private bool sortByName = false;
        private bool sortByTriCount = false;

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

            if (meshes.Count > 0)
            {
                GUILayout.BeginHorizontal();

                if (GUILayout.Button("Sort by Mesh Name", EditorStyles.boldLabel, GUILayout.Width(200)))
                {
                    sortByName = !sortByName;
                    sortByTriCount = false;
                    SortMeshes();
                }

                if (GUILayout.Button("Sort by Tri Count", EditorStyles.boldLabel, GUILayout.Width(100)))
                {
                    sortByTriCount = !sortByTriCount;
                    sortByName = false;
                    SortMeshes();
                }

                GUILayout.EndHorizontal();

                scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(300));
                foreach (Mesh mesh in meshes)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(mesh.name, GUILayout.Width(200));
                    GUILayout.Label($"{mesh.triangles.Length}", GUILayout.Width(100));
                    GUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void FindMeshes()
        {
            meshes.Clear();
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
                        meshes.Add(meshFilter.sharedMesh);
                    }
                    SkinnedMeshRenderer[] skinnedMeshes = rootGameObjects[j].GetComponentsInChildren<SkinnedMeshRenderer>();
                    foreach (SkinnedMeshRenderer skinnedMesh in skinnedMeshes)
                    {
                        meshes.Add(skinnedMesh.sharedMesh);
                    }
                }
            }

            SortMeshes();
        }

        private void SortMeshes()
        {
            if (sortByName)
            {
                meshes.Sort((a, b) => a.name.CompareTo(b.name) * (sortByName ? 1 : -1));
            }
            else if (sortByTriCount)
            {
                meshes.Sort((a, b) => (a.triangles.Length).CompareTo(b.triangles.Length) * (sortByTriCount ? 1 : -1));
            }
        }
    }
}
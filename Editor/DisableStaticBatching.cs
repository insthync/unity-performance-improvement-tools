using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace Insthync.PerformanceImprovementTools
{
    public class DisableStaticBatching : MonoBehaviour
    {
        [MenuItem("Tools/Performance Tools/Disable Static Batching")]
        public static void ProceedDisableStaticBatching()
        {
            List<GameObject> objs = GetGameObjectsFromAllLoadedScenes();
            foreach (GameObject obj in objs)
            {
                var flags = GameObjectUtility.GetStaticEditorFlags(obj);
                if ((flags & StaticEditorFlags.BatchingStatic) == StaticEditorFlags.BatchingStatic)
                {
                    // Has a batching static
                    GameObjectUtility.SetStaticEditorFlags(obj, flags & ~StaticEditorFlags.BatchingStatic);
                    Debug.Log($"[DisableStaticBatching] Remove static batching from {obj}", obj);
                }
            }
        }

        private static List<GameObject> GetGameObjectsFromAllLoadedScenes()
        {
            List<GameObject> gameObjects = new List<GameObject>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                {
                    continue;
                }
                GameObject[] rootGameObjects = scene.GetRootGameObjects();
                foreach (GameObject rootObject in rootGameObjects)
                {
                    gameObjects.AddRange(GetAllChildGameObjects(rootObject));
                }
            }
            return gameObjects;
        }

        private static List<GameObject> GetAllChildGameObjects(GameObject parent)
        {
            List<GameObject> allObjects = new List<GameObject> { parent };
            foreach (Transform child in parent.transform)
            {
                allObjects.AddRange(GetAllChildGameObjects(child.gameObject));
            }
            return allObjects;
        }
    }
}

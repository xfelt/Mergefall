using MergeSurvivor.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MergeSurvivor.Editor
{
    public static class LaunchSceneTools
    {
        [MenuItem("Merge Survivor/Create Launch Scene")]
        public static void CreateLaunchScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("PrototypeBootstrap");
            root.AddComponent<PrototypeBootstrap>();
            EditorSceneManager.SaveScene(scene, "Assets/_Project/Gameplay/Scenes/Launch.unity");
            AssetDatabase.SaveAssets();
            Debug.Log("Created Assets/_Project/Gameplay/Scenes/Launch.unity");
        }
    }
}

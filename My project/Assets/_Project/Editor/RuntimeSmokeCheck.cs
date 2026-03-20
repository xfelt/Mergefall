using System;
using System.Reflection;
using MergeSurvivor.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MergeSurvivor.Editor
{
    public static class RuntimeSmokeCheck
    {
        // Run with:
        // Unity.exe -batchmode -nographics -projectPath "<path>" -executeMethod MergeSurvivor.Editor.RuntimeSmokeCheck.Run -quit
        public static void Run()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("SmokeBootstrap");
            var bootstrap = go.AddComponent<PrototypeBootstrap>();

            try
            {
                InvokeNonPublic(bootstrap, "SetupServices");
                InvokeNonPublic(bootstrap, "BuildUi");
                InvokeNonPublic(bootstrap, "RefreshAll");
                Debug.Log("[SmokeCheck] Bootstrap runtime path executed without exception.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SmokeCheck] Failed: {ex}");
                throw;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        private static void InvokeNonPublic(object instance, string methodName)
        {
            var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
            {
                throw new MissingMethodException(instance.GetType().Name, methodName);
            }

            method.Invoke(instance, null);
        }
    }
}

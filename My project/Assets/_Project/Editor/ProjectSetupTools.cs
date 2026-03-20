using System.Collections.Generic;
using MergeSurvivor.Data;
using MergeSurvivor.Platform;
using UnityEditor;
using UnityEngine;

namespace MergeSurvivor.Editor
{
    public static class ProjectSetupTools
    {
        private const string DataRoot = "Assets/_Project/Data/Generated";
        // Note: Default game content is now under Assets/_Project/Resources/MergeSurvivorData (see ContentPipeline).

        [MenuItem("Merge Survivor/Setup/Create Default Data Assets")]
        public static void CreateDefaultDataAssets()
        {
            ContentPipeline.CreateDefaultDataAssets();
        }

        [MenuItem("Merge Survivor/Setup/Apply Android Defaults")]
        public static void ApplyAndroidDefaults()
        {
            var platformConfig = Resources.Load<AppPlatformConfig>("AppPlatformConfig");
            var packageName = platformConfig != null && !string.IsNullOrWhiteSpace(platformConfig.AndroidPackage)
                ? platformConfig.AndroidPackage.Trim()
                : "com.company.mergesurvivor";

            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, packageName);
            if (platformConfig == null || platformConfig.UseProjectSettingsVersion)
            {
                PlayerSettings.bundleVersion = "0.1.0";
                PlayerSettings.Android.bundleVersionCode = 1;
            }
            else
            {
                PlayerSettings.bundleVersion = string.IsNullOrWhiteSpace(platformConfig.AndroidBundleVersion)
                    ? "0.1.0"
                    : platformConfig.AndroidBundleVersion.Trim();
                PlayerSettings.Android.bundleVersionCode = Mathf.Max(1, platformConfig.AndroidBundleVersionCode);
            }
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            EditorUserBuildSettings.buildAppBundle = true;
            Debug.Log("Android defaults applied (package/version/minSDK/AAB).");
        }

        private static void EnsureFolder(string path)
        {
            var split = path.Split('/');
            var current = split[0];
            for (var i = 1; i < split.Length; i++)
            {
                var next = $"{current}/{split[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, split[i]);
                }
                current = next;
            }
        }

        private static void CreateAssetIfMissing<T>(string path) where T : ScriptableObject
        {
            if (AssetDatabase.LoadAssetAtPath<T>(path) != null)
            {
                return;
            }

            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
        }

        private static ItemDefinition CreateItemAsset(string id, string displayName, string familyId, int tier, int power)
        {
            var path = $"{DataRoot}/{id}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (existing != null)
            {
                existing.ConfigureRuntime(id, displayName, familyId, tier, power);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.ConfigureRuntime(id, displayName, familyId, tier, power);
            AssetDatabase.CreateAsset(item, path);
            return item;
        }
    }
}

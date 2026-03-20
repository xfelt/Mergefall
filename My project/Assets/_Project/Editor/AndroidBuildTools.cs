using System;
using System.IO;
using System.Linq;
using MergeSurvivor.Platform;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MergeSurvivor.Editor
{
    public static class AndroidBuildTools
    {
        private const string BuildMenuRoot = "Merge Survivor/Build/";
        private const string OutputFolder = "Builds/Android";

        [MenuItem(BuildMenuRoot + "Build AAB for dev")]
        public static void BuildAabForDev()
        {
            BuildAndroidAab(BuildFlavor.Dev);
        }

        [MenuItem(BuildMenuRoot + "Build AAB for release")]
        public static void BuildAabForRelease()
        {
            BuildAndroidAab(BuildFlavor.Release);
        }

        [MenuItem(BuildMenuRoot + "Build AAB (choose flavor)")]
        public static void BuildAabChooseFlavor()
        {
            var choice = EditorUtility.DisplayDialogComplex(
                "Build Android AAB",
                "Choose the build flavor.\n\nDev: development build with debugging.\nRelease: production-ready build and release signing checks.",
                "Release",
                "Cancel",
                "Dev");

            if (choice == 0)
            {
                BuildAndroidAab(BuildFlavor.Release);
            }
            else if (choice == 2)
            {
                BuildAndroidAab(BuildFlavor.Dev);
            }
        }

        private static void BuildAndroidAab(BuildFlavor flavor)
        {
            EnsureAndroidBuildTarget();
            ApplyAndroidSettingsFromConfig();

            if (flavor == BuildFlavor.Release)
            {
                ConfigureReleaseKeystore();
            }

            EditorUserBuildSettings.buildAppBundle = true;
            var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            if (scenes.Length == 0)
            {
                throw new BuildFailedException("No enabled scenes in Build Settings.");
            }

            var outputDirectory = Path.GetFullPath(OutputFolder);
            Directory.CreateDirectory(outputDirectory);
            var fileName = $"MergeSurvivor-{flavor.ToString().ToLowerInvariant()}-v{PlayerSettings.bundleVersion}-{PlayerSettings.Android.bundleVersionCode}.aab";
            var outputPath = Path.Combine(outputDirectory, fileName);

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = flavor == BuildFlavor.Dev
                    ? BuildOptions.Development | BuildOptions.AllowDebugging
                    : BuildOptions.None
            };

            Debug.Log($"Starting Android AAB build ({flavor}) -> {outputPath}");
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException($"Android AAB build failed ({flavor}). Result: {report.summary.result}");
            }

            Debug.Log($"Android AAB build succeeded ({flavor}). Output: {outputPath}");
        }

        private static void EnsureAndroidBuildTarget()
        {
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
            {
                return;
            }

            var switched = EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            if (!switched)
            {
                throw new BuildFailedException("Failed to switch active build target to Android.");
            }
        }

        private static void ApplyAndroidSettingsFromConfig()
        {
            var config = LoadPlatformConfig();
            if (config == null)
            {
                Debug.Log("AppPlatformConfig not found. Using current Project Settings for package/version.");
                return;
            }

            if (!string.IsNullOrWhiteSpace(config.AndroidPackage))
            {
                PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, config.AndroidPackage.Trim());
            }

            if (config.UseProjectSettingsVersion)
            {
                return;
            }

            var version = string.IsNullOrWhiteSpace(config.AndroidBundleVersion)
                ? PlayerSettings.bundleVersion
                : config.AndroidBundleVersion.Trim();
            var versionCode = Mathf.Max(1, config.AndroidBundleVersionCode);

            PlayerSettings.bundleVersion = version;
            PlayerSettings.Android.bundleVersionCode = versionCode;
        }

        private static AppPlatformConfig LoadPlatformConfig()
        {
            var fromResources = Resources.Load<AppPlatformConfig>("AppPlatformConfig");
            if (fromResources != null)
            {
                return fromResources;
            }

            var guids = AssetDatabase.FindAssets("t:AppPlatformConfig");
            if (guids == null || guids.Length == 0)
            {
                return null;
            }

            var firstPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<AppPlatformConfig>(firstPath);
        }

        private static void ConfigureReleaseKeystore()
        {
            var keystorePath = FirstSetEnvironmentVariable("MS_ANDROID_KEYSTORE_PATH", "ANDROID_KEYSTORE_PATH");
            var keystorePassword = FirstSetEnvironmentVariable("MS_ANDROID_KEYSTORE_PASSWORD", "KEYSTORE_PASSWORD");
            var keyAliasName = FirstSetEnvironmentVariable("MS_ANDROID_KEYALIAS_NAME", "KEYALIAS_NAME");
            var keyAliasPassword = FirstSetEnvironmentVariable("MS_ANDROID_KEYALIAS_PASSWORD", "KEYALIAS_PASSWORD");

            var anyEnvValuePresent =
                !string.IsNullOrWhiteSpace(keystorePath) ||
                !string.IsNullOrWhiteSpace(keystorePassword) ||
                !string.IsNullOrWhiteSpace(keyAliasName) ||
                !string.IsNullOrWhiteSpace(keyAliasPassword);

            var allEnvValuesPresent =
                !string.IsNullOrWhiteSpace(keystorePath) &&
                !string.IsNullOrWhiteSpace(keystorePassword) &&
                !string.IsNullOrWhiteSpace(keyAliasName) &&
                !string.IsNullOrWhiteSpace(keyAliasPassword);

            if (anyEnvValuePresent && !allEnvValuesPresent)
            {
                throw new BuildFailedException("Release keystore env vars are partially set. Provide all of: MS_ANDROID_KEYSTORE_PATH, MS_ANDROID_KEYSTORE_PASSWORD, MS_ANDROID_KEYALIAS_NAME, MS_ANDROID_KEYALIAS_PASSWORD.");
            }

            if (allEnvValuesPresent)
            {
                var fullKeystorePath = Path.GetFullPath(keystorePath);
                if (!File.Exists(fullKeystorePath))
                {
                    throw new BuildFailedException($"Keystore file from env var was not found: {fullKeystorePath}");
                }

                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = fullKeystorePath;
                PlayerSettings.Android.keystorePass = keystorePassword;
                PlayerSettings.Android.keyaliasName = keyAliasName;
                PlayerSettings.Android.keyaliasPass = keyAliasPassword;
                return;
            }

            if (!PlayerSettings.Android.useCustomKeystore || string.IsNullOrWhiteSpace(PlayerSettings.Android.keystoreName))
            {
                throw new BuildFailedException("Release build requires a custom keystore. Set one in Player Settings, or provide env vars in CI.");
            }
        }

        private static string FirstSetEnvironmentVariable(params string[] names)
        {
            foreach (var name in names)
            {
                var value = Environment.GetEnvironmentVariable(name);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }

            return null;
        }

        private enum BuildFlavor
        {
            Dev,
            Release
        }
    }
}

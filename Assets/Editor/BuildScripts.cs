using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Game2048.Editor
{
    /// <summary>
    /// Command-line builds, e.g.:
    /// Unity -batchmode -quit -projectPath . -executeMethod Game2048.Editor.BuildScripts.BuildiOSSimulator
    /// </summary>
    public static class BuildScripts
    {
        [MenuItem("2048/Build/iOS Simulator (Xcode project)")]
        public static void BuildiOSSimulator()
        {
            PlayerSettings.iOS.sdkVersion = iOSSdkVersion.SimulatorSDK;
            PlayerSettings.iOS.simulatorSdkArchitecture = AppleMobileArchitectureSimulator.ARM64;
            Build(BuildTarget.iOS, "Builds/iOS-Sim", BuildOptions.Development);
        }

        [MenuItem("2048/Build/iOS Device (Xcode project)")]
        public static void BuildiOSDevice()
        {
            PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
            Build(BuildTarget.iOS, "Builds/iOS", BuildOptions.None);
        }

        [MenuItem("2048/Build/Android APK")]
        public static void BuildAndroidApk()
        {
            EditorUserBuildSettings.buildAppBundle = false;
            Build(BuildTarget.Android, "Builds/Android/2048.apk", BuildOptions.None);
        }

        [MenuItem("2048/Build/Android App Bundle (AAB)")]
        public static void BuildAndroidBundle()
        {
            EditorUserBuildSettings.buildAppBundle = true;
            Build(BuildTarget.Android, "Builds/Android/2048.aab", BuildOptions.None);
        }

        [MenuItem("2048/Build/Windows (x64)")]
        public static void BuildWindows() =>
            Build(BuildTarget.StandaloneWindows64, "Builds/Windows/2048.exe", BuildOptions.None);

        [MenuItem("2048/Build/macOS")]
        public static void BuildMac() =>
            Build(BuildTarget.StandaloneOSX, "Builds/macOS/2048.app", BuildOptions.None);

        private static void Build(BuildTarget target, string location, BuildOptions options)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(location)) ?? ".");

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ProjectBootstrap.ScenePath },
                locationPathName = location,
                target = target,
                options = options
            });

            var summary = report.summary;
            Debug.Log($"[2048] {target} build {summary.result}: {location} ({summary.totalSize / 1024} KB, {summary.totalErrors} errors)");

            if (summary.result != BuildResult.Succeeded)
            {
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw new Exception($"{target} build failed: {summary.result}");
            }
        }
    }
}

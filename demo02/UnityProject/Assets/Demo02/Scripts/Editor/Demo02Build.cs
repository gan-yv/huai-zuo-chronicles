using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Demo02.Editor
{
    public static class Demo02Build
    {
        private const string ProductName = "Huai-Zuo Chronicles";
        private const string Version = "1.7.0";
        private const string OutputDirectory = "Builds/WindowsDemo";
        private const string ExecutableName = "HuaiZuoChronicles.exe";

        [MenuItem("Demo02/Build Windows Demo")]
        public static void BuildWindowsDemo()
        {
            Demo02GreyboxBuilder.RebuildForBatch();
            ConfigurePlayer();

            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                throw new InvalidOperationException("No enabled scenes in Build Settings.");
            }

            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(
                BuildTargetGroup.Standalone,
                BuildTarget.StandaloneWindows64))
            {
                throw new InvalidOperationException("Could not switch to Windows x64 build target.");
            }

            Directory.CreateDirectory(OutputDirectory);
            string executablePath = Path.Combine(OutputDirectory, ExecutableName);

            BuildPlayerOptions options = new()
            {
                scenes = scenes,
                locationPathName = executablePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            string reportText =
                $"Product: {ProductName}{Environment.NewLine}" +
                $"Version: {Version}{Environment.NewLine}" +
                $"Built: {DateTime.Now:O}{Environment.NewLine}" +
                $"Target: {summary.platform}{Environment.NewLine}" +
                $"Result: {summary.result}{Environment.NewLine}" +
                $"Output: {Path.GetFullPath(executablePath)}{Environment.NewLine}" +
                $"Size: {summary.totalSize} bytes{Environment.NewLine}" +
                $"Duration: {summary.totalTime}{Environment.NewLine}" +
                $"Scenes: {string.Join(", ", scenes)}";

            File.WriteAllText(
                Path.Combine(OutputDirectory, "BuildInfo.txt"),
                reportText
            );

            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Windows build failed: {summary.result}. See {OutputDirectory}/BuildInfo.txt"
                );
            }

            Debug.Log($"Windows demo built at {Path.GetFullPath(executablePath)}");
        }

        private static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "gan-yv";
            PlayerSettings.productName = ProductName;
            PlayerSettings.bundleVersion = Version;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;

            AssetDatabase.SaveAssets();
        }
    }
}

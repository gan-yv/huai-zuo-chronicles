using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Demo v0.1 的 Windows 构建入口。
///
/// 编辑器内可从“Tools > 春江花月夜 > Build Windows Demo”启动；
/// 自动化构建时可向 Unity 传入 -executeMethod DemoBuilder.BuildWindowsDemoBatch。
/// </summary>
public static class DemoBuilder
{
    private const string DemoScenePath = "Assets/Scenes/Demo.unity";

    /// <summary>
    /// 从 Unity 菜单构建 Windows 64 位试玩版。
    /// </summary>
    [MenuItem("Tools/春江花月夜/Build Windows Demo")]
    public static void BuildWindowsDemo()
    {
        Build();
    }

    /// <summary>
    /// 命令行批处理构建入口。
    /// </summary>
    public static void BuildWindowsDemoBatch()
    {
        Build();
    }

    /// <summary>
    /// 创建构建目录并执行 Windows 64 位构建。
    /// </summary>
    private static void Build()
    {
        string projectRoot =
            Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string buildDirectory = Path.Combine(projectRoot, "Builds", "WindowsDemo");
        string executablePath = Path.Combine(buildDirectory, "SpringRiverYiben.exe");

        Directory.CreateDirectory(buildDirectory);

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { DemoScenePath },
            locationPathName = executablePath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Windows Demo 构建失败：{report.summary.result}");
        }

        Debug.Log(
            $"Windows Demo 构建完成：{executablePath}\n" +
            $"大小：{report.summary.totalSize / (1024f * 1024f):0.0} MB");
    }
}

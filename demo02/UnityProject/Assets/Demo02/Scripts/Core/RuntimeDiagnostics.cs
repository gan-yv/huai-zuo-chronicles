using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Demo02.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Demo02.Core
{
    public sealed class RuntimeDiagnostics : MonoBehaviour
    {
        [Serializable]
        private sealed class BenchmarkReport
        {
            public string Version;
            public string CompletedAtUtc;
            public int Width;
            public int Height;
            public int SampledFrames;
            public float DurationSeconds;
            public float AverageFps;
            public float OnePercentLowFps;
            public float MinimumFps;
            public float AverageFrameMs;
            public float WorstFrameMs;
            public long ManagedMemoryBytes;
            public string GraphicsDevice;
        }

        private readonly List<float> benchmarkFrameTimes = new();

        private bool overlayVisible;
        private bool benchmarkRunning;
        private int displayFrames;
        private float displayIntervalStart;
        private float displayFps;
        private float windowWorstFrameMs;
        private float windowWorstStartedAt;
        private long previousManagedMemory;
        private float nextMemorySample;
        private long managedMemoryDelta;

        private void Start()
        {
            previousManagedMemory = GC.GetTotalMemory(false);
            nextMemorySample = Time.unscaledTime + 1f;
            displayIntervalStart = Time.unscaledTime;

            int seconds = ReadIntArgument("--benchmark-seconds=", 0);
            if (seconds > 0)
            {
                string outputPath = GetArgumentValue("--benchmark-output=");
                bool quitAfter = Array.IndexOf(
                    Environment.GetCommandLineArgs(),
                    "--benchmark-exit"
                ) >= 0;
                StartCoroutine(RunBenchmark(seconds, outputPath, quitAfter));
            }
        }

        private void Update()
        {
            if (Keyboard.current != null
                && Keyboard.current.f3Key.wasPressedThisFrame)
            {
                overlayVisible = !overlayVisible;
                GameAudio.PlayInteraction();
            }

            float frameTime = Time.unscaledDeltaTime;
            displayFrames++;
            if (frameTime > windowWorstFrameMs)
            {
                windowWorstFrameMs = frameTime;
            }

            float elapsed = Time.unscaledTime - displayIntervalStart;
            if (elapsed >= 1f)
            {
                displayFps = displayFrames / elapsed;
                displayFrames = 0;
                displayIntervalStart = Time.unscaledTime;
            }
            if (Time.unscaledTime - windowWorstStartedAt >= 5f)
            {
                windowWorstFrameMs = 0f;
                windowWorstStartedAt = Time.unscaledTime;
            }
            if (Time.unscaledTime >= nextMemorySample)
            {
                long currentMemory = GC.GetTotalMemory(false);
                managedMemoryDelta = currentMemory - previousManagedMemory;
                previousManagedMemory = currentMemory;
                nextMemorySample = Time.unscaledTime + 1f;
            }

            if (benchmarkRunning)
            {
                benchmarkFrameTimes.Add(frameTime);
            }
        }

        private void OnGUI()
        {
            if (!overlayVisible)
            {
                return;
            }

            GUI.depth = -55;
            float scale = UiTheme.Scale;
            float width = 270f * scale;
            Rect panel = new(
                Screen.width - width - 18f * scale,
                158f * scale,
                width,
                132f * scale
            );
            UiTheme.DrawRect(panel, new Color(0.02f, 0.027f, 0.038f, 0.96f));
            UiTheme.DrawBorder(panel, new Color(1f, 1f, 1f, 0.12f));
            UiTheme.DrawRect(
                new Rect(panel.x, panel.y, 3f * scale, panel.height),
                UiTheme.Accent
            );

            GUIStyle title = UiTheme.Label(11, TextAnchor.MiddleLeft, bold: true);
            title.normal.textColor = UiTheme.Accent;
            GUI.Label(
                new Rect(
                    panel.x + 14f * scale,
                    panel.y + 5f * scale,
                    panel.width - 28f * scale,
                    20f * scale
                ),
                "RUNTIME · F3",
                title
            );
            GUI.Label(
                new Rect(
                    panel.x + 14f * scale,
                    panel.y + 27f * scale,
                    panel.width - 28f * scale,
                    92f * scale
                ),
                $"FPS        {displayFps:0.0}\n"
                + $"帧耗时     {Time.unscaledDeltaTime * 1000f:0.0} ms\n"
                + $"5秒最差    {windowWorstFrameMs * 1000f:0.0} ms\n"
                + $"GC 变化    {managedMemoryDelta / 1024f:0.0} KB/s\n"
                + $"显存预算   {SystemInfo.graphicsMemorySize} MB",
                UiTheme.Label(11)
            );
        }

        private IEnumerator RunBenchmark(
            int seconds,
            string outputPath,
            bool quitAfter)
        {
            yield return new WaitForSecondsRealtime(1.5f);
            benchmarkFrameTimes.Clear();
            benchmarkRunning = true;
            float startedAt = Time.unscaledTime;

            while (Time.unscaledTime - startedAt < seconds)
            {
                yield return null;
            }

            benchmarkRunning = false;
            BenchmarkReport report = BuildReport(Time.unscaledTime - startedAt);
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                outputPath = Path.Combine(
                    Application.persistentDataPath,
                    $"benchmark-v{Application.version}.json"
                );
            }
            outputPath = Path.GetFullPath(outputPath);
            string directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllText(
                outputPath,
                JsonUtility.ToJson(report, prettyPrint: true)
            );
            Debug.Log(
                $"Benchmark complete: {report.AverageFps:0.0} FPS average, "
                + $"{report.OnePercentLowFps:0.0} FPS 1% low. Output: {outputPath}"
            );

            if (quitAfter)
            {
                yield return new WaitForSecondsRealtime(0.5f);
                Application.Quit();
            }
        }

        private BenchmarkReport BuildReport(float duration)
        {
            int frameCount = benchmarkFrameTimes.Count;
            if (frameCount == 0)
            {
                duration = Mathf.Max(0.001f, duration);
                frameCount = 1;
            }

            List<float> sorted = new(benchmarkFrameTimes);
            sorted.Sort();
            int lowIndex = Mathf.Clamp(
                Mathf.FloorToInt(sorted.Count * 0.99f),
                0,
                Mathf.Max(0, sorted.Count - 1)
            );
            float onePercentLow = sorted.Count > 0
                ? sorted[Mathf.Min(sorted.Count - 1, lowIndex)]
                : duration;

            return new BenchmarkReport
            {
                Version = Application.version,
                CompletedAtUtc = DateTime.UtcNow.ToString("O"),
                Width = Screen.width,
                Height = Screen.height,
                SampledFrames = benchmarkFrameTimes.Count,
                DurationSeconds = duration,
                AverageFps = benchmarkFrameTimes.Count / Mathf.Max(0.001f, duration),
                OnePercentLowFps = 1f / Mathf.Max(0.001f, onePercentLow),
                MinimumFps = sorted.Count > 0
                    ? 1f / Mathf.Max(0.001f, sorted[sorted.Count - 1])
                    : 0f,
                AverageFrameMs = duration * 1000f / Mathf.Max(1, frameCount),
                WorstFrameMs = sorted.Count > 0
                    ? sorted[sorted.Count - 1] * 1000f
                    : 0f,
                ManagedMemoryBytes = GC.GetTotalMemory(false),
                GraphicsDevice = SystemInfo.graphicsDeviceName
            };
        }

        private static int ReadIntArgument(string prefix, int fallback)
        {
            string value = GetArgumentValue(prefix);
            return int.TryParse(value, out int parsed) && parsed > 0
                ? parsed
                : fallback;
        }

        private static string GetArgumentValue(string prefix)
        {
            foreach (string argument in Environment.GetCommandLineArgs())
            {
                if (argument.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return argument.Substring(prefix.Length);
                }
            }

            return string.Empty;
        }
    }
}

using System.Collections;
using System.IO;
using Demo02.Narrative;
using Demo02.WorldState;
using UnityEngine;

namespace Demo02.UI
{
    [DefaultExecutionOrder(30)]
    public sealed class AreaBanner : MonoBehaviour
    {
        private const float FadeDuration = 0.24f;
        private const float HoldDuration = 2.2f;

        private WorldRuntime worldRuntime;
        private string lastAreaName;
        private string shownAreaName;
        private float shownAt;
        private bool hasShownInitialArea;
        private bool captureRequested;

        private void Start()
        {
            worldRuntime = WorldRuntime.Instance != null
                ? WorldRuntime.Instance
                : FindAnyObjectByType<WorldRuntime>();
        }

        private void Update()
        {
            if (worldRuntime == null || string.IsNullOrWhiteSpace(worldRuntime.CurrentAreaName))
            {
                return;
            }

            string currentArea = worldRuntime.CurrentAreaName;
            if (!hasShownInitialArea)
            {
                if (DialogueController.Instance != null && DialogueController.Instance.IsOpen)
                {
                    return;
                }

                hasShownInitialArea = true;
                lastAreaName = currentArea;
                Show(currentArea);
                return;
            }

            if (currentArea != lastAreaName)
            {
                lastAreaName = currentArea;
                Show(currentArea);
            }
        }

        private void Show(string areaName)
        {
            shownAreaName = areaName;
            shownAt = Time.unscaledTime;

            if (!captureRequested)
            {
                captureRequested = true;
                string capturePath = GetArgumentValue("--capture-world=");
                if (!string.IsNullOrWhiteSpace(capturePath))
                {
                    StartCoroutine(CaptureAreaCard(capturePath));
                }
            }
        }

        private static string GetArgumentValue(string prefix)
        {
            foreach (string argument in System.Environment.GetCommandLineArgs())
            {
                if (argument.StartsWith(prefix, System.StringComparison.Ordinal))
                {
                    return argument.Substring(prefix.Length);
                }
            }

            return string.Empty;
        }

        private IEnumerator CaptureAreaCard(string path)
        {
            yield return new WaitForSecondsRealtime(0.4f);
            yield return new WaitForEndOfFrame();
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            ScreenCapture.CaptureScreenshot(path);
            Debug.Log($"World preview captured to {path}");
        }

        private void OnGUI()
        {
            if (string.IsNullOrWhiteSpace(shownAreaName)
                || (WorldMapController.Instance != null && WorldMapController.Instance.IsOpen)
                || (InvestigationJournalController.Instance != null
                    && InvestigationJournalController.Instance.IsOpen)
                || (InvestigationVisionController.Instance != null
                    && InvestigationVisionController.Instance.IsActive)
                || (DialogueController.Instance != null
                    && DialogueController.Instance.IsOpen)
                || (ScreenFade.Instance != null && ScreenFade.Instance.IsTransitioning))
            {
                return;
            }

            float elapsed = Time.unscaledTime - shownAt;
            float totalDuration = FadeDuration * 2f + HoldDuration;
            if (elapsed >= totalDuration)
            {
                return;
            }

            float alpha = Mathf.Clamp01(elapsed / FadeDuration);
            if (elapsed > FadeDuration + HoldDuration)
            {
                alpha = Mathf.Clamp01((totalDuration - elapsed) / FadeDuration);
            }

            float scale = UiTheme.Scale;
            float width = Mathf.Min(560f * scale, Screen.width - 80f);
            Rect panel = new(
                (Screen.width - width) * 0.5f,
                Screen.height * 0.255f,
                width,
                104f * scale
            );

            Color previousColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            UiTheme.DrawRect(panel, UiTheme.Ink);
            UiTheme.DrawRect(
                new Rect(panel.x, panel.y, 4f * scale, panel.height),
                UiTheme.Accent
            );
            UiTheme.DrawRect(
                new Rect(panel.x + 24f * scale, panel.y + 20f * scale, 96f * scale, 2f),
                UiTheme.Accent
            );

            GUIStyle eyebrow = UiTheme.Label(12, TextAnchor.MiddleLeft, bold: true);
            eyebrow.normal.textColor = UiTheme.Accent;
            GUI.Label(
                new Rect(
                    panel.x + 24f * scale,
                    panel.y + 28f * scale,
                    panel.width - 48f * scale,
                    24f * scale
                ),
                "GUANGLING · 当前区域",
                eyebrow
            );
            GUI.Label(
                new Rect(
                    panel.x + 24f * scale,
                    panel.y + 52f * scale,
                    panel.width - 48f * scale,
                    42f * scale
                ),
                shownAreaName,
                UiTheme.Label(28, TextAnchor.MiddleLeft, bold: true)
            );
            GUI.color = previousColor;
        }
    }
}

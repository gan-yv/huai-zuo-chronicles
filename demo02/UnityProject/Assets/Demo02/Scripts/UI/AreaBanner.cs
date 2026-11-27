using System.Collections;
using System.IO;
using Demo02.WorldState;
using UnityEngine;
using UnityEngine.UI;

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
        private GameObject canvasObject;
        private Canvas canvas;
        private CanvasGroup group;
        private Text areaName;
        private string cachedAreaName = string.Empty;

        private void Awake()
        {
            EnsureUi();
        }

        private void Start()
        {
            worldRuntime = WorldRuntime.Instance != null
                ? WorldRuntime.Instance
                : FindAnyObjectByType<WorldRuntime>();
        }

        private void Update()
        {
            EnsureUi();
            if (worldRuntime == null
                || string.IsNullOrWhiteSpace(
                    worldRuntime.CurrentAreaName
                ))
            {
                canvas.enabled = false;
                return;
            }

            string currentArea = worldRuntime.CurrentAreaName;
            if (!hasShownInitialArea)
            {
                if (GameplayUiState.IsGameplayBlocked)
                {
                    canvas.enabled = false;
                    return;
                }
                hasShownInitialArea = true;
                lastAreaName = currentArea;
            }
            else if (currentArea != lastAreaName)
            {
                lastAreaName = currentArea;
                Show(currentArea);
            }

            RefreshVisual();
        }

        private void Show(string name)
        {
            shownAreaName = name;
            shownAt = Time.unscaledTime;
            if (!captureRequested)
            {
                captureRequested = true;
                string capturePath =
                    GetArgumentValue("--capture-world=");
                if (!string.IsNullOrWhiteSpace(capturePath))
                {
                    StartCoroutine(CaptureAreaCard(capturePath));
                }
            }
        }

        private void RefreshVisual()
        {
            bool visible = !string.IsNullOrWhiteSpace(shownAreaName)
                && !GameplayUiState.IsGameplayBlocked
                && (InvestigationVisionController.Instance == null
                    || !InvestigationVisionController.Instance.IsActive)
                && (ScreenFade.Instance == null
                    || !ScreenFade.Instance.IsTransitioning);
            if (!visible)
            {
                canvas.enabled = false;
                return;
            }

            float elapsed = Time.unscaledTime - shownAt;
            float totalDuration = FadeDuration * 2f + HoldDuration;
            if (elapsed >= totalDuration)
            {
                canvas.enabled = false;
                return;
            }

            canvas.enabled = true;
            float alpha = Mathf.Clamp01(elapsed / FadeDuration);
            if (elapsed > FadeDuration + HoldDuration)
            {
                alpha = Mathf.Clamp01(
                    (totalDuration - elapsed) / FadeDuration
                );
            }
            group.alpha = alpha;
            if (cachedAreaName != shownAreaName)
            {
                cachedAreaName = shownAreaName;
                areaName.text = shownAreaName;
            }
        }

        private void OnDisable()
        {
            if (canvas != null)
            {
                canvas.enabled = false;
            }
        }

        private void OnEnable()
        {
            if (canvas != null)
            {
                canvas.enabled = true;
            }
        }

        private void OnDestroy()
        {
            if (canvasObject == null)
            {
                return;
            }
            if (Application.isPlaying)
            {
                Destroy(canvasObject);
            }
            else
            {
                DestroyImmediate(canvasObject);
            }
            canvasObject = null;
        }

        private void EnsureUi()
        {
            if (canvasObject != null)
            {
                return;
            }
            RectTransform root = HudUiFactory.CreateCanvas(
                transform,
                "AreaBannerCanvas",
                26
            );
            canvasObject = root.gameObject;
            canvas = root.GetComponent<Canvas>();
            group = canvasObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            group.alpha = 0f;

            RectTransform panel = HudUiFactory.CreatePanel(
                root,
                "AreaPanel",
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -275f),
                new Vector2(560f, 104f),
                UiTheme.Ink
            );
            Image accent = HudUiFactory.CreateImage(
                panel,
                "Accent",
                UiTheme.Accent
            );
            RectTransform accentRect = accent.rectTransform;
            accentRect.anchorMin = new Vector2(0f, 0f);
            accentRect.anchorMax = new Vector2(0f, 1f);
            accentRect.pivot = new Vector2(0f, 0.5f);
            accentRect.anchoredPosition = Vector2.zero;
            accentRect.sizeDelta = new Vector2(4f, 0f);

            Image line = HudUiFactory.CreateImage(
                panel,
                "Line",
                UiTheme.Accent
            );
            RectTransform lineRect = line.rectTransform;
            lineRect.anchorMin = new Vector2(0f, 1f);
            lineRect.anchorMax = new Vector2(0f, 1f);
            lineRect.pivot = new Vector2(0f, 1f);
            lineRect.anchoredPosition = new Vector2(24f, -20f);
            lineRect.sizeDelta = new Vector2(96f, 2f);

            Text eyebrow = HudUiFactory.CreateText(
                panel,
                "Eyebrow",
                "GUANGLING · 当前区域",
                12,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f),
                new Vector2(24f, -28f),
                new Vector2(-48f, 24f),
                TextAnchor.MiddleLeft,
                bold: true
            );
            eyebrow.color = UiTheme.Accent;
            eyebrow.rectTransform.offsetMin =
                new Vector2(24f, -52f);
            eyebrow.rectTransform.offsetMax =
                new Vector2(-24f, -28f);

            areaName = HudUiFactory.CreateText(
                panel,
                "AreaName",
                string.Empty,
                28,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f),
                new Vector2(24f, -52f),
                new Vector2(-48f, 42f),
                TextAnchor.MiddleLeft,
                bold: true
            );
            areaName.rectTransform.offsetMin =
                new Vector2(24f, -94f);
            areaName.rectTransform.offsetMax =
                new Vector2(-24f, -52f);
        }

        private static string GetArgumentValue(string prefix)
        {
            foreach (string argument in
                     Demo02.Core.RuntimeLaunchOptions.Arguments)
            {
                if (argument.StartsWith(
                        prefix,
                        System.StringComparison.Ordinal
                    ))
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
    }
}

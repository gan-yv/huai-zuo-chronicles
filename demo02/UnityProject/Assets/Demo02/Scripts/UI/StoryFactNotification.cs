using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Demo02.Core;
using Demo02.Narrative;
using UnityEngine;
using UnityEngine.UI;

namespace Demo02.UI
{
    public sealed class StoryFactNotification : MonoBehaviour
    {
        private const float HoldDuration = 3.6f;
        private const float FadeDuration = 0.25f;

        private readonly Queue<StoryFactDefinition> pendingFacts = new();
        private InvestigationState investigationState;
        private CaseDefinition caseContent;
        private StoryFactDefinition currentFact;
        private float shownAt;
        private GameObject canvasObject;
        private Canvas canvas;
        private CanvasGroup group;
        private Text title;
        private Text detail;
        private string cachedTitle = string.Empty;
        private string cachedDetail = string.Empty;

        private void Awake()
        {
            EnsureUi();
        }

        private void Start()
        {
            investigationState =
                FindAnyObjectByType<InvestigationState>();
            if (investigationState == null)
            {
                return;
            }

            caseContent = investigationState.CaseContent;
            investigationState.StoryFactAdded += OnStoryFactAdded;

            if (Array.IndexOf(
                Demo02.Core.RuntimeLaunchOptions.Arguments,
                "--preview-fact"
            ) >= 0)
            {
                investigationState.SetFact(
                    "teahouse-cart-testimony"
                );
                string path = GetArgumentValue("--capture-fact=");
                if (!string.IsNullOrWhiteSpace(path))
                {
                    StartCoroutine(CapturePreview(path));
                }
            }
        }

        private void OnDisable()
        {
            if (investigationState != null)
            {
                investigationState.StoryFactAdded -=
                    OnStoryFactAdded;
            }
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
            if (investigationState != null)
            {
                investigationState.StoryFactAdded -=
                    OnStoryFactAdded;
            }
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

        private void OnStoryFactAdded(string factId)
        {
            StoryFactDefinition fact =
                caseContent?.FindStoryFact(factId);
            if (fact != null)
            {
                pendingFacts.Enqueue(fact);
            }
        }

        private void Update()
        {
            EnsureUi();
            if (currentFact == null && pendingFacts.Count > 0)
            {
                currentFact = pendingFacts.Dequeue();
                shownAt = Time.unscaledTime;
                GameAudio.PlayConfirm();
            }

            bool visible = currentFact != null
                && !GameplayUiState.IsGameplayBlocked;
            canvas.enabled = visible;
            if (!visible)
            {
                return;
            }

            float elapsed = Time.unscaledTime - shownAt;
            float totalDuration = HoldDuration + FadeDuration * 2f;
            if (elapsed >= totalDuration)
            {
                currentFact = null;
                group.alpha = 0f;
                return;
            }

            float alpha = Mathf.Clamp01(elapsed / FadeDuration);
            if (elapsed > HoldDuration + FadeDuration)
            {
                alpha = Mathf.Clamp01(
                    (totalDuration - elapsed) / FadeDuration
                );
            }
            group.alpha = alpha;
            if (cachedTitle != currentFact.Title)
            {
                cachedTitle = currentFact.Title;
                title.text = currentFact.Title;
            }
            if (cachedDetail != currentFact.Detail)
            {
                cachedDetail = currentFact.Detail;
                detail.text = currentFact.Detail;
            }
        }

        private void EnsureUi()
        {
            if (canvasObject != null)
            {
                return;
            }
            RectTransform root = HudUiFactory.CreateCanvas(
                transform,
                "StoryFactCanvas",
                24
            );
            canvasObject = root.gameObject;
            canvas = root.GetComponent<Canvas>();
            group = canvasObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            group.alpha = 0f;

            RectTransform panel = HudUiFactory.CreatePanel(
                root,
                "StoryFactPanel",
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-18f, -98f),
                new Vector2(430f, 96f),
                new Color(0.065f, 0.058f, 0.04f, 0.97f)
            );
            Outline outline = panel.gameObject.AddComponent<Outline>();
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;
            outline.effectColor =
                new Color(0.88f, 0.7f, 0.38f, 0.66f);
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

            Text eyebrow = HudUiFactory.CreateText(
                panel,
                "Eyebrow",
                "SIDE EVIDENCE · 支线旁证已记录",
                10,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f),
                new Vector2(18f, -7f),
                new Vector2(-36f, 19f),
                TextAnchor.MiddleLeft,
                bold: true
            );
            eyebrow.color = UiTheme.Accent;
            eyebrow.rectTransform.offsetMin =
                new Vector2(18f, -26f);
            eyebrow.rectTransform.offsetMax = new Vector2(-18f, -7f);

            title = HudUiFactory.CreateText(
                panel,
                "Title",
                string.Empty,
                16,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f),
                new Vector2(18f, -27f),
                new Vector2(-36f, 25f),
                TextAnchor.MiddleLeft,
                bold: true
            );
            title.rectTransform.offsetMin =
                new Vector2(18f, -52f);
            title.rectTransform.offsetMax =
                new Vector2(-18f, -27f);

            detail = HudUiFactory.CreateText(
                panel,
                "Detail",
                string.Empty,
                10,
                new Vector2(0f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 0.5f),
                new Vector2(18f, 0f),
                new Vector2(-36f, 0f),
                TextAnchor.UpperLeft
            );
            detail.rectTransform.offsetMin =
                new Vector2(18f, 8f);
            detail.rectTransform.offsetMax =
                new Vector2(-18f, -54f);
            detail.horizontalOverflow = HorizontalWrapMode.Wrap;
            detail.verticalOverflow = VerticalWrapMode.Truncate;
        }

        private static string GetArgumentValue(string prefix)
        {
            foreach (string argument in
                     Demo02.Core.RuntimeLaunchOptions.Arguments)
            {
                if (argument.StartsWith(
                        prefix,
                        StringComparison.Ordinal
                    ))
                {
                    return argument.Substring(prefix.Length);
                }
            }
            return string.Empty;
        }

        private IEnumerator CapturePreview(string path)
        {
            yield return new WaitForSecondsRealtime(0.7f);
            yield return new WaitForEndOfFrame();
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log($"Story fact preview captured to {path}");
        }
    }
}

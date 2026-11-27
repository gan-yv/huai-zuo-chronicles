using Demo02.Narrative;
using Demo02.WorldState;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Demo02.UI
{
    [RequireComponent(typeof(InvestigationState))]
    public sealed class ObjectiveDisplay : MonoBehaviour
    {
        private InvestigationState investigationState;
        private GameObject canvasObject;
        private Canvas canvas;
        private RectTransform panel;
        private Text chapter;
        private Text objective;
        private Text optional;
        private Text street;
        private string cachedChapter = string.Empty;
        private string cachedObjective = string.Empty;
        private string cachedOptional = string.Empty;
        private string cachedStreet = string.Empty;
        private bool hasOptional;
        private bool hasTrackedContent;
        private float nextTextRefresh;

        private void Awake()
        {
            investigationState = GetComponent<InvestigationState>();
            EnsureUi();
        }

        private void Start()
        {
            if (investigationState != null
                && Array.IndexOf(
                    Demo02.Core.RuntimeLaunchOptions.Arguments,
                    "--preview-optional"
                ) >= 0)
            {
                investigationState.CompleteWitnessBriefing();
                investigationState.CollectClue("old-street-sign");
                investigationState.CollectClue("salt-ledger");
                investigationState.CollectClue("official-seal");
            }
        }

        private void Update()
        {
            EnsureUi();
            bool visible = investigationState != null
                && !GameplayUiState.IsGameplayBlocked;
            canvas.enabled = visible;
            if (!visible)
            {
                return;
            }

            if (Time.unscaledTime >= nextTextRefresh)
            {
                nextTextRefresh = Time.unscaledTime + 0.1f;
                RefreshText();
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
                "ObjectiveDisplayCanvas",
                17
            );
            canvasObject = root.gameObject;
            canvas = root.GetComponent<Canvas>();

            panel = HudUiFactory.CreatePanel(
                root,
                "ObjectivePanel",
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(20f, -20f),
                new Vector2(448f, 66f),
                UiTheme.Ink
            );
            Outline outline =
                panel.gameObject.AddComponent<Outline>();
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;
            outline.effectColor = new Color(1f, 1f, 1f, 0.1f);

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

            chapter = HudUiFactory.CreateText(
                panel,
                "Chapter",
                string.Empty,
                11,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f),
                new Vector2(18f, -8f),
                new Vector2(0f, 20f),
                TextAnchor.MiddleLeft,
                bold: true
            );
            chapter.color = UiTheme.Accent;
            chapter.rectTransform.offsetMin =
                new Vector2(18f, chapter.rectTransform.offsetMin.y);
            chapter.rectTransform.offsetMax = new Vector2(-14f, -8f);

            objective = HudUiFactory.CreateText(
                panel,
                "Objective",
                string.Empty,
                16,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f),
                new Vector2(18f, -27f),
                new Vector2(0f, 30f),
                TextAnchor.MiddleLeft
            );
            objective.rectTransform.offsetMin =
                new Vector2(18f, objective.rectTransform.offsetMin.y);
            objective.rectTransform.offsetMax = new Vector2(-14f, -27f);

            optional = HudUiFactory.CreateText(
                panel,
                "Optional",
                string.Empty,
                11,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f),
                new Vector2(18f, -57f),
                new Vector2(0f, 26f),
                TextAnchor.MiddleLeft,
                bold: true
            );
            optional.color = UiTheme.Accent;
            optional.rectTransform.offsetMin =
                new Vector2(18f, optional.rectTransform.offsetMin.y);
            optional.rectTransform.offsetMax = new Vector2(-14f, -57f);

            street = HudUiFactory.CreateText(
                panel,
                "Street",
                string.Empty,
                11,
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 0f),
                new Vector2(18f, 4f),
                new Vector2(0f, 20f),
                TextAnchor.MiddleLeft,
                bold: true
            );
            street.color = new Color(0.42f, 0.82f, 0.58f);
            street.rectTransform.offsetMin =
                new Vector2(18f, 4f);
            street.rectTransform.offsetMax = new Vector2(-14f, 24f);
            optional.gameObject.SetActive(false);
            street.gameObject.SetActive(false);
        }

        private void RefreshText()
        {
            string chapterText =
                $"{investigationState.CaseContent?.ChapterLabel ?? "CHAPTER"} · "
                + $"{investigationState.CaseContent?.Title ?? "内容未加载"}";
            SetText(chapter, chapterText, ref cachedChapter);

            string objectiveText =
                ChapterCopy.GetObjective(investigationState);
            if (investigationState.IsConclusionReached)
            {
                objectiveText += "。按 R 重新开始";
            }
            SetText(objective, objectiveText, ref cachedObjective);

            OptionalObjectiveDefinition activeOptional =
                OptionalObjectiveRuntime.FindActive(
                    investigationState.CaseContent,
                    investigationState
                );
            hasOptional = activeOptional != null;
            optional.gameObject.SetActive(hasOptional);
            if (hasOptional)
            {
                SetText(
                    optional,
                    $"可选目标 · {activeOptional.Title}    "
                        + activeOptional.TrackerHint,
                    ref cachedOptional
                );
            }

            WorldContentEntry trackedContent =
                WorldContentTracker.Instance?.GetTrackedEntry();
            hasTrackedContent = trackedContent != null;
            street.gameObject.SetActive(hasTrackedContent);
            if (hasTrackedContent)
            {
                SetText(
                    street,
                    $"街区事件 · {trackedContent.Title}",
                    ref cachedStreet
                );
            }

            panel.sizeDelta = new Vector2(
                448f,
                (hasOptional ? 92f : 66f)
                    + (hasTrackedContent ? 25f : 0f)
            );
        }

        private static void SetText(
            Text target,
            string value,
            ref string cached)
        {
            if (cached == value)
            {
                return;
            }
            cached = value;
            target.text = value;
        }
    }
}

using Demo02.Core;
using Demo02.Interaction;
using Demo02.Narrative;
using Demo02.Player;
using Demo02.WorldState;
using UnityEngine;
using UnityEngine.UI;

namespace Demo02.UI
{
    [DefaultExecutionOrder(20)]
    public sealed class QuestNavigationHud : MonoBehaviour
    {
        private InvestigationState investigationState;
        private PlayerPartyController party;
        private TimeLayerController timeLayerController;
        private WorldRuntime worldRuntime;
        private Transform currentTarget;
        private string navigationHint = string.Empty;
        private Transform optionalTarget;
        private string optionalHint = string.Empty;
        private GameObject canvasObject;
        private Canvas canvas;
        private GameObject primaryPanel;
        private GameObject optionalPanel;
        private Text navigationText;
        private Text optionalText;
        private float nextTextRefresh;
        private string cachedNavigation = string.Empty;
        private string cachedOptional = string.Empty;

        private void Awake()
        {
            EnsureUi();
        }

        private void Start()
        {
            investigationState =
                Demo02.Core.GameServices.Resolve<InvestigationState>();
            party = Demo02.Core.GameServices.Resolve<PlayerPartyController>();
            timeLayerController =
                Demo02.Core.GameServices.Resolve<TimeLayerController>();
            worldRuntime =
                Demo02.Core.GameServices.Resolve<WorldRuntime>();

            if (investigationState != null)
            {
                investigationState.StageChanged += OnStageChanged;
                investigationState.ClueCountChanged += OnProgressChanged;
                investigationState.AnchorCountChanged += OnProgressChanged;
                investigationState.StoryFactAdded += OnStoryFactAdded;
            }
            if (timeLayerController != null)
            {
                timeLayerController.LayerChanged += OnLayerChanged;
            }

            RefreshNavigationTarget();
        }

        private void OnDestroy()
        {
            if (investigationState != null)
            {
                investigationState.StageChanged -= OnStageChanged;
                investigationState.ClueCountChanged -= OnProgressChanged;
                investigationState.AnchorCountChanged -= OnProgressChanged;
                investigationState.StoryFactAdded -= OnStoryFactAdded;
            }
            if (timeLayerController != null)
            {
                timeLayerController.LayerChanged -= OnLayerChanged;
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

        private void Update()
        {
            EnsureUi();
            bool visible = !GameplayUiState.IsGameplayBlocked
                && party != null
                && party.ActiveCharacter != null
                && (currentTarget != null || optionalTarget != null);
            canvas.enabled = visible;
            if (!visible)
            {
                return;
            }

            primaryPanel.SetActive(currentTarget != null);
            optionalPanel.SetActive(
                optionalTarget != null
                    && optionalTarget != currentTarget
            );
            if (Time.unscaledTime < nextTextRefresh)
            {
                return;
            }
            nextTextRefresh = Time.unscaledTime + 0.1f;
            SetText(
                navigationText,
                BuildNavigationText(
                    currentTarget,
                    navigationHint,
                    false
                ),
                ref cachedNavigation
            );
            SetText(
                optionalText,
                BuildNavigationText(
                    optionalTarget,
                    optionalHint,
                    true
                ),
                ref cachedOptional
            );
        }

        private void EnsureUi()
        {
            if (canvasObject != null)
            {
                return;
            }

            RectTransform root = HudUiFactory.CreateCanvas(
                transform,
                "QuestNavigationCanvas",
                18
            );
            canvasObject = root.gameObject;
            canvas = root.GetComponent<Canvas>();

            RectTransform primary = CreatePanel(
                root,
                "PrimaryNavigation",
                -84f,
                46f,
                UiTheme.Accent,
                new Color(0.92f, 0.82f, 0.62f),
                out navigationText
            );
            primaryPanel = primary.gameObject;

            RectTransform optional = CreatePanel(
                root,
                "OptionalNavigation",
                -136f,
                42f,
                new Color(0.72f, 0.58f, 0.3f),
                new Color(0.82f, 0.7f, 0.45f),
                out optionalText
            );
            optionalPanel = optional.gameObject;
            primaryPanel.SetActive(false);
            optionalPanel.SetActive(false);
        }

        private static RectTransform CreatePanel(
            Transform root,
            string name,
            float top,
            float height,
            Color accent,
            Color textColor,
            out Text text)
        {
            RectTransform panel = HudUiFactory.CreatePanel(
                root,
                name,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, top),
                new Vector2(430f, height),
                UiTheme.Panel
            );
            Outline outline =
                panel.gameObject.AddComponent<Outline>();
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;
            outline.effectColor = new Color(1f, 1f, 1f, 0.12f);

            Image topLine = HudUiFactory.CreateImage(
                panel,
                "TopLine",
                accent
            );
            RectTransform lineRect = topLine.rectTransform;
            lineRect.anchorMin = new Vector2(0f, 1f);
            lineRect.anchorMax = new Vector2(1f, 1f);
            lineRect.pivot = new Vector2(0.5f, 1f);
            lineRect.anchoredPosition = Vector2.zero;
            lineRect.sizeDelta = new Vector2(0f, 2f);

            text = HudUiFactory.CreateText(
                panel,
                "Text",
                string.Empty,
                15,
                new Vector2(0f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(-24f, 0f),
                TextAnchor.MiddleCenter,
                bold: true
            );
            text.color = textColor;
            return panel;
        }

        private void RefreshNavigationTarget()
        {
            currentTarget = null;
            navigationHint = string.Empty;
            optionalTarget = null;
            optionalHint = string.Empty;

            if (investigationState == null
                || party == null
                || party.ActiveCharacter == null)
            {
                return;
            }

            if (investigationState.SelectedConclusion
                != ConclusionChoice.None)
            {
                return;
            }

            OptionalObjectiveDefinition optional =
                OptionalObjectiveRuntime.FindActive(
                    investigationState.CaseContent,
                    investigationState
                );
            if (optional != null
                && !string.IsNullOrWhiteSpace(
                    optional.TargetObjectName
                ))
            {
                optionalTarget = FindWorldObject(
                    optional.TargetObjectName
                );
                optionalHint = optional.TrackerHint;
            }

            switch (investigationState.Stage)
            {
                case ChapterStage.Arrival:
                    currentTarget = FindShenBo();
                    navigationHint = "询问沈砚失踪的事";
                    return;
                case ChapterStage.Investigate:
                    currentTarget = FindUncollectedClue();
                    return;
                case ChapterStage.ReviewEvidence:
                    currentTarget = FindShenBo();
                    navigationHint = "让沈伯辨认三处记录";
                    return;
                case ChapterStage.CrossExamineTestimony:
                    currentTarget = FindShenBo();
                    navigationHint = "按 T 对照证词与账牌日期";
                    return;
                case ChapterStage.StabilizeAnchors:
                    TimeAnchor anchor = FindUnstabilizedAnchor();
                    currentTarget = anchor != null
                        ? anchor.transform
                        : null;
                    if (anchor != null
                        && timeLayerController != null)
                    {
                        TimeLayerVisibility visibility =
                            anchor.GetComponent<TimeLayerVisibility>();
                        if (visibility != null
                            && visibility.VisibleLayer
                                != timeLayerController.CurrentLayer)
                        {
                            navigationHint = "按 Q 切换时间层";
                        }
                    }
                    return;
                case ChapterStage.RepairRift:
                    currentTarget = FindWorldObject(
                        "RepairSite_TimeRift"
                    );
                    navigationHint = "压下被撕开的记录";
                    return;
                case ChapterStage.FinalDecision:
                    currentTarget = FindWorldObject(
                        "Choice_Preserve"
                    );
                    navigationHint = "决定盐引巷的命运";
                    return;
            }
        }

        private Transform FindShenBo()
        {
            return FindWorldObject("NPC_ShenBo");
        }

        private Transform FindWorldObject(string objectId)
        {
            return worldRuntime != null
                && worldRuntime.TryGetWorldObject(
                    objectId,
                    out WorldObjectId worldObject
                )
                ? worldObject.transform
                : null;
        }

        private Transform FindUncollectedClue()
        {
            ClueInteractable[] clues =
                FindObjectsByType<ClueInteractable>(
                    FindObjectsInactive.Exclude
                );
            foreach (ClueInteractable clue in clues)
            {
                if (!investigationState.HasClue(clue.ClueId))
                {
                    return clue.transform;
                }
            }
            return null;
        }

        private TimeAnchor FindUnstabilizedAnchor()
        {
            TimeAnchor[] anchors =
                FindObjectsByType<TimeAnchor>(
                    FindObjectsInactive.Exclude
                );
            string expectedId = TimeAnchorPuzzleRules.GetExpectedId(
                investigationState.StabilizedTimeAnchorCount
            );
            foreach (TimeAnchor anchor in anchors)
            {
                if (anchor.AnchorId == expectedId
                    && !investigationState.HasStabilizedAnchor(
                        anchor.AnchorId
                    ))
                {
                    return anchor;
                }
            }
            foreach (TimeAnchor anchor in anchors)
            {
                if (!investigationState.HasStabilizedAnchor(
                    anchor.AnchorId
                ))
                {
                    return anchor;
                }
            }
            return null;
        }

        private string BuildNavigationText(
            Transform target,
            string hint,
            bool optional)
        {
            if (target == null || party?.ActiveCharacter == null)
            {
                return string.Empty;
            }
            Camera currentCamera = Camera.main;
            Vector3 offset =
                target.position - party.ActiveCharacter.position;
            offset.y = 0f;
            float distance = offset.magnitude;
            string direction = string.Empty;
            if (currentCamera != null && distance > 0.1f)
            {
                Vector3 flatForward =
                    currentCamera.transform.forward;
                flatForward.y = 0f;
                float angle = Vector3.SignedAngle(
                    flatForward,
                    offset.normalized,
                    Vector3.up
                );
                direction = GetDirectionLabel(angle);
            }

            string text = optional
                ? $"可选 · {direction}  {distance:0.#}m"
                : $"{direction}  {distance:0.#}m";
            if (!string.IsNullOrWhiteSpace(hint))
            {
                text += $"    {hint}";
            }
            return text;
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

        private void OnStageChanged(ChapterStage stage)
        {
            RefreshNavigationTarget();
        }

        private void OnProgressChanged(int current, int required)
        {
            RefreshNavigationTarget();
        }

        private void OnStoryFactAdded(string factId)
        {
            RefreshNavigationTarget();
        }

        private void OnLayerChanged(TimeLayer layer)
        {
            RefreshNavigationTarget();
        }

        private static string GetDirectionLabel(float angle)
        {
            float absolute = Mathf.Abs(angle);
            if (absolute <= 22.5f)
            {
                return "前方";
            }
            if (absolute >= 157.5f)
            {
                return "后方";
            }
            if (angle > 0f)
            {
                return angle < 67.5f
                    ? "右前"
                    : angle < 112.5f
                        ? "右侧"
                        : "右后";
            }
            return angle > -67.5f
                ? "左前"
                : angle > -112.5f
                    ? "左侧"
                    : "左后";
        }
    }
}

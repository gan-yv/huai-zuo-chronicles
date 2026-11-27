using System.Collections.Generic;
using Demo02.Combat;
using Demo02.Core;
using Demo02.Narrative;
using UnityEngine;
using UnityEngine.UI;

namespace Demo02.UI
{
    public sealed class ContextHintController : MonoBehaviour
    {
        private const float HoldDuration = 4.6f;
        private const float FadeDuration = 0.2f;

        private readonly Queue<string> pendingHints = new();
        private readonly HashSet<string> shownHintIds = new();

        private InvestigationState investigationState;
        private InventoryState inventoryState;
        private GameObject canvasObject;
        private Canvas canvas;
        private CanvasGroup group;
        private Text hintText;
        private string currentHint;
        private string displayedHint;
        private float shownAt;
        private float nextContextCheck;

        private void Awake()
        {
            EnsureUi();
        }

        private void Start()
        {
            investigationState =
                FindAnyObjectByType<InvestigationState>();
            inventoryState = InventoryState.Instance != null
                ? InventoryState.Instance
                : FindAnyObjectByType<InventoryState>();

            if (investigationState != null)
            {
                investigationState.ClueCountChanged +=
                    OnClueCountChanged;
                investigationState.StageChanged += OnStageChanged;
            }
            if (inventoryState != null)
            {
                inventoryState.Changed += OnInventoryChanged;
            }

            EvaluateContext();
        }

        private void OnDestroy()
        {
            if (investigationState != null)
            {
                investigationState.ClueCountChanged -=
                    OnClueCountChanged;
                investigationState.StageChanged -= OnStageChanged;
            }
            if (inventoryState != null)
            {
                inventoryState.Changed -= OnInventoryChanged;
            }
            DestroyCanvas();
        }

        private void OnClueCountChanged(int count, int required)
        {
            if (count > 0)
            {
                Enqueue(
                    "journal",
                    "按 J 打开调查案卷，查看目标、证据和当前推理。"
                );
            }
        }

        private void OnStageChanged(ChapterStage stage)
        {
            EvaluateContext();
        }

        private void OnInventoryChanged()
        {
            if (inventoryState != null
                && inventoryState.DistinctItemCount > 0)
            {
                Enqueue(
                    "inventory",
                    "按 B 打开背包，查看物品说明与可使用效果。"
                );
            }
        }

        private void Update()
        {
            EnsureUi();
            if (Time.unscaledTime >= nextContextCheck)
            {
                nextContextCheck = Time.unscaledTime + 0.7f;
                EvaluateContext();
                EvaluateCombatContext();
            }

            if (GameplayUiState.IsGameplayBlocked)
            {
                group.alpha = 0f;
                return;
            }

            if (currentHint == null && pendingHints.Count > 0)
            {
                currentHint = Demo02Input.ResolvePrompt(
                    pendingHints.Dequeue()
                );
                shownAt = Time.unscaledTime;
            }
            if (currentHint == null)
            {
                group.alpha = 0f;
                return;
            }

            float elapsed = Time.unscaledTime - shownAt;
            float totalDuration = HoldDuration + FadeDuration * 2f;
            if (elapsed >= totalDuration)
            {
                currentHint = null;
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
            if (currentHint != displayedHint)
            {
                displayedHint = currentHint;
                hintText.text = currentHint;
            }
        }

        private void EvaluateCombatContext()
        {
            if (CombatRegistry.Enemies.Count == 0)
            {
                return;
            }

            Enqueue(
                "melee-combo",
                "左键三段连击；橙黄预警是普攻，暗红重击需要更早闪避。成功闪避触发“见隙”，H 快捷恢复。"
            );
        }

        private void EvaluateContext()
        {
            if (investigationState == null)
            {
                return;
            }

            switch (investigationState.Stage)
            {
                case ChapterStage.Arrival:
                    Enqueue(
                        "movement",
                        "WASD 移动 · Shift 冲刺耗体力 · 空中再按 Space 展开滑翔"
                    );
                    Enqueue(
                        "camera",
                        "按住鼠标右键转动镜头，松开右键后恢复鼠标指针。"
                    );
                    Enqueue(
                        "talk-shenbo",
                        "靠近沈伯后按 E 交谈，确认沈砚最后留下的记录。"
                    );
                    break;
                case ChapterStage.Investigate:
                    Enqueue(
                        "investigate",
                        "留意街边可调查记录，靠近后按 E 调查。"
                    );
                    if (investigationState.CollectedClueCount > 0)
                    {
                        Enqueue(
                            "journal",
                            "按 J 打开调查案卷，查看已知证据与任务方向。"
                        );
                    }
                    break;
                case ChapterStage.ReviewEvidence:
                    Enqueue(
                        "evidence-review",
                        "回到旧仓内院，按 E 把三处记录交给沈伯。"
                    );
                    break;
                case ChapterStage.CrossExamineTestimony:
                    Enqueue(
                        "testimony",
                        "按 T 打开证词对照，指出两条记录无法同时成立的地方。"
                    );
                    break;
                case ChapterStage.StabilizeAnchors:
                    Enqueue(
                        "time-layer",
                        "按 Q 切换现世与旧时，寻找下一处时间锚点。"
                    );
                    break;
                case ChapterStage.FinalDecision:
                    Enqueue(
                        "final-choice",
                        "街尾的两个选择会决定盐引巷与沈砚的结局。"
                    );
                    break;
            }
        }

        private void Enqueue(string id, string text)
        {
            if (!shownHintIds.Add(id))
            {
                return;
            }

            pendingHints.Enqueue(text);
        }

        private void EnsureUi()
        {
            if (canvasObject != null)
            {
                return;
            }

            RectTransform root = HudUiFactory.CreateCanvas(
                transform,
                "ContextHintCanvas",
                22
            );
            canvasObject = root.gameObject;
            canvas = root.GetComponent<Canvas>();
            group = canvasObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            group.alpha = 0f;

            RectTransform panel = HudUiFactory.CreatePanel(
                root,
                "HintPanel",
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 178f),
                new Vector2(620f, 54f),
                new Color(0.025f, 0.033f, 0.045f, 0.97f)
            );
            Outline outline = panel.gameObject.AddComponent<Outline>();
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;
            outline.effectColor = new Color(1f, 1f, 1f, 0.12f);

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
            accentRect.sizeDelta = new Vector2(3f, 0f);

            Text eyebrow = HudUiFactory.CreateText(
                panel,
                "Eyebrow",
                "GUIDE · 下一步",
                9,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(14f, -3f),
                new Vector2(592f, 17f),
                TextAnchor.MiddleLeft,
                bold: true
            );
            eyebrow.color = UiTheme.Accent;

            hintText = HudUiFactory.CreateText(
                panel,
                "HintText",
                string.Empty,
                13,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(14f, -19f),
                new Vector2(592f, 31f),
                TextAnchor.MiddleLeft
            );
            hintText.horizontalOverflow =
                HorizontalWrapMode.Wrap;
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

        private void DestroyCanvas()
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
    }
}

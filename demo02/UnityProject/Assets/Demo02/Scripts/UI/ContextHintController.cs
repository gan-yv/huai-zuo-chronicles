using System;
using System.Collections.Generic;
using Demo02.Core;
using Demo02.Narrative;
using UnityEngine;

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
        private string currentHint;
        private float shownAt;
        private float nextContextCheck;

        private void Start()
        {
            investigationState = FindAnyObjectByType<InvestigationState>();
            inventoryState = InventoryState.Instance != null
                ? InventoryState.Instance
                : FindAnyObjectByType<InventoryState>();

            if (investigationState != null)
            {
                investigationState.ClueCountChanged += OnClueCountChanged;
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
                investigationState.ClueCountChanged -= OnClueCountChanged;
                investigationState.StageChanged -= OnStageChanged;
            }
            if (inventoryState != null)
            {
                inventoryState.Changed -= OnInventoryChanged;
            }
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
            if (Time.unscaledTime >= nextContextCheck)
            {
                nextContextCheck = Time.unscaledTime + 0.7f;
                EvaluateContext();
            }
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

        private void OnGUI()
        {
            if (IsBlockedByOtherUi())
            {
                return;
            }

            if (currentHint == null && pendingHints.Count > 0)
            {
                currentHint = pendingHints.Dequeue();
                shownAt = Time.unscaledTime;
            }
            if (currentHint == null)
            {
                return;
            }

            float elapsed = Time.unscaledTime - shownAt;
            float totalDuration = HoldDuration + FadeDuration * 2f;
            if (elapsed >= totalDuration)
            {
                currentHint = null;
                return;
            }

            float alpha = Mathf.Clamp01(elapsed / FadeDuration);
            if (elapsed > HoldDuration + FadeDuration)
            {
                alpha = Mathf.Clamp01(
                    (totalDuration - elapsed) / FadeDuration
                );
            }

            float scale = UiTheme.Scale;
            float width = Mathf.Min(620f * scale, Screen.width - 50f);
            Rect panel = new(
                (Screen.width - width) * 0.5f,
                Screen.height - 232f * scale,
                width,
                54f * scale
            );

            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            UiTheme.DrawRect(panel, new Color(0.025f, 0.033f, 0.045f, 0.97f));
            UiTheme.DrawBorder(panel, new Color(1f, 1f, 1f, 0.12f));
            UiTheme.DrawRect(
                new Rect(panel.x, panel.y, 3f * scale, panel.height),
                UiTheme.Accent
            );
            GUIStyle eyebrow = UiTheme.Label(9, TextAnchor.MiddleLeft, bold: true);
            eyebrow.normal.textColor = UiTheme.Accent;
            GUI.Label(
                new Rect(
                    panel.x + 14f * scale,
                    panel.y + 3f * scale,
                    panel.width - 28f * scale,
                    17f * scale
                ),
                "GUIDE · 下一步",
                eyebrow
            );
            GUI.Label(
                new Rect(
                    panel.x + 14f * scale,
                    panel.y + 19f * scale,
                    panel.width - 28f * scale,
                    31f * scale
                ),
                currentHint,
                UiTheme.Label(13)
            );
            GUI.color = previous;
        }

        private static bool IsBlockedByOtherUi()
        {
            return (DialogueController.Instance != null
                    && DialogueController.Instance.IsOpen)
                || (WorldMapController.Instance != null
                    && WorldMapController.Instance.IsOpen)
                || (InvestigationJournalController.Instance != null
                    && InvestigationJournalController.Instance.IsOpen)
                || (InventoryController.Instance != null
                    && InventoryController.Instance.IsOpen)
                || (SettingsController.Instance != null
                    && SettingsController.Instance.IsOpen)
                || (TestimonyBoardController.Instance != null
                    && TestimonyBoardController.Instance.IsOpen)
                || (SaveSlotController.Instance != null
                    && SaveSlotController.Instance.IsOpen)
                || (GameFlowController.Instance != null
                    && GameFlowController.Instance.IsGameEnding);
        }
    }
}

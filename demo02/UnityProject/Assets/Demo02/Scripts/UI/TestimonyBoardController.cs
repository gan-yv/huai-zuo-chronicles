using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Demo02.Core;
using Demo02.Narrative;
using UnityEngine;

namespace Demo02.UI
{
    public sealed class TestimonyBoardController : MonoBehaviour
    {
        public static TestimonyBoardController Instance { get; private set; }

        private InvestigationState investigationState;
        private bool isOpen;
        private float previousTimeScale = 1f;
        private string selectedStatementId;
        private string selectedEvidenceId;
        private bool selectingEvidenceColumn;
        private string feedback;
        private Color feedbackColor = new(0.62f, 0.65f, 0.68f, 1f);

        public bool IsOpen => isOpen;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            investigationState = FindAnyObjectByType<InvestigationState>();
            if (Array.IndexOf(
                Demo02.Core.RuntimeLaunchOptions.Arguments,
                "--preview-testimony"
            ) >= 0)
            {
                PreparePreview();
                ToggleBoard();
                string path = GetArgumentValue("--capture-testimony=");
                if (!string.IsNullOrWhiteSpace(path))
                {
                    StartCoroutine(CapturePreview(path));
                }
            }
        }

        private void Update()
        {
            if (isOpen)
            {
                if (Demo02Input.CancelPressed
                    || Demo02Input.TestimonyPressed)
                {
                    ToggleBoard();
                    return;
                }

                if (Demo02Input.NavigateLeftPressed
                    || Demo02Input.NavigateRightPressed)
                {
                    selectingEvidenceColumn = !selectingEvidenceColumn;
                    GameAudio.PlayInteraction();
                }
                if (Demo02Input.NavigateUpPressed
                    || Demo02Input.NavigateDownPressed)
                {
                    MoveSelection(
                        Demo02Input.NavigateDownPressed ? 1 : -1
                    );
                }

                if (Demo02Input.SubmitPressed)
                {
                    SubmitSelection();
                }

                return;
            }

            if (!Demo02Input.TestimonyPressed
                || investigationState == null
                || !investigationState.CanOpenTestimonyBoard
                || Time.timeScale <= 0f
                || IsBlockedByOtherUi())
            {
                return;
            }

            ToggleBoard();
        }

        public void ToggleBoard()
        {
            if (!isOpen && (Time.timeScale <= 0f
                || GameplayUiState.HasOpenModalExcept(this)
                || investigationState == null
                || !investigationState.CanOpenTestimonyBoard))
            {
                return;
            }

            isOpen = !isOpen;
            if (isOpen)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                GameplayUiState.RegisterModal(this);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                if (investigationState.TestimonyContradictionSolved)
                {
                    ContradictionDefinition rule =
                        investigationState.CaseContent?.Contradiction;
                    selectedStatementId = rule?.StatementId;
                    selectedEvidenceId = rule?.EvidenceId;
                    SetFeedback(
                        rule?.Explanation ?? "证词矛盾已经解决。",
                        UiTheme.Accent
                    );
                }
                else if (string.IsNullOrWhiteSpace(feedback))
                {
                    SetFeedback(
                        investigationState.Stage
                            == ChapterStage.CrossExamineTestimony
                            ? "选择一条证词和一条记录，指出不能同时成立的地方。"
                            : "先与沈伯完成证据复核，对照记录才会解锁。",
                        UiTheme.Muted
                    );
                }

                SyncSelectionFromIds();
                GameAudio.PlayInteraction();
            }
            else
            {
                Time.timeScale = previousTimeScale;
                GameplayUiState.UnregisterModal(this);
                GameAudio.PlayInteraction();
            }
        }

        private void OnDisable()
        {
            if (isOpen)
            {
                Time.timeScale = previousTimeScale;
                isOpen = false;
            }
            GameplayUiState.UnregisterModal(this);
        }

        private void OnDestroy()
        {
            GameplayUiState.UnregisterModal(this);
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void SubmitSelection()
        {
            if (investigationState == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(selectedStatementId)
                || string.IsNullOrWhiteSpace(selectedEvidenceId))
            {
                SetFeedback("先各选一条证词和证据，再指出矛盾。", UiTheme.Accent);
                return;
            }

            ContradictionResult result = investigationState.SubmitContradiction(
                selectedStatementId,
                selectedEvidenceId
            );
            ContradictionDefinition rule =
                investigationState.CaseContent?.Contradiction;
            switch (result)
            {
                case ContradictionResult.Correct:
                    SetFeedback(
                        rule?.Explanation ?? "证词矛盾已经解决。",
                        UiTheme.Accent
                    );
                    GameAudio.PlayConfirm();
                    break;
                case ContradictionResult.Incorrect:
                    SetFeedback(
                        investigationState.ContradictionAttempts >= 2
                            ? rule?.RepeatedHint ?? "重新比对两份记录。"
                            : rule?.FirstHint ?? "这两份记录不能同时成立。",
                        new Color(0.94f, 0.58f, 0.32f)
                    );
                    GameAudio.PlayInteraction();
                    break;
                case ContradictionResult.AlreadySolved:
                    SetFeedback(
                        rule?.Explanation ?? "证词矛盾已经解决。",
                        UiTheme.Accent
                    );
                    break;
                default:
                    SetFeedback(
                        "证词对照尚未解锁。先完成沈伯的证据复核。",
                        UiTheme.Muted
                    );
                    break;
            }
        }

        private void MoveSelection(int direction)
        {
            CaseDefinition content = investigationState?.CaseContent;
            if (content == null)
            {
                return;
            }

            if (selectingEvidenceColumn)
            {
                int current = FindIndex(
                    content.Evidence,
                    item => item.Id,
                    selectedEvidenceId
                );
                int next = WrapIndex(
                    current + direction,
                    content.Evidence.Count
                );
                if (next >= 0)
                {
                    selectedEvidenceId =
                        content.Evidence[next].Id;
                }
            }
            else
            {
                int current = FindIndex(
                    content.Testimonies,
                    item => item.Id,
                    selectedStatementId
                );
                int next = WrapIndex(
                    current + direction,
                    content.Testimonies.Count
                );
                if (next >= 0)
                {
                    selectedStatementId =
                        content.Testimonies[next].Id;
                }
            }
            GameAudio.PlayInteraction();
        }

        private void SyncSelectionFromIds()
        {
            CaseDefinition content = investigationState?.CaseContent;
            if (content == null)
            {
                return;
            }

            int statementIndex = FindIndex(
                content.Testimonies,
                item => item.Id,
                selectedStatementId
            );
            if (statementIndex < 0 && content.Testimonies.Count > 0)
            {
                selectedStatementId = content.Testimonies[0].Id;
            }
            int evidenceIndex = FindIndex(
                content.Evidence,
                item => item.Id,
                selectedEvidenceId
            );
            if (evidenceIndex < 0 && content.Evidence.Count > 0)
            {
                selectedEvidenceId = content.Evidence[0].Id;
            }
        }

        private static int FindIndex<T>(
            IReadOnlyList<T> items,
            Func<T, string> getId,
            string id)
        {
            for (int index = 0; index < items.Count; index++)
            {
                if (string.Equals(
                        getId(items[index]),
                        id,
                        StringComparison.Ordinal))
                {
                    return index;
                }
            }
            return -1;
        }

        private static int WrapIndex(int index, int count)
        {
            if (count <= 0)
            {
                return -1;
            }
            return (index % count + count) % count;
        }

        private void SetFeedback(string message, Color color)
        {
            feedback = message;
            feedbackColor = color;
        }

        private void OnGUI()
        {
            if (!isOpen || investigationState == null)
            {
                return;
            }

            GUI.depth = -115;
            UiTheme.DrawRect(
                new Rect(0f, 0f, Screen.width, Screen.height),
                new Color(0.012f, 0.017f, 0.025f, 0.96f)
            );

            float scale = UiTheme.Scale;
            float panelWidth = Mathf.Min(Screen.width - 48f, 1120f * scale);
            float panelHeight = Mathf.Min(Screen.height - 48f, 660f * scale);
            Rect panel = new(
                (Screen.width - panelWidth) * 0.5f,
                (Screen.height - panelHeight) * 0.5f,
                panelWidth,
                panelHeight
            );
            DrawPanel(panel);
            DrawHeader(panel, scale);
            CaseDefinition content = investigationState.CaseContent;
            if (content == null)
            {
                GUI.Label(
                    new Rect(
                        panel.x + 28f * scale,
                        panel.y + 100f * scale,
                        panel.width - 56f * scale,
                        40f * scale
                    ),
                    "案件内容资产尚未加载。",
                    UiTheme.Label(16, TextAnchor.MiddleCenter)
                );
                return;
            }

            float padding = 28f * scale;
            float gap = 18f * scale;
            float contentTop = panel.y + 92f * scale;
            float footerHeight = 100f * scale;
            float contentBottom = panel.yMax - footerHeight;
            float contentWidth = panel.width - padding * 2f;
            float statementWidth = (contentWidth - gap) * 0.48f;
            float evidenceWidth = contentWidth - statementWidth - gap;
            Rect statementColumn = new(
                panel.x + padding,
                contentTop,
                statementWidth,
                contentBottom - contentTop
            );
            Rect evidenceColumn = new(
                statementColumn.xMax + gap,
                contentTop,
                evidenceWidth,
                contentBottom - contentTop
            );

            DrawColumn(
                statementColumn,
                "证词",
                "选择一段人物说法",
                content.Testimonies.Count,
                index => content.Testimonies[index].Id,
                index => content.Testimonies[index].Source,
                index => content.Testimonies[index].Text,
                selectedStatementId,
                select =>
                {
                    selectedStatementId = select;
                    GameAudio.PlayInteraction();
                }
            );
            DrawColumn(
                evidenceColumn,
                "证据",
                "选择一份账册或封条",
                content.Evidence.Count,
                index => content.Evidence[index].Id,
                index => content.Evidence[index].Title,
                index => content.Evidence[index].Detail,
                selectedEvidenceId,
                select =>
                {
                    selectedEvidenceId = select;
                    GameAudio.PlayInteraction();
                }
            );

            DrawFooter(panel, scale);
        }

        private static void DrawPanel(Rect panel)
        {
            UiTheme.DrawRect(panel, new Color(0.045f, 0.055f, 0.07f, 0.99f));
            UiTheme.DrawBorder(panel, new Color(1f, 1f, 1f, 0.13f));
            UiTheme.DrawRect(
                new Rect(panel.x, panel.y, panel.width, 3f),
                UiTheme.Accent
            );
        }

        private void DrawHeader(Rect panel, float scale)
        {
            GUIStyle eyebrow = UiTheme.Label(11, TextAnchor.MiddleLeft, bold: true);
            eyebrow.normal.textColor = UiTheme.Accent;
            GUI.Label(
                new Rect(
                    panel.x + 28f * scale,
                    panel.y + 13f * scale,
                    panel.width - 56f * scale,
                    20f * scale
                ),
                "TESTIMONY · CONTRADICTION",
                eyebrow
            );
            GUI.Label(
                new Rect(
                    panel.x + 28f * scale,
                    panel.y + 32f * scale,
                    panel.width - 230f * scale,
                    42f * scale
                ),
                "证词对照",
                UiTheme.Label(27, TextAnchor.MiddleLeft, bold: true)
            );
            GUI.Label(
                new Rect(
                    panel.xMax - 224f * scale,
                    panel.y + 37f * scale,
                    panel.width - 56f * scale,
                    34f * scale
                ),
                $"已尝试 {investigationState.ContradictionAttempts} 次  ·  [ T / Esc ] 返回",
                UiTheme.Label(11, TextAnchor.MiddleRight)
            );
        }

        private void DrawColumn(
            Rect column,
            string title,
            string subtitle,
            int count,
            Func<int, string> getId,
            Func<int, string> getHeading,
            Func<int, string> getBody,
            string selectedId,
            Action<string> select)
        {
            float scale = UiTheme.Scale;
            GUI.Label(
                new Rect(
                    column.x,
                    column.y,
                    column.width,
                    24f * scale
                ),
                title,
                UiTheme.Label(16, TextAnchor.MiddleLeft, bold: true)
            );
            GUI.Label(
                new Rect(
                    column.x,
                    column.y + 23f * scale,
                    column.width,
                    20f * scale
                ),
                subtitle,
                UiTheme.Label(10)
            );

            float top = column.y + 50f * scale;
            float gap = 8f * scale;
            float cardHeight = count > 0
                ? (column.yMax - top - gap * (count - 1)) / count
                : column.yMax - top;
            for (int index = 0; index < count; index++)
            {
                string id = getId(index);
                Rect card = new(
                    column.x,
                    top + index * (cardHeight + gap),
                    column.width,
                    cardHeight
                );
                DrawChoiceCard(
                    card,
                    id,
                    getHeading(index),
                    getBody(index),
                    id == selectedId,
                    () => select(id)
                );
            }
        }

        private void DrawChoiceCard(
            Rect card,
            string id,
            string heading,
            string body,
            bool selected,
            Action select)
        {
            float scale = UiTheme.Scale;
            UiTheme.DrawRect(
                card,
                selected
                    ? new Color(0.105f, 0.095f, 0.07f, 0.98f)
                    : UiTheme.Ink
            );
            UiTheme.DrawBorder(
                card,
                selected
                    ? new Color(
                        UiTheme.Accent.r,
                        UiTheme.Accent.g,
                        UiTheme.Accent.b,
                        0.78f
                    )
                    : new Color(1f, 1f, 1f, 0.09f)
            );
            if (selected)
            {
                UiTheme.DrawRect(
                    new Rect(card.x, card.y, 3f * scale, card.height),
                    UiTheme.Accent
                );
            }

            GUIStyle headingStyle = UiTheme.Label(
                13,
                TextAnchor.MiddleLeft,
                bold: true
            );
            headingStyle.normal.textColor = selected
                ? UiTheme.Accent
                : UiTheme.Text;
            GUI.Label(
                new Rect(
                    card.x + 14f * scale,
                    card.y + 4f * scale,
                    card.width - 28f * scale,
                    22f * scale
                ),
                heading,
                headingStyle
            );
            GUI.Label(
                new Rect(
                    card.x + 14f * scale,
                    card.y + 27f * scale,
                    card.width - 28f * scale,
                    card.height - 31f * scale
                ),
                body,
                UiTheme.Label(11, TextAnchor.UpperLeft)
            );

            if (GUI.Button(card, GUIContent.none, GUIStyle.none))
            {
                select();
            }
        }

        private void DrawFooter(Rect panel, float scale)
        {
            float padding = 28f * scale;
            float top = panel.yMax - 92f * scale;
            Rect feedbackPanel = new(
                panel.x + padding,
                top,
                panel.width - padding * 2f - 224f * scale,
                72f * scale
            );
            UiTheme.DrawRect(feedbackPanel, new Color(0.025f, 0.032f, 0.043f, 0.98f));
            UiTheme.DrawRect(
                new Rect(
                    feedbackPanel.x,
                    feedbackPanel.y,
                    3f * scale,
                    feedbackPanel.height
                ),
                feedbackColor
            );
            GUIStyle feedbackStyle = UiTheme.Label(12);
            feedbackStyle.normal.textColor = feedbackColor;
            GUI.Label(
                new Rect(
                    feedbackPanel.x + 14f * scale,
                    feedbackPanel.y + 5f * scale,
                    feedbackPanel.width - 24f * scale,
                    feedbackPanel.height - 10f * scale
                ),
                string.IsNullOrWhiteSpace(feedback)
                    ? "选择一条证词和一份证据。"
                    : feedback,
                feedbackStyle
            );

            Rect submitButton = new(
                panel.xMax - padding - 204f * scale,
                top,
                204f * scale,
                72f * scale
            );
            bool solved = investigationState.TestimonyContradictionSolved;
            GUI.enabled = !solved
                && investigationState.Stage
                    == ChapterStage.CrossExamineTestimony;
            if (GUI.Button(
                submitButton,
                solved
                    ? "推理已成立"
                    : "指出矛盾  [Enter]",
                UiTheme.Button(14)
            ))
            {
                SubmitSelection();
            }
            GUI.enabled = true;
        }

        private bool IsBlockedByOtherUi()
        {
            return GameplayUiState.HasOpenModalExcept(this)
                || (ScreenFade.Instance != null
                    && ScreenFade.Instance.IsTransitioning);
        }

        private void PreparePreview()
        {
            if (investigationState == null)
            {
                return;
            }

            investigationState.CompleteWitnessBriefing();
            investigationState.CollectClue("old-street-sign");
            investigationState.CollectClue("salt-ledger");
            investigationState.CollectClue("official-seal");
            investigationState.SetWitnessStance(WitnessStance.PersonFirst);
            investigationState.SetEvidencePlan(EvidencePlan.PrivateProtection);
            investigationState.CompleteEvidenceReview();
            ContradictionDefinition rule = investigationState.CaseContent?.Contradiction;
            selectedStatementId = rule?.StatementId;
            selectedEvidenceId = rule?.EvidenceId;
            SetFeedback(
                "已选：沈伯关于初五封条的说法 + 七车盐账牌。确认两者是否能够同时成立。",
                UiTheme.Muted
            );
        }

        private static string GetArgumentValue(string prefix)
        {
            foreach (string argument in Demo02.Core.RuntimeLaunchOptions.Arguments)
            {
                if (argument.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return argument.Substring(prefix.Length);
                }
            }

            return string.Empty;
        }

        private IEnumerator CapturePreview(string path)
        {
            yield return new WaitForSecondsRealtime(0.6f);
            yield return new WaitForEndOfFrame();
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            ScreenCapture.CaptureScreenshot(path);
            Debug.Log($"Testimony board preview captured to {path}");
        }
    }
}

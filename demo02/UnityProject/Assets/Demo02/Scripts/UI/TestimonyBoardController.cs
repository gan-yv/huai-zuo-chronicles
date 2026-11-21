using System;
using System.Collections;
using System.IO;
using Demo02.Core;
using Demo02.Narrative;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Demo02.UI
{
    public sealed class TestimonyBoardController : MonoBehaviour
    {
        private static readonly string[] StatementIds =
        {
            TestimonyPuzzleRules.StatementId,
            "teahouse-salt-carts",
            "clerk-removal-order"
        };

        private static readonly string[] StatementSources =
        {
            "沈伯 · 守仓人",
            "茶铺伙计 · 街口",
            "官署抄书吏 · 册页旁证"
        };

        private static readonly string[] Statements =
        {
            "封条是初五傍晚贴上的。那天阿砚还在仓里，门外还没有那七车盐。",
            "初五到初八都见过盐车，我记不清七车究竟是在哪一夜卸下的。",
            "旧街的名字是在封仓之后，才从官署册页上一起消失的。"
        };

        private static readonly string[] EvidenceIds =
        {
            "old-street-sign",
            "salt-ledger",
            "official-seal"
        };

        private static readonly string[] EvidenceTitles =
        {
            "盐引巷门牌",
            "七车盐账牌",
            "陆崇除名封条"
        };

        private static readonly string[] EvidenceDetails =
        {
            "旧漆下藏着“盐引巷”三字，背面铜钉标出旧时锚点顺序。",
            "七车私盐于初七后半夜入城，复核签字为沈砚。",
            "封条落款为初五，背面写着“知情人沈砚，宜除名”。"
        };

        public static TestimonyBoardController Instance { get; private set; }

        private InvestigationState investigationState;
        private bool isOpen;
        private float previousTimeScale = 1f;
        private string selectedStatementId;
        private string selectedEvidenceId;
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
                Environment.GetCommandLineArgs(),
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
            if (Keyboard.current == null)
            {
                return;
            }

            if (isOpen)
            {
                if (Keyboard.current.escapeKey.wasPressedThisFrame
                    || Keyboard.current.tKey.wasPressedThisFrame)
                {
                    ToggleBoard();
                    return;
                }

                if (Keyboard.current.enterKey.wasPressedThisFrame
                    || Keyboard.current.numpadEnterKey.wasPressedThisFrame)
                {
                    SubmitSelection();
                }

                return;
            }

            if (!Keyboard.current.tKey.wasPressedThisFrame
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
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                if (investigationState.TestimonyContradictionSolved)
                {
                    selectedStatementId = TestimonyPuzzleRules.StatementId;
                    selectedEvidenceId = TestimonyPuzzleRules.EvidenceId;
                    SetFeedback(
                        TestimonyPuzzleRules.CorrectExplanation,
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

                GameAudio.PlayInteraction();
            }
            else
            {
                Time.timeScale = previousTimeScale;
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
        }

        private void OnDestroy()
        {
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
            switch (result)
            {
                case ContradictionResult.Correct:
                    SetFeedback(
                        TestimonyPuzzleRules.CorrectExplanation,
                        UiTheme.Accent
                    );
                    GameAudio.PlayConfirm();
                    break;
                case ContradictionResult.Incorrect:
                    SetFeedback(
                        TestimonyPuzzleRules.GetIncorrectHint(
                            investigationState.ContradictionAttempts
                        ),
                        new Color(0.94f, 0.58f, 0.32f)
                    );
                    GameAudio.PlayInteraction();
                    break;
                case ContradictionResult.AlreadySolved:
                    SetFeedback(
                        TestimonyPuzzleRules.CorrectExplanation,
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
                StatementIds,
                StatementSources,
                Statements,
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
                EvidenceIds,
                EvidenceTitles,
                EvidenceDetails,
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
            string[] ids,
            string[] headings,
            string[] bodies,
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
            float cardHeight = (column.yMax - top - gap * 2f) / 3f;
            for (int index = 0; index < ids.Length; index++)
            {
                Rect card = new(
                    column.x,
                    top + index * (cardHeight + gap),
                    column.width,
                    cardHeight
                );
                DrawChoiceCard(
                    card,
                    ids[index],
                    headings[index],
                    bodies[index],
                    ids[index] == selectedId,
                    () => select(ids[index])
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
            return (WorldMapController.Instance != null
                    && WorldMapController.Instance.IsOpen)
                || (InvestigationJournalController.Instance != null
                    && InvestigationJournalController.Instance.IsOpen)
                || (SettingsController.Instance != null
                    && SettingsController.Instance.IsOpen)
                || (SaveSlotController.Instance != null
                    && SaveSlotController.Instance.IsOpen)
                || (DialogueController.Instance != null
                    && DialogueController.Instance.IsOpen)
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
            selectedStatementId = TestimonyPuzzleRules.StatementId;
            selectedEvidenceId = TestimonyPuzzleRules.EvidenceId;
            SetFeedback(
                "已选：沈伯关于初五封条的说法 + 七车盐账牌。确认两者是否能够同时成立。",
                UiTheme.Muted
            );
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

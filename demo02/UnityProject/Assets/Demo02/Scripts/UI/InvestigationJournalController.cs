using System.Collections;
using System.IO;
using Demo02.Core;
using Demo02.Narrative;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Demo02.UI
{
    public sealed class InvestigationJournalController : MonoBehaviour
    {
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
            "旧漆下藏着“盐引巷”三字。这里是盐船入城后的第一处验引点。",
            "七车盐在暴雨夜卸入旧仓，复核签字为沈砚。",
            "封条日期早于旧街消失一天，背面写着“知情人沈砚，宜除名”。"
        };

        private InvestigationState investigationState;
        private bool isOpen;
        private float previousTimeScale = 1f;

        public static InvestigationJournalController Instance { get; private set; }
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
            if (System.Array.IndexOf(
                System.Environment.GetCommandLineArgs(),
                "--preview-journal"
            ) >= 0)
            {
                ToggleJournal();
                string capturePath = GetArgumentValue("--capture-journal=");
                if (!string.IsNullOrWhiteSpace(capturePath))
                {
                    StartCoroutine(CaptureJournal(capturePath));
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
                    || Keyboard.current.jKey.wasPressedThisFrame)
                {
                    ToggleJournal();
                }

                return;
            }

            if (!Keyboard.current.jKey.wasPressedThisFrame
                || (WorldMapController.Instance != null && WorldMapController.Instance.IsOpen)
                || (DialogueController.Instance != null && DialogueController.Instance.IsOpen)
                || Time.timeScale <= 0f
                || (ScreenFade.Instance != null && ScreenFade.Instance.IsTransitioning))
            {
                return;
            }

            ToggleJournal();
        }

        public void ToggleJournal()
        {
            if (!isOpen && Time.timeScale <= 0f)
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

        private void OnGUI()
        {
            if (!isOpen || investigationState == null)
            {
                return;
            }

            GUI.depth = -110;
            UiTheme.DrawRect(
                new Rect(0f, 0f, Screen.width, Screen.height),
                new Color(0.015f, 0.02f, 0.03f, 0.94f)
            );

            float scale = UiTheme.Scale;
            float width = Mathf.Min(Screen.width - 56f, 1180f * scale);
            float height = Mathf.Min(Screen.height - 56f, 650f * scale);
            Rect panel = new(
                (Screen.width - width) * 0.5f,
                (Screen.height - height) * 0.5f,
                width,
                height
            );
            UiTheme.DrawRect(panel, new Color(0.055f, 0.065f, 0.08f, 0.98f));
            UiTheme.DrawBorder(panel, new Color(1f, 1f, 1f, 0.12f));
            UiTheme.DrawRect(
                new Rect(panel.x, panel.y, panel.width, 3f),
                UiTheme.Accent
            );

            GUIStyle eyebrowStyle = UiTheme.Label(
                11,
                TextAnchor.MiddleLeft,
                bold: true
            );
            eyebrowStyle.normal.textColor = UiTheme.Accent;
            GUI.Label(
                new Rect(
                    panel.x + 28f * scale,
                    panel.y + 16f * scale,
                    panel.width - 56f * scale,
                    20f * scale
                ),
                "CHAPTER 01 · INVESTIGATION FILE",
                eyebrowStyle
            );
            GUI.Label(
                new Rect(
                    panel.x + 28f * scale,
                    panel.y + 34f * scale,
                    panel.width - 56f * scale,
                    38f * scale
                ),
                "调查案卷 · 消失的盐引",
                UiTheme.Label(25, TextAnchor.MiddleLeft, bold: true)
            );

            float contentTop = panel.y + 84f * scale;
            float contentBottom = panel.yMax - 54f * scale;
            float contentWidth = panel.width - 56f * scale;
            float leftWidth = contentWidth * 0.56f;
            float rightWidth = contentWidth - leftWidth - 24f * scale;
            Rect leftColumn = new(
                panel.x + 28f * scale,
                contentTop,
                leftWidth,
                contentBottom - contentTop
            );
            Rect rightColumn = new(
                leftColumn.xMax + 24f * scale,
                contentTop,
                rightWidth,
                contentBottom - contentTop
            );

            DrawObjective(leftColumn, scale);
            DrawEvidenceList(leftColumn, scale);
            DrawPeople(rightColumn, scale);
            DrawReasoning(rightColumn, scale);

            GUI.Label(
                new Rect(
                    panel.x + 28f * scale,
                    panel.yMax - 42f * scale,
                    panel.width - 56f * scale,
                    24f * scale
                ),
                "[ J / Esc ] 关闭案卷",
                UiTheme.Label(12, TextAnchor.MiddleRight)
            );
        }

        private void DrawObjective(Rect column, float scale)
        {
            Rect objective = new(
                column.x,
                column.y,
                column.width,
                64f * scale
            );
            UiTheme.DrawRect(objective, UiTheme.Ink);
            UiTheme.DrawRect(
                new Rect(objective.x, objective.y, 3f * scale, objective.height),
                UiTheme.Accent
            );
            GUI.Label(
                new Rect(
                    objective.x + 16f * scale,
                    objective.y + 6f * scale,
                    objective.width - 28f * scale,
                    20f * scale
                ),
                "当前目标",
                UiTheme.Label(11, TextAnchor.MiddleLeft, bold: true)
            );
            GUI.Label(
                new Rect(
                    objective.x + 16f * scale,
                    objective.y + 27f * scale,
                    objective.width - 28f * scale,
                    30f * scale
                ),
                ChapterCopy.GetObjective(investigationState),
                UiTheme.Label(14)
            );
        }

        private void DrawEvidenceList(Rect column, float scale)
        {
            float listTop = column.y + 78f * scale;
            GUI.Label(
                new Rect(column.x, listTop, column.width, 24f * scale),
                "证据记录",
                UiTheme.Label(15, TextAnchor.MiddleLeft, bold: true)
            );

            float cardTop = listTop + 30f * scale;
            float available = column.yMax - cardTop;
            float gap = 8f * scale;
            float cardHeight = (available - gap * 2f) / 3f;
            for (int index = 0; index < EvidenceIds.Length; index++)
            {
                Rect card = new(
                    column.x,
                    cardTop + index * (cardHeight + gap),
                    column.width,
                    cardHeight
                );
                bool discovered = investigationState.HasClue(EvidenceIds[index]);
                UiTheme.DrawRect(card, discovered ? UiTheme.Panel : UiTheme.Ink);
                UiTheme.DrawRect(
                    new Rect(card.x, card.y, 3f * scale, card.height),
                    discovered ? UiTheme.Accent : new Color(0.3f, 0.32f, 0.34f)
                );
                UiTheme.DrawBorder(card, new Color(1f, 1f, 1f, 0.09f));

                GUIStyle titleStyle = UiTheme.Label(
                    14,
                    TextAnchor.MiddleLeft,
                    bold: true
                );
                titleStyle.normal.textColor = discovered
                    ? UiTheme.Text
                    : UiTheme.Muted;
                GUI.Label(
                    new Rect(
                        card.x + 14f * scale,
                        card.y + 4f * scale,
                        card.width - 96f * scale,
                        20f * scale
                    ),
                    $"{index + 1:00}  {EvidenceTitles[index]}",
                    titleStyle
                );
                GUIStyle statusStyle = UiTheme.Label(
                    10,
                    TextAnchor.MiddleRight,
                    bold: true
                );
                statusStyle.normal.textColor = discovered
                    ? UiTheme.Accent
                    : UiTheme.Muted;
                GUI.Label(
                    new Rect(
                        card.xMax - 86f * scale,
                        card.y + 4f * scale,
                        74f * scale,
                        20f * scale
                    ),
                    discovered ? "已记录" : "未发现",
                    statusStyle
                );
                GUI.Label(
                    new Rect(
                        card.x + 14f * scale,
                        card.y + 26f * scale,
                        card.width - 28f * scale,
                        card.height - 30f * scale
                    ),
                    discovered
                        ? EvidenceDetails[index]
                        : "这条记录仍藏在盐引巷中。",
                    UiTheme.Label(12, TextAnchor.UpperLeft)
                );
            }
        }

        private void DrawPeople(Rect column, float scale)
        {
            GUI.Label(
                new Rect(column.x, column.y, column.width, 24f * scale),
                "关键人物",
                UiTheme.Label(15, TextAnchor.MiddleLeft, bold: true)
            );

            bool knowsLuChong = investigationState.HasClue("official-seal");
            string[] names =
            {
                "沈伯",
                "沈砚",
                knowsLuChong ? "陆崇" : "未确认人物"
            };
            string[] roles =
            {
                "失踪者之父 · 记得官署已经忘记的人",
                "盐仓账房 · 带走七页私账簿后失踪",
                knowsLuChong
                    ? "转运副使 · 提前写下除名封条"
                    : "封条上的署名仍未查明"
            };
            float cardHeight = 58f * scale;
            for (int index = 0; index < names.Length; index++)
            {
                Rect card = new(
                    column.x,
                    column.y + (30f + index * 64f) * scale,
                    column.width,
                    cardHeight
                );
                UiTheme.DrawRect(card, UiTheme.Ink);
                UiTheme.DrawBorder(card, new Color(1f, 1f, 1f, 0.08f));
                GUI.Label(
                    new Rect(
                        card.x + 12f * scale,
                        card.y + 3f * scale,
                        card.width - 24f * scale,
                        20f * scale
                    ),
                    names[index],
                    UiTheme.Label(13, TextAnchor.MiddleLeft, bold: true)
                );
                GUI.Label(
                    new Rect(
                        card.x + 12f * scale,
                        card.y + 24f * scale,
                        card.width - 24f * scale,
                        28f * scale
                    ),
                    roles[index],
                    UiTheme.Label(11)
                );
            }
        }

        private void DrawReasoning(Rect column, float scale)
        {
            float top = column.y + 226f * scale;
            Rect card = new(
                column.x,
                top,
                column.width,
                column.yMax - top
            );
            UiTheme.DrawRect(card, new Color(0.085f, 0.075f, 0.055f, 0.96f));
            UiTheme.DrawBorder(card, new Color(0.88f, 0.7f, 0.38f, 0.35f));

            GUIStyle titleStyle = UiTheme.Label(
                14,
                TextAnchor.MiddleLeft,
                bold: true
            );
            titleStyle.normal.textColor = UiTheme.Accent;
            GUI.Label(
                new Rect(
                    card.x + 14f * scale,
                    card.y + 8f * scale,
                    card.width - 28f * scale,
                    24f * scale
                ),
                "当前推理",
                titleStyle
            );
            GUI.Label(
                new Rect(
                    card.x + 14f * scale,
                    card.y + 36f * scale,
                    card.width - 28f * scale,
                    card.height - 46f * scale
                ),
                ChapterCopy.GetReasoning(investigationState)
                    + $"\n\n时间锚点：{investigationState.StabilizedTimeAnchorCount}"
                    + $"/{investigationState.RequiredTimeAnchorCount}",
                UiTheme.Label(12, TextAnchor.UpperLeft)
            );
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

        private IEnumerator CaptureJournal(string path)
        {
            yield return new WaitForSecondsRealtime(0.6f);
            yield return new WaitForEndOfFrame();
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            ScreenCapture.CaptureScreenshot(path);
            Debug.Log($"Investigation journal preview captured to {path}");
        }
    }
}

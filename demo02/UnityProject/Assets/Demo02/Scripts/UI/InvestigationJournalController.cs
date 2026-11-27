using System.Collections;
using System.IO;
using Demo02.Core;
using Demo02.Narrative;
using Demo02.Player;
using Demo02.WorldState;
using UnityEngine;

namespace Demo02.UI
{
    public sealed class InvestigationJournalController : MonoBehaviour
    {
        private InvestigationState investigationState;
        private bool isOpen;
        private float previousTimeScale = 1f;
        private readonly string[] peopleNames = new string[6];
        private readonly string[] peopleRoles = new string[6];

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
            Demo02.Core.GameServices.Register(this);
        }

        private void Start()
        {
            investigationState = Demo02.Core.GameServices.Resolve<InvestigationState>();
            if (System.Array.IndexOf(
                Demo02.Core.RuntimeLaunchOptions.Arguments,
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
            if (isOpen)
            {
                if (Demo02Input.CancelPressed
                    || Demo02Input.JournalPressed)
                {
                    ToggleJournal();
                }

                return;
            }

            if (!Demo02Input.JournalPressed
                || GameplayUiState.HasOpenModalExcept(this)
                || Time.timeScale <= 0f
                || (Demo02.Core.GameServices.Resolve<ScreenFade>() != null && Demo02.Core.GameServices.Resolve<ScreenFade>().IsTransitioning))
            {
                return;
            }

            ToggleJournal();
        }

        public void ToggleJournal()
        {
            if (!isOpen
                && (Time.timeScale <= 0f
                    || GameplayUiState.HasOpenModalExcept(this)))
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
                Demo02.Core.GameServices.Unregister(this);
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
                $"{investigationState.CaseContent?.ChapterLabel ?? "CHAPTER"} · INVESTIGATION FILE",
                eyebrowStyle
            );
            GUI.Label(
                new Rect(
                    panel.x + 28f * scale,
                    panel.y + 34f * scale,
                    panel.width - 56f * scale,
                    38f * scale
                ),
                $"调查案卷 · {investigationState.CaseContent?.Title ?? "内容未加载"}",
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
            OptionalObjectiveDefinition optional =
                OptionalObjectiveRuntime.FindActive(
                    investigationState.CaseContent,
                    investigationState
                );
            Rect objective = new(
                column.x,
                column.y,
                column.width,
                (optional != null ? 96f : 64f) * scale
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
            if (optional != null)
            {
                GUIStyle optionalStyle = UiTheme.Label(
                    11,
                    TextAnchor.MiddleLeft,
                    bold: true
                );
                optionalStyle.normal.textColor = UiTheme.Accent;
                GUI.Label(
                    new Rect(
                        objective.x + 16f * scale,
                        objective.y + 58f * scale,
                        objective.width - 28f * scale,
                        30f * scale
                    ),
                    $"可选 · {optional.Title}\n{optional.TrackerHint}",
                    optionalStyle
                );
            }
        }

        private void DrawEvidenceList(Rect column, float scale)
        {
            CaseDefinition content = investigationState.CaseContent;
            if (content == null)
            {
                GUI.Label(
                    new Rect(column.x, column.y, column.width, 30f * scale),
                    "案件内容资产尚未加载",
                    UiTheme.Label(14)
                );
                return;
            }

            bool hasOptional = OptionalObjectiveRuntime.FindActive(
                investigationState.CaseContent,
                investigationState
            ) != null;
            float listTop = column.y + (hasOptional ? 110f : 78f) * scale;
            GUI.Label(
                new Rect(column.x, listTop, column.width, 24f * scale),
                "证据记录",
                UiTheme.Label(15, TextAnchor.MiddleLeft, bold: true)
            );

            float cardTop = listTop + 30f * scale;
            float available = column.yMax - cardTop;
            float gap = 8f * scale;
            int count = content.Evidence.Count;
            float cardHeight = count > 0
                ? (available - gap * (count - 1)) / count
                : available;
            for (int index = 0; index < count; index++)
            {
                EvidenceDefinition evidence = content.Evidence[index];
                Rect card = new(
                    column.x,
                    cardTop + index * (cardHeight + gap),
                    column.width,
                    cardHeight
                );
                bool discovered = investigationState.HasClue(evidence.Id);
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
                    $"{index + 1:00}  {evidence.Title}",
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
                        ? evidence.Detail
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

            bool knowsLuChong = investigationState.CaseContent != null
                && investigationState.HasClue(
                    investigationState.CaseContent.CulpritEvidenceId
                );
            int count = 0;
            AddPerson(
                ref count,
                "沈伯",
                "失踪者之父 · 记得官署已经忘记的人"
            );
            AddPerson(
                ref count,
                "沈砚",
                "盐仓账房 · 带走七页私账簿后失踪"
            );
            AddPerson(
                ref count,
                knowsLuChong ? "陆崇" : "未确认人物",
                knowsLuChong
                    ? investigationState.HasFact("lu-chong-threat")
                        ? "转运副使 · 当面以空白秩序威胁证人"
                        : "转运副使 · 提前写下除名封条"
                    : "封条上的署名仍未查明"
            );
            if (investigationState.Stage >= ChapterStage.Investigate)
            {
                string clerkRole = investigationState.HasFact(
                    "archive-clerk-recruited"
                )
                    ? "抄录同盟 · 在册边写下父亲严复"
                    : investigationState.HasFact(
                        "archive-clerk-sheltered"
                    )
                        ? "留在局外 · 保存父亲留下的拓页"
                        : "官署抄书吏 · 发现被刮名字下的新浆";
                AddPerson(
                    ref count,
                    "抄书吏 严槐",
                    clerkRole
                );
            }
            if (investigationState.HasFact("warehouse-night-shift"))
            {
                AddPerson(
                    ref count,
                    "盐仓搬运工",
                    "夜班证人 · 身体记得四十二袋盐"
                );
            }
            if (investigationState.HasFact(
                    "warehouse-daughter-remembered"
                )
                && investigationState.SelectedConclusion
                    == ConclusionChoice.Preserve)
            {
                AddPerson(
                    ref count,
                    "阿禾",
                    investigationState.HasFact(
                        "warehouse-daughter-public"
                    )
                        ? "搬运工之女 · 名字已写回公开名册"
                        : "搬运工之女 · 名字留在私人案卷"
                );
            }
            float cardHeight = 58f * scale;
            for (int index = 0; index < count; index++)
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
                    peopleNames[index],
                    UiTheme.Label(13, TextAnchor.MiddleLeft, bold: true)
                );
                GUI.Label(
                    new Rect(
                        card.x + 12f * scale,
                        card.y + 24f * scale,
                        card.width - 24f * scale,
                        28f * scale
                    ),
                    peopleRoles[index],
                    UiTheme.Label(11)
                );
            }
        }

        private void AddPerson(
            ref int count,
            string displayName,
            string role)
        {
            if (count >= peopleNames.Length)
            {
                return;
            }
            peopleNames[count] = displayName;
            peopleRoles[count] = role;
            count++;
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
                    + $"/{investigationState.RequiredTimeAnchorCount}"
                    + $"\n残留回声：{investigationState.DiscoveredEchoCount}/3"
                    + GetStoryFactSummary(investigationState.CaseContent)
                    + (Demo02.Core.GameServices.Resolve<WorldContentTracker>() != null
                        ? Demo02.Core.GameServices.Resolve<WorldContentTracker>().GetJournalSummary()
                        : string.Empty)
                    + "\n"
                    + (Demo02.Core.GameServices.Resolve<ContinuationLoadout>() != null
                        ? Demo02.Core.GameServices.Resolve<ContinuationLoadout>().GetJournalSummary()
                        : "续式槽位：未初始化")
                    + $"\n调查立场：{ChapterCopy.GetStanceLabel(investigationState)}"
                    + $"\n证据去向：{ChapterCopy.GetPlanLabel(investigationState)}"
                    + "\n按 V 开启修痕视界，寻找隐藏回声。",
                UiTheme.Label(12, TextAnchor.UpperLeft)
            );
        }

        private string GetStoryFactSummary(CaseDefinition content)
        {
            if (content == null)
            {
                return string.Empty;
            }

            string facts = string.Empty;
            for (int index = 0; index < content.StoryFacts.Count; index++)
            {
                StoryFactDefinition fact = content.StoryFacts[index];
                if (!investigationState.HasFact(fact.Id))
                {
                    continue;
                }

                facts += string.IsNullOrWhiteSpace(facts)
                    ? fact.Title
                    : $"、{fact.Title}";
            }

            return string.IsNullOrWhiteSpace(facts)
                ? "\n支线旁证：尚未发现"
                : $"\n支线旁证：{facts}";
        }

        private static string GetArgumentValue(string prefix)
        {
            foreach (string argument in Demo02.Core.RuntimeLaunchOptions.Arguments)
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
            yield return null;
            if (System.Array.IndexOf(
                    Demo02.Core.RuntimeLaunchOptions.Arguments,
                    "--capture-exit"
                ) >= 0)
            {
                Application.Quit();
            }
        }
    }
}

using System;
using System.Collections;
using System.IO;
using Demo02.Narrative;
using Demo02.Player;
using Demo02.UI;
using Demo02.WorldState;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Demo02.Core
{
    [DefaultExecutionOrder(-50)]
    public sealed class GameFlowController : MonoBehaviour, IGameFlowState
    {
        public static GameFlowController Instance { get; private set; }

        private InvestigationState investigationState;
        private PlayerPartyController party;
        private bool isPaused;
        private bool isEndingFrozen;
        private bool endingDismissed;
        private float sessionStartedAt;
        private float capturedPlayTime;

        public bool IsPaused => isPaused;
        public bool IsGameEnding => IsEnding;
        public bool CanExploreAfterEnding =>
            investigationState != null
            && investigationState.IsConclusionReached
            && (party == null || !party.IsPartyDefeated);

#if UNITY_EDITOR
        public void BindForTests(
            InvestigationState state,
            PlayerPartyController playerParty)
        {
            investigationState = state;
            party = playerParty;
        }
#endif

        private void Awake()
        {
            Instance = this;
            Demo02.Core.GameServices.Register(this);
            GameServices.Register<IGameFlowState>(this);
            investigationState = Demo02.Core.GameServices.Resolve<InvestigationState>();
            party = Demo02.Core.GameServices.Resolve<PlayerPartyController>();
        }

        private void Start()
        {
            Time.timeScale = 1f;
            sessionStartedAt = Time.unscaledTime;

            string gameplayCapture = GetArgumentValue("--capture-game=");
            if (!string.IsNullOrWhiteSpace(gameplayCapture))
            {
                StartCoroutine(CaptureGameplay(gameplayCapture));
            }
            if (Array.IndexOf(
                    Demo02.Core.RuntimeLaunchOptions.Arguments,
                    "--preview-teahouse"
                ) >= 0)
            {
                StartCoroutine(
                    PreparePreviewPosition(
                        new Vector3(61.4f, 0f, 106.5f),
                        "dongguan-street",
                        "东关街"
                    )
                );
            }
            if (Array.IndexOf(
                    Demo02.Core.RuntimeLaunchOptions.Arguments,
                    "--preview-combat"
                ) >= 0)
            {
                StartCoroutine(
                    PreparePreviewPosition(
                        new Vector3(0f, 1f, -1f),
                        "official-street",
                        "官署前街"
                    )
                );
            }
            if (Array.IndexOf(
                    Demo02.Core.RuntimeLaunchOptions.Arguments,
                    "--preview-companion-combat"
                ) >= 0)
            {
                StartCoroutine(
                    PreparePreviewPosition(
                        new Vector3(0f, 1f, 7.5f),
                        "official-street",
                        "官署前街"
                    )
                );
            }
            if (Array.IndexOf(
                    Demo02.Core.RuntimeLaunchOptions.Arguments,
                    "--preview-glide"
                ) >= 0)
            {
                StartCoroutine(PrepareGlidePreview());
            }
            if (Array.IndexOf(
                    Demo02.Core.RuntimeLaunchOptions.Arguments,
                    "--preview-abilities"
                ) >= 0)
            {
                StartCoroutine(PrepareAbilityPreview());
            }
            if (Array.IndexOf(
                    Demo02.Core.RuntimeLaunchOptions.Arguments,
                    "--preview-streaming"
                ) >= 0)
            {
                StartCoroutine(
                    PreparePreviewPosition(
                        new Vector3(0f, 1f, 2f),
                        "salt-street",
                        "盐仓横街"
                    )
                );
            }
            if (Array.IndexOf(
                    Demo02.Core.RuntimeLaunchOptions.Arguments,
                    "--preview-story"
                ) >= 0)
            {
                StartCoroutine(
                    PreparePreviewPosition(
                        new Vector3(60f, 1f, 35f),
                        "dongguan-street",
                        "东关街"
                    )
                );
            }
            if (Array.IndexOf(
                    Demo02.Core.RuntimeLaunchOptions.Arguments,
                    "--preview-elite"
                ) >= 0)
            {
                StartCoroutine(
                    PreparePreviewPosition(
                        new Vector3(60f, 1f, 210f),
                        "caiyi-street",
                        "彩衣街"
                    )
                );
            }
            if (Array.IndexOf(
                    Demo02.Core.RuntimeLaunchOptions.Arguments,
                    "--preview-glide-course"
                ) >= 0)
            {
                StartCoroutine(
                    PreparePreviewPosition(
                        new Vector3(57f, 1f, 8f),
                        "dongguan-street",
                        "东关街"
                    )
                );
            }
            if (Array.IndexOf(
                Demo02.Core.RuntimeLaunchOptions.Arguments,
                "--preview-paper-kite"
            ) >= 0)
            {
                StartCoroutine(PreparePaperKitePreview(showChoice: false));
            }
            if (Array.IndexOf(
                Demo02.Core.RuntimeLaunchOptions.Arguments,
                "--preview-paper-kite-choice"
            ) >= 0)
            {
                StartCoroutine(PreparePaperKitePreview(showChoice: true));
            }
            if (Array.IndexOf(
                Demo02.Core.RuntimeLaunchOptions.Arguments,
                "--preview-paper-kite-result"
            ) >= 0)
            {
                StartCoroutine(PreparePaperKiteResultPreview());
            }
            if (Array.IndexOf(
                Demo02.Core.RuntimeLaunchOptions.Arguments,
                "--preview-companion"
            ) >= 0)
            {
                StartCoroutine(
                    PreparePreviewPosition(
                        new Vector3(60f, 1f, 18f),
                        "dongguan-street",
                        "东关街"
                    )
                );
            }
            if (Array.IndexOf(
                Demo02.Core.RuntimeLaunchOptions.Arguments,
                "--preview-pavilion"
            ) >= 0)
            {
                StartCoroutine(
                    PreparePreviewPosition(
                        new Vector3(60f, 0.1f, 118f),
                        "dongguan-street",
                        "东关街"
                    )
                );
            }
            if (Array.IndexOf(
                Demo02.Core.RuntimeLaunchOptions.Arguments,
                "--preview-cottage"
            ) >= 0)
            {
                StartCoroutine(
                    PreparePreviewPosition(
                        new Vector3(0f, 0.1f, -107f),
                        "cottage-courtyard",
                        "林间小屋"
                    )
                );
            }

            if (Array.IndexOf(
                Demo02.Core.RuntimeLaunchOptions.Arguments,
                "--preview-ending"
            ) >= 0)
            {
                PrepareEndingPreview();
                string path = GetArgumentValue("--capture-ending=");
                if (!string.IsNullOrWhiteSpace(path))
                {
                    StartCoroutine(CaptureEnding(path));
                }
            }
        }

        private IEnumerator PreparePreviewPosition(
            Vector3 position,
            string areaId,
            string areaName)
        {
            yield return null;
            PlayerPartyController previewParty =
                Demo02.Core.GameServices.Resolve<PlayerPartyController>();
            Transform character = previewParty != null
                ? previewParty.ActiveCharacter
                : null;
            if (character == null)
            {
                yield break;
            }

            CharacterController controller =
                character.GetComponent<CharacterController>();
            bool wasEnabled = controller != null && controller.enabled;
            if (controller != null)
            {
                controller.enabled = false;
            }
            character.SetPositionAndRotation(
                position,
                Quaternion.identity
            );
            if (controller != null)
            {
                controller.enabled = wasEnabled;
            }
            Demo02.Core.GameServices.Resolve<WorldRuntime>()?.SetCurrentArea(
                areaId,
                areaName
            );
        }

        private IEnumerator PrepareGlidePreview()
        {
            yield return PreparePreviewPosition(
                new Vector3(0f, 12f, 0f),
                "official-street",
                "官署前街"
            );
            yield return null;
            PlayerPartyController previewParty =
                Demo02.Core.GameServices.Resolve<PlayerPartyController>();
            previewParty?.ActiveCharacter?
                .GetComponent<ThirdPersonController>()
                ?.ForceGlideForPreview();
        }

        private IEnumerator PreparePaperKitePreview(bool showChoice)
        {
            yield return PreparePreviewPosition(
                showChoice
                    ? new Vector3(60f, 1f, 51.5f)
                    : new Vector3(57f, 1f, -1f),
                "dongguan-street",
                "东关街"
            );
            yield return null;
            PaperKiteStoryController story =
                Demo02.Core.GameServices.Resolve<PaperKiteStoryController>();
            if (story == null)
            {
                yield break;
            }

            if (showChoice)
            {
                story.PrepareChoicePreview();
                story.BeginKnotInteraction();
            }
            else
            {
                story.StartQuestFromPreview();
            }
        }

        private IEnumerator PreparePaperKiteResultPreview()
        {
            yield return PreparePreviewPosition(
                new Vector3(60f, 1f, 34f),
                "dongguan-street",
                "东关街"
            );
            yield return null;
            Demo02.Core.GameServices.Resolve<PaperKiteStoryController>()
                ?.CompletePublicChoiceFromPreview();
        }

        private IEnumerator PrepareAbilityPreview()
        {
            yield return PreparePreviewPosition(
                new Vector3(0f, 1f, -1f),
                "official-street",
                "官署前街"
            );
            yield return null;
            PlayerPartyController previewParty =
                Demo02.Core.GameServices.Resolve<PlayerPartyController>();
            CharacterRuntime runtime = previewParty?.ActiveCharacter != null
                ? previewParty.ActiveCharacter
                    .GetComponent<CharacterRuntime>()
                : null;
            runtime?.RestoreEnergy(runtime.MaxEnergy);
        }

        private IEnumerator CaptureGameplay(string path)
        {
            float delay = 1.5f;
            string delayArgument = GetArgumentValue("--capture-delay=");
            if (float.TryParse(
                    delayArgument,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out float parsedDelay))
            {
                delay = Mathf.Clamp(parsedDelay, 0.1f, 30f);
            }
            yield return new WaitForSecondsRealtime(delay);
            yield return new WaitForEndOfFrame();
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log($"Gameplay preview captured to {path}");
            yield return null;
            if (Array.IndexOf(
                    Demo02.Core.RuntimeLaunchOptions.Arguments,
                    "--capture-exit"
                ) >= 0)
            {
                Application.Quit();
            }
        }

        private void Update()
        {
            if (GameplayUiState.IsModalOpen)
            {
                return;
            }

            if (IsEnding)
            {
                if (!isEndingFrozen)
                {
                    isEndingFrozen = true;
                    Time.timeScale = 0f;
                    capturedPlayTime = Time.unscaledTime - sessionStartedAt;
                }

                if (Demo02Input.RestartPressed)
                {
                    RestartCurrentScene();
                }

                return;
            }

            if (isEndingFrozen)
            {
                isEndingFrozen = false;
                Time.timeScale = 1f;
            }

            if (Demo02Input.CancelPressed)
            {
                TogglePause();
                return;
            }

            if (isPaused)
            {
                return;
            }
        }

        private void OnDisable()
        {
            Time.timeScale = 1f;
        }

        private void OnDestroy()
        {
            GameServices.Unregister<IGameFlowState>(this);
            if (Instance == this)
            {
                Demo02.Core.GameServices.Unregister(this);
                Instance = null;
            }
        }

        private void OnGUI()
        {
            if (!isPaused && !IsEnding)
            {
                return;
            }

            if (Demo02.Core.GameServices.Resolve<SaveSlotController>() != null
                && Demo02.Core.GameServices.Resolve<SaveSlotController>().IsOpen)
            {
                return;
            }
            bool endingShown = IsEnding;

            UiTheme.DrawRect(
                new Rect(0f, 0f, Screen.width, Screen.height),
                new Color(0.015f, 0.02f, 0.03f, 0.86f)
            );
            float scale = UiTheme.Scale;

            string title;
            string body;
            if (endingShown)
            {
                if (investigationState != null
                    && investigationState.SelectedConclusion == ConclusionChoice.Preserve)
                {
                    title = "结局：盐引巷重新入册";
                    body = "沈砚的名字重新出现在盐仓账册上，盐引巷回到广陵地图。陆崇背后的势力也看见了修复留下的光。"
                        + ChapterCopy.GetEndingDetail(investigationState)
                        + ChapterCopy.GetEndingEpilogue(investigationState);
                }
                else if (investigationState != null
                    && investigationState.SelectedConclusion == ConclusionChoice.Seal)
                {
                    title = "结局：盐引巷封存";
                    body = "时缝平息，私盐案被永久藏进旧街。沈砚没有回来，只在你手中的修复记录里留下一行名字。"
                        + ChapterCopy.GetEndingDetail(investigationState)
                        + ChapterCopy.GetEndingEpilogue(investigationState);
                }
                else
                {
                    title = "队伍倒下";
                    body = "当前行动失败，可以从最近检查点重新开始。";
                }
            }
            else
            {
                title = "游戏暂停";
                body = "世界已经停止，选择接下来的行动。";
            }

            UiTheme.DrawRect(
                new Rect(
                    Screen.width * 0.5f - 48f * scale,
                    Screen.height * 0.22f,
                    96f * scale,
                    3f
                ),
                UiTheme.Accent
            );
            GUI.Label(
                new Rect(0f, Screen.height * 0.255f, Screen.width, 54f * scale),
                title,
                UiTheme.Label(31, TextAnchor.MiddleCenter, bold: true)
            );
            GUI.Label(
                new Rect(
                    Screen.width * 0.18f,
                    Screen.height * 0.35f,
                    Screen.width * 0.64f,
                    74f * scale
                ),
                body,
                UiTheme.Label(17, TextAnchor.MiddleCenter)
            );

            float width = 300f * scale;
            float x = (Screen.width - width) * 0.5f;
            if (endingShown)
            {
                DrawEndingSummary(scale);
            }

            float y = Screen.height * (endingShown ? 0.69f : 0.48f);
            float buttonHeight = 48f * scale;
            if (endingShown && CanExploreAfterEnding)
            {
                if (GUI.Button(
                    new Rect(x, y, width, buttonHeight),
                    "继续探索街区",
                    UiTheme.Button(16)
                ))
                {
                    ContinueAfterEnding();
                }
                y += 62f * scale;
            }
            else if (!endingShown && GUI.Button(
                    new Rect(x, y, width, buttonHeight),
                    "继续游戏",
                    UiTheme.Button(16)
                ))
            {
                TogglePause();
            }
            float saveY = y + 62f * scale;
            if (!endingShown && GUI.Button(
                new Rect(x, saveY, width, buttonHeight),
                "存档与读档",
                UiTheme.Button(16)
            ))
            {
                OpenSaveSlots();
            }
            float restartY = endingShown ? y : saveY + 62f * scale;
            if (GUI.Button(
                new Rect(x, restartY, width, buttonHeight),
                "重新开始",
                UiTheme.Button(16)
            ))
            {
                RestartCurrentScene();
            }
            if (GUI.Button(
                new Rect(x, restartY + 62f * scale, width, buttonHeight),
                "返回标题",
                UiTheme.Button(16)
            ))
            {
                ReturnToTitle();
            }
        }

        private void DrawEndingSummary(float scale)
        {
            if (investigationState == null)
            {
                return;
            }

            string[] lines = BuildSummaryLines();
            float width = Mathf.Min(Screen.width - 100f, 760f * scale);
            float height = 156f * scale;
            Rect panel = new(
                (Screen.width - width) * 0.5f,
                Screen.height * 0.43f,
                width,
                height
            );
            UiTheme.DrawRect(panel, new Color(0.045f, 0.05f, 0.06f, 0.98f));
            UiTheme.DrawBorder(panel, new Color(0.88f, 0.7f, 0.38f, 0.34f));
            UiTheme.DrawRect(
                new Rect(panel.x, panel.y, 3f * scale, panel.height),
                UiTheme.Accent
            );

            GUIStyle eyebrow = UiTheme.Label(10, TextAnchor.MiddleLeft, bold: true);
            eyebrow.normal.textColor = UiTheme.Accent;
            GUI.Label(
                new Rect(
                    panel.x + 18f * scale,
                    panel.y + 8f * scale,
                    panel.width - 36f * scale,
                    18f * scale
                ),
                "CASE FILE · 结案记录",
                eyebrow
            );
            float lineHeight = 18f * scale;
            for (int index = 0; index < lines.Length; index++)
            {
                GUI.Label(
                    new Rect(
                        panel.x + 18f * scale,
                        panel.y + (29f + index * 18f) * scale,
                        panel.width - 36f * scale,
                        lineHeight
                    ),
                    lines[index],
                    UiTheme.Label(11)
                );
            }
        }

        private string[] BuildSummaryLines()
        {
            int minutes = Mathf.FloorToInt(capturedPlayTime / 60f);
            int seconds = Mathf.FloorToInt(capturedPlayTime % 60f);
            int completedOptional = 0;
            CaseDefinition content = investigationState.CaseContent;
            if (content != null)
            {
                for (int index = 0; index < content.OptionalObjectives.Count; index++)
                {
                    if (OptionalObjectiveRuntime.IsCompleted(
                        content.OptionalObjectives[index],
                        investigationState
                    ))
                    {
                        completedOptional++;
                    }
                }
            }

            string teaStatus;
            if (investigationState.HasFact("teahouse-keeper-protected"))
            {
                teaStatus = "已保护茶肆";
            }
            else if (investigationState.HasFact("teahouse-written-record"))
            {
                teaStatus = "已追问并抄录旁证";
            }
            else if (investigationState.HasFact("teahouse-cart-testimony"))
            {
                teaStatus = "已追问但未抄录";
            }
            else
            {
                teaStatus = "未调查茶肆旁证";
            }

            string clerkStatus;
            if (investigationState.HasFact("archive-clerk-recruited"))
            {
                clerkStatus = "严槐参与抄录";
            }
            else if (investigationState.HasFact(
                    "archive-clerk-sheltered"))
            {
                clerkStatus = "严槐留在局外";
            }
            else if (investigationState.HasFact(
                    "archive-clerk-ink-record"))
            {
                clerkStatus = "严槐已记录新浆";
            }
            else
            {
                clerkStatus = "严槐未追问";
            }
            string aheStatus;
            if (!investigationState.HasFact(
                    "warehouse-daughter-remembered"
                ))
            {
                aheStatus = "阿禾未找到";
            }
            else if (investigationState.SelectedConclusion
                == ConclusionChoice.Preserve)
            {
                aheStatus = investigationState.HasFact(
                    "warehouse-daughter-public"
                )
                    ? "阿禾写回公开名册"
                    : "阿禾留在私人案卷";
            }
            else
            {
                aheStatus = investigationState.HasFact(
                    "warehouse-daughter-public"
                )
                    ? "阿禾公开页转入私藏"
                    : "阿禾留在封条之外";
            }

            return new[]
            {
                $"本次用时  {minutes:00}:{seconds:00}",
                $"线索 {investigationState.CollectedClueCount}/"
                    + $"{investigationState.RequiredClueCount}    "
                    + $"时间锚 {investigationState.StabilizedTimeAnchorCount}/"
                    + $"{investigationState.RequiredTimeAnchorCount}    "
                    + $"回声 {investigationState.DiscoveredEchoCount}/3",
                $"可选目标  {completedOptional}/{content?.OptionalObjectives.Count ?? 0}    "
                    + teaStatus,
                $"调查立场  {ChapterCopy.GetStanceLabel(investigationState)}    "
                    + $"证据去向  {ChapterCopy.GetPlanLabel(investigationState)}",
                $"人物线  {clerkStatus}    {aheStatus}",
                investigationState.HasFact("lu-chong-threat")
                    ? "陆崇  仍以空白秩序威胁证人"
                    : "陆崇  尚未当面交涉"
            };
        }

        private void PrepareEndingPreview()
        {
            if (investigationState == null)
            {
                return;
            }

            investigationState.CompleteWitnessBriefing();
            investigationState.SetWitnessStance(WitnessStance.PersonFirst);
            CaseDefinition content = investigationState.CaseContent;
            if (content == null)
            {
                return;
            }
            for (int index = 0; index < content.Evidence.Count; index++)
            {
                investigationState.CollectClue(content.Evidence[index].Id);
            }
            investigationState.SetEvidencePlan(EvidencePlan.PublicRecord);
            investigationState.CompleteEvidenceReview();
            investigationState.SubmitContradiction(
                content.Contradiction.StatementId,
                content.Contradiction.EvidenceId
            );
            investigationState.TryStabilizeTimeAnchor("past-02", out _);
            investigationState.TryStabilizeTimeAnchor("present-01", out _);
            investigationState.TryStabilizeTimeAnchor("present-03", out _);
            investigationState.CompleteRepair();
            investigationState.SelectConclusion(ConclusionChoice.Preserve);
            investigationState.SetFact("teahouse-cart-testimony");
            investigationState.SetFact("teahouse-event-completed");
            investigationState.SetFact("teahouse-written-record");
            investigationState.SetFact("archive-clerk-ink-record");
            investigationState.SetFact(
                "archive-clerk-father-name"
            );
            investigationState.SetFact("archive-clerk-recruited");
            investigationState.SetFact("lu-chong-threat");
            investigationState.SetFact("warehouse-second-meal");
            investigationState.SetFact("warehouse-daughter-public");
            investigationState.SetFact(
                "warehouse-daughter-remembered"
            );
        }

        private IEnumerator CaptureEnding(string path)
        {
            yield return new WaitForSecondsRealtime(0.8f);
            yield return new WaitForEndOfFrame();
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log($"Ending preview captured to {path}");
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

        private bool IsEnding =>
            (party != null && party.IsPartyDefeated)
            || (investigationState != null
                && investigationState.IsConclusionReached
                && !endingDismissed);

        public void TogglePause()
        {
            isPaused = !isPaused;
            Time.timeScale = isPaused ? 0f : 1f;
        }

        public void ContinueAfterEnding()
        {
            if (!CanExploreAfterEnding)
            {
                return;
            }

            endingDismissed = true;
            isEndingFrozen = false;
            isPaused = false;
            Time.timeScale = 1f;
        }

        public void OpenSaveSlots()
        {
            if (!isPaused)
            {
                isPaused = true;
                Time.timeScale = 0f;
            }

            if (Demo02.Core.GameServices.Resolve<SaveSlotController>() != null)
            {
                Demo02.Core.GameServices.Resolve<SaveSlotController>().Open();
            }
        }

        public void RestartCurrentScene()
        {
            Time.timeScale = 1f;
            Scene activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(
                activeScene.buildIndex >= 0 ? activeScene.buildIndex : 0
            );
        }

        public void ReturnToTitle()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneNames.Boot);
        }
    }
}

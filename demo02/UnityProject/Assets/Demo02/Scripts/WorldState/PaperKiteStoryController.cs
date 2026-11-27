using Demo02.Core;
using Demo02.Narrative;
using Demo02.Player;
using Demo02.UI;
using UnityEngine;

namespace Demo02.WorldState
{
    public enum PaperKiteStoryStage
    {
        NotStarted,
        FindTraces,
        WindTrail,
        Knot,
        Choice,
        Completed
    }

    public sealed class PaperKiteStoryController : MonoBehaviour
    {
        public const string ContentId = "dongguan-paper-kite";
        public const string ReadyId = "dongguan-paper-kite-choice";
        public const string PresentTraceFact =
            "paper-kite-present-cord";
        public const string PastTraceFact =
            "paper-kite-past-bell";
        public const string TrailFact =
            "paper-kite-trail-secured";
        public const string PublicChoiceFact =
            "paper-kite-public-name";
        public const string PrivateChoiceFact =
            "paper-kite-private-name";
        public const string RewardItemId = "paper-kite-letter";

        [SerializeField] private Transform storytellerTarget;
        [SerializeField] private Transform presentTrace;
        [SerializeField] private Transform pastTrace;
        [SerializeField] private Transform knotTarget;
        [SerializeField] private GlideChallengeController windCourse;
        [SerializeField] private GameObject publicConsequence;
        [SerializeField] private GameObject privateConsequence;

        private InvestigationState investigationState;
        private TimeLayerController timeLayerController;
        private PlayerPartyController party;
        private WorldContentTracker contentTracker;
        private PaperKiteStoryStage cachedStage = PaperKiteStoryStage.NotStarted;
        private bool hasRefreshed;

        public static PaperKiteStoryController Instance { get; private set; }
        public Transform StorytellerTarget => storytellerTarget;
        public Transform KnotTarget => knotTarget;
        public PaperKiteStoryStage CurrentStage => ResolveStage();
        public TimeLayer CurrentLayer => timeLayerController != null
            ? timeLayerController.CurrentLayer
            : TimeLayer.Present;
        public bool HasPresentTrace =>
            investigationState != null
            && investigationState.HasFact(PresentTraceFact);
        public bool HasPastTrace =>
            investigationState != null
            && investigationState.HasFact(PastTraceFact);
        public bool HasTrail =>
            investigationState != null
            && investigationState.HasFact(TrailFact);
        public bool IsCompleted =>
            (investigationState != null
                && (investigationState.HasFact(PublicChoiceFact)
                    || investigationState.HasFact(PrivateChoiceFact)))
            || (WorldRuntime.Instance != null
                && WorldRuntime.Instance.IsWorldContentCompleted(ContentId));

        public void Configure(
            Transform storyteller,
            Transform present,
            Transform past,
            Transform knot,
            GlideChallengeController course,
            GameObject publicResult,
            GameObject privateResult)
        {
            storytellerTarget = storyteller;
            presentTrace = present;
            pastTrace = past;
            knotTarget = knot;
            windCourse = course;
            publicConsequence = publicResult;
            privateConsequence = privateResult;
        }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            investigationState =
                Demo02.Core.GameServices.Resolve<InvestigationState>();
            timeLayerController =
                Demo02.Core.GameServices.Resolve<TimeLayerController>();
            party = Demo02.Core.GameServices.Resolve<PlayerPartyController>();
            contentTracker = WorldContentTracker.Instance;
            RefreshState(force: true);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (investigationState == null)
            {
                investigationState =
                    Demo02.Core.GameServices.Resolve<InvestigationState>();
                timeLayerController =
                    Demo02.Core.GameServices.Resolve<TimeLayerController>();
                party = Demo02.Core.GameServices.Resolve<PlayerPartyController>();
                contentTracker = WorldContentTracker.Instance;
                hasRefreshed = false;
            }

            RefreshState();
        }

        private PaperKiteStoryStage ResolveStage()
        {
            if (IsCompleted)
            {
                return PaperKiteStoryStage.Completed;
            }
            if (HasTrail)
            {
                return PaperKiteStoryStage.Choice;
            }

            WorldRuntime runtime = WorldRuntime.Instance;
            if (runtime == null
                || !runtime.IsWorldContentStarted(ContentId))
            {
                return PaperKiteStoryStage.NotStarted;
            }
            if (!HasPresentTrace || !HasPastTrace)
            {
                return PaperKiteStoryStage.FindTraces;
            }

            return windCourse != null && windCourse.IsCompleted
                ? PaperKiteStoryStage.Knot
                : PaperKiteStoryStage.WindTrail;
        }

        private void RefreshState(bool force = false)
        {
            PaperKiteStoryStage stage = CurrentStage;
            if (!force && hasRefreshed && stage == cachedStage)
            {
                return;
            }

            cachedStage = stage;
            hasRefreshed = true;
            if (stage == PaperKiteStoryStage.Completed)
            {
                WorldRuntime.Instance?.MarkWorldContentCompleted(
                    ContentId
                );
            }
            ApplyConsequenceVisuals();
            contentTracker?.UpdateEntryHint(
                ContentId,
                GetStageHint(stage)
            );
        }

        private void ApplyConsequenceVisuals()
        {
            bool isPublic = investigationState != null
                && investigationState.HasFact(PublicChoiceFact);
            bool isPrivate = investigationState != null
                && investigationState.HasFact(PrivateChoiceFact);
            publicConsequence?.SetActive(isPublic);
            privateConsequence?.SetActive(isPrivate);
        }

        public string GetStorytellerPrompt()
        {
            return CurrentStage switch
            {
                PaperKiteStoryStage.NotStarted =>
                    "E  听一段纸鸢旧信",
                PaperKiteStoryStage.FindTraces =>
                    "E  询问纸鸢上的名字",
                PaperKiteStoryStage.WindTrail =>
                    "E  询问屋顶风线的去向",
                PaperKiteStoryStage.Knot
                    or PaperKiteStoryStage.Choice =>
                    "E  向说书人回报旧纸鸢",
                _ => "E  听东关纸鸢旧信的结尾"
            };
        }

        public string GetKnotPrompt()
        {
            bool pathGuideActive = party != null
                && party.ActiveIndex == 1;
            if (CurrentStage == PaperKiteStoryStage.Knot)
            {
                return pathGuideActive
                    ? "E  让途印追住旧风线"
                    : "2  切换途印后按 E 追住风线";
            }

            return party != null && party.ActiveIndex == 0
                ? "E  让修复者稳定风铃上的名字"
                : "1  切换修复者后按 E 决定名字去向";
        }

        public void InteractStoryteller()
        {
            DialogueController dialogue = DialogueController.Instance;
            if (dialogue == null || dialogue.IsOpen)
            {
                return;
            }

            switch (CurrentStage)
            {
                case PaperKiteStoryStage.NotStarted:
                    dialogue.Begin(
                        new[]
                        {
                            new DialogueLine(
                                "说书人",
                                "昨夜风大，一只断线纸鸢落在我的醒木边。线是青的，落款写着“阿绫”，可东关街的户册里没有这个人。"
                            ),
                            new DialogueLine(
                                "途印向导",
                                "纸鸢飞得再远也会认风。若它是从旧时来的，风里应该还留着回去的路。"
                            ),
                            new DialogueLine(
                                "主角",
                                "先找线头和旧风铃。也许它想让我们记住的，不只是一个名字。"
                            )
                        },
                        StartQuest,
                        storytellerTarget
                    );
                    break;
                case PaperKiteStoryStage.FindTraces:
                    dialogue.Begin(
                        new[]
                        {
                            new DialogueLine(
                                "说书人",
                                "纸鸢的线头还缠在茶肆门框上。旧风铃却只在旧时看得见，记得按 Q 换一层天。"
                            )
                        },
                        null,
                        storytellerTarget
                    );
                    break;
                case PaperKiteStoryStage.WindTrail:
                    dialogue.Begin(
                        new[]
                        {
                            new DialogueLine(
                                "说书人",
                                "两处痕对上了，剩下的路只在屋顶。街口那阵风认得纸鸢，跟着它走。"
                            )
                        },
                        null,
                        storytellerTarget
                    );
                    break;
                case PaperKiteStoryStage.Knot:
                    dialogue.Begin(
                        new[]
                        {
                            new DialogueLine(
                                "说书人",
                                "风还在航线尽头响。让途印走在前面，风线才不会从你手里滑走。"
                            )
                        },
                        null,
                        storytellerTarget
                    );
                    break;
                case PaperKiteStoryStage.Choice:
                    dialogue.Begin(
                        new[]
                        {
                            new DialogueLine(
                                "说书人",
                                "名字已经露出来了。怎么留下它，得由你决定。"
                            )
                        },
                        null,
                        storytellerTarget
                    );
                    break;
                default:
                    BeginCompletedDialogue(dialogue);
                    break;
            }
        }

        public void BeginPresentTrace()
        {
            BeginTraceDialogue(past: false);
        }

        public void BeginPastTrace()
        {
            BeginTraceDialogue(past: true);
        }

        private void BeginTraceDialogue(bool past)
        {
            if (CurrentStage != PaperKiteStoryStage.FindTraces)
            {
                return;
            }

            DialogueController dialogue = DialogueController.Instance;
            if (dialogue == null || dialogue.IsOpen)
            {
                return;
            }

            string factId = past ? PastTraceFact : PresentTraceFact;
            if (investigationState == null
                || investigationState.HasFact(factId))
            {
                return;
            }

            DialogueLine[] lines = past
                ? new[]
                {
                    new DialogueLine(
                        "风铃回声",
                        "旧时风铃内侧刻着“沈阿绫，十二岁”。铃声里混着孩子数屋顶瓦片的声音。"
                    ),
                    new DialogueLine(
                        "途印向导",
                        "她替父亲守过这只风铃。纸鸢就是从这里断线的。"
                    )
                }
                : new[]
                {
                    new DialogueLine(
                        "主角",
                        "茶肆门框的铁钉上缠着一段青线。掌柜说，这线今早才出现。"
                    ),
                    new DialogueLine(
                        "途印向导",
                        "线尾打的是旧时的结。纸鸢回来时，有人替它留了路。"
                    )
                };
            dialogue.Begin(
                lines,
                () =>
                {
                    investigationState?.SetFact(factId);
                    RefreshState(force: true);
                },
                past ? pastTrace : presentTrace
            );
        }

        public string BeginKnotInteraction()
        {
            PaperKiteStoryStage stage = CurrentStage;
            if (stage == PaperKiteStoryStage.WindTrail)
            {
                return "屋顶航线尚未完成，旧风线还没有完全显露。";
            }

            DialogueController dialogue = DialogueController.Instance;
            if (dialogue == null || dialogue.IsOpen || party == null)
            {
                return string.Empty;
            }

            if (stage == PaperKiteStoryStage.Knot)
            {
                if (party.ActiveIndex != 1)
                {
                    return "让途印向导走在前面，按 2 切换后再试。";
                }

                dialogue.Begin(
                    new[]
                    {
                        new DialogueLine(
                            "途印向导",
                            "风线还认这只纸鸢。我能把它暂时缚在风铃上，但要回记录里，还得让修复者来。"
                        ),
                        new DialogueLine(
                            "主角",
                            "那就先别松手。我们去把名字接回来。"
                        )
                    },
                    SecureTrail,
                    knotTarget
                );
                return string.Empty;
            }

            if (stage != PaperKiteStoryStage.Choice)
            {
                return string.Empty;
            }
            if (party.ActiveIndex != 0)
            {
                return "让修复者走在前面，按 1 切换后再稳定风铃。";
            }

            dialogue.BeginWithChoice(
                new[]
                {
                    new DialogueLine(
                        "修复者",
                        "纸鸢线、风铃名和街坊记忆已经叠在一起。我能把“沈阿绫”写回公开户册，也能只让东关街自己记住她。"
                    ),
                    new DialogueLine(
                        "途印向导",
                        "写回去，官府会追问这二十年；只留下，她的名字就再也不会被外人找到。"
                    )
                },
                new[]
                {
                    new DialogueChoice(
                        "把沈阿绫写回公开户册"
                    ),
                    new DialogueChoice(
                        "只让东关街坊记住这个名字"
                    )
                },
                CompleteStory,
                null,
                knotTarget,
                showChoicesImmediately: true
            );
            return string.Empty;
        }

        private void StartQuest()
        {
            if (CurrentStage != PaperKiteStoryStage.NotStarted)
            {
                return;
            }

            WorldRuntime.Instance?.MarkWorldContentStarted(ContentId);
            GameAudio.PlayConfirm();
            RefreshState(force: true);
        }

        private void SecureTrail()
        {
            investigationState?.SetFact(TrailFact);
            WorldRuntime.Instance?.MarkEncounterCompleted(ReadyId);
            GameAudio.PlayConfirm();
            RefreshState(force: true);
        }

        private void CompleteStory(int choiceIndex)
        {
            ResolveStory(choiceIndex == 0);
        }

        private void ResolveStory(bool makePublic)
        {
            if (IsCompleted || investigationState == null)
            {
                return;
            }

            investigationState.SetFact(
                makePublic ? PublicChoiceFact : PrivateChoiceFact
            );
            InventoryState inventory = InventoryState.Instance != null
                ? InventoryState.Instance
                : Demo02.Core.GameServices.Resolve<InventoryState>();
            if (inventory != null
                && inventory.GetCount(RewardItemId) <= 0)
            {
                inventory.AddItem(RewardItemId, 1);
            }
            bool hasReward = inventory != null
                && inventory.GetCount(RewardItemId) > 0;
            WorldRuntime.Instance?.MarkWorldContentCompleted(ContentId);
            string outcome = makePublic
                ? "沈阿绫重新入册，获得信物“纸鸢旧信”。"
                : "名字留在街坊记忆中，获得信物“纸鸢旧信”。";
            WorldContentEvent.AnnounceCompletion(
                "东关纸鸢旧信",
                outcome
            );
            Debug.Log(
                "Paper kite story completed: "
                + $"public={makePublic}, reward={hasReward}, "
                + $"itemCount={(inventory != null ? inventory.GetCount(RewardItemId) : 0)}."
            );
            GameAudio.PlayConfirm();
            RefreshState(force: true);
        }

        private void BeginCompletedDialogue(DialogueController dialogue)
        {
            bool isPublic = investigationState != null
                && investigationState.HasFact(PublicChoiceFact);
            dialogue.Begin(
                new[]
                {
                    new DialogueLine(
                        "说书人",
                        isPublic
                            ? "户册上重新有了沈阿绫的名字。今早还真有人说起，那个爱放纸鸢的孩子。"
                            : "官册仍是空的，可街坊已经会讲沈阿绫了。纸鸢落在谁家檐下，谁就替她把名字传下去。"
                    ),
                    new DialogueLine(
                        "说书人",
                        "你从旧时带回来的不只是一封信。下一次起风时，东关街会记得该往哪里看。"
                    )
                },
                null,
                storytellerTarget
            );
        }

        public void StartQuestFromPreview()
        {
            StartQuest();
        }

        public void PrepareChoicePreview()
        {
            StartQuest();
            investigationState ??=
                Demo02.Core.GameServices.Resolve<InvestigationState>();
            investigationState?.SetFact(PresentTraceFact);
            investigationState?.SetFact(PastTraceFact);
            investigationState?.SetFact(TrailFact);
            WorldRuntime.Instance?.MarkEncounterCompleted(ReadyId);
            party ??= Demo02.Core.GameServices.Resolve<PlayerPartyController>();
            party?.RestoreActiveCharacter(0);
            RefreshState(force: true);
        }

        public void CompletePublicChoiceFromPreview()
        {
            PrepareChoicePreview();
            ResolveStory(makePublic: true);
        }

        public string GetStageHint()
        {
            return GetStageHint(CurrentStage);
        }

        private string GetStageHint(PaperKiteStoryStage stage)
        {
            switch (stage)
            {
                case PaperKiteStoryStage.FindTraces:
                    if (!HasPresentTrace)
                    {
                        return "在茶肆门框寻找纸鸢线头";
                    }
                    return "按 Q 切换到旧时，寻找屋檐下的风铃";
                case PaperKiteStoryStage.WindTrail:
                    return "从街口风场起飞，沿屋顶航线追住风线";
                case PaperKiteStoryStage.Knot:
                    return "到航线尽头查看旧风铃，2 切换途印";
                case PaperKiteStoryStage.Choice:
                    return "1 切换修复者，在旧风铃处决定名字去向";
                case PaperKiteStoryStage.Completed:
                    return investigationState != null
                        && investigationState.HasFact(PublicChoiceFact)
                        ? "沈阿绫已写回公开户册"
                        : "沈阿绫留在东关街坊记忆中";
                default:
                    return "与东关街说书人交谈";
            }
        }

        private Transform GetCurrentTarget()
        {
            switch (CurrentStage)
            {
                case PaperKiteStoryStage.FindTraces:
                    if (CurrentLayer == TimeLayer.Present
                        && !HasPresentTrace)
                    {
                        return presentTrace;
                    }
                    if (CurrentLayer == TimeLayer.Past
                        && !HasPastTrace)
                    {
                        return pastTrace;
                    }
                    return !HasPresentTrace
                        ? presentTrace
                        : pastTrace;
                case PaperKiteStoryStage.WindTrail:
                    return windCourse != null
                        ? windCourse.transform
                        : knotTarget;
                case PaperKiteStoryStage.Knot:
                case PaperKiteStoryStage.Choice:
                    return knotTarget;
                default:
                    return storytellerTarget;
            }
        }

        private void OnGUI()
        {
            PaperKiteStoryStage stage = CurrentStage;
            if (stage == PaperKiteStoryStage.NotStarted
                || stage == PaperKiteStoryStage.Completed
                || GameplayUiState.IsGameplayBlocked
                || party == null
                || party.ActiveCharacter == null)
            {
                return;
            }

            Transform target = GetCurrentTarget();
            string direction = string.Empty;
            float distance = 0f;
            Camera camera = Camera.main;
            if (target != null && camera != null)
            {
                Vector3 offset =
                    target.position - party.ActiveCharacter.position;
                offset.y = 0f;
                distance = offset.magnitude;
                if (distance > 0.1f)
                {
                    Vector3 flatForward = camera.transform.forward;
                    flatForward.y = 0f;
                    float angle = Vector3.SignedAngle(
                        flatForward,
                        offset.normalized,
                        Vector3.up
                    );
                    direction = GetDirectionLabel(angle);
                }
            }

            float scale = UiTheme.Scale;
            Rect panel = new(
                20f * scale,
                126f * scale,
                448f * scale,
                66f * scale
            );
            UiTheme.DrawRect(
                panel,
                new Color(0.025f, 0.035f, 0.05f, 0.94f)
            );
            UiTheme.DrawBorder(
                panel,
                new Color(0.42f, 0.82f, 0.72f, 0.62f)
            );
            UiTheme.DrawRect(
                new Rect(
                    panel.x,
                    panel.y,
                    4f * scale,
                    panel.height
                ),
                new Color(0.42f, 0.82f, 0.72f)
            );

            GUIStyle titleStyle = UiTheme.Label(
                12,
                TextAnchor.MiddleLeft,
                bold: true
            );
            titleStyle.normal.textColor =
                new Color(0.62f, 0.92f, 0.8f);
            GUI.Label(
                new Rect(
                    panel.x + 18f * scale,
                    panel.y + 5f * scale,
                    panel.width - 34f * scale,
                    24f * scale
                ),
                "支线 · 东关纸鸢旧信",
                titleStyle
            );
            string location = target != null
                ? $"{direction} {distance:0.#}m"
                : string.Empty;
            GUI.Label(
                new Rect(
                    panel.x + 18f * scale,
                    panel.y + 29f * scale,
                    panel.width - 34f * scale,
                    30f * scale
                ),
                $"{GetStageHint(stage)}    {location}",
                UiTheme.Label(13)
            );
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

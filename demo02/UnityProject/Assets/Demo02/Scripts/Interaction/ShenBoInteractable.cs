using System.Collections;
using System.IO;
using Demo02.Narrative;
using UnityEngine;

namespace Demo02.Interaction
{
    public sealed class ShenBoInteractable : MonoBehaviour, IInteractable
    {
        private InvestigationState investigationState;

        public bool CanInteract => true;

        public string Prompt
        {
            get
            {
                if (investigationState == null)
                {
                    return "E  与守仓人交谈";
                }

                return investigationState.Stage switch
                {
                    ChapterStage.Arrival => "E  询问沈砚失踪的事",
                    ChapterStage.Investigate => "E  询问沈砚最后留下的记录",
                    ChapterStage.ReviewEvidence => "E  把三处记录交给沈伯辨认",
                    ChapterStage.StabilizeAnchors => "E  询问如何稳定时间锚点",
                    ChapterStage.RepairRift => "E  询问街尾被撕开的记录",
                    ChapterStage.FinalDecision => "E  请沈伯说出他的选择",
                    _ => "E  与沈伯告别"
                };
            }
        }

        private void Start()
        {
            investigationState = FindAnyObjectByType<InvestigationState>();
            if (System.Array.IndexOf(
                System.Environment.GetCommandLineArgs(),
                "--preview-choice"
            ) >= 0)
            {
                StartCoroutine(CaptureChoicePreview());
            }
        }

        private IEnumerator CaptureChoicePreview()
        {
            yield return new WaitForSecondsRealtime(0.4f);
            DialogueController dialogue = FindAnyObjectByType<DialogueController>();
            if (dialogue == null)
            {
                yield break;
            }

            dialogue.BeginWithChoice(
                new[]
                {
                    new DialogueLine(
                        "守仓人 沈伯",
                        "你说要查清阿砚的事。可若证据和活人只能先保住一个，你会怎么选？"
                    )
                },
                new[]
                {
                    new DialogueChoice("我按记录办事。证据齐了，才有资格谈救人。"),
                    new DialogueChoice("如果沈砚还活着，我会先把他带回来。")
                },
                _ => { },
                cameraFocus: transform,
                showChoicesImmediately: true
            );

            string path = GetArgumentValue("--capture-choice=");
            if (string.IsNullOrWhiteSpace(path))
            {
                yield break;
            }

            yield return new WaitForSecondsRealtime(0.5f);
            yield return new WaitForEndOfFrame();
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            ScreenCapture.CaptureScreenshot(path);
            Debug.Log($"Dialogue choice preview captured to {path}");
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

        public string Interact(PlayerInteractor interactor)
        {
            DialogueController dialogue = FindAnyObjectByType<DialogueController>();
            if (dialogue == null || investigationState == null)
            {
                return "沈伯似乎已经听不见你的声音。";
            }

            switch (investigationState.Stage)
            {
                case ChapterStage.Arrival:
                    BeginBriefing(dialogue);
                    break;
                case ChapterStage.Investigate:
                    BeginInvestigationReminder(dialogue);
                    break;
                case ChapterStage.ReviewEvidence:
                    BeginEvidenceReview(dialogue);
                    break;
                case ChapterStage.StabilizeAnchors:
                    BeginAnchorGuidance(dialogue);
                    break;
                case ChapterStage.RepairRift:
                    BeginRepairGuidance(dialogue);
                    break;
                case ChapterStage.FinalDecision:
                    BeginFinalChoice(dialogue);
                    break;
                default:
                    BeginEpilogue(dialogue);
                    break;
            }

            return string.Empty;
        }

        private void BeginBriefing(DialogueController dialogue)
        {
            dialogue.BeginWithChoice(
                new[]
                {
                    new DialogueLine(
                        "守仓人 沈伯",
                        "你也是为那条不存在的街来的？今早我去官署报官，他们说我从来没有儿子。"
                    ),
                    new DialogueLine(
                        "主角",
                        "官署收到一批盐引，交货地点写着‘盐引巷’。我在广陵地图上找不到这个名字。"
                    ),
                    new DialogueLine(
                        "守仓人 沈伯",
                        "盐引巷就是这条旧街从前的名字。我儿子沈砚守着盐仓账。昨夜他带走了七页账簿，再没回来。"
                    ),
                    new DialogueLine(
                        "主角",
                        "带我去看他最后留下的东西。旧街招牌、盐仓账牌、官府封条——三处记录若指向同一个人，就能把他找回来。"
                    )
                },
                new[]
                {
                    new DialogueChoice("我按记录办事。证据齐了，才有资格谈救人。"),
                    new DialogueChoice("如果沈砚还活着，我会先把他带回来。")
                },
                index => investigationState.SetWitnessStance(
                    index == 0
                        ? WitnessStance.EvidenceFirst
                        : WitnessStance.PersonFirst
                ),
                () => investigationState.CompleteWitnessBriefing(),
                transform
            );
        }

        private void BeginInvestigationReminder(DialogueController dialogue)
        {
            string response = investigationState.Stance == WitnessStance.PersonFirst
                ? "你肯先想着阿砚，我就把知道的都告诉你。后墙上的旧漆、账台的背面，还有官署那枚封条，都别漏。"
                : "你说得对，先让三张纸彼此印证。官署不会信一个父亲的话，但会怕一份完整的账。";
            dialogue.Begin(new[]
            {
                new DialogueLine("守仓人 沈伯", response),
                new DialogueLine(
                    "守仓人 沈伯",
                    "沈砚不会无缘无故带走账页。去找旧街招牌、盐仓账牌和官府封条。三张纸放在一起，才有人肯信。"
                )
            }, cameraFocus: transform);
        }

        private void BeginEvidenceReview(DialogueController dialogue)
        {
            dialogue.BeginWithChoice(
                new[]
                {
                    new DialogueLine(
                        "守仓人 沈伯",
                        "这三笔……都是阿砚的字。七车盐从盐引巷入城，复核签字也是他。"
                    ),
                    new DialogueLine(
                        "主角",
                        "封条是转运副使陆崇的，日期却在旧街消失前一天。他不是事后掩盖，而是提前写好了结局。"
                    ),
                    new DialogueLine(
                        "守仓人 沈伯",
                        "所以阿砚不是逃了。是有人把私盐的账、盐引巷，还有知情的阿砚一起从纸上抹掉了。"
                    ),
                    new DialogueLine(
                        "主角",
                        "这些证据让时间锚点显形了。我会把那条被撕掉的记录重新钉回现在。"
                    )
                },
                new[]
                {
                    new DialogueChoice("把复制记录送入官署，让所有人都看见。"),
                    new DialogueChoice("先藏住证据，保护沈伯和剩余证人。")
                },
                index => investigationState.SetEvidencePlan(
                    index == 0
                        ? EvidencePlan.PublicRecord
                        : EvidencePlan.PrivateProtection
                ),
                () => investigationState.CompleteEvidenceReview(),
                transform
            );
        }

        private void BeginAnchorGuidance(DialogueController dialogue)
        {
            string response = investigationState.Stance == WitnessStance.PersonFirst
                ? "阿砚若还听得见，他会记得你刚才的话。去把那些门一扇扇找出来。"
                : "先把旧时的路钉住。只有亲眼看见那一夜，你才知道这案子究竟被谁写下。";
            dialogue.Begin(new[]
            {
                new DialogueLine("守仓人 沈伯", response),
                new DialogueLine(
                    "守仓人 沈伯",
                    "阿砚小时候说，盐仓的火光照到旧墙上，会多出一排本不存在的门。"
                ),
                new DialogueLine(
                    "主角",
                    "那是旧时层留下的影子。门牌上写着“西仓、东验、中册”。按这个顺序稳定锚点，错序会让已经稳定的部分重新漂移。"
                )
            }, cameraFocus: transform);
        }

        private void BeginRepairGuidance(DialogueController dialogue)
        {
            string response = investigationState.Plan == EvidencePlan.PrivateProtection
                ? "你没把账页立刻交出去，陆崇的人就还不知道我们已经走到哪一步。小心些。"
                : "官署那边迟早会听见风声。你既然选择了公开，就不能只拿到一半真相。";
            dialogue.Begin(new[]
            {
                new DialogueLine("守仓人 沈伯", response),
                new DialogueLine(
                    "守仓人 沈伯",
                    "锚点安静下来了。可街尾那块地方……像被人从纸上硬生生撕开。"
                ),
                new DialogueLine(
                    "主角",
                    "沈砚的名字还在裂缝另一侧。压下时缝，我就能看见最后那一页。"
                )
            }, cameraFocus: transform);
        }

        private void BeginFinalChoice(DialogueController dialogue)
        {
            string response = investigationState.Stance == WitnessStance.PersonFirst
                ? "你从一开始就说会先救阿砚。现在轮到你决定，是让他回来，还是让整条街继续安静下去。"
                : "你说过记录要先说话。现在记录已经说完了，剩下的不是证据，是人的选择。";
            dialogue.Begin(new[]
            {
                new DialogueLine("守仓人 沈伯", response),
                new DialogueLine(
                    "守仓人 沈伯",
                    "我已经很多年没听见有人叫阿砚了。你若恢复盐引巷，陆崇的人会知道；你若封存这里，阿砚也许再也回不来。"
                ),
                new DialogueLine(
                    "主角",
                    "记录不会替我们选。要保住一个人、一条街，总得有人承担它重新出现后的代价。"
                )
            }, cameraFocus: transform);
        }

        private void BeginEpilogue(DialogueController dialogue)
        {
            bool preserved =
                investigationState.SelectedConclusion == ConclusionChoice.Preserve;
            dialogue.Begin(new[]
            {
                new DialogueLine(
                    "守仓人 沈伯",
                    preserved
                        ? investigationState.Plan == EvidencePlan.PublicRecord
                            ? "门口的盐引旗升起来了。你把账页送进官署，这回整条街都听得见阿砚的算盘。"
                            : "门口的盐引旗升起来了。账页仍藏在暗处，但至少阿砚的名字回到了街上。"
                        : investigationState.Stance == WitnessStance.PersonFirst
                            ? "街口又空了。我知道你已经尽力，可阿砚还是没能回来。至少别忘记他。"
                            : "街口又空了。记录稳定了，阿砚却仍不在册。或许这就是只相信纸张的代价。"
                )
            }, cameraFocus: transform);
        }
    }
}

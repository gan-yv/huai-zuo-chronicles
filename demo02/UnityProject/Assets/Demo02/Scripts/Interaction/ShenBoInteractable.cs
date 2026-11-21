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
            dialogue.Begin(
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
                () => investigationState.CompleteWitnessBriefing()
            );
        }

        private static void BeginInvestigationReminder(DialogueController dialogue)
        {
            dialogue.Begin(new[]
            {
                new DialogueLine(
                    "守仓人 沈伯",
                    "沈砚不会无缘无故带走账页。去找旧街招牌、盐仓账牌和官府封条。三张纸放在一起，才有人肯信。"
                )
            });
        }

        private void BeginEvidenceReview(DialogueController dialogue)
        {
            dialogue.Begin(
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
                () => investigationState.CompleteEvidenceReview()
            );
        }

        private static void BeginAnchorGuidance(DialogueController dialogue)
        {
            dialogue.Begin(new[]
            {
                new DialogueLine(
                    "守仓人 沈伯",
                    "阿砚小时候说，盐仓的火光照到旧墙上，会多出一排本不存在的门。"
                ),
                new DialogueLine(
                    "主角",
                    "那是旧时层留下的影子。按 Q 在现世与旧时之间切换，找到三处漂移的时间锚点。"
                )
            });
        }

        private static void BeginRepairGuidance(DialogueController dialogue)
        {
            dialogue.Begin(new[]
            {
                new DialogueLine(
                    "守仓人 沈伯",
                    "锚点安静下来了。可街尾那块地方……像被人从纸上硬生生撕开。"
                ),
                new DialogueLine(
                    "主角",
                    "沈砚的名字还在裂缝另一侧。压下时缝，我就能看见最后那一页。"
                )
            });
        }

        private static void BeginFinalChoice(DialogueController dialogue)
        {
            dialogue.Begin(new[]
            {
                new DialogueLine(
                    "守仓人 沈伯",
                    "我已经很多年没听见有人叫阿砚了。你若恢复盐引巷，陆崇的人会知道；你若封存这里，阿砚也许再也回不来。"
                ),
                new DialogueLine(
                    "主角",
                    "记录不会替我们选。要保住一个人、一条街，总得有人承担它重新出现后的代价。"
                )
            });
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
                        ? "门口的盐引旗升起来了。阿砚的算盘还在响——这回，整条街都听得见。"
                        : "街口又空了。可我知道阿砚的名字还在你的修复记录里。只要有人记得，就还不算真正消失。"
                )
            });
        }
    }
}

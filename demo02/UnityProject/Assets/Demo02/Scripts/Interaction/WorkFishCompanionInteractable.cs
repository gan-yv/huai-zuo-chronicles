using Demo02.Narrative;
using Demo02.Player;
using Demo02.WorldState;
using UnityEngine;

namespace Demo02.Interaction
{
    public sealed class WorkFishCompanionInteractable :
        MonoBehaviour,
        IInteractable
    {
        private static readonly DialogueLine[][] PresentChats =
        {
            new[]
            {
                new DialogueLine(
                    "阿鱼",
                    "我会跟着你。打起来的时候站远一点，"
                    + "看到什么再告诉你。"
                ),
                new DialogueLine(
                    "阿鱼",
                    "东关街的风比刚才轻。说书人那边"
                    + "或许还有一只纸鸢没讲完。"
                )
            },
            new[]
            {
                new DialogueLine(
                    "阿鱼",
                    "檐下那串风铃一直在响。要是发现新的动静，"
                    + "我会先晃一下尾巴提醒你。"
                )
            },
            new[]
            {
                new DialogueLine(
                    "阿鱼",
                    "你尽管走。我认得这条路，也认得出"
                    + "哪些影子是旧时留下来的。"
                )
            }
        };

        private static readonly DialogueLine[][] PastChats =
        {
            new[]
            {
                new DialogueLine(
                    "阿鱼",
                    "旧时的屋檐更高，风会从瓦沟里钻下来。"
                    + "纸鸢要回来，大概会先碰最外侧的铃。"
                ),
                new DialogueLine(
                    "阿鱼",
                    "我会跟着你。真打起来，我就站远一点。"
                )
            },
            new[]
            {
                new DialogueLine(
                    "阿鱼",
                    "旧时的灯都压在雾里，人声却更清楚。"
                    + "往石板缝里看，也许能找到失去的日期。"
                )
            },
            new[]
            {
                new DialogueLine(
                    "阿鱼",
                    "这里的水汽黏在鳞片上。别担心，"
                    + "我游得慢一些，也会跟紧。"
                )
            }
        };

        [SerializeField] private WorkFishCompanion companion;
        private int chatIndex;

        public bool CanInteract =>
            companion != null && !companion.IsInCombat;
        public string Prompt => "E  和阿鱼说说话";

        public void Configure(WorkFishCompanion follower)
        {
            companion = follower;
        }

        public string Interact(PlayerInteractor interactor)
        {
            DialogueController dialogue = DialogueController.Instance;
            if (companion == null
                || companion.IsInCombat
                || dialogue == null
                || dialogue.IsOpen)
            {
                return string.Empty;
            }

            TimeLayerController timeLayer =
                Demo02.Core.GameServices.Resolve<TimeLayerController>();
            DialogueLine[][] chats =
                timeLayer != null
                    && timeLayer.CurrentLayer == TimeLayer.Past
                    ? PastChats
                    : PresentChats;
            DialogueLine[] lines = chats[chatIndex % chats.Length];
            chatIndex = (chatIndex + 1) % chats.Length;
            dialogue.Begin(lines, null, transform);
            return string.Empty;
        }
    }
}

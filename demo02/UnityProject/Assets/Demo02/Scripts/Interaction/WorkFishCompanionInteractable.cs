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
        [SerializeField] private WorkFishCompanion companion;

        public bool CanInteract => companion != null;
        public string Prompt => "E  和阿鱼说说话";

        public void Configure(WorkFishCompanion follower)
        {
            companion = follower;
        }

        public string Interact(PlayerInteractor interactor)
        {
            DialogueController dialogue = DialogueController.Instance;
            if (dialogue == null || dialogue.IsOpen)
            {
                return string.Empty;
            }

            TimeLayerController timeLayer =
                FindAnyObjectByType<TimeLayerController>();
            DialogueLine[] lines =
                timeLayer != null
                    && timeLayer.CurrentLayer == TimeLayer.Past
                    ? new[]
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
                    }
                    : new[]
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
                    };
            dialogue.Begin(lines, null, transform);
            return string.Empty;
        }
    }
}

using Demo02.Narrative;
using Demo02.WorldState;
using UnityEngine;

namespace Demo02.Interaction
{
    public sealed class WorldStoryNpcInteractable :
        MonoBehaviour,
        IInteractable
    {
        [SerializeField] private string speakerName = "街坊";
        [SerializeField] private WorldContentEvent contentEvent;
        [SerializeField] private DialogueLine[] introLines =
            new DialogueLine[0];
        [SerializeField] private DialogueLine[] readyLines =
            new DialogueLine[0];
        [SerializeField] private DialogueLine[] completedLines =
            new DialogueLine[0];

        public bool CanInteract => true;
        public string Prompt => contentEvent != null
            && contentEvent.IsReadyToResolve
            && !contentEvent.IsCompleted
                ? $"E  向{speakerName}交付巡哨线索"
                : $"E  与{speakerName}交谈";

        public void Configure(
            string displayName,
            WorldContentEvent storyEvent,
            DialogueLine[] introduction,
            DialogueLine[] rewardDialogue,
            DialogueLine[] finishedDialogue)
        {
            speakerName = displayName;
            contentEvent = storyEvent;
            introLines = introduction ?? new DialogueLine[0];
            readyLines = rewardDialogue ?? new DialogueLine[0];
            completedLines = finishedDialogue ?? new DialogueLine[0];
        }

        public string Interact(PlayerInteractor interactor)
        {
            DialogueController controller = Demo02.Core.GameServices.Resolve<DialogueController>();
            if (controller == null)
            {
                return "对话系统尚未初始化。";
            }

            if (contentEvent == null)
            {
                controller.Begin(introLines, null, transform);
            }
            else if (contentEvent.IsCompleted)
            {
                controller.Begin(completedLines, null, transform);
            }
            else if (contentEvent.IsReadyToResolve)
            {
                controller.Begin(
                    readyLines,
                    () =>
                    {
                        if (contentEvent.TryCompleteReward(
                                out string message
                            ))
                        {
                            Debug.Log(message);
                        }
                    },
                    transform
                );
            }
            else
            {
                controller.Begin(
                    introLines,
                    contentEvent != null ? contentEvent.Discover : null,
                    transform
                );
            }
            return string.Empty;
        }
    }
}

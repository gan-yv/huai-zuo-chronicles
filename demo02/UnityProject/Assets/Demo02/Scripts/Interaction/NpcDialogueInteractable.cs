using Demo02.Narrative;
using UnityEngine;

namespace Demo02.Interaction
{
    public sealed class NpcDialogueInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string speakerName = "居民";
        [SerializeField] private string interactionPrompt = "E  与居民交谈";
        [SerializeField] private DialogueLine[] lines = new DialogueLine[0];

        public bool CanInteract => true;
        public string Prompt => interactionPrompt;

        public void Configure(
            string speaker,
            string prompt,
            params DialogueLine[] dialogueLines)
        {
            speakerName = speaker;
            interactionPrompt = prompt;
            lines = dialogueLines;
        }

        public string Interact(PlayerInteractor interactor)
        {
            DialogueController controller = FindAnyObjectByType<DialogueController>();
            if (controller == null)
            {
                return "对话系统尚未初始化。";
            }

            controller.Begin(lines, null, transform);
            return string.Empty;
        }
    }
}

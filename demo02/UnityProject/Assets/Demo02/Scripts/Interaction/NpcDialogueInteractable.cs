using Demo02.Narrative;
using UnityEngine;

namespace Demo02.Interaction
{
    public sealed class NpcDialogueInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string speakerName = "居民";
        [SerializeField] private string interactionPrompt = "E  与居民交谈";
        [SerializeField] private string dialogueNpcId;
        [SerializeField] private DialogueLine[] lines = new DialogueLine[0];

        private InvestigationState investigationState;

        public bool CanInteract => true;
        public string Prompt
        {
            get
            {
                DialogueScriptDefinition script =
                    investigationState?.CaseContent?.FindDialogueScript(
                        dialogueNpcId,
                        investigationState
                    );
                return script != null
                    ? script.InteractionPrompt
                    : interactionPrompt;
            }
        }

        private void Start()
        {
            investigationState = FindAnyObjectByType<InvestigationState>();
        }

        public void Configure(
            string speaker,
            string prompt,
            params DialogueLine[] dialogueLines)
        {
            speakerName = speaker;
            interactionPrompt = prompt;
            lines = dialogueLines;
            dialogueNpcId = string.Empty;
        }

        public void ConfigureData(string npcId)
        {
            dialogueNpcId = npcId;
        }

        public string Interact(PlayerInteractor interactor)
        {
            DialogueController controller = FindAnyObjectByType<DialogueController>();
            if (controller == null)
            {
                return "对话系统尚未初始化。";
            }

            if (investigationState != null
                && !string.IsNullOrWhiteSpace(dialogueNpcId)
                && DialogueContentRuntime.TryPlay(
                    investigationState.CaseContent,
                    investigationState,
                    controller,
                    dialogueNpcId,
                    transform
                ))
            {
                return string.Empty;
            }

            controller.Begin(lines, null, transform);
            return string.Empty;
        }
    }
}

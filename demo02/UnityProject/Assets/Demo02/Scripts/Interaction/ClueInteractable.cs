using UnityEngine;
using Demo02.Narrative;

namespace Demo02.Interaction
{
    public sealed class ClueInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string clueId = "old-street-sign";
        [SerializeField] private string prompt = "E  调查旧街招牌";
        [SerializeField, TextArea(2, 5)]
        private string clueText = "招牌背面留着被刮掉的旧街名。有人不希望这条街继续存在。";

        private bool hasBeenRead;
        private InvestigationState investigationState;

        public string Prompt => hasBeenRead ? "E  再看一次旧街招牌" : prompt;
        public bool CanInteract => true;

        private void Start()
        {
            investigationState = FindAnyObjectByType<InvestigationState>();
        }

        public void Configure(string id, string interactionPrompt, string discoveredText)
        {
            clueId = id;
            prompt = interactionPrompt;
            clueText = discoveredText;
        }

        public string Interact(PlayerInteractor interactor)
        {
            hasBeenRead = true;
            investigationState?.CollectClue(clueId);
            return clueText;
        }
    }
}

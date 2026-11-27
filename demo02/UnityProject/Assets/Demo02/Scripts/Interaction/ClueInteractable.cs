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

        public string ClueId => clueId;
        public string Prompt
        {
            get
            {
                if (investigationState != null && !investigationState.CanInvestigateClues)
                {
                    return "E  调查记录（先询问守仓人沈伯）";
                }

                return hasBeenRead ? "E  重新查看这条记录" : prompt;
            }
        }
        public bool CanInteract => true;

        private void Start()
        {
            investigationState = Demo02.Core.GameServices.Resolve<InvestigationState>();
        }

        public void Configure(string id, string interactionPrompt, string discoveredText)
        {
            clueId = id;
            prompt = interactionPrompt;
            clueText = discoveredText;
        }

        public string Interact(PlayerInteractor interactor)
        {
            if (investigationState != null && !investigationState.CanInvestigateClues)
            {
                return "这条记录与一名失踪的账房有关。先去旧仓内院询问沈伯。";
            }

            hasBeenRead = true;
            investigationState?.CollectClue(clueId);
            return clueText;
        }
    }
}

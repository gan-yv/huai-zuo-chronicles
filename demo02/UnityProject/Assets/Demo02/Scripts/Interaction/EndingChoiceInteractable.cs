using Demo02.Narrative;
using UnityEngine;

namespace Demo02.Interaction
{
    public sealed class EndingChoiceInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private ConclusionChoice choice;
        [SerializeField] private GameObject preserveOutcome;
        [SerializeField] private GameObject sealOutcome;

        private InvestigationState investigationState;

        public bool CanInteract =>
            investigationState != null
            && investigationState.IsRepaired
            && !investigationState.IsConclusionReached;

        public string Prompt => choice == ConclusionChoice.Preserve
            ? "E  保住旧街"
            : "E  封存旧街";

        private void Start()
        {
            investigationState = FindAnyObjectByType<InvestigationState>();
        }

        public void Configure(
            ConclusionChoice conclusion,
            GameObject preserveVisual,
            GameObject sealVisual)
        {
            choice = conclusion;
            preserveOutcome = preserveVisual;
            sealOutcome = sealVisual;
        }

        public string Interact(PlayerInteractor interactor)
        {
            if (investigationState == null)
            {
                return "主角无法判断这条记录应当如何保存。";
            }

            if (!investigationState.IsRepaired)
            {
                return "必须先稳定街尾的时空接点。";
            }

            if (!investigationState.SelectConclusion(choice))
            {
                return "旧街的命运已经决定。";
            }

            preserveOutcome?.SetActive(choice == ConclusionChoice.Preserve);
            sealOutcome?.SetActive(choice == ConclusionChoice.Seal);

            return choice == ConclusionChoice.Preserve
                ? "旧街居民重新出现在城市记录中。官府必须承认这片街区。"
                : "旧街从现存记录中消失，街尾被新的围墙封死。";
        }
    }
}

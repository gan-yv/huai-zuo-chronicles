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
            ? "E  恢复盐引巷与沈砚的名字"
            : "E  封存盐引巷，维持现存记录";

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
                ? "沈砚的名字重新出现在盐仓账册上。盐引巷回到广陵地图，但陆崇背后的势力也看见了修复留下的光。"
                : "盐引巷再次从地图上褪去。沈砚的名字没有回来，只在你手中的修复记录里保留了一行。";
        }
    }
}

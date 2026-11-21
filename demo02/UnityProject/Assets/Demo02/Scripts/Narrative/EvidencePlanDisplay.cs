using UnityEngine;

namespace Demo02.Narrative
{
    [DefaultExecutionOrder(30)]
    public sealed class EvidencePlanDisplay : MonoBehaviour
    {
        [SerializeField] private GameObject publicRecordVisual;
        [SerializeField] private GameObject privateProtectionVisual;

        private InvestigationState investigationState;
        private EvidencePlan lastPlan = EvidencePlan.Undecided;

        private void Start()
        {
            investigationState = FindAnyObjectByType<InvestigationState>();
            Refresh();
        }

        public void Configure(GameObject publicVisual, GameObject privateVisual)
        {
            publicRecordVisual = publicVisual;
            privateProtectionVisual = privateVisual;
            Refresh();
        }

        private void Update()
        {
            if (investigationState != null && investigationState.Plan != lastPlan)
            {
                Refresh();
            }
        }

        private void Refresh()
        {
            EvidencePlan plan = investigationState != null
                ? investigationState.Plan
                : EvidencePlan.Undecided;
            lastPlan = plan;
            publicRecordVisual?.SetActive(plan == EvidencePlan.PublicRecord);
            privateProtectionVisual?.SetActive(plan == EvidencePlan.PrivateProtection);
        }
    }
}

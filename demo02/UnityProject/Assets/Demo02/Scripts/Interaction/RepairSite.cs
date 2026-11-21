using Demo02.Combat;
using Demo02.Narrative;
using UnityEngine;

namespace Demo02.Interaction
{
    public sealed class RepairSite : MonoBehaviour, IInteractable
    {
        private InvestigationState investigationState;
        private EncounterState encounterState;

        public string Prompt
        {
            get
            {
                if (!CanInteract)
                {
                    return "时缝已经暂时稳定";
                }

                if (encounterState != null && !encounterState.IsCleared)
                {
                    return $"E  清除附近巡逻队  ({encounterState.AliveEnemyCount}/{encounterState.TotalEnemyCount})";
                }

                return investigationState != null && investigationState.HasAllClues
                    ? "E  稳定时空接点"
                    : "E  检查时空接点";
            }
        }

        public bool CanInteract => investigationState == null || !investigationState.IsRepaired;

        private void Start()
        {
            investigationState = FindAnyObjectByType<InvestigationState>();
            encounterState = FindAnyObjectByType<EncounterState>();
        }

        public string Interact(PlayerInteractor interactor)
        {
            if (investigationState == null)
            {
                return "修复工具没有检测到可用的记录。";
            }

            if (!investigationState.HasAllClues)
            {
                return $"记录不足。还需要 {investigationState.RequiredClueCount - investigationState.CollectedClueCount} 条线索。";
            }

            if (encounterState != null && !encounterState.IsCleared)
            {
                return "巡逻队仍在附近，修复工具无法稳定暴露的接点。";
            }

            if (!investigationState.CompleteRepair())
            {
                return "时空接点没有产生新的反应。";
            }

            transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);
            return "三条记录相互印证。主角将时缝暂时压回旧街之下，城市记录重新稳定。";
        }
    }
}

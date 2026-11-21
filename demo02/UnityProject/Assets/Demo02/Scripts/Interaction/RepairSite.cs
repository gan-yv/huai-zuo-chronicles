using Demo02.Narrative;
using UnityEngine;

namespace Demo02.Interaction
{
    public sealed class RepairSite : MonoBehaviour, IInteractable
    {
        private InvestigationState investigationState;

        public string Prompt
        {
            get
            {
                if (!CanInteract)
                {
                    return "时缝已经暂时稳定";
                }

                if (investigationState != null && !investigationState.HasAllTimeAnchors)
                {
                    return $"E  检查被撕开的记录（锚点 {investigationState.StabilizedTimeAnchorCount}/{investigationState.RequiredTimeAnchorCount}）";
                }

                return investigationState != null && investigationState.HasAllClues
                    ? "E  压下时缝，读取被抹去的一页"
                    : "E  检查被撕开的记录";
            }
        }

        public bool CanInteract => investigationState == null || !investigationState.IsRepaired;

        private void Start()
        {
            investigationState = FindAnyObjectByType<InvestigationState>();
        }

        public string Interact(PlayerInteractor interactor)
        {
            if (investigationState == null)
            {
                return "修复工具没有检测到盐引巷残留的记录。";
            }

            if (!investigationState.HasAllClues)
            {
                return $"证据链仍缺 {investigationState.RequiredClueCount - investigationState.CollectedClueCount} 条记录。";
            }

            if (!investigationState.HasAllTimeAnchors)
            {
                int remaining = investigationState.RequiredTimeAnchorCount
                    - investigationState.StabilizedTimeAnchorCount;
                return $"仍有 {remaining} 个时间锚点漂移。按 Q 切换时间层完成校正。";
            }

            if (!investigationState.CompleteRepair())
            {
                return "时空接点没有产生新的反应。";
            }

            transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);
            return "三处记录相互印证：陆崇为掩盖私盐，将整条盐引巷和账房沈砚从官册中抹去。时缝暂时平息，但被撕掉的那一页仍等你决定。";
        }
    }
}

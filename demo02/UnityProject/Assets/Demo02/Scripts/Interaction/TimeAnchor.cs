using Demo02.Narrative;
using UnityEngine;

namespace Demo02.Interaction
{
    public sealed class TimeAnchor : MonoBehaviour, IInteractable
    {
        [SerializeField] private string anchorId = "time-anchor";

        private InvestigationState investigationState;

        public string AnchorId => anchorId;
        public bool CanInteract =>
            investigationState == null || !investigationState.HasStabilizedAnchor(anchorId);

        public string Prompt
        {
            get
            {
                if (!CanInteract)
                {
                    return "时间锚点已经稳定";
                }

                if (investigationState == null || !investigationState.HasAllClues)
                {
                    return "E  检查时间锚点（记录不足）";
                }

                return "E  稳定时间锚点";
            }
        }

        private void Start()
        {
            investigationState = FindAnyObjectByType<InvestigationState>();
        }

        public void Configure(string id)
        {
            anchorId = id;
        }

        public string Interact(PlayerInteractor interactor)
        {
            if (investigationState == null)
            {
                return "修复工具没有检测到可用的时间锚点。";
            }

            if (!investigationState.HasAllClues)
            {
                return "现有记录不足以校正这个锚点。先调查街区并稳定其他锚点。";
            }

            if (!investigationState.StabilizeTimeAnchor(anchorId))
            {
                return "这个时间锚点已经稳定。";
            }

            return "时间锚点停止漂移，现世与旧时的边界重新清晰。";
        }
    }
}

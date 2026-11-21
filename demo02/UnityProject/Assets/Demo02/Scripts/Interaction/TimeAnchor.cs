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

                if (investigationState == null || !investigationState.HasReviewedEvidence)
                {
                    return "E  检查时间锚点（证据链尚未完成）";
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

            if (!investigationState.HasReviewedEvidence)
            {
                return "这些记录还没有形成完整证据链。先回旧仓找沈伯辨认笔迹。";
            }

            if (!investigationState.StabilizeTimeAnchor(anchorId))
            {
                return "这个时间锚点已经稳定。";
            }

            return "时间锚点停止漂移。那一夜盐仓的火光在旧时层中重新显现。";
        }
    }
}

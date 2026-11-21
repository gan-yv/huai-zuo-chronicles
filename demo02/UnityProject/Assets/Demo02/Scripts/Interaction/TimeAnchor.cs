using Demo02.Narrative;
using UnityEngine;

namespace Demo02.Interaction
{
    public sealed class TimeAnchor : MonoBehaviour, IInteractable
    {
        [SerializeField] private string anchorId = "time-anchor";
        [SerializeField] private string anchorLabel = "时间锚点";

        private InvestigationState investigationState;

        public string AnchorId => anchorId;
        public bool CanInteract =>
            investigationState == null || !investigationState.HasStabilizedAnchor(anchorId);
        public string AnchorLabel => anchorLabel;

        public string Prompt
        {
            get
            {
                if (!CanInteract)
                {
                    return $"{anchorLabel}已经稳定";
                }

                if (investigationState == null || !investigationState.HasReviewedEvidence)
                {
                    return $"E  检查{anchorLabel}（证据链尚未完成）";
                }

                return $"E  稳定{anchorLabel}";
            }
        }

        private void Start()
        {
            investigationState = FindAnyObjectByType<InvestigationState>();
        }

        public void Configure(string id, string label = null)
        {
            anchorId = id;
            if (!string.IsNullOrWhiteSpace(label))
            {
                anchorLabel = label;
            }
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

            TimeAnchorResult result = investigationState.TryStabilizeTimeAnchor(
                anchorId,
                out string expectedAnchorId
            );
            if (result == TimeAnchorResult.AlreadyStable)
            {
                return "这个时间锚点已经稳定。";
            }

            if (result == TimeAnchorResult.WrongOrder)
            {
                return $"锚点顺序错误，已稳定的记录全部重置。先稳定{TimeAnchorPuzzleRules.GetDisplayName(expectedAnchorId)}。";
            }

            if (result == TimeAnchorResult.SequenceComplete)
            {
                return "三处时间锚点按“西仓、东验、中册”的顺序全部稳定。那一夜盐仓的火光在旧时层中完整显现。";
            }

            return $"{anchorLabel}停止漂移。按门牌记录继续寻找下一处锚点。";
        }
    }
}

using Demo02.Combat;
using Demo02.Core;
using UnityEngine;

namespace Demo02.WorldState
{
    public sealed class WorldContentEvent : MonoBehaviour
    {
        [SerializeField] private string contentId = "world-content";
        [SerializeField] private string displayName = "街区事件";
        [SerializeField] private WorldEncounterZone requiredZone;
        [SerializeField] private string rewardItemId;
        [SerializeField, Min(1)] private int rewardAmount = 1;
        [SerializeField] private string rewardText =
            "街区事件已经完成。";

        private bool completed;

        public static string LastCompletedText { get; private set; }
        public static float LastCompletedUntil { get; private set; }

        public string ContentId => contentId;
        public string DisplayName => displayName;
        public WorldEncounterZone RequiredZone => requiredZone;
        public bool IsCompleted => completed;
        public bool IsReadyToResolve =>
            completed || requiredZone == null || requiredZone.IsResolved;

        public void Configure(
            string id,
            string eventDisplayName,
            WorldEncounterZone zone,
            string itemId,
            int amount,
            string completionText)
        {
            contentId = id;
            displayName = eventDisplayName;
            requiredZone = zone;
            rewardItemId = itemId;
            rewardAmount = Mathf.Max(1, amount);
            rewardText = completionText;
            completed = WorldRuntime.Instance != null
                && WorldRuntime.Instance.IsWorldContentCompleted(contentId);
        }

        private void Start()
        {
            completed = WorldRuntime.Instance != null
                && WorldRuntime.Instance.IsWorldContentCompleted(contentId);
        }

        public bool TryCompleteReward(out string message)
        {
            if (completed)
            {
                message = $"{displayName}已经完成。";
                return true;
            }
            if (!IsReadyToResolve)
            {
                message = "巡逻尚未解除，还不能交付这件事。";
                return false;
            }

            InventoryState inventory = InventoryState.Instance != null
                ? InventoryState.Instance
                : Demo02.Core.GameServices.Resolve<InventoryState>();
            if (inventory != null
                && !string.IsNullOrWhiteSpace(rewardItemId)
                && !inventory.AddItem(rewardItemId, rewardAmount))
            {
                message = "背包已满，暂时无法领取奖励。";
                return false;
            }

            completed = true;
            WorldRuntime.Instance?.MarkWorldContentCompleted(contentId);
            GameAudio.PlayConfirm();
            message = rewardText;
            LastCompletedText = $"{displayName} · {rewardText}";
            LastCompletedUntil = Time.unscaledTime + 3.2f;
            return true;
        }

        public void Discover()
        {
            WorldRuntime.Instance?.MarkWorldContentStarted(contentId);
        }

        public static void AnnounceCompletion(
            string displayName,
            string completionText)
        {
            LastCompletedText =
                $"{displayName} · {completionText}";
            LastCompletedUntil = Time.unscaledTime + 4.2f;
        }
    }
}

using Demo02.Core;
using Demo02.Narrative;
using UnityEngine;

namespace Demo02.Interaction
{
    public sealed class InventoryItemPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private string pickupId;
        [SerializeField] private string itemId;
        [SerializeField, Min(1)] private int amount = 1;

        private InventoryState inventoryState;
        private CaseDefinition caseContent;

        public bool CanInteract =>
            inventoryState == null
            || !inventoryState.IsPickupCollected(pickupId);

        public string Prompt
        {
            get
            {
                InventoryItemDefinition item =
                    caseContent?.FindInventoryItem(itemId);
                return item != null
                    ? $"E  拾取 {item.DisplayName}"
                    : "E  拾取物品";
            }
        }

        public void Configure(
            string newPickupId,
            string newItemId,
            int newAmount = 1)
        {
            pickupId = newPickupId;
            itemId = newItemId;
            amount = Mathf.Max(1, newAmount);
        }

        private void Start()
        {
            inventoryState = Demo02.Core.GameServices.Resolve<InventoryState>() != null
                ? Demo02.Core.GameServices.Resolve<InventoryState>()
                : Demo02.Core.GameServices.Resolve<InventoryState>();
            InvestigationState investigation =
                Demo02.Core.GameServices.Resolve<InvestigationState>();
            caseContent = investigation?.CaseContent;

            if (inventoryState != null)
            {
                inventoryState.Changed += RefreshCollectedState;
                RefreshCollectedState();
            }
        }

        private void OnDisable()
        {
            if (inventoryState != null)
            {
                inventoryState.Changed -= RefreshCollectedState;
            }
        }

        private void RefreshCollectedState()
        {
            if (inventoryState != null
                && inventoryState.IsPickupCollected(pickupId)
                && gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }

        public string Interact(PlayerInteractor interactor)
        {
            if (inventoryState == null)
            {
                return "背包系统尚未初始化。";
            }
            if (inventoryState.IsPickupCollected(pickupId))
            {
                return string.Empty;
            }

            InventoryItemDefinition item = caseContent?.FindInventoryItem(itemId);
            if (item == null)
            {
                return "无法辨认这件物品。";
            }
            if (!inventoryState.AddItem(itemId, amount))
            {
                return $"{item.DisplayName}已经达到携带上限。";
            }

            inventoryState.MarkPickupCollected(pickupId);
            GameAudio.PlayConfirm();
            return $"获得 {item.DisplayName} ×{amount}";
        }
    }
}

using System;
using System.Collections.Generic;
using Demo02.Combat;
using Demo02.Narrative;
using UnityEngine;

namespace Demo02.Core
{
    public sealed class InventoryState : MonoBehaviour
    {
        private readonly Dictionary<string, int> itemCounts = new();
        private readonly HashSet<string> collectedPickupIds = new();
        [SerializeField] private CaseDefinition caseContent;

        public static InventoryState Instance { get; private set; }

        public event Action Changed;

        public int DistinctItemCount => itemCounts.Count;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Configure(CaseDefinition content)
        {
            caseContent = content;
        }

        public int GetCount(string itemId)
        {
            return !string.IsNullOrWhiteSpace(itemId)
                && itemCounts.TryGetValue(itemId, out int count)
                ? count
                : 0;
        }

        public bool AddItem(string itemId, int amount)
        {
            InventoryItemDefinition item = caseContent?.FindInventoryItem(itemId);
            if (item == null || amount <= 0)
            {
                Debug.LogWarning($"Cannot add unknown inventory item: {itemId}");
                return false;
            }

            int previous = GetCount(itemId);
            int next = Mathf.Min(item.MaxStack, previous + amount);
            if (next <= previous)
            {
                return false;
            }

            itemCounts[itemId] = next;
            Changed?.Invoke();
            return true;
        }

        public bool RemoveItem(string itemId, int amount)
        {
            int previous = GetCount(itemId);
            if (previous <= 0 || amount <= 0)
            {
                return false;
            }

            int next = Mathf.Max(0, previous - amount);
            if (next == 0)
            {
                itemCounts.Remove(itemId);
            }
            else
            {
                itemCounts[itemId] = next;
            }
            Changed?.Invoke();
            return true;
        }

        public bool MarkPickupCollected(string pickupId)
        {
            if (string.IsNullOrWhiteSpace(pickupId)
                || !collectedPickupIds.Add(pickupId))
            {
                return false;
            }

            Changed?.Invoke();
            return true;
        }

        public bool IsPickupCollected(string pickupId)
        {
            return !string.IsNullOrWhiteSpace(pickupId)
                && collectedPickupIds.Contains(pickupId);
        }

        public bool TryUseItem(
            string itemId,
            Combatant target,
            InvestigationState investigation,
            out string message)
        {
            InventoryItemDefinition item = caseContent?.FindInventoryItem(itemId);
            if (item == null || GetCount(itemId) <= 0)
            {
                message = "物品数量不足。";
                return false;
            }

            switch (item.Effect)
            {
                case InventoryItemEffect.RestoreHealth:
                    if (target == null || !target.IsAlive)
                    {
                        message = "当前没有可使用物品的角色。";
                        return false;
                    }
                    if (target.CurrentHealth >= target.MaxHealth)
                    {
                        message = "生命值已满，不需要使用。";
                        return false;
                    }

                    target.RestoreHealth(
                        Mathf.Min(
                            target.MaxHealth,
                            target.CurrentHealth + item.EffectAmount
                        )
                    );
                    if (item.ConsumeOnUse)
                    {
                        RemoveItem(itemId, 1);
                    }
                    message = string.IsNullOrWhiteSpace(item.UseResultText)
                        ? $"使用 {item.DisplayName}，恢复了生命。"
                        : item.UseResultText;
                    return true;

                case InventoryItemEffect.GrantStoryFact:
                    if (investigation == null
                        || string.IsNullOrWhiteSpace(item.ResultStoryFactId))
                    {
                        message = "暂时无法记录这件事。";
                        return false;
                    }
                    if (investigation.HasFact(item.ResultStoryFactId))
                    {
                        message = "这项记录已经存在。";
                        return false;
                    }
                    if (!string.IsNullOrWhiteSpace(item.RequiredStoryFactId)
                        && !investigation.HasFact(item.RequiredStoryFactId))
                    {
                        message = "你还没有获得足够的旁证来完成这项记录。";
                        return false;
                    }

                    investigation.SetFact(item.ResultStoryFactId);
                    if (item.ConsumeOnUse)
                    {
                        RemoveItem(itemId, 1);
                    }
                    message = string.IsNullOrWhiteSpace(item.UseResultText)
                        ? item.DisplayName + "已被使用。"
                        : item.UseResultText;
                    return true;

                default:
                    message = "这件物品不能直接使用。";
                    return false;
            }
        }

        public bool TryUseFirstHealingItem(
            Combatant target,
            InvestigationState investigation,
            out string message)
        {
            if (caseContent == null)
            {
                message = "案件物品数据尚未加载。";
                return false;
            }

            IReadOnlyList<InventoryItemDefinition> items =
                caseContent.InventoryItems;
            for (int index = 0; index < items.Count; index++)
            {
                InventoryItemDefinition item = items[index];
                if (item != null
                    && item.Effect == InventoryItemEffect.RestoreHealth
                    && GetCount(item.Id) > 0)
                {
                    return TryUseItem(
                        item.Id,
                        target,
                        investigation,
                        out message
                    );
                }
            }

            message = "没有可用的恢复物品。";
            return false;
        }

        public List<InventoryItemSaveData> ExportItems()
        {
            List<InventoryItemSaveData> result = new();
            foreach (KeyValuePair<string, int> pair in itemCounts)
            {
                result.Add(new InventoryItemSaveData
                {
                    ItemId = pair.Key,
                    Count = pair.Value
                });
            }
            return result;
        }

        public string[] ExportCollectedPickupIds()
        {
            return new List<string>(collectedPickupIds).ToArray();
        }

        public string BuildSignature()
        {
            List<string> keys = new(itemCounts.Keys);
            keys.Sort(StringComparer.Ordinal);
            List<string> parts = new();
            for (int index = 0; index < keys.Count; index++)
            {
                parts.Add($"{keys[index]}:{itemCounts[keys[index]]}");
            }
            return string.Join("|", parts);
        }

        public void Restore(
            IEnumerable<InventoryItemSaveData> items,
            IEnumerable<string> pickupIds)
        {
            itemCounts.Clear();
            collectedPickupIds.Clear();

            if (items != null)
            {
                foreach (InventoryItemSaveData savedItem in items)
                {
                    if (savedItem == null
                        || string.IsNullOrWhiteSpace(savedItem.ItemId)
                        || savedItem.Count <= 0
                        || caseContent?.FindInventoryItem(savedItem.ItemId) == null)
                    {
                        continue;
                    }
                    itemCounts[savedItem.ItemId] = savedItem.Count;
                }
            }

            if (pickupIds != null)
            {
                foreach (string pickupId in pickupIds)
                {
                    if (!string.IsNullOrWhiteSpace(pickupId))
                    {
                        collectedPickupIds.Add(pickupId);
                    }
                }
            }

            Changed?.Invoke();
        }
    }
}

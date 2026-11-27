using System.Collections.Generic;
using Demo02.Core;
using Demo02.WorldState;
using UnityEngine;

namespace Demo02.Player
{
    public sealed class ContinuationLoadout : MonoBehaviour
    {
        public const int SlotCount = 2;

        private readonly List<string> equippedIds = new();
        private float nextRefresh;

        public static ContinuationLoadout Instance { get; private set; }
        public int EquippedCount => equippedIds.Count;
        public IReadOnlyList<string> EquippedIds => equippedIds;

        private void Awake()
        {
            Instance = this;
            Demo02.Core.GameServices.Register(this);
        }

        private void Start()
        {
            Refresh();
        }

        private void Update()
        {
            if (Time.unscaledTime >= nextRefresh)
            {
                nextRefresh = Time.unscaledTime + 1f;
                Refresh();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Demo02.Core.GameServices.Unregister(this);
                Instance = null;
            }
        }

        public void Refresh()
        {
            WorldRuntime runtime = Demo02.Core.GameServices.Resolve<WorldRuntime>();
            if (runtime == null)
            {
                return;
            }

            equippedIds.Clear();
            if (runtime.IsWorldContentCompleted(
                    "dongguan-apprentice-ledger"
                ))
            {
                equippedIds.Add("continuation-returning-current");
            }
            if (runtime.IsWorldContentCompleted(
                    "caiyi-apprentice-ledger-page"
                ))
            {
                equippedIds.Add("continuation-whole-page");
            }

            PlayerPartyController party =
                Demo02.Core.GameServices.Resolve<PlayerPartyController>();
            if (party == null)
            {
                return;
            }

            float staminaBonus = equippedIds.Contains(
                "continuation-returning-current"
            )
                ? 10f
                : 0f;
            float energyMultiplier = equippedIds.Contains(
                "continuation-whole-page"
            )
                ? 1.25f
                : 1f;
            for (int index = 0; index < party.PartySlotCount; index++)
            {
                Transform member = party.GetCharacter(index);
                CharacterRuntime characterRuntime = member != null
                    ? member.GetComponent<CharacterRuntime>()
                    : null;
                characterRuntime?.SetContinuationBonuses(
                    staminaBonus,
                    energyMultiplier
                );
            }
        }

        public string GetJournalSummary()
        {
            if (equippedIds.Count == 0)
            {
                return "续式槽位：0/2 · 完成街区事件可获得";
            }
            return "续式槽位：" + $"{equippedIds.Count}/2 · "
                + (equippedIds.Contains("continuation-returning-current")
                    ? "续·回流（体力上限 +10）"
                    : string.Empty)
                + (equippedIds.Contains("continuation-returning-current")
                    && equippedIds.Contains("continuation-whole-page")
                    ? "；"
                    : string.Empty)
                + (equippedIds.Contains("continuation-whole-page")
                    ? "续·整页（终结技能量 +25%）"
                    : string.Empty);
        }
    }
}

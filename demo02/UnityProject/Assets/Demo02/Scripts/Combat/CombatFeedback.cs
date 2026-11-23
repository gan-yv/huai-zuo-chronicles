using System;
using UnityEngine;

namespace Demo02.Combat
{
    public sealed class DamageFeedbackData
    {
        public Vector3 WorldPosition;
        public float Amount;
        public bool IsReaction;
        public bool IsPlayerDamage;
    }

    public static class CombatFeedback
    {
        public static event Action<DamageFeedbackData> DamageReported;
        public static event Action PlayerHurt;

        public static void ReportDamage(
            Vector3 worldPosition,
            float amount,
            bool isReaction = false,
            bool isPlayerDamage = false)
        {
            if (amount <= 0.1f)
            {
                return;
            }

            DamageReported?.Invoke(new DamageFeedbackData
            {
                WorldPosition = worldPosition,
                Amount = amount,
                IsReaction = isReaction,
                IsPlayerDamage = isPlayerDamage
            });
        }

        public static void ReportPlayerHurt()
        {
            PlayerHurt?.Invoke();
        }
    }
}

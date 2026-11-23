using System.Collections.Generic;
using Demo02.Combat;
using Demo02.Core;
using Demo02.Narrative;
using UnityEngine;

namespace Demo02.UI
{
    public sealed class CombatFeedbackHud : MonoBehaviour
    {
        private sealed class DamageEntry
        {
            public Vector3 Position;
            public float Amount;
            public bool IsReaction;
            public bool IsPlayerDamage;
            public float StartedAt;
            public float Offset;
        }

        private const float Lifetime = 0.9f;
        private readonly List<DamageEntry> entries = new();
        private GUIStyle damageStyle;
        private GUIStyle reactionStyle;
        private GUIStyle playerDamageStyle;
        private float hurtUntil;
        private float cachedScale = -1f;

        private void OnEnable()
        {
            CombatFeedback.DamageReported += OnDamageReported;
            CombatFeedback.PlayerHurt += OnPlayerHurt;
        }

        private void OnDisable()
        {
            CombatFeedback.DamageReported -= OnDamageReported;
            CombatFeedback.PlayerHurt -= OnPlayerHurt;
        }

        private void OnDamageReported(DamageFeedbackData data)
        {
            if (data == null)
            {
                return;
            }
            if (entries.Count >= 40)
            {
                entries.RemoveAt(0);
            }
            entries.Add(new DamageEntry
            {
                Position = data.WorldPosition,
                Amount = data.Amount,
                IsReaction = data.IsReaction,
                IsPlayerDamage = data.IsPlayerDamage,
                StartedAt = Time.unscaledTime,
                Offset = Random.Range(-0.35f, 0.35f)
            });
        }

        private void OnPlayerHurt()
        {
            hurtUntil = Time.unscaledTime + 0.2f;
        }

        private void OnGUI()
        {
            if (IsBlockedByOtherUi() || Camera.main == null)
            {
                return;
            }

            float scale = UiTheme.Scale;
            if (!Mathf.Approximately(cachedScale, scale))
            {
                cachedScale = scale;
                damageStyle = UiTheme.Label(
                    15,
                    TextAnchor.MiddleCenter,
                    bold: true
                );
                reactionStyle = UiTheme.Label(
                    16,
                    TextAnchor.MiddleCenter,
                    bold: true
                );
                reactionStyle.normal.textColor =
                    new Color(1f, 0.72f, 0.28f);
                playerDamageStyle = UiTheme.Label(
                    15,
                    TextAnchor.MiddleCenter,
                    bold: true
                );
                playerDamageStyle.normal.textColor =
                    new Color(1f, 0.36f, 0.26f);
            }

            if (Time.unscaledTime < hurtUntil)
            {
                float alpha = Mathf.Clamp01(
                    (hurtUntil - Time.unscaledTime) / 0.2f
                );
                UiTheme.DrawRect(
                    new Rect(0f, 0f, Screen.width, Screen.height),
                    new Color(0.55f, 0.03f, 0.02f, 0.08f * alpha)
                );
            }

            for (int index = entries.Count - 1; index >= 0; index--)
            {
                DamageEntry entry = entries[index];
                float age = Time.unscaledTime - entry.StartedAt;
                if (age >= Lifetime)
                {
                    entries.RemoveAt(index);
                    continue;
                }

                Vector3 screen = Camera.main.WorldToScreenPoint(
                    entry.Position + Vector3.up * (1.5f + age * 0.8f)
                );
                if (screen.z <= 0f)
                {
                    continue;
                }

                float alpha = Mathf.Clamp01(1f - age / Lifetime);
                Rect rect = new(
                    screen.x - 48f * scale + entry.Offset * 20f * scale,
                    Screen.height - screen.y - 16f * scale,
                    96f * scale,
                    30f * scale
                );
                string text = entry.IsReaction
                    ? $"时势 +{Mathf.RoundToInt(entry.Amount)}"
                    : $"{Mathf.RoundToInt(entry.Amount)}";
                GUIStyle style = entry.IsPlayerDamage
                    ? playerDamageStyle
                    : entry.IsReaction
                        ? reactionStyle
                        : damageStyle;
                Color previous = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, alpha);
                GUI.Label(
                    new Rect(rect.x + 1f, rect.y + 2f, rect.width, rect.height),
                    text,
                    entry.IsPlayerDamage
                        ? playerDamageStyle
                        : entry.IsReaction
                            ? reactionStyle
                            : damageStyle
                );
                GUI.Label(rect, text, style);
                GUI.color = previous;
            }
        }

        private static bool IsBlockedByOtherUi()
        {
            return GameplayUiState.IsGameplayBlocked;
        }
    }
}

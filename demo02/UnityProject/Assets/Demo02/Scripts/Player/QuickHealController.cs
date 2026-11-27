using Demo02.Combat;
using Demo02.Core;
using Demo02.Narrative;
using Demo02.UI;
using UnityEngine;

namespace Demo02.Player
{
    [DefaultExecutionOrder(35)]
    public sealed class QuickHealController : MonoBehaviour
    {
        private const float UseCooldown = 0.8f;
        private const float StatusDuration = 2.4f;

        private PlayerPartyController party;
        private InventoryState inventory;
        private InvestigationState investigation;
        private string statusMessage;
        private float statusUntil;
        private float nextUseTime;
        private float cachedScale = -1f;
        private GUIStyle statusStyle;

        private void Start()
        {
            party = Demo02.Core.GameServices.Resolve<PlayerPartyController>();
            inventory = Demo02.Core.GameServices.Resolve<InventoryState>() != null
                ? Demo02.Core.GameServices.Resolve<InventoryState>()
                : Demo02.Core.GameServices.Resolve<InventoryState>();
            investigation = Demo02.Core.GameServices.Resolve<InvestigationState>();
        }

        private void Update()
        {
            if (!Demo02Input.QuickHealPressed
                || Time.unscaledTime < nextUseTime
                || Time.timeScale <= 0f
                || GameplayUiState.IsGameplayBlocked)
            {
                return;
            }

            Transform character = party != null
                ? party.ActiveCharacter
                : null;
            Combatant target = character != null
                ? character.GetComponent<Combatant>()
                : null;
            if (inventory == null || target == null)
            {
                ShowStatus("当前无法使用恢复物品。");
                return;
            }

            nextUseTime = Time.unscaledTime + UseCooldown;
            bool used = inventory.TryUseFirstHealingItem(
                target,
                investigation,
                out string message
            );
            ShowStatus(message);
            if (used)
            {
                GameAudio.PlayConfirm();
            }
            else
            {
                GameAudio.PlayInteraction();
            }
        }

        private void ShowStatus(string message)
        {
            statusMessage = message;
            statusUntil = Time.unscaledTime + StatusDuration;
        }

        private void OnGUI()
        {
            if (string.IsNullOrWhiteSpace(statusMessage)
                || Time.unscaledTime >= statusUntil
                || GameplayUiState.IsModalOpen)
            {
                return;
            }

            EnsureStyle();
            float scale = UiTheme.Scale;
            float alpha = Mathf.Clamp01(
                (statusUntil - Time.unscaledTime) / 0.35f
            );
            Rect panel = new(
                18f * scale,
                Screen.height - 148f * scale,
                330f * scale,
                36f * scale
            );
            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            UiTheme.DrawRect(
                panel,
                new Color(0.025f, 0.035f, 0.05f, 0.94f)
            );
            UiTheme.DrawRect(
                new Rect(
                    panel.x,
                    panel.y,
                    3f * scale,
                    panel.height
                ),
                UiTheme.Accent
            );
            GUI.Label(
                new Rect(
                    panel.x + 12f * scale,
                    panel.y,
                    panel.width - 20f * scale,
                    panel.height
                ),
                $"快捷恢复 · {statusMessage}",
                statusStyle
            );
            GUI.color = previous;
        }

        private void EnsureStyle()
        {
            float scale = UiTheme.Scale;
            if (statusStyle != null
                && Mathf.Approximately(cachedScale, scale))
            {
                return;
            }
            cachedScale = scale;
            statusStyle = UiTheme.Label(
                11,
                TextAnchor.MiddleLeft,
                bold: true
            );
        }
    }
}

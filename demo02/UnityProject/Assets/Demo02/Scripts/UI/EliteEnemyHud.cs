using Demo02.Combat;
using Demo02.WorldState;
using UnityEngine;

namespace Demo02.UI
{
    public sealed class EliteEnemyHud : MonoBehaviour
    {
        private GUIStyle titleStyle;
        private GUIStyle phaseStyle;
        private float cachedScale = -1f;

        private void OnGUI()
        {
            if (GameplayUiState.IsGameplayBlocked)
            {
                return;
            }

            EliteEnemyController elite = null;
            foreach (Combatant combatant in CombatRegistry.Enemies)
            {
                EliteEnemyController candidate =
                    combatant != null
                        ? combatant.GetComponent<EliteEnemyController>()
                        : null;
                if (candidate != null && candidate.IsActive)
                {
                    elite = candidate;
                    break;
                }
            }
            if (elite == null)
            {
                return;
            }

            Combatant health = elite.GetComponent<Combatant>();
            EnemyBreakStatus breakStatus =
                elite.GetComponent<EnemyBreakStatus>();
            EliteChargeAttack chargeAttack =
                elite.GetComponent<EliteChargeAttack>();
            float scale = UiTheme.Scale;
            if (!Mathf.Approximately(cachedScale, scale))
            {
                cachedScale = scale;
                titleStyle = UiTheme.Label(
                    15,
                    TextAnchor.MiddleCenter,
                    bold: true
                );
                phaseStyle = UiTheme.Label(
                    10,
                    TextAnchor.MiddleCenter,
                    bold: true
                );
                phaseStyle.normal.textColor = UiTheme.Accent;
            }

            Rect panel = new(
                Screen.width * 0.5f - 275f * scale,
                146f * scale,
                550f * scale,
                62f * scale
            );
            UiTheme.DrawRect(panel, new Color(0.025f, 0.03f, 0.04f, 0.95f));
            UiTheme.DrawBorder(panel, new Color(0.94f, 0.68f, 0.3f, 0.7f));
            GUI.Label(
                new Rect(
                    panel.x,
                    panel.y + 3f * scale,
                    panel.width,
                    24f * scale
                ),
                elite.DisplayName,
                titleStyle
            );

            Rect healthBar = new(
                panel.x + 28f * scale,
                panel.y + 29f * scale,
                panel.width - 56f * scale,
                8f * scale
            );
            UiTheme.DrawRect(healthBar, new Color(0f, 0f, 0f, 0.55f));
            UiTheme.DrawRect(
                new Rect(
                    healthBar.x,
                    healthBar.y,
                    healthBar.width
                        * Mathf.Clamp01(
                            health.CurrentHealth / health.MaxHealth
                        ),
                    healthBar.height
                ),
                new Color(0.82f, 0.2f, 0.14f)
            );
            GUI.Label(
                new Rect(
                    panel.x + 28f * scale,
                    panel.y + 41f * scale,
                    panel.width - 56f * scale,
                    18f * scale
                ),
                chargeAttack != null && chargeAttack.IsCharging
                    ? "蓄势冲锋 · 准备闪避"
                    : elite.PhaseLabel
                        + (breakStatus != null && breakStatus.IsBroken
                            ? " · 破防"
                            : string.Empty),
                phaseStyle
            );
        }
    }
}

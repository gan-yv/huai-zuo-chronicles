using Demo02.Combat;
using UnityEngine;

namespace Demo02.UI
{
    public sealed class EliteBattleResultHud : MonoBehaviour
    {
        private EliteEnemyController elite;
        private Combatant health;
        private EnemyBreakStatus breakStatus;
        private float startedAt;
        private float resultUntil;
        private float duration;
        private int breakCount;
        private bool tracking;
        private bool shown;
        private GUIStyle titleStyle;
        private GUIStyle detailStyle;
        private float cachedScale = -1f;

        private void Update()
        {
            if (!tracking)
            {
                elite = FindAnyObjectByType<EliteEnemyController>(
                    FindObjectsInactive.Exclude
                );
                if (elite == null || !elite.IsActive)
                {
                    return;
                }
                health = elite.GetComponent<Combatant>();
                breakStatus = elite.GetComponent<EnemyBreakStatus>();
                startedAt = Time.unscaledTime;
                tracking = true;
            }

            if (health != null && !health.IsAlive && !shown)
            {
                shown = true;
                resultUntil = Time.unscaledTime + 5f;
                duration = Time.unscaledTime - startedAt;
                breakCount = breakStatus != null
                    ? breakStatus.BreakCount
                    : 0;
            }
        }

        private void OnGUI()
        {
            if (!shown
                || Time.unscaledTime >= resultUntil
                || GameplayUiState.IsModalOpen)
            {
                return;
            }

            float scale = UiTheme.Scale;
            if (!Mathf.Approximately(cachedScale, scale))
            {
                cachedScale = scale;
                titleStyle = UiTheme.Label(
                    24,
                    TextAnchor.MiddleCenter,
                    bold: true
                );
                titleStyle.normal.textColor = UiTheme.Accent;
                detailStyle = UiTheme.Label(13);
            }

            Rect panel = new(
                Screen.width * 0.5f - 290f * scale,
                Screen.height * 0.3f,
                580f * scale,
                142f * scale
            );
            UiTheme.DrawRect(panel, new Color(0.02f, 0.03f, 0.04f, 0.96f));
            UiTheme.DrawBorder(panel, UiTheme.Accent, 2f);
            GUI.Label(
                new Rect(
                    panel.x + 20f * scale,
                    panel.y + 13f * scale,
                    panel.width - 40f * scale,
                    38f * scale
                ),
                "精英击破",
                titleStyle
            );
            GUI.Label(
                new Rect(
                    panel.x + 36f * scale,
                    panel.y + 58f * scale,
                    panel.width - 72f * scale,
                    64f * scale
                ),
                $"战斗用时  {duration:0.0} 秒\n"
                + $"破防次数  {breakCount}\n"
                + "线索已确认：返回裁衣学徒领取街区奖励",
                detailStyle
            );
        }
    }
}

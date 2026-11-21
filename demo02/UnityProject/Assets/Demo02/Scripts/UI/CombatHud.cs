using Demo02.Combat;
using Demo02.Player;
using UnityEngine;

namespace Demo02.UI
{
    public sealed class CombatHud : MonoBehaviour
    {
        private PlayerPartyController party;
        private EncounterState encounterState;

        private void Awake()
        {
            party = FindAnyObjectByType<PlayerPartyController>();
            encounterState = FindAnyObjectByType<EncounterState>();
        }

        private void OnGUI()
        {
            Transform activeCharacter = party != null ? party.ActiveCharacter : null;
            Combatant activeHealth = activeCharacter != null
                ? activeCharacter.GetComponent<Combatant>()
                : null;

            if (activeHealth != null)
            {
                DrawHealth(activeHealth);
            }

            int aliveEnemies = encounterState != null ? encounterState.AliveEnemyCount : 0;

            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleRight,
                fontSize = 16
            };

            GUI.Label(
                new Rect(Screen.width - 384f, 84f, 360f, 30f),
                $"敌人：{aliveEnemies}    Tab 切换角色",
                style
            );
            GUI.Label(
                new Rect(Screen.width - 484f, Screen.height - 42f, 460f, 30f),
                "WASD 移动  |  Shift 奔跑  |  Space 跳跃  |  左键攻击  |  右键镜头",
                style
            );

            if (party != null && party.IsPartyDefeated)
            {
                GUI.Box(
                    new Rect((Screen.width - 420f) * 0.5f, Screen.height * 0.35f, 420f, 82f),
                    "队伍倒下\n按 R 重新开始",
                    new GUIStyle(GUI.skin.box)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 22
                    }
                );
            }
        }

        private static void DrawHealth(Combatant health)
        {
            const float width = 260f;
            const float height = 24f;
            Rect background = new Rect(Screen.width - width - 24f, 24f, width, height);
            Rect fill = new Rect(
                background.x + 2f,
                background.y + 2f,
                (width - 4f) * Mathf.Clamp01(health.CurrentHealth / health.MaxHealth),
                height - 4f
            );

            GUI.Box(background, string.Empty);
            GUI.DrawTexture(fill, Texture2D.whiteTexture);
            GUI.Label(
                background,
                $"生命  {Mathf.CeilToInt(health.CurrentHealth)} / {Mathf.CeilToInt(health.MaxHealth)}",
                new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 15,
                    normal = { textColor = Color.black }
                }
            );
        }
    }
}

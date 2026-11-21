using Demo02.Combat;
using Demo02.Player;
using Demo02.WorldState;
using UnityEngine;

namespace Demo02.UI
{
    public sealed class CombatHud : MonoBehaviour
    {
        private PlayerPartyController party;
        private EncounterState encounterState;
        private TimeLayerController timeLayerController;
        private WorldRuntime worldRuntime;

        private void Awake()
        {
            party = FindAnyObjectByType<PlayerPartyController>();
            encounterState = FindAnyObjectByType<EncounterState>();
            timeLayerController = FindAnyObjectByType<TimeLayerController>();
            worldRuntime = WorldRuntime.Instance != null
                ? WorldRuntime.Instance
                : FindAnyObjectByType<WorldRuntime>();
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
                "WASD 移动 | Shift 奔跑 | Ctrl 闪避 | M 世界地图 | Q 时间层 | Tab 切换角色",
                style
            );

            if (timeLayerController != null)
            {
                string layerName = timeLayerController.CurrentLayer == TimeLayer.Present
                    ? "现世"
                    : "旧时";
                if (GUI.Button(
                    new Rect(Screen.width * 0.5f - 120f, 24f, 240f, 42f),
                    $"当前时间层：{layerName}  （Q/点击切换）"
                ))
                {
                    timeLayerController.ToggleLayer();
                }
            }

            if (worldRuntime != null && !string.IsNullOrWhiteSpace(worldRuntime.CurrentAreaName))
            {
                GUI.Label(
                    new Rect(24f, Screen.height - 74f, 360f, 34f),
                    $"区域：{worldRuntime.CurrentAreaName}",
                    new GUIStyle(GUI.skin.box)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        fontSize = 16,
                        padding = new RectOffset(12, 12, 6, 6)
                    }
                );
            }

            DrawEnemyHealthBars();

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

        private void DrawEnemyHealthBars()
        {
            Camera sceneCamera = Camera.main;
            if (encounterState == null || sceneCamera == null)
            {
                return;
            }

            foreach (Combatant enemy in encounterState.Enemies)
            {
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                Vector3 screenPoint = sceneCamera.WorldToScreenPoint(
                    enemy.transform.position + Vector3.up * 1.4f
                );
                if (screenPoint.z <= 0f)
                {
                    continue;
                }

                const float width = 72f;
                const float height = 10f;
                Rect background = new Rect(
                    screenPoint.x - width * 0.5f,
                    Screen.height - screenPoint.y - height,
                    width,
                    height
                );
                Rect fill = new Rect(
                    background.x + 1f,
                    background.y + 1f,
                    (width - 2f) * Mathf.Clamp01(enemy.CurrentHealth / enemy.MaxHealth),
                    height - 2f
                );

                GUI.Box(background, string.Empty);
                Color previousColor = GUI.color;
                GUI.color = new Color(0.85f, 0.2f, 0.15f, 1f);
                GUI.DrawTexture(fill, Texture2D.whiteTexture);
                GUI.color = previousColor;
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

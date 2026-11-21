using Demo02.Combat;
using Demo02.Player;
using Demo02.WorldState;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Demo02.UI
{
    public sealed class CombatHud : MonoBehaviour
    {
        private PlayerPartyController party;
        private EncounterState encounterState;
        private TimeLayerController timeLayerController;
        private WorldRuntime worldRuntime;
        private bool showHelp;

        private void Awake()
        {
            party = FindAnyObjectByType<PlayerPartyController>();
            encounterState = FindAnyObjectByType<EncounterState>();
            timeLayerController = FindAnyObjectByType<TimeLayerController>();
            worldRuntime = WorldRuntime.Instance != null
                ? WorldRuntime.Instance
                : FindAnyObjectByType<WorldRuntime>();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
            {
                showHelp = !showHelp;
            }
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

            if (aliveEnemies > 0)
            {
                GUI.Label(
                    new Rect(Screen.width - 384f, 84f, 360f, 30f),
                    $"附近威胁：{aliveEnemies}",
                    style
                );
            }

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

            if (GUI.Button(
                new Rect(Screen.width - 116f, Screen.height - 42f, 92f, 28f),
                "F1 操作"
            ))
            {
                showHelp = !showHelp;
            }

            if (showHelp)
            {
                float width = Mathf.Min(640f, Screen.width - 60f);
                GUI.Box(
                    new Rect((Screen.width - width) * 0.5f, Screen.height * 0.2f, width, 360f),
                    "操作说明\n\n" +
                    "WASD 移动    Shift 奔跑    Ctrl 闪避    Space 跳跃\n" +
                    "鼠标左键 攻击    按住鼠标右键 旋转镜头\n" +
                    "E 调查/对话/修复    Tab 切换角色    Q 切换时间层\n" +
                    "M 世界地图    Esc 暂停    F5 保存    F9 读取\n" +
                    "R 在结局或队伍倒下后重新开始\n\n" +
                    "按 F1 关闭",
                    new GUIStyle(GUI.skin.box)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 17,
                        wordWrap = true
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

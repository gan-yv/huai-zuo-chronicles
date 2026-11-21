using Demo02.Combat;
using Demo02.Core;
using Demo02.Narrative;
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
            if (Keyboard.current != null && Keyboard.current.f2Key.wasPressedThisFrame)
            {
                GameAudio.ToggleMuted();
            }
        }

        private void OnGUI()
        {
            if (WorldMapController.Instance != null && WorldMapController.Instance.IsOpen)
            {
                return;
            }
            if (InvestigationJournalController.Instance != null
                && InvestigationJournalController.Instance.IsOpen)
            {
                return;
            }
            if (DialogueController.Instance != null && DialogueController.Instance.IsOpen)
            {
                return;
            }

            Transform activeCharacter = party != null ? party.ActiveCharacter : null;
            Combatant activeHealth = activeCharacter != null
                ? activeCharacter.GetComponent<Combatant>()
                : null;

            if (activeHealth != null)
            {
                DrawHealth(activeHealth);
            }

            int aliveEnemies = encounterState != null ? encounterState.AliveEnemyCount : 0;
            float scale = UiTheme.Scale;

            if (aliveEnemies > 0)
            {
                Rect threatPanel = new(
                    Screen.width - 176f * scale,
                    72f * scale,
                    152f * scale,
                    32f * scale
                );
                UiTheme.DrawRect(threatPanel, UiTheme.Ink);
                GUIStyle threatStyle = UiTheme.Label(
                    13,
                    TextAnchor.MiddleCenter,
                    bold: true
                );
                threatStyle.normal.textColor = new Color(0.94f, 0.48f, 0.3f);
                GUI.Label(
                    threatPanel,
                    $"威胁  {aliveEnemies}",
                    threatStyle
                );
            }

            if (timeLayerController != null)
            {
                string layerName = timeLayerController.CurrentLayer == TimeLayer.Present
                    ? "现世"
                    : "旧时";
                if (GUI.Button(
                    new Rect(
                        Screen.width * 0.5f - 132f * scale,
                        20f * scale,
                        264f * scale,
                        44f * scale
                    ),
                    $"时间层  {layerName}  [Q]",
                    UiTheme.Button(14)
                ))
                {
                    timeLayerController.ToggleLayer();
                }
            }

            if (worldRuntime != null && !string.IsNullOrWhiteSpace(worldRuntime.CurrentAreaName))
            {
                Rect areaPanel = new(
                    18f * scale,
                    Screen.height - 56f * scale,
                    240f * scale,
                    36f * scale
                );
                UiTheme.DrawRect(areaPanel, UiTheme.Ink);
                UiTheme.DrawRect(
                    new Rect(areaPanel.x, areaPanel.y, 3f * scale, areaPanel.height),
                    UiTheme.Accent
                );
                GUI.Label(
                    new Rect(
                        areaPanel.x + 14f * scale,
                        areaPanel.y,
                        areaPanel.width - 20f * scale,
                        areaPanel.height
                    ),
                    $"区域  {worldRuntime.CurrentAreaName}",
                    UiTheme.Label(14)
                );
            }

            DrawEnemyHealthBars();

            float utilityWidth = 100f * scale;
            float utilityHeight = 30f * scale;
            float utilityY = Screen.height - utilityHeight - 12f * scale;
            float right = Screen.width - 18f * scale;

            if (GUI.Button(
                new Rect(
                    right - utilityWidth,
                    utilityY,
                    utilityWidth,
                    utilityHeight
                ),
                "F1 操作",
                UiTheme.Button(13)
            ))
            {
                showHelp = !showHelp;
            }
            if (GUI.Button(
                new Rect(
                    right - utilityWidth * 2f - 10f * scale,
                    utilityY,
                    utilityWidth,
                    utilityHeight
                ),
                "M 地图",
                UiTheme.Button(13)
            ))
            {
                WorldMapController.Instance?.ToggleMap();
            }
            if (GUI.Button(
                new Rect(
                    right - utilityWidth * 3f - 20f * scale,
                    utilityY,
                    utilityWidth,
                    utilityHeight
                ),
                GameAudio.Instance != null && GameAudio.Instance.IsMuted
                    ? "声音关 [F2]"
                    : "声音开 [F2]",
                UiTheme.Button(13)
            ))
            {
                GameAudio.ToggleMuted();
            }
            if (GUI.Button(
                new Rect(
                    right - utilityWidth * 4f - 30f * scale,
                    utilityY,
                    utilityWidth,
                    utilityHeight
                ),
                "J 案卷",
                UiTheme.Button(13)
            ))
            {
                InvestigationJournalController.Instance?.ToggleJournal();
            }
            if (GUI.Button(
                new Rect(
                    right - utilityWidth * 5f - 40f * scale,
                    utilityY,
                    utilityWidth,
                    utilityHeight
                ),
                InvestigationVisionController.Instance != null
                    && InvestigationVisionController.Instance.IsActive
                    ? "V 关视界"
                    : "V 视界",
                UiTheme.Button(13)
            ))
            {
                InvestigationVisionController.Instance?.ToggleVision();
            }

            if (showHelp)
            {
                float width = Mathf.Min(720f * scale, Screen.width - 60f);
                Rect panel = new(
                    (Screen.width - width) * 0.5f,
                    Screen.height * 0.16f,
                    width,
                    394f * scale
                );
                UiTheme.DrawRect(panel, UiTheme.Ink);
                UiTheme.DrawBorder(panel, UiTheme.Accent, 2f);
                UiTheme.DrawRect(
                    new Rect(panel.x + 24f * scale, panel.y + 24f * scale, 72f * scale, 3f),
                    UiTheme.Accent
                );
                GUI.Label(
                    new Rect(
                        panel.x + 24f * scale,
                        panel.y + 38f * scale,
                        panel.width - 48f * scale,
                        42f * scale
                    ),
                    "操作说明",
                    UiTheme.Label(25, TextAnchor.MiddleLeft, bold: true)
                );
                GUI.Label(
                    new Rect(
                        panel.x + 28f * scale,
                        panel.y + 92f * scale,
                        panel.width - 56f * scale,
                        236f * scale
                    ),
                    "WASD  移动      Shift  奔跑      Ctrl  闪避      Space  跳跃\n"
                    + "鼠标左键  攻击      按住鼠标右键  旋转镜头\n"
                    + "E  调查 / 对话 / 修复      Tab  切换角色      Q  切换时间层\n"
                    + "J  调查案卷      M  世界地图      Esc  暂停      F2  静音\n"
                    + "V  修痕视界      F5  保存      F9  读取      F1  关闭帮助\n"
                    + "R  在结局或队伍倒下后重新开始",
                    UiTheme.Label(16)
                );
                GUI.Label(
                    new Rect(
                        panel.x + 24f * scale,
                        panel.yMax - 36f * scale,
                        panel.width - 48f * scale,
                        24f * scale
                    ),
                    "F1 关闭",
                    UiTheme.Label(13, TextAnchor.MiddleRight)
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

                float width = 76f * UiTheme.Scale;
                float height = 9f * UiTheme.Scale;
                Rect background = new Rect(
                    screenPoint.x - width * 0.5f,
                    Screen.height - screenPoint.y - height,
                    width,
                    height
                );
                Rect fill = new Rect(
                    background.x + 1f,
                    background.y + 1f,
                    (width - 4f) * Mathf.Clamp01(enemy.CurrentHealth / enemy.MaxHealth),
                    height - 4f
                );

                UiTheme.DrawRect(background, UiTheme.Ink);
                UiTheme.DrawRect(fill, new Color(0.86f, 0.24f, 0.16f));
                UiTheme.DrawBorder(background, new Color(1f, 1f, 1f, 0.16f));
            }
        }

        private static void DrawHealth(Combatant health)
        {
            float scale = UiTheme.Scale;
            float width = 236f * scale;
            float height = 26f * scale;
            Rect background = new(
                Screen.width - width - 18f * scale,
                20f * scale,
                width,
                height
            );
            Rect fill = new Rect(
                background.x + 2f,
                background.y + 2f,
                (width - 4f) * Mathf.Clamp01(health.CurrentHealth / health.MaxHealth),
                height - 4f
            );

            float healthRatio = Mathf.Clamp01(health.CurrentHealth / health.MaxHealth);
            Color healthColor = Color.Lerp(
                new Color(0.78f, 0.18f, 0.14f),
                new Color(0.38f, 0.64f, 0.3f),
                healthRatio
            );
            UiTheme.DrawRect(background, UiTheme.Ink);
            UiTheme.DrawRect(fill, healthColor);
            UiTheme.DrawBorder(background, new Color(1f, 1f, 1f, 0.18f));
            GUIStyle healthStyle = UiTheme.Label(
                13,
                TextAnchor.MiddleCenter,
                bold: true
            );
            healthStyle.normal.textColor = Color.white;
            GUI.Label(
                background,
                $"生命  {Mathf.CeilToInt(health.CurrentHealth)} / {Mathf.CeilToInt(health.MaxHealth)}",
                healthStyle
            );
        }
    }
}

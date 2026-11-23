using System.Collections.Generic;
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
        private int previousAliveEnemies;
        private float clearNoticeUntil;

        private void Awake()
        {
            party = FindAnyObjectByType<PlayerPartyController>();
            encounterState = FindAnyObjectByType<EncounterState>();
            timeLayerController = FindAnyObjectByType<TimeLayerController>();
            worldRuntime = WorldRuntime.Instance != null
                ? WorldRuntime.Instance
                : FindAnyObjectByType<WorldRuntime>();
            previousAliveEnemies = encounterState != null
                ? encounterState.AliveEnemyCount
                : 0;
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
            if (GameFlowController.Instance != null
                && GameFlowController.Instance.IsGameEnding)
            {
                return;
            }

            if (WorldMapController.Instance != null && WorldMapController.Instance.IsOpen)
            {
                return;
            }
            if (InvestigationJournalController.Instance != null
                && InvestigationJournalController.Instance.IsOpen)
            {
                return;
            }
            if (TestimonyBoardController.Instance != null
                && TestimonyBoardController.Instance.IsOpen)
            {
                return;
            }
            if (InventoryController.Instance != null
                && InventoryController.Instance.IsOpen)
            {
                return;
            }
            if (SettingsController.Instance != null && SettingsController.Instance.IsOpen)
            {
                return;
            }
            if (DialogueController.Instance != null && DialogueController.Instance.IsOpen)
            {
                return;
            }

            DrawTimeLayerTransition();

            Transform activeCharacter = party != null ? party.ActiveCharacter : null;
            Combatant activeHealth = activeCharacter != null
                ? activeCharacter.GetComponent<Combatant>()
                : null;

            if (activeHealth != null)
            {
                DrawHealth(activeHealth);
            }

            int nearbyEnemies = GetNearbyEnemyCount(activeCharacter, 16f);
            int aliveEnemies = encounterState != null
                ? encounterState.AliveEnemyCount
                : 0;
            float scale = UiTheme.Scale;

            if (previousAliveEnemies > 0 && aliveEnemies == 0)
            {
                clearNoticeUntil = Time.unscaledTime + 2.6f;
            }
            previousAliveEnemies = aliveEnemies;

            if (nearbyEnemies > 0)
            {
                Rect threatPanel = new(
                    Screen.width - 236f * scale - 18f * scale,
                    52f * scale,
                    236f * scale,
                    26f * scale
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
                    $"街口封锁 · {nearbyEnemies} 名守卫",
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
            DrawTargetMarker(activeCharacter);
            DrawCombatClearNotice();

            float utilityWidth = 92f * scale;
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
                "J 案卷",
                UiTheme.Button(13)
            ))
            {
                InvestigationJournalController.Instance?.ToggleJournal();
            }
            if (GUI.Button(
                new Rect(
                    right - utilityWidth * 4f - 30f * scale,
                    utilityY,
                    utilityWidth,
                    utilityHeight
                ),
                "B 背包",
                UiTheme.Button(13)
            ))
            {
                InventoryController.Instance?.ToggleInventory();
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
                    "WASD  移动      Shift  冲刺（耗体力）      Ctrl  闪避\n"
                    + "Space  跳跃 / 空中再按展开滑翔      1/2/Tab  切换角色\n"
                    + "鼠标左键  攻击      F  核心技能      R  终结技（满能量）\n"
                    + "按住鼠标右键  旋转镜头\n"
                    + "E  调查 / 对话 / 修复      Q  切换时间层\n"
                    + "J  调查案卷      M  世界地图      Esc  暂停      F2  静音\n"
                    + "B  背包      T  证词对照      V  修痕视界      O  设置\n"
                    + "F3  性能面板      F5/F9  保存/读取\n"
                    + "F1  关闭帮助      R  在结局或队伍倒下后重新开始",
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

        private void DrawTimeLayerTransition()
        {
            if (timeLayerController == null)
            {
                return;
            }

            float age = Time.unscaledTime - timeLayerController.LastChangedAt;
            if (age < 0f || age >= 0.55f)
            {
                return;
            }

            float fade = 1f - age / 0.55f;
            Color tint = timeLayerController.CurrentLayer == TimeLayer.Past
                ? new Color(0.18f, 0.38f, 0.48f, 0.12f * fade)
                : new Color(0.86f, 0.68f, 0.36f, 0.1f * fade);
            UiTheme.DrawRect(
                new Rect(0f, 0f, Screen.width, Screen.height),
                tint
            );

            float scale = UiTheme.Scale;
            GUIStyle style = UiTheme.Label(
                24,
                TextAnchor.MiddleCenter,
                bold: true
            );
            style.normal.textColor = new Color(
                1f,
                0.92f,
                0.72f,
                fade
            );
            GUI.Label(
                new Rect(
                    0f,
                    Screen.height * 0.32f,
                    Screen.width,
                    46f * scale
                ),
                timeLayerController.CurrentLayer == TimeLayer.Past
                    ? "旧 时"
                    : "现 世",
                style
            );
        }

        private void DrawEnemyHealthBars()
        {
            Camera sceneCamera = Camera.main;
            if (sceneCamera == null)
            {
                return;
            }

            IReadOnlyList<Combatant> enemies = CombatRegistry.Enemies;
            for (int index = 0; index < enemies.Count; index++)
            {
                Combatant enemy = enemies[index];
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

                EnemyAI enemyAi = enemy.GetComponent<EnemyAI>();
                EnemyBreakStatus breakStatus =
                    enemy.GetComponent<EnemyBreakStatus>();
                if (breakStatus != null)
                {
                    Rect poiseBackground = new(
                        background.x,
                        background.yMax + 2f * UiTheme.Scale,
                        background.width,
                        4f * UiTheme.Scale
                    );
                    UiTheme.DrawRect(
                        poiseBackground,
                        new Color(0f, 0f, 0f, 0.45f)
                    );
                    UiTheme.DrawRect(
                        new Rect(
                            poiseBackground.x,
                            poiseBackground.y,
                            poiseBackground.width
                                * (breakStatus.IsBroken
                                    ? 1f
                                    : breakStatus.PoiseRatio),
                            poiseBackground.height
                        ),
                        breakStatus.IsBroken
                            ? new Color(1f, 0.72f, 0.24f)
                            : new Color(0.52f, 0.7f, 0.82f)
                    );
                    if (breakStatus.IsBroken)
                    {
                        GUIStyle brokenStyle = UiTheme.Label(
                            9,
                            TextAnchor.MiddleCenter,
                            bold: true
                        );
                        brokenStyle.normal.textColor =
                            new Color(1f, 0.78f, 0.35f);
                        GUI.Label(
                            new Rect(
                                background.x - 18f * UiTheme.Scale,
                                background.yMax + 5f * UiTheme.Scale,
                                background.width + 36f * UiTheme.Scale,
                                18f * UiTheme.Scale
                            ),
                            "破防 · 伤害提升",
                            brokenStyle
                        );
                    }
                }
                if (enemyAi != null && enemyAi.IsWindingUp)
                {
                    Rect warning = new(
                        background.x - 24f * UiTheme.Scale,
                        background.y - 24f * UiTheme.Scale,
                        background.width + 48f * UiTheme.Scale,
                        20f * UiTheme.Scale
                    );
                    UiTheme.DrawRect(
                        warning,
                        new Color(0.14f, 0.045f, 0.02f, 0.94f)
                    );
                    UiTheme.DrawBorder(
                        warning,
                        new Color(1f, 0.6f, 0.2f, 0.9f)
                    );
                    UiTheme.DrawRect(
                        new Rect(
                            warning.x + 1f,
                            warning.yMax - 3f * UiTheme.Scale,
                            (warning.width - 2f) *
                            enemyAi.AttackWindupNormalized,
                            2f * UiTheme.Scale
                        ),
                        new Color(1f, 0.58f, 0.16f)
                    );
                    GUIStyle warningStyle = UiTheme.Label(
                        10,
                        TextAnchor.MiddleCenter,
                        bold: true
                    );
                    warningStyle.normal.textColor =
                        new Color(1f, 0.78f, 0.46f);
                    GUI.Label(warning, "攻击蓄力", warningStyle);
                }
            }
        }

        private int GetNearbyEnemyCount(
            Transform activeCharacter,
            float radius)
        {
            if (activeCharacter == null)
            {
                return 0;
            }

            return CombatRegistry.CountNearbyEnemies(
                activeCharacter.position,
                radius
            );
        }

        private void DrawTargetMarker(Transform activeCharacter)
        {
            if (!TryFindNearestEnemy(
                    activeCharacter,
                    18f,
                    out Combatant target,
                    out float distance)
                || Camera.main == null)
            {
                return;
            }

            Vector3 screenPoint = Camera.main.WorldToScreenPoint(
                target.transform.position + Vector3.up * 1.75f
            );
            if (screenPoint.z <= 0f)
            {
                return;
            }

            float scale = UiTheme.Scale;
            float size = 42f * scale;
            Rect marker = new(
                screenPoint.x - size * 0.5f,
                Screen.height - screenPoint.y - size * 0.5f,
                size,
                size
            );
            Color markerColor = new(1f, 0.69f, 0.3f, 0.96f);
            DrawTargetCorners(marker, markerColor, 3f * scale, 10f * scale);

            Rect labelPanel = new(
                marker.center.x - 79f * scale,
                marker.y - 26f * scale,
                158f * scale,
                21f * scale
            );
            UiTheme.DrawRect(
                labelPanel,
                new Color(0.025f, 0.035f, 0.05f, 0.88f)
            );
            UiTheme.DrawRect(
                new Rect(
                    labelPanel.x,
                    labelPanel.y,
                    2f * scale,
                    labelPanel.height
                ),
                markerColor
            );

            GUIStyle label = UiTheme.Label(
                11,
                TextAnchor.MiddleCenter,
                bold: true
            );
            label.normal.textColor = markerColor;
            string targetLabel = GetTargetLabel(target);
            GUI.Label(
                labelPanel,
                $"锁定 · {targetLabel} · {distance:0.0}m",
                label
            );
        }

        private static string GetTargetLabel(Combatant target)
        {
            string objectName = target.gameObject.name;
            if (objectName.Contains("Dongguan"))
            {
                return "东关巡哨";
            }
            if (objectName.Contains("Caiyi"))
            {
                return "彩衣望风";
            }
            if (objectName.Contains("Support"))
            {
                return "盐仓增援";
            }

            string guardNumber = objectName
                .Replace("Enemy_Patrol_", string.Empty)
                .Trim();
            return string.IsNullOrWhiteSpace(guardNumber)
                ? "巡逻守卫"
                : $"守卫 {guardNumber}";
        }

        private static void DrawTargetCorners(
            Rect rect,
            Color color,
            float thickness,
            float length)
        {
            length = Mathf.Min(length, rect.width * 0.45f);
            UiTheme.DrawRect(
                new Rect(rect.x, rect.y, length, thickness),
                color
            );
            UiTheme.DrawRect(
                new Rect(rect.x, rect.y, thickness, length),
                color
            );
            UiTheme.DrawRect(
                new Rect(rect.xMax - length, rect.y, length, thickness),
                color
            );
            UiTheme.DrawRect(
                new Rect(rect.xMax - thickness, rect.y, thickness, length),
                color
            );
            UiTheme.DrawRect(
                new Rect(rect.x, rect.yMax - thickness, length, thickness),
                color
            );
            UiTheme.DrawRect(
                new Rect(rect.x, rect.yMax - length, thickness, length),
                color
            );
            UiTheme.DrawRect(
                new Rect(
                    rect.xMax - length,
                    rect.yMax - thickness,
                    length,
                    thickness
                ),
                color
            );
            UiTheme.DrawRect(
                new Rect(rect.xMax - thickness, rect.yMax - length, thickness, length),
                color
            );
        }

        private void DrawCombatClearNotice()
        {
            if (Time.unscaledTime >= clearNoticeUntil)
            {
                return;
            }

            float alpha = Mathf.Clamp01(
                (clearNoticeUntil - Time.unscaledTime) / 0.5f
            );
            float scale = UiTheme.Scale;
            Rect panel = new(
                Screen.width * 0.5f - 170f * scale,
                Screen.height * 0.25f,
                340f * scale,
                42f * scale
            );
            UiTheme.DrawRect(
                panel,
                new Color(0.06f, 0.08f, 0.065f, 0.92f * alpha)
            );
            UiTheme.DrawBorder(
                panel,
                new Color(0.86f, 0.7f, 0.34f, alpha),
                2f
            );
            GUIStyle style = UiTheme.Label(
                16,
                TextAnchor.MiddleCenter,
                bold: true
            );
            style.normal.textColor = new Color(1f, 0.86f, 0.52f, alpha);
            GUI.Label(panel, "街口封锁已解除", style);
        }

        private bool TryFindNearestEnemy(
            Transform activeCharacter,
            float radius,
            out Combatant target,
            out float distance)
        {
            if (activeCharacter == null)
            {
                target = null;
                distance = float.MaxValue;
                return false;
            }
            target = CombatTargeting.FindNearestEnemy(
                activeCharacter,
                radius,
                out distance
            );
            return target != null;
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

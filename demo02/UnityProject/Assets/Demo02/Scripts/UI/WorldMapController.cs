using System.Collections;
using System.IO;
using Demo02.Core;
using Demo02.Interaction;
using Demo02.Narrative;
using Demo02.WorldState;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Demo02.UI
{
    public sealed class WorldMapController : MonoBehaviour
    {
        private bool isOpen;
        private float previousTimeScale = 1f;
        private string statusMessage;
        private ScreenFade screenFade;
        private InvestigationState investigationState;

        public static WorldMapController Instance { get; private set; }
        public bool IsOpen => isOpen;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            screenFade = FindAnyObjectByType<ScreenFade>();
            investigationState = FindAnyObjectByType<InvestigationState>();
            if (System.Array.IndexOf(
                System.Environment.GetCommandLineArgs(),
                "--preview-map"
            ) >= 0)
            {
                ToggleMap();
                string capturePath = GetArgumentValue("--capture-map=");
                if (!string.IsNullOrWhiteSpace(capturePath))
                {
                    StartCoroutine(CaptureMapPreview(capturePath));
                }
            }
        }

        private static string GetArgumentValue(string prefix)
        {
            foreach (string argument in System.Environment.GetCommandLineArgs())
            {
                if (argument.StartsWith(prefix, System.StringComparison.Ordinal))
                {
                    return argument.Substring(prefix.Length);
                }
            }

            return string.Empty;
        }

        private IEnumerator CaptureMapPreview(string path)
        {
            yield return new WaitForSecondsRealtime(0.6f);
            yield return new WaitForEndOfFrame();
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            ScreenCapture.CaptureScreenshot(path);
            Debug.Log($"World map preview captured to {path}");
            yield return null;
            if (System.Array.IndexOf(
                    System.Environment.GetCommandLineArgs(),
                    "--capture-exit"
                ) >= 0)
            {
                Application.Quit();
            }
        }

        private void Update()
        {
            if (Keyboard.current == null)
            {
                return;
            }

            if (isOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                ToggleMap();
                return;
            }

            if (!Keyboard.current.mKey.wasPressedThisFrame)
            {
                return;
            }

            if (DialogueController.Instance != null && DialogueController.Instance.IsOpen)
            {
                return;
            }

            if (!isOpen
                && (Time.timeScale <= 0f
                    || (screenFade != null && screenFade.IsTransitioning)))
            {
                return;
            }

            ToggleMap();
        }

        private void OnDisable()
        {
            if (isOpen)
            {
                Time.timeScale = previousTimeScale;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnGUI()
        {
            if (!isOpen)
            {
                return;
            }

            GUI.depth = -100;
            UiTheme.DrawRect(
                new Rect(0f, 0f, Screen.width, Screen.height),
                UiTheme.Ink
            );

            float scale = UiTheme.Scale;
            GUIStyle eyebrowStyle = UiTheme.Label(
                12,
                TextAnchor.MiddleCenter,
                bold: true
            );
            eyebrowStyle.normal.textColor = UiTheme.Accent;
            GUI.Label(
                new Rect(0f, 38f * scale, Screen.width, 24f * scale),
                "GUANGLING · DISTRICT ROUTE",
                eyebrowStyle
            );
            GUI.Label(
                new Rect(0f, 66f * scale, Screen.width, 52f * scale),
                "广陵街区图",
                UiTheme.Label(34, TextAnchor.MiddleCenter, bold: true)
            );
            GUI.Label(
                new Rect(0f, 116f * scale, Screen.width, 32f * scale),
                "选择落脚点，调查、队伍与时间层状态都会保留",
                UiTheme.Label(15, TextAnchor.MiddleCenter)
            );

            string currentAreaId = WorldRuntime.Instance != null
                ? WorldRuntime.Instance.CurrentAreaId
                : string.Empty;
            float contentTop = 166f * scale;
            float gap = 24f * scale;
            float margin = 44f * scale;
            float rightWidth = Mathf.Min(360f * scale, Screen.width * 0.29f);
            float mapWidth = Mathf.Min(
                Screen.width * 0.61f,
                Screen.width - margin * 2f - gap - rightWidth
            );
            float contentWidth = mapWidth + gap + rightWidth;
            float startX = (Screen.width - contentWidth) * 0.5f;
            float mapHeight = Mathf.Clamp(
                Screen.height - contentTop - 176f * scale,
                330f * scale,
                440f * scale
            );
            Rect mapRect = new(startX, contentTop, mapWidth, mapHeight);
            DrawSchematicMap(mapRect, currentAreaId);

            float cardX = mapRect.xMax + gap;
            float cardHeight = 95f * scale;
            float cardGap = 10f * scale;
            DrawAreaCard(
                new Rect(cardX, contentTop, rightWidth, cardHeight),
                "01",
                "官署前街",
                "调查起点 · 旧仓入口",
                "official-street",
                currentAreaId,
                "PlayerSpawn",
                "官署前街"
            );
            DrawAreaCard(
                new Rect(
                    cardX,
                    contentTop + cardHeight + cardGap,
                    rightWidth,
                    cardHeight
                ),
                "02",
                "东关街",
                "市集主轴 · 茶肆与商铺",
                "dongguan-street",
                currentAreaId,
                "DongguanSpawn",
                "东关街"
            );
            DrawAreaCard(
                new Rect(
                    cardX,
                    contentTop + (cardHeight + cardGap) * 2f,
                    rightWidth,
                    cardHeight
                ),
                "03",
                "彩衣街",
                "生活商街 · 裁衣与小吃",
                "caiyi-street",
                currentAreaId,
                "CaiyiSpawn",
                "彩衣街"
            );

            float footerY = mapRect.yMax + 18f * scale;
            Rect footer = new(
                mapRect.x,
                footerY,
                contentWidth,
                58f * scale
            );
            UiTheme.DrawRect(footer, UiTheme.Panel);
            UiTheme.DrawBorder(footer, new Color(1f, 1f, 1f, 0.12f));

            string progress = investigationState != null
                ? $"线索 {investigationState.CollectedClueCount}/{investigationState.RequiredClueCount}"
                    + $"    时间锚点 {investigationState.StabilizedTimeAnchorCount}/{investigationState.RequiredTimeAnchorCount}"
                : "调查状态不可用";
            GUI.Label(
                new Rect(
                    footer.x + 18f * scale,
                    footer.y,
                    footer.width * 0.62f,
                    footer.height
                ),
                progress,
                UiTheme.Label(14)
            );
            GUI.Label(
                new Rect(
                    footer.x + footer.width * 0.62f,
                    footer.y,
                    footer.width * 0.38f - 18f * scale,
                    footer.height
                ),
                "[ M / Esc ]  返回探索",
                UiTheme.Label(14, TextAnchor.MiddleRight, bold: true)
            );

            float closeY = footerY + footer.height + 18f * scale;
            if (GUI.Button(
                new Rect(
                    (Screen.width - 260f * scale) * 0.5f,
                    closeY,
                    260f * scale,
                    44f * scale
                ),
                "关闭地图  [M / Esc]",
                UiTheme.Button(15)
            ))
            {
                ToggleMap();
            }

            if (!string.IsNullOrWhiteSpace(statusMessage))
            {
                GUI.Label(
                    new Rect(0f, Screen.height - 72f, Screen.width, 32f),
                    statusMessage,
                    UiTheme.Label(15, TextAnchor.MiddleCenter)
                );
            }
        }

        private void DrawSchematicMap(Rect rect, string currentAreaId)
        {
            float scale = UiTheme.Scale;
            UiTheme.DrawRect(rect, new Color(0.045f, 0.055f, 0.07f, 0.96f));
            UiTheme.DrawBorder(
                rect,
                new Color(0.82f, 0.74f, 0.56f, 0.2f)
            );

            GUI.Label(
                new Rect(
                    rect.x + 18f * scale,
                    rect.y + 12f * scale,
                    180f * scale,
                    24f * scale
                ),
                "街区路线 / ROUTE",
                UiTheme.Label(12, TextAnchor.MiddleLeft, bold: true)
            );
            GUIStyle north = UiTheme.Label(11, TextAnchor.MiddleRight);
            north.normal.textColor = UiTheme.Muted;
            GUI.Label(
                new Rect(
                    rect.xMax - 92f * scale,
                    rect.y + 12f * scale,
                    72f * scale,
                    24f * scale
                ),
                "N  ↑",
                north
            );

            Rect draw = new(
                rect.x + 28f * scale,
                rect.y + 48f * scale,
                rect.width - 56f * scale,
                rect.height - 74f * scale
            );
            Vector2 official = new(
                draw.x + draw.width * 0.25f,
                draw.y + draw.height * 0.76f
            );
            Vector2 dongguan = new(
                draw.x + draw.width * 0.52f,
                draw.y + draw.height * 0.49f
            );
            Vector2 caiyi = new(
                draw.x + draw.width * 0.76f,
                draw.y + draw.height * 0.22f
            );
            Vector2 warehouse = new(
                draw.x + draw.width * 0.11f,
                draw.y + draw.height * 0.48f
            );

            DrawRouteLine(official, dongguan, 24f * scale);
            DrawRouteLine(dongguan, caiyi, 22f * scale);
            DrawRouteLine(official, warehouse, 16f * scale);
            DrawLine(
                warehouse,
                new Vector2(warehouse.x - 26f * scale, warehouse.y),
                new Color(0.78f, 0.7f, 0.48f, 0.34f),
                2f * scale
            );

            GUIStyle warehouseLabel = UiTheme.Label(11, TextAnchor.MiddleLeft);
            warehouseLabel.normal.textColor = UiTheme.Muted;
            GUI.Label(
                new Rect(
                    warehouse.x - 20f * scale,
                    warehouse.y + 18f * scale,
                    132f * scale,
                    28f * scale
                ),
                "旧仓内院",
                warehouseLabel
            );

            DrawMapNode(
                official,
                "01",
                "官署前街",
                "official-street",
                currentAreaId,
                "PlayerSpawn",
                "官署前街",
                new Vector2(-20f * scale, 23f * scale)
            );
            DrawMapNode(
                dongguan,
                "02",
                "东关街",
                "dongguan-street",
                currentAreaId,
                "DongguanSpawn",
                "东关街",
                new Vector2(-12f * scale, 23f * scale)
            );
            DrawMapNode(
                caiyi,
                "03",
                "彩衣街",
                "caiyi-street",
                currentAreaId,
                "CaiyiSpawn",
                "彩衣街",
                new Vector2(-12f * scale, 23f * scale)
            );
            DrawWorldContentMarkers(draw, scale);

            string currentName = currentAreaId switch
            {
                "official-street" => "官署前街",
                "dongguan-street" => "东关街",
                "caiyi-street" => "彩衣街",
                _ => "广陵"
            };
            GUIStyle legend = UiTheme.Label(11, TextAnchor.MiddleLeft);
            legend.normal.textColor = UiTheme.Muted;
            GUI.Label(
                new Rect(
                    draw.x,
                    draw.yMax - 22f * scale,
                    draw.width,
                    22f * scale
                ),
                $"当前位置：{currentName}    点击地图节点可快速旅行",
                legend
            );
        }

        private void DrawMapNode(
            Vector2 position,
            string number,
            string areaName,
            string areaId,
            string currentAreaId,
            string spawnName,
            string displayName,
            Vector2 labelOffset)
        {
            float scale = UiTheme.Scale;
            bool isCurrent = areaId == currentAreaId;
            bool unlocked = IsSpawnUnlocked(spawnName);
            float size = 28f * scale;
            Rect hit = new(
                position.x - size * 0.5f,
                position.y - size * 0.5f,
                size,
                size
            );

            GUI.enabled = isCurrent || unlocked;
            if (GUI.Button(hit, GUIContent.none, GUIStyle.none))
            {
                if (isCurrent)
                {
                    statusMessage = $"你已经位于{displayName}。";
                }
                else
                {
                    TravelTo(spawnName, areaId, displayName);
                }
            }
            GUI.enabled = true;

            UiTheme.DrawRect(
                new Rect(
                    hit.x - 4f * scale,
                    hit.y - 4f * scale,
                    hit.width + 8f * scale,
                    hit.height + 8f * scale
                ),
                new Color(0.04f, 0.05f, 0.065f, 0.9f)
            );
            UiTheme.DrawRect(
                hit,
                isCurrent
                    ? UiTheme.Accent
                    : unlocked
                        ? new Color(0.2f, 0.24f, 0.27f)
                        : new Color(0.1f, 0.11f, 0.12f)
            );
            UiTheme.DrawBorder(
                hit,
                isCurrent ? Color.white : new Color(0.8f, 0.75f, 0.65f, 0.4f),
                isCurrent ? 2f : 1f
            );
            GUIStyle numberStyle = UiTheme.Label(
                10,
                TextAnchor.MiddleCenter,
                bold: true
            );
            numberStyle.normal.textColor = isCurrent ? UiTheme.Ink : UiTheme.Text;
            GUI.Label(hit, number, numberStyle);

            Rect label = new(
                position.x + labelOffset.x,
                position.y + labelOffset.y,
                120f * scale,
                24f * scale
            );
            GUIStyle areaStyle = UiTheme.Label(
                13,
                TextAnchor.MiddleCenter,
                bold: isCurrent
            );
            areaStyle.normal.textColor = isCurrent
                ? UiTheme.Accent
                : unlocked
                    ? UiTheme.Text
                    : UiTheme.Muted;
            GUI.Label(
                label,
                unlocked || isCurrent ? areaName : $"{areaName} · 未记录",
                areaStyle
            );

            if (isCurrent)
            {
                UiTheme.DrawRect(
                    new Rect(
                        position.x + labelOffset.x + 46f * scale,
                        label.yMax,
                        28f * scale,
                        2f * scale
                    ),
                    UiTheme.Accent
                );
            }
        }

        private static void DrawWorldContentMarkers(Rect draw, float scale)
        {
            WorldContentTracker tracker = WorldContentTracker.Instance;
            if (tracker == null)
            {
                return;
            }

            foreach (WorldContentEntry entry in tracker.Entries)
            {
                WorldContentStatus status = tracker.GetStatus(entry);
                Color color = status switch
                {
                    WorldContentStatus.InProgress =>
                        new Color(0.94f, 0.72f, 0.28f),
                    WorldContentStatus.ReadyToTurnIn =>
                        new Color(0.42f, 0.82f, 0.58f),
                    WorldContentStatus.Completed =>
                        new Color(0.42f, 0.55f, 0.5f),
                    _ => new Color(0.55f, 0.58f, 0.6f)
                };
                Vector2 position = new(
                    draw.x + draw.width * entry.MapX,
                    draw.y + draw.height * entry.MapY
                );
                float size = 12f * scale;
                Rect marker = new(
                    position.x - size * 0.5f,
                    position.y - size * 0.5f,
                    size,
                    size
                );
                UiTheme.DrawRect(marker, color);
                UiTheme.DrawBorder(
                    marker,
                    new Color(1f, 1f, 1f, 0.65f)
                );

                GUI.Label(
                    new Rect(
                        position.x + 8f * scale,
                        position.y - 10f * scale,
                        132f * scale,
                        22f * scale
                    ),
                    entry.Title,
                    UiTheme.Label(10)
                );
            }
        }

        private static void DrawRouteLine(
            Vector2 start,
            Vector2 end,
            float width)
        {
            DrawLine(
                start,
                end,
                new Color(0.66f, 0.6f, 0.48f, 0.2f),
                width
            );
            DrawLine(
                start,
                end,
                new Color(0.21f, 0.23f, 0.24f, 0.95f),
                width - 5f * UiTheme.Scale
            );
            DrawLine(
                start,
                end,
                new Color(0.58f, 0.53f, 0.43f, 0.3f),
                1.5f * UiTheme.Scale
            );
        }

        private static void DrawLine(
            Vector2 start,
            Vector2 end,
            Color color,
            float thickness)
        {
            Vector2 delta = end - start;
            float length = delta.magnitude;
            if (length <= 0.01f)
            {
                return;
            }

            Matrix4x4 previous = GUI.matrix;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            GUIUtility.RotateAroundPivot(angle, start);
            UiTheme.DrawRect(
                new Rect(
                    start.x,
                    start.y - thickness * 0.5f,
                    length,
                    thickness
                ),
                color
            );
            GUI.matrix = previous;
        }

        private void DrawAreaCard(
            Rect rect,
            string number,
            string areaName,
            string description,
            string areaId,
            string currentAreaId,
            string spawnName,
            string displayName)
        {
            bool isCurrent = areaId == currentAreaId;
            float scale = UiTheme.Scale;
            bool unlocked = IsSpawnUnlocked(spawnName);

            string buttonText =
                $"{number}   {areaName}\n{description}\n"
                + (isCurrent
                    ? "当前所在"
                    : unlocked
                        ? "点击快速旅行  →"
                        : "尚未记录地区锚点");
            GUI.enabled = isCurrent || unlocked;
            if (GUI.Button(rect, buttonText, UiTheme.Button(16)))
            {
                if (isCurrent)
                {
                    statusMessage = $"你已经位于{displayName}。";
                }
                else
                {
                    TravelTo(spawnName, areaId, displayName);
                }
            }
            GUI.enabled = true;

            UiTheme.DrawRect(
                new Rect(rect.x, rect.y, rect.width, 4f * scale),
                isCurrent ? UiTheme.Accent : UiTheme.PanelHover
            );
            UiTheme.DrawBorder(
                rect,
                isCurrent
                    ? UiTheme.Accent
                    : unlocked
                        ? new Color(1f, 1f, 1f, 0.14f)
                        : new Color(1f, 1f, 1f, 0.06f),
                isCurrent ? 2f : 1f
            );
        }

        private void TravelTo(string spawnName, string areaId, string areaName)
        {
            if (!IsSpawnUnlocked(spawnName))
            {
                statusMessage = $"{areaName}尚未记录锚点。";
                return;
            }

            GameAudio.PlayPortal();
            GameObject spawn = GameObject.Find(spawnName);
            if (spawn == null)
            {
                statusMessage = $"找不到地点：{areaName}";
                return;
            }

            ToggleMap();
            if (screenFade != null)
            {
                if (!screenFade.PlayTransition(() =>
                {
                    LocalWorldPortal.TeleportParty(
                        spawn.transform,
                        areaId,
                        areaName,
                        out _
                    );
                }))
                {
                    statusMessage = "当前正在转场，请稍候。";
                }

                return;
            }

            if (!LocalWorldPortal.TeleportParty(
                spawn.transform,
                areaId,
                areaName,
                out string message))
            {
                statusMessage = "当前没有可传送的角色。";
            }
        }

        private static bool IsSpawnUnlocked(string spawnName)
        {
            GameObject spawn = GameObject.Find(spawnName);
            WorldSpawnPoint spawnPoint = spawn != null
                ? spawn.GetComponent<WorldSpawnPoint>()
                : null;
            if (spawnPoint == null || !spawnPoint.IsFastTravelAnchor)
            {
                return true;
            }

            return WorldRuntime.Instance == null
                || WorldRuntime.Instance.IsAnchorUnlocked(
                    spawnPoint.SpawnId
                );
        }

        public void ToggleMap()
        {
            if (!isOpen && Time.timeScale <= 0f)
            {
                return;
            }

            isOpen = !isOpen;
            statusMessage = string.Empty;
            if (isOpen)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Time.timeScale = previousTimeScale;
            }
        }
    }
}

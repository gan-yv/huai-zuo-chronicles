using System.Collections;
using System.Collections.Generic;
using System.IO;
using Demo02.Core;
using Demo02.Interaction;
using Demo02.Narrative;
using Demo02.WorldState;
using UnityEngine;
using UnityEngine.UI;

namespace Demo02.UI
{
    public sealed class WorldMapController : MonoBehaviour
    {
        private sealed class MapNodeView
        {
            public Button Button;
            public Image Fill;
            public Outline Border;
            public Text Number;
            public Text Label;
            public string AreaId;
            public string SpawnName;
            public string DisplayName;
        }

        private sealed class AreaCardView
        {
            public Button Button;
            public Image Top;
            public Outline Border;
            public Text Label;
            public string AreaId;
            public string SpawnName;
            public string DisplayName;
        }

        private sealed class MarkerView
        {
            public GameObject Root;
            public Image Dot;
            public Text Label;
        }

        private bool isOpen;
        private float previousTimeScale = 1f;
        private string statusMessage;
        private ScreenFade screenFade;
        private InvestigationState investigationState;
        private GameObject canvasObject;
        private Canvas canvas;
        private Text footerProgress;
        private Text statusText;
        private Text currentPosition;
        private readonly MapNodeView[] nodes = new MapNodeView[3];
        private readonly AreaCardView[] cards = new AreaCardView[3];
        private readonly List<MarkerView> markers = new();

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
            EnsureUi();
        }

        private void Start()
        {
            screenFade = FindAnyObjectByType<ScreenFade>();
            investigationState =
                FindAnyObjectByType<InvestigationState>();
            if (System.Array.IndexOf(
                Demo02.Core.RuntimeLaunchOptions.Arguments,
                "--preview-map"
            ) >= 0)
            {
                ToggleMap();
                string capturePath =
                    GetArgumentValue("--capture-map=");
                if (!string.IsNullOrWhiteSpace(capturePath))
                {
                    StartCoroutine(CaptureMapPreview(capturePath));
                }
            }
        }

        private void Update()
        {
            if (isOpen && Demo02Input.CancelPressed)
            {
                ToggleMap();
                return;
            }
            if (!Demo02Input.MapPressed)
            {
                return;
            }
            if (GameplayUiState.HasOpenModalExcept(this))
            {
                return;
            }
            if (!isOpen
                && (Time.timeScale <= 0f
                    || (screenFade != null
                        && screenFade.IsTransitioning)))
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
                isOpen = false;
            }
            if (canvas != null)
            {
                canvas.enabled = false;
            }
            GameplayUiState.UnregisterModal(this);
        }

        private void OnDestroy()
        {
            GameplayUiState.UnregisterModal(this);
            if (Instance == this)
            {
                Instance = null;
            }
            if (canvasObject == null)
            {
                return;
            }
            if (Application.isPlaying)
            {
                Destroy(canvasObject);
            }
            else
            {
                DestroyImmediate(canvasObject);
            }
            canvasObject = null;
        }

        public void ToggleMap()
        {
            if (!isOpen
                && (Time.timeScale <= 0f
                    || GameplayUiState.HasOpenModalExcept(this)))
            {
                return;
            }
            isOpen = !isOpen;
            statusMessage = string.Empty;
            canvas.enabled = isOpen;
            if (isOpen)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                GameplayUiState.RegisterModal(this);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                RefreshMap();
            }
            else
            {
                Time.timeScale = previousTimeScale;
                GameplayUiState.UnregisterModal(this);
            }
        }

        private void EnsureUi()
        {
            if (canvasObject != null)
            {
                return;
            }
            RectTransform root = HudUiFactory.CreateCanvas(
                transform,
                "WorldMapCanvas",
                101
            );
            canvasObject = root.gameObject;
            canvas = root.GetComponent<Canvas>();
            canvas.enabled = false;
            Image background = HudUiFactory.CreateImage(
                root,
                "Background",
                UiTheme.Ink
            );
            HudUiFactory.Stretch(background.rectTransform);

            Text eyebrow = CreateText(
                root,
                "Eyebrow",
                "GUANGLING · DISTRICT ROUTE",
                12,
                new Vector2(0f, -38f),
                new Vector2(1920f, 24f),
                TextAnchor.MiddleCenter,
                bold: true
            );
            eyebrow.color = UiTheme.Accent;
            CreateText(
                root,
                "Title",
                "广陵街区图",
                34,
                new Vector2(0f, -66f),
                new Vector2(1920f, 52f),
                TextAnchor.MiddleCenter,
                bold: true
            );
            CreateText(
                root,
                "Subtitle",
                "选择落脚点，调查、队伍与时间层状态都会保留",
                15,
                new Vector2(0f, -116f),
                new Vector2(1920f, 32f),
                TextAnchor.MiddleCenter
            );

            RectTransform map = HudUiFactory.CreatePanel(
                root,
                "MapPanel",
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(218f, -166f),
                new Vector2(1120f, 440f),
                new Color(0.045f, 0.055f, 0.07f, 0.96f)
            );
            Outline mapOutline = map.gameObject.AddComponent<Outline>();
            mapOutline.effectDistance = new Vector2(1f, -1f);
            mapOutline.effectColor =
                new Color(0.82f, 0.74f, 0.56f, 0.2f);
            CreateText(
                map,
                "RouteLabel",
                "街区路线 / ROUTE",
                12,
                new Vector2(18f, -12f),
                new Vector2(180f, 24f),
                TextAnchor.MiddleLeft,
                bold: true
            );
            Text north = CreateText(
                map,
                "North",
                "N  ↑",
                11,
                new Vector2(1028f, -12f),
                new Vector2(72f, 24f),
                TextAnchor.MiddleRight
            );
            north.color = UiTheme.Muted;

            CreateRouteLine(
                map,
                new Vector2(294f, 326f),
                new Vector2(581f, 227f)
            );
            CreateRouteLine(
                map,
                new Vector2(581f, 227f),
                new Vector2(837f, 129f)
            );
            CreateRouteLine(
                map,
                new Vector2(294f, 326f),
                new Vector2(145f, 224f)
            );
            CreateText(
                map,
                "WarehouseLabel",
                "旧仓内院",
                11,
                new Vector2(124f, -242f),
                new Vector2(132f, 28f),
                TextAnchor.MiddleLeft
            ).color = UiTheme.Muted;

            nodes[0] = CreateNode(
                map,
                "01",
                new Vector2(254f, -303f),
                new Vector2(274f, -326f),
                "official-street",
                "PlayerSpawn",
                "官署前街"
            );
            nodes[1] = CreateNode(
                map,
                "02",
                new Vector2(541f, -204f),
                new Vector2(569f, -227f),
                "dongguan-street",
                "DongguanSpawn",
                "东关街"
            );
            nodes[2] = CreateNode(
                map,
                "03",
                new Vector2(797f, -106f),
                new Vector2(825f, -129f),
                "caiyi-street",
                "CaiyiSpawn",
                "彩衣街"
            );
            currentPosition = CreateText(
                map,
                "CurrentPosition",
                string.Empty,
                11,
                new Vector2(28f, -394f),
                new Vector2(1064f, 22f),
                TextAnchor.MiddleLeft
            );
            currentPosition.color = UiTheme.Muted;

            cards[0] = CreateAreaCard(
                root,
                "01",
                "官署前街",
                "调查起点 · 旧仓入口",
                new Vector2(1362f, -166f),
                "official-street",
                "PlayerSpawn",
                "官署前街"
            );
            cards[1] = CreateAreaCard(
                root,
                "02",
                "东关街",
                "市集主轴 · 茶肆与商铺",
                new Vector2(1362f, -271f),
                "dongguan-street",
                "DongguanSpawn",
                "东关街"
            );
            cards[2] = CreateAreaCard(
                root,
                "03",
                "彩衣街",
                "生活商街 · 裁衣与小吃",
                new Vector2(1362f, -376f),
                "caiyi-street",
                "CaiyiSpawn",
                "彩衣街"
            );

            RectTransform footer = HudUiFactory.CreatePanel(
                root,
                "Footer",
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(218f, -624f),
                new Vector2(1484f, 58f),
                UiTheme.Panel
            );
            Outline footerOutline =
                footer.gameObject.AddComponent<Outline>();
            footerOutline.effectDistance = new Vector2(1f, -1f);
            footerOutline.effectColor =
                new Color(1f, 1f, 1f, 0.12f);
            footerProgress = CreateText(
                footer,
                "Progress",
                string.Empty,
                14,
                new Vector2(18f, 0f),
                new Vector2(920f, 58f),
                TextAnchor.MiddleLeft
            );
            Text returnHint = CreateText(
                footer,
                "ReturnHint",
                "[ M / Esc ]  返回探索",
                14,
                new Vector2(940f, 0f),
                new Vector2(526f, 58f),
                TextAnchor.MiddleRight,
                bold: true
            );
            returnHint.color = UiTheme.Muted;

            CreateButton(
                root,
                "CloseButton",
                "关闭地图  [M / Esc]",
                new Vector2(830f, -700f),
                new Vector2(260f, 44f),
                ToggleMap
            );
            statusText = CreateText(
                root,
                "Status",
                string.Empty,
                15,
                new Vector2(0f, -1000f),
                new Vector2(1920f, 32f),
                TextAnchor.MiddleCenter
            );
        }

        private MapNodeView CreateNode(
            Transform parent,
            string number,
            Vector2 labelPosition,
            Vector2 nodePosition,
            string areaId,
            string spawnName,
            string displayName)
        {
            RectTransform hit = HudUiFactory.CreatePanel(
                parent,
                "MapNode" + number,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                nodePosition,
                new Vector2(28f, 28f),
                new Color(0.2f, 0.24f, 0.27f),
                raycastTarget: true
            );
            Button button = hit.gameObject.AddComponent<Button>();
            button.targetGraphic = hit.GetComponent<Image>();
            Outline border = hit.gameObject.AddComponent<Outline>();
            border.effectDistance = new Vector2(1f, -1f);
            border.useGraphicAlpha = true;
            border.effectColor =
                new Color(0.8f, 0.75f, 0.65f, 0.4f);
            Text count = HudUiFactory.CreateText(
                hit,
                "Number",
                number,
                10,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                TextAnchor.MiddleCenter,
                bold: true
            );
            Text label = CreateText(
                parent,
                "NodeLabel" + number,
                displayName,
                13,
                labelPosition,
                new Vector2(120f, 24f),
                TextAnchor.MiddleCenter,
                bold: true
            );
            MapNodeView view = new()
            {
                Button = button,
                Fill = hit.GetComponent<Image>(),
                Border = border,
                Number = count,
                Label = label,
                AreaId = areaId,
                SpawnName = spawnName,
                DisplayName = displayName
            };
            button.onClick.AddListener(
                () => ActivateArea(
                    view.SpawnName,
                    view.AreaId,
                    view.DisplayName
                )
            );
            return view;
        }

        private AreaCardView CreateAreaCard(
            Transform parent,
            string number,
            string name,
            string description,
            Vector2 position,
            string areaId,
            string spawnName,
            string displayName)
        {
            RectTransform card = HudUiFactory.CreatePanel(
                parent,
                "AreaCard" + number,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                position,
                new Vector2(340f, 95f),
                UiTheme.Ink,
                raycastTarget: true
            );
            Button button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            Outline border = card.gameObject.AddComponent<Outline>();
            border.effectDistance = new Vector2(1f, -1f);
            border.useGraphicAlpha = true;
            Image top = HudUiFactory.CreateImage(
                card,
                "Top",
                UiTheme.PanelHover
            );
            top.rectTransform.anchorMin = new Vector2(0f, 1f);
            top.rectTransform.anchorMax = new Vector2(1f, 1f);
            top.rectTransform.pivot = new Vector2(0.5f, 1f);
            top.rectTransform.anchoredPosition = Vector2.zero;
            top.rectTransform.sizeDelta = new Vector2(0f, 4f);
            Text label = CreateText(
                card,
                "Label",
                $"{number}   {name}\n{description}\n点击快速旅行",
                16,
                new Vector2(16f, 10f),
                new Vector2(308f, 75f),
                TextAnchor.MiddleLeft,
                bold: true
            );
            AreaCardView view = new()
            {
                Button = button,
                Top = top,
                Border = border,
                Label = label,
                AreaId = areaId,
                SpawnName = spawnName,
                DisplayName = displayName
            };
            button.onClick.AddListener(
                () => ActivateArea(
                    view.SpawnName,
                    view.AreaId,
                    view.DisplayName
                )
            );
            return view;
        }

        private void CreateRouteLine(
            Transform parent,
            Vector2 start,
            Vector2 end)
        {
            CreateLine(
                parent,
                start,
                end,
                new Color(0.66f, 0.6f, 0.48f, 0.2f),
                24f
            );
            CreateLine(
                parent,
                start,
                end,
                new Color(0.21f, 0.23f, 0.24f, 0.95f),
                19f
            );
            CreateLine(
                parent,
                start,
                end,
                new Color(0.58f, 0.53f, 0.43f, 0.3f),
                2f
            );
        }

        private static void CreateLine(
            Transform parent,
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
            RectTransform line = HudUiFactory.CreatePanel(
                parent,
                "Route",
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 0.5f),
                start,
                new Vector2(length, thickness),
                color
            );
            line.localRotation = Quaternion.Euler(
                0f,
                0f,
                -Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg
            );
        }

        private static Text CreateText(
            Transform parent,
            string name,
            string value,
            int size,
            Vector2 position,
            Vector2 dimensions,
            TextAnchor alignment,
            bool bold = false)
        {
            return HudUiFactory.CreateText(
                parent,
                name,
                value,
                size,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                position,
                dimensions,
                alignment,
                bold
            );
        }

        private static Button CreateButton(
            Transform parent,
            string name,
            string value,
            Vector2 position,
            Vector2 dimensions,
            UnityEngine.Events.UnityAction action)
        {
            RectTransform rect = HudUiFactory.CreatePanel(
                parent,
                name,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                position,
                dimensions,
                UiTheme.Panel,
                raycastTarget: true
            );
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            button.onClick.AddListener(action);
            HudUiFactory.CreateText(
                rect,
                "Label",
                value,
                15,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                TextAnchor.MiddleCenter,
                true
            );
            return button;
        }

        private void RefreshMap()
        {
            string currentAreaId = WorldRuntime.Instance != null
                ? WorldRuntime.Instance.CurrentAreaId
                : string.Empty;
            foreach (MapNodeView node in nodes)
            {
                bool current = node.AreaId == currentAreaId;
                bool unlocked = IsSpawnUnlocked(node.SpawnName);
                node.Button.interactable = current || unlocked;
                node.Fill.color = current
                    ? UiTheme.Accent
                    : unlocked
                        ? new Color(0.2f, 0.24f, 0.27f)
                        : new Color(0.1f, 0.11f, 0.12f);
                node.Border.effectColor = current
                    ? Color.white
                    : new Color(0.8f, 0.75f, 0.65f, 0.4f);
                node.Number.color = current
                    ? UiTheme.Ink
                    : UiTheme.Text;
                node.Label.text = unlocked || current
                    ? node.DisplayName
                    : node.DisplayName + " · 未记录";
                node.Label.color = current
                    ? UiTheme.Accent
                    : unlocked
                        ? UiTheme.Text
                        : UiTheme.Muted;
            }

            foreach (AreaCardView card in cards)
            {
                bool current = card.AreaId == currentAreaId;
                bool unlocked = IsSpawnUnlocked(card.SpawnName);
                card.Button.interactable = current || unlocked;
                card.Top.color = current
                    ? UiTheme.Accent
                    : UiTheme.PanelHover;
                card.Border.effectColor = current
                    ? UiTheme.Accent
                    : unlocked
                        ? new Color(1f, 1f, 1f, 0.14f)
                        : new Color(1f, 1f, 1f, 0.06f);
                card.Label.text = current
                    ? card.Label.text.Split('\n')[0] + "\n"
                        + card.Label.text.Split('\n')[1]
                        + "\n当前所在"
                    : unlocked
                        ? card.Label.text.Split('\n')[0] + "\n"
                            + card.Label.text.Split('\n')[1]
                            + "\n点击快速旅行  →"
                        : card.Label.text.Split('\n')[0] + "\n"
                            + card.Label.text.Split('\n')[1]
                            + "\n尚未记录地区锚点";
            }

            string currentName = currentAreaId switch
            {
                "official-street" => "官署前街",
                "dongguan-street" => "东关街",
                "caiyi-street" => "彩衣街",
                _ => "广陵"
            };
            currentPosition.text =
                $"当前位置：{currentName}    点击地图节点可快速旅行";
            footerProgress.text = investigationState != null
                ? $"线索 {investigationState.CollectedClueCount}/"
                    + $"{investigationState.RequiredClueCount}"
                    + $"    时间锚点 "
                    + $"{investigationState.StabilizedTimeAnchorCount}/"
                    + $"{investigationState.RequiredTimeAnchorCount}"
                : "调查状态不可用";
            RefreshWorldMarkers();
        }

        private void RefreshWorldMarkers()
        {
            WorldContentTracker tracker = WorldContentTracker.Instance;
            IReadOnlyList<WorldContentEntry> entries =
                tracker != null ? tracker.Entries : null;
            int count = entries?.Count ?? 0;
            while (markers.Count < count)
            {
                RectTransform dot = HudUiFactory.CreatePanel(
                    canvasObject.transform,
                    "WorldMarker" + markers.Count,
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    new Vector2(12f, 12f),
                    new Color(0.55f, 0.58f, 0.6f)
                );
                Text label = CreateText(
                    dot,
                    "Label",
                    string.Empty,
                    10,
                    new Vector2(20f, -11f),
                    new Vector2(132f, 22f),
                    TextAnchor.MiddleLeft
                );
                markers.Add(new MarkerView
                {
                    Root = dot.gameObject,
                    Dot = dot.GetComponent<Image>(),
                    Label = label
                });
            }
            for (int index = 0; index < markers.Count; index++)
            {
                MarkerView marker = markers[index];
                marker.Root.SetActive(index < count);
                if (index >= count)
                {
                    continue;
                }
                WorldContentEntry entry = entries[index];
                WorldContentStatus status =
                    tracker.GetStatus(entry);
                marker.Dot.color = status switch
                {
                    WorldContentStatus.InProgress =>
                        new Color(0.94f, 0.72f, 0.28f),
                    WorldContentStatus.ReadyToTurnIn =>
                        new Color(0.42f, 0.82f, 0.58f),
                    WorldContentStatus.Completed =>
                        new Color(0.42f, 0.55f, 0.5f),
                    _ => new Color(0.55f, 0.58f, 0.6f)
                };
                marker.Root.transform.position = new Vector3(
                    218f + 28f + (1120f - 56f) * entry.MapX,
                    1080f - 166f - 48f
                        - (440f - 74f) * entry.MapY,
                    0f
                );
                marker.Label.text = entry.Title;
            }
        }

        private void ActivateArea(
            string spawnName,
            string areaId,
            string displayName)
        {
            string currentAreaId = WorldRuntime.Instance != null
                ? WorldRuntime.Instance.CurrentAreaId
                : string.Empty;
            if (areaId == currentAreaId)
            {
                statusMessage = $"你已经位于{displayName}。";
            }
            else
            {
                TravelTo(spawnName, areaId, displayName);
            }
            HudUiFactory.SetText(statusText, statusMessage);
        }

        private void TravelTo(
            string spawnName,
            string areaId,
            string areaName)
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
                out _
            ))
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

        private static string GetArgumentValue(string prefix)
        {
            foreach (string argument in
                     Demo02.Core.RuntimeLaunchOptions.Arguments)
            {
                if (argument.StartsWith(
                        prefix,
                        System.StringComparison.Ordinal
                    ))
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
                    Demo02.Core.RuntimeLaunchOptions.Arguments,
                    "--capture-exit"
                ) >= 0)
            {
                Application.Quit();
            }
        }
    }
}

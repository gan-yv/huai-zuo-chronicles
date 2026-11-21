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
            float cardWidth = 280f * scale;
            float cardHeight = 180f * scale;
            float gap = 28f * scale;
            float totalWidth = cardWidth * 3f + gap * 2f;
            float startX = (Screen.width - totalWidth) * 0.5f;
            float cardY = 196f * scale;
            float routeY = cardY + cardHeight * 0.5f;

            UiTheme.DrawRect(
                new Rect(startX + cardWidth, routeY, totalWidth - cardWidth * 2f, 2f),
                UiTheme.Accent
            );

            DrawAreaCard(
                new Rect(startX, cardY, cardWidth, cardHeight),
                "01",
                "官署前街",
                "调查起点 · 盐仓旧址",
                "official-street",
                currentAreaId,
                "PlayerSpawn",
                "官署前街"
            );
            DrawAreaCard(
                new Rect(startX + cardWidth + gap, cardY, cardWidth, cardHeight),
                "02",
                "东关街",
                "市集主轴 · 沿街商铺",
                "dongguan-street",
                currentAreaId,
                "DongguanSpawn",
                "东关街"
            );
            DrawAreaCard(
                new Rect(startX + (cardWidth + gap) * 2f, cardY, cardWidth, cardHeight),
                "03",
                "彩衣街",
                "生活商街 · 裁衣与小吃",
                "caiyi-street",
                currentAreaId,
                "CaiyiSpawn",
                "彩衣街"
            );

            float footerY = cardY + cardHeight + 42f * scale;
            float footerWidth = Mathf.Min(900f * scale, Screen.width - 80f);
            Rect footer = new(
                (Screen.width - footerWidth) * 0.5f,
                footerY,
                footerWidth,
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

            string buttonText =
                $"{number}   {areaName}\n\n{description}\n\n"
                + (isCurrent ? "当前所在" : "点击快速旅行");
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

            UiTheme.DrawRect(
                new Rect(rect.x, rect.y, rect.width, 4f * scale),
                isCurrent ? UiTheme.Accent : UiTheme.PanelHover
            );
            UiTheme.DrawBorder(
                rect,
                isCurrent ? UiTheme.Accent : new Color(1f, 1f, 1f, 0.14f),
                isCurrent ? 2f : 1f
            );
        }

        private void TravelTo(string spawnName, string areaId, string areaName)
        {
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

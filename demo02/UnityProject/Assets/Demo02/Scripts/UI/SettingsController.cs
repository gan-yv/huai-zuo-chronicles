using System.Collections;
using System.IO;
using Demo02.Core;
using Demo02.Narrative;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Demo02.UI
{
    public sealed class SettingsController : MonoBehaviour
    {
        private bool isOpen;
        private float previousTimeScale = 1f;

        public static SettingsController Instance { get; private set; }
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
            if (System.Array.IndexOf(
                System.Environment.GetCommandLineArgs(),
                "--preview-settings"
            ) >= 0)
            {
                ToggleSettings();
                string path = GetArgumentValue("--capture-settings=");
                if (!string.IsNullOrWhiteSpace(path))
                {
                    StartCoroutine(CaptureSettings(path));
                }
            }
        }

        private void Update()
        {
            if (Keyboard.current == null)
            {
                return;
            }

            if (isOpen)
            {
                if (Keyboard.current.escapeKey.wasPressedThisFrame
                    || Keyboard.current.oKey.wasPressedThisFrame)
                {
                    ToggleSettings();
                }

                return;
            }

            if (!Keyboard.current.oKey.wasPressedThisFrame
                || Time.timeScale <= 0f
                || (DialogueController.Instance != null
                    && DialogueController.Instance.IsOpen)
                || (WorldMapController.Instance != null
                    && WorldMapController.Instance.IsOpen)
                || (InvestigationJournalController.Instance != null
                    && InvestigationJournalController.Instance.IsOpen)
                || (ScreenFade.Instance != null
                    && ScreenFade.Instance.IsTransitioning))
            {
                return;
            }

            ToggleSettings();
        }

        public void ToggleSettings()
        {
            if (!isOpen && Time.timeScale <= 0f)
            {
                return;
            }

            isOpen = !isOpen;
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

            GameAudio.PlayInteraction();
        }

        private void OnDisable()
        {
            if (isOpen)
            {
                Time.timeScale = previousTimeScale;
                isOpen = false;
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

            GUI.depth = -120;
            UiTheme.DrawRect(
                new Rect(0f, 0f, Screen.width, Screen.height),
                new Color(0.015f, 0.02f, 0.03f, 0.94f)
            );

            float scale = UiTheme.Scale;
            float width = Mathf.Min(Screen.width - 60f, 720f * scale);
            float height = Mathf.Min(Screen.height - 60f, 500f * scale);
            Rect panel = new(
                (Screen.width - width) * 0.5f,
                (Screen.height - height) * 0.5f,
                width,
                height
            );
            UiTheme.DrawRect(panel, new Color(0.055f, 0.065f, 0.08f, 0.98f));
            UiTheme.DrawBorder(panel, new Color(1f, 1f, 1f, 0.12f));
            UiTheme.DrawRect(
                new Rect(panel.x, panel.y, panel.width, 3f),
                UiTheme.Accent
            );

            GUIStyle eyebrow = UiTheme.Label(11, TextAnchor.MiddleLeft, bold: true);
            eyebrow.normal.textColor = UiTheme.Accent;
            GUI.Label(
                new Rect(
                    panel.x + 28f * scale,
                    panel.y + 14f * scale,
                    panel.width - 56f * scale,
                    20f * scale
                ),
                "SETTINGS · ACCESSIBILITY",
                eyebrow
            );
            GUI.Label(
                new Rect(
                    panel.x + 28f * scale,
                    panel.y + 34f * scale,
                    panel.width - 56f * scale,
                    42f * scale
                ),
                "游戏设置",
                UiTheme.Label(26, TextAnchor.MiddleLeft, bold: true)
            );

            float rowX = panel.x + 36f * scale;
            float rowWidth = panel.width - 72f * scale;
            float rowY = panel.y + 100f * scale;
            float rowHeight = 58f * scale;

            DrawStepperRow(
                new Rect(rowX, rowY, rowWidth, rowHeight),
                "主音量",
                $"{Mathf.RoundToInt(GameSettings.MasterVolume * 100f)}%",
                () => GameSettings.SetMasterVolume(GameSettings.MasterVolume - 0.1f),
                () => GameSettings.SetMasterVolume(GameSettings.MasterVolume + 0.1f)
            );
            DrawStepperRow(
                new Rect(rowX, rowY + 66f * scale, rowWidth, rowHeight),
                "界面缩放",
                $"{Mathf.RoundToInt(GameSettings.UiScaleMultiplier * 100f)}%",
                () => GameSettings.SetUiScale(GameSettings.UiScaleMultiplier - 0.05f),
                () => GameSettings.SetUiScale(GameSettings.UiScaleMultiplier + 0.05f)
            );
            DrawStepperRow(
                new Rect(rowX, rowY + 132f * scale, rowWidth, rowHeight),
                "镜头灵敏度",
                $"{Mathf.RoundToInt(GameSettings.CameraSensitivity * 100f)}%",
                () => GameSettings.SetCameraSensitivity(
                    GameSettings.CameraSensitivity - 0.1f
                ),
                () => GameSettings.SetCameraSensitivity(
                    GameSettings.CameraSensitivity + 0.1f
                )
            );

            Rect motionRow = new(
                rowX,
                rowY + 198f * scale,
                rowWidth,
                rowHeight
            );
            UiTheme.DrawRect(motionRow, UiTheme.Ink);
            GUI.Label(
                new Rect(
                    motionRow.x + 16f * scale,
                    motionRow.y,
                    motionRow.width * 0.55f,
                    motionRow.height
                ),
                "减弱动态效果",
                UiTheme.Label(15)
            );
            if (GUI.Button(
                new Rect(
                    motionRow.xMax - 160f * scale,
                    motionRow.y + 9f * scale,
                    140f * scale,
                    40f * scale
                ),
                GameSettings.ReduceMotion ? "已减弱" : "标准",
                UiTheme.Button(13)
            ))
            {
                GameSettings.SetReduceMotion(!GameSettings.ReduceMotion);
            }

            float footerY = panel.yMax - 72f * scale;
            if (GUI.Button(
                new Rect(
                    panel.x + 36f * scale,
                    footerY,
                    150f * scale,
                    44f * scale
                ),
                "恢复默认",
                UiTheme.Button(13)
            ))
            {
                GameSettings.ResetToDefaults();
            }
            if (GUI.Button(
                new Rect(
                    panel.xMax - 186f * scale,
                    footerY,
                    150f * scale,
                    44f * scale
                ),
                "关闭  [O / Esc]",
                UiTheme.Button(13)
            ))
            {
                ToggleSettings();
            }
        }

        private static void DrawStepperRow(
            Rect row,
            string label,
            string value,
            System.Action decrement,
            System.Action increment)
        {
            float scale = UiTheme.Scale;
            UiTheme.DrawRect(row, UiTheme.Ink);
            GUI.Label(
                new Rect(
                    row.x + 16f * scale,
                    row.y,
                    row.width * 0.45f,
                    row.height
                ),
                label,
                UiTheme.Label(15)
            );
            if (GUI.Button(
                new Rect(
                    row.xMax - 192f * scale,
                    row.y + 9f * scale,
                    42f * scale,
                    40f * scale
                ),
                "−",
                UiTheme.Button(17)
            ))
            {
                decrement();
            }

            GUI.Label(
                new Rect(
                    row.xMax - 142f * scale,
                    row.y,
                    90f * scale,
                    row.height
                ),
                value,
                UiTheme.Label(14, TextAnchor.MiddleCenter, bold: true)
            );
            if (GUI.Button(
                new Rect(
                    row.xMax - 52f * scale,
                    row.y + 9f * scale,
                    42f * scale,
                    40f * scale
                ),
                "+",
                UiTheme.Button(17)
            ))
            {
                increment();
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

        private IEnumerator CaptureSettings(string path)
        {
            yield return new WaitForSecondsRealtime(0.5f);
            yield return new WaitForEndOfFrame();
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            ScreenCapture.CaptureScreenshot(path);
            Debug.Log($"Settings preview captured to {path}");
        }
    }
}

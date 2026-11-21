using System.Collections;
using System.IO;
using Demo02.WorldState;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Demo02.Core
{
    public sealed class BootController : MonoBehaviour
    {
        private bool hasSave;

        private void Awake()
        {
            hasSave = SaveSystem.HasSave();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
            {
                StartGame();
            }
        }

        private void Start()
        {
            FitWindowToDisplay();

            string[] arguments = System.Environment.GetCommandLineArgs();
            string capturePath = GetArgumentValue(arguments, "--capture-title=");
            if (!string.IsNullOrWhiteSpace(capturePath))
            {
                StartCoroutine(CaptureTitle(capturePath));
            }

            if (System.Array.IndexOf(arguments, "--preview-world") >= 0
                || System.Array.IndexOf(arguments, "--preview-map") >= 0
                || System.Array.IndexOf(arguments, "--preview-journal") >= 0
                || System.Array.IndexOf(arguments, "--preview-vision") >= 0
                || System.Array.IndexOf(arguments, "--preview-choice") >= 0
                || System.Array.IndexOf(arguments, "--preview-settings") >= 0
                || System.Array.IndexOf(arguments, "--preview-saves") >= 0
                || System.Array.IndexOf(arguments, "--preview-testimony") >= 0
                || System.Array.IndexOf(arguments, "--preview-fact") >= 0
                || System.Array.IndexOf(arguments, "--preview-inventory") >= 0
                || System.Array.IndexOf(arguments, "--preview-optional") >= 0)
            {
                StartGame(showOpening: false);
            }
        }

        private static string GetArgumentValue(string[] arguments, string prefix)
        {
            foreach (string argument in arguments)
            {
                if (argument.StartsWith(prefix, System.StringComparison.Ordinal))
                {
                    return argument.Substring(prefix.Length);
                }
            }

            return string.Empty;
        }

        private IEnumerator CaptureTitle(string path)
        {
            yield return new WaitForSecondsRealtime(0.8f);
            yield return new WaitForEndOfFrame();
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            ScreenCapture.CaptureScreenshot(path);
            Debug.Log($"Title preview captured to {path}");
        }

        private static void FitWindowToDisplay()
        {
            Display display = Display.main;
            if (display == null
                || display.systemWidth <= 0
                || display.systemHeight <= 0
                || Screen.fullScreenMode != FullScreenMode.Windowed)
            {
                return;
            }

            int maxWidth = Mathf.Max(960, display.systemWidth - 96);
            int maxHeight = Mathf.Max(540, display.systemHeight - 120);
            int targetWidth = Mathf.Min(Screen.width, maxWidth);
            int targetHeight = Mathf.Min(Screen.height, maxHeight);

            const float targetAspect = 16f / 9f;
            if (targetHeight > targetWidth / targetAspect)
            {
                targetHeight = Mathf.RoundToInt(targetWidth / targetAspect);
            }
            else
            {
                targetWidth = Mathf.RoundToInt(targetHeight * targetAspect);
            }

            if (targetWidth != Screen.width || targetHeight != Screen.height)
            {
                Screen.SetResolution(targetWidth, targetHeight, FullScreenMode.Windowed);
            }
        }

        private void OnGUI()
        {
            Demo02.UI.UiTheme.DrawRect(
                new Rect(0f, 0f, Screen.width, Screen.height),
                Demo02.UI.UiTheme.Ink
            );

            float scale = Demo02.UI.UiTheme.Scale;
            Demo02.UI.UiTheme.DrawRect(
                new Rect(0f, Screen.height * 0.17f, Screen.width, 1f),
                new Color(1f, 1f, 1f, 0.07f)
            );
            Demo02.UI.UiTheme.DrawRect(
                new Rect(
                    Screen.width * 0.5f - 54f * scale,
                    Screen.height * 0.17f - 1f,
                    108f * scale,
                    3f
                ),
                Demo02.UI.UiTheme.Accent
            );

            GUIStyle eyebrowStyle = Demo02.UI.UiTheme.Label(
                12,
                TextAnchor.MiddleCenter,
                bold: true
            );
            eyebrowStyle.normal.textColor = Demo02.UI.UiTheme.Accent;
            GUIStyle titleStyle = Demo02.UI.UiTheme.Label(
                40,
                TextAnchor.MiddleCenter,
                bold: true
            );
            GUIStyle subtitleStyle = Demo02.UI.UiTheme.Label(
                17,
                TextAnchor.MiddleCenter
            );
            subtitleStyle.normal.textColor = Demo02.UI.UiTheme.Muted;

            GUI.Label(
                new Rect(0f, Screen.height * 0.21f, Screen.width, 30f * scale),
                "CHAPTER ONE · A PLAYABLE VERTICAL SLICE",
                eyebrowStyle
            );
            GUI.Label(
                new Rect(0f, Screen.height * 0.25f, Screen.width, 74f * scale),
                "HUAI-ZUO CHRONICLES",
                titleStyle
            );
            GUI.Label(
                new Rect(0f, Screen.height * 0.34f, Screen.width, 42f * scale),
                "Chronicles of Strange Events in Huai-Zuo · 第一章：消失的盐引",
                subtitleStyle
            );

            float buttonWidth = 320f * scale;
            float buttonHeight = 52f * scale;
            float buttonX = (Screen.width - buttonWidth) * 0.5f;
            float buttonY = Screen.height * 0.5f;
            GUIStyle buttonStyle = Demo02.UI.UiTheme.Button(17);

            if (GUI.Button(
                new Rect(buttonX, buttonY, buttonWidth, buttonHeight),
                "开始游戏  [Enter]",
                buttonStyle
            ))
            {
                StartGame();
            }

            GUI.enabled = hasSave;
            if (GUI.Button(
                new Rect(buttonX, buttonY + 68f * scale, buttonWidth, buttonHeight),
                hasSave ? "继续游戏" : "继续游戏  ·  暂无存档",
                buttonStyle
            ))
            {
                ContinueGame();
            }
            GUI.enabled = true;

            if (GUI.Button(
                new Rect(buttonX, buttonY + 136f * scale, buttonWidth, buttonHeight),
                "退出游戏",
                buttonStyle
            ))
            {
                QuitGame();
            }

            GUI.Label(
                new Rect(
                    0f,
                    Screen.height - 92f * scale,
                    Screen.width,
                    28f * scale
                ),
                "WASD 移动   E 调查/对话   Tab 切换角色   T 证词对照   J 案卷   M 地图   F1 帮助",
                subtitleStyle
            );
            GUI.Label(
                new Rect(
                    0f,
                    Screen.height - 52f * scale,
                    Screen.width,
                    30f * scale
                ),
                $"当前版本：v{Application.version} · 灰盒开发版",
                subtitleStyle
            );
        }

        private static void StartGame(bool showOpening = true)
        {
            WorldRuntime.Instance?.BeginNewSession();
            if (!showOpening)
            {
                WorldRuntime.Instance?.ConsumeOpeningRequest();
            }

            WorldRuntime.Instance?.SetCurrentArea("official-street", "官署前街");
            if (WorldRuntime.Instance != null)
            {
                WorldRuntime.Instance.RequestSceneLoad(SceneNames.Guangling, "guangling_start");
            }
            else
            {
                SceneManager.LoadScene(SceneNames.Guangling);
            }
        }

        private static void ContinueGame()
        {
            SaveSystem.LoadLatestSavedGame(out _);
        }

        private static void QuitGame()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}

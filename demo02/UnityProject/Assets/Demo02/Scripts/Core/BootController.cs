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
            DisplaySettings.Apply();

            string[] arguments = System.Environment.GetCommandLineArgs();
            string capturePath = GetArgumentValue(arguments, "--capture-title=");
            if (!string.IsNullOrWhiteSpace(capturePath))
            {
                StartCoroutine(CaptureTitle(capturePath));
            }

            bool benchmarkRequested = System.Array.Exists(
                arguments,
                argument => argument.StartsWith(
                    "--benchmark-",
                    System.StringComparison.Ordinal
                )
            );
            if (benchmarkRequested
                || System.Array.IndexOf(arguments, "--preview-world") >= 0
                || System.Array.IndexOf(arguments, "--preview-map") >= 0
                || System.Array.IndexOf(arguments, "--preview-journal") >= 0
                || System.Array.IndexOf(arguments, "--preview-vision") >= 0
                || System.Array.IndexOf(arguments, "--preview-choice") >= 0
                || System.Array.IndexOf(arguments, "--preview-settings") >= 0
                || System.Array.IndexOf(arguments, "--preview-saves") >= 0
                || System.Array.IndexOf(arguments, "--preview-testimony") >= 0
                || System.Array.IndexOf(arguments, "--preview-fact") >= 0
                || System.Array.IndexOf(arguments, "--preview-inventory") >= 0
                || System.Array.IndexOf(arguments, "--preview-optional") >= 0
                || System.Array.IndexOf(arguments, "--preview-ending") >= 0
                || System.Array.IndexOf(arguments, "--preview-teahouse") >= 0
                || System.Array.IndexOf(arguments, "--preview-combat") >= 0
                || System.Array.IndexOf(
                    arguments,
                    "--preview-companion-combat"
                ) >= 0
                || System.Array.IndexOf(arguments, "--preview-glide") >= 0
                || System.Array.IndexOf(arguments, "--preview-abilities") >= 0
                || System.Array.IndexOf(arguments, "--preview-streaming") >= 0
                || System.Array.IndexOf(arguments, "--preview-story") >= 0
                || System.Array.IndexOf(arguments, "--preview-pavilion") >= 0
                || System.Array.IndexOf(arguments, "--preview-cottage") >= 0
                || System.Array.IndexOf(arguments, "--preview-elite") >= 0
                || System.Array.IndexOf(arguments, "--preview-glide-course") >= 0
                || System.Array.IndexOf(arguments, "--preview-companion") >= 0
                || System.Array.IndexOf(arguments, "--preview-paper-kite") >= 0
                || System.Array.IndexOf(
                    arguments,
                    "--preview-paper-kite-choice"
                ) >= 0
                || System.Array.IndexOf(
                    arguments,
                    "--preview-paper-kite-result"
                ) >= 0
                || System.Array.Exists(
                    arguments,
                    argument => argument.StartsWith(
                        "--capture-game=",
                        System.StringComparison.Ordinal
                    )
                ))
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
            yield return null;
            if (System.Array.IndexOf(
                    System.Environment.GetCommandLineArgs(),
                    "--capture-exit"
                ) >= 0)
            {
                Application.Quit();
            }
        }

        private void OnGUI()
        {
            float scale = Demo02.UI.UiTheme.Scale;
            float split = Mathf.Clamp(
                Screen.width * 0.56f,
                620f * scale,
                Screen.width * 0.64f
            );
            Demo02.UI.UiTheme.DrawRect(
                new Rect(0f, 0f, Screen.width, Screen.height),
                new Color(0.025f, 0.035f, 0.05f, 0.16f)
            );
            Demo02.UI.UiTheme.DrawRect(
                new Rect(0f, 0f, split, Screen.height),
                new Color(0.025f, 0.035f, 0.05f, 0.965f)
            );
            Demo02.UI.UiTheme.DrawRect(
                new Rect(
                    split,
                    0f,
                    2f,
                    Screen.height
                ),
                new Color(0.88f, 0.7f, 0.38f, 0.55f)
            );

            GUIStyle eyebrowStyle = Demo02.UI.UiTheme.Label(
                12,
                TextAnchor.MiddleLeft,
                bold: true
            );
            eyebrowStyle.normal.textColor = Demo02.UI.UiTheme.Accent;
            GUIStyle titleStyle = Demo02.UI.UiTheme.Label(
                38,
                TextAnchor.MiddleLeft,
                bold: true
            );
            GUIStyle subtitleStyle = Demo02.UI.UiTheme.Label(
                17,
                TextAnchor.MiddleLeft
            );
            subtitleStyle.normal.textColor = Demo02.UI.UiTheme.Muted;

            float contentX = 72f * scale;
            float contentWidth = split - contentX - 54f * scale;
            Demo02.UI.UiTheme.DrawRect(
                new Rect(
                    contentX,
                    Screen.height * 0.125f,
                    54f * scale,
                    3f
                ),
                Demo02.UI.UiTheme.Accent
            );
            GUI.Label(
                new Rect(
                    contentX,
                    Screen.height * 0.14f,
                    contentWidth,
                    28f * scale
                ),
                "CHAPTER ONE · A PLAYABLE VERTICAL SLICE",
                eyebrowStyle
            );
            GUI.Label(
                new Rect(
                    contentX,
                    Screen.height * 0.19f,
                    contentWidth,
                    72f * scale
                ),
                "HUAI-ZUO CHRONICLES",
                titleStyle
            );
            GUI.Label(
                new Rect(
                    contentX,
                    Screen.height * 0.285f,
                    contentWidth,
                    38f * scale
                ),
                "Chronicles of Strange Events in Huai-Zuo · 第一章：消失的盐引",
                subtitleStyle
            );
            GUIStyle descriptionStyle = Demo02.UI.UiTheme.Label(14);
            descriptionStyle.normal.textColor =
                new Color(0.72f, 0.75f, 0.78f);
            GUI.Label(
                new Rect(
                    contentX,
                    Screen.height * 0.345f,
                    contentWidth - 32f * scale,
                    68f * scale
                ),
                "旧街从黄昏后浮现，名字从城市记录里消失。"
                + "穿过广陵街巷，修复叠在一起的时间。",
                descriptionStyle
            );

            float buttonWidth = Mathf.Min(
                340f * scale,
                contentWidth
            );
            float buttonHeight = 52f * scale;
            float buttonX = contentX;
            float buttonY = Screen.height * 0.47f;
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
                    contentX,
                    Screen.height - 92f * scale,
                    contentWidth,
                    28f * scale
                ),
                "WASD 移动   Shift 冲刺   Space 跳跃/滑翔   1/2/Tab 换人   F 技能   R 终结技   F1 帮助",
                subtitleStyle
            );
            GUI.Label(
                new Rect(
                    contentX,
                    Screen.height - 52f * scale,
                    contentWidth,
                    30f * scale
                ),
                $"当前版本：v{Application.version} · 可玩垂直切片",
                subtitleStyle
            );

            GUIStyle creditStyle = Demo02.UI.UiTheme.Label(
                11,
                TextAnchor.LowerRight
            );
            creditStyle.normal.textColor =
                new Color(0.65f, 0.69f, 0.72f, 0.82f);
            GUI.Label(
                new Rect(
                    split + 18f * scale,
                    Screen.height - 54f * scale,
                    Screen.width - split - 36f * scale,
                    30f * scale
                ),
                "美术角色：大肥鱼 打工版 · 木子恶魔",
                creditStyle
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

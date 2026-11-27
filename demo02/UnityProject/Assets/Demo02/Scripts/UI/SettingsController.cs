using System.Collections;
using System.IO;
using Demo02.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Demo02.UI
{
    public sealed class SettingsController : MonoBehaviour
    {
        private sealed class SettingRow
        {
            public GameObject Root;
            public Outline Border;
            public Text Value;
        }

        private bool isOpen;
        private float previousTimeScale = 1f;
        private int selectedRow;
        private int rebindActionIndex;
        private bool isRebinding;
        private string inputStatus = string.Empty;
        private string cachedInputStatus = string.Empty;
        private GameObject canvasObject;
        private Canvas canvas;
        private SettingRow[] rows;
        private Text status;
        private Text closeLabel;

        private static readonly string[] RebindableActions =
        {
            "Attack",
            "Interact",
            "Jump",
            "Dodge",
            "Skill",
            "Burst",
            "ToggleTime",
            "QuickHeal"
        };

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
            Demo02.Core.GameServices.Register(this);
            EnsureUi();
        }

        private void Start()
        {
            if (System.Array.IndexOf(
                Demo02.Core.RuntimeLaunchOptions.Arguments,
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
            if (isOpen)
            {
                if (Demo02Input.CancelPressed
                    || Demo02Input.SettingsPressed)
                {
                    ToggleSettings();
                    return;
                }

                if (Demo02Input.NavigateUpPressed)
                {
                    selectedRow = (selectedRow + 5) % 6;
                }
                else if (Demo02Input.NavigateDownPressed)
                {
                    selectedRow = (selectedRow + 1) % 6;
                }

                if (Demo02Input.NavigateLeftPressed)
                {
                    AdjustSelected(-1);
                }
                else if (Demo02Input.NavigateRightPressed)
                {
                    AdjustSelected(1);
                }
                else if (Demo02Input.SubmitPressed)
                {
                    ActivateSelected();
                }

                RefreshUi();
                return;
            }

            if (!Demo02Input.SettingsPressed
                || Time.timeScale <= 0f
                || GameplayUiState.HasOpenModalExcept(this)
                || (Demo02.Core.GameServices.Resolve<ScreenFade>() != null
                    && Demo02.Core.GameServices.Resolve<ScreenFade>().IsTransitioning))
            {
                return;
            }
            ToggleSettings();
        }

        public void ToggleSettings()
        {
            if (!isOpen
                && (Time.timeScale <= 0f
                    || GameplayUiState.HasOpenModalExcept(this)))
            {
                return;
            }

            isOpen = !isOpen;
            canvas.enabled = isOpen;
            if (isOpen)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                GameplayUiState.RegisterModal(this);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                RefreshUi();
            }
            else
            {
                Time.timeScale = previousTimeScale;
                GameplayUiState.UnregisterModal(this);
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
                Demo02.Core.GameServices.Unregister(this);
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

        private void EnsureUi()
        {
            if (canvasObject != null)
            {
                return;
            }
            RectTransform root = HudUiFactory.CreateCanvas(
                transform,
                "SettingsCanvas",
                100
            );
            canvasObject = root.gameObject;
            canvas = root.GetComponent<Canvas>();
            canvas.enabled = false;

            Image dim = HudUiFactory.CreateImage(
                root,
                "Dim",
                new Color(0.015f, 0.02f, 0.03f, 0.94f)
            );
            HudUiFactory.Stretch(dim.rectTransform);

            RectTransform panel = HudUiFactory.CreatePanel(
                root,
                "SettingsPanel",
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(720f, 560f),
                new Color(0.055f, 0.065f, 0.08f, 0.98f),
                raycastTarget: true
            );
            Outline outline = panel.gameObject.AddComponent<Outline>();
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;
            outline.effectColor = new Color(1f, 1f, 1f, 0.12f);
            Image topLine = HudUiFactory.CreateImage(
                panel,
                "TopLine",
                UiTheme.Accent
            );
            topLine.rectTransform.anchorMin =
                new Vector2(0f, 1f);
            topLine.rectTransform.anchorMax =
                new Vector2(1f, 1f);
            topLine.rectTransform.pivot =
                new Vector2(0.5f, 1f);
            topLine.rectTransform.anchoredPosition = Vector2.zero;
            topLine.rectTransform.sizeDelta =
                new Vector2(0f, 3f);

            Text eyebrow = CreateText(
                panel,
                "Eyebrow",
                "SETTINGS · ACCESSIBILITY",
                11,
                new Vector2(28f, -14f),
                new Vector2(664f, 20f),
                TextAnchor.MiddleLeft,
                bold: true
            );
            eyebrow.color = UiTheme.Accent;
            CreateText(
                panel,
                "Title",
                "游戏设置",
                26,
                new Vector2(28f, -34f),
                new Vector2(664f, 42f),
                TextAnchor.MiddleLeft,
                bold: true
            );

            rows = new SettingRow[6];
            for (int index = 0; index < rows.Length; index++)
            {
                rows[index] = CreateRow(panel, index);
            }
            CreateFooter(panel);
        }

        private SettingRow CreateRow(Transform parent, int index)
        {
            RectTransform row = HudUiFactory.CreatePanel(
                parent,
                "SettingRow" + index,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(36f, -100f - index * 66f),
                new Vector2(648f, 58f),
                UiTheme.Ink,
                raycastTarget: true
            );
            Outline outline = row.gameObject.AddComponent<Outline>();
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;
            outline.effectColor = Color.clear;

            string label = index switch
            {
                0 => "主音量",
                1 => "界面缩放",
                2 => "镜头灵敏度",
                3 => "减弱动态效果",
                4 => "显示模式",
                _ => "按键重绑定"
            };
            CreateText(
                row,
                "Label",
                label,
                15,
                new Vector2(16f, -29f),
                new Vector2(270f, 58f),
                TextAnchor.MiddleLeft
            );
            Text value = CreateText(
                row,
                "Value",
                string.Empty,
                13,
                new Vector2(500f, -29f),
                new Vector2(80f, 58f),
                TextAnchor.MiddleCenter,
                bold: true
            );

            int rowIndex = index;
            CreateButton(
                row,
                "Minus",
                "−",
                new Vector2(456f, -9f),
                new Vector2(42f, 40f),
                () =>
                {
                    selectedRow = rowIndex;
                    AdjustSelected(-1);
                }
            );
            CreateButton(
                row,
                "Plus",
                "+",
                new Vector2(596f, -9f),
                new Vector2(42f, 40f),
                () =>
                {
                    selectedRow = rowIndex;
                    AdjustSelected(1);
                }
            );
            return new SettingRow
            {
                Root = row.gameObject,
                Border = outline,
                Value = value
            };
        }

        private void CreateFooter(Transform parent)
        {
            CreateButton(
                parent,
                "ResetButton",
                "恢复默认",
                new Vector2(36f, -500f),
                new Vector2(150f, 44f),
                () =>
                {
                    GameSettings.ResetToDefaults();
                    Demo02Input.ResetBindingOverrides();
                    inputStatus = "设置与按键已恢复默认";
                }
            );
            Button close = CreateButton(
                parent,
                "CloseButton",
                string.Empty,
                new Vector2(534f, -500f),
                new Vector2(150f, 44f),
                ToggleSettings
            );
            closeLabel = close.GetComponentInChildren<Text>(true);
            status = CreateText(
                parent,
                "Status",
                string.Empty,
                11,
                new Vector2(202f, -500f),
                new Vector2(316f, 44f),
                TextAnchor.MiddleRight
            );
            status.color = UiTheme.Muted;
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
            string label,
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
                label,
                13,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                TextAnchor.MiddleCenter
            );
            return button;
        }

        private void RefreshUi()
        {
            if (!isOpen)
            {
                return;
            }
            rows[0].Value.text =
                $"{Mathf.RoundToInt(GameSettings.MasterVolume * 100f)}%";
            rows[1].Value.text =
                $"{Mathf.RoundToInt(GameSettings.UiScaleMultiplier * 100f)}%";
            rows[2].Value.text =
                $"{Mathf.RoundToInt(GameSettings.CameraSensitivity * 100f)}%";
            rows[3].Value.text = GameSettings.ReduceMotion
                ? "已减弱"
                : "标准";
            rows[4].Value.text =
                DisplaySettings.UseBorderlessFullscreen
                    ? "无边框全屏"
                    : "窗口化";
            rows[5].Value.text =
                RebindableActions[rebindActionIndex] + " · "
                + Demo02Input.GetBindingDisplay(
                    RebindableActions[rebindActionIndex],
                    0
                );

            for (int index = 0; index < rows.Length; index++)
            {
                rows[index].Border.effectColor = index == selectedRow
                    ? new Color(
                        UiTheme.Accent.r,
                        UiTheme.Accent.g,
                        UiTheme.Accent.b,
                        0.72f
                    )
                    : Color.clear;
            }
            if (cachedInputStatus != inputStatus)
            {
                cachedInputStatus = inputStatus;
                status.text = inputStatus;
            }
            string close =
                $"关闭  [{Demo02Input.GetPreferredBindingDisplay("Settings")}"
                + $" / {Demo02Input.GetPreferredBindingDisplay("Cancel")}]";
            HudUiFactory.SetText(closeLabel, close);
        }

        private void AdjustSelected(int direction)
        {
            switch (selectedRow)
            {
                case 0:
                    GameSettings.SetMasterVolume(
                        GameSettings.MasterVolume + direction * 0.1f
                    );
                    break;
                case 1:
                    GameSettings.SetUiScale(
                        GameSettings.UiScaleMultiplier
                            + direction * 0.05f
                    );
                    break;
                case 2:
                    GameSettings.SetCameraSensitivity(
                        GameSettings.CameraSensitivity
                            + direction * 0.1f
                    );
                    break;
                case 3:
                    GameSettings.SetReduceMotion(
                        !GameSettings.ReduceMotion
                    );
                    break;
                case 4:
                    DisplaySettings.ToggleMode();
                    break;
                case 5:
                    CycleRebindAction(direction);
                    break;
            }
            RefreshUi();
        }

        private void ActivateSelected()
        {
            switch (selectedRow)
            {
                case 3:
                    GameSettings.SetReduceMotion(
                        !GameSettings.ReduceMotion
                    );
                    break;
                case 4:
                    DisplaySettings.ToggleMode();
                    break;
                case 5:
                    StartSelectedRebind();
                    break;
            }
            RefreshUi();
        }

        private void CycleRebindAction(int direction)
        {
            rebindActionIndex =
                (rebindActionIndex + direction
                    + RebindableActions.Length)
                % RebindableActions.Length;
            inputStatus = "按 Enter 开始重新绑定";
            GameAudio.PlayInteraction();
        }

        private void StartSelectedRebind()
        {
            if (isRebinding)
            {
                return;
            }
            string action = RebindableActions[rebindActionIndex];
            inputStatus = $"请按下 {action} 的新按键";
            isRebinding = true;
            Demo02Input.TryRebind(
                action,
                0,
                (success, message) =>
                {
                    isRebinding = false;
                    inputStatus = success
                        ? $"{action} 已绑定为 {message}"
                        : message;
                    GameAudio.PlayConfirm();
                }
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Demo02.Core;
using Demo02.Narrative;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Demo02.UI
{
    public sealed class SaveSlotController : MonoBehaviour
    {
        private enum PendingAction
        {
            None,
            Save,
            Load,
            Delete
        }

        public static SaveSlotController Instance { get; private set; }

        private bool isOpen;
        private int selectedManualSlot = 1;
        private PendingAction pendingAction;
        private int pendingSlot;
        private string statusMessage;
        private float statusUntil;
        private SaveSystem saveSystem;

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
            saveSystem = FindAnyObjectByType<SaveSystem>();
            SaveSystem.HasSave();
            selectedManualSlot = SaveSystem.ActiveManualSlot > 0
                ? SaveSystem.ActiveManualSlot
                : 1;

            if (Array.IndexOf(
                Environment.GetCommandLineArgs(),
                "--preview-saves"
            ) >= 0)
            {
                Open();
                string path = GetArgumentValue("--capture-saves=");
                if (!string.IsNullOrWhiteSpace(path))
                {
                    StartCoroutine(CapturePreview(path));
                }
            }
        }

        private void Update()
        {
            if (!isOpen || Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (pendingAction != PendingAction.None)
                {
                    CancelPending();
                }
                else
                {
                    Close();
                }

                return;
            }

            if (pendingAction != PendingAction.None)
            {
                return;
            }

            if (Keyboard.current.f5Key.wasPressedThisFrame)
            {
                RequestSave(selectedManualSlot);
            }
            else if (Keyboard.current.f9Key.wasPressedThisFrame)
            {
                RequestLoad(selectedManualSlot);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Open()
        {
            if (isOpen)
            {
                return;
            }

            isOpen = true;
            pendingAction = PendingAction.None;
            selectedManualSlot = SaveSystem.ActiveManualSlot > 0
                ? SaveSystem.ActiveManualSlot
                : selectedManualSlot;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            GameAudio.PlayInteraction();
        }

        public void Close()
        {
            if (!isOpen)
            {
                return;
            }

            isOpen = false;
            pendingAction = PendingAction.None;
            GameAudio.PlayInteraction();
        }

        private void RequestSave(int slot)
        {
            if (slot <= SaveSystem.AutoSlot)
            {
                return;
            }

            selectedManualSlot = slot;
            if (SaveSystem.TryReadSave(slot, out _))
            {
                QueueAction(PendingAction.Save, slot);
                return;
            }

            ExecuteSave(slot);
        }

        private void RequestLoad(int slot)
        {
            if (!SaveSystem.TryReadSave(slot, out _))
            {
                ShowStatus($"{SaveSystem.GetSlotDisplayName(slot)}为空");
                return;
            }

            if (slot > SaveSystem.AutoSlot)
            {
                selectedManualSlot = slot;
            }

            QueueAction(PendingAction.Load, slot);
        }

        private void RequestDelete(int slot)
        {
            if (slot <= SaveSystem.AutoSlot
                || !SaveSystem.TryReadSave(slot, out _))
            {
                return;
            }

            selectedManualSlot = slot;
            QueueAction(PendingAction.Delete, slot);
        }

        private void QueueAction(PendingAction action, int slot)
        {
            pendingAction = action;
            pendingSlot = slot;
            GameAudio.PlayInteraction();
        }

        private void CancelPending()
        {
            pendingAction = PendingAction.None;
            GameAudio.PlayInteraction();
        }

        private void ConfirmPending()
        {
            PendingAction action = pendingAction;
            int slot = pendingSlot;
            pendingAction = PendingAction.None;

            switch (action)
            {
                case PendingAction.Save:
                    ExecuteSave(slot);
                    break;
                case PendingAction.Load:
                    ExecuteLoad(slot);
                    break;
                case PendingAction.Delete:
                    ExecuteDelete(slot);
                    break;
            }
        }

        private void ExecuteSave(int slot)
        {
            saveSystem ??= FindAnyObjectByType<SaveSystem>();
            if (saveSystem == null)
            {
                ShowStatus("存档系统尚未就绪");
                return;
            }

            selectedManualSlot = slot;
            saveSystem.SaveCurrentState(announce: true, slotIndex: slot);
            ShowStatus($"已保存到{SaveSystem.GetSlotDisplayName(slot)}");
        }

        private void ExecuteLoad(int slot)
        {
            if (SaveSystem.LoadSavedGame(slot, out _))
            {
                isOpen = false;
                return;
            }

            ShowStatus("读取失败，存档可能已损坏");
        }

        private void ExecuteDelete(int slot)
        {
            if (SaveSystem.DeleteSlot(slot))
            {
                ShowStatus($"已删除{SaveSystem.GetSlotDisplayName(slot)}");
                GameAudio.PlayConfirm();
            }
            else
            {
                ShowStatus("删除失败，请查看 Console");
            }
        }

        private void ShowStatus(string message)
        {
            statusMessage = message;
            statusUntil = Time.unscaledTime + 3f;
        }

        private void OnGUI()
        {
            if (!isOpen)
            {
                return;
            }

            GUI.depth = -130;
            UiTheme.DrawRect(
                new Rect(0f, 0f, Screen.width, Screen.height),
                new Color(0.015f, 0.02f, 0.03f, 0.94f)
            );

            float scale = UiTheme.Scale;
            float panelWidth = Mathf.Min(Screen.width - 48f, 1060f * scale);
            float panelHeight = Mathf.Min(Screen.height - 48f, 620f * scale);
            Rect panel = new(
                (Screen.width - panelWidth) * 0.5f,
                (Screen.height - panelHeight) * 0.5f,
                panelWidth,
                panelHeight
            );
            DrawPanel(panel);

            GUIStyle eyebrow = UiTheme.Label(11, TextAnchor.MiddleLeft, bold: true);
            eyebrow.normal.textColor = UiTheme.Accent;
            GUI.Label(
                new Rect(
                    panel.x + 28f * scale,
                    panel.y + 13f * scale,
                    panel.width - 56f * scale,
                    20f * scale
                ),
                "ARCHIVE · SAVE / LOAD",
                eyebrow
            );
            GUI.Label(
                new Rect(
                    panel.x + 28f * scale,
                    panel.y + 32f * scale,
                    panel.width - 220f * scale,
                    44f * scale
                ),
                "存档与读档",
                UiTheme.Label(27, TextAnchor.MiddleLeft, bold: true)
            );
            GUI.Label(
                new Rect(
                    panel.x + panel.width - 250f * scale,
                    panel.y + 38f * scale,
                    220f * scale,
                    34f * scale
                ),
                "自动存档 + 3 个手动槽位",
                UiTheme.Label(12, TextAnchor.MiddleRight)
            );

            float padding = 28f * scale;
            float gap = 14f * scale;
            float headerHeight = 92f * scale;
            float footerHeight = 66f * scale;
            float gridWidth = panel.width - padding * 2f;
            float cardWidth = (gridWidth - gap) * 0.5f;
            float cardHeight = (
                panel.height - headerHeight - footerHeight - gap
            ) * 0.5f;
            for (int slot = SaveSystem.AutoSlot;
                slot <= SaveSystem.ManualSlotCount;
                slot++)
            {
                int index = slot - SaveSystem.AutoSlot;
                int column = index % 2;
                int row = index / 2;
                Rect card = new(
                    panel.x + padding + column * (cardWidth + gap),
                    panel.y + headerHeight + row * (cardHeight + gap),
                    cardWidth,
                    cardHeight
                );
                DrawSlotCard(card, slot);
            }

            Rect footer = new(
                panel.x + padding,
                panel.yMax - footerHeight + 8f * scale,
                gridWidth,
                footerHeight - 16f * scale
            );
            string status = string.IsNullOrWhiteSpace(statusMessage)
                || Time.unscaledTime >= statusUntil
                ? $"F5 保存到快捷槽 {selectedManualSlot}   ·   F9 读取快捷槽 {selectedManualSlot}"
                : statusMessage;
            GUI.Label(
                new Rect(
                    footer.x,
                    footer.y,
                    footer.width - 170f * scale,
                    footer.height
                ),
                status,
                UiTheme.Label(12)
            );
            if (GUI.Button(
                new Rect(
                    footer.xMax - 156f * scale,
                    footer.y,
                    156f * scale,
                    footer.height
                ),
                "返回暂停  [Esc]",
                UiTheme.Button(13)
            ))
            {
                Close();
            }

            if (pendingAction != PendingAction.None)
            {
                DrawConfirmation();
            }
        }

        private static void DrawPanel(Rect panel)
        {
            UiTheme.DrawRect(panel, new Color(0.045f, 0.055f, 0.07f, 0.99f));
            UiTheme.DrawBorder(panel, new Color(1f, 1f, 1f, 0.13f));
            UiTheme.DrawRect(
                new Rect(panel.x, panel.y, panel.width, 3f),
                UiTheme.Accent
            );
        }

        private void DrawSlotCard(Rect card, int slot)
        {
            float scale = UiTheme.Scale;
            bool hasSave = SaveSystem.TryReadSave(slot, out WorldSaveData data);
            bool isQuickSlot = slot > SaveSystem.AutoSlot
                && slot == selectedManualSlot;
            Color background = isQuickSlot
                ? new Color(0.105f, 0.105f, 0.09f, 0.98f)
                : UiTheme.Ink;
            UiTheme.DrawRect(card, background);
            UiTheme.DrawBorder(
                card,
                isQuickSlot
                    ? new Color(
                        UiTheme.Accent.r,
                        UiTheme.Accent.g,
                        UiTheme.Accent.b,
                        0.72f
                    )
                    : new Color(1f, 1f, 1f, 0.09f)
            );
            if (isQuickSlot)
            {
                UiTheme.DrawRect(
                    new Rect(card.x, card.y, 3f * scale, card.height),
                    UiTheme.Accent
                );
            }

            GUI.Label(
                new Rect(
                    card.x + 16f * scale,
                    card.y + 5f * scale,
                    card.width - 100f * scale,
                    28f * scale
                ),
                SaveSystem.GetSlotDisplayName(slot),
                UiTheme.Label(16, TextAnchor.MiddleLeft, bold: true)
            );

            GUIStyle badge = UiTheme.Label(10, TextAnchor.MiddleCenter, bold: true);
            badge.normal.textColor = slot == SaveSystem.AutoSlot
                ? UiTheme.Muted
                : UiTheme.Accent;
            GUI.Label(
                new Rect(
                    card.xMax - 88f * scale,
                    card.y + 7f * scale,
                    72f * scale,
                    22f * scale
                ),
                slot == SaveSystem.AutoSlot
                    ? "只读"
                    : isQuickSlot ? "快捷槽" : "手动",
                badge
            );

            if (hasSave)
            {
                GUI.Label(
                    new Rect(
                        card.x + 16f * scale,
                        card.y + 34f * scale,
                        card.width - 32f * scale,
                        22f * scale
                    ),
                    $"{SafeArea(data.AreaName)} · {StageLabel(data.ChapterStage)}",
                    UiTheme.Label(12, bold: true)
                );
                GUI.Label(
                    new Rect(
                        card.x + 16f * scale,
                        card.y + 55f * scale,
                        card.width - 32f * scale,
                        22f * scale
                    ),
                    $"线索 {Count(data.ClueIds)}   时间锚 {Count(data.TimeAnchorIds)}   回声 {Count(data.EchoIds)}",
                    UiTheme.Label(11)
                );
                GUI.Label(
                    new Rect(
                        card.x + 16f * scale,
                        card.y + 76f * scale,
                        card.width - 32f * scale,
                        20f * scale
                    ),
                    FormatSaveTime(data.SavedAtUtc, slot),
                    UiTheme.Label(10)
                );
            }
            else
            {
                GUI.Label(
                    new Rect(
                        card.x + 16f * scale,
                        card.y + 42f * scale,
                        card.width - 32f * scale,
                        42f * scale
                    ),
                    "空槽位\n可保存当前调查进度",
                    UiTheme.Label(12, TextAnchor.MiddleCenter)
                );
            }

            float buttonY = card.yMax - 43f * scale;
            float buttonHeight = 32f * scale;
            float innerWidth = card.width - 32f * scale;
            if (slot == SaveSystem.AutoSlot)
            {
                GUI.enabled = hasSave;
                if (GUI.Button(
                    new Rect(
                        card.x + 16f * scale,
                        buttonY,
                        innerWidth,
                        buttonHeight
                    ),
                    hasSave ? "读取自动存档" : "暂无自动存档",
                    UiTheme.Button(12)
                ))
                {
                    RequestLoad(slot);
                }
                GUI.enabled = true;
                return;
            }

            float saveWidth = innerWidth * 0.40f;
            float loadWidth = innerWidth * 0.31f;
            float deleteWidth = innerWidth - saveWidth - loadWidth
                - 12f * scale;
            if (GUI.Button(
                new Rect(
                    card.x + 16f * scale,
                    buttonY,
                    saveWidth,
                    buttonHeight
                ),
                hasSave ? "覆盖" : "保存",
                UiTheme.Button(12)
            ))
            {
                RequestSave(slot);
            }

            GUI.enabled = hasSave;
            if (GUI.Button(
                new Rect(
                    card.x + 16f * scale + saveWidth + 6f * scale,
                    buttonY,
                    loadWidth,
                    buttonHeight
                ),
                "读取",
                UiTheme.Button(12)
            ))
            {
                RequestLoad(slot);
            }
            if (GUI.Button(
                new Rect(
                    card.xMax - 16f * scale - deleteWidth,
                    buttonY,
                    deleteWidth,
                    buttonHeight
                ),
                "删除",
                UiTheme.Button(12)
            ))
            {
                RequestDelete(slot);
            }
            GUI.enabled = true;
        }

        private void DrawConfirmation()
        {
            float scale = UiTheme.Scale;
            UiTheme.DrawRect(
                new Rect(0f, 0f, Screen.width, Screen.height),
                new Color(0f, 0f, 0f, 0.66f)
            );

            float width = Mathf.Min(Screen.width - 48f, 500f * scale);
            float height = 238f * scale;
            Rect dialog = new(
                (Screen.width - width) * 0.5f,
                (Screen.height - height) * 0.5f,
                width,
                height
            );
            DrawPanel(dialog);

            string title;
            string body;
            switch (pendingAction)
            {
                case PendingAction.Save:
                    title = "覆盖这个存档槽？";
                    body = $"{SaveSystem.GetSlotDisplayName(pendingSlot)}已有进度。覆盖后，旧存档无法恢复。";
                    break;
                case PendingAction.Load:
                    title = "读取这个存档？";
                    body = $"将载入{SaveSystem.GetSlotDisplayName(pendingSlot)}。当前尚未保存的进度会丢失。";
                    break;
                default:
                    title = "永久删除这个存档？";
                    body = $"{SaveSystem.GetSlotDisplayName(pendingSlot)}删除后无法恢复。";
                    break;
            }

            GUI.Label(
                new Rect(
                    dialog.x + 26f * scale,
                    dialog.y + 18f * scale,
                    dialog.width - 52f * scale,
                    42f * scale
                ),
                title,
                UiTheme.Label(23, TextAnchor.MiddleLeft, bold: true)
            );
            GUI.Label(
                new Rect(
                    dialog.x + 26f * scale,
                    dialog.y + 66f * scale,
                    dialog.width - 52f * scale,
                    76f * scale
                ),
                body,
                UiTheme.Label(14)
            );

            float buttonY = dialog.yMax - 66f * scale;
            float buttonWidth = (dialog.width - 68f * scale) * 0.5f;
            if (GUI.Button(
                new Rect(
                    dialog.x + 26f * scale,
                    buttonY,
                    buttonWidth,
                    42f * scale
                ),
                "取消  [Esc]",
                UiTheme.Button(14)
            ))
            {
                CancelPending();
            }
            if (GUI.Button(
                new Rect(
                    dialog.x + 42f * scale + buttonWidth,
                    buttonY,
                    buttonWidth,
                    42f * scale
                ),
                "确认",
                UiTheme.Button(14)
            ))
            {
                ConfirmPending();
            }
        }

        private static string SafeArea(string areaName)
        {
            return string.IsNullOrWhiteSpace(areaName) ? "未知区域" : areaName;
        }

        private static int Count(List<string> values)
        {
            return values?.Count ?? 0;
        }

        private static string StageLabel(ChapterStage stage)
        {
            return stage switch
            {
                ChapterStage.Arrival => "初到广陵",
                ChapterStage.Investigate => "调查线索",
                ChapterStage.ReviewEvidence => "审阅证据",
                ChapterStage.StabilizeAnchors => "稳定时间锚",
                ChapterStage.RepairRift => "修复时缝",
                ChapterStage.FinalDecision => "最终抉择",
                ChapterStage.Resolved => "章节完成",
                _ => "未知阶段"
            };
        }

        private static string FormatSaveTime(string savedAtUtc, int slot)
        {
            if (!DateTime.TryParse(savedAtUtc, out DateTime savedAt))
            {
                savedAt = SaveSystem.GetSlotLastWriteTime(slot);
            }

            return savedAt == DateTime.MinValue
                ? "保存时间未知"
                : $"保存于 {savedAt.ToLocalTime():yyyy-MM-dd HH:mm}";
        }

        private static string GetArgumentValue(string prefix)
        {
            foreach (string argument in Environment.GetCommandLineArgs())
            {
                if (argument.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return argument.Substring(prefix.Length);
                }
            }

            return string.Empty;
        }

        private IEnumerator CapturePreview(string path)
        {
            yield return new WaitForSecondsRealtime(0.6f);
            yield return new WaitForEndOfFrame();
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            ScreenCapture.CaptureScreenshot(path);
            Debug.Log($"Save-slot preview captured to {path}");
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Demo02.Core;
using Demo02.Narrative;
using UnityEngine;
using UnityEngine.UI;

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

        private sealed class SlotView
        {
            public GameObject Root;
            public Outline Border;
            public Image Accent;
            public Text Title;
            public Text Badge;
            public Text Area;
            public Text Progress;
            public Text Time;
            public Text Empty;
            public Button Save;
            public Text SaveLabel;
            public Button Load;
            public Button Delete;
        }

        public static SaveSlotController Instance { get; private set; }

        private bool isOpen;
        private int selectedManualSlot = 1;
        private PendingAction pendingAction;
        private int pendingSlot;
        private string statusMessage;
        private float statusUntil;
        private SaveSystem saveSystem;
        private GameObject canvasObject;
        private Canvas canvas;
        private SlotView[] slots;
        private Text footerStatus;
        private GameObject confirmationRoot;
        private Text confirmationTitle;
        private Text confirmationBody;

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
            saveSystem = Demo02.Core.GameServices.Resolve<SaveSystem>();
            SaveSystem.HasSave();
            selectedManualSlot = SaveSystem.ActiveManualSlot > 0
                ? SaveSystem.ActiveManualSlot
                : 1;

            if (Array.IndexOf(
                Demo02.Core.RuntimeLaunchOptions.Arguments,
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
            if (!isOpen)
            {
                return;
            }

            if (Demo02Input.CancelPressed)
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
                if (Demo02Input.SubmitPressed)
                {
                    ConfirmPending();
                }
                else
                {
                    RefreshStatus();
                }
                return;
            }

            if (Demo02Input.NavigateUpPressed)
            {
                selectedManualSlot = Mathf.Max(
                    1,
                    selectedManualSlot - 1
                );
                RefreshSelection();
                RefreshStatus();
            }
            else if (Demo02Input.NavigateDownPressed)
            {
                selectedManualSlot = Mathf.Min(
                    SaveSystem.ManualSlotCount,
                    selectedManualSlot + 1
                );
                RefreshSelection();
                RefreshStatus();
            }
            else if (Demo02Input.SubmitPressed)
            {
                RequestLoad(selectedManualSlot);
                return;
            }

            if (Demo02Input.QuickSavePressed)
            {
                RequestSave(selectedManualSlot);
            }
            else if (Demo02Input.QuickLoadPressed)
            {
                RequestLoad(selectedManualSlot);
            }
            RefreshStatus();
        }

        private void OnDisable()
        {
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

        public void Open()
        {
            if (isOpen || GameplayUiState.HasOpenModalExcept(this))
            {
                return;
            }
            isOpen = true;
            canvas.enabled = true;
            GameplayUiState.RegisterModal(this);
            pendingAction = PendingAction.None;
            confirmationRoot.SetActive(false);
            selectedManualSlot = SaveSystem.ActiveManualSlot > 0
                ? SaveSystem.ActiveManualSlot
                : selectedManualSlot;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            RefreshSlots();
            GameAudio.PlayInteraction();
        }

        public void Close()
        {
            if (!isOpen)
            {
                return;
            }
            isOpen = false;
            canvas.enabled = false;
            GameplayUiState.UnregisterModal(this);
            pendingAction = PendingAction.None;
            confirmationRoot.SetActive(false);
            GameAudio.PlayInteraction();
        }

        private void EnsureUi()
        {
            if (canvasObject != null)
            {
                return;
            }
            RectTransform root = HudUiFactory.CreateCanvas(
                transform,
                "SaveSlotCanvas",
                102
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
                "SavePanel",
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(1060f, 620f),
                new Color(0.045f, 0.055f, 0.07f, 0.99f),
                raycastTarget: true
            );
            Outline outline = panel.gameObject.AddComponent<Outline>();
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;
            outline.effectColor = new Color(1f, 1f, 1f, 0.13f);
            Image top = HudUiFactory.CreateImage(
                panel,
                "TopLine",
                UiTheme.Accent
            );
            top.rectTransform.anchorMin = new Vector2(0f, 1f);
            top.rectTransform.anchorMax = new Vector2(1f, 1f);
            top.rectTransform.pivot = new Vector2(0.5f, 1f);
            top.rectTransform.anchoredPosition = Vector2.zero;
            top.rectTransform.sizeDelta = new Vector2(0f, 3f);

            CreateText(
                panel,
                "Eyebrow",
                "ARCHIVE · SAVE / LOAD",
                11,
                new Vector2(28f, -13f),
                new Vector2(1004f, 20f),
                TextAnchor.MiddleLeft,
                bold: true
            ).color = UiTheme.Accent;
            CreateText(
                panel,
                "Title",
                "存档与读档",
                27,
                new Vector2(28f, -32f),
                new Vector2(700f, 44f),
                TextAnchor.MiddleLeft,
                bold: true
            );
            CreateText(
                panel,
                "Subtitle",
                "自动存档 + 3 个手动槽位",
                12,
                new Vector2(810f, -38f),
                new Vector2(220f, 34f),
                TextAnchor.MiddleRight
            );

            slots = new SlotView[SaveSystem.ManualSlotCount + 1];
            for (int index = 0; index < slots.Length; index++)
            {
                int column = index % 2;
                int row = index / 2;
                slots[index] = CreateSlotCard(
                    panel,
                    index,
                    new Vector2(
                        28f + column * 509f,
                        -92f - row * 238f
                    )
                );
            }

            footerStatus = CreateText(
                panel,
                "FooterStatus",
                string.Empty,
                12,
                new Vector2(28f, -558f),
                new Vector2(850f, 50f),
                TextAnchor.MiddleLeft
            );
            CreateButton(
                panel,
                "CloseButton",
                "返回暂停  [Esc]",
                new Vector2(876f, -558f),
                new Vector2(156f, 50f),
                Close
            );
            CreateConfirmation(root);
        }

        private SlotView CreateSlotCard(
            Transform parent,
            int slot,
            Vector2 position)
        {
            RectTransform card = HudUiFactory.CreatePanel(
                parent,
                "SaveSlot" + slot,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                position,
                new Vector2(495f, 224f),
                UiTheme.Ink,
                raycastTarget: true
            );
            Outline border = card.gameObject.AddComponent<Outline>();
            border.effectDistance = new Vector2(1f, -1f);
            border.useGraphicAlpha = true;
            border.effectColor = new Color(1f, 1f, 1f, 0.09f);
            Image accent = HudUiFactory.CreateImage(
                card,
                "Accent",
                UiTheme.Accent
            );
            accent.rectTransform.anchorMin = new Vector2(0f, 0f);
            accent.rectTransform.anchorMax = new Vector2(0f, 1f);
            accent.rectTransform.pivot = new Vector2(0f, 0.5f);
            accent.rectTransform.anchoredPosition = Vector2.zero;
            accent.rectTransform.sizeDelta = new Vector2(3f, 0f);
            Text title = CreateText(
                card,
                "Title",
                SaveSystem.GetSlotDisplayName(slot),
                16,
                new Vector2(16f, -5f),
                new Vector2(370f, 28f),
                TextAnchor.MiddleLeft,
                bold: true
            );
            Text badge = CreateText(
                card,
                "Badge",
                slot == SaveSystem.AutoSlot ? "只读" : "手动",
                10,
                new Vector2(407f, -7f),
                new Vector2(72f, 22f),
                TextAnchor.MiddleCenter,
                bold: true
            );
            Text area = CreateText(
                card,
                "Area",
                string.Empty,
                12,
                new Vector2(16f, -34f),
                new Vector2(463f, 22f),
                TextAnchor.MiddleLeft,
                bold: true
            );
            Text progress = CreateText(
                card,
                "Progress",
                string.Empty,
                11,
                new Vector2(16f, -55f),
                new Vector2(463f, 22f),
                TextAnchor.MiddleLeft
            );
            Text time = CreateText(
                card,
                "Time",
                string.Empty,
                10,
                new Vector2(16f, -76f),
                new Vector2(463f, 20f),
                TextAnchor.MiddleLeft
            );
            Text empty = CreateText(
                card,
                "Empty",
                "空槽位\n可保存当前调查进度",
                12,
                new Vector2(16f, -42f),
                new Vector2(463f, 42f),
                TextAnchor.MiddleCenter
            );

            Button save = null;
            Text saveLabel = null;
            if (slot != SaveSystem.AutoSlot)
            {
                save = CreateButton(
                    card,
                    "SaveButton",
                    "保存",
                    new Vector2(16f, -181f),
                    new Vector2(181f, 32f),
                    () => RequestSave(slot)
                );
                saveLabel = save.GetComponentInChildren<Text>(true);
            }
            Button load = CreateButton(
                card,
                "LoadButton",
                slot == SaveSystem.AutoSlot
                    ? "读取自动存档"
                    : "读取",
                new Vector2(
                    slot == SaveSystem.AutoSlot ? 16f : 203f,
                    -181f
                ),
                new Vector2(
                    slot == SaveSystem.AutoSlot ? 463f : 140f,
                    32f
                ),
                () => RequestLoad(slot)
            );
            Button delete = null;
            if (slot != SaveSystem.AutoSlot)
            {
                delete = CreateButton(
                    card,
                    "DeleteButton",
                    "删除",
                    new Vector2(349f, -181f),
                    new Vector2(130f, 32f),
                    () => RequestDelete(slot)
                );
            }

            SlotView view = new()
            {
                Root = card.gameObject,
                Border = border,
                Accent = accent,
                Title = title,
                Badge = badge,
                Area = area,
                Progress = progress,
                Time = time,
                Empty = empty,
                Save = save,
                SaveLabel = saveLabel,
                Load = load,
                Delete = delete
            };
            return view;
        }

        private void CreateConfirmation(Transform root)
        {
            RectTransform overlay = HudUiFactory.CreatePanel(
                root,
                "ConfirmationOverlay",
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                new Color(0f, 0f, 0f, 0.66f),
                raycastTarget: true
            );
            confirmationRoot = overlay.gameObject;
            RectTransform dialog = HudUiFactory.CreatePanel(
                overlay,
                "Dialog",
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(500f, 238f),
                new Color(0.045f, 0.055f, 0.07f, 0.99f),
                raycastTarget: true
            );
            Outline outline = dialog.gameObject.AddComponent<Outline>();
            outline.effectDistance = new Vector2(1f, -1f);
            outline.effectColor = new Color(1f, 1f, 1f, 0.14f);
            confirmationTitle = CreateText(
                dialog,
                "Title",
                string.Empty,
                23,
                new Vector2(26f, -18f),
                new Vector2(448f, 42f),
                TextAnchor.MiddleLeft,
                bold: true
            );
            confirmationBody = CreateText(
                dialog,
                "Body",
                string.Empty,
                14,
                new Vector2(26f, -66f),
                new Vector2(448f, 76f),
                TextAnchor.UpperLeft
            );
            confirmationBody.horizontalOverflow =
                HorizontalWrapMode.Wrap;
            CreateButton(
                dialog,
                "CancelButton",
                "取消  [Esc]",
                new Vector2(26f, -172f),
                new Vector2(216f, 42f),
                CancelPending
            );
            CreateButton(
                dialog,
                "ConfirmButton",
                "确认",
                new Vector2(258f, -172f),
                new Vector2(216f, 42f),
                ConfirmPending
            );
            confirmationRoot.SetActive(false);
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

        private void RefreshSlots()
        {
            for (int slot = SaveSystem.AutoSlot;
                 slot <= SaveSystem.ManualSlotCount;
                 slot++)
            {
                SlotView view = slots[slot];
                bool hasSave = SaveSystem.TryReadSave(
                    slot,
                    out WorldSaveData data
                );
                view.Badge.text = slot == SaveSystem.AutoSlot
                    ? "只读"
                    : slot == selectedManualSlot
                        ? "快捷槽"
                        : "手动";
                view.Badge.color = slot == SaveSystem.AutoSlot
                    ? UiTheme.Muted
                    : UiTheme.Accent;
                view.Area.gameObject.SetActive(hasSave);
                view.Progress.gameObject.SetActive(hasSave);
                view.Time.gameObject.SetActive(hasSave);
                view.Empty.gameObject.SetActive(!hasSave);
                view.Load.interactable = hasSave;
                if (view.Save != null)
                {
                    view.Save.interactable = true;
                    view.SaveLabel.text = hasSave ? "覆盖" : "保存";
                }
                if (view.Delete != null)
                {
                    view.Delete.interactable = hasSave;
                }
                if (hasSave)
                {
                    view.Area.text =
                        $"{SafeArea(data.AreaName)} · "
                        + StageLabel(data.ChapterStage);
                    view.Progress.text =
                        $"线索 {Count(data.ClueIds)}   "
                        + $"时间锚 {Count(data.TimeAnchorIds)}   "
                        + $"回声 {Count(data.EchoIds)}";
                    view.Time.text =
                        FormatSaveTime(data.SavedAtUtc, slot);
                }
            }
            RefreshSelection();
            RefreshStatus();
        }

        private void RefreshSelection()
        {
            for (int slot = SaveSystem.AutoSlot;
                 slot <= SaveSystem.ManualSlotCount;
                 slot++)
            {
                SlotView view = slots[slot];
                bool selected = slot > SaveSystem.AutoSlot
                    && slot == selectedManualSlot;
                view.Badge.text = slot == SaveSystem.AutoSlot
                    ? "只读"
                    : selected
                        ? "快捷槽"
                        : "手动";
                view.Badge.color = slot == SaveSystem.AutoSlot
                    ? UiTheme.Muted
                    : UiTheme.Accent;
                view.Border.effectColor = selected
                    ? new Color(
                        UiTheme.Accent.r,
                        UiTheme.Accent.g,
                        UiTheme.Accent.b,
                        0.72f
                    )
                    : new Color(1f, 1f, 1f, 0.09f);
                view.Accent.enabled = selected;
            }
        }

        private void RefreshStatus()
        {
            string value = string.IsNullOrWhiteSpace(statusMessage)
                || Time.unscaledTime >= statusUntil
                    ? $"F5 保存到快捷槽 {selectedManualSlot}   ·   "
                        + $"F9 读取快捷槽 {selectedManualSlot}"
                    : statusMessage;
            HudUiFactory.SetText(footerStatus, value);
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
                ShowStatus(
                    $"{SaveSystem.GetSlotDisplayName(slot)}为空"
                );
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
            UpdateConfirmation();
            confirmationRoot.SetActive(true);
            GameAudio.PlayInteraction();
        }

        private void UpdateConfirmation()
        {
            switch (pendingAction)
            {
                case PendingAction.Save:
                    confirmationTitle.text = "覆盖这个存档槽？";
                    confirmationBody.text =
                        $"{SaveSystem.GetSlotDisplayName(pendingSlot)}"
                        + "已有进度。覆盖后，旧存档无法恢复。";
                    break;
                case PendingAction.Load:
                    confirmationTitle.text = "读取这个存档？";
                    confirmationBody.text =
                        $"将载入{SaveSystem.GetSlotDisplayName(pendingSlot)}。"
                        + "当前尚未保存的进度会丢失。";
                    break;
                default:
                    confirmationTitle.text = "永久删除这个存档？";
                    confirmationBody.text =
                        $"{SaveSystem.GetSlotDisplayName(pendingSlot)}"
                        + "删除后无法恢复。";
                    break;
            }
        }

        private void CancelPending()
        {
            pendingAction = PendingAction.None;
            confirmationRoot.SetActive(false);
            GameAudio.PlayInteraction();
        }

        private void ConfirmPending()
        {
            PendingAction action = pendingAction;
            int slot = pendingSlot;
            pendingAction = PendingAction.None;
            confirmationRoot.SetActive(false);
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
            saveSystem ??= Demo02.Core.GameServices.Resolve<SaveSystem>();
            if (saveSystem == null)
            {
                ShowStatus("存档系统尚未就绪");
                return;
            }
            selectedManualSlot = slot;
            saveSystem.SaveCurrentState(
                announce: true,
                slotIndex: slot
            );
            ShowStatus(
                $"已保存到{SaveSystem.GetSlotDisplayName(slot)}"
            );
            RefreshSlots();
        }

        private void ExecuteLoad(int slot)
        {
            if (SaveSystem.LoadSavedGame(slot, out _))
            {
                isOpen = false;
                canvas.enabled = false;
                GameplayUiState.UnregisterModal(this);
                return;
            }
            ShowStatus("读取失败，存档可能已损坏");
        }

        private void ExecuteDelete(int slot)
        {
            if (SaveSystem.DeleteSlot(slot))
            {
                ShowStatus(
                    $"已删除{SaveSystem.GetSlotDisplayName(slot)}"
                );
                GameAudio.PlayConfirm();
                RefreshSlots();
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
            RefreshStatus();
        }

        private static string SafeArea(string areaName)
        {
            return string.IsNullOrWhiteSpace(areaName)
                ? "未知区域"
                : areaName;
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
                ChapterStage.CrossExamineTestimony => "证词对照",
                ChapterStage.StabilizeAnchors => "稳定时间锚",
                ChapterStage.RepairRift => "修复时缝",
                ChapterStage.FinalDecision => "最终抉择",
                ChapterStage.Resolved => "章节完成",
                _ => "未知阶段"
            };
        }

        private static string FormatSaveTime(
            string savedAtUtc,
            int slot)
        {
            if (!DateTime.TryParse(
                    savedAtUtc,
                    out DateTime savedAt
                ))
            {
                savedAt = SaveSystem.GetSlotLastWriteTime(slot);
            }
            return savedAt == DateTime.MinValue
                ? "保存时间未知"
                : $"保存于 {savedAt.ToLocalTime():yyyy-MM-dd HH:mm}";
        }

        private static string GetArgumentValue(string prefix)
        {
            foreach (string argument in
                     Demo02.Core.RuntimeLaunchOptions.Arguments)
            {
                if (argument.StartsWith(
                        prefix,
                        StringComparison.Ordinal
                    ))
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

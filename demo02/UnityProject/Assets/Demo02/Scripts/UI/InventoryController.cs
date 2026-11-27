using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Demo02.Combat;
using Demo02.Core;
using Demo02.Narrative;
using Demo02.Player;
using UnityEngine;

namespace Demo02.UI
{
    public sealed class InventoryController : MonoBehaviour
    {
        private InventoryState inventoryState;
        private InvestigationState investigationState;
        private CaseDefinition caseContent;
        private PlayerPartyController party;
        private bool isOpen;
        private float previousTimeScale = 1f;
        private string selectedItemId;
        private string selectedCategory = "全部";
        private string statusMessage;
        private float statusUntil;

        public static InventoryController Instance { get; private set; }
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
            inventoryState = InventoryState.Instance != null
                ? InventoryState.Instance
                : Demo02.Core.GameServices.Resolve<InventoryState>();
            party = Demo02.Core.GameServices.Resolve<PlayerPartyController>();
            investigationState =
                Demo02.Core.GameServices.Resolve<InvestigationState>();
            caseContent = investigationState?.CaseContent;

            if (Array.IndexOf(
                Demo02.Core.RuntimeLaunchOptions.Arguments,
                "--preview-inventory"
            ) >= 0)
            {
                inventoryState?.AddItem("salt-token-charm", 1);
                inventoryState?.AddItem("ginger-tea", 3);
                inventoryState?.AddItem("blank-ledger-page", 2);
                selectedItemId = "ginger-tea";
                ToggleInventory();
                string path = GetArgumentValue("--capture-inventory=");
                if (!string.IsNullOrWhiteSpace(path))
                {
                    StartCoroutine(CapturePreview(path));
                }
            }
        }

        private void Update()
        {
            if (isOpen)
            {
                if (Demo02Input.CancelPressed
                    || Demo02Input.InventoryPressed)
                {
                    ToggleInventory();
                }
                return;
            }

            if (!Demo02Input.InventoryPressed
                || Time.timeScale <= 0f
                || IsBlockedByOtherUi())
            {
                return;
            }

            ToggleInventory();
        }

        public void ToggleInventory()
        {
            if (!isOpen
                && (Time.timeScale <= 0f
                    || GameplayUiState.HasOpenModalExcept(this)))
            {
                return;
            }

            isOpen = !isOpen;
            if (isOpen)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                GameplayUiState.RegisterModal(this);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                EnsureSelection();
                GameAudio.PlayInteraction();
            }
            else
            {
                Time.timeScale = previousTimeScale;
                GameplayUiState.UnregisterModal(this);
                GameAudio.PlayInteraction();
            }
        }

        private void OnDisable()
        {
            if (isOpen)
            {
                Time.timeScale = previousTimeScale;
                isOpen = false;
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
        }

        private bool IsBlockedByOtherUi()
        {
            return GameplayUiState.HasOpenModalExcept(this)
                || (ScreenFade.Instance != null
                    && ScreenFade.Instance.IsTransitioning);
        }

        private void OnGUI()
        {
            if (!isOpen || inventoryState == null || caseContent == null)
            {
                return;
            }

            GUI.depth = -108;
            UiTheme.DrawRect(
                new Rect(0f, 0f, Screen.width, Screen.height),
                new Color(0.012f, 0.017f, 0.025f, 0.96f)
            );

            float scale = UiTheme.Scale;
            float panelWidth = Mathf.Min(Screen.width - 48f, 1040f * scale);
            float panelHeight = Mathf.Min(Screen.height - 48f, 620f * scale);
            Rect panel = new(
                (Screen.width - panelWidth) * 0.5f,
                (Screen.height - panelHeight) * 0.5f,
                panelWidth,
                panelHeight
            );
            UiTheme.DrawRect(panel, new Color(0.045f, 0.055f, 0.07f, 0.99f));
            UiTheme.DrawBorder(panel, new Color(1f, 1f, 1f, 0.13f));
            UiTheme.DrawRect(
                new Rect(panel.x, panel.y, panel.width, 3f),
                UiTheme.Accent
            );

            GUIStyle eyebrow = UiTheme.Label(11, TextAnchor.MiddleLeft, bold: true);
            eyebrow.normal.textColor = UiTheme.Accent;
            GUI.Label(
                new Rect(
                    panel.x + 28f * scale,
                    panel.y + 13f * scale,
                    panel.width - 56f * scale,
                    20f * scale
                ),
                "PACK · INVENTORY",
                eyebrow
            );
            GUI.Label(
                new Rect(
                    panel.x + 28f * scale,
                    panel.y + 32f * scale,
                    panel.width - 250f * scale,
                    42f * scale
                ),
                "背包",
                UiTheme.Label(27, TextAnchor.MiddleLeft, bold: true)
            );
            GUI.Label(
                new Rect(
                    panel.xMax - 250f * scale,
                    panel.y + 37f * scale,
                    panel.width - 56f * scale,
                    34f * scale
                ),
                $"{inventoryState.DistinctItemCount} 种物品  ·  B / Esc 返回",
                UiTheme.Label(11, TextAnchor.MiddleRight)
            );

            float padding = 28f * scale;
            float gap = 18f * scale;
            float contentTop = panel.y + 88f * scale;
            float contentBottom = panel.yMax - 62f * scale;
            float contentWidth = panel.width - padding * 2f;
            float listWidth = contentWidth * 0.52f;
            float detailWidth = contentWidth - listWidth - gap;
            Rect list = new(
                panel.x + padding,
                contentTop,
                listWidth,
                contentBottom - contentTop
            );
            Rect detail = new(
                list.xMax + gap,
                contentTop,
                detailWidth,
                contentBottom - contentTop
            );

            DrawItemList(list, scale);
            DrawItemDetail(detail, scale);

            string footer = string.IsNullOrWhiteSpace(statusMessage)
                || Time.unscaledTime >= statusUntil
                ? "拾取物品会自动进入背包。消耗品可在右侧直接使用。"
                : statusMessage;
            GUI.Label(
                new Rect(
                    panel.x + padding,
                    panel.yMax - 46f * scale,
                    panel.width - padding * 2f,
                    26f * scale
                ),
                footer,
                UiTheme.Label(11)
            );
        }

        private void DrawItemList(Rect list, float scale)
        {
            GUI.Label(
                new Rect(list.x, list.y, list.width, 24f * scale),
                "物品列表",
                UiTheme.Label(15, TextAnchor.MiddleLeft, bold: true)
            );

            List<string> categories = BuildCategories();
            float tabGap = 5f * scale;
            float tabWidth = categories.Count > 0
                ? (list.width - tabGap * (categories.Count - 1))
                    / categories.Count
                : list.width;
            for (int categoryIndex = 0;
                categoryIndex < categories.Count;
                categoryIndex++)
            {
                string category = categories[categoryIndex];
                if (GUI.Button(
                    new Rect(
                        list.x + categoryIndex * (tabWidth + tabGap),
                        list.y + 27f * scale,
                        tabWidth,
                        25f * scale
                    ),
                    category,
                    UiTheme.Button(10)
                ))
                {
                    selectedCategory = category;
                    EnsureSelection();
                    GameAudio.PlayInteraction();
                }
            }

            float top = list.y + 62f * scale;
            int visibleCount = 0;
            for (int index = 0; index < caseContent.InventoryItems.Count; index++)
            {
                InventoryItemDefinition item =
                    caseContent.InventoryItems[index];
                if (inventoryState.GetCount(item.Id) <= 0
                    || !MatchesCategory(item))
                {
                    continue;
                }

                Rect card = new(
                    list.x,
                    top + visibleCount * 58f * scale,
                    list.width,
                    50f * scale
                );
                DrawItemCard(card, item);
                visibleCount++;
            }

            if (visibleCount == 0)
            {
                GUI.Label(
                    new Rect(
                        list.x,
                        top + 30f * scale,
                        list.width,
                        60f * scale
                    ),
                    "背包还是空的。\n在街区中寻找可拾取物品。",
                    UiTheme.Label(14, TextAnchor.MiddleCenter)
                );
            }
        }

        private void DrawItemCard(Rect card, InventoryItemDefinition item)
        {
            float scale = UiTheme.Scale;
            bool selected = item.Id == selectedItemId;
            UiTheme.DrawRect(
                card,
                selected
                    ? new Color(0.105f, 0.095f, 0.07f, 0.98f)
                    : UiTheme.Ink
            );
            UiTheme.DrawBorder(
                card,
                selected
                    ? new Color(0.88f, 0.7f, 0.38f, 0.72f)
                    : new Color(1f, 1f, 1f, 0.08f)
            );
            if (selected)
            {
                UiTheme.DrawRect(
                    new Rect(card.x, card.y, 3f * scale, card.height),
                    UiTheme.Accent
                );
            }

            GUI.Label(
                new Rect(
                    card.x + 15f * scale,
                    card.y + 4f * scale,
                    card.width - 86f * scale,
                    22f * scale
                ),
                item.DisplayName,
                UiTheme.Label(14, TextAnchor.MiddleLeft, bold: true)
            );
            GUI.Label(
                new Rect(
                    card.x + 15f * scale,
                    card.y + 25f * scale,
                    card.width - 86f * scale,
                    20f * scale
                ),
                item.Category,
                UiTheme.Label(10)
            );
            GUI.Label(
                new Rect(
                    card.xMax - 70f * scale,
                    card.y,
                    55f * scale,
                    card.height
                ),
                $"×{inventoryState.GetCount(item.Id)}",
                UiTheme.Label(14, TextAnchor.MiddleRight, bold: true)
            );
            if (GUI.Button(card, GUIContent.none, GUIStyle.none))
            {
                selectedItemId = item.Id;
                GameAudio.PlayInteraction();
            }
        }

        private void DrawItemDetail(Rect detail, float scale)
        {
            InventoryItemDefinition item = caseContent.FindInventoryItem(
                selectedItemId
            );
            if (item == null)
            {
                EnsureSelection();
                item = caseContent.FindInventoryItem(selectedItemId);
            }
            if (item == null)
            {
                return;
            }

            UiTheme.DrawRect(detail, UiTheme.Ink);
            UiTheme.DrawBorder(detail, new Color(1f, 1f, 1f, 0.09f));
            GUIStyle eyebrow = UiTheme.Label(10, TextAnchor.MiddleLeft, bold: true);
            eyebrow.normal.textColor = UiTheme.Accent;
            GUI.Label(
                new Rect(
                    detail.x + 20f * scale,
                    detail.y + 10f * scale,
                    detail.width - 40f * scale,
                    20f * scale
                ),
                item.Category.ToUpperInvariant(),
                eyebrow
            );
            GUI.Label(
                new Rect(
                    detail.x + 20f * scale,
                    detail.y + 36f * scale,
                    detail.width - 40f * scale,
                    42f * scale
                ),
                item.DisplayName,
                UiTheme.Label(24, TextAnchor.MiddleLeft, bold: true)
            );
            GUI.Label(
                new Rect(
                    detail.x + 20f * scale,
                    detail.y + 86f * scale,
                    detail.width - 40f * scale,
                    84f * scale
                ),
                item.Description,
                UiTheme.Label(14)
            );
            GUI.Label(
                new Rect(
                    detail.x + 20f * scale,
                    detail.y + 182f * scale,
                    detail.width - 40f * scale,
                    24f * scale
                ),
                $"持有 {inventoryState.GetCount(item.Id)} / {item.MaxStack}",
                UiTheme.Label(12)
            );

            string useLabel = item.Effect == InventoryItemEffect.RestoreHealth
                ? $"{FirstNonEmpty(item.UseLabel, "使用")}  ·  "
                    + $"恢复 {Mathf.RoundToInt(item.EffectAmount)} 生命"
                : item.Effect == InventoryItemEffect.GrantStoryFact
                    ? FirstNonEmpty(item.UseLabel, "使用剧情物品")
                    : "剧情物品 · 暂不可使用";

            bool canUse = item.Effect != InventoryItemEffect.None;
            if (item.Effect == InventoryItemEffect.GrantStoryFact)
            {
                canUse = !investigationState.HasFact(item.ResultStoryFactId)
                    && (string.IsNullOrWhiteSpace(item.RequiredStoryFactId)
                        || investigationState.HasFact(
                            item.RequiredStoryFactId
                        ));
            }
            if (item.Effect == InventoryItemEffect.RestoreHealth)
            {
                Combatant active = GetActiveCombatant();
                canUse = active != null
                    && active.IsAlive
                    && active.CurrentHealth < active.MaxHealth;
            }

            GUI.enabled = canUse;
            if (GUI.Button(
                new Rect(
                    detail.x + 20f * scale,
                    detail.yMax - 66f * scale,
                    detail.width - 40f * scale,
                    44f * scale
                ),
                useLabel,
                UiTheme.Button(14)
            ))
            {
                UseItem(item);
            }
            GUI.enabled = true;
        }

        private void UseItem(InventoryItemDefinition item)
        {
            if (inventoryState.TryUseItem(
                item.Id,
                GetActiveCombatant(),
                investigationState,
                out string message
            ))
            {
                ShowStatus(message);
                GameAudio.PlayConfirm();
                EnsureSelection();
            }
            else
            {
                ShowStatus(message);
            }
        }

        private Combatant GetActiveCombatant()
        {
            Transform character = party != null ? party.ActiveCharacter : null;
            return character != null ? character.GetComponent<Combatant>() : null;
        }

        private void EnsureSelection()
        {
            if (caseContent == null || inventoryState == null)
            {
                return;
            }
            InventoryItemDefinition selected =
                caseContent.FindInventoryItem(selectedItemId);
            if (selected != null
                && inventoryState.GetCount(selectedItemId) > 0
                && MatchesCategory(selected))
            {
                return;
            }

            selectedItemId = null;
            for (int index = 0; index < caseContent.InventoryItems.Count; index++)
            {
                InventoryItemDefinition item = caseContent.InventoryItems[index];
                if (inventoryState.GetCount(item.Id) > 0)
                {
                    if (!MatchesCategory(item))
                    {
                        continue;
                    }
                    selectedItemId = item.Id;
                    return;
                }
            }
        }

        private List<string> BuildCategories()
        {
            List<string> categories = new() { "全部" };
            for (int index = 0; index < caseContent.InventoryItems.Count; index++)
            {
                string category = caseContent.InventoryItems[index].Category;
                if (!string.IsNullOrWhiteSpace(category)
                    && !categories.Contains(category))
                {
                    categories.Add(category);
                }
            }
            return categories;
        }

        private bool MatchesCategory(InventoryItemDefinition item)
        {
            return selectedCategory == "全部"
                || item.Category == selectedCategory;
        }

        private static string FirstNonEmpty(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }

        private void ShowStatus(string message)
        {
            statusMessage = message;
            statusUntil = Time.unscaledTime + 3f;
        }

        private static string GetArgumentValue(string prefix)
        {
            foreach (string argument in Demo02.Core.RuntimeLaunchOptions.Arguments)
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
            Debug.Log($"Inventory preview captured to {path}");
        }
    }
}

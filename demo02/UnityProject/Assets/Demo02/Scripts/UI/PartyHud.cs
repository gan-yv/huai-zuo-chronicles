using Demo02.Combat;
using Demo02.Core;
using Demo02.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Demo02.UI
{
    public sealed class PartyHud : MonoBehaviour
    {
        private sealed class SlotView
        {
            public GameObject Root;
            public Image Background;
            public Outline Border;
            public Image Accent;
            public Text Index;
            public Text Name;
            public Text Locked;
            public RectTransform HealthFill;
            public RectTransform EnergyFill;
            public Button Button;
            public string CachedName;
        }

        private static readonly Color ActiveCardColor =
            new(0.075f, 0.09f, 0.105f, 0.97f);
        private static readonly Color MutedCardColor =
            new(0.03f, 0.04f, 0.055f, 0.9f);
        private static readonly Color HealthColor =
            new(0.38f, 0.72f, 0.42f);
        private static readonly Color CompanionColor =
            new(0.32f, 0.74f, 0.78f);

        private PlayerPartyController party;
        private WorkFishCompanion companion;
        private GameObject canvasObject;
        private Canvas canvas;
        private SlotView[] slots;
        private GameObject companionCard;
        private Text companionPrompt;
        private GameObject staminaPanel;
        private RectTransform staminaFill;
        private Text staminaText;
        private string cachedStaminaText;

        private void Awake()
        {
            party = Demo02.Core.GameServices.Resolve<PlayerPartyController>();
            companion = Demo02.Core.GameServices.Resolve<WorkFishCompanion>();
            EnsureUi();
        }

        private void Update()
        {
            EnsureUi();
            bool blocked = party == null
                || GameplayUiState.IsGameplayBlocked;
            canvas.enabled = !blocked;
            if (blocked)
            {
                return;
            }

            RefreshPartyCards();
            RefreshCompanion();
            RefreshStamina();
        }

        private void OnDisable()
        {
            if (canvas != null)
            {
                canvas.enabled = false;
            }
        }

        private void OnEnable()
        {
            if (canvas != null)
            {
                canvas.enabled = true;
            }
        }

        private void OnDestroy()
        {
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
                "PartyHudCanvas",
                19
            );
            canvasObject = root.gameObject;
            canvas = root.GetComponent<Canvas>();

            slots = new SlotView[PlayerPartyController.MaxPartySlots];
            for (int index = 0; index < slots.Length; index++)
            {
                slots[index] = CreateSlot(root, index);
            }
            CreateCompanionCard(root);
            CreateStamina(root);
        }

        private SlotView CreateSlot(Transform root, int index)
        {
            float top = 88f + index * 52f;
            RectTransform card = HudUiFactory.CreatePanel(
                root,
                $"PartySlot{index + 1}",
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-18f, -top),
                new Vector2(172f, 46f),
                MutedCardColor,
                raycastTarget: true
            );
            SlotView view = new()
            {
                Root = card.gameObject,
                Background = card.GetComponent<Image>()
            };
            view.Border = card.gameObject.AddComponent<Outline>();
            view.Border.effectDistance = new Vector2(1f, -1f);
            view.Border.useGraphicAlpha = true;

            view.Accent = HudUiFactory.CreateImage(
                card,
                "Accent",
                new Color(1f, 1f, 1f, 0.1f)
            );
            RectTransform accentRect = view.Accent.rectTransform;
            accentRect.anchorMin = new Vector2(0f, 0f);
            accentRect.anchorMax = new Vector2(0f, 1f);
            accentRect.pivot = new Vector2(0f, 0.5f);
            accentRect.anchoredPosition = Vector2.zero;
            accentRect.sizeDelta = new Vector2(3f, 0f);

            view.Index = HudUiFactory.CreateText(
                card,
                "Index",
                (index + 1).ToString(),
                10,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 0.5f),
                new Vector2(7f, -23f),
                new Vector2(26f, 46f),
                TextAnchor.MiddleCenter,
                bold: true
            );

            view.Name = HudUiFactory.CreateText(
                card,
                "Name",
                string.Empty,
                12,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f),
                new Vector2(34f, -3f),
                new Vector2(0f, 21f),
                TextAnchor.MiddleLeft,
                bold: true
            );
            view.Name.rectTransform.offsetMin =
                new Vector2(34f, view.Name.rectTransform.offsetMin.y);
            view.Name.rectTransform.offsetMax =
                new Vector2(-8f, -3f);

            view.Locked = HudUiFactory.CreateText(
                card,
                "Locked",
                "席 位 待 解 锁",
                11,
                new Vector2(0f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 0.5f),
                new Vector2(34f, 0f),
                new Vector2(0f, 0f),
                TextAnchor.MiddleLeft
            );
            view.Locked.rectTransform.offsetMin =
                new Vector2(34f, 0f);
            view.Locked.rectTransform.offsetMax =
                new Vector2(-8f, 0f);

            RectTransform healthBackground =
                HudUiFactory.CreatePanel(
                    card,
                    "HealthBackground",
                    new Vector2(0f, 0f),
                    new Vector2(0f, 0f),
                    new Vector2(0f, 0f),
                    new Vector2(35f, 17f),
                    new Vector2(127f, 5f),
                    new Color(0f, 0f, 0f, 0.45f)
                );
            Image health = HudUiFactory.CreateImage(
                healthBackground,
                "HealthFill",
                HealthColor
            );
            view.HealthFill = health.rectTransform;
            view.HealthFill.anchorMin = Vector2.zero;
            view.HealthFill.anchorMax = Vector2.one;
            view.HealthFill.pivot = new Vector2(0f, 0.5f);
            view.HealthFill.offsetMin = Vector2.zero;
            view.HealthFill.offsetMax = Vector2.zero;

            RectTransform energyBackground =
                HudUiFactory.CreatePanel(
                    card,
                    "EnergyBackground",
                    new Vector2(0f, 0f),
                    new Vector2(0f, 0f),
                    new Vector2(0f, 0f),
                    new Vector2(35f, 14f),
                    new Vector2(127f, 2f),
                    new Color(0f, 0f, 0f, 0.3f)
                );
            Image energy = HudUiFactory.CreateImage(
                energyBackground,
                "EnergyFill",
                UiTheme.Accent
            );
            view.EnergyFill = energy.rectTransform;
            view.EnergyFill.anchorMin = Vector2.zero;
            view.EnergyFill.anchorMax = Vector2.one;
            view.EnergyFill.pivot = new Vector2(0f, 0.5f);
            view.EnergyFill.offsetMin = Vector2.zero;
            view.EnergyFill.offsetMax = Vector2.zero;

            int slotIndex = index;
            view.Button = card.gameObject.AddComponent<Button>();
            view.Button.targetGraphic = view.Background;
            view.Button.transition = Selectable.Transition.None;
            view.Button.onClick.AddListener(
                () => SwitchCharacter(slotIndex)
            );
            return view;
        }

        private void CreateCompanionCard(Transform root)
        {
            RectTransform card = HudUiFactory.CreatePanel(
                root,
                "CompanionCard",
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-18f, -302f),
                new Vector2(172f, 42f),
                new Color(0.025f, 0.055f, 0.075f, 0.92f)
            );
            companionCard = card.gameObject;
            Outline outline = card.gameObject.AddComponent<Outline>();
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;
            outline.effectColor =
                new Color(0.32f, 0.74f, 0.78f, 0.72f);

            Image accent = HudUiFactory.CreateImage(
                card,
                "Accent",
                CompanionColor
            );
            RectTransform accentRect = accent.rectTransform;
            accentRect.anchorMin = new Vector2(0f, 0f);
            accentRect.anchorMax = new Vector2(0f, 1f);
            accentRect.pivot = new Vector2(0f, 0.5f);
            accentRect.anchoredPosition = Vector2.zero;
            accentRect.sizeDelta = new Vector2(3f, 0f);

            Text name = HudUiFactory.CreateText(
                card,
                "CompanionName",
                "随行 · 阿鱼",
                12,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f),
                new Vector2(12f, -4f),
                new Vector2(0f, 20f),
                TextAnchor.MiddleLeft,
                bold: true
            );
            name.color = new Color(0.62f, 0.9f, 0.9f);
            name.rectTransform.offsetMin =
                new Vector2(12f, name.rectTransform.offsetMin.y);
            name.rectTransform.offsetMax = new Vector2(-8f, -4f);

            companionPrompt = HudUiFactory.CreateText(
                card,
                "CompanionState",
                string.Empty,
                9,
                new Vector2(0f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 0.5f),
                new Vector2(12f, 0f),
                new Vector2(0f, 0f),
                TextAnchor.MiddleLeft
            );
            companionPrompt.color =
                new Color(0.62f, 0.7f, 0.74f);
            companionPrompt.rectTransform.offsetMin =
                new Vector2(12f, 0f);
            companionPrompt.rectTransform.offsetMax =
                new Vector2(-8f, 0f);
        }

        private void CreateStamina(Transform root)
        {
            RectTransform panel = HudUiFactory.CreatePanel(
                root,
                "StaminaPanel",
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 190f),
                new Vector2(310f, 24f),
                new Color(0.02f, 0.027f, 0.038f, 0.88f)
            );
            staminaPanel = panel.gameObject;
            RectTransform bar = HudUiFactory.CreatePanel(
                panel,
                "StaminaBackground",
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(10f, 6f),
                new Vector2(290f, 5f),
                new Color(0f, 0f, 0f, 0.5f)
            );
            Image fill = HudUiFactory.CreateImage(
                bar,
                "StaminaFill",
                new Color(0.88f, 0.75f, 0.38f)
            );
            staminaFill = fill.rectTransform;
            staminaFill.anchorMin = Vector2.zero;
            staminaFill.anchorMax = Vector2.one;
            staminaFill.pivot = new Vector2(0f, 0.5f);
            staminaFill.offsetMin = Vector2.zero;
            staminaFill.offsetMax = Vector2.zero;

            staminaText = HudUiFactory.CreateText(
                panel,
                "StaminaText",
                string.Empty,
                9,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(10f, -1f),
                new Vector2(180f, 14f),
                TextAnchor.MiddleLeft,
                bold: true
            );
            staminaText.color =
                new Color(0.88f, 0.75f, 0.38f);
        }

        private void RefreshPartyCards()
        {
            for (int index = 0; index < slots.Length; index++)
            {
                SlotView view = slots[index];
                Transform character = index < party.PartySlotCount
                    ? party.GetCharacter(index)
                    : null;
                CharacterRuntime runtime = character != null
                    ? character.GetComponent<CharacterRuntime>()
                    : null;
                CharacterDefinition definition =
                    runtime != null ? runtime.Definition : null;
                bool available = character != null;
                bool active = index == party.ActiveIndex;
                Color accent = GetAccent(definition);

                view.Button.interactable = available;
                view.Background.color = active
                    ? ActiveCardColor
                    : MutedCardColor;
                view.Border.effectColor = active
                    ? accent
                    : new Color(
                        1f,
                        1f,
                        1f,
                        available ? 0.12f : 0.06f
                    );
                view.Accent.color = active
                    ? accent
                    : new Color(1f, 1f, 1f, 0.1f);
                view.Index.color = active ? accent : UiTheme.Muted;
                view.Locked.gameObject.SetActive(!available);
                view.Name.gameObject.SetActive(available);
                if (!available)
                {
                    view.HealthFill.parent.gameObject.SetActive(false);
                    view.EnergyFill.parent.gameObject.SetActive(false);
                    continue;
                }

                view.HealthFill.parent.gameObject.SetActive(true);
                view.EnergyFill.parent.gameObject.SetActive(true);
                Combatant combatant =
                    character.GetComponent<Combatant>();
                float health = combatant != null
                    ? Mathf.Clamp01(
                        combatant.CurrentHealth
                            / combatant.MaxHealth
                    )
                    : 1f;
                RectTransform healthParent =
                    view.HealthFill.parent as RectTransform;
                if (healthParent != null)
                {
                    healthParent.sizeDelta = new Vector2(
                        Mathf.Lerp(
                            healthParent.sizeDelta.x,
                            127f * health,
                            0.3f
                        ),
                        healthParent.sizeDelta.y
                    );
                }
                view.EnergyFill.localScale = new Vector3(
                    runtime != null
                        ? Mathf.Clamp01(runtime.EnergyRatio)
                        : 0f,
                    1f,
                    1f
                );

                string name = definition != null
                    ? GetMarkLabel(definition.ResonanceMark)
                        + " · "
                        + definition.DisplayName
                    : character.name;
                if (view.CachedName != name)
                {
                    view.CachedName = name;
                    view.Name.text = name;
                }
            }
        }

        private void RefreshCompanion()
        {
            companionCard.SetActive(companion != null);
            if (companion == null)
            {
                return;
            }
            string interact =
                Demo02Input.GetPreferredBindingDisplay("Interact");
            HudUiFactory.SetText(
                companionPrompt,
                $"非战斗伙伴 · 靠近按 {interact} 交谈"
            );
        }

        private void RefreshStamina()
        {
            Transform active = party.ActiveCharacter;
            CharacterRuntime runtime = active != null
                ? active.GetComponent<CharacterRuntime>()
                : null;
            ThirdPersonController controller = active != null
                ? active.GetComponent<ThirdPersonController>()
                : null;
            if (runtime == null)
            {
                staminaPanel.SetActive(false);
                return;
            }

            bool moving = controller != null
                && (controller.IsSprinting || controller.IsGliding);
            bool visible = moving || runtime.StaminaRatio < 0.999f;
            staminaPanel.SetActive(visible);
            if (!visible)
            {
                return;
            }

            staminaFill.localScale = new Vector3(
                Mathf.Clamp01(runtime.StaminaRatio),
                1f,
                1f
            );
            bool low = runtime.StaminaRatio < 0.22f;
            Color color = low
                ? new Color(0.94f, 0.42f, 0.22f)
                : new Color(0.88f, 0.75f, 0.38f);
            ((Image)staminaFill.GetComponent<Image>()).color = color;
            staminaText.color = color;

            string state = controller != null
                && controller.IsGliding
                    ? "滑翔"
                    : controller != null
                        && controller.IsSprinting
                        ? "冲刺"
                        : "体力";
            string value =
                $"{state}  {Mathf.CeilToInt(runtime.CurrentStamina)}";
            if (value != cachedStaminaText)
            {
                cachedStaminaText = value;
                staminaText.text = value;
            }
        }

        private void SwitchCharacter(int index)
        {
            if (!GameplayUiState.IsGameplayBlocked)
            {
                party.SwitchCharacter(index);
            }
        }

        private static Color GetAccent(
            CharacterDefinition definition)
        {
            return definition != null
                && definition.AccentColor.a > 0f
                    ? definition.AccentColor
                    : UiTheme.Accent;
        }

        private static string GetMarkLabel(ResonanceMark mark)
        {
            return mark switch
            {
                ResonanceMark.Continuation => "续",
                ResonanceMark.Order => "镇",
                ResonanceMark.Path => "途",
                ResonanceMark.War => "烽",
                ResonanceMark.Mirror => "镜",
                ResonanceMark.Growth => "芽",
                ResonanceMark.Erosion => "蚀",
                ResonanceMark.Balance => "衡",
                _ => "印"
            };
        }
    }
}

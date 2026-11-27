using Demo02.Combat;
using Demo02.Core;
using Demo02.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Demo02.UI
{
    /// <summary>
    /// UGUI-based ability HUD. The visual tree is created once and only its
    /// values are updated, avoiding per-frame IMGUI layout allocation.
    /// </summary>
    public sealed class AbilityHud : MonoBehaviour
    {
        private static readonly Color CardColor =
            new(0.025f, 0.035f, 0.05f, 0.94f);
        private static readonly Color MutedColor =
            new(0.62f, 0.66f, 0.7f, 1f);
        private static readonly Color ReactionColor =
            new(0.03f, 0.045f, 0.055f, 0.94f);

        private PlayerPartyController party;
        private GameObject canvasObject;
        private Canvas canvas;
        private Image skillBackground;
        private Image skillCooldownOverlay;
        private Image burstBackground;
        private Image burstEnergyFill;
        private Text skillKey;
        private Text skillName;
        private Text skillStatus;
        private Text burstKey;
        private Text burstName;
        private Text burstStatus;
        private CanvasGroup reactionGroup;
        private Image reactionBackground;
        private Image reactionAccent;
        private Text reactionLabel;
        private RectTransform reactionRect;
        private float nextRefresh;
        private string cachedSkillStatus = string.Empty;
        private string cachedBurstStatus = string.Empty;
        private string cachedReaction = string.Empty;

        private void Awake()
        {
            party = Demo02.Core.GameServices.Resolve<PlayerPartyController>();
        }

        private void Start()
        {
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

            Transform active = party.ActiveCharacter;
            CharacterRuntime runtime = active != null
                ? active.GetComponent<CharacterRuntime>()
                : null;
            CharacterAbilityController abilities = active != null
                ? active.GetComponent<CharacterAbilityController>()
                : null;
            PlayerCombat combat = active != null
                ? active.GetComponent<PlayerCombat>()
                : null;
            if (runtime?.Definition == null || abilities == null)
            {
                canvas.enabled = false;
                return;
            }

            ApplyAbilityState(runtime, abilities);
            ApplyReactionState(runtime.Definition, combat);
        }

        private void OnDisable()
        {
            if (canvas != null)
            {
                canvas.enabled = false;
            }
        }

        private void OnDestroy()
        {
            if (canvasObject != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(canvasObject);
                }
                else
                {
                    DestroyImmediate(canvasObject);
                }
            }
        }

        private void EnsureUi()
        {
            if (canvasObject != null)
            {
                return;
            }

            canvasObject = new GameObject(
                "AbilityHudCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler)
            );
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform root = canvasObject.GetComponent<RectTransform>();
            Stretch(root);

            RectTransform skillCard = CreateCard(
                root,
                "SkillCard",
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(-154f, 148f),
                new Vector2(126f, 50f)
            );
            skillBackground = skillCard.GetComponent<Image>();
            skillKey = CreateText(
                skillCard,
                "SkillKey",
                "F",
                12,
                new Vector2(0f, 1f),
                new Vector2(8f, -1f),
                new Vector2(22f, 20f)
            );
            skillName = CreateText(
                skillCard,
                "SkillName",
                string.Empty,
                11,
                new Vector2(0f, 1f),
                new Vector2(30f, -3f),
                new Vector2(90f, 21f)
            );
            skillStatus = CreateText(
                skillCard,
                "SkillStatus",
                "就绪",
                9,
                new Vector2(0f, 0f),
                new Vector2(9f, 7f),
                new Vector2(108f, 17f)
            );
            skillCooldownOverlay = CreateImage(
                skillCard,
                "CooldownOverlay",
                new Color(0f, 0f, 0f, 0.68f)
            );
            skillCooldownOverlay.transform.SetSiblingIndex(1);
            RectTransform cooldownRect =
                skillCooldownOverlay.rectTransform;
            cooldownRect.anchorMin = new Vector2(0f, 0f);
            cooldownRect.anchorMax = new Vector2(1f, 0f);
            cooldownRect.pivot = new Vector2(0.5f, 0f);
            cooldownRect.offsetMin = Vector2.zero;
            cooldownRect.offsetMax = Vector2.zero;
            skillCooldownOverlay.raycastTarget = false;

            RectTransform burstCard = CreateCard(
                root,
                "BurstCard",
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(-18f, 148f),
                new Vector2(126f, 50f)
            );
            burstBackground = burstCard.GetComponent<Image>();
            burstKey = CreateText(
                burstCard,
                "BurstKey",
                "R",
                12,
                new Vector2(0f, 1f),
                new Vector2(8f, -1f),
                new Vector2(22f, 20f)
            );
            burstName = CreateText(
                burstCard,
                "BurstName",
                string.Empty,
                11,
                new Vector2(0f, 1f),
                new Vector2(30f, -3f),
                new Vector2(90f, 21f)
            );
            burstStatus = CreateText(
                burstCard,
                "BurstStatus",
                string.Empty,
                9,
                new Vector2(0f, 0f),
                new Vector2(9f, 25f),
                new Vector2(108f, 16f)
            );
            burstEnergyFill = CreateImage(
                burstCard,
                "EnergyFill",
                Color.white
            );
            RectTransform energyRect =
                burstEnergyFill.rectTransform;
            energyRect.anchorMin = new Vector2(0f, 0f);
            energyRect.anchorMax = new Vector2(0f, 0f);
            energyRect.pivot = new Vector2(0f, 0.5f);
            energyRect.anchoredPosition = new Vector2(9f, 9f);
            energyRect.sizeDelta = new Vector2(108f, 4f);

            reactionRect = CreatePanel(
                root,
                "Reaction",
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 203f),
                new Vector2(300f, 28f),
                ReactionColor
            );
            reactionGroup =
                reactionRect.gameObject.AddComponent<CanvasGroup>();
            reactionGroup.blocksRaycasts = false;
            reactionGroup.interactable = false;
            reactionBackground =
                reactionRect.GetComponent<Image>();
            reactionAccent = CreateImage(
                reactionRect,
                "Accent",
                UiTheme.Accent
            );
            RectTransform accentRect = reactionAccent.rectTransform;
            accentRect.anchorMin = new Vector2(0f, 0f);
            accentRect.anchorMax = new Vector2(0f, 1f);
            accentRect.pivot = new Vector2(0f, 0.5f);
            accentRect.sizeDelta = new Vector2(2f, 0f);
            reactionLabel = CreateText(
                reactionRect,
                "ReactionLabel",
                string.Empty,
                12,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(284f, 26f),
                TextAnchor.MiddleCenter,
                bold: true
            );
            reactionGroup.alpha = 0f;
        }

        private void ApplyAbilityState(
            CharacterRuntime runtime,
            CharacterAbilityController abilities)
        {
            CharacterDefinition definition = runtime.Definition;
            Color accent = definition.AccentColor;
            SetAccent(skillBackground, accent);
            SetAccent(
                burstBackground,
                abilities.IsBurstReady
                    ? accent
                    : new Color(1f, 1f, 1f, 0.14f)
            );
            skillKey.color = accent;
            burstKey.color = accent;
            SetText(skillName, definition.SkillName);
            SetText(burstName, definition.BurstName);

            float cooldownRatio = Mathf.Clamp01(
                abilities.SkillCooldownRatio
            );
            RectTransform coolRect =
                skillCooldownOverlay.rectTransform;
            coolRect.anchorMax = new Vector2(1f, cooldownRatio);
            skillStatus.color = cooldownRatio > 0f
                ? Color.white
                : MutedColor;

            float energyRatio = Mathf.Clamp01(runtime.EnergyRatio);
            RectTransform energyRect =
                burstEnergyFill.rectTransform;
            energyRect.sizeDelta = new Vector2(
                108f * energyRatio,
                4f
            );
            burstEnergyFill.color = accent;

            if (Time.unscaledTime >= nextRefresh)
            {
                nextRefresh = Time.unscaledTime + 0.1f;
                string skillText =
                    abilities.SkillCooldownRemaining > 0f
                        ? $"{abilities.SkillCooldownRemaining:0.0}s"
                        : "核心续式 · 就绪";
                string burstText = abilities.IsBurstReady
                    ? "终结技已充能"
                    : $"能量 {Mathf.FloorToInt(runtime.CurrentEnergy)} / "
                        + $"{Mathf.FloorToInt(runtime.MaxEnergy)}";
                if (skillText != cachedSkillStatus)
                {
                    cachedSkillStatus = skillText;
                    skillStatus.text = skillText;
                }
                if (burstText != cachedBurstStatus)
                {
                    cachedBurstStatus = burstText;
                    burstStatus.text = burstText;
                }
            }
        }

        private void ApplyReactionState(
            CharacterDefinition definition,
            PlayerCombat combat)
        {
            bool showCounter =
                combat != null && combat.IsPerfectDodgeCounterReady;
            bool showReaction =
                Time.unscaledTime < ResonanceStatus.LastReactionUntil;
            bool visible = showReaction || showCounter;
            reactionGroup.alpha = visible ? 1f : 0f;
            if (!visible)
            {
                return;
            }

            Color accent = showCounter
                ? new Color(1f, 0.76f, 0.3f)
                : definition.AccentColor;
            reactionAccent.color = accent;
            reactionBackground.color = ReactionColor;
            reactionLabel.color = accent;
            string reaction = showCounter
                ? "见隙 · 下一击强化"
                : $"{GetMarkLabel(ResonanceStatus.LastPreviousMark)} × "
                    + $"{GetMarkLabel(ResonanceStatus.LastAppliedMark)}"
                    + " · 时势错位";
            if (reaction != cachedReaction)
            {
                cachedReaction = reaction;
                reactionLabel.text = reaction;
            }
        }

        private static RectTransform CreateCard(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            RectTransform rect = CreatePanel(
                parent,
                name,
                anchorMin,
                anchorMax,
                anchoredPosition,
                size,
                CardColor
            );
            Outline outline =
                rect.gameObject.AddComponent<Outline>();
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;
            return rect;
        }

        private static RectTransform CreatePanel(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 size,
            Color color)
        {
            GameObject gameObject = new(
                name,
                typeof(RectTransform),
                typeof(Image)
            );
            gameObject.transform.SetParent(parent, false);
            RectTransform rect =
                gameObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = anchorMin;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            Image image = gameObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        private static Image CreateImage(
            Transform parent,
            string name,
            Color color)
        {
            GameObject gameObject =
                new(name, typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            Image image = gameObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            string content,
            int fontSize,
            Vector2 anchor,
            Vector2 anchoredPosition,
            Vector2 size,
            TextAnchor alignment = TextAnchor.MiddleLeft,
            bool bold = false)
        {
            GameObject gameObject =
                new(name, typeof(RectTransform), typeof(Text));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect =
                gameObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Text text = gameObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>(
                "LegacyRuntime.ttf"
            );
            text.fontSize = fontSize;
            text.fontStyle = bold
                ? FontStyle.Bold
                : FontStyle.Normal;
            text.alignment = alignment;
            text.color = MutedColor;
            text.horizontalOverflow =
                HorizontalWrapMode.Overflow;
            text.verticalOverflow =
                VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            text.text = content;
            return text;
        }

        private static void SetAccent(Image image, Color accent)
        {
            image.color = CardColor;
            Outline outline = image.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = accent;
            }
        }

        private static void SetText(Text target, string value)
        {
            if (target != null && target.text != value)
            {
                target.text = value;
            }
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
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

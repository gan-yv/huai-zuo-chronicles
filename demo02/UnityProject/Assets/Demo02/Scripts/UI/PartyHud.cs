using Demo02.Combat;
using Demo02.Core;
using Demo02.Narrative;
using Demo02.Player;
using UnityEngine;

namespace Demo02.UI
{
    public sealed class PartyHud : MonoBehaviour
    {
        private PlayerPartyController party;
        private WorkFishCompanion companion;
        private float cachedScale = -1f;
        private GUIStyle activeSlotStyle;
        private GUIStyle mutedSlotStyle;
        private GUIStyle lockedSlotStyle;
        private GUIStyle memberNameStyle;
        private GUIStyle staminaStyle;
        private GUIStyle lowStaminaStyle;
        private GUIStyle companionNameStyle;
        private GUIStyle companionStateStyle;

        private void Awake()
        {
            party = FindAnyObjectByType<PlayerPartyController>();
            companion = FindAnyObjectByType<WorkFishCompanion>();
        }

        private void OnGUI()
        {
            if (party == null || IsBlockedByOtherUi())
            {
                return;
            }

            EnsureStyles();
            DrawPartyCards();
            DrawCompanionCard();
            DrawStamina();
        }

        private void DrawPartyCards()
        {
            float scale = UiTheme.Scale;
            float width = 172f * scale;
            float height = 46f * scale;
            float gap = 6f * scale;
            float right = Screen.width - 18f * scale;
            float top = 88f * scale;

            for (int index = 0; index < party.PartySlotCount; index++)
            {
                Rect card = new(
                    right - width,
                    top + index * (height + gap),
                    width,
                    height
                );
                Transform character = party.GetCharacter(index);
                CharacterRuntime runtime = character != null
                    ? character.GetComponent<CharacterRuntime>()
                    : null;
                CharacterDefinition definition = runtime != null
                    ? runtime.Definition
                    : null;
                bool active = index == party.ActiveIndex;
                bool available = character != null;
                if (active)
                {
                    activeSlotStyle.normal.textColor = GetAccent(definition);
                }

                if (available
                    && GUI.Button(card, GUIContent.none, GUIStyle.none))
                {
                    party.SwitchCharacter(index);
                }

                UiTheme.DrawRect(
                    card,
                    active
                        ? new Color(0.075f, 0.09f, 0.105f, 0.97f)
                        : new Color(0.03f, 0.04f, 0.055f, 0.9f)
                );
                UiTheme.DrawBorder(
                    card,
                    active
                        ? GetAccent(definition)
                        : new Color(1f, 1f, 1f, available ? 0.12f : 0.06f)
                );
                UiTheme.DrawRect(
                    new Rect(card.x, card.y, 3f * scale, card.height),
                    active
                        ? GetAccent(definition)
                        : new Color(1f, 1f, 1f, 0.1f)
                );

                GUI.Label(
                    new Rect(
                        card.x + 7f * scale,
                        card.y,
                        26f * scale,
                        card.height
                    ),
                    $"{index + 1}",
                    active ? activeSlotStyle : mutedSlotStyle
                );

                if (!available)
                {
                    GUI.Label(
                        new Rect(
                            card.x + 34f * scale,
                            card.y,
                            card.width - 42f * scale,
                            card.height
                        ),
                        "席 位 待 解 锁",
                        lockedSlotStyle
                    );
                    continue;
                }

                string markLabel = definition != null
                    ? GetMarkLabel(definition.ResonanceMark)
                    : "印";
                string nameLabel = definition != null
                    ? definition.DisplayName
                    : character.name;
                GUI.Label(
                    new Rect(
                        card.x + 34f * scale,
                        card.y + 3f * scale,
                        card.width - 42f * scale,
                        21f * scale
                    ),
                    $"{markLabel} · {nameLabel}",
                    memberNameStyle
                );

                Combatant combatant = character.GetComponent<Combatant>();
                float health = combatant != null
                    ? Mathf.Clamp01(combatant.CurrentHealth / combatant.MaxHealth)
                    : 1f;
                Rect healthBack = new(
                    card.x + 35f * scale,
                    card.yMax - 17f * scale,
                    card.width - 45f * scale,
                    5f * scale
                );
                UiTheme.DrawRect(healthBack, new Color(0f, 0f, 0f, 0.45f));
                UiTheme.DrawRect(
                    new Rect(
                        healthBack.x,
                        healthBack.y,
                        healthBack.width * health,
                        healthBack.height
                    ),
                    new Color(0.38f, 0.72f, 0.42f)
                );

                if (runtime != null)
                {
                    UiTheme.DrawRect(
                        new Rect(
                            healthBack.x,
                            healthBack.yMax + 2f * scale,
                            healthBack.width * runtime.EnergyRatio,
                            2f * scale
                        ),
                        GetAccent(definition)
                    );
                }
            }
        }

        private void DrawCompanionCard()
        {
            if (companion == null)
            {
                return;
            }

            float scale = UiTheme.Scale;
            float width = 172f * scale;
            float height = 42f * scale;
            float gap = 6f * scale;
            float right = Screen.width - 18f * scale;
            float top = 88f * scale
                + 4f * (46f * scale + gap)
                + 6f * scale;
            Rect card = new(right - width, top, width, height);
            UiTheme.DrawRect(
                card,
                new Color(0.025f, 0.055f, 0.075f, 0.92f)
            );
            UiTheme.DrawBorder(
                card,
                new Color(0.32f, 0.74f, 0.78f, 0.72f)
            );
            UiTheme.DrawRect(
                new Rect(card.x, card.y, 3f * scale, card.height),
                new Color(0.32f, 0.74f, 0.78f)
            );
            GUI.Label(
                new Rect(
                    card.x + 12f * scale,
                    card.y + 4f * scale,
                    card.width - 20f * scale,
                    20f * scale
                ),
                "随行 · 阿鱼",
                companionNameStyle
            );
            GUI.Label(
                new Rect(
                    card.x + 12f * scale,
                    card.y + 22f * scale,
                    card.width - 20f * scale,
                    16f * scale
                ),
                "非战斗伙伴 · 靠近按 E 交谈",
                companionStateStyle
            );
        }

        private void DrawStamina()
        {
            Transform character = party.ActiveCharacter;
            CharacterRuntime runtime = character != null
                ? character.GetComponent<CharacterRuntime>()
                : null;
            ThirdPersonController controller = character != null
                ? character.GetComponent<ThirdPersonController>()
                : null;
            if (runtime == null)
            {
                return;
            }

            bool moving = controller != null
                && (controller.IsSprinting || controller.IsGliding);
            if (!moving && runtime.StaminaRatio >= 0.999f)
            {
                return;
            }

            float scale = UiTheme.Scale;
            float width = 310f * scale;
            Rect panel = new(
                Screen.width * 0.5f - width * 0.5f,
                Screen.height - 166f * scale,
                width,
                24f * scale
            );
            UiTheme.DrawRect(panel, new Color(0.02f, 0.027f, 0.038f, 0.88f));
            Rect bar = new(
                panel.x + 10f * scale,
                panel.y + 13f * scale,
                panel.width - 20f * scale,
                5f * scale
            );
            UiTheme.DrawRect(bar, new Color(0f, 0f, 0f, 0.5f));
            Color staminaColor = runtime.StaminaRatio < 0.22f
                ? new Color(0.94f, 0.42f, 0.22f)
                : new Color(0.88f, 0.75f, 0.38f);
            UiTheme.DrawRect(
                new Rect(
                    bar.x,
                    bar.y,
                    bar.width * runtime.StaminaRatio,
                    bar.height
                ),
                staminaColor
            );

            GUIStyle style = runtime.StaminaRatio < 0.22f
                ? lowStaminaStyle
                : staminaStyle;
            string state = controller != null && controller.IsGliding
                ? "滑翔"
                : controller != null && controller.IsSprinting
                    ? "冲刺"
                    : "体力";
            GUI.Label(
                new Rect(
                    panel.x + 10f * scale,
                    panel.y - 1f * scale,
                    140f * scale,
                    14f * scale
                ),
                $"{state}  {Mathf.CeilToInt(runtime.CurrentStamina)}",
                style
            );
        }

        private void EnsureStyles()
        {
            float scale = UiTheme.Scale;
            if (Mathf.Approximately(cachedScale, scale)
                && activeSlotStyle != null)
            {
                return;
            }

            cachedScale = scale;
            activeSlotStyle = UiTheme.Label(
                10,
                TextAnchor.MiddleCenter,
                bold: true
            );
            activeSlotStyle.normal.textColor = UiTheme.Accent;
            mutedSlotStyle = UiTheme.Label(
                10,
                TextAnchor.MiddleCenter,
                bold: true
            );
            mutedSlotStyle.normal.textColor = UiTheme.Muted;
            lockedSlotStyle = UiTheme.Label(11);
            memberNameStyle = UiTheme.Label(
                12,
                TextAnchor.MiddleLeft,
                bold: true
            );
            staminaStyle = UiTheme.Label(
                9,
                TextAnchor.MiddleLeft,
                bold: true
            );
            staminaStyle.normal.textColor = new Color(0.88f, 0.75f, 0.38f);
            lowStaminaStyle = UiTheme.Label(
                9,
                TextAnchor.MiddleLeft,
                bold: true
            );
            lowStaminaStyle.normal.textColor =
                new Color(0.94f, 0.42f, 0.22f);
            companionNameStyle = UiTheme.Label(
                12,
                TextAnchor.MiddleLeft,
                bold: true
            );
            companionNameStyle.normal.textColor =
                new Color(0.62f, 0.9f, 0.9f);
            companionStateStyle = UiTheme.Label(
                9,
                TextAnchor.MiddleLeft
            );
            companionStateStyle.normal.textColor =
                new Color(0.62f, 0.7f, 0.74f);
        }

        private static bool IsBlockedByOtherUi()
        {
            return GameplayUiState.IsGameplayBlocked;
        }

        private static Color GetAccent(CharacterDefinition definition)
        {
            return definition != null && definition.AccentColor.a > 0f
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

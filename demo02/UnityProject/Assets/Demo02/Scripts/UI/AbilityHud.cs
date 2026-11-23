using Demo02.Combat;
using Demo02.Core;
using Demo02.Narrative;
using Demo02.Player;
using UnityEngine;

namespace Demo02.UI
{
    public sealed class AbilityHud : MonoBehaviour
    {
        private PlayerPartyController party;
        private float cachedScale = -1f;
        private GUIStyle keyStyle;
        private GUIStyle nameStyle;
        private GUIStyle statusStyle;
        private GUIStyle reactionStyle;

        private void Awake()
        {
            party = FindAnyObjectByType<PlayerPartyController>();
        }

        private void OnGUI()
        {
            if (party == null || IsBlockedByOtherUi())
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
            if (runtime?.Definition == null || abilities == null)
            {
                return;
            }

            EnsureStyles();
            float scale = UiTheme.Scale;
            float width = 126f * scale;
            float height = 50f * scale;
            float gap = 10f * scale;
            float right = Screen.width - 18f * scale;
            float y = Screen.height - 148f * scale;
            Rect skillCard = new(
                right - width * 2f - gap,
                y,
                width,
                height
            );
            Rect burstCard = new(right - width, y, width, height);
            DrawSkillCard(skillCard, runtime.Definition, abilities);
            DrawBurstCard(
                burstCard,
                runtime.Definition,
                runtime,
                abilities
            );

            if (Time.unscaledTime < ResonanceStatus.LastReactionUntil)
            {
                Rect reaction = new(
                    Screen.width * 0.5f - 150f * scale,
                    Screen.height - 203f * scale,
                    300f * scale,
                    28f * scale
                );
                UiTheme.DrawRect(
                    reaction,
                    new Color(0.03f, 0.045f, 0.055f, 0.94f)
                );
                UiTheme.DrawBorder(
                    reaction,
                    runtime.Definition.AccentColor,
                    2f
                );
                GUI.Label(
                    reaction,
                    $"{GetMarkLabel(ResonanceStatus.LastPreviousMark)} × "
                    + $"{GetMarkLabel(ResonanceStatus.LastAppliedMark)}"
                    + " · 时势错位",
                    reactionStyle
                );
            }
        }

        private void DrawSkillCard(
            Rect card,
            CharacterDefinition definition,
            CharacterAbilityController abilities)
        {
            UiTheme.DrawRect(card, new Color(0.025f, 0.035f, 0.05f, 0.94f));
            UiTheme.DrawBorder(card, definition.AccentColor);
            GUI.Label(
                new Rect(
                    card.x + 8f * UiTheme.Scale,
                    card.y + 1f * UiTheme.Scale,
                    22f * UiTheme.Scale,
                    20f * UiTheme.Scale
                ),
                "F",
                keyStyle
            );
            GUI.Label(
                new Rect(
                    card.x + 30f * UiTheme.Scale,
                    card.y + 3f * UiTheme.Scale,
                    card.width - 36f * UiTheme.Scale,
                    21f * UiTheme.Scale
                ),
                definition.SkillName,
                nameStyle
            );

            if (abilities.SkillCooldownRemaining > 0f)
            {
                float overlayHeight =
                    card.height * abilities.SkillCooldownRatio;
                UiTheme.DrawRect(
                    new Rect(
                        card.x,
                        card.yMax - overlayHeight,
                        card.width,
                        overlayHeight
                    ),
                    new Color(0f, 0f, 0f, 0.68f)
                );
                GUI.Label(
                    card,
                    $"{abilities.SkillCooldownRemaining:0.0}s",
                    statusStyle
                );
            }
            else
            {
                GUI.Label(
                    new Rect(
                        card.x + 9f * UiTheme.Scale,
                        card.y + 25f * UiTheme.Scale,
                        card.width - 18f * UiTheme.Scale,
                        18f * UiTheme.Scale
                    ),
                    "核心续式 · 就绪",
                    statusStyle
                );
            }
        }

        private void DrawBurstCard(
            Rect card,
            CharacterDefinition definition,
            CharacterRuntime runtime,
            CharacterAbilityController abilities)
        {
            UiTheme.DrawRect(card, new Color(0.025f, 0.035f, 0.05f, 0.94f));
            UiTheme.DrawBorder(
                card,
                abilities.IsBurstReady
                    ? definition.AccentColor
                    : new Color(1f, 1f, 1f, 0.14f)
            );
            GUI.Label(
                new Rect(
                    card.x + 8f * UiTheme.Scale,
                    card.y + 1f * UiTheme.Scale,
                    22f * UiTheme.Scale,
                    20f * UiTheme.Scale
                ),
                "R",
                keyStyle
            );
            GUI.Label(
                new Rect(
                    card.x + 30f * UiTheme.Scale,
                    card.y + 3f * UiTheme.Scale,
                    card.width - 36f * UiTheme.Scale,
                    21f * UiTheme.Scale
                ),
                definition.BurstName,
                nameStyle
            );
            Rect energyBar = new(
                card.x + 9f * UiTheme.Scale,
                card.yMax - 9f * UiTheme.Scale,
                card.width - 18f * UiTheme.Scale,
                4f * UiTheme.Scale
            );
            UiTheme.DrawRect(energyBar, new Color(0f, 0f, 0f, 0.5f));
            UiTheme.DrawRect(
                new Rect(
                    energyBar.x,
                    energyBar.y,
                    energyBar.width * runtime.EnergyRatio,
                    energyBar.height
                ),
                definition.AccentColor
            );
            GUI.Label(
                new Rect(
                    card.x + 9f * UiTheme.Scale,
                    card.y + 25f * UiTheme.Scale,
                    card.width - 18f * UiTheme.Scale,
                    16f * UiTheme.Scale
                ),
                abilities.IsBurstReady
                    ? "终结技已充能"
                    : $"能量 {Mathf.FloorToInt(runtime.CurrentEnergy)} / "
                        + $"{Mathf.FloorToInt(runtime.MaxEnergy)}",
                statusStyle
            );
        }

        private void EnsureStyles()
        {
            float scale = UiTheme.Scale;
            if (Mathf.Approximately(cachedScale, scale) && keyStyle != null)
            {
                return;
            }

            cachedScale = scale;
            keyStyle = UiTheme.Label(12, TextAnchor.MiddleLeft, bold: true);
            keyStyle.normal.textColor = UiTheme.Accent;
            nameStyle = UiTheme.Label(
                11,
                TextAnchor.MiddleLeft,
                bold: true
            );
            statusStyle = UiTheme.Label(9, TextAnchor.MiddleLeft);
            reactionStyle = UiTheme.Label(
                12,
                TextAnchor.MiddleCenter,
                bold: true
            );
            reactionStyle.normal.textColor = UiTheme.Accent;
        }

        private static bool IsBlockedByOtherUi()
        {
            return GameplayUiState.IsGameplayBlocked;
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

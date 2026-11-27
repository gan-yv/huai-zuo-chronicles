using Demo02.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace Demo02.UI
{
    public sealed class EliteEnemyHud : MonoBehaviour
    {
        private GameObject canvasObject;
        private Canvas canvas;
        private CanvasGroup group;
        private Outline border;
        private Image healthFill;
        private RectTransform healthFillRect;
        private Text title;
        private Text phase;
        private float nextRefresh;
        private string cachedPhase = string.Empty;

        private void Awake()
        {
            EnsureUi();
        }

        private void Update()
        {
            EnsureUi();
            if (GameplayUiState.IsGameplayBlocked
                || !TryFindElite(
                    out EliteEnemyController elite,
                    out Combatant health
                ))
            {
                group.alpha = 0f;
                return;
            }

            group.alpha = 1f;
            EnemyBreakStatus breakStatus =
                elite.GetComponent<EnemyBreakStatus>();
            EliteChargeAttack chargeAttack =
                elite.GetComponent<EliteChargeAttack>();
            float ratio = Mathf.Clamp01(
                health != null
                    ? health.CurrentHealth / health.MaxHealth
                    : 0f
            );
            healthFillRect.sizeDelta = new Vector2(
                494f * ratio,
                8f
            );
            border.effectColor = ratio <= 0.3f
                ? new Color(0.96f, 0.3f, 0.2f, 0.85f)
                : new Color(0.94f, 0.68f, 0.3f, 0.7f);

            if (Time.unscaledTime < nextRefresh)
            {
                return;
            }
            nextRefresh = Time.unscaledTime + 0.1f;
            HudUiFactory.SetText(title, elite.DisplayName);
            string phaseText = chargeAttack != null
                && chargeAttack.IsCharging
                    ? "蓄势冲锋 · 准备闪避"
                    : elite.PhaseLabel
                        + (breakStatus != null
                            && breakStatus.IsBroken
                                ? " · 破防"
                                : string.Empty);
            if (phaseText != cachedPhase)
            {
                cachedPhase = phaseText;
                phase.text = phaseText;
            }
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
            DestroyCanvas();
        }

        private void EnsureUi()
        {
            if (canvasObject != null)
            {
                return;
            }

            RectTransform root = HudUiFactory.CreateCanvas(
                transform,
                "EliteEnemyHudCanvas",
                21
            );
            canvasObject = root.gameObject;
            canvas = root.GetComponent<Canvas>();
            group = canvasObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            group.alpha = 0f;

            RectTransform panel = HudUiFactory.CreatePanel(
                root,
                "ElitePanel",
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -146f),
                new Vector2(550f, 62f),
                new Color(0.025f, 0.03f, 0.04f, 0.95f)
            );
            border = panel.gameObject.AddComponent<Outline>();
            border.effectDistance = new Vector2(1f, -1f);
            border.useGraphicAlpha = true;
            border.effectColor =
                new Color(0.94f, 0.68f, 0.3f, 0.7f);

            title = HudUiFactory.CreateText(
                panel,
                "Title",
                string.Empty,
                15,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -3f),
                new Vector2(534f, 24f),
                TextAnchor.MiddleCenter,
                bold: true
            );
            title.color = new Color(0.95f, 0.82f, 0.58f);

            RectTransform healthBackground =
                HudUiFactory.CreatePanel(
                    panel,
                    "HealthBackground",
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(28f, -29f),
                    new Vector2(494f, 8f),
                    new Color(0f, 0f, 0f, 0.55f)
                );
            healthFill = HudUiFactory.CreateImage(
                healthBackground,
                "HealthFill",
                new Color(0.82f, 0.2f, 0.14f)
            );
            healthFillRect = healthFill.rectTransform;
            healthFillRect.anchorMin = new Vector2(0f, 0f);
            healthFillRect.anchorMax = new Vector2(0f, 1f);
            healthFillRect.pivot = new Vector2(0f, 0.5f);
            healthFillRect.anchoredPosition = Vector2.zero;
            healthFillRect.sizeDelta = new Vector2(494f, 0f);

            phase = HudUiFactory.CreateText(
                panel,
                "Phase",
                string.Empty,
                10,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -41f),
                new Vector2(494f, 18f),
                TextAnchor.MiddleCenter,
                bold: true
            );
            phase.color = UiTheme.Accent;
        }

        private static bool TryFindElite(
            out EliteEnemyController elite,
            out Combatant health)
        {
            foreach (Combatant combatant in CombatRegistry.Enemies)
            {
                EliteEnemyController candidate = combatant != null
                    ? combatant.GetComponent<EliteEnemyController>()
                    : null;
                if (candidate != null && candidate.IsActive)
                {
                    elite = candidate;
                    health = candidate.GetComponent<Combatant>();
                    return true;
                }
            }
            elite = null;
            health = null;
            return false;
        }

        private void DestroyCanvas()
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
    }
}

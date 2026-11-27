using System.Collections.Generic;
using Demo02.Combat;
using Demo02.Core;
using Demo02.Narrative;
using Demo02.Player;
using Demo02.WorldState;
using UnityEngine;
using UnityEngine.UI;

namespace Demo02.UI
{
    public sealed class CombatHud : MonoBehaviour
    {
        private sealed class EnemyBarView
        {
            public RectTransform Root;
            public RectTransform HealthFill;
            public GameObject PoiseRoot;
            public RectTransform PoiseFill;
            public Text Broken;
            public GameObject WarningRoot;
            public RectTransform WarningFill;
            public Text WarningText;
        }

        private static readonly Color HealthLow =
            new(0.78f, 0.18f, 0.14f);
        private static readonly Color HealthHigh =
            new(0.38f, 0.64f, 0.3f);

        private PlayerPartyController party;
        private EncounterState encounterState;
        private TimeLayerController timeLayerController;
        private WorldRuntime worldRuntime;
        private GameObject canvasObject;
        private Canvas canvas;
        private GameObject healthPanel;
        private RectTransform healthFill;
        private Text healthText;
        private GameObject threatPanel;
        private Text threatText;
        private Button timeLayerButton;
        private Text timeLayerButtonText;
        private GameObject areaPanel;
        private Text areaText;
        private GameObject helpPanel;
        private Text helpBody;
        private Image transitionOverlay;
        private Text transitionText;
        private GameObject clearPanel;
        private CanvasGroup clearGroup;
        private RectTransform targetMarker;
        private Text targetLabel;
        private readonly List<EnemyBarView> enemyBars = new();
        private bool showHelp;
        private int previousAliveEnemies;
        private float clearNoticeUntil;
        private float nextTextRefresh;
        private Combatant cachedHealth;
        private Combatant cachedTarget;
        private string cachedHealthText = string.Empty;
        private string cachedThreatText = string.Empty;
        private string cachedAreaText = string.Empty;
        private string cachedTargetText = string.Empty;

        private void Awake()
        {
            party = Demo02.Core.GameServices.Resolve<PlayerPartyController>();
            encounterState = Demo02.Core.GameServices.Resolve<EncounterState>();
            timeLayerController =
                Demo02.Core.GameServices.Resolve<TimeLayerController>();
            worldRuntime = Demo02.Core.GameServices.Resolve<WorldRuntime>() != null
                ? Demo02.Core.GameServices.Resolve<WorldRuntime>()
                : Demo02.Core.GameServices.Resolve<WorldRuntime>();
            previousAliveEnemies = encounterState != null
                ? encounterState.AliveEnemyCount
                : 0;
            EnsureUi();
        }

        private void Update()
        {
            EnsureUi();
            if (Demo02Input.HelpPressed)
            {
                showHelp = !showHelp;
                helpPanel.SetActive(showHelp);
            }
            if (Demo02Input.MutePressed)
            {
                GameAudio.ToggleMuted();
            }

            bool blocked = GameplayUiState.IsGameplayBlocked;
            canvas.enabled = !blocked;
            if (blocked)
            {
                return;
            }

            int aliveEnemies = encounterState != null
                ? encounterState.AliveEnemyCount
                : 0;
            if (previousAliveEnemies > 0 && aliveEnemies == 0)
            {
                clearNoticeUntil = Time.unscaledTime + 2.6f;
            }
            previousAliveEnemies = aliveEnemies;

            if (Time.unscaledTime >= nextTextRefresh)
            {
                nextTextRefresh = Time.unscaledTime + 0.1f;
                RefreshDynamicText();
                RefreshUtilityLabels();
            }

            RefreshHealth();
            RefreshTimeTransition();
            RefreshEnemyBars();
            RefreshTargetMarker();
            RefreshClearNotice();
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
                "CombatHudCanvas",
                16
            );
            canvasObject = root.gameObject;
            canvas = root.GetComponent<Canvas>();
            CreateTransitionOverlay(root);
            CreateHealthPanel(root);
            CreateThreatPanel(root);
            CreateTimeLayerButton(root);
            CreateAreaPanel(root);
            CreateUtilityButtons(root);
            CreateHelpPanel(root);
            CreateClearNotice(root);
            CreateTargetMarker(root);
        }

        private void CreateTransitionOverlay(Transform root)
        {
            transitionOverlay = HudUiFactory.CreateImage(
                root,
                "TimeLayerTransition",
                Color.clear
            );
            HudUiFactory.Stretch(transitionOverlay.rectTransform);
            transitionOverlay.gameObject.SetActive(false);

            transitionText = HudUiFactory.CreateText(
                root,
                "TimeLayerTransitionText",
                string.Empty,
                24,
                new Vector2(0f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 120f),
                new Vector2(0f, 46f),
                TextAnchor.MiddleCenter,
                bold: true
            );
            transitionText.gameObject.SetActive(false);
        }

        private void CreateHealthPanel(Transform root)
        {
            RectTransform panel = HudUiFactory.CreatePanel(
                root,
                "HealthPanel",
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-18f, -20f),
                new Vector2(236f, 26f),
                UiTheme.Ink
            );
            healthPanel = panel.gameObject;
            Outline outline = panel.gameObject.AddComponent<Outline>();
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;
            outline.effectColor = new Color(1f, 1f, 1f, 0.18f);

            Image fill = HudUiFactory.CreateImage(
                panel,
                "HealthFill",
                HealthHigh
            );
            healthFill = fill.rectTransform;
            HudUiFactory.Stretch(healthFill);
            healthFill.pivot = new Vector2(0f, 0.5f);
            healthFill.offsetMin = new Vector2(2f, 2f);
            healthFill.offsetMax = new Vector2(-2f, -2f);

            healthText = HudUiFactory.CreateText(
                panel,
                "HealthText",
                string.Empty,
                13,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                TextAnchor.MiddleCenter,
                bold: true
            );
            healthText.color = Color.white;
        }

        private void CreateThreatPanel(Transform root)
        {
            RectTransform panel = HudUiFactory.CreatePanel(
                root,
                "ThreatPanel",
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-18f, -52f),
                new Vector2(236f, 26f),
                UiTheme.Ink
            );
            threatPanel = panel.gameObject;
            threatText = HudUiFactory.CreateText(
                panel,
                "ThreatText",
                string.Empty,
                13,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                TextAnchor.MiddleCenter,
                bold: true
            );
            threatText.color =
                new Color(0.94f, 0.48f, 0.3f);
        }

        private void CreateTimeLayerButton(Transform root)
        {
            RectTransform panel = HudUiFactory.CreatePanel(
                root,
                "TimeLayerButton",
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -20f),
                new Vector2(264f, 44f),
                UiTheme.Panel,
                raycastTarget: true
            );
            timeLayerButton = panel.gameObject.AddComponent<Button>();
            timeLayerButton.targetGraphic =
                panel.GetComponent<Image>();
            timeLayerButton.transition =
                Selectable.Transition.ColorTint;
            timeLayerButton.onClick.AddListener(
                () => timeLayerController?.ToggleLayer()
            );
            timeLayerButtonText = HudUiFactory.CreateText(
                panel,
                "Label",
                string.Empty,
                14,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                TextAnchor.MiddleCenter,
                bold: true
            );
        }

        private void CreateAreaPanel(Transform root)
        {
            RectTransform panel = HudUiFactory.CreatePanel(
                root,
                "AreaPanel",
                Vector2.zero,
                Vector2.zero,
                Vector2.zero,
                new Vector2(18f, 20f),
                new Vector2(240f, 36f),
                UiTheme.Ink
            );
            areaPanel = panel.gameObject;
            Image accent = HudUiFactory.CreateImage(
                panel,
                "Accent",
                UiTheme.Accent
            );
            RectTransform accentRect = accent.rectTransform;
            accentRect.anchorMin = new Vector2(0f, 0f);
            accentRect.anchorMax = new Vector2(0f, 1f);
            accentRect.pivot = new Vector2(0f, 0.5f);
            accentRect.anchoredPosition = Vector2.zero;
            accentRect.sizeDelta = new Vector2(3f, 0f);

            areaText = HudUiFactory.CreateText(
                panel,
                "AreaText",
                string.Empty,
                14,
                new Vector2(0f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 0.5f),
                new Vector2(14f, 0f),
                new Vector2(-20f, 0f),
                TextAnchor.MiddleLeft
            );
        }

        private void CreateUtilityButtons(Transform root)
        {
            CreateUtilityButton(
                root,
                0,
                "HelpButton",
                () =>
                {
                    showHelp = !showHelp;
                    helpPanel.SetActive(showHelp);
                }
            );
            CreateUtilityButton(
                root,
                1,
                "MapButton",
                () => Demo02.Core.GameServices.Resolve<WorldMapController>()?.ToggleMap()
            );
            CreateUtilityButton(
                root,
                2,
                "JournalButton",
                () => Demo02.Core.GameServices.Resolve<InvestigationJournalController>()
                    ?.ToggleJournal()
            );
            CreateUtilityButton(
                root,
                3,
                "InventoryButton",
                () => Demo02.Core.GameServices.Resolve<InventoryController>()
                    ?.ToggleInventory()
            );
        }

        private void CreateUtilityButton(
            Transform root,
            int index,
            string name,
            UnityEngine.Events.UnityAction action)
        {
            RectTransform panel = HudUiFactory.CreatePanel(
                root,
                name,
                Vector2.zero,
                Vector2.zero,
                Vector2.zero,
                new Vector2(-18f - index * 102f, 12f),
                new Vector2(92f, 30f),
                UiTheme.Panel,
                raycastTarget: true
            );
            panel.anchorMin = new Vector2(1f, 0f);
            panel.anchorMax = new Vector2(1f, 0f);
            panel.pivot = new Vector2(1f, 0f);
            Button button = panel.gameObject.AddComponent<Button>();
            button.targetGraphic = panel.GetComponent<Image>();
            button.transition = Selectable.Transition.ColorTint;
            button.onClick.AddListener(action);
            Text label = HudUiFactory.CreateText(
                panel,
                "Label",
                string.Empty,
                13,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                TextAnchor.MiddleCenter
            );
            label.name = "UtilityLabel" + index;
        }

        private void CreateHelpPanel(Transform root)
        {
            RectTransform panel = HudUiFactory.CreatePanel(
                root,
                "HelpPanel",
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -173f),
                new Vector2(720f, 394f),
                UiTheme.Ink,
                raycastTarget: true
            );
            helpPanel = panel.gameObject;
            Outline outline = panel.gameObject.AddComponent<Outline>();
            outline.effectDistance = new Vector2(2f, -2f);
            outline.effectColor = UiTheme.Accent;

            Image line = HudUiFactory.CreateImage(
                panel,
                "AccentLine",
                UiTheme.Accent
            );
            RectTransform lineRect = line.rectTransform;
            lineRect.anchorMin = new Vector2(0f, 1f);
            lineRect.anchorMax = new Vector2(0f, 1f);
            lineRect.pivot = new Vector2(0f, 1f);
            lineRect.anchoredPosition = new Vector2(24f, -24f);
            lineRect.sizeDelta = new Vector2(72f, 3f);

            Text title = HudUiFactory.CreateText(
                panel,
                "Title",
                "操作说明",
                25,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f),
                new Vector2(24f, -38f),
                new Vector2(-48f, 42f),
                TextAnchor.MiddleLeft,
                bold: true
            );
            title.rectTransform.offsetMin = new Vector2(24f, -80f);
            title.rectTransform.offsetMax = new Vector2(-24f, -38f);

            helpBody = HudUiFactory.CreateText(
                panel,
                "Body",
                string.Empty,
                16,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f),
                new Vector2(28f, -92f),
                new Vector2(-56f, 236f),
                TextAnchor.UpperLeft
            );
            helpBody.rectTransform.offsetMin =
                new Vector2(28f, -328f);
            helpBody.rectTransform.offsetMax = new Vector2(-28f, -92f);
            helpBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            helpBody.verticalOverflow = VerticalWrapMode.Truncate;

            Text footer = HudUiFactory.CreateText(
                panel,
                "Footer",
                string.Empty,
                13,
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 0f),
                new Vector2(24f, 12f),
                new Vector2(-48f, 24f),
                TextAnchor.MiddleRight
            );
            footer.name = "HelpFooter";
            helpPanel.SetActive(false);
        }

        private void CreateClearNotice(Transform root)
        {
            RectTransform panel = HudUiFactory.CreatePanel(
                root,
                "CombatClearNotice",
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -270f),
                new Vector2(340f, 42f),
                new Color(0.06f, 0.08f, 0.065f, 0.92f)
            );
            clearPanel = panel.gameObject;
            clearGroup = clearPanel.AddComponent<CanvasGroup>();
            clearGroup.blocksRaycasts = false;
            clearGroup.interactable = false;
            clearGroup.alpha = 0f;
            Outline outline = clearPanel.AddComponent<Outline>();
            outline.effectDistance = new Vector2(2f, -2f);
            outline.effectColor =
                new Color(0.86f, 0.7f, 0.34f);
            Text text = HudUiFactory.CreateText(
                panel,
                "Text",
                "街口封锁已解除",
                16,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                TextAnchor.MiddleCenter,
                bold: true
            );
            text.color = new Color(1f, 0.86f, 0.52f);
        }

        private void CreateTargetMarker(Transform root)
        {
            RectTransform marker = HudUiFactory.CreatePanel(
                root,
                "TargetMarker",
                Vector2.zero,
                Vector2.zero,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(42f, 42f),
                Color.clear
            );
            targetMarker = marker;
            Color color = new Color(1f, 0.69f, 0.3f, 0.96f);
            CreateMarkerBar(marker, "TopLeftH", -11f, 20f, 10f, 3f, color);
            CreateMarkerBar(marker, "TopLeftV", -20f, 11f, 3f, 10f, color);
            CreateMarkerBar(marker, "TopRightH", 11f, 20f, 10f, 3f, color);
            CreateMarkerBar(marker, "TopRightV", 20f, 11f, 3f, 10f, color);
            CreateMarkerBar(marker, "BottomLeftH", -11f, -20f, 10f, 3f, color);
            CreateMarkerBar(marker, "BottomLeftV", -20f, -11f, 3f, 10f, color);
            CreateMarkerBar(marker, "BottomRightH", 11f, -20f, 10f, 3f, color);
            CreateMarkerBar(marker, "BottomRightV", 20f, -11f, 3f, 10f, color);

            RectTransform labelPanel = HudUiFactory.CreatePanel(
                marker,
                "LabelPanel",
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 30f),
                new Vector2(158f, 21f),
                new Color(0.025f, 0.035f, 0.05f, 0.88f)
            );
            targetLabel = HudUiFactory.CreateText(
                labelPanel,
                "TargetLabel",
                string.Empty,
                11,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                TextAnchor.MiddleCenter,
                bold: true
            );
            targetLabel.color = color;
            marker.gameObject.SetActive(false);
        }

        private static void CreateMarkerBar(
            Transform parent,
            string name,
            float x,
            float y,
            float width,
            float height,
            Color color)
        {
            RectTransform rect = HudUiFactory.CreatePanel(
                parent,
                name,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(x, y),
                new Vector2(width, height),
                color
            );
            rect.gameObject.name = name;
        }

        private EnemyBarView CreateEnemyBar()
        {
            RectTransform root = HudUiFactory.CreatePanel(
                canvasObject.transform,
                "EnemyBar" + enemyBars.Count,
                Vector2.zero,
                Vector2.zero,
                new Vector2(0.5f, 0f),
                Vector2.zero,
                new Vector2(76f, 9f),
                UiTheme.Ink
            );
            Outline outline = root.gameObject.AddComponent<Outline>();
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;
            outline.effectColor = new Color(1f, 1f, 1f, 0.16f);

            Image fill = HudUiFactory.CreateImage(
                root,
                "HealthFill",
                new Color(0.86f, 0.24f, 0.16f)
            );
            HudUiFactory.Stretch(fill.rectTransform);
            fill.rectTransform.pivot =
                new Vector2(0f, 0.5f);
            fill.rectTransform.offsetMin = new Vector2(2f, 2f);
            fill.rectTransform.offsetMax = new Vector2(-2f, -2f);

            RectTransform poise = HudUiFactory.CreatePanel(
                root,
                "Poise",
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 11f),
                new Vector2(76f, 4f),
                new Color(0f, 0f, 0f, 0.45f)
            );
            Image poiseFill = HudUiFactory.CreateImage(
                poise,
                "Fill",
                new Color(0.52f, 0.7f, 0.82f)
            );
            HudUiFactory.Stretch(poiseFill.rectTransform);
            poiseFill.rectTransform.pivot =
                new Vector2(0f, 0.5f);

            Text broken = HudUiFactory.CreateText(
                root,
                "Broken",
                "破防 · 伤害提升",
                9,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 17f),
                new Vector2(112f, 18f),
                TextAnchor.MiddleCenter,
                bold: true
            );
            broken.color = new Color(1f, 0.78f, 0.35f);

            RectTransform warning = HudUiFactory.CreatePanel(
                root,
                "Warning",
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -5f),
                new Vector2(124f, 20f),
                new Color(0.14f, 0.045f, 0.02f, 0.94f)
            );
            Outline warningOutline =
                warning.gameObject.AddComponent<Outline>();
            warningOutline.effectDistance = new Vector2(1f, -1f);
            warningOutline.useGraphicAlpha = true;
            Image warningFill = HudUiFactory.CreateImage(
                warning,
                "Progress",
                new Color(1f, 0.58f, 0.16f)
            );
            warningFill.rectTransform.anchorMin =
                new Vector2(0f, 0f);
            warningFill.rectTransform.anchorMax =
                new Vector2(0f, 0f);
            warningFill.rectTransform.pivot =
                new Vector2(0f, 0f);
            warningFill.rectTransform.anchoredPosition = Vector2.zero;
            warningFill.rectTransform.sizeDelta =
                new Vector2(122f, 2f);
            Text warningText = HudUiFactory.CreateText(
                warning,
                "Text",
                string.Empty,
                10,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                TextAnchor.MiddleCenter,
                bold: true
            );
            warningText.color = new Color(1f, 0.78f, 0.46f);

            EnemyBarView view = new()
            {
                Root = root,
                HealthFill = fill.rectTransform,
                PoiseRoot = poise.gameObject,
                PoiseFill = poiseFill.rectTransform,
                Broken = broken,
                WarningRoot = warning.gameObject,
                WarningFill = warningFill.rectTransform,
                WarningText = warningText
            };
            enemyBars.Add(view);
            return view;
        }

        private void RefreshDynamicText()
        {
            Transform active = party != null
                ? party.ActiveCharacter
                : null;
            cachedHealth = active != null
                ? active.GetComponent<Combatant>()
                : null;
            healthPanel.SetActive(cachedHealth != null);
            if (cachedHealth != null)
            {
                string value =
                    $"生命  {Mathf.CeilToInt(cachedHealth.CurrentHealth)} / "
                    + $"{Mathf.CeilToInt(cachedHealth.MaxHealth)}";
                if (cachedHealthText != value)
                {
                    cachedHealthText = value;
                    healthText.text = value;
                }
            }

            int nearby = active != null
                ? CombatRegistry.CountNearbyEnemies(
                    active.position,
                    16f
                )
                : 0;
            threatPanel.SetActive(nearby > 0);
            if (nearby > 0)
            {
                string value = $"街口封锁 · {nearby} 名守卫";
                if (cachedThreatText != value)
                {
                    cachedThreatText = value;
                    threatText.text = value;
                }
            }

            string area = worldRuntime != null
                && !string.IsNullOrWhiteSpace(
                    worldRuntime.CurrentAreaName
                )
                    ? $"区域  {worldRuntime.CurrentAreaName}"
                    : string.Empty;
            areaPanel.SetActive(!string.IsNullOrWhiteSpace(area));
            if (!string.IsNullOrWhiteSpace(area)
                && cachedAreaText != area)
            {
                cachedAreaText = area;
                areaText.text = area;
            }

            float distance = 0f;
            cachedTarget = active != null
                ? CombatTargeting.FindNearestEnemy(
                    active,
                    18f,
                    out distance
                )
                : null;
            targetMarker.gameObject.SetActive(cachedTarget != null);
            if (cachedTarget != null)
            {
                string value =
                    $"锁定 · {GetTargetLabel(cachedTarget)} · {distance:0.0}m";
                if (cachedTargetText != value)
                {
                    cachedTargetText = value;
                    targetLabel.text = value;
                }
            }
        }

        private void RefreshUtilityLabels()
        {
            Text help = FindLabel("UtilityLabel0");
            Text map = FindLabel("UtilityLabel1");
            Text journal = FindLabel("UtilityLabel2");
            Text inventory = FindLabel("UtilityLabel3");
            HudUiFactory.SetText(
                help,
                $"{Demo02Input.GetPreferredBindingDisplay("Help")} 操作"
            );
            HudUiFactory.SetText(
                map,
                $"{Demo02Input.GetPreferredBindingDisplay("Map")} 地图"
            );
            HudUiFactory.SetText(
                journal,
                $"{Demo02Input.GetPreferredBindingDisplay("Journal")} 案卷"
            );
            HudUiFactory.SetText(
                inventory,
                $"{Demo02Input.GetPreferredBindingDisplay("Inventory")} 背包"
            );

            Text footer = FindLabel("HelpFooter");
            string switchKey =
                Demo02Input.GetPreferredBindingDisplay("ToggleTime");
            string helpKey =
                Demo02Input.GetPreferredBindingDisplay("Help");
            HudUiFactory.SetText(
                timeLayerButtonText,
                $"时间层  "
                    + (timeLayerController != null
                        && timeLayerController.CurrentLayer
                            == TimeLayer.Past
                                ? "旧时"
                                : "现世")
                    + $"  [{switchKey}]"
            );
            HudUiFactory.SetText(
                footer,
                $"{helpKey} 关闭"
            );
            HudUiFactory.SetText(
                helpBody,
                BuildHelpText()
            );
        }

        private Text FindLabel(string objectName)
        {
            foreach (Text text in
                     canvasObject.GetComponentsInChildren<Text>(true))
            {
                if (text.gameObject.name == objectName)
                {
                    return text;
                }
            }
            return null;
        }

        private static string BuildHelpText()
        {
            string move =
                Demo02Input.GetPreferredBindingDisplay("Move");
            string sprint =
                Demo02Input.GetPreferredBindingDisplay("Sprint");
            string dodge =
                Demo02Input.GetPreferredBindingDisplay("Dodge");
            string jump =
                Demo02Input.GetPreferredBindingDisplay("Jump");
            string attack =
                Demo02Input.GetPreferredBindingDisplay("Attack");
            string skill =
                Demo02Input.GetPreferredBindingDisplay("Skill");
            string burst =
                Demo02Input.GetPreferredBindingDisplay("Burst");
            string interact =
                Demo02Input.GetPreferredBindingDisplay("Interact");
            string toggle =
                Demo02Input.GetPreferredBindingDisplay("ToggleTime");
            string journal =
                Demo02Input.GetPreferredBindingDisplay("Journal");
            string map =
                Demo02Input.GetPreferredBindingDisplay("Map");
            string cancel =
                Demo02Input.GetPreferredBindingDisplay("Cancel");
            string inventory =
                Demo02Input.GetPreferredBindingDisplay("Inventory");
            string testimony =
                Demo02Input.GetPreferredBindingDisplay("Testimony");
            string vision =
                Demo02Input.GetPreferredBindingDisplay("Vision");
            string settings =
                Demo02Input.GetPreferredBindingDisplay("Settings");
            string save =
                Demo02Input.GetPreferredBindingDisplay("QuickSave");
            string load =
                Demo02Input.GetPreferredBindingDisplay("QuickLoad");
            return $"{move}  移动      {sprint}  冲刺（耗体力）      "
                + $"{dodge}  闪避\n"
                + $"{jump}  跳跃 / 空中再按展开滑翔      "
                + "1/2/Tab  切换角色\n"
                + $"{attack}  攻击      {skill}  核心技能      "
                + $"{burst}  终结技（满能量）\n"
                + "按住右键  旋转镜头\n"
                + $"{interact}  调查 / 对话 / 修复      "
                + $"{toggle}  切换时间层\n"
                + $"{journal}  调查案卷      {map}  世界地图      "
                + $"{cancel}  暂停\n"
                + $"{inventory}  背包      {testimony}  证词对照      "
                + $"{vision}  修痕视界      {settings}  设置\n"
                + $"{save}/{load}  保存/读取";
        }

        private void RefreshHealth()
        {
            if (cachedHealth == null)
            {
                return;
            }
            float ratio = Mathf.Clamp01(
                cachedHealth.CurrentHealth / cachedHealth.MaxHealth
            );
            healthFill.localScale = new Vector3(ratio, 1f, 1f);
            healthFill.GetComponent<Image>().color =
                Color.Lerp(HealthLow, HealthHigh, ratio);
        }

        private void RefreshTimeTransition()
        {
            if (timeLayerController == null)
            {
                transitionOverlay.gameObject.SetActive(false);
                transitionText.gameObject.SetActive(false);
                return;
            }
            float age =
                Time.unscaledTime - timeLayerController.LastChangedAt;
            bool visible = age >= 0f && age < 0.55f;
            transitionOverlay.gameObject.SetActive(visible);
            transitionText.gameObject.SetActive(visible);
            if (!visible)
            {
                return;
            }
            float fade = 1f - age / 0.55f;
            Color tint =
                timeLayerController.CurrentLayer == TimeLayer.Past
                    ? new Color(0.18f, 0.38f, 0.48f, 0.12f * fade)
                    : new Color(0.86f, 0.68f, 0.36f, 0.1f * fade);
            transitionOverlay.color = tint;
            transitionText.color =
                new Color(1f, 0.92f, 0.72f, fade);
            HudUiFactory.SetText(
                transitionText,
                timeLayerController.CurrentLayer == TimeLayer.Past
                    ? "旧 时"
                    : "现 世"
            );
        }

        private void RefreshEnemyBars()
        {
            Camera sceneCamera = Camera.main;
            IReadOnlyList<Combatant> enemies =
                CombatRegistry.Enemies;
            if (sceneCamera == null)
            {
                foreach (EnemyBarView view in enemyBars)
                {
                    view.Root.gameObject.SetActive(false);
                }
                return;
            }

            int activeIndex = 0;
            for (int index = 0; index < enemies.Count; index++)
            {
                Combatant enemy = enemies[index];
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }
                Vector3 screenPoint = sceneCamera.WorldToScreenPoint(
                    enemy.transform.position + Vector3.up * 1.4f
                );
                if (screenPoint.z <= 0f)
                {
                    continue;
                }
                EnemyBarView view = activeIndex < enemyBars.Count
                    ? enemyBars[activeIndex]
                    : CreateEnemyBar();
                activeIndex++;
                view.Root.gameObject.SetActive(true);
                view.Root.position = new Vector3(
                    screenPoint.x,
                    Screen.height - screenPoint.y,
                    0f
                );

                float health = Mathf.Clamp01(
                    enemy.CurrentHealth / enemy.MaxHealth
                );
                view.HealthFill.localScale =
                    new Vector3(health, 1f, 1f);

                EnemyBreakStatus breakStatus =
                    enemy.GetComponent<EnemyBreakStatus>();
                view.PoiseRoot.SetActive(breakStatus != null);
                if (breakStatus != null)
                {
                    view.PoiseFill.localScale = new Vector3(
                        breakStatus.IsBroken
                            ? 1f
                            : breakStatus.PoiseRatio,
                        1f,
                        1f
                    );
                    view.PoiseFill.GetComponent<Image>().color =
                        breakStatus.IsBroken
                            ? new Color(1f, 0.72f, 0.24f)
                            : new Color(0.52f, 0.7f, 0.82f);
                    view.Broken.gameObject.SetActive(
                        breakStatus.IsBroken
                    );
                }
                else
                {
                    view.Broken.gameObject.SetActive(false);
                }

                EnemyAI enemyAi = enemy.GetComponent<EnemyAI>();
                bool winding = enemyAi != null
                    && enemyAi.IsWindingUp;
                view.WarningRoot.SetActive(winding);
                if (winding)
                {
                    view.WarningFill.localScale = new Vector3(
                        enemyAi.AttackWindupNormalized,
                        1f,
                        1f
                    );
                    HudUiFactory.SetText(
                        view.WarningText,
                        enemyAi.IsHeavyAttack
                            ? "重击蓄力 · 闪避反击"
                            : "攻击蓄力"
                    );
                    Outline warningOutline =
                        view.WarningRoot.GetComponent<Outline>();
                    if (warningOutline != null)
                    {
                        warningOutline.effectColor =
                            enemyAi.IsHeavyAttack
                                ? new Color(1f, 0.4f, 0.16f, 1f)
                                : new Color(1f, 0.6f, 0.2f, 0.9f);
                    }
                }
            }

            for (int index = activeIndex;
                 index < enemyBars.Count;
                 index++)
            {
                enemyBars[index].Root.gameObject.SetActive(false);
            }
        }

        private void RefreshTargetMarker()
        {
            Camera sceneCamera = Camera.main;
            if (cachedTarget == null || sceneCamera == null)
            {
                targetMarker.gameObject.SetActive(false);
                return;
            }
            Vector3 screenPoint = sceneCamera.WorldToScreenPoint(
                cachedTarget.transform.position + Vector3.up * 1.75f
            );
            bool visible = screenPoint.z > 0f;
            targetMarker.gameObject.SetActive(visible);
            if (visible)
            {
                targetMarker.position = new Vector3(
                    screenPoint.x,
                    Screen.height - screenPoint.y,
                    0f
                );
            }
        }

        private void RefreshClearNotice()
        {
            bool visible = Time.unscaledTime < clearNoticeUntil;
            clearGroup.alpha = visible
                ? Mathf.Clamp01(
                    (clearNoticeUntil - Time.unscaledTime) / 0.5f
                )
                : 0f;
        }

        private static string GetTargetLabel(Combatant target)
        {
            string objectName = target.gameObject.name;
            if (objectName.Contains("Dongguan"))
            {
                return "东关巡哨";
            }
            if (objectName.Contains("Caiyi"))
            {
                return "彩衣望风";
            }
            if (objectName.Contains("Support"))
            {
                return "盐仓增援";
            }
            string guardNumber = objectName
                .Replace("Enemy_Patrol_", string.Empty)
                .Trim();
            return string.IsNullOrWhiteSpace(guardNumber)
                ? "巡逻守卫"
                : $"守卫 {guardNumber}";
        }
    }
}

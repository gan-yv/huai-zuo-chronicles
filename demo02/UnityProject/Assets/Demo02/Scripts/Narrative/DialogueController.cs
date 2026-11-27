using System;
using Demo02.Core;
using Demo02.Player;
using Demo02.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Demo02.Narrative
{
    [DefaultExecutionOrder(1000)]
    public sealed class DialogueController : MonoBehaviour
    {
        private sealed class ChoiceView
        {
            public GameObject Root;
            public Outline Border;
            public Text Label;
            public Button Button;
        }

        private DialogueLine[] lines = new DialogueLine[0];
        private int currentLineIndex;
        private int ignoreInputUntilFrame;
        private float previousTimeScale = 1f;
        private Action onCompleted;
        private Action<int> onChoiceSelected;
        private DialogueChoice[] choices = new DialogueChoice[0];
        private bool isChoosing;
        private int selectedChoiceIndex;
        private Transform focusTarget;
        private ReadyMadeCharacterAnimator speakerAnimator;
        private CharacterExpressionAnimator speakerExpressions;
        private Camera dialogueCamera;
        private float originalFieldOfView = 60f;
        private bool hasCameraState;
        private GameObject canvasObject;
        private Canvas canvas;
        private RectTransform panel;
        private Text speaker;
        private Text body;
        private Text hint;
        private readonly ChoiceView[] choiceViews = new ChoiceView[4];

        public static DialogueController Instance { get; private set; }
        public bool IsOpen { get; private set; }
        public bool IsChoosing => isChoosing;
        public int LastClosedFrame { get; private set; } = -1;
        public DialogueLine CurrentLine =>
            currentLineIndex >= 0 && currentLineIndex < lines.Length
                ? lines[currentLineIndex]
                : null;

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

        private void OnDestroy()
        {
            GameplayUiState.UnregisterModal(this);
            if (Instance == this)
            {
                Demo02.Core.GameServices.Unregister(this);
                Instance = null;
            }
            Time.timeScale = 1f;
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
                canvasObject = null;
            }
        }

        public void Begin(
            DialogueLine[] dialogueLines,
            Action completed = null,
            Transform cameraFocus = null)
        {
            if (IsOpen
                || GameplayUiState.HasOpenModalExcept(this)
                || dialogueLines == null
                || dialogueLines.Length == 0)
            {
                return;
            }
            lines = dialogueLines;
            currentLineIndex = 0;
            onCompleted = completed;
            focusTarget = cameraFocus;
            speakerAnimator =
                cameraFocus?.GetComponentInChildren<
                    ReadyMadeCharacterAnimator
                >();
            speakerAnimator?.SetTalking(true);
            speakerExpressions =
                cameraFocus?.GetComponentInChildren<
                    CharacterExpressionAnimator
                >();
            speakerExpressions?.SetTalking(true);
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            IsOpen = true;
            canvas.enabled = true;
            GameplayUiState.RegisterModal(this);
            ignoreInputUntilFrame = Time.frameCount + 1;
            dialogueCamera = Camera.main;
            if (dialogueCamera != null)
            {
                originalFieldOfView = dialogueCamera.fieldOfView;
                hasCameraState = true;
            }
            RefreshUi();
        }

        public void BeginWithChoice(
            DialogueLine[] dialogueLines,
            DialogueChoice[] dialogueChoices,
            Action<int> choiceSelected,
            Action completed = null,
            Transform cameraFocus = null,
            bool showChoicesImmediately = false)
        {
            Begin(dialogueLines, completed, cameraFocus);
            if (!IsOpen)
            {
                return;
            }
            choices = dialogueChoices ?? new DialogueChoice[0];
            onChoiceSelected = choiceSelected;
            if (showChoicesImmediately && choices.Length > 0)
            {
                isChoosing = true;
                selectedChoiceIndex = 0;
                ignoreInputUntilFrame = Time.frameCount + 1;
            }
            RefreshUi();
        }

        private void Update()
        {
            if (!IsOpen || Time.frameCount <= ignoreInputUntilFrame)
            {
                return;
            }
            if (isChoosing)
            {
                if (Demo02Input.NavigateUpPressed)
                {
                    selectedChoiceIndex = choices.Length == 0
                        ? 0
                        : (selectedChoiceIndex - 1 + choices.Length)
                            % choices.Length;
                    RefreshUi();
                }
                else if (Demo02Input.NavigateDownPressed)
                {
                    selectedChoiceIndex = choices.Length == 0
                        ? 0
                        : (selectedChoiceIndex + 1) % choices.Length;
                    RefreshUi();
                }

                if (Demo02Input.ChoicePressed(0))
                {
                    SelectChoice(0);
                }
                else if (Demo02Input.ChoicePressed(1))
                {
                    SelectChoice(1);
                }
                else if (Demo02Input.SubmitPressed)
                {
                    SelectChoice(selectedChoiceIndex);
                }
                return;
            }

            if (Demo02Input.CancelPressed && choices.Length == 0)
            {
                Close();
                return;
            }
            if (Demo02Input.AdvancePressed)
            {
                Advance();
            }
        }

        private void Advance()
        {
            if (currentLineIndex + 1 < lines.Length)
            {
                currentLineIndex++;
                RefreshUi();
                return;
            }
            if (choices.Length > 0)
            {
                isChoosing = true;
                selectedChoiceIndex = 0;
                ignoreInputUntilFrame = Time.frameCount + 1;
                RefreshUi();
                return;
            }
            Close();
        }

        private void SelectChoice(int index)
        {
            if (!isChoosing || index < 0 || index >= choices.Length)
            {
                return;
            }
            Action<int> choiceCallback = onChoiceSelected;
            onChoiceSelected = null;
            isChoosing = false;
            focusTarget = null;
            choiceCallback?.Invoke(index);
            Close();
        }

        private void Close()
        {
            IsOpen = false;
            canvas.enabled = false;
            GameplayUiState.UnregisterModal(this);
            isChoosing = false;
            LastClosedFrame = Time.frameCount;
            Time.timeScale = previousTimeScale;
            choices = new DialogueChoice[0];
            onChoiceSelected = null;
            focusTarget = null;
            speakerAnimator?.SetTalking(false);
            speakerAnimator = null;
            speakerExpressions?.SetTalking(false);
            speakerExpressions = null;
            RestoreCamera();
            Action callback = onCompleted;
            onCompleted = null;
            callback?.Invoke();
        }

        private void EnsureUi()
        {
            if (canvasObject != null)
            {
                return;
            }
            RectTransform root = HudUiFactory.CreateCanvas(
                transform,
                "DialogueCanvas",
                103
            );
            canvasObject = root.gameObject;
            canvas = root.GetComponent<Canvas>();
            canvas.enabled = false;

            Image topBar = HudUiFactory.CreateImage(
                root,
                "TopBar",
                new Color(0f, 0f, 0f, 0.9f)
            );
            topBar.rectTransform.anchorMin = new Vector2(0f, 1f);
            topBar.rectTransform.anchorMax = new Vector2(1f, 1f);
            topBar.rectTransform.pivot = new Vector2(0.5f, 1f);
            topBar.rectTransform.anchoredPosition = Vector2.zero;
            topBar.rectTransform.sizeDelta = new Vector2(0f, 56f);
            Image bottomBar = HudUiFactory.CreateImage(
                root,
                "BottomBar",
                new Color(0f, 0f, 0f, 0.9f)
            );
            bottomBar.rectTransform.anchorMin = Vector2.zero;
            bottomBar.rectTransform.anchorMax = new Vector2(1f, 0f);
            bottomBar.rectTransform.pivot = new Vector2(0.5f, 0f);
            bottomBar.rectTransform.anchoredPosition = Vector2.zero;
            bottomBar.rectTransform.sizeDelta = new Vector2(0f, 56f);

            panel = HudUiFactory.CreatePanel(
                root,
                "DialoguePanel",
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 40f),
                new Vector2(960f, 192f),
                UiTheme.Ink,
                raycastTarget: true
            );
            Outline outline = panel.gameObject.AddComponent<Outline>();
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;
            outline.effectColor = new Color(1f, 1f, 1f, 0.12f);
            Image line = HudUiFactory.CreateImage(
                panel,
                "TopLine",
                UiTheme.Accent
            );
            line.rectTransform.anchorMin = new Vector2(0f, 1f);
            line.rectTransform.anchorMax = new Vector2(1f, 1f);
            line.rectTransform.pivot = new Vector2(0.5f, 1f);
            line.rectTransform.anchoredPosition = Vector2.zero;
            line.rectTransform.sizeDelta = new Vector2(0f, 3f);

            speaker = CreateText(
                panel,
                "Speaker",
                20,
                new Vector2(24f, -14f),
                new Vector2(912f, 34f),
                TextAnchor.MiddleLeft,
                bold: true
            );
            speaker.color = UiTheme.Accent;
            body = CreateText(
                panel,
                "Body",
                17,
                new Vector2(24f, -54f),
                new Vector2(912f, 92f),
                TextAnchor.UpperLeft
            );
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Truncate;
            hint = CreateText(
                panel,
                "Hint",
                12,
                new Vector2(24f, -156f),
                new Vector2(912f, 22f),
                TextAnchor.MiddleRight
            );
            hint.color = UiTheme.Muted;

            for (int index = 0; index < choiceViews.Length; index++)
            {
                RectTransform choice = HudUiFactory.CreatePanel(
                    panel,
                    "Choice" + index,
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(42f, -138f - index * 58f),
                    new Vector2(876f, 46f),
                    UiTheme.Panel,
                    raycastTarget: true
                );
                Button button = choice.gameObject.AddComponent<Button>();
                button.targetGraphic = choice.GetComponent<Image>();
                int choiceIndex = index;
                button.onClick.AddListener(
                    () => SelectChoice(choiceIndex)
                );
                Outline choiceOutline =
                    choice.gameObject.AddComponent<Outline>();
                choiceOutline.effectDistance = new Vector2(1f, -1f);
                choiceOutline.useGraphicAlpha = true;
                Text label = CreateText(
                    choice,
                    "Label",
                    15,
                    new Vector2(14f, -23f),
                    new Vector2(848f, 46f),
                    TextAnchor.MiddleLeft
                );
                choiceViews[index] = new ChoiceView
                {
                    Root = choice.gameObject,
                    Border = choiceOutline,
                    Label = label,
                    Button = button
                };
                choice.gameObject.SetActive(false);
            }
        }

        private static Text CreateText(
            Transform parent,
            string name,
            int size,
            Vector2 position,
            Vector2 dimensions,
            TextAnchor alignment,
            bool bold = false)
        {
            return HudUiFactory.CreateText(
                parent,
                name,
                string.Empty,
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

        private void RefreshUi()
        {
            if (!IsOpen || CurrentLine == null || canvas == null)
            {
                return;
            }
            speaker.text = CurrentLine.Speaker;
            body.text = CurrentLine.Text;
            float height = isChoosing ? 320f : 192f;
            panel.sizeDelta = new Vector2(960f, height);
            for (int index = 0; index < choiceViews.Length; index++)
            {
                bool visible = isChoosing && index < choices.Length;
                ChoiceView view = choiceViews[index];
                view.Root.SetActive(visible);
                if (!visible)
                {
                    continue;
                }
                view.Label.text =
                    $"{index + 1}  {choices[index].Text}";
                view.Border.effectColor = index == selectedChoiceIndex
                    ? new Color(
                        UiTheme.Accent.r,
                        UiTheme.Accent.g,
                        UiTheme.Accent.b,
                        0.85f
                    )
                    : new Color(1f, 1f, 1f, 0.08f);
            }
            string advance =
                Demo02Input.GetPreferredBindingDisplay("Advance");
            string cancel =
                Demo02Input.GetPreferredBindingDisplay("Cancel");
            hint.text = isChoosing
                ? "使用方向键或鼠标选择"
                : choices.Length > 0
                    ? $"{advance}  继续     对话结束后需要选择"
                    : $"{advance}  继续     {cancel}  关闭";
        }

        private void LateUpdate()
        {
            if (!IsOpen || focusTarget == null || dialogueCamera == null)
            {
                return;
            }
            PlayerPartyController party =
                Demo02.Core.GameServices.Resolve<PlayerPartyController>();
            Transform player = party != null
                ? party.ActiveCharacter
                : null;
            if (player == null)
            {
                return;
            }
            Vector3 playerPoint = player.position + Vector3.up * 1.35f;
            Vector3 focusPoint =
                focusTarget.position + Vector3.up * 1.35f;
            Vector3 line = focusPoint - playerPoint;
            float distance = Mathf.Clamp(line.magnitude, 2.5f, 6f);
            Vector3 direction = line.sqrMagnitude > 0.01f
                ? line.normalized
                : player.forward;
            Vector3 side =
                Vector3.Cross(Vector3.up, direction).normalized;
            Vector3 midpoint = (playerPoint + focusPoint) * 0.5f;
            Vector3 desiredPosition = midpoint
                + side * (distance * 0.72f)
                + Vector3.up * 0.6f
                - direction * 0.35f;
            float blend =
                1f - Mathf.Exp(-4.5f * Time.unscaledDeltaTime);
            dialogueCamera.transform.position = Vector3.Lerp(
                dialogueCamera.transform.position,
                desiredPosition,
                blend
            );
            dialogueCamera.transform.LookAt(
                midpoint + Vector3.up * 0.12f,
                Vector3.up
            );
            dialogueCamera.fieldOfView = Mathf.Lerp(
                dialogueCamera.fieldOfView,
                52f,
                blend
            );
        }

        private void OnDisable()
        {
            GameplayUiState.UnregisterModal(this);
            IsOpen = false;
            isChoosing = false;
            if (canvas != null)
            {
                canvas.enabled = false;
            }
            speakerAnimator?.SetTalking(false);
            speakerAnimator = null;
            speakerExpressions?.SetTalking(false);
            speakerExpressions = null;
            Time.timeScale = 1f;
            RestoreCamera();
        }

        private void RestoreCamera()
        {
            if (hasCameraState && dialogueCamera != null)
            {
                dialogueCamera.fieldOfView = originalFieldOfView;
            }
            hasCameraState = false;
        }
    }
}

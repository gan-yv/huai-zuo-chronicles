using System;
using Demo02.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Demo02.Narrative
{
    public sealed class DialogueController : MonoBehaviour
    {
        private DialogueLine[] lines = new DialogueLine[0];
        private int currentLineIndex;
        private int ignoreInputUntilFrame;
        private float previousTimeScale = 1f;
        private Action onCompleted;
        private Action<int> onChoiceSelected;
        private DialogueChoice[] choices = new DialogueChoice[0];
        private bool isChoosing;

        public static DialogueController Instance { get; private set; }
        public bool IsOpen { get; private set; }
        public bool IsChoosing => isChoosing;
        public int LastClosedFrame { get; private set; } = -1;
        public DialogueLine CurrentLine =>
            currentLineIndex >= 0 && currentLineIndex < lines.Length ? lines[currentLineIndex] : null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            Time.timeScale = 1f;
        }

        public void Begin(DialogueLine[] dialogueLines, Action completed = null)
        {
            if (IsOpen || dialogueLines == null || dialogueLines.Length == 0)
            {
                return;
            }

            lines = dialogueLines;
            currentLineIndex = 0;
            onCompleted = completed;
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            IsOpen = true;
            ignoreInputUntilFrame = Time.frameCount + 1;
        }

        public void BeginWithChoice(
            DialogueLine[] dialogueLines,
            DialogueChoice[] dialogueChoices,
            Action<int> choiceSelected,
            Action completed = null,
            bool showChoicesImmediately = false)
        {
            Begin(dialogueLines, completed);
            if (!IsOpen)
            {
                return;
            }

            choices = dialogueChoices ?? new DialogueChoice[0];
            onChoiceSelected = choiceSelected;
            if (showChoicesImmediately && choices.Length > 0)
            {
                isChoosing = true;
                ignoreInputUntilFrame = Time.frameCount + 1;
            }
        }

        private void Update()
        {
            if (!IsOpen || Time.frameCount <= ignoreInputUntilFrame || Keyboard.current == null)
            {
                return;
            }

            if (isChoosing)
            {
                if (Keyboard.current.digit1Key.wasPressedThisFrame)
                {
                    SelectChoice(0);
                }
                else if (Keyboard.current.digit2Key.wasPressedThisFrame)
                {
                    SelectChoice(1);
                }

                return;
            }

            if (Keyboard.current.escapeKey.wasPressedThisFrame && choices.Length == 0)
            {
                Close();
                return;
            }

            if (Keyboard.current.eKey.wasPressedThisFrame
                || Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                Advance();
            }
        }

        private void Advance()
        {
            if (currentLineIndex + 1 < lines.Length)
            {
                currentLineIndex++;
                return;
            }

            if (choices.Length > 0)
            {
                isChoosing = true;
                ignoreInputUntilFrame = Time.frameCount + 1;
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
            choiceCallback?.Invoke(index);
            Close();
        }

        private void Close()
        {
            IsOpen = false;
            isChoosing = false;
            LastClosedFrame = Time.frameCount;
            Time.timeScale = previousTimeScale;
            choices = new DialogueChoice[0];
            onChoiceSelected = null;
            Action callback = onCompleted;
            onCompleted = null;
            callback?.Invoke();
        }

        private void OnGUI()
        {
            if (!IsOpen || CurrentLine == null)
            {
                return;
            }

            float scale = UiTheme.Scale;
            float width = Mathf.Min(960f * scale, Screen.width - 64f);
            float panelHeight = isChoosing ? 320f * scale : 192f * scale;
            Rect panel = new Rect(
                (Screen.width - width) * 0.5f,
                Screen.height - panelHeight - 40f * scale,
                width,
                panelHeight
            );

            UiTheme.DrawRect(panel, UiTheme.Ink);
            UiTheme.DrawBorder(panel, new Color(1f, 1f, 1f, 0.12f));
            UiTheme.DrawRect(
                new Rect(panel.x, panel.y, panel.width, 3f),
                UiTheme.Accent
            );

            GUIStyle speakerStyle = UiTheme.Label(
                20,
                TextAnchor.MiddleLeft,
                bold: true
            );
            speakerStyle.normal.textColor = UiTheme.Accent;
            GUI.Label(
                new Rect(
                    panel.x + 24f * scale,
                    panel.y + 14f * scale,
                    panel.width - 48f * scale,
                    34f * scale
                ),
                CurrentLine.Speaker,
                speakerStyle
            );
            GUI.Label(
                new Rect(
                    panel.x + 24f * scale,
                    panel.y + 54f * scale,
                    panel.width - 48f * scale,
                    (isChoosing ? 74f : 92f) * scale
                ),
                CurrentLine.Text,
                UiTheme.Label(17)
            );

            if (isChoosing)
            {
                float choiceTop = panel.y + 138f * scale;
                for (int index = 0; index < choices.Length; index++)
                {
                    Rect button = new(
                        panel.x + 42f * scale,
                        choiceTop + index * 58f * scale,
                        panel.width - 84f * scale,
                        46f * scale
                    );
                    if (GUI.Button(
                        button,
                        $"{index + 1}  {choices[index].Text}",
                        UiTheme.Button(15)
                    ))
                    {
                        SelectChoice(index);
                    }
                }
            }

            GUIStyle hintStyle = UiTheme.Label(12, TextAnchor.MiddleRight);
            hintStyle.normal.textColor = UiTheme.Muted;
            GUI.Label(
                new Rect(
                    panel.x + 24f * scale,
                    panel.yMax - 36f * scale,
                    panel.width - 48f * scale,
                    22f * scale
                ),
                isChoosing
                    ? "1 / 2  选择调查立场"
                    : choices.Length > 0
                        ? "E / Space  继续     对话结束后需要选择"
                        : "E / Space  继续      Esc  关闭",
                hintStyle
            );
        }
    }
}

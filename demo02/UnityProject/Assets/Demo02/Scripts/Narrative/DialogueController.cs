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

        public static DialogueController Instance { get; private set; }
        public bool IsOpen { get; private set; }
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

        private void Update()
        {
            if (!IsOpen || Time.frameCount <= ignoreInputUntilFrame || Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
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
            currentLineIndex++;
            if (currentLineIndex >= lines.Length)
            {
                Close();
            }
        }

        private void Close()
        {
            IsOpen = false;
            LastClosedFrame = Time.frameCount;
            Time.timeScale = previousTimeScale;
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
            Rect panel = new Rect(
                (Screen.width - width) * 0.5f,
                Screen.height - 232f * scale,
                width,
                192f * scale
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
                    92f * scale
                ),
                CurrentLine.Text,
                UiTheme.Label(18)
            );
            GUIStyle hintStyle = UiTheme.Label(12, TextAnchor.MiddleRight);
            hintStyle.normal.textColor = UiTheme.Muted;
            GUI.Label(
                new Rect(
                    panel.x + 24f * scale,
                    panel.yMax - 36f * scale,
                    panel.width - 48f * scale,
                    22f * scale
                ),
                "E / Space  继续      Esc  关闭",
                hintStyle
            );
        }
    }
}

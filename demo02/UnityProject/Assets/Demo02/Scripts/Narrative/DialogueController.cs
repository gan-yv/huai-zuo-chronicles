using System;
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

            float width = Mathf.Min(900f, Screen.width - 80f);
            Rect panel = new Rect(
                (Screen.width - width) * 0.5f,
                Screen.height - 240f,
                width,
                176f
            );

            GUIStyle speakerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(20, 20, 12, 4)
            };
            GUIStyle textStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 19,
                wordWrap = true,
                padding = new RectOffset(20, 20, 8, 8)
            };
            GUIStyle hintStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.LowerRight,
                fontSize = 14
            };

            GUI.Box(panel, string.Empty);
            GUI.Label(
                new Rect(panel.x, panel.y + 8f, panel.width, 38f),
                CurrentLine.Speaker,
                speakerStyle
            );
            GUI.Label(
                new Rect(panel.x + 6f, panel.y + 48f, panel.width - 12f, 84f),
                CurrentLine.Text,
                textStyle
            );
            GUI.Label(
                new Rect(panel.x, panel.yMax - 32f, panel.width - 18f, 24f),
                "E / Space 继续    Esc 关闭",
                hintStyle
            );
        }
    }
}

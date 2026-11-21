using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Demo02.Core;
using Demo02.Narrative;
using UnityEngine;

namespace Demo02.UI
{
    public sealed class StoryFactNotification : MonoBehaviour
    {
        private const float HoldDuration = 3.6f;
        private const float FadeDuration = 0.25f;

        private readonly Queue<StoryFactDefinition> pendingFacts = new();

        private InvestigationState investigationState;
        private CaseDefinition caseContent;
        private StoryFactDefinition currentFact;
        private float shownAt;

        private void Start()
        {
            investigationState = FindAnyObjectByType<InvestigationState>();
            if (investigationState == null)
            {
                return;
            }

            caseContent = investigationState.CaseContent;
            investigationState.StoryFactAdded += OnStoryFactAdded;

            if (Array.IndexOf(
                Environment.GetCommandLineArgs(),
                "--preview-fact"
            ) >= 0)
            {
                investigationState.SetFact("teahouse-cart-testimony");
                string path = GetArgumentValue("--capture-fact=");
                if (!string.IsNullOrWhiteSpace(path))
                {
                    StartCoroutine(CapturePreview(path));
                }
            }
        }

        private void OnDisable()
        {
            if (investigationState != null)
            {
                investigationState.StoryFactAdded -= OnStoryFactAdded;
            }
        }

        private void OnStoryFactAdded(string factId)
        {
            StoryFactDefinition fact = caseContent?.FindStoryFact(factId);
            if (fact != null)
            {
                pendingFacts.Enqueue(fact);
            }
        }

        private void OnGUI()
        {
            if (currentFact == null && pendingFacts.Count > 0)
            {
                currentFact = pendingFacts.Dequeue();
                shownAt = Time.unscaledTime;
                GameAudio.PlayConfirm();
            }

            if (currentFact == null
                || (DialogueController.Instance != null
                    && DialogueController.Instance.IsOpen)
                || (WorldMapController.Instance != null
                    && WorldMapController.Instance.IsOpen)
                || (InvestigationJournalController.Instance != null
                    && InvestigationJournalController.Instance.IsOpen)
                || (TestimonyBoardController.Instance != null
                    && TestimonyBoardController.Instance.IsOpen)
                || (SaveSlotController.Instance != null
                    && SaveSlotController.Instance.IsOpen)
                || (SettingsController.Instance != null
                    && SettingsController.Instance.IsOpen))
            {
                return;
            }

            float elapsed = Time.unscaledTime - shownAt;
            float totalDuration = HoldDuration + FadeDuration * 2f;
            if (elapsed >= totalDuration)
            {
                currentFact = null;
                return;
            }

            float alpha = Mathf.Clamp01(elapsed / FadeDuration);
            if (elapsed > HoldDuration + FadeDuration)
            {
                alpha = Mathf.Clamp01(
                    (totalDuration - elapsed) / FadeDuration
                );
            }

            float scale = UiTheme.Scale;
            float width = Mathf.Min(430f * scale, Screen.width - 36f);
            Rect panel = new(
                Screen.width - width - 18f * scale,
                98f * scale,
                width,
                96f * scale
            );

            Color previousColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            UiTheme.DrawRect(panel, new Color(0.065f, 0.058f, 0.04f, 0.97f));
            UiTheme.DrawBorder(
                panel,
                new Color(0.88f, 0.7f, 0.38f, 0.66f)
            );
            UiTheme.DrawRect(
                new Rect(panel.x, panel.y, 4f * scale, panel.height),
                UiTheme.Accent
            );

            GUIStyle eyebrow = UiTheme.Label(
                10,
                TextAnchor.MiddleLeft,
                bold: true
            );
            eyebrow.normal.textColor = UiTheme.Accent;
            GUI.Label(
                new Rect(
                    panel.x + 18f * scale,
                    panel.y + 7f * scale,
                    panel.width - 36f * scale,
                    19f * scale
                ),
                "SIDE EVIDENCE · 支线旁证已记录",
                eyebrow
            );
            GUI.Label(
                new Rect(
                    panel.x + 18f * scale,
                    panel.y + 27f * scale,
                    panel.width - 36f * scale,
                    25f * scale
                ),
                currentFact.Title,
                UiTheme.Label(16, TextAnchor.MiddleLeft, bold: true)
            );
            GUI.Label(
                new Rect(
                    panel.x + 18f * scale,
                    panel.y + 54f * scale,
                    panel.width - 36f * scale,
                    34f * scale
                ),
                currentFact.Detail,
                UiTheme.Label(10)
            );
            GUI.color = previousColor;
        }

        private static string GetArgumentValue(string prefix)
        {
            foreach (string argument in Environment.GetCommandLineArgs())
            {
                if (argument.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return argument.Substring(prefix.Length);
                }
            }

            return string.Empty;
        }

        private IEnumerator CapturePreview(string path)
        {
            yield return new WaitForSecondsRealtime(0.7f);
            yield return new WaitForEndOfFrame();
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            ScreenCapture.CaptureScreenshot(path);
            Debug.Log($"Story fact preview captured to {path}");
        }
    }
}

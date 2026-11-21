using Demo02.Narrative;
using UnityEngine;

namespace Demo02.UI
{
    [RequireComponent(typeof(InvestigationState))]
    public sealed class ObjectiveDisplay : MonoBehaviour
    {
        private InvestigationState investigationState;

        private void Awake()
        {
            investigationState = GetComponent<InvestigationState>();
        }

        private void OnGUI()
        {
            if (investigationState == null
                || (WorldMapController.Instance != null && WorldMapController.Instance.IsOpen)
                || (InvestigationJournalController.Instance != null
                    && InvestigationJournalController.Instance.IsOpen))
            {
                return;
            }

            string text = ChapterCopy.GetObjective(investigationState);
            if (investigationState.IsConclusionReached)
            {
                text += "。按 R 重新开始";
            }

            float scale = UiTheme.Scale;
            Rect panel = new(
                20f * scale,
                20f * scale,
                448f * scale,
                66f * scale
            );
            UiTheme.DrawRect(panel, UiTheme.Ink);
            UiTheme.DrawRect(
                new Rect(panel.x, panel.y, 4f * scale, panel.height),
                UiTheme.Accent
            );
            UiTheme.DrawBorder(panel, new Color(1f, 1f, 1f, 0.1f));

            GUIStyle eyebrowStyle = UiTheme.Label(
                11,
                TextAnchor.MiddleLeft,
                bold: true
            );
            eyebrowStyle.normal.textColor = UiTheme.Accent;
            GUI.Label(
                new Rect(
                    panel.x + 18f * scale,
                    panel.y + 8f * scale,
                    panel.width - 32f * scale,
                    20f * scale
                ),
                "CHAPTER 01 · 消失的盐引",
                eyebrowStyle
            );
            GUI.Label(
                new Rect(
                    panel.x + 18f * scale,
                    panel.y + 27f * scale,
                    panel.width - 32f * scale,
                    30f * scale
                ),
                text,
                UiTheme.Label(16)
            );
        }
    }
}

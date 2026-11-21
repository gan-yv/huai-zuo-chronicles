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
                || (WorldMapController.Instance != null && WorldMapController.Instance.IsOpen))
            {
                return;
            }

            string text;
            if (investigationState.SelectedConclusion == ConclusionChoice.Preserve)
            {
                text = "结局：旧街被保留，居民重新出现在城市记录中。按 R 重新开始";
            }
            else if (investigationState.SelectedConclusion == ConclusionChoice.Seal)
            {
                text = "结局：旧街被封存，官府恢复了官方记录。按 R 重新开始";
            }
            else if (investigationState.IsRepaired)
            {
                text = "在街尾决定保留还是封存旧街";
            }
            else if (investigationState.HasAllClues
                && !investigationState.HasAllTimeAnchors)
            {
                text = $"切换时间层，稳定锚点  {investigationState.StabilizedTimeAnchorCount}/{investigationState.RequiredTimeAnchorCount}";
            }
            else if (investigationState.HasAllClues)
            {
                text = "前往街尾，稳定时空接点";
            }
            else
            {
                text = $"调查异常线索  {investigationState.CollectedClueCount}/{investigationState.RequiredClueCount}";
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
                "CURRENT OBJECTIVE · 当前目标",
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

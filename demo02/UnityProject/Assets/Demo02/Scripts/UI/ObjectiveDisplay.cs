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
                text = "盐引巷重新入册，沈砚的名字回到城市记录中。按 R 重新开始";
            }
            else if (investigationState.SelectedConclusion == ConclusionChoice.Seal)
            {
                text = "盐引巷被永久封存，沈砚只留在你的修复记录里。按 R 重新开始";
            }
            else
            {
                text = investigationState.Stage switch
                {
                    ChapterStage.Arrival => "前往旧仓内院，询问守仓人沈伯",
                    ChapterStage.Investigate =>
                        $"调查盐引巷留下的记录  {investigationState.CollectedClueCount}/{investigationState.RequiredClueCount}",
                    ChapterStage.ReviewEvidence => "把三处记录带回旧仓，让沈伯辨认笔迹",
                    ChapterStage.StabilizeAnchors =>
                        $"按 Q 切换时间层，稳定锚点  {investigationState.StabilizedTimeAnchorCount}/{investigationState.RequiredTimeAnchorCount}",
                    ChapterStage.RepairRift => "前往街尾，压下被撕开的记录",
                    ChapterStage.FinalDecision => "在街尾决定盐引巷与沈砚的命运",
                    _ => "调查已经结束"
                };
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

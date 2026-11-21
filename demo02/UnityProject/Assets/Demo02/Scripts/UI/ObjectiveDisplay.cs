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
            if (investigationState == null)
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
                text = "目标：在街尾决定保留还是封存旧街";
            }
            else if (investigationState.HasAllClues)
            {
                text = "目标：前往街尾，稳定时空接点";
            }
            else
            {
                text = $"目标：调查异常线索  {investigationState.CollectedClueCount}/{investigationState.RequiredClueCount}";
            }

            GUIStyle style = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 17,
                padding = new RectOffset(16, 16, 10, 10)
            };

            GUI.Box(new Rect(24f, 24f, 460f, 54f), text, style);
        }
    }
}

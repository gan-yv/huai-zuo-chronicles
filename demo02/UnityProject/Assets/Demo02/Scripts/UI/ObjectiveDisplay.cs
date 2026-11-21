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
            if (investigationState.IsRepaired)
            {
                text = "修复完成：旧街暂时稳定";
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

            GUI.Box(new Rect(24f, 24f, 330f, 48f), text, style);
        }
    }
}

using Demo02.Core;
using Demo02.Narrative;
using Demo02.WorldState;
using UnityEngine;

namespace Demo02.UI
{
    public sealed class WorldContentToast : MonoBehaviour
    {
        private GUIStyle cachedStyle;
        private float cachedScale = -1f;

        private void OnGUI()
        {
            if (Time.unscaledTime >= WorldContentEvent.LastCompletedUntil
                || string.IsNullOrWhiteSpace(
                    WorldContentEvent.LastCompletedText
                )
                || IsBlockedByOtherUi())
            {
                return;
            }

            float scale = UiTheme.Scale;
            if (!Mathf.Approximately(cachedScale, scale))
            {
                cachedScale = scale;
                cachedStyle = UiTheme.Label(
                    15,
                    TextAnchor.MiddleCenter,
                    bold: true
                );
                cachedStyle.normal.textColor = UiTheme.Accent;
            }

            Rect panel = new(
                Screen.width * 0.5f - 280f * scale,
                Screen.height * 0.2f,
                560f * scale,
                48f * scale
            );
            UiTheme.DrawRect(
                panel,
                new Color(0.025f, 0.035f, 0.05f, 0.94f)
            );
            UiTheme.DrawBorder(panel, UiTheme.Accent, 2f);
            GUI.Label(panel, WorldContentEvent.LastCompletedText, cachedStyle);
        }

        private static bool IsBlockedByOtherUi()
        {
            return GameplayUiState.IsGameplayBlocked;
        }
    }
}

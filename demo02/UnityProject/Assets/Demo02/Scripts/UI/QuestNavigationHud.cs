using Demo02.Interaction;
using Demo02.Narrative;
using Demo02.Player;
using Demo02.WorldState;
using UnityEngine;

namespace Demo02.UI
{
    [DefaultExecutionOrder(20)]
    public sealed class QuestNavigationHud : MonoBehaviour
    {
        private InvestigationState investigationState;
        private PlayerPartyController party;
        private TimeLayerController timeLayerController;
        private Transform currentTarget;
        private string navigationHint = string.Empty;
        private float nextRefreshTime;

        private void Start()
        {
            investigationState = FindAnyObjectByType<InvestigationState>();
            party = FindAnyObjectByType<PlayerPartyController>();
            timeLayerController = FindAnyObjectByType<TimeLayerController>();
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRefreshTime)
            {
                return;
            }

            nextRefreshTime = Time.unscaledTime + 0.25f;
            RefreshNavigationTarget();
        }

        private void RefreshNavigationTarget()
        {
            currentTarget = null;
            navigationHint = string.Empty;

            if (investigationState == null || party == null || party.ActiveCharacter == null)
            {
                return;
            }

            if (investigationState.SelectedConclusion != ConclusionChoice.None)
            {
                return;
            }

            if (!investigationState.IsRepaired)
            {
                if (!investigationState.HasAllClues)
                {
                    currentTarget = FindUncollectedClue();
                    return;
                }

                if (!investigationState.HasAllTimeAnchors)
                {
                    TimeAnchor anchor = FindUnstabilizedAnchor();
                    currentTarget = anchor != null ? anchor.transform : null;
                    if (anchor != null && timeLayerController != null)
                    {
                        TimeLayerVisibility visibility = anchor.GetComponent<TimeLayerVisibility>();
                        if (visibility != null
                            && visibility.VisibleLayer != timeLayerController.CurrentLayer)
                        {
                            navigationHint = "按 Q 切换时间层";
                        }
                    }

                    return;
                }

                GameObject repairSite = GameObject.Find("RepairSite_TimeRift");
                currentTarget = repairSite != null ? repairSite.transform : null;
                return;
            }

            GameObject choice = GameObject.Find("Choice_Preserve");
            currentTarget = choice != null ? choice.transform : null;
            navigationHint = "选择旧街结局";
        }

        private Transform FindUncollectedClue()
        {
            ClueInteractable[] clues = FindObjectsByType<ClueInteractable>(
                FindObjectsInactive.Exclude
            );
            foreach (ClueInteractable clue in clues)
            {
                if (!investigationState.HasClue(clue.ClueId))
                {
                    return clue.transform;
                }
            }

            return null;
        }

        private TimeAnchor FindUnstabilizedAnchor()
        {
            TimeAnchor[] anchors = FindObjectsByType<TimeAnchor>(FindObjectsInactive.Exclude);
            foreach (TimeAnchor anchor in anchors)
            {
                if (!investigationState.HasStabilizedAnchor(anchor.AnchorId))
                {
                    return anchor;
                }
            }

            return null;
        }

        private void OnGUI()
        {
            if ((WorldMapController.Instance != null && WorldMapController.Instance.IsOpen)
                || currentTarget == null
                || party == null
                || party.ActiveCharacter == null)
            {
                return;
            }

            Camera currentCamera = Camera.main;
            Vector3 offset = currentTarget.position - party.ActiveCharacter.position;
            offset.y = 0f;
            float distance = offset.magnitude;

            string direction = string.Empty;
            if (currentCamera != null && distance > 0.1f)
            {
                Vector3 flatForward = currentCamera.transform.forward;
                flatForward.y = 0f;
                float angle = Vector3.SignedAngle(flatForward, offset.normalized, Vector3.up);
                direction = GetDirectionLabel(angle);
            }

            string text = $"{direction}  {distance:0.#}m";
            if (!string.IsNullOrWhiteSpace(navigationHint))
            {
                text += $"    {navigationHint}";
            }

            float scale = UiTheme.Scale;
            float width = 430f * scale;
            Rect panel = new(
                (Screen.width - width) * 0.5f,
                84f * scale,
                width,
                48f * scale
            );
            UiTheme.DrawRect(panel, UiTheme.Panel);
            UiTheme.DrawRect(
                new Rect(panel.x, panel.y, panel.width, 2f),
                UiTheme.Accent
            );
            UiTheme.DrawBorder(panel, new Color(1f, 1f, 1f, 0.12f));

            GUIStyle style = UiTheme.Label(15, TextAnchor.MiddleCenter, bold: true);
            style.normal.textColor = new Color(0.92f, 0.82f, 0.62f);
            GUI.Label(panel, text, style);
        }

        private static string GetDirectionLabel(float angle)
        {
            float absolute = Mathf.Abs(angle);
            if (absolute <= 22.5f) return "前方";
            if (absolute >= 157.5f) return "后方";
            if (angle > 0f) return angle < 67.5f ? "右前" : (angle < 112.5f ? "右侧" : "右后");
            return angle > -67.5f ? "左前" : (angle > -112.5f ? "左侧" : "左后");
        }
    }
}

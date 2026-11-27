using Demo02.Core;
using Demo02.Player;
using Demo02.UI;
using UnityEngine;

namespace Demo02.WorldState
{
    public sealed class GlideChallengeController : MonoBehaviour
    {
        [SerializeField] private string contentId = "dongguan-rooftop-glide";
        [SerializeField] private Vector3[] checkpoints = new Vector3[0];
        [SerializeField, Min(5f)] private float timeLimit = 40f;
        [SerializeField, Min(0.5f)] private float checkpointRadius = 3.5f;

        private int checkpointIndex;
        private bool active;
        private float startedAt;
        private float liftGraceUntil;
        private float checkpointDistance;
        private float finishUntil;
        private float failUntil;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private float cachedScale = -1f;

        public bool CanUseLift =>
            !active
            && Demo02.Core.GameServices.Resolve<WorldRuntime>() != null
            && !Demo02.Core.GameServices.Resolve<WorldRuntime>().IsWorldContentCompleted(contentId);

        public bool IsActive => active;
        public bool IsCompleted =>
            Demo02.Core.GameServices.Resolve<WorldRuntime>() != null
            && Demo02.Core.GameServices.Resolve<WorldRuntime>().IsWorldContentCompleted(contentId);

        public void Configure(
            string id,
            Vector3[] route,
            float limit)
        {
            contentId = id;
            checkpoints = route ?? new Vector3[0];
            timeLimit = Mathf.Max(5f, limit);
        }

        public void NotifyLiftEntered()
        {
            if (!CanUseLift || checkpoints.Length == 0)
            {
                return;
            }

            active = true;
            checkpointIndex = 0;
            startedAt = Time.time;
            liftGraceUntil = startedAt + 1.25f;
            Demo02.Core.GameServices.Resolve<WorldRuntime>()?.MarkWorldContentStarted(contentId);
            PlayerPartyController party =
                Demo02.Core.GameServices.Resolve<PlayerPartyController>();
            Transform player = party != null ? party.ActiveCharacter : null;
            player?.GetComponent<CharacterRuntime>()
                ?.RestoreStamina(
                    player.GetComponent<CharacterRuntime>().MaxStamina
                );
        }

        private void Update()
        {
            if (!active)
            {
                if (Demo02.Core.GameServices.Resolve<WorldRuntime>() == null
                    || Demo02.Core.GameServices.Resolve<WorldRuntime>().IsWorldContentCompleted(
                        contentId
                    ))
                {
                    return;
                }
                PlayerPartyController nearbyParty =
                    Demo02.Core.GameServices.Resolve<PlayerPartyController>();
                Transform nearbyPlayer = nearbyParty != null
                    ? nearbyParty.ActiveCharacter
                    : null;
                if (nearbyPlayer != null)
                {
                    Vector3 offset =
                        nearbyPlayer.position - transform.position;
                    offset.y = 0f;
                    if (offset.sqrMagnitude <= 16f)
                    {
                        NotifyLiftEntered();
                    }
                }
                return;
            }

            PlayerPartyController party =
                Demo02.Core.GameServices.Resolve<PlayerPartyController>();
            Transform player = party != null ? party.ActiveCharacter : null;
            if (player == null)
            {
                return;
            }

            CharacterController controller =
                player.GetComponent<CharacterController>();
            bool failedToLift = controller != null
                && controller.isGrounded
                && Time.time > liftGraceUntil;
            if (Time.time - startedAt > timeLimit
                || player.position.y < -1f
                || failedToLift)
            {
                active = false;
                failUntil = Time.unscaledTime + 2.5f;
                return;
            }

            Vector3 checkpoint = checkpoints[checkpointIndex];
            checkpointDistance = Vector3.Distance(
                player.position,
                checkpoint
            );
            if ((player.position - checkpoint).sqrMagnitude
                > checkpointRadius * checkpointRadius)
            {
                return;
            }

            checkpointIndex++;
            if (checkpointIndex < checkpoints.Length)
            {
                return;
            }

            active = false;
            Demo02.Core.GameServices.Resolve<WorldRuntime>()?.MarkWorldContentCompleted(contentId);
            InventoryState inventory = Demo02.Core.GameServices.Resolve<InventoryState>() != null
                ? Demo02.Core.GameServices.Resolve<InventoryState>()
                : Demo02.Core.GameServices.Resolve<InventoryState>();
            inventory?.AddItem("ginger-tea", 2);
            finishUntil = Time.unscaledTime + 4f;
            GameAudio.PlayConfirm();
        }

        private void OnGUI()
        {
            if (GameplayUiState.IsGameplayBlocked
                || (!active && Time.unscaledTime >= finishUntil
                    && Time.unscaledTime >= failUntil))
            {
                return;
            }

            float scale = UiTheme.Scale;
            if (!Mathf.Approximately(cachedScale, scale))
            {
                cachedScale = scale;
                titleStyle = UiTheme.Label(
                    15,
                    TextAnchor.MiddleCenter,
                    bold: true
                );
                titleStyle.normal.textColor = UiTheme.Accent;
                bodyStyle = UiTheme.Label(12, TextAnchor.MiddleCenter);
            }

            Rect panel = new(
                Screen.width * 0.5f - 265f * scale,
                Screen.height * 0.255f + 118f * scale,
                530f * scale,
                62f * scale
            );
            UiTheme.DrawRect(panel, new Color(0.025f, 0.035f, 0.05f, 0.94f));
            UiTheme.DrawBorder(panel, new Color(0.4f, 0.82f, 0.72f, 0.7f));
            string title;
            string body;
            if (active)
            {
                title = "东关风廊 · 屋顶航线";
                int remaining = Mathf.CeilToInt(
                    timeLimit - (Time.time - startedAt)
                );
                body = $"检查点 {checkpointIndex + 1}/{checkpoints.Length}"
                    + $"    目标 {checkpointDistance:0}m"
                    + $"    剩余 {remaining} 秒";
            }
            else if (Time.unscaledTime < finishUntil)
            {
                title = "航线完成";
                body = "获得热姜汤 ×2 · 记录已写入世界进度";
            }
            else
            {
                title = "航线中断";
                body = "回到风场重新开始";
            }
            GUI.Label(
                new Rect(
                    panel.x,
                    panel.y + 4f * scale,
                    panel.width,
                    25f * scale
                ),
                title,
                titleStyle
            );
            GUI.Label(
                new Rect(
                    panel.x,
                    panel.y + 29f * scale,
                    panel.width,
                    26f * scale
                ),
                body,
                bodyStyle
            );
        }
    }
}

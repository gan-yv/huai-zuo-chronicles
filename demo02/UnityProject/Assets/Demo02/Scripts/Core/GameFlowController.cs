using Demo02.Narrative;
using Demo02.Player;
using Demo02.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Demo02.Core
{
    [DefaultExecutionOrder(-50)]
    public sealed class GameFlowController : MonoBehaviour
    {
        private InvestigationState investigationState;
        private PlayerPartyController party;
        private bool isPaused;
        private bool isEndingFrozen;

        private void Awake()
        {
            investigationState = FindAnyObjectByType<InvestigationState>();
            party = FindAnyObjectByType<PlayerPartyController>();
        }

        private void Start()
        {
            Time.timeScale = 1f;
        }

        private void Update()
        {
            if (DialogueController.Instance != null && DialogueController.Instance.IsOpen)
            {
                return;
            }

            if (WorldMapController.Instance != null && WorldMapController.Instance.IsOpen)
            {
                return;
            }

            if (IsEnding)
            {
                if (!isEndingFrozen)
                {
                    isEndingFrozen = true;
                    Time.timeScale = 0f;
                }

                if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                {
                    RestartCurrentScene();
                }

                return;
            }

            if (isEndingFrozen)
            {
                isEndingFrozen = false;
                Time.timeScale = 1f;
            }

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                TogglePause();
                return;
            }

            if (isPaused)
            {
                return;
            }
        }

        private void OnDisable()
        {
            Time.timeScale = 1f;
        }

        private void OnGUI()
        {
            if (!isPaused && !IsEnding)
            {
                return;
            }

            UiTheme.DrawRect(
                new Rect(0f, 0f, Screen.width, Screen.height),
                new Color(0.015f, 0.02f, 0.03f, 0.86f)
            );
            float scale = UiTheme.Scale;

            string title;
            string body;
            if (IsEnding)
            {
                if (investigationState != null
                    && investigationState.SelectedConclusion == ConclusionChoice.Preserve)
                {
                    title = "结局：盐引巷重新入册";
                    body = "沈砚的名字重新出现在盐仓账册上，盐引巷回到广陵地图。陆崇背后的势力也看见了修复留下的光。";
                }
                else if (investigationState != null
                    && investigationState.SelectedConclusion == ConclusionChoice.Seal)
                {
                    title = "结局：盐引巷封存";
                    body = "时缝平息，私盐案被永久藏进旧街。沈砚没有回来，只在你手中的修复记录里留下一行名字。";
                }
                else
                {
                    title = "队伍倒下";
                    body = "当前行动失败，可以从最近检查点重新开始。";
                }
            }
            else
            {
                title = "游戏暂停";
                body = "世界已经停止，选择接下来的行动。";
            }

            UiTheme.DrawRect(
                new Rect(
                    Screen.width * 0.5f - 48f * scale,
                    Screen.height * 0.22f,
                    96f * scale,
                    3f
                ),
                UiTheme.Accent
            );
            GUI.Label(
                new Rect(0f, Screen.height * 0.255f, Screen.width, 54f * scale),
                title,
                UiTheme.Label(31, TextAnchor.MiddleCenter, bold: true)
            );
            GUI.Label(
                new Rect(
                    Screen.width * 0.18f,
                    Screen.height * 0.35f,
                    Screen.width * 0.64f,
                    74f * scale
                ),
                body,
                UiTheme.Label(17, TextAnchor.MiddleCenter)
            );

            float width = 300f * scale;
            float x = (Screen.width - width) * 0.5f;
            float y = Screen.height * 0.48f;
            float buttonHeight = 48f * scale;
            if (!IsEnding && GUI.Button(
                new Rect(x, y, width, buttonHeight),
                "继续游戏",
                UiTheme.Button(16)
            ))
            {
                TogglePause();
            }
            float restartY = IsEnding ? y : y + 62f * scale;
            if (GUI.Button(
                new Rect(x, restartY, width, buttonHeight),
                "重新开始",
                UiTheme.Button(16)
            ))
            {
                RestartCurrentScene();
            }
            if (GUI.Button(
                new Rect(x, restartY + 62f * scale, width, buttonHeight),
                "返回标题",
                UiTheme.Button(16)
            ))
            {
                ReturnToTitle();
            }
        }

        private bool IsEnding =>
            (investigationState != null && investigationState.IsConclusionReached)
            || (party != null && party.IsPartyDefeated);

        public void TogglePause()
        {
            isPaused = !isPaused;
            Time.timeScale = isPaused ? 0f : 1f;
        }

        public void RestartCurrentScene()
        {
            Time.timeScale = 1f;
            Scene activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(
                activeScene.buildIndex >= 0 ? activeScene.buildIndex : 0
            );
        }

        public void ReturnToTitle()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneNames.Boot);
        }
    }
}

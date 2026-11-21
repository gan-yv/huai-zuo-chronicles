using Demo02.Narrative;
using Demo02.Player;
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

            Color previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.78f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previousColor;

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 30,
                fontStyle = FontStyle.Bold
            };
            GUIStyle bodyStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                wordWrap = true
            };

            string title;
            string body;
            if (IsEnding)
            {
                if (investigationState != null
                    && investigationState.SelectedConclusion == ConclusionChoice.Preserve)
                {
                    title = "结局：旧街被保留";
                    body = "居民重新出现在城市记录中。官府、盐商与旧街之间的冲突才刚刚开始。";
                }
                else if (investigationState != null
                    && investigationState.SelectedConclusion == ConclusionChoice.Seal)
                {
                    title = "结局：旧街被封存";
                    body = "官方记录重新稳定，但旧街居民从此从城市生活中消失。";
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

            GUI.Label(
                new Rect(0f, Screen.height * 0.25f, Screen.width, 54f),
                title,
                titleStyle
            );
            GUI.Label(
                new Rect(Screen.width * 0.2f, Screen.height * 0.34f, Screen.width * 0.6f, 64f),
                body,
                bodyStyle
            );

            const float width = 280f;
            float x = (Screen.width - width) * 0.5f;
            float y = Screen.height * 0.46f;
            if (!IsEnding && GUI.Button(new Rect(x, y, width, 48f), "继续游戏"))
            {
                TogglePause();
            }
            float restartY = IsEnding ? y : y + 64f;
            if (GUI.Button(new Rect(x, restartY, width, 48f), "重新开始"))
            {
                RestartCurrentScene();
            }
            if (GUI.Button(new Rect(x, restartY + 64f, width, 48f), "返回标题"))
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

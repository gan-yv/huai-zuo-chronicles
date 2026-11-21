using Demo02.Narrative;
using Demo02.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Demo02.Core
{
    public sealed class GameFlowController : MonoBehaviour
    {
        private InvestigationState investigationState;
        private PlayerPartyController party;
        private bool isPaused;

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
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                TogglePause();
                return;
            }

            if (isPaused)
            {
                return;
            }

            bool canRestart = (investigationState != null && investigationState.IsConclusionReached)
                || (party != null && party.IsPartyDefeated);
            if (!canRestart
                || Keyboard.current == null
                || !Keyboard.current.rKey.wasPressedThisFrame)
            {
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.buildIndex >= 0)
            {
                SceneManager.LoadScene(activeScene.buildIndex);
            }
            else
            {
                SceneManager.LoadScene(SceneNames.Guangling);
            }
        }

        private void OnDisable()
        {
            Time.timeScale = 1f;
        }

        private void OnGUI()
        {
            if (!isPaused)
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
            GUI.Label(
                new Rect(0f, Screen.height * 0.25f, Screen.width, 54f),
                "游戏暂停",
                titleStyle
            );

            const float width = 280f;
            float x = (Screen.width - width) * 0.5f;
            float y = Screen.height * 0.4f;
            if (GUI.Button(new Rect(x, y, width, 48f), "继续游戏"))
            {
                TogglePause();
            }
            if (GUI.Button(new Rect(x, y + 64f, width, 48f), "重新开始"))
            {
                RestartCurrentScene();
            }
            if (GUI.Button(new Rect(x, y + 128f, width, 48f), "返回标题"))
            {
                ReturnToTitle();
            }
        }

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

using Demo02.WorldState;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Demo02.Core
{
    public sealed class BootController : MonoBehaviour
    {
        private void OnGUI()
        {
            Color previousColor = GUI.color;
            GUI.color = new Color(0.04f, 0.06f, 0.08f, 0.94f);
            GUI.DrawTexture(
                new Rect(0f, 0f, Screen.width, Screen.height),
                Texture2D.whiteTexture
            );
            GUI.color = previousColor;

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 38,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.9f, 0.82f, 0.62f) }
            };
            GUIStyle subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                normal = { textColor = new Color(0.7f, 0.72f, 0.74f) }
            };

            GUI.Label(
                new Rect(0f, Screen.height * 0.24f, Screen.width, 70f),
                "CHRONICLES OF STRANGE EVENTS IN HUAI-ZUO",
                titleStyle
            );
            GUI.Label(
                new Rect(0f, Screen.height * 0.34f, Screen.width, 40f),
                "广陵灰盒原型",
                subtitleStyle
            );

            const float buttonWidth = 300f;
            const float buttonHeight = 52f;
            float buttonX = (Screen.width - buttonWidth) * 0.5f;
            float buttonY = Screen.height * 0.55f;

            if (GUI.Button(new Rect(buttonX, buttonY, buttonWidth, buttonHeight), "开始游戏"))
            {
                StartGame();
            }

            if (GUI.Button(
                new Rect(buttonX, buttonY + 72f, buttonWidth, buttonHeight),
                "退出游戏"
            ))
            {
                QuitGame();
            }

            GUI.Label(
                new Rect(0f, Screen.height - 60f, Screen.width, 30f),
                "当前版本：Demo02 v0.6 灰盒开发版",
                subtitleStyle
            );
        }

        private static void StartGame()
        {
            if (WorldRuntime.Instance != null)
            {
                WorldRuntime.Instance.RequestSceneLoad(SceneNames.Guangling, "guangling_start");
            }
            else
            {
                SceneManager.LoadScene(SceneNames.Guangling);
            }
        }

        private static void QuitGame()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}

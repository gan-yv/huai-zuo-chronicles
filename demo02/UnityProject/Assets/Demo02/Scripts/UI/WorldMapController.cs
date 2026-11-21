using Demo02.Interaction;
using Demo02.Narrative;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Demo02.UI
{
    public sealed class WorldMapController : MonoBehaviour
    {
        private bool isOpen;
        private float previousTimeScale = 1f;
        private string statusMessage;

        private void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.mKey.wasPressedThisFrame)
            {
                return;
            }

            if (DialogueController.Instance != null && DialogueController.Instance.IsOpen)
            {
                return;
            }

            ToggleMap();
        }

        private void OnDisable()
        {
            if (isOpen)
            {
                Time.timeScale = previousTimeScale;
            }
        }

        private void OnGUI()
        {
            if (!isOpen)
            {
                return;
            }

            Color previousColor = GUI.color;
            GUI.color = new Color(0.03f, 0.05f, 0.07f, 0.9f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previousColor;

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 30,
                fontStyle = FontStyle.Bold
            };
            GUI.Label(
                new Rect(0f, 48f, Screen.width, 54f),
                "世界地图 · 街内传送",
                titleStyle
            );

            const float buttonWidth = 420f;
            float x = (Screen.width - buttonWidth) * 0.5f;
            float y = 150f;

            if (GUI.Button(new Rect(x, y, buttonWidth, 58f), "官署前街"))
            {
                TravelTo("PlayerSpawn", "official-street", "官署前街");
            }
            if (GUI.Button(new Rect(x, y + 76f, buttonWidth, 58f), "东关街"))
            {
                TravelTo("DongguanSpawn", "dongguan-street", "东关街");
            }
            if (GUI.Button(new Rect(x, y + 152f, buttonWidth, 58f), "彩衣街"))
            {
                TravelTo("CaiyiSpawn", "caiyi-street", "彩衣街");
            }

            if (GUI.Button(
                new Rect(x, y + 232f, buttonWidth, 48f),
                "关闭地图  [M]"
            ))
            {
                ToggleMap();
            }

            if (!string.IsNullOrWhiteSpace(statusMessage))
            {
                GUI.Label(
                    new Rect(0f, Screen.height - 72f, Screen.width, 32f),
                    statusMessage,
                    new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 16
                    }
                );
            }
        }

        private void TravelTo(string spawnName, string areaId, string areaName)
        {
            GameObject spawn = GameObject.Find(spawnName);
            if (spawn == null)
            {
                statusMessage = $"找不到地点：{areaName}";
                return;
            }

            if (LocalWorldPortal.TeleportParty(
                spawn.transform,
                areaId,
                areaName,
                out string message))
            {
                statusMessage = message;
                ToggleMap();
            }
            else
            {
                statusMessage = "当前没有可传送的角色。";
            }
        }

        private void ToggleMap()
        {
            isOpen = !isOpen;
            statusMessage = string.Empty;
            if (isOpen)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }
            else
            {
                Time.timeScale = previousTimeScale;
            }
        }
    }
}

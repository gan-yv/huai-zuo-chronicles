using Demo02.WorldState;
using UnityEngine;
using UnityEngine.UI;

namespace Demo02.UI
{
    public sealed class WorldContentToast : MonoBehaviour
    {
        private GameObject canvasObject;
        private Canvas canvas;
        private Text label;
        private string cachedText = string.Empty;

        private void Awake()
        {
            EnsureUi();
        }

        private void Update()
        {
            EnsureUi();
            string text = WorldContentEvent.LastCompletedText;
            bool visible =
                Time.unscaledTime < WorldContentEvent.LastCompletedUntil
                && !string.IsNullOrWhiteSpace(text)
                && !GameplayUiState.IsGameplayBlocked;
            canvas.enabled = visible;
            if (!visible)
            {
                return;
            }
            if (cachedText != text)
            {
                cachedText = text;
                label.text = text;
            }
        }

        private void OnDisable()
        {
            if (canvas != null)
            {
                canvas.enabled = false;
            }
        }

        private void OnEnable()
        {
            if (canvas != null)
            {
                canvas.enabled = true;
            }
        }

        private void OnDestroy()
        {
            if (canvasObject == null)
            {
                return;
            }
            if (Application.isPlaying)
            {
                Destroy(canvasObject);
            }
            else
            {
                DestroyImmediate(canvasObject);
            }
            canvasObject = null;
        }

        private void EnsureUi()
        {
            if (canvasObject != null)
            {
                return;
            }
            RectTransform root = HudUiFactory.CreateCanvas(
                transform,
                "WorldContentToastCanvas",
                25
            );
            canvasObject = root.gameObject;
            canvas = root.GetComponent<Canvas>();
            RectTransform panel = HudUiFactory.CreatePanel(
                root,
                "ToastPanel",
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -216f),
                new Vector2(560f, 48f),
                new Color(0.025f, 0.035f, 0.05f, 0.94f)
            );
            Outline outline = panel.gameObject.AddComponent<Outline>();
            outline.effectDistance = new Vector2(2f, -2f);
            outline.effectColor = UiTheme.Accent;
            label = HudUiFactory.CreateText(
                panel,
                "Label",
                string.Empty,
                15,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                TextAnchor.MiddleCenter,
                bold: true
            );
            label.color = UiTheme.Accent;
        }
    }
}

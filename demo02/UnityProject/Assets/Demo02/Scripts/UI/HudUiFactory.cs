using UnityEngine;
using UnityEngine.UI;

namespace Demo02.UI
{
    /// <summary>
    /// Shared primitives for runtime HUDs that create their UGUI tree once.
    /// </summary>
    internal static class HudUiFactory
    {
        internal static readonly Color CardColor =
            new(0.025f, 0.035f, 0.05f, 0.94f);
        internal static readonly Color MutedColor =
            new(0.62f, 0.66f, 0.7f, 1f);

        internal static RectTransform CreateCanvas(
            Transform parent,
            string name,
            int sortingOrder)
        {
            GameObject gameObject = new(
                name,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler)
            );
            gameObject.transform.SetParent(parent, false);
            Canvas canvas = gameObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            CanvasScaler scaler =
                gameObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution =
                new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform root =
                gameObject.GetComponent<RectTransform>();
            Stretch(root);
            return root;
        }

        internal static RectTransform CreatePanel(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size,
            Color color,
            bool raycastTarget = false)
        {
            GameObject gameObject = new(
                name,
                typeof(RectTransform),
                typeof(Image)
            );
            gameObject.transform.SetParent(parent, false);
            RectTransform rect =
                gameObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Image image = gameObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = raycastTarget;
            return rect;
        }

        internal static Image CreateImage(
            Transform parent,
            string name,
            Color color)
        {
            GameObject gameObject =
                new(name, typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            Image image = gameObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        internal static Text CreateText(
            Transform parent,
            string name,
            string content,
            int fontSize,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size,
            TextAnchor alignment = TextAnchor.MiddleLeft,
            bool bold = false)
        {
            GameObject gameObject =
                new(name, typeof(RectTransform), typeof(Text));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect =
                gameObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Text text = gameObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>(
                "LegacyRuntime.ttf"
            );
            text.fontSize = fontSize;
            text.fontStyle = bold
                ? FontStyle.Bold
                : FontStyle.Normal;
            text.alignment = alignment;
            text.color = MutedColor;
            text.horizontalOverflow =
                HorizontalWrapMode.Overflow;
            text.verticalOverflow =
                VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            text.text = content;
            return text;
        }

        internal static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        internal static void SetText(Text target, string value)
        {
            if (target != null && target.text != value)
            {
                target.text = value;
            }
        }
    }
}

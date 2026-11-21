using System.Collections.Generic;
using Demo02.Core;
using UnityEngine;

namespace Demo02.UI
{
    internal static class UiTheme
    {
        public static readonly Color Ink = new(0.025f, 0.035f, 0.05f, 0.94f);
        public static readonly Color Panel = new(0.075f, 0.09f, 0.115f, 0.96f);
        public static readonly Color PanelHover = new(0.13f, 0.15f, 0.18f, 0.98f);
        public static readonly Color Accent = new(0.88f, 0.7f, 0.38f, 1f);
        public static readonly Color Text = new(0.94f, 0.93f, 0.88f, 1f);
        public static readonly Color Muted = new(0.62f, 0.65f, 0.68f, 1f);
        private static readonly Texture2D PanelTexture = CreateTexture(Panel);
        private static readonly Texture2D PanelHoverTexture = CreateTexture(PanelHover);
        private static readonly Texture2D AccentTexture = CreateTexture(Accent);
        private static readonly Dictionary<int, GUIStyle> ButtonStyleCache = new();

        public static float Scale =>
            Mathf.Clamp(
                Screen.height / 900f * GameSettings.UiScaleMultiplier,
                0.72f,
                1.55f
            );

        public static GUIStyle Label(
            int fontSize,
            TextAnchor alignment = TextAnchor.MiddleLeft,
            bool bold = false)
        {
            return new GUIStyle(GUI.skin.label)
            {
                alignment = alignment,
                fontSize = Mathf.Max(10, Mathf.RoundToInt(fontSize * Scale)),
                fontStyle = bold ? FontStyle.Bold : FontStyle.Normal,
                normal = { textColor = Text },
                wordWrap = true
            };
        }

        public static GUIStyle Button(int fontSize)
        {
            int pixelFontSize = Mathf.Max(10, Mathf.RoundToInt(fontSize * Scale));
            if (ButtonStyleCache.TryGetValue(
                pixelFontSize,
                out GUIStyle cached
            ))
            {
                return cached;
            }

            GUIStyle style = new(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = pixelFontSize,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal =
                {
                    textColor = Text,
                    background = PanelTexture
                },
                hover =
                {
                    textColor = Accent,
                    background = PanelHoverTexture
                },
                active =
                {
                    textColor = Ink,
                    background = AccentTexture
                }
            };
            ButtonStyleCache[pixelFontSize] = style;
            return style;
        }

        public static void DrawRect(Rect rect, Color color)
        {
            Color previousColor = GUI.color;
            color.a *= previousColor.a;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        public static void DrawBorder(Rect rect, Color color, float thickness = 1f)
        {
            DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        private static Texture2D CreateTexture(Color color)
        {
            Texture2D texture = new(1, 1)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }
}

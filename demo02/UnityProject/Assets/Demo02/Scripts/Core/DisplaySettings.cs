using UnityEngine;

namespace Demo02.Core
{
    public static class DisplaySettings
    {
        private const string BorderlessPreference =
            "demo02.display.borderless";

        public static bool UseBorderlessFullscreen
        {
            get => PlayerPrefs.GetInt(BorderlessPreference, 1) == 1;
            private set
            {
                PlayerPrefs.SetInt(
                    BorderlessPreference,
                    value ? 1 : 0
                );
                PlayerPrefs.Save();
            }
        }

        public static void Apply()
        {
            Display display = Display.main;
            if (display == null
                || display.systemWidth <= 0
                || display.systemHeight <= 0)
            {
                return;
            }

            if (UseBorderlessFullscreen)
            {
                Screen.SetResolution(
                    display.systemWidth,
                    display.systemHeight,
                    FullScreenMode.FullScreenWindow
                );
                return;
            }

            int maxWidth = Mathf.Max(960, display.systemWidth - 96);
            int maxHeight = Mathf.Max(540, display.systemHeight - 120);
            int targetWidth = maxWidth;
            int targetHeight = Mathf.RoundToInt(targetWidth * 9f / 16f);
            if (targetHeight > maxHeight)
            {
                targetHeight = maxHeight;
                targetWidth = Mathf.RoundToInt(targetHeight * 16f / 9f);
            }
            Screen.SetResolution(
                targetWidth,
                targetHeight,
                FullScreenMode.Windowed
            );
        }

        public static void ToggleMode()
        {
            UseBorderlessFullscreen = !UseBorderlessFullscreen;
            Apply();
        }
    }
}

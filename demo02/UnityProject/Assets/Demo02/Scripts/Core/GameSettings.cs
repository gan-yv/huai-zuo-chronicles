using UnityEngine;

namespace Demo02.Core
{
    public static class GameSettings
    {
        private const string MasterVolumeKey = "demo02.settings.masterVolume";
        private const string UiScaleKey = "demo02.settings.uiScale";
        private const string CameraSensitivityKey = "demo02.settings.cameraSensitivity";
        private const string ReduceMotionKey = "demo02.settings.reduceMotion";

        private static bool isLoaded;
        private static float masterVolume = 0.8f;
        private static float uiScaleMultiplier = 1f;
        private static float cameraSensitivity = 1f;
        private static bool reduceMotion;

        public static float MasterVolume
        {
            get
            {
                EnsureLoaded();
                return masterVolume;
            }
        }

        public static float UiScaleMultiplier
        {
            get
            {
                EnsureLoaded();
                return uiScaleMultiplier;
            }
        }

        public static float CameraSensitivity
        {
            get
            {
                EnsureLoaded();
                return cameraSensitivity;
            }
        }

        public static bool ReduceMotion
        {
            get
            {
                EnsureLoaded();
                return reduceMotion;
            }
        }

        public static void SetMasterVolume(float value)
        {
            EnsureLoaded();
            masterVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MasterVolumeKey, masterVolume);
            Save();
            GameAudio.RefreshVolume();
        }

        public static void SetUiScale(float value)
        {
            EnsureLoaded();
            uiScaleMultiplier = Mathf.Clamp(value, 0.8f, 1.3f);
            PlayerPrefs.SetFloat(UiScaleKey, uiScaleMultiplier);
            Save();
        }

        public static void SetCameraSensitivity(float value)
        {
            EnsureLoaded();
            cameraSensitivity = Mathf.Clamp(value, 0.5f, 1.6f);
            PlayerPrefs.SetFloat(CameraSensitivityKey, cameraSensitivity);
            Save();
        }

        public static void SetReduceMotion(bool value)
        {
            EnsureLoaded();
            reduceMotion = value;
            PlayerPrefs.SetInt(ReduceMotionKey, reduceMotion ? 1 : 0);
            Save();
        }

        public static void ResetToDefaults()
        {
            PlayerPrefs.DeleteKey(MasterVolumeKey);
            PlayerPrefs.DeleteKey(UiScaleKey);
            PlayerPrefs.DeleteKey(CameraSensitivityKey);
            PlayerPrefs.DeleteKey(ReduceMotionKey);
            isLoaded = false;
            EnsureLoaded();
            Save();
            GameAudio.RefreshVolume();
        }

        private static void EnsureLoaded()
        {
            if (isLoaded)
            {
                return;
            }

            masterVolume = Mathf.Clamp01(
                PlayerPrefs.GetFloat(MasterVolumeKey, 0.8f)
            );
            uiScaleMultiplier = Mathf.Clamp(
                PlayerPrefs.GetFloat(UiScaleKey, 1f),
                0.8f,
                1.3f
            );
            cameraSensitivity = Mathf.Clamp(
                PlayerPrefs.GetFloat(CameraSensitivityKey, 1f),
                0.5f,
                1.6f
            );
            reduceMotion = PlayerPrefs.GetInt(ReduceMotionKey, 0) == 1;
            isLoaded = true;
        }

        private static void Save()
        {
            PlayerPrefs.Save();
        }
    }
}

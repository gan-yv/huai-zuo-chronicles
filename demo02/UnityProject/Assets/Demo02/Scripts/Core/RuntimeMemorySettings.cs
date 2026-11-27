using UnityEngine;

namespace Demo02.Core
{
    public static class RuntimeMemorySettings
    {
        public static void Apply()
        {
            float graphicsBudget = Mathf.Max(
                256f,
                SystemInfo.graphicsMemorySize * 0.12f
            );
            QualitySettings.streamingMipmapsActive = true;
            QualitySettings.streamingMipmapsAddAllCameras = true;
            QualitySettings.streamingMipmapsMemoryBudget = Mathf.RoundToInt(
                Mathf.Min(graphicsBudget, 768f)
            );
            QualitySettings.streamingMipmapsMaxLevelReduction = 2;
            QualitySettings.streamingMipmapsMaxFileIORequests = 8;
            QualitySettings.anisotropicFiltering =
                AnisotropicFiltering.Enable;
            Texture.SetGlobalAnisotropicFilteringLimits(4, 4);
        }
    }
}

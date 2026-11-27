using System;
using Demo02.Core;
using UnityEngine;

namespace Demo02.WorldState
{
    public enum TimeLayer
    {
        Present,
        Past
    }

    public sealed class TimeLayerController : MonoBehaviour
    {
        [SerializeField] private TimeLayer currentLayer = TimeLayer.Present;

        public event Action<TimeLayer> LayerChanged;
        public TimeLayer CurrentLayer => currentLayer;
        public float LastChangedAt { get; private set; } = -10f;

        private void Start()
        {
            ApplyAtmosphere(currentLayer);
        }

        private void Update()
        {
            if (Time.timeScale > 0f
                && Demo02Input.ToggleTimePressed)
            {
                ToggleLayer();
            }
        }

        public void ToggleLayer()
        {
            SetLayer(currentLayer == TimeLayer.Present ? TimeLayer.Past : TimeLayer.Present);
        }

        public void SetLayer(TimeLayer layer)
        {
            if (currentLayer == layer)
            {
                return;
            }

            currentLayer = layer;
            LastChangedAt = Time.unscaledTime;
            ApplyAtmosphere(currentLayer);
            LayerChanged?.Invoke(currentLayer);
            Debug.Log($"Time layer changed to {currentLayer}.");
        }

        private static void ApplyAtmosphere(TimeLayer layer)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            if (layer == TimeLayer.Present)
            {
                RenderSettings.fogColor =
                    new Color(0.55f, 0.62f, 0.68f);
                RenderSettings.fogStartDistance = 48f;
                RenderSettings.fogEndDistance = 165f;
                RenderSettings.ambientLight =
                    new Color(0.42f, 0.46f, 0.52f);
            }
            else
            {
                RenderSettings.fogColor =
                    new Color(0.34f, 0.43f, 0.5f);
                RenderSettings.fogStartDistance = 34f;
                RenderSettings.fogEndDistance = 128f;
                RenderSettings.ambientLight =
                    new Color(0.34f, 0.4f, 0.48f);
            }
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.InputSystem;

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

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame)
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
            LayerChanged?.Invoke(currentLayer);
            Debug.Log($"Time layer changed to {currentLayer}.");
        }
    }
}

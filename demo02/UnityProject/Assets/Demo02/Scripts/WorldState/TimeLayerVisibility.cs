using UnityEngine;

namespace Demo02.WorldState
{
    public sealed class TimeLayerVisibility : MonoBehaviour
    {
        [SerializeField] private TimeLayer visibleLayer = TimeLayer.Past;

        private TimeLayerController controller;
        private Renderer[] renderers;
        private Collider[] colliders;

        public TimeLayer VisibleLayer => visibleLayer;

        private void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
            colliders = GetComponentsInChildren<Collider>(includeInactive: true);
        }

        private void Start()
        {
            controller = FindAnyObjectByType<TimeLayerController>();
            if (controller != null)
            {
                controller.LayerChanged += OnLayerChanged;
            }

            RefreshVisibility();
        }

        private void OnDestroy()
        {
            if (controller != null)
            {
                controller.LayerChanged -= OnLayerChanged;
            }
        }

        public void Configure(TimeLayer layer)
        {
            visibleLayer = layer;
        }

        private void OnLayerChanged(TimeLayer layer)
        {
            RefreshVisibility();
        }

        private void RefreshVisibility()
        {
            bool isVisible = controller != null && controller.CurrentLayer == visibleLayer;
            foreach (Renderer item in renderers)
            {
                item.enabled = isVisible;
            }

            foreach (Collider item in colliders)
            {
                item.enabled = isVisible;
            }
        }
    }
}

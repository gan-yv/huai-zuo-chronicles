using Demo02.Narrative;
using UnityEngine;

namespace Demo02.Interaction
{
    public sealed class MemoryEchoInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string echoId = "memory-echo";
        [SerializeField] private string echoTitle = "残留回声";
        [SerializeField, TextArea(2, 5)] private string echoText;

        private InvestigationState investigationState;
        private Renderer[] renderers;
        private Collider[] colliders;
        private bool isRevealed;

        public string EchoId => echoId;
        public string EchoTitle => echoTitle;
        public bool IsDiscovered =>
            investigationState != null && investigationState.HasEcho(echoId);
        public bool CanInteract => isRevealed;
        public string Prompt => IsDiscovered
            ? $"E  再次读取回声 · {echoTitle}"
            : $"E  读取残留回声 · {echoTitle}";

        private void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
            colliders = GetComponentsInChildren<Collider>(includeInactive: true);
            SetRevealed(false);
        }

        private void Start()
        {
            investigationState = Demo02.Core.GameServices.Resolve<InvestigationState>();
        }

        public void Configure(string id, string title, string text)
        {
            echoId = id;
            echoTitle = title;
            echoText = text;
        }

        public void SetRevealed(bool revealed)
        {
            isRevealed = revealed;
            foreach (Renderer item in renderers)
            {
                item.enabled = revealed;
            }

            foreach (Collider item in colliders)
            {
                item.enabled = revealed;
            }
        }

        public string Interact(PlayerInteractor interactor)
        {
            if (!isRevealed)
            {
                return "这道回声已经沉回时间层。";
            }

            investigationState?.DiscoverEcho(echoId);
            return echoText;
        }
    }
}

using UnityEngine;
using UnityEngine.InputSystem;
using Demo02.Core;
using Demo02.Narrative;
using Demo02.Player;
using Demo02.UI;

namespace Demo02.Interaction
{
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField, Min(0.5f)] private float interactionRadius = 2.8f;
        [SerializeField] private LayerMask interactionMask = ~0;
        [SerializeField, Min(0f)] private float messageDuration = 5f;

        private readonly Collider[] overlaps = new Collider[16];
        private IInteractable currentInteractable;
        private string statusMessage;
        private float statusMessageUntil;
        private Transform highlightedTransform;
        private Quaternion highlightedBaseRotation;

        private void Update()
        {
            if (Time.timeScale <= 0f
                || (ScreenFade.Instance != null && ScreenFade.Instance.IsTransitioning))
            {
                currentInteractable = null;
                RestoreHighlight();
                return;
            }

            if (DialogueController.Instance != null
                && (DialogueController.Instance.IsOpen
                    || DialogueController.Instance.LastClosedFrame == Time.frameCount))
            {
                currentInteractable = null;
                return;
            }

            currentInteractable = FindClosestInteractable();
            UpdateInteractionHighlight();

            bool interactPressed = Keyboard.current != null
                && Keyboard.current.eKey.wasPressedThisFrame;

            if (!interactPressed || currentInteractable == null || !currentInteractable.CanInteract)
            {
                return;
            }

            ReadyMadeCharacterAnimator characterAnimator =
                GetComponentInChildren<ReadyMadeCharacterAnimator>(true);
            if (currentInteractable is InventoryItemPickup)
            {
                characterAnimator?.PlayPickUp();
            }
            else
            {
                characterAnimator?.PlayInteract();
            }

            statusMessage = currentInteractable.Interact(this);
            statusMessageUntil = Time.time + messageDuration;
            GameAudio.PlayInteraction();
        }

        private void OnGUI()
        {
            if ((GameFlowController.Instance != null
                    && GameFlowController.Instance.IsGameEnding)
                || (DialogueController.Instance != null && DialogueController.Instance.IsOpen)
                || (ScreenFade.Instance != null && ScreenFade.Instance.IsTransitioning))
            {
                return;
            }

            if (currentInteractable != null && currentInteractable.CanInteract)
            {
                float scale = UiTheme.Scale;
                DrawCenteredLabel(
                    currentInteractable.Prompt,
                    Screen.height - 104f * scale,
                    15,
                    true
                );
            }

            if (!string.IsNullOrWhiteSpace(statusMessage) && Time.time < statusMessageUntil)
            {
                float scale = UiTheme.Scale;
                DrawCenteredLabel(
                    statusMessage,
                    Screen.height - 158f * scale,
                    12,
                    false
                );
            }
        }

        private IInteractable FindClosestInteractable()
        {
            int count = Physics.OverlapSphereNonAlloc(
                transform.position,
                interactionRadius,
                overlaps,
                interactionMask,
                QueryTriggerInteraction.Collide
            );

            IInteractable closest = null;
            float closestDistance = float.MaxValue;
            Vector3 origin = transform.position;

            for (int index = 0; index < count; index++)
            {
                IInteractable candidate = overlaps[index].GetComponentInParent<IInteractable>();
                if (candidate == null || !candidate.CanInteract)
                {
                    continue;
                }

                float distance = (overlaps[index].ClosestPoint(origin) - origin).sqrMagnitude;
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = candidate;
                }
            }

            return closest;
        }

        private void UpdateInteractionHighlight()
        {
            Transform nextTransform = (currentInteractable as Component)?.transform;
            if (nextTransform != highlightedTransform)
            {
                RestoreHighlight();
                highlightedTransform = nextTransform;
                if (highlightedTransform != null)
                {
                    highlightedBaseRotation = highlightedTransform.localRotation;
                }
            }

            if (highlightedTransform != null)
            {
                float angle = Mathf.Sin(Time.unscaledTime * 4f) * 3f;
                highlightedTransform.localRotation =
                    highlightedBaseRotation * Quaternion.Euler(0f, angle, 0f);
            }
        }

        private void RestoreHighlight()
        {
            if (highlightedTransform != null)
            {
                highlightedTransform.localRotation = highlightedBaseRotation;
            }

            highlightedTransform = null;
        }

        private void OnDisable()
        {
            RestoreHighlight();
        }

        private static void DrawCenteredLabel(
            string text,
            float y,
            int fontSize,
            bool isPrompt)
        {
            float scale = UiTheme.Scale;
            float width = Mathf.Min(
                (isPrompt ? 520f : 470f) * scale,
                Screen.width - 40f
            );
            float height = (isPrompt ? 44f : 38f) * scale;
            Rect rect = new(
                (Screen.width - width) * 0.5f,
                y,
                width,
                height
            );
            UiTheme.DrawRect(
                rect,
                isPrompt
                    ? new Color(0.025f, 0.033f, 0.045f, 0.97f)
                    : new Color(0.06f, 0.055f, 0.04f, 0.96f)
            );
            UiTheme.DrawBorder(
                rect,
                isPrompt
                    ? new Color(1f, 1f, 1f, 0.14f)
                    : new Color(0.88f, 0.7f, 0.38f, 0.35f)
            );
            UiTheme.DrawRect(
                new Rect(
                    rect.x,
                    rect.y,
                    3f * scale,
                    rect.height
                ),
                isPrompt ? UiTheme.Accent : new Color(0.72f, 0.58f, 0.3f)
            );
            GUI.Label(
                rect,
                text,
                UiTheme.Label(
                    fontSize,
                    TextAnchor.MiddleCenter,
                    bold: isPrompt
                )
            );
        }
    }
}

using UnityEngine;
using UnityEngine.InputSystem;
using Demo02.Narrative;
using Demo02.UI;

namespace Demo02.Interaction
{
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField, Min(0.5f)] private float interactionRadius = 2.5f;
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

            statusMessage = currentInteractable.Interact(this);
            statusMessageUntil = Time.time + messageDuration;
        }

        private void OnGUI()
        {
            if ((DialogueController.Instance != null && DialogueController.Instance.IsOpen)
                || (ScreenFade.Instance != null && ScreenFade.Instance.IsTransitioning))
            {
                return;
            }

            if (currentInteractable != null && currentInteractable.CanInteract)
            {
                DrawCenteredLabel(currentInteractable.Prompt, Screen.height - 96f, 24);
            }

            if (!string.IsNullOrWhiteSpace(statusMessage) && Time.time < statusMessageUntil)
            {
                DrawCenteredLabel(statusMessage, Screen.height - 152f, 18);
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

        private static void DrawCenteredLabel(string text, float y, int fontSize)
        {
            GUIStyle style = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = fontSize,
                wordWrap = true,
                padding = new RectOffset(16, 16, 8, 8)
            };

            const float width = 460f;
            Rect rect = new Rect((Screen.width - width) * 0.5f, y, width, 54f);
            GUI.Box(rect, text, style);
        }
    }
}

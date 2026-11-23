using UnityEngine;
using UnityEngine.InputSystem;

namespace Demo02.UI
{
    public sealed class TitleCharacterPresentation : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float yawRange = 7f;
        [SerializeField, Min(0f)] private float pitchRange = 2.5f;
        [SerializeField, Min(0f)] private float bobHeight = 0.018f;

        private Vector3 basePosition;
        private Quaternion baseRotation;

        private void Awake()
        {
            basePosition = transform.localPosition;
            baseRotation = transform.localRotation;
        }

        private void Update()
        {
            Vector2 pointer = Mouse.current != null
                ? Mouse.current.position.ReadValue()
                : new Vector2(
                    Screen.width * 0.5f,
                    Screen.height * 0.5f
                );
            float x = Mathf.Clamp(
                pointer.x / Mathf.Max(1f, Screen.width),
                0f,
                1f
            );
            float y = Mathf.Clamp(
                pointer.y / Mathf.Max(1f, Screen.height),
                0f,
                1f
            );
            float yaw = (x - 0.5f) * 2f * yawRange;
            float pitch = (0.5f - y) * pitchRange;
            float bob = Mathf.Sin(Time.unscaledTime * 1.35f)
                * bobHeight;
            transform.localPosition = basePosition + Vector3.up * bob;
            transform.localRotation =
                baseRotation * Quaternion.Euler(pitch, yaw, 0f);
        }
    }
}

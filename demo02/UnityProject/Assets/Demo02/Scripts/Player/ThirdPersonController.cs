using UnityEngine;
using UnityEngine.InputSystem;
using Demo02.Core;

namespace Demo02.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class ThirdPersonController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField, Min(0f)] private float walkSpeed = 4f;
        [SerializeField, Min(0f)] private float runSpeed = 7f;
        [SerializeField, Min(0f)] private float jumpHeight = 1.2f;
        [SerializeField] private float gravity = -24f;
        [SerializeField, Min(0f)] private float rotationSpeed = 12f;

        [Header("Dodge")]
        [SerializeField, Min(0f)] private float dodgeSpeed = 9f;
        [SerializeField, Min(0.05f)] private float dodgeDuration = 0.25f;
        [SerializeField, Min(0f)] private float dodgeCooldown = 0.65f;

        [Header("Camera")]
        [SerializeField, Min(1f)] private float cameraDistance = 6f;
        [SerializeField, Min(0f)] private float cameraHeight = 1.5f;
        [SerializeField, Min(0f)] private float mouseSensitivity = 0.12f;
        [SerializeField] private float minPitch = -20f;
        [SerializeField] private float maxPitch = 65f;
        [SerializeField, Min(0f)] private float cameraCollisionRadius = 0.2f;

        private CharacterController motor;
        private Transform cameraTransform;
        private readonly RaycastHit[] cameraHits = new RaycastHit[12];
        private float cameraYaw;
        private float cameraPitch = 18f;
        private float verticalVelocity;
        private float dodgeRemaining;
        private float nextDodgeTime;
        private Vector3 dodgeDirection;

        public bool IsInvulnerable => dodgeRemaining > 0f;

        private void Awake()
        {
            motor = GetComponent<CharacterController>();
        }

        private void Start()
        {
            Camera sceneCamera = Camera.main;
            if (sceneCamera != null)
            {
                cameraTransform = sceneCamera.transform;
                cameraYaw = transform.eulerAngles.y;
            }
            else
            {
                Debug.LogWarning("ThirdPersonController requires a camera tagged MainCamera.");
            }
        }

        private void Update()
        {
            if (Time.timeScale <= 0f)
            {
                return;
            }

            UpdateCameraInput();
            UpdateMovement();
        }

        private void LateUpdate()
        {
            UpdateCameraPosition();
        }

        private void OnDisable()
        {
            dodgeRemaining = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void UpdateCameraInput()
        {
            Mouse mouse = Mouse.current;
            bool isRotating = mouse != null && mouse.rightButton.isPressed;

            Cursor.lockState = isRotating ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !isRotating;

            if (!isRotating)
            {
                return;
            }

            Vector2 delta = mouse.delta.ReadValue();
            float sensitivity = mouseSensitivity * GameSettings.CameraSensitivity;
            cameraYaw += delta.x * sensitivity;
            cameraPitch = Mathf.Clamp(cameraPitch - delta.y * sensitivity, minPitch, maxPitch);
        }

        private void UpdateMovement()
        {
            Vector2 input = ReadMoveInput();
            Vector3 moveDirection = GetCameraRelativeDirection(input);
            bool isRunning = Keyboard.current != null
                && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);

            float speed = isRunning ? runSpeed : walkSpeed;
            if (moveDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    1f - Mathf.Exp(-rotationSpeed * Time.deltaTime)
                );
            }

            if (motor.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            bool wantsToJump = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
            if (wantsToJump && motor.isGrounded)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            verticalVelocity += gravity * Time.deltaTime;

            bool dodgePressed = Keyboard.current != null
                && (Keyboard.current.leftCtrlKey.wasPressedThisFrame
                    || Keyboard.current.rightCtrlKey.wasPressedThisFrame);
            if (dodgePressed && motor.isGrounded && Time.time >= nextDodgeTime)
            {
                dodgeDirection = moveDirection.sqrMagnitude > 0.001f
                    ? moveDirection
                    : -transform.forward;
                dodgeRemaining = dodgeDuration;
                nextDodgeTime = Time.time + dodgeDuration + dodgeCooldown;
                transform.rotation = Quaternion.LookRotation(dodgeDirection, Vector3.up);
            }

            if (dodgeRemaining > 0f)
            {
                dodgeRemaining = Mathf.Max(0f, dodgeRemaining - Time.deltaTime);
                motor.Move(
                    dodgeDirection * dodgeSpeed * Time.deltaTime
                    + Vector3.up * verticalVelocity * Time.deltaTime
                );
                return;
            }

            Vector3 velocity = moveDirection * speed + Vector3.up * verticalVelocity;
            motor.Move(velocity * Time.deltaTime);
        }

        private static Vector2 ReadMoveInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return Vector2.zero;
            }

            Vector2 input = Vector2.zero;
            if (keyboard.wKey.isPressed) input.y += 1f;
            if (keyboard.sKey.isPressed) input.y -= 1f;
            if (keyboard.dKey.isPressed) input.x += 1f;
            if (keyboard.aKey.isPressed) input.x -= 1f;

            return input.sqrMagnitude > 1f ? input.normalized : input;
        }

        private Vector3 GetCameraRelativeDirection(Vector2 input)
        {
            if (input.sqrMagnitude < 0.001f)
            {
                return Vector3.zero;
            }

            Vector3 forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
            Vector3 right = cameraTransform != null ? cameraTransform.right : Vector3.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            return (forward * input.y + right * input.x).normalized;
        }

        private void UpdateCameraPosition()
        {
            if (cameraTransform == null)
            {
                return;
            }

            Vector3 focusPoint = transform.position + Vector3.up * cameraHeight;
            Quaternion orbitRotation = Quaternion.Euler(cameraPitch, cameraYaw, 0f);
            Vector3 offset = orbitRotation * new Vector3(0f, 0f, -cameraDistance);
            Vector3 direction = offset.normalized;
            float resolvedDistance = cameraDistance;
            int hitCount = Physics.SphereCastNonAlloc(
                focusPoint,
                cameraCollisionRadius,
                direction,
                cameraHits,
                cameraDistance,
                ~0,
                QueryTriggerInteraction.Ignore
            );

            for (int index = 0; index < hitCount; index++)
            {
                Collider hitCollider = cameraHits[index].collider;
                if (hitCollider == null || hitCollider.transform.IsChildOf(transform))
                {
                    continue;
                }

                resolvedDistance = Mathf.Min(
                    resolvedDistance,
                    Mathf.Max(0.5f, cameraHits[index].distance - 0.15f)
                );
            }

            cameraTransform.position = focusPoint + direction * resolvedDistance;
            cameraTransform.LookAt(focusPoint + Vector3.up * 0.2f, Vector3.up);
        }
    }
}

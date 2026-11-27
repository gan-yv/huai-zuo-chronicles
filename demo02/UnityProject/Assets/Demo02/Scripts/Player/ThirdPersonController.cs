using UnityEngine;
using Demo02.Core;

namespace Demo02.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class ThirdPersonController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField, Min(0f)] private float walkSpeed = 4.3f;
        [SerializeField, Min(0f)] private float runSpeed = 7.2f;
        [SerializeField, Min(0f)] private float acceleration = 18f;
        [SerializeField, Min(0f)] private float deceleration = 26f;
        [SerializeField, Min(0f)] private float jumpHeight = 1.2f;
        [SerializeField] private float gravity = -24f;
        [SerializeField, Min(0f)] private float rotationSpeed = 14f;

        [Header("Dodge")]
        [SerializeField, Min(0f)] private float dodgeSpeed = 9f;
        [SerializeField, Min(0.05f)] private float dodgeDuration = 0.25f;
        [SerializeField, Min(0f)] private float dodgeCooldown = 0.65f;

        [Header("Exploration")]
        [SerializeField, Min(0f)] private float glideMoveSpeed = 5.8f;
        [SerializeField, Min(0f)] private float glideFallSpeed = 2.1f;
        [SerializeField, Min(0f)] private float glideGravity = 8f;

        [Header("Camera")]
        [SerializeField, Min(1f)] private float cameraDistance = 5.3f;
        [SerializeField, Min(0f)] private float cameraHeight = 1.45f;
        [SerializeField, Min(0f)] private float mouseSensitivity = 0.12f;
        [SerializeField] private float minPitch = -20f;
        [SerializeField] private float maxPitch = 65f;
        [SerializeField, Min(0f)] private float cameraCollisionRadius = 0.2f;
        [SerializeField, Min(1f)] private float cameraReturnSpeed = 12f;

        private CharacterController motor;
        private CharacterRuntime characterRuntime;
        private GameObject gliderVisual;
        private Transform cameraTransform;
        private readonly RaycastHit[] cameraHits = new RaycastHit[12];
        private float cameraYaw;
        private float cameraPitch = 18f;
        private float verticalVelocity;
        private float dodgeRemaining;
        private float nextDodgeTime;
        private Vector3 dodgeDirection;
        private Vector3 planarVelocity;
        private float smoothedCameraDistance;
        private bool isGliding;
        private bool isSprinting;
        private float shakeStrength;
        private float shakeRemaining;

        public bool IsInvulnerable => dodgeRemaining > 0f;
        public bool IsGliding => isGliding;
        public bool IsSprinting => isSprinting;
        public bool IsGrounded => motor == null || motor.isGrounded;
        public float VerticalSpeed => verticalVelocity;

        private void Awake()
        {
            motor = GetComponent<CharacterController>();
            characterRuntime = GetComponent<CharacterRuntime>();
            Transform glider = transform.Find("ExplorationGlider");
            gliderVisual = glider != null ? glider.gameObject : null;
            SetGliding(false);
        }

        private void Start()
        {
            Camera sceneCamera = Camera.main;
            if (sceneCamera != null)
            {
                cameraTransform = sceneCamera.transform;
                cameraYaw = transform.eulerAngles.y;
                smoothedCameraDistance = cameraDistance;
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
            ApplyCameraShake();
        }

        private void OnDisable()
        {
            dodgeRemaining = 0f;
            SetGliding(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void UpdateCameraInput()
        {
            bool isRotating = Demo02Input.CameraRotateHeld;

            Cursor.lockState = isRotating ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !isRotating;

            if (!isRotating)
            {
                return;
            }

            Vector2 delta = Demo02Input.Look;
            float sensitivity = mouseSensitivity * GameSettings.CameraSensitivity;
            cameraYaw += delta.x * sensitivity;
            cameraPitch = Mathf.Clamp(cameraPitch - delta.y * sensitivity, minPitch, maxPitch);
        }

        private void UpdateMovement()
        {
            Vector2 input = ReadMoveInput();
            Vector3 moveDirection = GetCameraRelativeDirection(input);
            bool sprintRequested = Demo02Input.SprintHeld;
            sprintRequested &= moveDirection.sqrMagnitude > 0.001f;

            bool wantsToJump = Demo02Input.JumpPressed;
            if (wantsToJump && motor.isGrounded)
            {
                isSprinting = false;
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
            else if (wantsToJump && !motor.isGrounded)
            {
                if (isGliding)
                {
                    SetGliding(false);
                }
                else if (characterRuntime != null
                         && characterRuntime.CanUseStamina)
                {
                    SetGliding(true);
                }
            }

            if (motor.isGrounded)
            {
                SetGliding(false);
            }

            isSprinting = sprintRequested
                && motor.isGrounded
                && !isGliding
                && characterRuntime != null
                && characterRuntime.CanUseStamina;
            if (isSprinting
                && !characterRuntime.DrainStamina(
                    characterRuntime.Definition != null
                        ? characterRuntime.Definition.SprintCostPerSecond
                            * Time.deltaTime
                        : 18f * Time.deltaTime
                ))
            {
                isSprinting = false;
            }

            float speed = isSprinting
                ? runSpeed
                : isGliding
                    ? glideMoveSpeed
                    : walkSpeed;
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

            if (isGliding)
            {
                verticalVelocity = Mathf.MoveTowards(
                    verticalVelocity,
                    -glideFallSpeed,
                    glideGravity * Time.deltaTime
                );
                if (!characterRuntime.DrainStamina(
                        characterRuntime.Definition != null
                            ? characterRuntime.Definition.GlideCostPerSecond
                                * Time.deltaTime
                            : 14f * Time.deltaTime
                    ))
                {
                    SetGliding(false);
                }
            }
            else
            {
                verticalVelocity += gravity * Time.deltaTime;
            }

            bool dodgePressed = Demo02Input.DodgePressed;
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

            Vector3 targetVelocity = moveDirection * speed;
            float velocityChangeRate = moveDirection.sqrMagnitude > 0.001f
                ? acceleration
                : deceleration;
            planarVelocity = Vector3.MoveTowards(
                planarVelocity,
                targetVelocity,
                velocityChangeRate * Time.deltaTime
            );
            Vector3 velocity = planarVelocity + Vector3.up * verticalVelocity;
            motor.Move(velocity * Time.deltaTime);

            if (motor.isGrounded && !isSprinting && !isGliding)
            {
                characterRuntime?.RecoverStamina(Time.deltaTime);
            }
        }

        private void SetGliding(bool gliding)
        {
            if (isGliding == gliding)
            {
                if (gliderVisual != null)
                {
                    gliderVisual.SetActive(gliding);
                }
                return;
            }

            isGliding = gliding;
            if (gliderVisual != null)
            {
                gliderVisual.SetActive(gliding);
            }
            if (gliding)
            {
                isSprinting = false;
                verticalVelocity = Mathf.Min(verticalVelocity, -0.35f);
            }
        }

        public void ForceGlideForPreview()
        {
            if (characterRuntime == null || !characterRuntime.CanUseStamina)
            {
                return;
            }

            verticalVelocity = -glideFallSpeed;
            SetGliding(true);
        }

        public void AddCameraShake(float strength, float duration)
        {
            float motionScale = GameSettings.ReduceMotion ? 0.28f : 1f;
            shakeStrength = Mathf.Max(shakeStrength, strength * motionScale);
            shakeRemaining = Mathf.Max(shakeRemaining, duration);
        }

        public void ApplyWindLift(float verticalSpeed)
        {
            verticalVelocity = Mathf.Max(verticalVelocity, verticalSpeed);
        }

        private void ApplyCameraShake()
        {
            if (cameraTransform == null || shakeRemaining <= 0f)
            {
                return;
            }

            shakeRemaining = Mathf.Max(
                0f,
                shakeRemaining - Time.unscaledDeltaTime
            );
            float strength = shakeStrength * Mathf.Clamp01(
                shakeRemaining * 5f
            );
            cameraTransform.position += Random.insideUnitSphere * strength;
            if (shakeRemaining <= 0f)
            {
                shakeStrength = 0f;
            }
        }

        private static Vector2 ReadMoveInput()
        {
            Vector2 input = Demo02Input.Move;
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

            if (resolvedDistance < smoothedCameraDistance)
            {
                smoothedCameraDistance = resolvedDistance;
            }
            else
            {
                smoothedCameraDistance = Mathf.Lerp(
                    smoothedCameraDistance,
                    resolvedDistance,
                    1f - Mathf.Exp(-cameraReturnSpeed * Time.deltaTime)
                );
            }

            cameraTransform.position =
                focusPoint + direction * smoothedCameraDistance;
            cameraTransform.LookAt(focusPoint + Vector3.up * 0.2f, Vector3.up);
        }
    }
}

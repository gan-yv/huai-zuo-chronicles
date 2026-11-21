using UnityEngine;

namespace Demo02.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class CompanionFollower : MonoBehaviour
    {
        [SerializeField] private Transform followTarget;
        [SerializeField, Min(0.5f)] private float followDistance = 2.2f;
        [SerializeField, Min(0f)] private float moveSpeed = 5f;
        [SerializeField] private float gravity = -24f;

        private CharacterController controller;
        private float verticalVelocity;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        public void Configure(Transform target)
        {
            followTarget = target;
        }

        private void LateUpdate()
        {
            if (followTarget == null)
            {
                return;
            }

            Vector3 targetPosition = followTarget.position - followTarget.forward * followDistance;
            targetPosition.y = transform.position.y;
            Vector3 direction = targetPosition - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.04f)
            {
                Vector3 move = direction.normalized * moveSpeed * Time.deltaTime;
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(direction.normalized, Vector3.up),
                    1f - Mathf.Exp(-10f * Time.deltaTime)
                );
                controller.Move(move);
            }

            if (controller.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            verticalVelocity += gravity * Time.deltaTime;
            controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);
        }
    }
}

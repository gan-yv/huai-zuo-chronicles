using Demo02.Player;
using UnityEngine;

namespace Demo02.Combat
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class EnemyAI : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float moveSpeed = 2.2f;
        [SerializeField, Min(0f)] private float aggroDistance = 12f;
        [SerializeField, Min(0f)] private float attackDistance = 1.7f;
        [SerializeField, Min(0f)] private float attackDamage = 10f;
        [SerializeField, Min(0.1f)] private float attackCooldown = 1.2f;
        [SerializeField] private float gravity = -24f;

        private CharacterController controller;
        private Combatant self;
        private PlayerPartyController party;
        private float verticalVelocity;
        private float nextAttackTime;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            self = GetComponent<Combatant>();
        }

        private void Start()
        {
            party = FindAnyObjectByType<PlayerPartyController>();
        }

        private void Update()
        {
            if (self == null || !self.IsAlive)
            {
                gameObject.SetActive(false);
                return;
            }

            Transform target = party != null ? party.ActiveCharacter : null;
            if (target == null)
            {
                ApplyGravity();
                return;
            }

            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;

            if (distance <= aggroDistance && distance > attackDistance)
            {
                Vector3 direction = toTarget.normalized;
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(direction, Vector3.up),
                    1f - Mathf.Exp(-10f * Time.deltaTime)
                );
                controller.Move((direction * moveSpeed + Vector3.up * verticalVelocity) * Time.deltaTime);
            }
            else
            {
                ApplyGravity();
            }

            if (distance <= attackDistance && Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + attackCooldown;
                Combatant targetHealth = target.GetComponent<Combatant>();
                targetHealth?.TakeDamage(attackDamage);
            }
        }

        private void ApplyGravity()
        {
            if (controller.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            verticalVelocity += gravity * Time.deltaTime;
            controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);
        }
    }
}

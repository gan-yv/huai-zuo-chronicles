using Demo02.Player;
using Demo02.Core;
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
        [SerializeField, Min(0.05f)] private float attackWindup = 0.4f;
        [SerializeField] private float gravity = -24f;

        private CharacterController controller;
        private Combatant self;
        private PlayerPartyController party;
        private float verticalVelocity;
        private float nextAttackTime;
        private float attackWindupRemaining;
        private bool isWindingUp;
        private ColorTint colorTint;
        private bool deathStarted;
        private float deathVisibleUntil;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            self = GetComponent<Combatant>();
            colorTint = GetComponent<ColorTint>();
        }

        private void Start()
        {
            party = FindAnyObjectByType<PlayerPartyController>();
        }

        private void Update()
        {
            if (self == null)
            {
                return;
            }
            if (!self.IsAlive)
            {
                if (!deathStarted)
                {
                    deathStarted = true;
                    deathVisibleUntil = Time.time + 2.6f;
                    colorTint?.Configure(new Color(0.34f, 0.12f, 0.1f));
                    GetComponentInChildren<ReadyMadeCharacterAnimator>()?
                        .PlayDeath();
                    if (controller != null)
                    {
                        controller.enabled = false;
                    }
                }
                if (Time.time >= deathVisibleUntil)
                {
                    gameObject.SetActive(false);
                }
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

            if (distance > aggroDistance || distance > attackDistance * 1.15f)
            {
                CancelAttackWindup();
            }

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

            if (distance <= attackDistance && Time.time >= nextAttackTime && !isWindingUp)
            {
                BeginAttackWindup();
            }

            if (isWindingUp)
            {
                attackWindupRemaining -= Time.deltaTime;
                if (attackWindupRemaining <= 0f)
                {
                    ResolveAttack(target);
                }
            }
        }

        private void BeginAttackWindup()
        {
            isWindingUp = true;
            attackWindupRemaining = attackWindup;
            colorTint?.Configure(new Color(1f, 0.68f, 0.16f));
            GetComponentInChildren<ReadyMadeCharacterAnimator>()?.PlayAttack();
        }

        private void ResolveAttack(Transform target)
        {
            nextAttackTime = Time.time + attackCooldown;
            isWindingUp = false;
            colorTint?.Configure(new Color(0.68f, 0.16f, 0.16f));

            ThirdPersonController targetController = target.GetComponent<ThirdPersonController>();
            if (targetController != null && targetController.IsInvulnerable)
            {
                Debug.Log("Enemy attack dodged.");
                return;
            }

            Combatant targetHealth = target.GetComponent<Combatant>();
            targetHealth?.TakeDamage(attackDamage);
            if (targetHealth != null)
            {
                GameAudio.PlayPlayerHurt();
            }
        }

        private void CancelAttackWindup()
        {
            if (!isWindingUp)
            {
                return;
            }

            isWindingUp = false;
            attackWindupRemaining = 0f;
            colorTint?.Configure(new Color(0.68f, 0.16f, 0.16f));
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

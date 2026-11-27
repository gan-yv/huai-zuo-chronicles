using Demo02.Combat;
using Demo02.Core;
using UnityEngine;

namespace Demo02.Player
{
    [DisallowMultipleComponent]
    public sealed class ReadyMadeCharacterAnimator : MonoBehaviour
    {
        private static readonly int SpeedParameter =
            Animator.StringToHash("Speed");
        private static readonly int AttackParameter =
            Animator.StringToHash("Attack");
        private static readonly int InteractParameter =
            Animator.StringToHash("Interact");
        private static readonly int PickUpParameter =
            Animator.StringToHash("PickUp");
        private static readonly int HitParameter =
            Animator.StringToHash("Hit");
        private static readonly int DieParameter =
            Animator.StringToHash("Die");
        private static readonly int JumpParameter =
            Animator.StringToHash("Jump");
        private static readonly int TalkingParameter =
            Animator.StringToHash("Talking");
        private static readonly int GroundedParameter =
            Animator.StringToHash("Grounded");
        private static readonly int VerticalSpeedParameter =
            Animator.StringToHash("VerticalSpeed");
        private static readonly int AlertParameter =
            Animator.StringToHash("Alert");

        [SerializeField, Min(0.1f)] private float fullSpeed = 7.2f;

        private Animator animator;
        private Combatant combatant;
        private ThirdPersonController thirdPersonController;
        private Vector3 lastPosition;
        private bool hasSpeed;
        private bool hasAttack;
        private bool hasInteract;
        private bool hasPickUp;
        private bool hasHit;
        private bool hasDie;
        private bool hasJump;
        private bool hasTalking;
        private bool hasGrounded;
        private bool hasVerticalSpeed;
        private bool hasAlert;
        private bool deathPlayed;
        private float nextJumpTime;
        private float previousHealth;
        private bool subscribedToHealth;

        private void Awake()
        {
            CacheComponents();
        }

        private void OnEnable()
        {
            CacheComponents();
            lastPosition = transform.position;
            deathPlayed = false;
            if (animator != null && hasTalking)
            {
                animator.SetBool(TalkingParameter, false);
            }
            if (animator != null && hasAlert)
            {
                animator.ResetTrigger(AlertParameter);
            }
            if (combatant != null)
            {
                combatant.HealthChanged -= OnHealthChanged;
                combatant.HealthChanged += OnHealthChanged;
                subscribedToHealth = true;
                previousHealth = combatant.CurrentHealth;
            }
        }

        private void OnDisable()
        {
            if (combatant != null && subscribedToHealth)
            {
                combatant.HealthChanged -= OnHealthChanged;
            }
            subscribedToHealth = false;
            if (animator != null && hasTalking)
            {
                animator.SetBool(TalkingParameter, false);
            }
        }

        private void Update()
        {
            if (animator == null || Time.deltaTime <= 0f)
            {
                return;
            }

            if (combatant != null && !combatant.IsAlive)
            {
                if (!deathPlayed && hasDie)
                {
                    deathPlayed = true;
                    animator.SetTrigger(DieParameter);
                }
                return;
            }

            if (animator != null && hasGrounded)
            {
                animator.SetBool(
                    GroundedParameter,
                    thirdPersonController == null ||
                    thirdPersonController.IsGrounded
                );
            }
            if (animator != null && hasVerticalSpeed)
            {
                animator.SetFloat(
                    VerticalSpeedParameter,
                    thirdPersonController != null
                        ? thirdPersonController.VerticalSpeed
                        : 0f
                );
            }

            Vector3 delta = transform.position - lastPosition;
            if (!hasGrounded
                && delta.y > 0.04f
                && Time.time >= nextJumpTime)
            {
                nextJumpTime = Time.time + 0.5f;
                Trigger(JumpParameter, hasJump);
            }
            delta.y = 0f;
            lastPosition = transform.position;
            if (delta.sqrMagnitude > 9f)
            {
                if (hasSpeed)
                {
                    animator.SetFloat(SpeedParameter, 0f);
                }
                return;
            }

            float speed = delta.magnitude / Time.deltaTime;
            if (hasSpeed)
            {
                animator.SetFloat(
                    SpeedParameter,
                    Mathf.Clamp01(speed / fullSpeed)
                );
            }
        }

        public void PlayAttack()
        {
            Trigger(AttackParameter, hasAttack);
        }

        public void PlayInteract()
        {
            Trigger(InteractParameter, hasInteract);
        }

        public void PlayPickUp()
        {
            Trigger(PickUpParameter, hasPickUp);
        }

        public void PlayHit()
        {
            Trigger(HitParameter, hasHit);
        }

        public void PlayDeath()
        {
            if (deathPlayed)
            {
                return;
            }
            deathPlayed = true;
            Trigger(DieParameter, hasDie);
        }

        public void SetTalking(bool talking)
        {
            if (hasTalking && animator != null)
            {
                animator.SetBool(TalkingParameter, talking);
            }
        }

        public void PlayAlert()
        {
            Trigger(AlertParameter, hasAlert);
        }

        private void CacheComponents()
        {
            animator ??= GetComponentInChildren<Animator>(true);
            combatant ??= GetComponentInParent<Combatant>();
            thirdPersonController ??=
                GetComponentInParent<ThirdPersonController>();
            if (animator == null)
            {
                return;
            }

            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                switch (parameter.name)
                {
                    case "Speed":
                        hasSpeed = true;
                        break;
                    case "Attack":
                        hasAttack = true;
                        break;
                    case "Interact":
                        hasInteract = true;
                        break;
                    case "PickUp":
                        hasPickUp = true;
                        break;
                    case "Hit":
                        hasHit = true;
                        break;
                    case "Die":
                        hasDie = true;
                        break;
                    case "Jump":
                        hasJump = true;
                        break;
                    case "Talking":
                        hasTalking = true;
                        break;
                    case "Grounded":
                        hasGrounded = true;
                        break;
                    case "VerticalSpeed":
                        hasVerticalSpeed = true;
                        break;
                    case "Alert":
                        hasAlert = true;
                        break;
                }
            }
        }

        private void OnHealthChanged(Combatant changed)
        {
            if (changed != combatant)
            {
                return;
            }

            float current = changed.CurrentHealth;
            if (current < previousHealth)
            {
                if (changed.IsAlive)
                {
                    PlayHit();
                }
                else
                {
                    PlayDeath();
                }
            }
            previousHealth = current;
        }

        private void Trigger(int parameter, bool available)
        {
            if (!available || animator == null || !isActiveAndEnabled)
            {
                return;
            }

            animator.SetTrigger(parameter);
        }
    }
}

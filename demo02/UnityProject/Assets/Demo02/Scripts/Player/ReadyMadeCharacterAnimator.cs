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
        private static readonly int DieParameter =
            Animator.StringToHash("Die");
        private static readonly int JumpParameter =
            Animator.StringToHash("Jump");

        [SerializeField, Min(0.1f)] private float fullSpeed = 7.2f;

        private Animator animator;
        private Combatant combatant;
        private Vector3 lastPosition;
        private bool hasSpeed;
        private bool hasAttack;
        private bool hasInteract;
        private bool hasPickUp;
        private bool hasDie;
        private bool hasJump;
        private bool deathPlayed;
        private float nextJumpTime;

        private void Awake()
        {
            CacheComponents();
        }

        private void OnEnable()
        {
            CacheComponents();
            lastPosition = transform.position;
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

            Vector3 delta = transform.position - lastPosition;
            if (delta.y > 0.04f && Time.time >= nextJumpTime)
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

        private void CacheComponents()
        {
            animator ??= GetComponentInChildren<Animator>(true);
            combatant ??= GetComponentInParent<Combatant>();
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
                    case "Die":
                        hasDie = true;
                        break;
                    case "Jump":
                        hasJump = true;
                        break;
                }
            }
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

using Demo02.Player;
using Demo02.Core;
using UnityEngine;

namespace Demo02.Combat
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class EnemyAI : MonoBehaviour
    {
        [SerializeField] private EnemyArchetype archetype =
            EnemyArchetype.Chaser;
        [SerializeField, Min(0f)] private float moveSpeed = 2.2f;
        [SerializeField, Min(0f)] private float aggroDistance = 12f;
        [SerializeField, Min(0f)] private float attackDistance = 1.7f;
        [SerializeField, Min(0f)] private float attackDamage = 10f;
        [SerializeField, Min(0.1f)] private float attackCooldown = 1.2f;
        [SerializeField, Min(0.05f)] private float attackWindup = 0.4f;
        [SerializeField, Min(2)] private int heavyAttackEvery = 3;
        [SerializeField, Min(1f)] private float heavyDamageMultiplier = 1.8f;
        [SerializeField, Min(1f)] private float heavyWindupMultiplier = 1.8f;
        [SerializeField, Min(1f)] private float heavyRecoveryMultiplier = 1.35f;
        [SerializeField, Min(2f)] private float rangedPreferredDistance = 7f;
        [SerializeField, Min(1f)] private float rangedMinimumDistance = 4.5f;
        [SerializeField, Min(2f)] private float rangedMaximumDistance = 9.5f;
        [SerializeField, Min(1f)] private float projectileSpeed = 12f;
        [SerializeField, Min(0f)] private float shieldDamageMultiplier = 0.28f;
        [SerializeField] private float gravity = -24f;

        private CharacterController controller;
        private Combatant self;
        private PlayerPartyController party;
        private float verticalVelocity;
        private float nextAttackTime;
        private float attackWindupRemaining;
        private float currentAttackWindup;
        private float staggerRemaining;
        private Vector3 knockbackVelocity;
        private bool isWindingUp;
        private ColorTint colorTint;
        private bool deathStarted;
        private float deathVisibleUntil;
        private bool externalActionLock;
        private bool isHeavyAttack;
        private EnemyAttackCadence attackCadence;

        public EnemyArchetype Archetype => archetype;
        public bool IsRanged => archetype == EnemyArchetype.Ranged;
        public bool IsGuarding =>
            archetype == EnemyArchetype.Shield && !isWindingUp;
        public bool IsWindingUp => isWindingUp;
        public bool IsHeavyAttack => isHeavyAttack;
        public float AttackWindupNormalized =>
            isWindingUp
                ? Mathf.Clamp01(
                    1f - attackWindupRemaining
                        / Mathf.Max(0.01f, currentAttackWindup)
                )
                : 0f;

        public void ConfigureCombatProfile(
            float newMoveSpeed,
            float newAttackDamage,
            float newAttackCooldown,
            float newAttackWindup)
        {
            moveSpeed = Mathf.Max(0f, newMoveSpeed);
            attackDamage = Mathf.Max(0f, newAttackDamage);
            attackCooldown = Mathf.Max(0.1f, newAttackCooldown);
            attackWindup = Mathf.Max(0.05f, newAttackWindup);
        }

        public void ConfigureArchetype(EnemyArchetype newArchetype)
        {
            archetype = newArchetype;
            if (archetype == EnemyArchetype.Shield)
            {
                moveSpeed = Mathf.Min(moveSpeed, 1.75f);
                attackDistance = Mathf.Max(attackDistance, 1.9f);
            }
            else if (archetype == EnemyArchetype.Ranged)
            {
                moveSpeed = Mathf.Max(moveSpeed, 2.4f);
            }
            CancelAttackWindup();
        }

        public void SetActionLocked(bool locked)
        {
            externalActionLock = locked;
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            self = GetComponent<Combatant>();
            colorTint = GetComponent<ColorTint>();
            attackCadence = new EnemyAttackCadence(heavyAttackEvery);
        }

        private void Start()
        {
            party = Demo02.Core.GameServices.Resolve<PlayerPartyController>();
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
                    attackCadence.Reset();
                }
                if (Time.time >= deathVisibleUntil)
                {
                    gameObject.SetActive(false);
                }
                return;
            }

            if (externalActionLock)
            {
                return;
            }

            if (staggerRemaining > 0f)
            {
                staggerRemaining = Mathf.Max(
                    0f,
                    staggerRemaining - Time.deltaTime
                );
                if (controller.enabled)
                {
                    controller.Move(
                        (knockbackVelocity + Vector3.down * 2f) *
                        Time.deltaTime
                    );
                }
                knockbackVelocity = Vector3.MoveTowards(
                    knockbackVelocity,
                    Vector3.zero,
                    12f * Time.deltaTime
                );
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
            float engagementDistance = IsRanged
                ? rangedMaximumDistance
                : attackDistance;

            if (distance > aggroDistance
                || distance > engagementDistance * 1.25f)
            {
                CancelAttackWindup();
            }

            if (distance <= aggroDistance)
            {
                Vector3 direction = toTarget.sqrMagnitude > 0.001f
                    ? toTarget.normalized
                    : transform.forward;
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(direction, Vector3.up),
                    1f - Mathf.Exp(-10f * Time.deltaTime)
                );
                MoveForArchetype(direction, distance);
            }
            else
            {
                ApplyGravity();
            }

            bool inAttackRange = IsRanged
                ? distance <= rangedMaximumDistance
                    && distance >= rangedMinimumDistance
                : distance <= attackDistance;
            if (inAttackRange
                && Time.time >= nextAttackTime
                && !isWindingUp)
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
            isHeavyAttack = attackCadence.NextIsHeavy;
            currentAttackWindup = attackWindup
                * (isHeavyAttack ? heavyWindupMultiplier : 1f);
            attackWindupRemaining = currentAttackWindup;
            GameAudio.PlayAttackSwing();
            colorTint?.Configure(
                IsRanged
                    ? new Color(0.9f, 0.42f, 0.12f)
                    : isHeavyAttack
                        ? new Color(1f, 0.36f, 0.12f)
                        : archetype == EnemyArchetype.Shield
                            ? new Color(0.3f, 0.68f, 0.92f)
                            : new Color(1f, 0.68f, 0.16f)
            );
            GetComponentInChildren<ReadyMadeCharacterAnimator>()?.PlayAttack();
        }

        private void ResolveAttack(Transform target)
        {
            bool heavy = isHeavyAttack;
            attackCadence.CompleteAttack();
            nextAttackTime = Time.time
                + attackCooldown
                    * (heavy ? heavyRecoveryMultiplier : 1f);
            isWindingUp = false;
            isHeavyAttack = false;
            colorTint?.Configure(new Color(0.68f, 0.16f, 0.16f));

            if (IsRanged)
            {
                ResolveRangedAttack(target, heavy);
                return;
            }

            ThirdPersonController targetController = target.GetComponent<ThirdPersonController>();
            if (targetController != null && targetController.IsInvulnerable)
            {
                target.GetComponent<PlayerCombat>()?.RewardPerfectDodge();
                GameAudio.PlayConfirm();
                Debug.Log("Enemy attack dodged.");
                return;
            }

            Combatant targetHealth = target.GetComponent<Combatant>();
            float previousHealth = targetHealth != null
                ? targetHealth.CurrentHealth
                : 0f;
            targetHealth?.TakeDamage(
                attackDamage * (heavy ? heavyDamageMultiplier : 1f)
            );
            if (targetHealth != null)
            {
                CombatFeedback.ReportDamage(
                    target.position + Vector3.up * 1.8f,
                    previousHealth - targetHealth.CurrentHealth,
                    isPlayerDamage: true
                );
                CombatFeedback.ReportPlayerHurt();
                targetController?.AddCameraShake(
                    heavy ? 0.13f : 0.05f,
                    heavy ? 0.14f : 0.08f
                );
                GameAudio.PlayPlayerHurt();
            }
        }

        private void ResolveRangedAttack(Transform target, bool heavy)
        {
            Vector3 origin = transform.position
                + Vector3.up * 1.25f
                + transform.forward * 0.55f;
            Vector3 aimPoint = target.position
                + Vector3.up * 1.05f;
            EnemyProjectile.Spawn(
                origin,
                aimPoint - origin,
                projectileSpeed,
                attackDamage * (heavy ? heavyDamageMultiplier : 1f),
                maxLifetime: 3.5f
            );
        }

        public void ApplyHit(
            Vector3 sourcePosition,
            float staggerDuration = 0.2f,
            float pushDistance = 1.8f)
        {
            if (!self.IsAlive)
            {
                return;
            }

            Vector3 direction = transform.position - sourcePosition;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
            {
                direction = -transform.forward;
            }
            Vector3 impactDirection = -direction.normalized;
            bool blocked = archetype == EnemyArchetype.Shield
                && Vector3.Dot(transform.forward, impactDirection) > 0.2f;
            CancelAttackWindup();
            staggerRemaining = Mathf.Max(
                staggerRemaining,
                staggerDuration * (blocked ? 0.35f : 1f)
            );
            knockbackVelocity = direction.normalized
                * pushDistance
                * (blocked ? 0.18f : 1f);
        }

        public float ModifyIncomingDamage(
            float damage,
            Vector3 sourcePosition)
        {
            if (archetype != EnemyArchetype.Shield || damage <= 0f)
            {
                return damage;
            }

            Vector3 toSource = sourcePosition - transform.position;
            toSource.y = 0f;
            if (toSource.sqrMagnitude < 0.001f)
            {
                return damage;
            }

            bool blocked = Vector3.Dot(
                transform.forward,
                toSource.normalized
            ) > 0.2f;
            return blocked
                ? damage * Mathf.Clamp(shieldDamageMultiplier, 0.05f, 1f)
                : damage;
        }

        private void CancelAttackWindup()
        {
            if (!isWindingUp)
            {
                return;
            }

            isWindingUp = false;
            isHeavyAttack = false;
            attackWindupRemaining = 0f;
            colorTint?.Configure(new Color(0.68f, 0.16f, 0.16f));
        }

        private void MoveForArchetype(
            Vector3 direction,
            float distance)
        {
            Vector3 movement = Vector3.zero;
            if (IsRanged)
            {
                if (distance > rangedPreferredDistance + 0.65f)
                {
                    movement = direction * moveSpeed;
                }
                else if (distance < rangedMinimumDistance)
                {
                    movement = -direction * moveSpeed;
                }
                else
                {
                    Vector3 strafe = Vector3.Cross(
                        Vector3.up,
                        direction
                    );
                    movement = strafe * (moveSpeed * 0.38f);
                }
            }
            else if (distance > attackDistance)
            {
                movement = direction * moveSpeed;
            }

            controller.Move(
                (movement + Vector3.up * verticalVelocity)
                * Time.deltaTime
            );
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

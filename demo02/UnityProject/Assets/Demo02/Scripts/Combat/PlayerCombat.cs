using System.Collections.Generic;
using Demo02.Core;
using Demo02.Player;
using UnityEngine;
using Demo02.UI;

namespace Demo02.Combat
{
    public sealed class PlayerCombat : MonoBehaviour
    {
        private const int ComboSteps = 3;

        [SerializeField, Min(0f)] private float attackDamage = 28f;
        [SerializeField, Min(0f)] private float attackRange = 1.8f;
        [SerializeField, Min(0f)] private float attackRadius = 1.2f;
        [SerializeField, Min(0f)] private float attackCooldown = 0.28f;
        [SerializeField, Min(0.1f)] private float comboWindow = 0.72f;
        [SerializeField, Min(0.1f)] private float counterWindow = 1.4f;
        [SerializeField, Min(1f)] private float counterDamageMultiplier = 1.55f;
        [SerializeField] private LayerMask targetMask = ~0;

        private readonly Collider[] overlaps = new Collider[12];
        private readonly HashSet<Combatant> hitObjects = new();
        private readonly MeleeComboTimeline combo = new(ComboSteps);
        private ThirdPersonController movement;
        private CharacterRuntime characterRuntime;
        private ProceduralCharacterAnimator proceduralAnimator;
        private ReadyMadeCharacterAnimator readyMadeAnimator;
        private float nextAttackTime;
        private float counterReadyUntil;
        private bool attackQueued;
        private GameObject attackVisual;
        private Vector3 attackVisualBasePosition;
        private Vector3 attackVisualBaseScale;
        private Quaternion attackVisualBaseRotation;

        public int CurrentComboStep => combo.CurrentStep;
        public int MaxComboSteps => combo.MaxSteps;
        public bool IsComboWindowOpen =>
            combo.IsWindowOpen(Time.time, comboWindow);
        public bool IsPerfectDodgeCounterReady =>
            Time.time < counterReadyUntil;

        private void Awake()
        {
            movement = GetComponent<ThirdPersonController>();
            characterRuntime = GetComponent<CharacterRuntime>();
            proceduralAnimator = GetComponent<ProceduralCharacterAnimator>();
            readyMadeAnimator =
                GetComponentInChildren<ReadyMadeCharacterAnimator>();
            Transform visual = transform.Find("MeleeArc");
            attackVisual = visual != null ? visual.gameObject : null;
            if (visual != null)
            {
                attackVisualBasePosition = visual.localPosition;
                attackVisualBaseScale = visual.localScale;
                attackVisualBaseRotation = visual.localRotation;
            }
        }

        private void OnDisable()
        {
            attackQueued = false;
            combo.Reset();
            counterReadyUntil = 0f;
        }

        public void ConfigureMeleeProfile(
            float damage,
            float range,
            float radius,
            float cooldown,
            float chainWindow)
        {
            attackDamage = Mathf.Max(0f, damage);
            attackRange = Mathf.Max(0.1f, range);
            attackRadius = Mathf.Max(0.1f, radius);
            attackCooldown = Mathf.Max(0.05f, cooldown);
            comboWindow = Mathf.Max(0.1f, chainWindow);
            counterWindow = 1.4f;
            counterDamageMultiplier = 1.55f;
        }

        public void RewardPerfectDodge()
        {
            counterReadyUntil = Time.time + counterWindow;
            characterRuntime?.GainEnergy(10f);
        }

        private void Update()
        {
            if (Time.timeScale <= 0f
                || (ScreenFade.Instance != null
                    && ScreenFade.Instance.IsTransitioning)
                || GameplayUiState.IsGameplayBlocked)
            {
                attackQueued = false;
                return;
            }

            if (movement != null && movement.IsInvulnerable)
            {
                attackQueued = false;
                combo.Reset();
                return;
            }
            if (combo.CurrentStep > 0 && !IsComboWindowOpen)
            {
                combo.Reset();
            }

            if (Demo02Input.AttackPressed)
            {
                attackQueued = true;
            }
            if (Time.time < nextAttackTime)
            {
                return;
            }

            bool heldChain = Demo02Input.AttackHeld
                && combo.CurrentStep > 0
                && IsComboWindowOpen;
            if (!attackQueued && !heldChain)
            {
                return;
            }

            attackQueued = false;
            int step = combo.RegisterAttack(Time.time, comboWindow);
            float stepCooldown = attackCooldown
                * (step == ComboSteps ? 1.75f : step == 2 ? 1.1f : 1f);
            nextAttackTime = Time.time + stepCooldown;
            Attack(step);
        }

        private void Attack(int comboStep)
        {
            float range = attackRange
                + (comboStep - 1) * 0.12f
                + (comboStep == ComboSteps ? 0.12f : 0f);
            float radius = attackRadius
                + (comboStep - 1) * 0.18f
                + (comboStep == ComboSteps ? 0.24f : 0f);
            float damageMultiplier = comboStep == ComboSteps
                ? 1.55f
                : comboStep == 2
                    ? 1.12f
                    : 1f;
            float poiseDamage = comboStep == ComboSteps
                ? 52f
                : comboStep == 2
                    ? 28f
                    : 20f;
            float pushDistance = comboStep == ComboSteps
                ? 3.2f
                : comboStep == 2
                    ? 2f
                    : 1.6f;
            float staggerDuration = comboStep == ComboSteps ? 0.36f : 0.2f;
            float energyGain = comboStep == ComboSteps
                ? 18f
                : comboStep == 2
                    ? 10f
                    : 8f;
            bool counterAttack = IsPerfectDodgeCounterReady;
            if (counterAttack)
            {
                counterReadyUntil = 0f;
                damageMultiplier *= counterDamageMultiplier;
                poiseDamage *= 1.2f;
                energyGain += 6f;
            }

            Combatant assistedTarget = FindAssistTarget(range, radius);
            if (assistedTarget != null)
            {
                Vector3 direction =
                    assistedTarget.transform.position - transform.position;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(
                        direction.normalized,
                        Vector3.up
                    );
                }
            }

            ShowAttackVisual(comboStep);
            GameAudio.PlayAttackSwing();
            proceduralAnimator?.PlayAttack();
            readyMadeAnimator?.PlayAttack();
            Vector3 center =
                transform.position + Vector3.up + transform.forward * range;
            int count = Physics.OverlapSphereNonAlloc(
                center,
                radius,
                overlaps,
                targetMask,
                QueryTriggerInteraction.Ignore
            );

            hitObjects.Clear();
            int hits = 0;
            for (int index = 0; index < count; index++)
            {
                Combatant target = overlaps[index].GetComponentInParent<Combatant>();
                if (target == null
                    || target.Team != CombatTeam.Enemy
                    || !target.IsAlive
                    || !hitObjects.Add(target))
                {
                    continue;
                }

                EnemyBreakStatus breakStatus =
                    target.GetComponent<EnemyBreakStatus>();
                breakStatus?.ApplyPoiseDamage(poiseDamage);
                float finalDamage = attackDamage * damageMultiplier
                    * (breakStatus?.DamageMultiplier ?? 1f);
                EnemyAI enemyAi = target.GetComponent<EnemyAI>();
                finalDamage = enemyAi != null
                    ? enemyAi.ModifyIncomingDamage(
                        finalDamage,
                        transform.position
                    )
                    : finalDamage;
                float previousHealth = target.CurrentHealth;
                target.TakeDamage(finalDamage);
                CombatFeedback.ReportDamage(
                    target.transform.position,
                    previousHealth - target.CurrentHealth
                );
                enemyAi?.ApplyHit(
                    transform.position,
                    staggerDuration,
                    pushDistance
                );
                characterRuntime?.GainEnergy(energyGain);
                hits++;
            }

            Debug.Log(
                $"PlayerCombat combo step {comboStep}/{ComboSteps} " +
                $"resolved with {hits} hit(s)" +
                (counterAttack ? " and a perfect-dodge counter." : ".")
            );
            if (hits > 0)
            {
                GameAudio.PlayHit();
                movement?.AddCameraShake(
                    comboStep == ComboSteps ? 0.16f : 0.075f,
                    comboStep == ComboSteps ? 0.16f : 0.09f
                );
            }
        }

        private Combatant FindAssistTarget(float range, float radius)
        {
            return CombatTargeting.FindNearestEnemy(
                transform,
                range + radius + 1.2f,
                out _,
                minimumFacingDot: 0.12f
            );
        }

        private void ShowAttackVisual(int comboStep)
        {
            if (attackVisual == null)
            {
                return;
            }

            float scale = comboStep == ComboSteps
                ? 1.55f
                : comboStep == 2
                    ? 1.18f
                    : 1f;
            attackVisual.transform.localPosition =
                attackVisualBasePosition
                + Vector3.forward * ((comboStep - 1) * 0.12f);
            attackVisual.transform.localScale =
                attackVisualBaseScale
                * scale;
            attackVisual.transform.localRotation =
                attackVisualBaseRotation
                * Quaternion.Euler(
                    0f,
                    comboStep == ComboSteps ? -18f : 12f,
                    0f
                );
            attackVisual.SetActive(true);
            CancelInvoke(nameof(HideAttackVisual));
            Invoke(
                nameof(HideAttackVisual),
                comboStep == ComboSteps ? 0.2f : 0.12f
            );
        }

        private void HideAttackVisual()
        {
            if (attackVisual != null)
            {
                attackVisual.SetActive(false);
            }
        }
    }
}

using System.Collections.Generic;
using Demo02.Core;
using Demo02.Player;
using Demo02.UI;
using UnityEngine;

namespace Demo02.Combat
{
    [DisallowMultipleComponent]
    public sealed class CharacterAbilityController : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float pathDashDistance = 4.2f;
        [SerializeField, Min(0f)] private float burstLockout = 0.8f;

        private CharacterRuntime characterRuntime;
        private Combatant self;
        private CharacterController motor;
        private AbilityPulseVisual pulseVisual;
        private float skillCooldownRemaining;
        private float nextBurstTime;

        public float SkillCooldownRemaining => skillCooldownRemaining;
        public float SkillCooldownRatio
        {
            get
            {
                float duration = characterRuntime?.Definition != null
                    ? characterRuntime.Definition.SkillCooldown
                    : 0f;
                return duration <= 0f
                    ? 0f
                    : Mathf.Clamp01(skillCooldownRemaining / duration);
            }
        }
        public bool IsBurstReady =>
            characterRuntime != null
            && characterRuntime.CurrentEnergy
                >= characterRuntime.MaxEnergy - 0.01f;

        private void Awake()
        {
            characterRuntime = GetComponent<CharacterRuntime>();
            self = GetComponent<Combatant>();
            motor = GetComponent<CharacterController>();
            pulseVisual = GetComponentInChildren<AbilityPulseVisual>(true);
        }

        private void Update()
        {
            skillCooldownRemaining = Mathf.Max(
                0f,
                skillCooldownRemaining - Time.deltaTime
            );
            if (!CanReadInput())
            {
                return;
            }

            if (Demo02Input.SkillPressed)
            {
                TryUseSkill();
            }
            if (Demo02Input.BurstPressed)
            {
                TryUseBurst();
            }
        }

        public bool TryUseSkill()
        {
            CharacterDefinition definition = characterRuntime?.Definition;
            if (definition == null
                || skillCooldownRemaining > 0f
                || Time.timeScale <= 0f
                || self == null
                || !self.IsAlive)
            {
                return false;
            }

            skillCooldownRemaining = definition.SkillCooldown;
            bool pathDash = definition.SkillStyle
                == CharacterSkillStyle.PathDash;
            if (pathDash && motor != null)
            {
                motor.Move(
                    transform.forward * pathDashDistance
                    + Vector3.down * 0.05f
                );
            }

            Vector3 center = pathDash
                ? transform.position
                : transform.position + transform.forward * 1.7f;
            int hits = ApplyAreaDamage(
                center,
                definition.SkillRadius,
                definition.SkillDamage,
                2.4f,
                requireForward: !pathDash
            );

            PlayAbilityAnimation();
            pulseVisual?.Play(
                definition.AccentColor,
                definition.SkillRadius
            );
            GameAudio.PlayAttackSwing();
            if (hits > 0)
            {
                GameAudio.PlayHit();
                GetComponent<ThirdPersonController>()
                    ?.AddCameraShake(0.11f, 0.13f);
            }
            characterRuntime.GainEnergy(hits > 0 ? 18f : 10f);
            Debug.Log(
                $"{definition.DisplayName} used {definition.SkillName}: "
                + $"{hits} hit(s)."
            );
            return true;
        }

        public bool TryUseBurst()
        {
            CharacterDefinition definition = characterRuntime?.Definition;
            if (definition == null
                || !IsBurstReady
                || Time.time < nextBurstTime
                || Time.timeScale <= 0f
                || self == null
                || !self.IsAlive
                || !characterRuntime.TrySpendEnergy(
                    characterRuntime.MaxEnergy
                ))
            {
                return false;
            }

            nextBurstTime = Time.time + burstLockout;
            int hits = ApplyAreaDamage(
                transform.position,
                definition.BurstRadius,
                definition.BurstDamage,
                3.6f,
                requireForward: false
            );
            if (definition.BurstHealing > 0f)
            {
                self.RestoreHealth(
                    Mathf.Min(
                        self.MaxHealth,
                        self.CurrentHealth + definition.BurstHealing
                    )
                );
            }

            PlayAbilityAnimation();
            pulseVisual?.Play(
                definition.AccentColor,
                definition.BurstRadius
            );
            GameAudio.PlayConfirm();
            if (hits > 0)
            {
                GameAudio.PlayHit();
                GetComponent<ThirdPersonController>()
                    ?.AddCameraShake(0.16f, 0.18f);
            }
            Debug.Log(
                $"{definition.DisplayName} used {definition.BurstName}: "
                + $"{hits} hit(s)."
            );
            return true;
        }

        private int ApplyAreaDamage(
            Vector3 center,
            float radius,
            float damage,
            float pushDistance,
            bool requireForward)
        {
            int hits = 0;
            float radiusSquared = radius * radius;
            Vector3 forward = transform.forward;
            IReadOnlyList<Combatant> enemies = CombatRegistry.Enemies;
            for (int index = 0; index < enemies.Count; index++)
            {
                Combatant enemy = enemies[index];
                if (enemy == null
                    || enemy.Team != CombatTeam.Enemy
                    || !enemy.IsAlive)
                {
                    continue;
                }

                Vector3 offset = enemy.transform.position - center;
                offset.y = 0f;
                if (offset.sqrMagnitude > radiusSquared)
                {
                    continue;
                }
                if (requireForward)
                {
                    Vector3 direction = enemy.transform.position
                        - transform.position;
                    direction.y = 0f;
                    if (direction.sqrMagnitude > 0.001f
                        && Vector3.Dot(forward, direction.normalized) < 0.05f)
                    {
                        continue;
                    }
                }

                EnemyBreakStatus breakStatus =
                    enemy.GetComponent<EnemyBreakStatus>();
                breakStatus?.ApplyPoiseDamage(
                    damage >= 60f ? 45f : 28f
                );
                float finalDamage = damage
                    * (breakStatus?.DamageMultiplier ?? 1f);
                EnemyAI enemyAi = enemy.GetComponent<EnemyAI>();
                finalDamage = enemyAi != null
                    ? enemyAi.ModifyIncomingDamage(
                        finalDamage,
                        transform.position
                    )
                    : finalDamage;
                float previousHealth = enemy.CurrentHealth;
                enemy.TakeDamage(finalDamage);
                CombatFeedback.ReportDamage(
                    enemy.transform.position,
                    previousHealth - enemy.CurrentHealth
                );
                ResonanceStatus status =
                    enemy.GetComponent<ResonanceStatus>();
                if (status != null && status.TryApply(
                        characterRuntime.Definition.ResonanceMark,
                        6f,
                        out ResonanceMark previousMark
                    ))
                {
                    previousHealth = enemy.CurrentHealth;
                    float reactionDamage = enemyAi != null
                        ? enemyAi.ModifyIncomingDamage(
                            20f,
                            transform.position
                        )
                        : 20f;
                    enemy.TakeDamage(reactionDamage);
                    CombatFeedback.ReportDamage(
                        enemy.transform.position,
                        previousHealth - enemy.CurrentHealth,
                        isReaction: true
                    );
                    Debug.Log(
                        $"Time-state reaction: {previousMark} + "
                        + $"{characterRuntime.Definition.ResonanceMark}."
                    );
                }
                enemyAi?.ApplyHit(
                    transform.position,
                    0.28f,
                    pushDistance
                );
                hits++;
            }
            return hits;
        }

        private void PlayAbilityAnimation()
        {
            GetComponent<ProceduralCharacterAnimator>()?.PlayAttack();
            GetComponentInChildren<ReadyMadeCharacterAnimator>()
                ?.PlayAttack();
        }

        private static bool CanReadInput()
        {
            return Time.timeScale > 0f
                && (ScreenFade.Instance == null
                    || !ScreenFade.Instance.IsTransitioning)
                && !GameplayUiState.IsGameplayBlocked;
        }
    }
}

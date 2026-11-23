using System.Collections.Generic;
using Demo02.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using Demo02.UI;

namespace Demo02.Combat
{
    public sealed class PlayerCombat : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float attackDamage = 30f;
        [SerializeField, Min(0f)] private float attackRange = 1.8f;
        [SerializeField, Min(0f)] private float attackRadius = 1.2f;
        [SerializeField, Min(0f)] private float attackCooldown = 0.35f;
        [SerializeField] private LayerMask targetMask = ~0;

        private readonly Collider[] overlaps = new Collider[12];
        private readonly HashSet<Combatant> hitObjects = new();
        private float nextAttackTime;
        private GameObject attackVisual;

        private void Awake()
        {
            Transform visual = transform.Find("MeleeArc");
            attackVisual = visual != null ? visual.gameObject : null;
        }

        private void Update()
        {
            if (Time.timeScale <= 0f
                || (ScreenFade.Instance != null
                    && ScreenFade.Instance.IsTransitioning)
                || GameplayUiState.IsGameplayBlocked)
            {
                return;
            }

            Mouse mouse = Mouse.current;
            if (mouse == null
                || !mouse.leftButton.isPressed
                || Time.time < nextAttackTime)
            {
                return;
            }

            nextAttackTime = Time.time + attackCooldown;
            Attack();
        }

        private void Attack()
        {
            Combatant assistedTarget = FindAssistTarget();
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

            ShowAttackVisual();
            GameAudio.PlayAttackSwing();
            GetComponent<Demo02.Player.ProceduralCharacterAnimator>()?.PlayAttack();
            GetComponentInChildren<Demo02.Player.ReadyMadeCharacterAnimator>()?.PlayAttack();
            Vector3 center = transform.position + Vector3.up + transform.forward * attackRange;
            int count = Physics.OverlapSphereNonAlloc(
                center,
                attackRadius,
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
                breakStatus?.ApplyPoiseDamage(24f);
                float finalDamage = attackDamage
                    * (breakStatus?.DamageMultiplier ?? 1f);
                float previousHealth = target.CurrentHealth;
                target.TakeDamage(finalDamage);
                CombatFeedback.ReportDamage(
                    target.transform.position,
                    previousHealth - target.CurrentHealth
                );
                target.GetComponent<EnemyAI>()?.ApplyHit(transform.position);
                GetComponent<Demo02.Player.CharacterRuntime>()
                    ?.GainEnergy(12f);
                hits++;
            }

            Debug.Log($"PlayerCombat attack resolved with {hits} hit(s).");
            if (hits > 0)
            {
                GameAudio.PlayHit();
                GetComponent<Demo02.Player.ThirdPersonController>()
                    ?.AddCameraShake(0.075f, 0.09f);
            }
        }

        private Combatant FindAssistTarget()
        {
            return CombatTargeting.FindNearestEnemy(
                transform,
                attackRange + attackRadius + 1.2f,
                out _,
                minimumFacingDot: 0.12f
            );
        }

        private void ShowAttackVisual()
        {
            if (attackVisual == null)
            {
                return;
            }

            attackVisual.SetActive(true);
            CancelInvoke(nameof(HideAttackVisual));
            Invoke(nameof(HideAttackVisual), 0.12f);
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

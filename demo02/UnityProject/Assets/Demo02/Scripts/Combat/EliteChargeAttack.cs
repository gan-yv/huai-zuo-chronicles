using System.Collections;
using Demo02.Core;
using Demo02.Player;
using UnityEngine;

namespace Demo02.Combat
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class EliteChargeAttack : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float windup = 0.85f;
        [SerializeField, Min(0f)] private float dashDistance = 6f;
        [SerializeField, Min(0.05f)] private float dashDuration = 0.18f;
        [SerializeField, Min(0f)] private float damage = 18f;
        [SerializeField, Min(0.1f)] private float hitRadius = 1.8f;

        private EliteEnemyController elite;
        private EnemyAI enemyAi;
        private Combatant health;
        private CharacterController motor;
        private float nextChargeTime;
        private bool charging;
        private Vector3 chargeDirection;

        public bool IsCharging => charging;
        public float Windup => windup;

        private void Awake()
        {
            elite = GetComponent<EliteEnemyController>();
            enemyAi = GetComponent<EnemyAI>();
            health = GetComponent<Combatant>();
            motor = GetComponent<CharacterController>();
        }

        private void Update()
        {
            if (charging
                || elite == null
                || elite.Phase < 2
                || health == null
                || !health.IsAlive
                || Time.time < nextChargeTime
                || Time.timeScale <= 0f)
            {
                return;
            }

            Transform player = FindAnyObjectByType<PlayerPartyController>()
                ?.ActiveCharacter;
            if (player == null)
            {
                return;
            }

            Vector3 offset = player.position - transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude <= 12f * 12f)
            {
                StartCoroutine(ChargeAt(player));
            }
        }

        private IEnumerator ChargeAt(Transform player)
        {
            charging = true;
            Vector3 direction = player.position - transform.position;
            direction.y = 0f;
            chargeDirection = direction.sqrMagnitude > 0.01f
                ? direction.normalized
                : transform.forward;
            transform.rotation = Quaternion.LookRotation(
                chargeDirection,
                Vector3.up
            );
            if (enemyAi != null)
            {
                enemyAi.SetActionLocked(true);
            }

            yield return new WaitForSeconds(windup);

            if (health == null || !health.IsAlive)
            {
                FinishCharge();
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < dashDuration)
            {
                elapsed += Time.deltaTime;
                if (motor != null && motor.enabled)
                {
                    motor.Move(
                        chargeDirection
                        * (dashDistance / dashDuration)
                        * Time.deltaTime
                    );
                }
                yield return null;
            }

            if (player == null)
            {
                FinishCharge();
                yield break;
            }

            Vector3 offset = player.position - transform.position;
            offset.y = 0f;
            if (offset.magnitude <= hitRadius)
            {
                ThirdPersonController controller =
                    player.GetComponent<ThirdPersonController>();
                if (controller == null || !controller.IsInvulnerable)
                {
                    Combatant target = player.GetComponent<Combatant>();
                    float previousHealth = target != null
                        ? target.CurrentHealth
                        : 0f;
                    target?.TakeDamage(damage);
                    if (target != null)
                    {
                        CombatFeedback.ReportDamage(
                            player.position + Vector3.up * 1.8f,
                            previousHealth - target.CurrentHealth,
                            isPlayerDamage: true
                        );
                        CombatFeedback.ReportPlayerHurt();
                        controller?.AddCameraShake(0.17f, 0.18f);
                        GameAudio.PlayPlayerHurt();
                    }
                }
            }
            FinishCharge();
        }

        private void FinishCharge()
        {
            charging = false;
            nextChargeTime = Time.time
                + (elite != null && elite.Phase >= 3 ? 3.5f : 5.5f);
            if (enemyAi != null && health != null && health.IsAlive)
            {
                enemyAi.SetActionLocked(false);
            }
        }
    }
}

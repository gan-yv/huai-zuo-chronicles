using Demo02.Core;
using Demo02.Player;
using UnityEngine;

namespace Demo02.Combat
{
    public sealed class EnemyProjectile : MonoBehaviour
    {
        private readonly RaycastHit[] hits = new RaycastHit[8];
        private Vector3 direction;
        private float speed;
        private float damage;
        private float lifetime;
        private LayerMask targetMask = ~0;

        public static EnemyProjectile Spawn(
            Vector3 position,
            Vector3 travelDirection,
            float travelSpeed,
            float hitDamage,
            float maxLifetime = 4f)
        {
            GameObject visual =
                GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "EnemyProjectile";
            visual.transform.position = position;
            visual.transform.localScale = Vector3.one * 0.22f;
            Object.DestroyImmediate(visual.GetComponent<SphereCollider>());
            visual.AddComponent<ColorTint>().Configure(
                new Color(1f, 0.42f, 0.12f),
                0.65f
            );
            EnemyProjectile projectile =
                visual.AddComponent<EnemyProjectile>();
            projectile.Configure(
                travelDirection,
                travelSpeed,
                hitDamage,
                maxLifetime
            );
            return projectile;
        }

        public void Configure(
            Vector3 travelDirection,
            float travelSpeed,
            float hitDamage,
            float maxLifetime)
        {
            direction = travelDirection.sqrMagnitude > 0.001f
                ? travelDirection.normalized
                : Vector3.forward;
            speed = Mathf.Max(0.5f, travelSpeed);
            damage = Mathf.Max(0f, hitDamage);
            lifetime = Mathf.Max(0.1f, maxLifetime);
            transform.rotation = Quaternion.LookRotation(direction);
        }

        private void Update()
        {
            if (Time.timeScale <= 0f)
            {
                return;
            }

            lifetime -= Time.deltaTime;
            if (lifetime <= 0f || transform.position.y < -3f)
            {
                Destroy(gameObject);
                return;
            }

            float distance = speed * Time.deltaTime;
            Vector3 origin = transform.position;
            int count = Physics.SphereCastNonAlloc(
                origin,
                0.18f,
                direction,
                hits,
                distance,
                targetMask,
                QueryTriggerInteraction.Ignore
            );
            for (int index = 0; index < count; index++)
            {
                Combatant target =
                    hits[index].collider.GetComponentInParent<Combatant>();
                if (target == null
                    || target.Team != CombatTeam.Player
                    || !target.IsAlive)
                {
                    continue;
                }

                ResolveHit(target);
                return;
            }

            transform.position = origin + direction * distance;
        }

        private void ResolveHit(Combatant target)
        {
            ThirdPersonController controller =
                target.GetComponent<ThirdPersonController>();
            if (controller != null && controller.IsInvulnerable)
            {
                target.GetComponent<PlayerCombat>()
                    ?.RewardPerfectDodge();
                GameAudio.PlayConfirm();
                Destroy(gameObject);
                return;
            }

            float previousHealth = target.CurrentHealth;
            target.TakeDamage(damage);
            CombatFeedback.ReportDamage(
                target.transform.position + Vector3.up * 1.8f,
                previousHealth - target.CurrentHealth,
                isPlayerDamage: true
            );
            CombatFeedback.ReportPlayerHurt();
            controller?.AddCameraShake(0.06f, 0.1f);
            GameAudio.PlayPlayerHurt();
            Destroy(gameObject);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Demo02.Combat
{
    public sealed class PlayerCombat : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float attackDamage = 30f;
        [SerializeField, Min(0f)] private float attackRange = 1.4f;
        [SerializeField, Min(0f)] private float attackRadius = 1f;
        [SerializeField, Min(0f)] private float attackCooldown = 0.4f;
        [SerializeField] private LayerMask targetMask = ~0;

        private readonly Collider[] overlaps = new Collider[12];
        private readonly HashSet<Combatant> hitObjects = new();
        private float nextAttackTime;

        private void Update()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null
                || !mouse.leftButton.wasPressedThisFrame
                || Time.time < nextAttackTime)
            {
                return;
            }

            nextAttackTime = Time.time + attackCooldown;
            Attack();
        }

        private void Attack()
        {
            Vector3 center = transform.position + Vector3.up + transform.forward * attackRange;
            int count = Physics.OverlapSphereNonAlloc(
                center,
                attackRadius,
                overlaps,
                targetMask,
                QueryTriggerInteraction.Ignore
            );

            hitObjects.Clear();
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

                target.TakeDamage(attackDamage);
            }
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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
            ShowAttackVisual();
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

                target.TakeDamage(attackDamage);
                hits++;
            }

            Debug.Log($"PlayerCombat attack resolved with {hits} hit(s).");
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

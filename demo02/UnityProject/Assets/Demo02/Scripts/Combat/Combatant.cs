using System;
using UnityEngine;

namespace Demo02.Combat
{
    public enum CombatTeam
    {
        Player,
        Enemy
    }

    [DisallowMultipleComponent]
    public sealed class Combatant : MonoBehaviour
    {
        [SerializeField] private CombatTeam team;
        [SerializeField, Min(1f)] private float maxHealth = 100f;

        private float currentHealth;

        public event Action<Combatant> HealthChanged;
        public event Action<Combatant> Died;

        public CombatTeam Team => team;
        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth > 0f ? currentHealth : maxHealth;
        public bool IsAlive => CurrentHealth > 0f;

        private void Awake()
        {
            if (currentHealth <= 0f)
            {
                currentHealth = maxHealth;
            }
        }

        public void Configure(CombatTeam combatTeam, float health)
        {
            team = combatTeam;
            maxHealth = Mathf.Max(1f, health);
            currentHealth = maxHealth;
        }

        public void TakeDamage(float damage)
        {
            if (!IsAlive || damage <= 0f)
            {
                return;
            }

            currentHealth = Mathf.Max(0f, currentHealth - damage);
            HealthChanged?.Invoke(this);

            if (currentHealth <= 0f)
            {
                Died?.Invoke(this);
            }
        }
    }
}

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
        private bool isInitialized;

        public event Action<Combatant> HealthChanged;
        public event Action<Combatant> Died;

        public CombatTeam Team => team;
        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public bool IsAlive => CurrentHealth > 0f;

        private void Awake()
        {
            if (!isInitialized)
            {
                currentHealth = maxHealth;
                isInitialized = true;
            }
        }

        public void Configure(CombatTeam combatTeam, float health)
        {
            team = combatTeam;
            maxHealth = Mathf.Max(1f, health);
            currentHealth = maxHealth;
            isInitialized = true;
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

        public void RestoreHealth(float health)
        {
            maxHealth = Mathf.Max(1f, maxHealth);
            currentHealth = Mathf.Clamp(health, 0f, maxHealth);
            isInitialized = true;
            HealthChanged?.Invoke(this);
        }
    }
}

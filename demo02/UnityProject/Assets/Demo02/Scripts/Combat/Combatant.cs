using System;
using System.Text;
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
        [SerializeField] private string persistentId;

        private float currentHealth;
        private bool isInitialized;
        private string runtimePersistentId;

        public event Action<Combatant> HealthChanged;
        public event Action<Combatant> Died;

        public CombatTeam Team => team;
        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public bool IsAlive => CurrentHealth > 0f;
        public string PersistentId
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(persistentId))
                {
                    return persistentId;
                }
                if (string.IsNullOrWhiteSpace(runtimePersistentId))
                {
                    runtimePersistentId = BuildSceneScopedFallbackId();
                }
                return runtimePersistentId;
            }
        }

        private void Awake()
        {
            if (!isInitialized)
            {
                currentHealth = maxHealth;
                isInitialized = true;
            }
        }

        private void OnEnable()
        {
            CombatRegistry.Register(this);
        }

        private void OnDisable()
        {
            CombatRegistry.Unregister(this);
        }

        public void Configure(
            CombatTeam combatTeam,
            float health,
            string identity = null)
        {
            bool registered = isActiveAndEnabled;
            if (registered)
            {
                CombatRegistry.Unregister(this);
            }
            team = combatTeam;
            maxHealth = Mathf.Max(1f, health);
            currentHealth = maxHealth;
            isInitialized = true;
            if (!string.IsNullOrWhiteSpace(identity))
            {
                persistentId = identity;
                runtimePersistentId = identity;
            }
            if (registered)
            {
                CombatRegistry.Register(this);
            }
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

        private string BuildSceneScopedFallbackId()
        {
            string sceneName = gameObject.scene.IsValid()
                ? gameObject.scene.name
                : "DontDestroyOnLoad";
            StringBuilder path = new();
            Transform current = transform;
            while (current != null)
            {
                if (path.Length > 0)
                {
                    path.Insert(0, '/');
                }
                path.Insert(0, current.name);
                current = current.parent;
            }
            return $"scene:{sceneName}/{path}";
        }
    }
}

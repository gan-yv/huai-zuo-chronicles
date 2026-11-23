using System.Collections.Generic;
using UnityEngine;

namespace Demo02.Combat
{
    [CreateAssetMenu(
        fileName = "ENC_NewEncounter",
        menuName = "Demo02/Combat/Encounter Definition"
    )]
    public sealed class WorldEncounterDefinition : ScriptableObject
    {
        [SerializeField] private string encounterId = "new-encounter";
        [SerializeField] private string displayName = "新遭遇";
        [SerializeField, Min(1f)] private float activationRadius = 16f;
        [SerializeField, Min(1f)] private float enemyHealth = 60f;
        [SerializeField, Min(0f)] private float enemyDamage = 10f;
        [SerializeField, Min(0f)] private float enemyMoveSpeed = 2.2f;
        [SerializeField, Min(0f)] private float enemyAggroDistance = 12f;
        [SerializeField] private bool startsActive;
        [SerializeField] private List<string> supportEncounterIds = new();

        public string EncounterId => encounterId;
        public string DisplayName => displayName;
        public float ActivationRadius => activationRadius;
        public float EnemyHealth => enemyHealth;
        public float EnemyDamage => enemyDamage;
        public float EnemyMoveSpeed => enemyMoveSpeed;
        public float EnemyAggroDistance => enemyAggroDistance;
        public bool StartsActive => startsActive;
        public IReadOnlyList<string> SupportEncounterIds =>
            supportEncounterIds;

        public void ConfigureForEditor(
            string id,
            string encounterDisplayName,
            float radius,
            float health,
            float damage,
            float moveSpeed,
            float aggroDistance,
            bool activeByDefault,
            IEnumerable<string> supportIds)
        {
            encounterId = id;
            displayName = encounterDisplayName;
            activationRadius = Mathf.Max(1f, radius);
            enemyHealth = Mathf.Max(1f, health);
            enemyDamage = Mathf.Max(0f, damage);
            enemyMoveSpeed = Mathf.Max(0f, moveSpeed);
            enemyAggroDistance = Mathf.Max(0f, aggroDistance);
            startsActive = activeByDefault;
            supportEncounterIds = supportIds != null
                ? new List<string>(supportIds)
                : new List<string>();
        }

        public bool IsValid(out string error)
        {
            if (string.IsNullOrWhiteSpace(encounterId)
                || string.IsNullOrWhiteSpace(displayName)
                || activationRadius <= 0f
                || enemyHealth <= 0f)
            {
                error = $"{name}: encounter definition is incomplete";
                return false;
            }

            error = null;
            return true;
        }
    }
}

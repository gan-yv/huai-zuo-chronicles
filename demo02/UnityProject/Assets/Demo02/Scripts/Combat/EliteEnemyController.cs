using UnityEngine;

namespace Demo02.Combat
{
    public sealed class EliteEnemyController : MonoBehaviour
    {
        [SerializeField] private string displayName = "精英巡官";
        [SerializeField, Range(0.1f, 0.9f)] private float phaseTwoThreshold = 0.6f;
        [SerializeField, Range(0.05f, 0.5f)] private float enragedThreshold = 0.3f;

        private Combatant health;
        private EnemyAI enemyAi;
        private int phase = 1;

        public string DisplayName => displayName;
        public int Phase => phase;
        public EnemyArchetype PhaseArchetype => phase switch
        {
            2 => EnemyArchetype.Shield,
            3 => EnemyArchetype.Ranged,
            _ => EnemyArchetype.Chaser
        };
        public string PhaseLabel => phase switch
        {
            2 => "第二阶段 · 盾压冲锋",
            3 => "第三阶段 · 远距压制",
            _ => "第一阶段 · 追击"
        };
        public bool IsActive => health != null && health.IsAlive;

        public void Configure(string eliteName)
        {
            displayName = eliteName;
        }

        private void Awake()
        {
            health = GetComponent<Combatant>();
            enemyAi = GetComponent<EnemyAI>();
            enemyAi?.ConfigureArchetype(EnemyArchetype.Chaser);
        }

        private void Update()
        {
            EvaluatePhase();
        }

        public void EvaluatePhase()
        {
            health ??= GetComponent<Combatant>();
            enemyAi ??= GetComponent<EnemyAI>();
            if (health == null || !health.IsAlive)
            {
                return;
            }

            float ratio = health.CurrentHealth / health.MaxHealth;
            if (phase == 1 && ratio <= phaseTwoThreshold)
            {
                phase = 2;
                enemyAi?.ConfigureArchetype(EnemyArchetype.Shield);
                enemyAi?.ConfigureCombatProfile(
                    1.9f,
                    12f,
                    0.9f,
                    0.3f
                );
                Debug.Log($"{displayName} entered phase 2.");
            }
            else if (phase == 2 && ratio <= enragedThreshold)
            {
                phase = 3;
                enemyAi?.ConfigureArchetype(EnemyArchetype.Ranged);
                enemyAi?.ConfigureCombatProfile(
                    2.8f,
                    16f,
                    0.72f,
                    0.22f
                );
                Debug.Log($"{displayName} entered enrage phase.");
            }
        }
    }
}

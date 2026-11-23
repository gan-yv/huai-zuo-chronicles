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
        public string PhaseLabel => phase switch
        {
            2 => "第二阶段 · 压制",
            3 => "第三阶段 · 狂怒",
            _ => "第一阶段 · 巡守"
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
        }

        private void Update()
        {
            if (health == null || !health.IsAlive)
            {
                return;
            }

            float ratio = health.CurrentHealth / health.MaxHealth;
            if (phase == 1 && ratio <= phaseTwoThreshold)
            {
                phase = 2;
                enemyAi?.ConfigureCombatProfile(
                    2.7f,
                    12f,
                    0.9f,
                    0.3f
                );
                Debug.Log($"{displayName} entered phase 2.");
            }
            else if (phase == 2 && ratio <= enragedThreshold)
            {
                phase = 3;
                enemyAi?.ConfigureCombatProfile(
                    3.1f,
                    16f,
                    0.72f,
                    0.22f
                );
                Debug.Log($"{displayName} entered enrage phase.");
            }
        }
    }
}

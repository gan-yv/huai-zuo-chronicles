using UnityEngine;

namespace Demo02.Combat
{
    public sealed class EnemyBreakStatus : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float maxPoise = 60f;
        [SerializeField, Min(0f)] private float breakDuration = 2.2f;
        [SerializeField, Min(1f)] private float damageMultiplier = 1.35f;

        private float poise;
        private float brokenUntil;

        public int BreakCount { get; private set; }
        public float MaxPoise => maxPoise;
        public float CurrentPoise => poise;
        public float PoiseRatio => Mathf.Clamp01(poise / maxPoise);
        public bool IsBroken => Time.time < brokenUntil;
        public float DamageMultiplier => IsBroken ? damageMultiplier : 1f;

        private void Awake()
        {
            poise = maxPoise;
        }

        public void ApplyPoiseDamage(float amount)
        {
            if (amount <= 0f || IsBroken)
            {
                return;
            }

            poise = Mathf.Max(0f, poise - amount);
            if (poise > 0f)
            {
                return;
            }

            brokenUntil = Time.time + breakDuration;
            BreakCount++;
            CombatFeedback.ReportDamage(
                transform.position + Vector3.up * 2f,
                0f
            );
            Debug.Log($"{name} broke: incoming damage increased.");
        }

        private void Update()
        {
            if (!IsBroken && poise < maxPoise)
            {
                poise = Mathf.Min(
                    maxPoise,
                    poise + maxPoise * 0.18f * Time.deltaTime
                );
            }
        }
    }
}

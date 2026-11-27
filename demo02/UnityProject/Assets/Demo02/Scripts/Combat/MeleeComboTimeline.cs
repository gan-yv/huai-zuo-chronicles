using UnityEngine;

namespace Demo02.Combat
{
    public sealed class MeleeComboTimeline
    {
        private readonly int maxSteps;

        public MeleeComboTimeline(int stepCount)
        {
            maxSteps = Mathf.Max(1, stepCount);
        }

        public int MaxSteps => maxSteps;
        public int CurrentStep { get; private set; }
        public float LastAttackTime { get; private set; } =
            float.NegativeInfinity;

        public bool IsWindowOpen(float now, float chainWindow)
        {
            return CurrentStep > 0
                && now <= LastAttackTime + Mathf.Max(0.05f, chainWindow);
        }

        public int RegisterAttack(float now, float chainWindow)
        {
            bool chains = IsWindowOpen(now, chainWindow);
            CurrentStep = chains
                ? CurrentStep % maxSteps + 1
                : 1;
            LastAttackTime = now;
            return CurrentStep;
        }

        public void Reset()
        {
            CurrentStep = 0;
            LastAttackTime = float.NegativeInfinity;
        }
    }
}

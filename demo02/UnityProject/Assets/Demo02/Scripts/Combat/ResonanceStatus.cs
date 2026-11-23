using Demo02.Player;
using UnityEngine;

namespace Demo02.Combat
{
    [DisallowMultipleComponent]
    public sealed class ResonanceStatus : MonoBehaviour
    {
        [SerializeField] private ResonanceMark activeMark;
        [SerializeField] private float expiresAt;

        public static ResonanceMark LastPreviousMark { get; private set; }
        public static ResonanceMark LastAppliedMark { get; private set; }
        public static float LastReactionUntil { get; private set; }

        public ResonanceMark ActiveMark => activeMark;
        public bool HasActiveMark =>
            activeMark != ResonanceMark.Continuation
            || expiresAt > Time.time;

        public bool TryApply(
            ResonanceMark mark,
            float duration,
            out ResonanceMark previousMark)
        {
            bool hadMark = expiresAt > Time.time;
            previousMark = hadMark ? activeMark : ResonanceMark.Continuation;
            bool triggered = hadMark && activeMark != mark;

            activeMark = mark;
            expiresAt = Time.time + Mathf.Max(0.1f, duration);
            if (triggered)
            {
                LastPreviousMark = previousMark;
                LastAppliedMark = mark;
                LastReactionUntil = Time.unscaledTime + 1.8f;
            }
            return triggered;
        }

        public void Clear()
        {
            activeMark = ResonanceMark.Continuation;
            expiresAt = 0f;
        }
    }
}

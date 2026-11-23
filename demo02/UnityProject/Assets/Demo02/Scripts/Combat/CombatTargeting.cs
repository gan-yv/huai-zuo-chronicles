using UnityEngine;

namespace Demo02.Combat
{
    public static class CombatTargeting
    {
        public static Combatant FindNearestEnemy(
            Transform source,
            float maxDistance,
            out float distance,
            float minimumFacingDot = -1f)
        {
            distance = float.MaxValue;
            if (source == null)
            {
                return null;
            }

            Combatant nearest = null;
            Vector3 forward = source.forward;
            foreach (Combatant candidate in CombatRegistry.Enemies)
            {
                if (candidate == null
                    || candidate.Team != CombatTeam.Enemy
                    || !candidate.IsAlive)
                {
                    continue;
                }

                Vector3 offset = candidate.transform.position - source.position;
                offset.y = 0f;
                float candidateDistance = offset.magnitude;
                if (candidateDistance > maxDistance
                    || candidateDistance < 0.001f
                    || candidateDistance >= distance)
                {
                    continue;
                }
                if (minimumFacingDot > -1f
                    && Vector3.Dot(forward, offset.normalized)
                        < minimumFacingDot)
                {
                    continue;
                }

                nearest = candidate;
                distance = candidateDistance;
            }
            return nearest;
        }
    }
}
